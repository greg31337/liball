using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents streaming workbook write session data used by the public workbook API.
/// </summary>
public class XlsxStreamingWorkbookWriteSession : IXlsxStreamingWorkbookWriteSession
{
    internal XlsxStreamingWorkbookWriteResult Result;
    internal XlsxWriterOptions Options;
    internal List<XlsxStreamingWorkbookSheetInfo> Sheets;
    internal ZipArchive Archive;
    internal XmlWriter? CurrentSheetWriter;
    internal Stream? CurrentSheetStream;
    internal XlsxStreamingWorkbookSheetInfo? CurrentSheet;
    internal bool Closed;
    /// <summary>
    /// Initializes a new streaming workbook write session with its default state.
    /// </summary>
    /// <param name = "archive">The archive value used by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <param name = "result">The result value used by the operation.</param>
    public XlsxStreamingWorkbookWriteSession(ZipArchive archive, XlsxWriterOptions options, XlsxStreamingWorkbookWriteResult result)
    {
        Archive = archive;
        Options = options;
        Result = result;
        Sheets = new List<XlsxStreamingWorkbookSheetInfo>();
        CurrentSheetWriter = null;
        CurrentSheetStream = null;
        CurrentSheet = null;
        Closed = false;
    }

    /// <summary>
    /// Returns a snapshot of the worksheet metadata accumulated by the session.
    /// </summary>
    /// <returns>Independent worksheet metadata objects in workbook order.</returns>
    public List<XlsxStreamingWorkbookSheetInfo> GetSheetInfos()
    {
        List<XlsxStreamingWorkbookSheetInfo> infos = new List<XlsxStreamingWorkbookSheetInfo>();
        foreach (XlsxStreamingWorkbookSheetInfo sheet in Sheets)
        {
            XlsxStreamingWorkbookSheetInfo info = new XlsxStreamingWorkbookSheetInfo();
            info.Name = sheet.Name;
            info.SheetId = sheet.SheetId;
            info.RelationshipId = sheet.RelationshipId;
            info.PartPath = sheet.PartPath;
            info.RowCount = sheet.RowCount;
            info.CellCount = sheet.CellCount;
            infos.Add(info);
        }

        return infos;
    }

    /// <summary>
    /// Performs the begin sheet operation using the current model state.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookWriteResult BeginSheet(string name)
    {
        if (Closed)
        {
            Result.AddError("streamingWorkbook.write.closed", "Streaming workbook writer is already closed.");
            return Result;
        }

        if (CurrentSheetWriter != null)
        {
            Result.AddError("streamingWorkbook.write.sheetAlreadyOpen", "A streaming worksheet is already open.");
            return Result;
        }

        string sheetName = XlsxCellValueConverter.NormalizeText(name);
        if (sheetName.Length == 0)
        {
            Result.AddError("workbook.sheet.nameMissing", "Workbook sheet must have a name.");
            return Result;
        }

        if (!XlsxWorkbookPartWriter.IsValidSheetName(sheetName))
        {
            Result.AddError("workbook.sheet.nameInvalid", "Workbook sheet name contains invalid characters or exceeds the length limit.");
            return Result;
        }

        if (HasSheetName(sheetName))
        {
            Result.AddError("workbook.sheet.nameDuplicate", "Workbook sheet names must be unique.");
            return Result;
        }

        XlsxStreamingWorkbookSheetInfo sheet = new XlsxStreamingWorkbookSheetInfo();
        sheet.Name = sheetName;
        sheet.SheetId = (uint)(Sheets.Count + 1);
        sheet.RelationshipId = "rId" + sheet.SheetId.ToString(CultureInfo.InvariantCulture);
        sheet.PartPath = XlsxPackagePaths.WorksheetsDirectory + "sheet" + sheet.SheetId.ToString(CultureInfo.InvariantCulture) + ".xml";
        Sheets.Add(sheet);
        CurrentSheet = sheet;
        ZipArchiveEntry entry = Archive.CreateEntry(ToZipEntryName(sheet.PartPath), CompressionLevel.Optimal);
        CurrentSheetStream = entry.Open();
        CurrentSheetWriter = XmlWriter.Create(CurrentSheetStream, XlsxXmlWriterHelpers.CreateStableWriterSettings());
        CurrentSheetWriter.WriteStartDocument();
        CurrentSheetWriter.WriteStartElement("", "worksheet", XlsxNamespaces.Spreadsheet);
        CurrentSheetWriter.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.OfficeDocumentRelationships);
        CurrentSheetWriter.WriteStartElement("", "sheetData", XlsxNamespaces.Spreadsheet);
        return Result;
    }

    /// <summary>
    /// Validates and writes text row in the requested representation.
    /// </summary>
    /// <param name = "values">The values value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookWriteResult WriteTextRow(List<string> values)
    {
        List<XlsxCellValue> rowValues = new List<XlsxCellValue>();
        if (values == null)
        {
            Result.AddError("streamingWorkbook.write.rowValuesNull", "Streaming row values must not be null.");
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
    public XlsxStreamingWorkbookWriteResult WriteRow(List<XlsxCellValue> values)
    {
        if (Closed)
        {
            Result.AddError("streamingWorkbook.write.closed", "Streaming workbook writer is already closed.");
            return Result;
        }

        if (CurrentSheetWriter == null || CurrentSheet == null)
        {
            Result.AddError("streamingWorkbook.write.sheetMissing", "A streaming worksheet must be open before writing rows.");
            return Result;
        }

        if (values == null)
        {
            Result.AddError("streamingWorkbook.write.rowValuesNull", "Streaming row values must not be null.");
            return Result;
        }

        if (values.Count > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            Result.AddError("streamingWorkbook.write.maxColumns", "Streaming row exceeds the worksheet column limit.");
            return Result;
        }

        if (CurrentSheet.RowCount >= XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            Result.AddError("streamingWorkbook.write.maxRows", "Streaming worksheet exceeds the worksheet row limit.");
            return Result;
        }

        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = (uint)(CurrentSheet.RowCount + 1);
        int column = 1;
        foreach (XlsxCellValue value in values)
        {
            XlsxWorksheetCell cell = CreateCell(row.RowIndex, column, value);
            row.Cells.Add(cell);
            column = column + 1;
        }

        XlsxWorksheetPartWriter.WriteRow(CurrentSheetWriter, row);
        CurrentSheet.RowCount = CurrentSheet.RowCount + 1;
        CurrentSheet.CellCount = CurrentSheet.CellCount + row.Cells.Count;
        Result.RowCount = Result.RowCount + 1;
        Result.CellCount = Result.CellCount + row.Cells.Count;
        return Result;
    }

    /// <summary>
    /// Performs the end sheet operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookWriteResult EndSheet()
    {
        if (Closed)
        {
            Result.AddError("streamingWorkbook.write.closed", "Streaming workbook writer is already closed.");
            return Result;
        }

        if (CurrentSheetWriter == null || CurrentSheetStream == null)
        {
            Result.AddError("streamingWorkbook.write.sheetMissing", "A streaming worksheet must be open before it can be ended.");
            return Result;
        }

        CurrentSheetWriter.WriteEndElement();
        CurrentSheetWriter.WriteEndElement();
        CurrentSheetWriter.WriteEndDocument();
        CurrentSheetWriter.Close();
        CurrentSheetWriter = null;
        CurrentSheetStream.Close();
        CurrentSheetStream = null;
        CurrentSheet = null;
        return Result;
    }

    /// <summary>
    /// Completes or closes the streaming operation and releases the reader or writer owned by the session.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookWriteResult Close()
    {
        if (Closed)
        {
            return Result;
        }

        if (CurrentSheetWriter != null)
        {
            EndSheet();
        }

        if (Sheets.Count == 0)
        {
            Result.AddError("workbook.sheetsMissing", "Workbook must contain at least one sheet.");
        }

        if (Result.IsValid)
        {
            WriteMetadataEntries();
        }

        Closed = true;
        Archive.Dispose();
        Result.SheetCount = Sheets.Count;
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

        XlsxCellValue cellValue = value;
        if (cellValue == null)
        {
            cellValue = XlsxCellValue.CreateBlank();
        }

        XlsxCellValueResult valueResult = XlsxCellValueConverter.ApplyToWorksheetCell(cellValue, cell);
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            Result.AddDiagnostic(diagnostic);
        }

        return cell;
    }

    internal void WriteMetadataEntries()
    {
        XlsxWorkbook workbook = BuildWorkbook();
        XlsxContentTypesPart contentTypes = workbook.BuildContentTypesPartForVariant(workbook.ResolveSavePackageVariant(Options));
        workbook.AddContentTypesForWorkbookRelationships(contentTypes, workbook.WorkbookRelationships);
        AddXmlEntry(XlsxPackagePaths.Workbook, new XlsxWorkbookPartWriter().WriteBytes(workbook.WorkbookPart).Bytes);
        AddXmlEntry(XlsxPackagePaths.PackageRelationships, new XlsxPartRelationshipsWriter().WriteBytes(workbook.PackageRelationships).Bytes);
        AddXmlEntry(XlsxPackagePaths.WorkbookRelationships, new XlsxPartRelationshipsWriter().WriteBytes(workbook.WorkbookRelationships).Bytes);
        AddXmlEntry(XlsxPackagePaths.ContentTypes, new XlsxContentTypesWriter().WriteBytes(contentTypes).Bytes);
    }

    internal XlsxWorkbook BuildWorkbook()
    {
        XlsxWorkbook workbook = new XlsxWorkbook();
        workbook.WorkbookPart = new XlsxWorkbookPart();
        workbook.PackageRelationships = XlsxWorkbook.CreatePackageRelationships();
        workbook.WorkbookRelationships = new XlsxRelationshipsPart();
        workbook.WorkbookRelationships.SourcePartPath = XlsxPackagePaths.Workbook;
        workbook.WorkbookRelationships.PartPath = XlsxPackagePaths.WorkbookRelationships;
        foreach (XlsxStreamingWorkbookSheetInfo sheet in Sheets)
        {
            workbook.WorkbookPart.Sheets.Add(XlsxWorkbook.CreateSheet(sheet.Name, sheet.SheetId, sheet.RelationshipId));
            workbook.WorkbookRelationships.Relationships.Add(XlsxWorkbook.CreateWorksheetRelationship(sheet.RelationshipId, "worksheets/sheet" + sheet.SheetId.ToString(CultureInfo.InvariantCulture) + ".xml"));
        }

        return workbook;
    }

    internal void AddXmlEntry(string path, byte[] bytes)
    {
        ZipArchiveEntry entry = Archive.CreateEntry(ToZipEntryName(path), CompressionLevel.Optimal);
        using (Stream entryStream = entry.Open())
        {
            entryStream.Write(bytes, 0, bytes.Length);
        }
    }

    internal bool HasSheetName(string name)
    {
        foreach (XlsxStreamingWorkbookSheetInfo sheet in Sheets)
        {
            if (string.Equals(sheet.Name, name, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static string ToZipEntryName(string path)
    {
        if (path != null && path.StartsWith("/", System.StringComparison.Ordinal))
        {
            return path.Substring(1);
        }

        return XlsxCellValueConverter.NormalizeText(path);
    }
}
