using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents table data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxTable
{
    /// <summary>
    /// Identifies the first row value accepted by the API.
    /// </summary>
    public const string FirstRow = "firstRow";
    /// <summary>
    /// Identifies the last row value accepted by the API.
    /// </summary>
    public const string LastRow = "lastRow";
    /// <summary>
    /// Identifies the first column value accepted by the API.
    /// </summary>
    public const string FirstColumn = "firstCol";
    /// <summary>
    /// Identifies the last column value accepted by the API.
    /// </summary>
    public const string LastColumn = "lastCol";
    /// <summary>
    /// Identifies the banded rows value accepted by the API.
    /// </summary>
    public const string BandedRows = "bandRow";
    /// <summary>
    /// Identifies the banded columns value accepted by the API.
    /// </summary>
    public const string BandedColumns = "bandCol";

    internal PptxShape Shape;
    internal XmlElement Element;
    internal XmlElement Grid;
    internal List<XmlElement> Columns;
    /// <summary>
    /// Contains the rows in package or presentation order.
    /// </summary>
    public List<PptxTableRow> Rows;

    internal PptxTable(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        Shape = shape;
        XmlElement? table = PptxXml.FindFirstDescendant(shape.Element, PptxNamespaces.Drawing, "tbl");
        if (table == null)
        {
            throw new System.ArgumentException("Shape is not a table.");
        }

        Element = table;
        Grid = GetOrCreateGrid();
        Columns = new List<XmlElement>();
        Rows = new List<PptxTableRow>();
        Load();
    }
    internal int RowCount()
    {
        return Rows.Count;
    }
    internal int ColumnCount()
    {
        return Columns.Count;
    }
    internal PptxTableRow GetRow(int index)
    {
        ValidateRowIndex(index);
        return Rows[index];
    }
    internal PptxTableCell GetCell(int rowIndex, int columnIndex)
    {
        ValidateRowIndex(rowIndex);
        return Rows[rowIndex].GetCell(columnIndex);
    }
    internal string GetText(int rowIndex, int columnIndex)
    {
        return GetCell(rowIndex, columnIndex).GetText();
    }
    internal void SetText(int rowIndex, int columnIndex, string text)
    {
        GetCell(rowIndex, columnIndex).SetText(text);
    }
    internal long GetColumnWidth(int columnIndex)
    {
        ValidateColumnIndex(columnIndex);
        return PptxTransform.GetLongAttribute(Columns[columnIndex], "w", 0L);
    }
    internal void SetColumnWidth(int columnIndex, long width)
    {
        ValidateColumnIndex(columnIndex);
        if (width < 0L)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        PptxXml.SetAttributeValue(Columns[columnIndex], "", "", "w", width.ToString());
        MarkDirty();
    }
    internal long GetRowHeight(int rowIndex)
    {
        return GetRow(rowIndex).GetHeight();
    }
    internal void SetRowHeight(int rowIndex, long height)
    {
        GetRow(rowIndex).SetHeight(height);
    }
    internal void InsertRow(int index, long height)
    {
        ValidateRowInsertIndex(index);
        ValidateNonNegativeSize(height, "height");
        ValidateNoMergedCells();
        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        XmlElement row = Shape.Tree.CreateTableRowElement(document, ColumnCount(), height);
        if (index < Rows.Count)
        {
            Element.InsertBefore(row, Rows[index].Element);
        }
        else
        {
            Element.AppendChild(row);
        }

        MarkDirty();
    }
    internal void RemoveRow(int index)
    {
        ValidateRowIndex(index);
        if (Rows.Count <= 1)
        {
            throw new System.ArgumentException("A table must contain at least one row.");
        }

        ValidateNoMergedCells();
        Element.RemoveChild(Rows[index].Element);
        MarkDirty();
    }
    internal void InsertColumn(int index, long width)
    {
        ValidateColumnInsertIndex(index);
        ValidateNonNegativeSize(width, "width");
        ValidateNoMergedCells();
        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        XmlElement column = document.CreateElement("a", "gridCol", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(column, "", "", "w", width.ToString());
        if (index < Columns.Count)
        {
            Grid.InsertBefore(column, Columns[index]);
        }
        else
        {
            Grid.AppendChild(column);
        }

        for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
        {
            PptxTableRow row = Rows[rowIndex];
            XmlElement cell = Shape.Tree.CreateTableCellElement(document);
            if (index < row.Cells.Count)
            {
                row.Element.InsertBefore(cell, row.Cells[index].Element);
            }
            else
            {
                row.Element.AppendChild(cell);
            }
        }

        MarkDirty();
    }
    internal void RemoveColumn(int index)
    {
        ValidateColumnIndex(index);
        if (Columns.Count <= 1)
        {
            throw new System.ArgumentException("A table must contain at least one column.");
        }

        ValidateNoMergedCells();
        Grid.RemoveChild(Columns[index]);
        for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
        {
            PptxTableRow row = Rows[rowIndex];
            row.Element.RemoveChild(row.Cells[index].Element);
        }

        MarkDirty();
    }
    internal void MergeCells(int rowIndex, int columnIndex, int rowCount, int columnCount)
    {
        ValidateRectangle(rowIndex, columnIndex, rowCount, columnCount);
        if (rowCount == 1 && columnCount == 1)
        {
            throw new System.ArgumentException("A merged region must contain at least two cells.");
        }

        ValidateUnmergedRectangle(rowIndex, columnIndex, rowCount, columnCount);
        PptxTableCell origin = GetCell(rowIndex, columnIndex);
        SetSpanAttribute(origin.Element, "rowSpan", rowCount);
        SetSpanAttribute(origin.Element, "gridSpan", columnCount);
        origin.Element.RemoveAttribute("hMerge");
        origin.Element.RemoveAttribute("vMerge");

        int rowEnd = rowIndex + rowCount;
        int columnEnd = columnIndex + columnCount;
        for (int currentRow = rowIndex; currentRow < rowEnd; currentRow++)
        {
            for (int currentColumn = columnIndex; currentColumn < columnEnd; currentColumn++)
            {
                if (currentRow == rowIndex && currentColumn == columnIndex)
                {
                    continue;
                }

                PptxTableCell cell = GetCell(currentRow, currentColumn);
                cell.Element.RemoveAttribute("rowSpan");
                cell.Element.RemoveAttribute("gridSpan");
                SetMergeAttribute(cell.Element, "hMerge", currentColumn > columnIndex);
                SetMergeAttribute(cell.Element, "vMerge", currentRow > rowIndex);
            }
        }

        MarkDirty();
    }
    internal void SplitCell(int rowIndex, int columnIndex)
    {
        ValidateRowIndex(rowIndex);
        ValidateColumnIndex(columnIndex);
        PptxTableCell origin = FindMergeOrigin(rowIndex, columnIndex);
        if (!origin.IsMergeOrigin())
        {
            throw new System.ArgumentException("Cell is not part of a merged region.");
        }

        int rowCount = origin.GetRowSpan();
        int columnCount = origin.GetColumnSpan();
        if (rowCount < 1)
        {
            rowCount = 1;
        }

        if (columnCount < 1)
        {
            columnCount = 1;
        }

        ValidateRectangle(origin.RowIndex, origin.ColumnIndex, rowCount, columnCount);
        int rowEnd = origin.RowIndex + rowCount;
        int columnEnd = origin.ColumnIndex + columnCount;
        for (int currentRow = origin.RowIndex; currentRow < rowEnd; currentRow++)
        {
            for (int currentColumn = origin.ColumnIndex; currentColumn < columnEnd; currentColumn++)
            {
                PptxTableCell cell = GetCell(currentRow, currentColumn);
                cell.Element.RemoveAttribute("rowSpan");
                cell.Element.RemoveAttribute("gridSpan");
                cell.Element.RemoveAttribute("hMerge");
                cell.Element.RemoveAttribute("vMerge");
            }
        }

        MarkDirty();
    }
    internal PptxTableCell FindMergeOrigin(int rowIndex, int columnIndex)
    {
        ValidateRowIndex(rowIndex);
        ValidateColumnIndex(columnIndex);
        for (int currentRow = 0; currentRow < Rows.Count; currentRow++)
        {
            for (int currentColumn = 0; currentColumn < Rows[currentRow].CellCount(); currentColumn++)
            {
                PptxTableCell cell = GetCell(currentRow, currentColumn);
                if (cell.IsMergeContinuation())
                {
                    continue;
                }

                int rowSpan = cell.GetRowSpan();
                int columnSpan = cell.GetColumnSpan();
                if (rowSpan < 1)
                {
                    rowSpan = 1;
                }

                if (columnSpan < 1)
                {
                    columnSpan = 1;
                }

                if (rowIndex >= currentRow && rowIndex - currentRow < rowSpan && columnIndex >= currentColumn && columnIndex - currentColumn < columnSpan)
                {
                    return cell;
                }
            }
        }

        return GetCell(rowIndex, columnIndex);
    }
    internal void SetData(List<List<string>> data)
    {
        SetDataAt(0, 0, data);
    }
    internal void SetDataAt(int rowIndex, int columnIndex, List<List<string>> data)
    {
        ValidateData(data);
        if (data.Count == 0)
        {
            return;
        }

        int columnCount = data[0].Count;
        if (columnCount == 0)
        {
            return;
        }

        ValidateRectangle(rowIndex, columnIndex, data.Count, columnCount);
        for (int currentRow = 0; currentRow < data.Count; currentRow++)
        {
            for (int currentColumn = 0; currentColumn < columnCount; currentColumn++)
            {
                SetText(rowIndex + currentRow, columnIndex + currentColumn, data[currentRow][currentColumn]);
            }
        }
    }
    internal List<List<string>> GetData()
    {
        return ExtractData();
    }
    internal List<List<string>> ExtractData()
    {
        List<List<string>> data = new List<List<string>>();
        for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
        {
            List<string> row = new List<string>();
            for (int columnIndex = 0; columnIndex < Rows[rowIndex].CellCount(); columnIndex++)
            {
                PptxTableCell cell = GetCell(rowIndex, columnIndex);
                if (cell.IsMergeContinuation())
                {
                    row.Add("");
                }
                else
                {
                    row.Add(cell.GetText());
                }
            }

            data.Add(row);
        }

        return data;
    }
    internal void ImportData(System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<string>> data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        List<List<string>> rows = new List<List<string>>();
        foreach (System.Collections.Generic.IEnumerable<string> sourceRow in data)
        {
            if (sourceRow == null)
            {
                throw new System.ArgumentNullException("data");
            }

            List<string> row = new List<string>();
            foreach (string value in sourceRow)
            {
                row.Add(PptxDiagnostic.CleanText(value));
            }

            rows.Add(row);
        }

        ValidateData(rows);
        if (rows.Count == 0 || rows[0].Count == 0)
        {
            throw new System.ArgumentException("Imported table data must contain at least one row and one column.");
        }

        ValidateNoMergedCells();
        ResizeForImport(rows.Count, rows[0].Count);
        SetData(rows);
    }
    internal System.Collections.Generic.IEnumerable<System.Collections.Generic.IReadOnlyList<string>> ExportData()
    {
        List<List<string>> data = ExtractData();
        List<System.Collections.Generic.IReadOnlyList<string>> rows = new List<System.Collections.Generic.IReadOnlyList<string>>();
        for (int index = 0; index < data.Count; index++)
        {
            rows.Add(data[index]);
        }

        return rows;
    }
    internal void ResizeForImport(int rowCount, int columnCount)
    {
        long defaultWidth = GetColumnWidth(0);
        long defaultHeight = GetRowHeight(0);
        while (ColumnCount() < columnCount)
        {
            InsertColumn(ColumnCount(), defaultWidth);
        }

        while (ColumnCount() > columnCount)
        {
            RemoveColumn(ColumnCount() - 1);
        }

        while (RowCount() < rowCount)
        {
            InsertRow(RowCount(), defaultHeight);
        }

        while (RowCount() > rowCount)
        {
            RemoveRow(RowCount() - 1);
        }
    }
    internal string GetStyleId()
    {
        XmlElement tableProperties = GetOrCreateTableProperties();
        XmlElement? styleId = PptxXml.FindFirstChild(tableProperties, PptxNamespaces.Drawing, "tableStyleId");
        if (styleId == null)
        {
            return "";
        }

        return styleId.InnerText;
    }
    internal void SetStyleId(string styleId)
    {
        XmlElement tableProperties = GetOrCreateTableProperties();
        XmlElement? existing = PptxXml.FindFirstChild(tableProperties, PptxNamespaces.Drawing, "tableStyleId");
        string clean = PptxDiagnostic.CleanText(styleId);
        if (clean.Length == 0)
        {
            if (existing != null)
            {
                tableProperties.RemoveChild(existing);
                MarkDirty();
            }

            return;
        }

        if (existing == null)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(tableProperties);
            existing = document.CreateElement("a", "tableStyleId", PptxNamespaces.Drawing);
            tableProperties.AppendChild(existing);
        }

        existing.InnerText = clean;
        MarkDirty();
    }
    internal bool GetStyleFlag(string flagName)
    {
        XmlElement tableProperties = GetOrCreateTableProperties();
        return PptxXml.GetBoolAttribute(tableProperties, "", NormalizeStyleFlag(flagName), false);
    }
    internal void SetStyleFlag(string flagName, bool enabled)
    {
        XmlElement tableProperties = GetOrCreateTableProperties();
        PptxXml.SetAttributeValue(tableProperties, "", "", NormalizeStyleFlag(flagName), PptxTransform.BoolToXml(enabled));
        MarkDirty();
    }
    internal bool GetFirstRow()
    {
        return GetStyleFlag(FirstRow);
    }
    internal void SetFirstRow(bool enabled)
    {
        SetStyleFlag(FirstRow, enabled);
    }
    internal bool GetLastRow()
    {
        return GetStyleFlag(LastRow);
    }
    internal void SetLastRow(bool enabled)
    {
        SetStyleFlag(LastRow, enabled);
    }
    internal bool GetFirstColumn()
    {
        return GetStyleFlag(FirstColumn);
    }
    internal void SetFirstColumn(bool enabled)
    {
        SetStyleFlag(FirstColumn, enabled);
    }
    internal bool GetLastColumn()
    {
        return GetStyleFlag(LastColumn);
    }
    internal void SetLastColumn(bool enabled)
    {
        SetStyleFlag(LastColumn, enabled);
    }
    internal bool GetBandedRows()
    {
        return GetStyleFlag(BandedRows);
    }
    internal void SetBandedRows(bool enabled)
    {
        SetStyleFlag(BandedRows, enabled);
    }
    internal bool GetBandedColumns()
    {
        return GetStyleFlag(BandedColumns);
    }
    internal void SetBandedColumns(bool enabled)
    {
        SetStyleFlag(BandedColumns, enabled);
    }

    internal void Load()
    {
        Grid = GetOrCreateGrid();
        Columns.Clear();
        Rows.Clear();
        CollectDirectChildren(Grid, PptxNamespaces.Drawing, "gridCol", Columns);
        List<XmlElement> rowElements = new List<XmlElement>();
        CollectDirectChildren(Element, PptxNamespaces.Drawing, "tr", rowElements);
        for (int index = 0; index < rowElements.Count; index++)
        {
            Rows.Add(new PptxTableRow(this, rowElements[index], index));
        }
    }

    internal XmlElement GetOrCreateGrid()
    {
        XmlElement? grid = PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "tblGrid");
        if (grid != null)
        {
            return grid;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        XmlElement created = document.CreateElement("a", "tblGrid", PptxNamespaces.Drawing);
        XmlElement? firstRow = PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "tr");
        if (firstRow == null)
        {
            Element.AppendChild(created);
        }
        else
        {
            Element.InsertBefore(created, firstRow);
        }

        return created;
    }

    internal XmlElement GetOrCreateTableProperties()
    {
        XmlElement? tableProperties = PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "tblPr");
        if (tableProperties != null)
        {
            return tableProperties;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        XmlElement created = document.CreateElement("a", "tblPr", PptxNamespaces.Drawing);
        Element.InsertBefore(created, Element.FirstChild);
        return created;
    }

    internal void ValidateRowIndex(int index)
    {
        if (index < 0 || index >= Rows.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    internal void ValidateColumnIndex(int index)
    {
        if (index < 0 || index >= Columns.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    internal void ValidateRowInsertIndex(int index)
    {
        if (index < 0 || index > Rows.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    internal void ValidateColumnInsertIndex(int index)
    {
        if (index < 0 || index > Columns.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    internal static void ValidateNonNegativeSize(long value, string name)
    {
        if (value < 0L)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    internal void ValidateNoMergedCells()
    {
        for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < Rows[rowIndex].CellCount(); columnIndex++)
            {
                if (GetCell(rowIndex, columnIndex).IsMerged())
                {
                    throw new System.ArgumentException("Split merged cells before changing table rows or columns.");
                }
            }
        }
    }

    internal void ValidateUnmergedRectangle(int rowIndex, int columnIndex, int rowCount, int columnCount)
    {
        int rowEnd = rowIndex + rowCount;
        int columnEnd = columnIndex + columnCount;
        for (int currentRow = rowIndex; currentRow < rowEnd; currentRow++)
        {
            for (int currentColumn = columnIndex; currentColumn < columnEnd; currentColumn++)
            {
                if (GetCell(currentRow, currentColumn).IsMerged())
                {
                    throw new System.ArgumentException("Merge region overlaps an existing merged cell.");
                }
            }
        }
    }

    internal void ValidateRectangle(int rowIndex, int columnIndex, int rowCount, int columnCount)
    {
        if (rowCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rowCount");
        }

        if (columnCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columnCount");
        }

        ValidateRowIndex(rowIndex);
        ValidateColumnIndex(columnIndex);
        if (rowCount > Rows.Count - rowIndex)
        {
            throw new System.ArgumentOutOfRangeException("rowCount");
        }

        if (columnCount > ColumnCount() - columnIndex)
        {
            throw new System.ArgumentOutOfRangeException("columnCount");
        }

        int columnEnd = columnIndex + columnCount;
        int rowEnd = rowIndex + rowCount;
        for (int currentRow = rowIndex; currentRow < rowEnd; currentRow++)
        {
            if (Rows[currentRow].CellCount() < columnEnd)
            {
                throw new System.ArgumentOutOfRangeException("columnCount");
            }
        }
    }

    internal void ValidateData(List<List<string>> data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (data.Count == 0)
        {
            return;
        }

        if (data[0] == null)
        {
            throw new System.ArgumentNullException("data");
        }

        int columnCount = data[0].Count;
        for (int rowIndex = 1; rowIndex < data.Count; rowIndex++)
        {
            if (data[rowIndex] == null)
            {
                throw new System.ArgumentNullException("data");
            }

            if (data[rowIndex].Count != columnCount)
            {
                throw new System.ArgumentException("Table data must be rectangular.");
            }
        }
    }

    internal void MarkDirty()
    {
        Shape.Tree.MarkDirty();
        Load();
    }

    internal static void CollectDirectChildren(XmlElement parent, string namespaceUri, string localName, List<XmlElement> matches)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                matches.Add(element);
            }
        }
    }

    internal static string NormalizeStyleFlag(string flagName)
    {
        string clean = PptxDiagnostic.CleanText(flagName).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean == "firstrow")
        {
            return FirstRow;
        }

        if (clean == "lastrow")
        {
            return LastRow;
        }

        if (clean == "firstcolumn" || clean == "firstcol")
        {
            return FirstColumn;
        }

        if (clean == "lastcolumn" || clean == "lastcol")
        {
            return LastColumn;
        }

        if (clean == "bandedrows" || clean == "bandrow")
        {
            return BandedRows;
        }

        if (clean == "bandedcolumns" || clean == "bandcol")
        {
            return BandedColumns;
        }

        throw new System.ArgumentException("Unknown table style flag.");
    }

    internal static void SetSpanAttribute(XmlElement cell, string attributeName, int span)
    {
        if (span > 1)
        {
            PptxXml.SetAttributeValue(cell, "", "", attributeName, span.ToString());
            return;
        }

        cell.RemoveAttribute(attributeName);
    }

    internal static void SetMergeAttribute(XmlElement cell, string attributeName, bool enabled)
    {
        if (enabled)
        {
            PptxXml.SetAttributeValue(cell, "", "", attributeName, "1");
            return;
        }

        cell.RemoveAttribute(attributeName);
    }
}
