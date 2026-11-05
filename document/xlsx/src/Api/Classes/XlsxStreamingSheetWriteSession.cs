using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents streaming sheet write session data used by the public workbook API.
/// </summary>
public class XlsxStreamingSheetWriteSession : IXlsxStreamingSheetWriteSession
{
    internal XlsxStreamingSheetWriteResult Result;
    internal XlsxStreamingSheetWriterOptions Options;
    internal XmlWriter Writer;
    internal bool Closed;
    /// <summary>
    /// Initializes a new streaming sheet write session with its default state.
    /// </summary>
    /// <param name = "writer">The writer value used by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <param name = "result">The result value used by the operation.</param>
    public XlsxStreamingSheetWriteSession(XmlWriter writer, XlsxStreamingSheetWriterOptions options, XlsxStreamingSheetWriteResult result)
    {
        Writer = writer;
        Options = options;
        Result = result;
        Closed = false;
    }

    internal void WriteStartWorksheet()
    {
        Writer.WriteStartDocument();
        Writer.WriteStartElement("", "worksheet", XlsxNamespaces.Spreadsheet);
        Writer.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.OfficeDocumentRelationships);
        Writer.WriteStartElement("", "sheetData", XlsxNamespaces.Spreadsheet);
    }

    /// <summary>
    /// Validates and writes text row in the requested representation.
    /// </summary>
    /// <param name = "values">The values value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingSheetWriteResult WriteTextRow(List<string> values)
    {
        List<XlsxCellValue> rowValues = new List<XlsxCellValue>();
        if (values == null)
        {
            Result.AddError("streamingSheet.write.rowValuesNull", "Streaming row values must not be null.");
            return Result;
        }

        foreach (string value in values)
        {
            rowValues.Add(XlsxCellValue.CreateInlineString(value));
        }

        return WriteRow(rowValues);
    }

    /// <summary>
    /// Validates and writes row in the requested representation.
    /// </summary>
    /// <param name = "values">The values value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingSheetWriteResult WriteRow(List<XlsxCellValue> values)
    {
        if (Closed)
        {
            Result.AddError("streamingSheet.write.closed", "Streaming worksheet writer is already closed.");
            return Result;
        }

        if (values == null)
        {
            Result.AddError("streamingSheet.write.rowValuesNull", "Streaming row values must not be null.");
            return Result;
        }

        if (values.Count > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            Result.AddError("streamingSheet.write.maxColumns", "Streaming row exceeds the worksheet column limit.");
            return Result;
        }

        if (Result.RowCount >= XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            Result.AddError("streamingSheet.write.maxRows", "Streaming worksheet exceeds the worksheet row limit.");
            return Result;
        }

        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = (uint)(Result.RowCount + 1);
        int column = 1;
        foreach (XlsxCellValue value in values)
        {
            row.Cells.Add(CreateCell(row.RowIndex, column, value));
            column = column + 1;
        }

        XlsxWorksheetPartWriter.WriteRow(Writer, row);
        Result.RowCount = Result.RowCount + 1;
        Result.CellCount = Result.CellCount + row.Cells.Count;
        return Result;
    }

    /// <summary>
    /// Completes or closes the streaming operation and releases the reader or writer owned by the session.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingSheetWriteResult Close()
    {
        if (Closed)
        {
            return Result;
        }

        Writer.WriteEndElement();
        Writer.WriteEndElement();
        Writer.WriteEndDocument();
        Writer.Close();
        Closed = true;
        Result.IsValid = Result.Validation.IsValid;
        return Result;
    }

    internal XlsxWorksheetCell CreateCell(uint rowIndex, int columnIndex, XlsxCellValue value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Format((int)rowIndex, columnIndex, false, false);
        if (reference.IsValid)
        {
            cell.Reference = reference.Text;
        }
        else
        {
            foreach (XlsxDiagnostic diagnostic in reference.Validation.Diagnostics)
            {
                Result.AddDiagnostic(diagnostic);
            }
        }

        XlsxCellValue cellValue = NormalizeStringValue(value);
        XlsxCellValueResult valueResult = XlsxCellValueConverter.ApplyToWorksheetCell(cellValue, cell);
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            Result.AddDiagnostic(diagnostic);
        }

        return cell;
    }

    internal XlsxCellValue NormalizeStringValue(XlsxCellValue value)
    {
        if (value == null)
        {
            return XlsxCellValue.CreateBlank();
        }

        if (!IsStringLikeValue(value))
        {
            return value;
        }

        if (Options.UseSharedStrings)
        {
            int index = Result.SharedStrings.GetOrAddText(value.Text);
            return XlsxCellValue.CreateSharedString((uint)index);
        }

        return XlsxCellValue.CreateInlineString(value.Text);
    }

    internal static bool IsStringLikeValue(XlsxCellValue value)
    {
        if (value == null)
        {
            return false;
        }

        return value.Kind == XlsxCellValueKind.Text || value.Kind == XlsxCellValueKind.InlineString || value.Kind == XlsxCellValueKind.RichText;
    }
}
