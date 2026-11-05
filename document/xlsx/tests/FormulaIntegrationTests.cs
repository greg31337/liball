using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaIntegrationTests
{
    [Test]
    public void ParseWriteUpdateCalculateAndRoundTripWorksheetFormula()
    {
        XlsxWorksheetPart worksheet = CreateFormulaWorksheet();
        string originalFormula = "SUM(A1:B2)+C1";
        XlsxFormulaParseResult parseResult = new XlsxFormulaParser().Parse(originalFormula);
        XlsxFormulaWriteResult formulaWrite = new XlsxFormulaWriter().WriteParseResult(parseResult);
        worksheet.SetFormulaCachedValue("D1", formulaWrite.Text, "", "");

        XlsxWorksheetRowEditResult insertResult = worksheet.InsertRows(2, 1);
        XlsxFormulaParseResult updatedParse = new XlsxFormulaParser().Parse(worksheet.GetCell("D1")!.Formula);
        XlsxFormulaWriteResult updatedWrite = new XlsxFormulaWriter().WriteParseResult(updatedParse);
        XlsxFormulaCalculationResult calculation = worksheet.CalculateCellFormula("D1");
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(worksheetWrite.Xml);

        Assert.That(parseResult.IsValid, Is.True);
        Assert.That(formulaWrite.IsValid, Is.True);
        Assert.That(formulaWrite.Text, Is.EqualTo(originalFormula));
        Assert.That(insertResult.IsValid, Is.True);
        Assert.That(insertResult.FormulaReferenceUpdateCount, Is.EqualTo(1));
        Assert.That(worksheet.GetCell("D1")!.Formula, Is.EqualTo("SUM(A1:B3)+C1"));
        Assert.That(updatedParse.IsValid, Is.True);
        Assert.That(updatedWrite.IsValid, Is.True);
        Assert.That(updatedWrite.Text, Is.EqualTo("SUM(A1:B3)+C1"));
        Assert.That(calculation.IsValid, Is.True);
        Assert.That(calculation.CalculatedFormulaCount, Is.EqualTo(1));
        Assert.That(calculation.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(calculation.Value.Number, Is.EqualTo(15d).Within(0.0000001d));
        Assert.That(worksheet.GetCell("D1")!.Value, Is.EqualTo("15"));
        Assert.That(worksheet.GetCell("D1")!.FormulaInfo.CachedValue, Is.EqualTo("15"));
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(worksheetWrite.Xml, Does.Contain("<f>SUM(A1:B3)+C1</f><v>15</v>"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.GetCell("D1")!.Formula, Is.EqualTo("SUM(A1:B3)+C1"));
        Assert.That(roundTrip.GetCell("D1")!.Value, Is.EqualTo("15"));
        Assert.That(roundTrip.GetCell("A3")!.Value, Is.EqualTo("2"));
        Assert.That(roundTrip.GetCell("B3")!.Value, Is.EqualTo("4"));
    }

    [Test]
    public void WorkbookSaveOpen_PreservesCalculatedFormulaModels()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.RenameSheet("Sheet1", "Formulas");
        XlsxWorksheetPart worksheet = CreateFormulaWorksheet();
        worksheet.SetFormulaCachedValue("D1", "SUM(A1:B2)+C1", "", "");
        XlsxFormulaCalculationResult calculation = worksheet.CalculateCellFormula("D1");
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        workbook.UnknownParts.AddPart(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.SelectedSheets.Add("Formulas");

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes, readerOptions);
        XlsxWorksheetPart openWorksheet = openResult.Workbook!.Worksheets[0];

        Assert.That(calculation.IsValid, Is.True);
        Assert.That(calculation.Value!.Number, Is.EqualTo(15d).Within(0.0000001d));
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook.Worksheets.Count, Is.EqualTo(1));
        Assert.That(openWorksheet.GetCell("D1")!.HasFormula, Is.True);
        Assert.That(openWorksheet.GetCell("D1")!.Formula, Is.EqualTo("SUM(A1:B2)+C1"));
        Assert.That(openWorksheet.GetCell("D1")!.FormulaInfo.Text, Is.EqualTo("SUM(A1:B2)+C1"));
        Assert.That(openWorksheet.GetCell("D1")!.FormulaInfo.CachedValue, Is.EqualTo("15"));
        Assert.That(openWorksheet.GetCell("D1")!.Value, Is.EqualTo("15"));
    }

    static XlsxWorksheetPart CreateFormulaWorksheet()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetNumberValue("A1", 1);
        worksheet.SetNumberValue("A2", 2);
        worksheet.SetNumberValue("B1", 3);
        worksheet.SetNumberValue("B2", 4);
        worksheet.SetNumberValue("C1", 5);
        return worksheet;
    }
}
