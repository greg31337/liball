using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StreamingWorkbookWriterTests
{
    [Test]
    public void WriteStream_WritesLargeSheetRowsDirectlyToPackage()
    {
        MemoryStream stream = new MemoryStream();
        XlsxStreamingWorkbookWriteResult beginResult = new XlsxStreamingWorkbookWriter().BeginStream(stream);
        XlsxStreamingWorkbookWriteSession? session = beginResult.Session;

        session!.BeginSheet("Data");
        int row = 1;
        while (row <= 200)
        {
            List<XlsxCellValue> values = new List<XlsxCellValue>();
            values.Add(XlsxCellValue.CreateNumber(row));
            values.Add(XlsxCellValue.CreateInlineString("Row " + row.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            values.Add(XlsxCellValue.CreateBoolean(row == 200));
            session.WriteRow(values);
            row = row + 1;
        }

        XlsxStreamingWorkbookWriteResult closeResult = session.Close();
        byte[] bytes = stream.ToArray();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(bytes, new XlsxReaderOptions());
        XlsxPackageEntry? sheetEntry = package.GetEntry(XlsxPackagePaths.Worksheet1);
        XlsxStreamingSheetOpenResult sheetResult = new XlsxStreamingSheetReader().OpenBytes(sheetEntry!.Data);
        int rowsRead = 0;
        XlsxStreamingSheetRow? lastRow = null;
        while (sheetResult.Cursor!.Read())
        {
            rowsRead = rowsRead + 1;
            lastRow = sheetResult.Cursor.Current;
        }

        Assert.That(beginResult.IsValid, Is.True);
        Assert.That(closeResult.IsValid, Is.True);
        Assert.That(closeResult.SheetCount, Is.EqualTo(1));
        Assert.That(closeResult.RowCount, Is.EqualTo(200));
        Assert.That(closeResult.CellCount, Is.EqualTo(600));
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.WorkbookPart.Sheets.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.WorkbookPart.Sheets[0].Name, Is.EqualTo("Data"));
        Assert.That(package.GetEntry(XlsxPackagePaths.ContentTypes), Is.Not.Null);
        Assert.That(package.GetEntry(XlsxPackagePaths.WorkbookRelationships), Is.Not.Null);
        Assert.That(sheetEntry, Is.Not.Null);
        Assert.That(rowsRead, Is.EqualTo(200));
        Assert.That(lastRow!.GetCell("A200")!.FormattedValue, Is.EqualTo("200"));
        Assert.That(lastRow.GetCell("B200")!.FormattedValue, Is.EqualTo("Row 200"));
        Assert.That(lastRow.GetCell("C200")!.FormattedValue, Is.EqualTo("TRUE"));
        Assert.That(sheetResult.Cursor.WorksheetPart.Rows.Count, Is.EqualTo(0));
    }

    [Test]
    public void WriteStream_WritesMultipleSheetMetadataAndWorksheetEntries()
    {
        MemoryStream stream = new MemoryStream();
        XlsxStreamingWorkbookWriteResult beginResult = new XlsxStreamingWorkbookWriter().BeginStream(stream);
        XlsxStreamingWorkbookWriteSession? session = beginResult.Session;

        session!.BeginSheet("First");
        session.WriteTextRow(new List<string> { "A" });
        session.EndSheet();
        session.BeginSheet("Second");
        session.WriteTextRow(new List<string> { "B" });
        List<XlsxStreamingWorkbookSheetInfo> sheetInfos = session.GetSheetInfos();
        XlsxStreamingWorkbookWriteResult closeResult = session.Close();

        byte[] bytes = stream.ToArray();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(bytes, new XlsxReaderOptions());

        Assert.That(beginResult.IsValid, Is.True);
        Assert.That(closeResult.IsValid, Is.True);
        Assert.That(closeResult.SheetCount, Is.EqualTo(2));
        Assert.That(sheetInfos.Count, Is.EqualTo(2));
        Assert.That(sheetInfos[1].Name, Is.EqualTo("Second"));
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.WorkbookPart.Sheets.Count, Is.EqualTo(2));
        Assert.That(openResult.Workbook.WorkbookPart.Sheets[0].Name, Is.EqualTo("First"));
        Assert.That(openResult.Workbook.WorkbookPart.Sheets[1].Name, Is.EqualTo("Second"));
        Assert.That(openResult.Workbook.WorkbookRelationships.GetRelationship("rId1")!.Target, Is.EqualTo("worksheets/sheet1.xml"));
        Assert.That(openResult.Workbook.WorkbookRelationships.GetRelationship("rId2")!.Target, Is.EqualTo("worksheets/sheet2.xml"));
        Assert.That(package.GetEntry("/xl/worksheets/sheet1.xml"), Is.Not.Null);
        Assert.That(package.GetEntry("/xl/worksheets/sheet2.xml"), Is.Not.Null);
    }

    [Test]
    public void WriteStream_ReportsInvalidOperations()
    {
        XlsxStreamingWorkbookWriteResult? nullStream = new XlsxStreamingWorkbookWriter().BeginStream(null!);
        MemoryStream stream = new MemoryStream();
        XlsxStreamingWorkbookWriteResult beginResult = new XlsxStreamingWorkbookWriter().BeginStream(stream);
        XlsxStreamingWorkbookWriteSession? session = beginResult.Session;

        XlsxStreamingWorkbookWriteResult writeBeforeSheet = session!.WriteTextRow(new List<string> { "Bad" });
        session.BeginSheet("Data");
        XlsxStreamingWorkbookWriteResult duplicateOpen = session.BeginSheet("Other");
        session.EndSheet();
        XlsxStreamingWorkbookWriteResult duplicateName = session.BeginSheet("Data");
        XlsxStreamingWorkbookWriteResult closeResult = session.Close();

        Assert.That(nullStream.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullStream, "streamingWorkbook.write.streamNull"), Is.True);
        Assert.That(writeBeforeSheet.IsValid, Is.False);
        Assert.That(HasDiagnostic(writeBeforeSheet, "streamingWorkbook.write.sheetMissing"), Is.True);
        Assert.That(duplicateOpen.IsValid, Is.False);
        Assert.That(HasDiagnostic(duplicateOpen, "streamingWorkbook.write.sheetAlreadyOpen"), Is.True);
        Assert.That(duplicateName.IsValid, Is.False);
        Assert.That(HasDiagnostic(duplicateName, "workbook.sheet.nameDuplicate"), Is.True);
        Assert.That(closeResult.IsValid, Is.False);
    }

    [Test]
    public void Close_ReportsMissingSheets()
    {
        MemoryStream stream = new MemoryStream();
        XlsxStreamingWorkbookWriteResult beginResult = new XlsxStreamingWorkbookWriter().BeginStream(stream);

        XlsxStreamingWorkbookWriteResult closeResult = beginResult.Session!.Close();

        Assert.That(closeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(closeResult, "workbook.sheetsMissing"), Is.True);
    }

    static bool HasDiagnostic(XlsxStreamingWorkbookWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
