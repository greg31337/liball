using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookPerformanceStatisticsTests
{
    [Test]
    public void SaveAndOpen_DoNotCollectStatisticsByDefault()
    {
        XlsxWorkbook workbook = CreateWorkbookWithCells();

        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(save.Bytes);

        Assert.That(save.IsValid, Is.True);
        Assert.That(open.IsValid, Is.True);
        Assert.That(save.Statistics.Enabled, Is.False);
        Assert.That(open.Statistics.Enabled, Is.False);
    }

    [Test]
    public void SaveBytes_CollectsOptionalWriteStatistics()
    {
        XlsxWorkbook workbook = CreateWorkbookWithCells();
        XlsxWriterOptions options = new XlsxWriterOptions();
        options.CollectStatistics = true;

        XlsxWorkbookSaveResult result = workbook.SaveBytesWithOptions(options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Statistics.Enabled, Is.True);
        Assert.That(result.Statistics, Is.SameAs(options.Statistics));
        Assert.That(result.Statistics.Operation, Is.EqualTo("workbook.save"));
        Assert.That(result.Statistics.PackageEntryCount, Is.GreaterThanOrEqualTo(4));
        Assert.That(result.Statistics.OutputBytes, Is.EqualTo(result.Bytes.Length));
        Assert.That(result.Statistics.WorksheetCount, Is.EqualTo(1));
        Assert.That(result.Statistics.RowCount, Is.EqualTo(1));
        Assert.That(result.Statistics.CellCount, Is.EqualTo(2));
        Assert.That(result.Statistics.FormulaCount, Is.EqualTo(1));
        Assert.That(result.Statistics.RelationshipCount, Is.EqualTo(2));
    }

    [Test]
    public void OpenBytes_CollectsOptionalParseStatistics()
    {
        XlsxWorkbookSaveResult save = CreateWorkbookWithCells().SaveBytes();
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.CollectStatistics = true;
        options.SelectedSheets.Add("Sheet1");

        XlsxWorkbookOpenResult result = XlsxWorkbook.OpenBytes(save.Bytes, options);

        Assert.That(save.IsValid, Is.True);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Statistics.Enabled, Is.True);
        Assert.That(result.Statistics, Is.SameAs(options.Statistics));
        Assert.That(result.Statistics.Operation, Is.EqualTo("workbook.open"));
        Assert.That(result.Statistics.InputBytes, Is.EqualTo(save.Bytes.Length));
        Assert.That(result.Statistics.PackageEntryCount, Is.GreaterThanOrEqualTo(4));
        Assert.That(result.Statistics.TotalCompressedBytes, Is.GreaterThan(0));
        Assert.That(result.Statistics.TotalUncompressedBytes, Is.GreaterThan(0));
        Assert.That(result.Statistics.WorksheetCount, Is.EqualTo(1));
        Assert.That(result.Statistics.RowCount, Is.EqualTo(1));
        Assert.That(result.Statistics.CellCount, Is.EqualTo(2));
        Assert.That(result.Statistics.FormulaCount, Is.EqualTo(1));
    }

    static XlsxWorkbook CreateWorkbookWithCells()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = XlsxPackagePaths.Worksheet1;
        worksheet.SetNumberValue("A1", 2);
        worksheet.SetFormulaCachedValue("B1", "A1*3", "6", "n");
        workbook.Worksheets.Add(worksheet);
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        workbook.UnknownParts.AddPart(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet, write.Bytes);
        return workbook;
    }
}
