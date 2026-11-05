using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents streaming sheet row cursor data used by the public workbook API.
/// </summary>
public class XlsxStreamingSheetRowCursor
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the worksheet part value used by this model.
    /// </summary>
    public XlsxWorksheetPart WorksheetPart;
    /// <summary>
    /// Contains shared strings.
    /// </summary>
    public XlsxSharedStringTablePart SharedStrings;
    /// <summary>
    /// Contains styles.
    /// </summary>
    public XlsxStylesPart Styles;
    /// <summary>
    /// Stores the use1904 date system value used by this model.
    /// </summary>
    public bool Use1904DateSystem;
    /// <summary>
    /// Stores the current value used by this model.
    /// </summary>
    public XlsxStreamingSheetRow? Current;
    /// <summary>
    /// Stores the rows read value used by this model.
    /// </summary>
    public int RowsRead;
    /// <summary>
    /// Stores the cells read value used by this model.
    /// </summary>
    public int CellsRead;
    /// <summary>
    /// Contains end of rows.
    /// </summary>
    public bool EndOfRows;
    internal XmlReader Reader;
    internal bool Initialized;
    internal bool InSheetData;
    internal int SheetDataDepth;
    /// <summary>
    /// Initializes a new streaming sheet row cursor with its default state.
    /// </summary>
    /// <param name = "reader">The reader value used by the operation.</param>
    /// <param name = "sharedStrings">The shared strings value used by the operation.</param>
    /// <param name = "styles">The styles value used by the operation.</param>
    /// <param name = "use1904DateSystem">The use1904 date system value used by the operation.</param>
    public XlsxStreamingSheetRowCursor(XmlReader reader, XlsxSharedStringTablePart sharedStrings, XlsxStylesPart styles, bool use1904DateSystem)
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        WorksheetPart = new XlsxWorksheetPart();
        SharedStrings = sharedStrings;
        if (SharedStrings == null)
        {
            SharedStrings = new XlsxSharedStringTablePart();
        }

        Styles = styles;
        if (Styles == null)
        {
            Styles = new XlsxStylesPart();
        }

        Use1904DateSystem = use1904DateSystem;
        Current = null;
        RowsRead = 0;
        CellsRead = 0;
        EndOfRows = false;
        Reader = reader;
        Initialized = false;
        InSheetData = false;
        SheetDataDepth = 0;
    }

    internal void Initialize()
    {
        if (Initialized)
        {
            return;
        }

        Initialized = true;
        bool sawRoot = false;
        while (!Reader.EOF)
        {
            if (Reader.NodeType != XmlNodeType.Element)
            {
                Reader.Read();
                continue;
            }

            if (!sawRoot)
            {
                sawRoot = true;
                XlsxWorksheetPartReader.ReadRootElement(WorksheetPart, Reader);
                CopyWorksheetDiagnostics();
                Reader.Read();
                continue;
            }

            if (Reader.Depth == 1 && XlsxWorksheetPartReader.IsWorksheetNamespace(WorksheetPart, Reader.NamespaceURI) && Reader.LocalName == "sheetData")
            {
                InSheetData = true;
                SheetDataDepth = Reader.Depth;
                if (Reader.IsEmptyElement)
                {
                    EndOfRows = true;
                    Reader.Read();
                    return;
                }

                Reader.Read();
                return;
            }

            if (Reader.NodeType == XmlNodeType.Element)
            {
                Reader.ReadOuterXml();
                continue;
            }

            Reader.Read();
        }

        if (!sawRoot)
        {
            AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.missingRoot", "Worksheet XML must contain a worksheet root element."));
        }

        EndOfRows = true;
    }

    /// <summary>
    /// Advances the streaming cursor to the next worksheet row and updates the current row.
    /// </summary>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool Read()
    {
        Initialize();
        if (EndOfRows)
        {
            Current = null;
            return false;
        }

        while (!Reader.EOF)
        {
            if (InSheetData && Reader.NodeType == XmlNodeType.EndElement && Reader.Depth == SheetDataDepth)
            {
                Reader.Read();
                EndOfRows = true;
                Current = null;
                return false;
            }

            if (InSheetData && Reader.NodeType == XmlNodeType.Element && Reader.Depth == SheetDataDepth + 1 && Reader.LocalName == "row")
            {
                Current = ReadCurrentRow();
                RowsRead = RowsRead + 1;
                CellsRead = CellsRead + Current.Cells.Count;
                return true;
            }

            if (Reader.NodeType == XmlNodeType.Element)
            {
                Reader.ReadOuterXml();
                continue;
            }

            Reader.Read();
        }

        EndOfRows = true;
        Current = null;
        return false;
    }

    /// <summary>
    /// Completes or closes the streaming operation and releases the reader or writer owned by the session.
    /// </summary>
    internal void Close()
    {
        if (Reader != null)
        {
            Reader.Close();
        }
    }

    internal XlsxStreamingSheetRow ReadCurrentRow()
    {
        int rowIndex = WorksheetPart.Rows.Count;
        XlsxWorksheetPartReader.ReadRow(WorksheetPart, Reader);
        CopyWorksheetDiagnostics();
        XlsxWorksheetRow row = WorksheetPart.Rows[rowIndex];
        WorksheetPart.Rows.RemoveAt(rowIndex);
        return BuildStreamingRow(row);
    }

    internal XlsxStreamingSheetRow BuildStreamingRow(XlsxWorksheetRow row)
    {
        XlsxStreamingSheetRow streamingRow = new XlsxStreamingSheetRow();
        streamingRow.Row = row;
        streamingRow.RowIndex = row.RowIndex;
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            streamingRow.Cells.Add(BuildStreamingCell(cell));
        }

        return streamingRow;
    }

    internal XlsxStreamingSheetCell BuildStreamingCell(XlsxWorksheetCell cell)
    {
        XlsxStreamingSheetCell streamingCell = new XlsxStreamingSheetCell();
        streamingCell.Cell = cell;
        streamingCell.Reference = XlsxCellValueConverter.NormalizeText(cell.Reference);
        XlsxCellValueResult valueResult = XlsxCellValueConverter.FromWorksheetCell(cell);
        CopyValueDiagnostics(valueResult.Validation);
        streamingCell.Value = ResolveSharedStringValue(valueResult.Value);
        streamingCell.FormatCode = ResolveCellFormatCode(cell, Styles);
        XlsxFormattedTextRenderResult formatted = XlsxFormattedTextRenderer.RenderCellValue(streamingCell.Value, streamingCell.FormatCode, Use1904DateSystem);
        CopyFormattedDiagnostics(formatted.Validation);
        streamingCell.FormattedValue = formatted.Text;
        return streamingCell;
    }

    internal XlsxCellValue ResolveSharedStringValue(XlsxCellValue value)
    {
        if (value == null)
        {
            return XlsxCellValue.CreateBlank();
        }

        if (value.Kind != XlsxCellValueKind.SharedString)
        {
            return value;
        }

        int index = (int)value.SharedStringIndex;
        XlsxSharedStringItem? item = SharedStrings.GetItem(index);
        if (item == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingSheet.sharedStringMissing", "Shared string cell references an index that is not present in the shared string table.");
            diagnostic.SetLocation(WorksheetPart.PartPath, "c", "v");
            AddDiagnostic(diagnostic);
            return XlsxCellValue.CreateText("");
        }

        if (item.IsRichText)
        {
            return XlsxCellValue.CreateRichText(item.Text, "");
        }

        return XlsxCellValue.CreateText(item.Text);
    }

    internal static string ResolveCellFormatCode(XlsxWorksheetCell cell, XlsxStylesPart styles)
    {
        if (cell == null || styles == null || !cell.HasStyle)
        {
            return "General";
        }

        int styleIndex = (int)cell.Style;
        if (styleIndex < 0 || styleIndex >= styles.CellFormats.Count)
        {
            return "General";
        }

        XlsxCellFormatDefinition format = styles.CellFormats[styleIndex];
        if (format == null || !format.HasNumberFormatId)
        {
            return "General";
        }

        XlsxNumberFormatDefinition? custom = FindNumberFormat(styles, format.NumberFormatId);
        if (custom != null && custom.FormatCode != null && custom.FormatCode.Length > 0)
        {
            return custom.FormatCode;
        }

        string builtIn = XlsxNumberFormatParser.GetBuiltInFormatCode(format.NumberFormatId);
        if (builtIn != null && builtIn.Length > 0)
        {
            return builtIn;
        }

        return "General";
    }

    internal static XlsxNumberFormatDefinition? FindNumberFormat(XlsxStylesPart styles, uint numberFormatId)
    {
        if (styles == null)
        {
            return null;
        }

        foreach (XlsxNumberFormatDefinition format in styles.NumberFormats)
        {
            if (format != null && format.HasNumberFormatId && format.NumberFormatId == numberFormatId)
            {
                return format;
            }
        }

        return null;
    }

    internal void CopyWorksheetDiagnostics()
    {
        CopyDiagnostics(WorksheetPart.Validation);
        WorksheetPart.Validation.Diagnostics.Clear();
        WorksheetPart.IsValid = true;
    }

    internal void CopyValueDiagnostics(XlsxValidationResult validation)
    {
        CopyDiagnostics(validation);
    }

    internal void CopyFormattedDiagnostics(XlsxValidationResult validation)
    {
        CopyDiagnostics(validation);
    }

    internal void CopyDiagnostics(XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            AddDiagnostic(diagnostic);
        }
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
