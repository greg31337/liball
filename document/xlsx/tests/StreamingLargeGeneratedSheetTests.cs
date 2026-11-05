using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StreamingLargeGeneratedSheetTests
{
    [Test]
    public void StreamingSheetWriter_WritesAndReadsLargeSharedStringSheetWithoutAccumulatingRows()
    {
        const int rowCount = 5000;
        MemoryStream stream = new MemoryStream();
        XlsxStreamingSheetWriterOptions options = new XlsxStreamingSheetWriterOptions();
        options.UseSharedStrings = true;
        XlsxStreamingSheetWriteResult begin = new XlsxStreamingSheetWriter().BeginStreamWithOptions(stream, options);

        int row = 1;
        while (row <= rowCount)
        {
            begin.Session!.WriteRow(CreateLargeRow(row));
            row = row + 1;
        }

        XlsxStreamingSheetWriteResult close = begin.Session!.Close();
        XlsxStreamingSheetOpenResult read = new XlsxStreamingSheetReader().OpenBytesWithWorkbookContext(stream.ToArray(), close.SharedStrings, new XlsxStylesPart(), false);
        int rowsRead = 0;
        int cellsRead = 0;
        XlsxStreamingSheetRow? first = null;
        XlsxStreamingSheetRow? middle = null;
        XlsxStreamingSheetRow? last = null;
        while (read.Cursor!.Read())
        {
            rowsRead = rowsRead + 1;
            cellsRead = cellsRead + read.Cursor.Current!.Cells.Count;
            if (rowsRead == 1)
            {
                first = read.Cursor.Current;
            }

            if (rowsRead == 2500)
            {
                middle = read.Cursor.Current;
            }

            last = read.Cursor.Current;
            Assert.That(read.Cursor.WorksheetPart.Rows.Count, Is.EqualTo(0));
        }

        Assert.That(begin.IsValid, Is.True);
        Assert.That(close.IsValid, Is.True);
        Assert.That(close.RowCount, Is.EqualTo(rowCount));
        Assert.That(close.CellCount, Is.EqualTo(rowCount * 5));
        Assert.That(close.SharedStrings.Items.Count, Is.EqualTo(11));
        Assert.That(read.IsValid, Is.True);
        Assert.That(rowsRead, Is.EqualTo(rowCount));
        Assert.That(cellsRead, Is.EqualTo(rowCount * 5));
        Assert.That(first!.GetCell("A1")!.FormattedValue, Is.EqualTo("1"));
        Assert.That(first.GetCell("B1")!.FormattedValue, Is.EqualTo("Group 1"));
        Assert.That(first.GetCell("C1")!.FormattedValue, Is.EqualTo("FALSE"));
        Assert.That(middle!.GetCell("A2500")!.FormattedValue, Is.EqualTo("2500"));
        Assert.That(middle.GetCell("B2500")!.FormattedValue, Is.EqualTo("Group 0"));
        Assert.That(middle.GetCell("C2500")!.FormattedValue, Is.EqualTo("TRUE"));
        Assert.That(last!.GetCell("A5000")!.FormattedValue, Is.EqualTo("5000"));
        Assert.That(last.GetCell("E5000")!.FormattedValue, Is.EqualTo("50000"));
        Assert.That(read.Cursor.RowsRead, Is.EqualTo(rowCount));
        Assert.That(read.Cursor.CellsRead, Is.EqualTo(rowCount * 5));
    }

    [Test]
    public void StreamingWorkbookWriter_WritesLargeGeneratedSheetsAndStreamingReaderSkipsSheetPayloads()
    {
        const int firstSheetRows = 3000;
        const int secondSheetRows = 750;
        MemoryStream stream = new MemoryStream();
        XlsxStreamingWorkbookWriteResult begin = new XlsxStreamingWorkbookWriter().BeginStream(stream);
        XlsxStreamingWorkbookWriteSession? session = begin.Session;

        session!.BeginSheet("Facts");
        int row = 1;
        while (row <= firstSheetRows)
        {
            session.WriteRow(CreateWorkbookRow(row));
            row = row + 1;
        }

        session.EndSheet();
        session.BeginSheet("Summary");
        row = 1;
        while (row <= secondSheetRows)
        {
            session.WriteTextRow(CreateSummaryRow(row));
            row = row + 1;
        }

        XlsxStreamingWorkbookWriteResult close = session.Close();
        byte[] bytes = stream.ToArray();
        XlsxStreamingWorkbookReadResult metadata = new XlsxStreamingWorkbookReader().ReadBytes(bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(bytes, new XlsxReaderOptions());
        XlsxStreamingSheetOpenResult facts = new XlsxStreamingSheetReader().OpenBytes(package.GetEntry(XlsxPackagePaths.Worksheet1)!.Data);
        XlsxStreamingSheetOpenResult summary = new XlsxStreamingSheetReader().OpenBytes(package.GetEntry("/xl/worksheets/sheet2.xml")!.Data);
        int factsRows = CountRows(facts);
        int summaryRows = CountRows(summary);

        Assert.That(begin.IsValid, Is.True);
        Assert.That(close.IsValid, Is.True);
        Assert.That(close.SheetCount, Is.EqualTo(2));
        Assert.That(close.RowCount, Is.EqualTo(firstSheetRows + secondSheetRows));
        Assert.That(close.CellCount, Is.EqualTo(firstSheetRows * 4 + secondSheetRows * 2));
        Assert.That(metadata.IsValid, Is.True);
        Assert.That(metadata.WorkbookInfo.WorkbookPart.Sheets.Count, Is.EqualTo(2));
        Assert.That(metadata.WorkbookInfo.WorkbookPart.Sheets[0].Name, Is.EqualTo("Facts"));
        Assert.That(metadata.WorkbookInfo.WorkbookPart.Sheets[1].Name, Is.EqualTo("Summary"));
        Assert.That(metadata.WorkbookInfo.SkippedSheetEntryCount, Is.EqualTo(2));
        Assert.That(metadata.WorkbookInfo.SkippedSheetBytes, Is.GreaterThan(0));
        Assert.That(package.IsValid, Is.True);
        Assert.That(factsRows, Is.EqualTo(firstSheetRows));
        Assert.That(summaryRows, Is.EqualTo(secondSheetRows));
        Assert.That(facts.Cursor!.Current, Is.Null);
        Assert.That(summary.Cursor!.Current, Is.Null);
    }

    static List<XlsxCellValue> CreateLargeRow(int row)
    {
        List<XlsxCellValue> values = new List<XlsxCellValue>();
        values.Add(XlsxCellValue.CreateNumber(row));
        values.Add(XlsxCellValue.CreateText("Group " + (row % 10).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        values.Add(XlsxCellValue.CreateBoolean(row % 2 == 0));
        values.Add(XlsxCellValue.CreateText("Static"));
        values.Add(XlsxCellValue.CreateNumber(row * 10));
        return values;
    }

    static List<XlsxCellValue> CreateWorkbookRow(int row)
    {
        List<XlsxCellValue> values = new List<XlsxCellValue>();
        values.Add(XlsxCellValue.CreateNumber(row));
        values.Add(XlsxCellValue.CreateInlineString("Item " + row.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        values.Add(XlsxCellValue.CreateNumber(row * 2));
        values.Add(XlsxCellValue.CreateBoolean(row == 3000));
        return values;
    }

    static List<string> CreateSummaryRow(int row)
    {
        List<string> values = new List<string>();
        values.Add("Summary");
        values.Add(row.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return values;
    }

    static int CountRows(XlsxStreamingSheetOpenResult result)
    {
        int count = 0;
        while (result.Cursor!.Read())
        {
            count = count + 1;
        }

        return count;
    }
}
