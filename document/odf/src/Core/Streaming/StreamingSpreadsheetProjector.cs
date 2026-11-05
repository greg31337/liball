using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace odf;

internal class StreamingCellProjectionState
{
    internal OdfCell Cell = new OdfCell();
    internal OdfAttribute[] Attributes = [];
    internal StringBuilder Text = new StringBuilder();
    internal List<OdfTable> NestedTables = new List<OdfTable>();
    internal int CellDepth;
    internal int NestedTableDepth = -1;
    internal bool HasTopLevelParagraph;
}

internal class StreamingTableProjectionState
{
    internal OdfTable Table = new OdfTable();
    internal List<OdfRow> Rows = new List<OdfRow>();
    internal List<OdfCell> Cells = new List<OdfCell>();
    internal OdfRow? CurrentRow;
    internal StreamingCellProjectionState? CurrentCell;
    internal long RowIndex;
    internal long ColumnIndex;
    internal long MaximumColumnCount;
    internal int HeaderRowDepth;
    internal int RowGroupDepth;
}

internal class StreamingSpreadsheetState
{
    internal List<OdfTable> Tables = new List<OdfTable>();
    internal List<StreamingTableProjectionState> TableStack =
        new List<StreamingTableProjectionState>();
    internal int MaximumTextExpansion;
}

internal static class StreamingSpreadsheetProjector
{
    internal static OdfTable[] ProjectXmlTables(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        ValidateSpreadsheetXmlInputs(input, partPath, options);
        OdfLoadOptions readerOptions = CreateSpreadsheetXmlOptions(options);
        XmlEventReader reader = new XmlEventReader(
            input,
            partPath,
            readerOptions);
        return ReadProjectedTables(
            reader,
            readerOptions.SecurityLimits.MaximumXmlTextLength);
    }

    internal static OdfTable[] ProjectPackageTables(
        Stream input,
        OdfLoadOptions options)
    {
        ValidateSpreadsheetPackageInputs(input, options);
        OdfLoadOptions packageOptions =
            CreateSpreadsheetPackageOptions(options);
        PackageEventReader reader = new PackageEventReader(
            input,
            packageOptions);
        List<OdfTable> result = new List<OdfTable>();
        OdfStreamEvent streamEvent = reader.ReadNextEvent();
        while (streamEvent.Kind != OdfStreamEventKind.EndOfStream)
        {
            if (streamEvent.Kind == OdfStreamEventKind.PackageEntryStart)
            {
                if (IsSpreadsheetXmlPart(streamEvent.PartPath))
                {
                    byte[] content = reader.ReadCurrentEntryBytes();
                    AddProjectedPackageTables(
                        result,
                        content,
                        streamEvent.PartPath,
                        options);
                }
                else
                {
                    reader.SkipCurrentEntry();
                }
            }
            streamEvent = reader.ReadNextEvent();
        }
        return result.ToArray();
    }

    static OdfTable[] ReadProjectedTables(
        XmlEventReader reader,
        int maximumTextExpansion)
    {
        StreamingSpreadsheetState state =
            new StreamingSpreadsheetState();
        state.MaximumTextExpansion = maximumTextExpansion;
        OdfStreamEvent streamEvent = reader.ReadNextEvent();
        while (streamEvent.Kind != OdfStreamEventKind.EndOfStream)
        {
            if (streamEvent.Kind == OdfStreamEventKind.ElementStart)
            {
                CaptureCellElementStart(state, streamEvent);
                StartSpreadsheetElement(state, streamEvent);
            }
            else if (streamEvent.Kind == OdfStreamEventKind.ElementEnd)
            {
                FinishSpreadsheetElement(state, streamEvent);
                CaptureCellElementEnd(state, streamEvent);
            }
            else if (streamEvent.Kind == OdfStreamEventKind.Text)
            {
                CaptureCellText(state, streamEvent.Value);
            }
            streamEvent = reader.ReadNextEvent();
        }
        if (state.TableStack.Count != 0)
        {
            throw new OdfXmlException(
                "The spreadsheet event stream ended inside a table.");
        }
        return state.Tables.ToArray();
    }

    static void StartSpreadsheetElement(
        StreamingSpreadsheetState state,
        OdfStreamEvent streamEvent)
    {
        if (IsTableName(streamEvent.Name, "table"))
        {
            StartProjectedTable(state, streamEvent);
            return;
        }
        StreamingTableProjectionState? table = CurrentTableState(state);
        if (table == null)
        {
            return;
        }
        if (IsTableName(streamEvent.Name, "table-header-rows"))
        {
            table.HeaderRowDepth = table.HeaderRowDepth + 1;
        }
        else if (IsTableName(streamEvent.Name, "table-row-group"))
        {
            table.RowGroupDepth = table.RowGroupDepth + 1;
        }
        else if (IsTableName(streamEvent.Name, "table-row"))
        {
            StartProjectedRow(table, streamEvent);
        }
        else if (IsTableName(streamEvent.Name, "table-cell")
            || IsTableName(streamEvent.Name, "covered-table-cell"))
        {
            StartProjectedCell(table, streamEvent);
        }
    }

    static void FinishSpreadsheetElement(
        StreamingSpreadsheetState state,
        OdfStreamEvent streamEvent)
    {
        StreamingTableProjectionState? table = CurrentTableState(state);
        if (table == null)
        {
            return;
        }
        if (IsTableName(streamEvent.Name, "table-cell")
            || IsTableName(streamEvent.Name, "covered-table-cell"))
        {
            FinishProjectedCell(table);
        }
        else if (IsTableName(streamEvent.Name, "table-row"))
        {
            FinishProjectedRow(table);
        }
        else if (IsTableName(streamEvent.Name, "table-header-rows"))
        {
            if (table.HeaderRowDepth > 0)
            {
                table.HeaderRowDepth = table.HeaderRowDepth - 1;
            }
        }
        else if (IsTableName(streamEvent.Name, "table-row-group"))
        {
            if (table.RowGroupDepth > 0)
            {
                table.RowGroupDepth = table.RowGroupDepth - 1;
            }
        }
        else if (IsTableName(streamEvent.Name, "table"))
        {
            FinishProjectedTable(state);
        }
    }

    static void StartProjectedTable(
        StreamingSpreadsheetState state,
        OdfStreamEvent streamEvent)
    {
        StreamingTableProjectionState tableState =
            new StreamingTableProjectionState();
        OdfTable table = tableState.Table;
        table.Name = AttributeValue(
            streamEvent.Attributes,
            OdfNamespaces.Table,
            "name");
        table.StyleName = AttributeValue(
            streamEvent.Attributes,
            OdfNamespaces.Table,
            "style-name");
        table.PartPath = streamEvent.PartPath;
        StreamingTableProjectionState? parent = CurrentTableState(state);
        if (parent != null && parent.CurrentCell != null)
        {
            table.ParentTable = parent.Table;
            table.ParentCell = parent.CurrentCell.Cell;
        }
        state.Tables.Add(table);
        state.TableStack.Add(tableState);
    }

    static void FinishProjectedTable(StreamingSpreadsheetState state)
    {
        int index = state.TableStack.Count - 1;
        StreamingTableProjectionState tableState = state.TableStack[index];
        tableState.Table.Rows = tableState.Rows.ToArray();
        tableState.Table.RowCount = tableState.RowIndex;
        tableState.Table.ColumnCount = tableState.MaximumColumnCount;
        state.TableStack.RemoveAt(index);
        StreamingTableProjectionState? parent = CurrentTableState(state);
        if (parent != null && parent.CurrentCell != null)
        {
            parent.CurrentCell.NestedTables.Add(tableState.Table);
        }
    }

    static void StartProjectedRow(
        StreamingTableProjectionState table,
        OdfStreamEvent streamEvent)
    {
        if (table.CurrentRow != null)
        {
            throw new OdfXmlException(
                "A spreadsheet row cannot begin inside another row.");
        }
        OdfRow row = new OdfRow();
        row.StartIndex = table.RowIndex;
        row.RepeatCount = ReadPositiveCount(
            streamEvent.Attributes,
            "number-rows-repeated");
        row.IsHeader = table.HeaderRowDepth > 0;
        row.GroupDepth = table.RowGroupDepth;
        row.StyleName = AttributeValue(
            streamEvent.Attributes,
            OdfNamespaces.Table,
            "style-name");
        row.DefaultCellStyleName = AttributeValue(
            streamEvent.Attributes,
            OdfNamespaces.Table,
            "default-cell-style-name");
        string visibility = AttributeValue(
            streamEvent.Attributes,
            OdfNamespaces.Table,
            "visibility");
        if (visibility.Length > 0)
        {
            row.Visibility = visibility;
        }
        row.IsVisible = row.Visibility == "visible";
        row.Table = table.Table;
        table.CurrentRow = row;
        table.Cells = new List<OdfCell>();
        table.ColumnIndex = 0;
    }

    static void FinishProjectedRow(StreamingTableProjectionState table)
    {
        OdfRow? row = table.CurrentRow;
        if (row == null || table.CurrentCell != null)
        {
            throw new OdfXmlException(
                "A spreadsheet row ended without a complete cell sequence.");
        }
        row.Cells = table.Cells.ToArray();
        table.Rows.Add(row);
        table.RowIndex = AddSaturated(table.RowIndex, row.RepeatCount);
        if (table.ColumnIndex > table.MaximumColumnCount)
        {
            table.MaximumColumnCount = table.ColumnIndex;
        }
        table.CurrentRow = null;
        table.Cells = new List<OdfCell>();
        table.ColumnIndex = 0;
    }

    static void StartProjectedCell(
        StreamingTableProjectionState table,
        OdfStreamEvent streamEvent)
    {
        if (table.CurrentRow == null)
        {
            return;
        }
        if (table.CurrentCell != null)
        {
            throw new OdfXmlException(
                "A spreadsheet cell cannot begin inside another cell in the same table.");
        }
        StreamingCellProjectionState cellState =
            new StreamingCellProjectionState();
        OdfCell cell = cellState.Cell;
        cell.StartColumnIndex = table.ColumnIndex;
        cell.RepeatCount = ReadPositiveCount(
            streamEvent.Attributes,
            "number-columns-repeated");
        cell.IsCovered = IsTableName(
            streamEvent.Name,
            "covered-table-cell");
        cell.ColumnSpan = ReadPositiveCount(
            streamEvent.Attributes,
            "number-columns-spanned");
        cell.RowSpan = ReadPositiveCount(
            streamEvent.Attributes,
            "number-rows-spanned");
        cell.MatrixColumnSpan = ReadPositiveCount(
            streamEvent.Attributes,
            "number-matrix-columns-spanned");
        cell.MatrixRowSpan = ReadPositiveCount(
            streamEvent.Attributes,
            "number-matrix-rows-spanned");
        cell.IsMergeOrigin = cell.ColumnSpan > 1 || cell.RowSpan > 1;
        cell.IsMatrixFormula = cell.MatrixColumnSpan > 1
            || cell.MatrixRowSpan > 1;
        cell.StyleName = AttributeValue(
            streamEvent.Attributes,
            OdfNamespaces.Table,
            "style-name");
        cell.ValidationName = AttributeValue(
            streamEvent.Attributes,
            OdfNamespaces.Table,
            "content-validation-name");
        cell.Formula = AttributeValue(
            streamEvent.Attributes,
            OdfNamespaces.Table,
            "formula");
        cell.OriginalFormula = cell.Formula;
        cell.Row = table.CurrentRow;
        cellState.Attributes = streamEvent.Attributes;
        cellState.CellDepth = streamEvent.Depth;
        table.CurrentCell = cellState;
    }

    static void FinishProjectedCell(StreamingTableProjectionState table)
    {
        StreamingCellProjectionState? cellState = table.CurrentCell;
        if (cellState == null)
        {
            return;
        }
        cellState.Cell.NestedTables = cellState.NestedTables.ToArray();
        TableCellValueModel.InitializeStreamingCellValue(
            cellState.Cell,
            cellState.Attributes,
            cellState.Text.ToString());
        table.Cells.Add(cellState.Cell);
        table.ColumnIndex = AddSaturated(
            table.ColumnIndex,
            cellState.Cell.RepeatCount);
        table.CurrentCell = null;
    }

    static void CaptureCellElementStart(
        StreamingSpreadsheetState state,
        OdfStreamEvent streamEvent)
    {
        StreamingCellProjectionState? cell =
            CurrentContainingCell(state);
        if (cell == null)
        {
            return;
        }
        if (cell.NestedTableDepth >= 0)
        {
            return;
        }
        if (IsTableName(streamEvent.Name, "table"))
        {
            cell.NestedTableDepth = streamEvent.Depth;
            return;
        }
        if ((IsTextName(streamEvent.Name, "p")
                || IsTextName(streamEvent.Name, "h"))
            && streamEvent.Depth == cell.CellDepth + 1)
        {
            if (cell.HasTopLevelParagraph)
            {
                cell.Text.Append('\n');
            }
            cell.HasTopLevelParagraph = true;
        }
        if (IsTextName(streamEvent.Name, "tab"))
        {
            cell.Text.Append('\t');
        }
        else if (IsTextName(streamEvent.Name, "line-break"))
        {
            cell.Text.Append('\n');
        }
        else if (IsTextName(streamEvent.Name, "s"))
        {
            int count = ReadBoundedSpaceCount(
                streamEvent.Attributes,
                state.MaximumTextExpansion);
            int index = 0;
            while (index < count)
            {
                cell.Text.Append(' ');
                index = index + 1;
            }
        }
    }

    static void CaptureCellElementEnd(
        StreamingSpreadsheetState state,
        OdfStreamEvent streamEvent)
    {
        StreamingCellProjectionState? cell =
            CurrentContainingCell(state);
        if (cell != null && cell.NestedTableDepth == streamEvent.Depth
            && IsTableName(streamEvent.Name, "table"))
        {
            cell.NestedTableDepth = -1;
        }
    }

    static void CaptureCellText(
        StreamingSpreadsheetState state,
        string value)
    {
        StreamingCellProjectionState? cell =
            CurrentContainingCell(state);
        if (cell != null && cell.NestedTableDepth < 0)
        {
            cell.Text.Append(value);
        }
    }

    static StreamingCellProjectionState? CurrentContainingCell(
        StreamingSpreadsheetState state)
    {
        int index = state.TableStack.Count - 1;
        while (index >= 0)
        {
            StreamingCellProjectionState? cell =
                state.TableStack[index].CurrentCell;
            if (cell != null)
            {
                return cell;
            }
            index = index - 1;
        }
        return null;
    }

    static StreamingTableProjectionState? CurrentTableState(
        StreamingSpreadsheetState state)
    {
        if (state.TableStack.Count == 0)
        {
            return null;
        }
        return state.TableStack[state.TableStack.Count - 1];
    }

    static long ReadPositiveCount(
        OdfAttribute[] attributes,
        string localName)
    {
        string lexical = AttributeValue(
            attributes,
            OdfNamespaces.Table,
            localName);
        if (lexical.Length == 0)
        {
            return 1;
        }
        OdfLexicalValue value = OdfLexicalParser.ParseInteger(lexical);
        return value.IsValid && value.IntegerValue > 0
            ? value.IntegerValue
            : 1;
    }

    static int ReadBoundedSpaceCount(
        OdfAttribute[] attributes,
        int maximumTextExpansion)
    {
        string lexical = AttributeValue(
            attributes,
            OdfNamespaces.Text,
            "c");
        OdfLexicalValue value = OdfLexicalParser.ParseInteger(lexical);
        long count = value.IsValid && value.IntegerValue > 0
            ? value.IntegerValue
            : 1;
        int limit = maximumTextExpansion;
        if (limit > 1000000)
        {
            limit = 1000000;
        }
        if (count > limit)
        {
            return limit;
        }
        return (int)count;
    }

    static string AttributeValue(
        OdfAttribute[] attributes,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            OdfAttribute attribute = attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static bool IsTableName(
        OdfQualifiedName? name,
        string localName)
    {
        return name != null
            && name.NamespaceName == OdfNamespaces.Table
            && name.LocalName == localName;
    }

    static bool IsTextName(
        OdfQualifiedName? name,
        string localName)
    {
        return name != null
            && name.NamespaceName == OdfNamespaces.Text
            && name.LocalName == localName;
    }

    static long AddSaturated(long value, long count)
    {
        if (count > long.MaxValue - value)
        {
            return long.MaxValue;
        }
        return value + count;
    }

    static bool IsSpreadsheetXmlPart(string partPath)
    {
        return partPath.EndsWith(
            ".xml",
            StringComparison.OrdinalIgnoreCase);
    }

    static void AddProjectedPackageTables(
        List<OdfTable> result,
        byte[] content,
        string partPath,
        OdfLoadOptions options)
    {
        OdfLoadOptions xmlOptions = CreateSpreadsheetXmlOptions(options);
        xmlOptions.LeaveInputOpen = true;
        OdfTable[] tables;
        using (MemoryStream input = new MemoryStream(content, false))
        {
            tables = ProjectXmlTables(input, partPath, xmlOptions);
        }
        int index = 0;
        while (index < tables.Length)
        {
            result.Add(tables[index]);
            index = index + 1;
        }
    }

    static OdfLoadOptions CreateSpreadsheetXmlOptions(
        OdfLoadOptions source)
    {
        OdfLoadOptions result = CopySpreadsheetBaseOptions(source);
        result.StreamSelection = CopySpreadsheetSelection(
            source.StreamSelection);
        result.StreamSelection.IncludeElementEvents = true;
        result.StreamSelection.IncludeTextEvents = true;
        result.StreamSelection.IncludeCommentEvents = false;
        result.StreamSelection.IncludeProcessingInstructionEvents = false;
        return result;
    }

    static OdfLoadOptions CreateSpreadsheetPackageOptions(
        OdfLoadOptions source)
    {
        OdfLoadOptions result = CopySpreadsheetBaseOptions(source);
        result.StreamSelection = CopySpreadsheetSelection(
            source.StreamSelection);
        result.StreamSelection.IncludePackageEntryEvents = true;
        result.StreamSelection.IncludeBinaryContent = false;
        return result;
    }

    static OdfLoadOptions CopySpreadsheetBaseOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = new OdfLoadOptions();
        result.SecurityLimits = source.SecurityLimits;
        result.Cancellation = source.Cancellation;
        result.LeaveInputOpen = source.LeaveInputOpen;
        result.PreserveLexicalValues = source.PreserveLexicalValues;
        result.PreserveUnknownContent = source.PreserveUnknownContent;
        result.MaterializeXmlTrees = source.MaterializeXmlTrees;
        result.IndexPackageEntries = source.IndexPackageEntries;
        result.ValidateOnLoad = source.ValidateOnLoad;
        result.AllowRecovery = source.AllowRecovery;
        return result;
    }

    static OdfStreamSelection CopySpreadsheetSelection(
        OdfStreamSelection source)
    {
        OdfStreamSelection result = new OdfStreamSelection();
        result.PartPaths = CopySpreadsheetPartPaths(source.PartPaths);
        result.ElementNames = CopySpreadsheetElementNames(
            source.ElementNames);
        result.IncludePackageEntryEvents = source.IncludePackageEntryEvents;
        result.IncludeElementEvents = source.IncludeElementEvents;
        result.IncludeTextEvents = source.IncludeTextEvents;
        result.IncludeCommentEvents = source.IncludeCommentEvents;
        result.IncludeProcessingInstructionEvents =
            source.IncludeProcessingInstructionEvents;
        result.IncludeBinaryContent = source.IncludeBinaryContent;
        result.SkipUnselectedSubtrees = source.SkipUnselectedSubtrees;
        return result;
    }

    static string[] CopySpreadsheetPartPaths(string[] source)
    {
        if (source == null)
        {
            return [];
        }
        string[] result = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }

    static OdfQualifiedName[] CopySpreadsheetElementNames(
        OdfQualifiedName[] source)
    {
        if (source == null)
        {
            return [];
        }
        List<OdfQualifiedName> result = new List<OdfQualifiedName>();
        int index = 0;
        while (index < source.Length)
        {
            OdfQualifiedName? sourceName = source[index];
            if (sourceName != null)
            {
                OdfQualifiedName name = new OdfQualifiedName();
                name.NamespaceName = sourceName.NamespaceName;
                name.LocalName = sourceName.LocalName;
                name.Prefix = sourceName.Prefix;
                result.Add(name);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ValidateSpreadsheetXmlInputs(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        if (input == null || !input.CanRead || partPath == null
            || options == null || options.StreamSelection == null
            || options.SecurityLimits == null
            || options.Cancellation == null)
        {
            throw new OdfDocumentException(
                "A readable XML stream, part path, and load options are required for spreadsheet projection.");
        }
    }

    static void ValidateSpreadsheetPackageInputs(
        Stream input,
        OdfLoadOptions options)
    {
        if (input == null || !input.CanRead || options == null
            || options.StreamSelection == null
            || options.SecurityLimits == null
            || options.Cancellation == null)
        {
            throw new OdfDocumentException(
                "A readable package stream and load options are required for spreadsheet projection.");
        }
    }
}
