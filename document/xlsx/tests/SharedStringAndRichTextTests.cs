using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SharedStringAndRichTextTests
{
    [Test]
    public void WriteRead_DeduplicatesRichTextItemsAndCombinesReferenceCounts()
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();
        part.Items.Add(CreatePlainItem("Alpha", 2));
        part.Items.Add(CreateRichItem("BoldTail", "<rPr><b /></rPr>", "", 1));
        part.Items.Add(CreateRichItem("BoldTail", "<rPr><b /></rPr>", "", 2));
        part.Items.Add(CreateRichItem("BoldTail", "<rPr><i /></rPr>", "", 1));

        XlsxSharedStringTableWriteResult writeResult = new XlsxSharedStringTableWriter().WriteString(part);
        XlsxSharedStringTablePart roundTrip = new XlsxSharedStringTableReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Count, Is.EqualTo(6));
        Assert.That(writeResult.UniqueCount, Is.EqualTo(3));
        Assert.That(writeResult.Xml, Does.Contain("count=\"6\""));
        Assert.That(writeResult.Xml, Does.Contain("uniqueCount=\"3\""));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Items.Count, Is.EqualTo(3));
        Assert.That(roundTrip.GetText(0), Is.EqualTo("Alpha"));
        Assert.That(roundTrip.Items[1].IsRichText, Is.True);
        Assert.That(roundTrip.Items[1].Text, Is.EqualTo("BoldTail"));
        Assert.That(roundTrip.Items[1].RichTextRuns[0].PropertiesRawXml, Does.Contain("<b"));
        Assert.That(roundTrip.Items[2].RichTextRuns[0].PropertiesRawXml, Does.Contain("<i"));
    }

    [Test]
    public void WorkbookSaveOpen_PreservesSharedStringPartAndWorksheetReferences()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.RenameSheet("Sheet1", "Strings");
        XlsxSharedStringTablePart sharedStrings = new XlsxSharedStringTablePart();
        sharedStrings.Items.Add(CreatePlainItem("Alpha", 2));
        sharedStrings.Items.Add(CreateRichItem("BoldTail", "<rPr><b /></rPr>", "<rPr><i /></rPr>", 1));
        XlsxSharedStringTableWriteResult sharedStringWrite = new XlsxSharedStringTableWriter().WriteBytes(sharedStrings);
        XlsxWorksheetPart worksheet = CreateWorksheetWithSharedStringReferences();
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        workbook.WorkbookRelationships.Relationships.Add(XlsxWorkbook.CreateRelationship("rIdSharedStrings1", XlsxRelationshipTypes.SharedStrings, "sharedStrings.xml"));
        workbook.UnknownParts.AddPart(XlsxPackagePaths.SharedStrings, XlsxContentTypes.SharedStrings, sharedStringWrite.Bytes);
        workbook.UnknownParts.AddPart(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.SelectedSheets.Add("Strings");

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes, readerOptions);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());
        XlsxSharedStringTablePart packageSharedStrings = new XlsxSharedStringTableReader().ReadBytes(package.GetEntry(XlsxPackagePaths.SharedStrings)!.Data);
        XlsxWorkbookRelationshipResolutionResult relationships = new XlsxWorkbookRelationshipResolver().Resolve(openResult.Workbook!.WorkbookPart, openResult.Workbook.WorkbookRelationships);

        Assert.That(sharedStringWrite.IsValid, Is.True);
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(package.GetEntry(XlsxPackagePaths.SharedStrings), Is.Not.Null);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(relationships.IsValid, Is.True);
        Assert.That(relationships.HasSharedStrings, Is.True);
        Assert.That(relationships.SharedStrings!.TargetPath, Is.EqualTo(XlsxPackagePaths.SharedStrings));
        Assert.That(packageSharedStrings.IsValid, Is.True);
        Assert.That(packageSharedStrings.Count, Is.EqualTo(3));
        Assert.That(packageSharedStrings.UniqueCount, Is.EqualTo(2));
        Assert.That(packageSharedStrings.GetText(0), Is.EqualTo("Alpha"));
        Assert.That(packageSharedStrings.Items[1].IsRichText, Is.True);
        Assert.That(packageSharedStrings.Items[1].RichTextRuns.Count, Is.EqualTo(2));
        Assert.That(packageSharedStrings.Items[1].RichTextRuns[0].PropertiesRawXml, Does.Contain("<b"));
        Assert.That(openResult.Workbook.Worksheets.Count, Is.EqualTo(1));
        AssertSharedStringCell(openResult.Workbook.Worksheets[0].GetCell("A1"), 0u);
        AssertSharedStringCell(openResult.Workbook.Worksheets[0].GetCell("B1"), 1u);
        AssertSharedStringCell(openResult.Workbook.Worksheets[0].GetCell("A2"), 0u);
    }

    static XlsxWorksheetPart CreateWorksheetWithSharedStringReferences()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetSharedStringValue("A1", 0u);
        worksheet.SetSharedStringValue("B1", 1u);
        worksheet.SetSharedStringValue("A2", 0u);
        worksheet.ApplyCalculatedDimension();
        return worksheet;
    }

    static XlsxSharedStringItem CreatePlainItem(string text, int referenceCount)
    {
        XlsxSharedStringItem item = new XlsxSharedStringItem();
        item.Text = text;
        item.ReferenceCount = referenceCount;
        return item;
    }

    static XlsxSharedStringItem CreateRichItem(string text, string firstPropertiesRawXml, string secondPropertiesRawXml, int referenceCount)
    {
        XlsxSharedStringItem item = new XlsxSharedStringItem();
        item.Text = text;
        item.IsRichText = true;
        item.ReferenceCount = referenceCount;
        item.RichTextRuns.Add(CreateRun("Bold", firstPropertiesRawXml));
        item.RichTextRuns.Add(CreateRun("Tail", secondPropertiesRawXml));
        return item;
    }

    static XlsxSharedStringRichTextRun CreateRun(string text, string propertiesRawXml)
    {
        XlsxSharedStringRichTextRun run = new XlsxSharedStringRichTextRun();
        run.Text = text;
        run.PropertiesRawXml = propertiesRawXml;
        return run;
    }

    static void AssertSharedStringCell(XlsxWorksheetCell? cell, uint expectedIndex)
    {
        XlsxCellValueResult value = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(value.IsValid, Is.True);
        Assert.That(value.Value.Kind, Is.EqualTo(XlsxCellValueKind.SharedString));
        Assert.That(value.Value.SharedStringIndex, Is.EqualTo(expectedIndex));
    }
}
