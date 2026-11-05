using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookRecalculationModeTests
{
    [Test]
    public void PreserveCachedFormulaValues_LeavesCachedValuesAndDisablesForcedRecalc()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        sheet.SetFormulaCachedValue("A1", "1+1", "99", "");
        List<XlsxWorksheetPart> worksheets = CreateWorksheetList(sheet);

        XlsxWorkbookRecalculationResult result = workbook.PreserveCachedFormulaValues(worksheets);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PreservedFormulaCount, Is.EqualTo(1));
        Assert.That(sheet.GetCell("A1")!.Value, Is.EqualTo("99"));
        Assert.That(workbook.WorkbookPart.HasCalculationProperties, Is.True);
        Assert.That(workbook.WorkbookPart.CalculationProperties.CalcMode, Is.EqualTo("manual"));
        Assert.That(workbook.WorkbookPart.CalculationProperties.FullCalcOnLoad, Is.False);
        Assert.That(workbook.WorkbookPart.CalculationProperties.ForceFullCalc, Is.False);
    }

    [Test]
    public void RecalculateSupportedFormulas_UpdatesAllFormulaCaches()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        sheet.SetFormulaCachedValue("A1", "1+1", "99", "");
        sheet.SetFormulaCachedValue("A2", "TEXTJOIN(\"/\",TRUE,\"x\",\"y\")", "old", "str");
        List<XlsxWorksheetPart> worksheets = CreateWorksheetList(sheet);

        XlsxWorkbookRecalculationResult result = workbook.RecalculateSupportedFormulas(worksheets);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.RecalculatedFormulaCount, Is.EqualTo(2));
        Assert.That(result.PreservedFormulaCount, Is.EqualTo(0));
        Assert.That(sheet.GetCell("A1")!.Value, Is.EqualTo("2"));
        Assert.That(sheet.GetCell("A2")!.Value, Is.EqualTo("x/y"));
        Assert.That(workbook.WorkbookPart.CalculationProperties.CalcMode, Is.EqualTo("auto"));
        Assert.That(workbook.WorkbookPart.CalculationProperties.FullCalcOnLoad, Is.False);
        Assert.That(workbook.WorkbookPart.CalculationProperties.ForceFullCalc, Is.False);
    }

    [Test]
    public void RecalculateChangedFormulas_OnlyUpdatesCellsMissingCachedValues()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        sheet.SetFormulaCachedValue("A1", "1+1", "2", "");
        sheet.SetFormulaCachedValue("A2", "2+3", "", "");
        List<XlsxWorksheetPart> worksheets = CreateWorksheetList(sheet);

        XlsxWorkbookRecalculationResult result = workbook.RecalculateChangedFormulas(worksheets);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.RecalculatedFormulaCount, Is.EqualTo(1));
        Assert.That(result.ChangedFormulaCount, Is.EqualTo(1));
        Assert.That(result.PreservedFormulaCount, Is.EqualTo(1));
        Assert.That(sheet.GetCell("A1")!.Value, Is.EqualTo("2"));
        Assert.That(sheet.GetCell("A2")!.Value, Is.EqualTo("5"));
    }

    [Test]
    public void MarkForExcelRecalculation_SetsWorkbookCalculationFlags()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();

        XlsxWorkbookRecalculationResult result = workbook.MarkForExcelRecalculation();
        XlsxWorkbookPartWriteResult writeResult = new XlsxWorkbookPartWriter().WriteString(workbook.WorkbookPart);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.MarkedForExcelRecalculation, Is.True);
        Assert.That(workbook.WorkbookPart.CalculationProperties.CalcMode, Is.EqualTo("auto"));
        Assert.That(workbook.WorkbookPart.CalculationProperties.FullCalcOnLoad, Is.True);
        Assert.That(workbook.WorkbookPart.CalculationProperties.ForceFullCalc, Is.True);
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<calcPr calcMode=\"auto\" fullCalcOnLoad=\"1\" forceFullCalc=\"1\""));
    }

    [Test]
    public void RecalculateSupportedFormulas_AppliesSpillResults()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        sheet.SetFormulaCachedValue("B1", "SEQUENCE(1,2,1,1)", "", "");
        List<XlsxWorksheetPart> worksheets = CreateWorksheetList(sheet);

        XlsxWorkbookRecalculationResult result = workbook.RecalculateSupportedFormulas(worksheets);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.RecalculatedFormulaCount, Is.EqualTo(1));
        Assert.That(result.SpillCellCount, Is.EqualTo(1));
        Assert.That(sheet.GetCell("B1")!.Value, Is.EqualTo("1"));
        Assert.That(sheet.GetCell("C1")!.Value, Is.EqualTo("2"));
    }

    static List<XlsxWorksheetPart> CreateWorksheetList(XlsxWorksheetPart sheet)
    {
        List<XlsxWorksheetPart> worksheets = new List<XlsxWorksheetPart>();
        worksheets.Add(sheet);
        return worksheets;
    }
}
