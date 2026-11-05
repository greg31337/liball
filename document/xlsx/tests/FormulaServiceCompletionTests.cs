using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaServiceCompletionTests
{
    [Test]
    public void AnalyzeFormula_ReportsVolatileUnsafeExternalUnsupportedAndFutureSyntax()
    {
        XlsxFormulaService service = new XlsxFormulaService();

        XlsxFormulaAnalysisResult result = service.AnalyzeFormula("TODAY()+WEBSERVICE(\"https://example.test\")+[Book.xlsx]Sheet1!A1+_xlfn.SUM(1,2)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasVolatileFunction, Is.True);
        Assert.That(result.HasUnsafeFunction, Is.True);
        Assert.That(result.HasExternalReference, Is.True);
        Assert.That(result.HasFutureSyntax, Is.True);
        Assert.That(result.VolatileFunctions, Does.Contain("TODAY"));
        Assert.That(result.UnsafeFunctions, Does.Contain("WEBSERVICE"));
        Assert.That(result.UnsupportedFunctions, Does.Contain("TODAY"));
        Assert.That(result.UnsupportedFunctions, Does.Contain("WEBSERVICE"));
        Assert.That(result.FutureFunctions, Does.Contain("_XLFN.SUM"));
        Assert.That(result.UnsupportedFunctions, Does.Not.Contain("_XLFN.SUM"));
        Assert.That(result.ExternalReferences, Does.Contain("[Book.xlsx]Sheet1!A1"));
    }

    [Test]
    public void BuildWorkbookDependencyGraph_ConnectsSheetsAndFindsTransitiveImpact()
    {
        XlsxWorkbook workbook = CreateWorkbook("Inputs", "Results");
        workbook.Worksheets[0].SetNumberValue("A1", 4);
        workbook.Worksheets[1].SetFormulaCachedValue("B1", "Inputs!A1*2", "", "");
        workbook.Worksheets[1].SetFormulaCachedValue("C1", "B1+1", "", "");
        XlsxFormulaService service = new XlsxFormulaService();

        XlsxWorkbookFormulaDependencyGraphResult graph = service.BuildWorkbookDependencyGraph(workbook);
        XlsxImpactedCellsResult impact = service.FindImpactedCells(graph, new List<string> { "Inputs!A1" });

        Assert.That(graph.IsValid, Is.True);
        Assert.That(graph.GetNode("Results!B1"), Is.Not.Null);
        Assert.That(graph.GetNode("Results!B1")!.Precedents, Does.Contain("Inputs!A1"));
        Assert.That(graph.GetNode("Inputs!A1")!.Dependents, Does.Contain("Results!B1"));
        Assert.That(impact.IsValid, Is.True);
        Assert.That(impact.DirectDependents, Is.EqualTo(new[] { "Results!B1" }));
        Assert.That(impact.ImpactedCells, Is.EqualTo(new[] { "Results!B1", "Results!C1" }));
    }

    [Test]
    public void CalculateFormula_UsesWorkbookContextForCrossSheetCellsAndRanges()
    {
        XlsxWorkbook workbook = CreateWorkbook("Inputs", "Data Sheet", "Results");
        workbook.Worksheets[0].SetNumberValue("A1", 4);
        workbook.Worksheets[1].SetNumberValue("A1", 2);
        workbook.Worksheets[1].SetNumberValue("A2", 3);
        XlsxFormulaService service = new XlsxFormulaService();

        XlsxFormulaCalculationResult result = service.CalculateFormula(workbook, "Results", "Inputs!A1+SUM('Data Sheet'!A1:A2)", new XlsxFormulaCalculationOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(9));
    }

    [Test]
    public void CalculateFormula_AppliesSafetyOptionsAndReportsFutureSyntax()
    {
        XlsxWorkbook workbook = CreateWorkbook("Inputs", "Results");
        workbook.Worksheets[0].SetNumberValue("A1", 4);
        XlsxFormulaService service = new XlsxFormulaService();
        XlsxFormulaCalculationOptions crossSheetOptions = new XlsxFormulaCalculationOptions();
        crossSheetOptions.AllowCrossSheetReferences = false;

        XlsxFormulaCalculationResult blockedReference = service.CalculateFormula(workbook, "Results", "Inputs!A1", crossSheetOptions);
        XlsxFormulaCalculationResult blockedUnsafe = service.CalculateFormula(workbook, "Results", "WEBSERVICE(\"https://example.test\")", new XlsxFormulaCalculationOptions());
        XlsxFormulaCalculationResult future = service.CalculateFormula(workbook, "Results", "_xlfn.SUM(1,2)", new XlsxFormulaCalculationOptions());

        Assert.That(blockedReference.IsValid, Is.False);
        Assert.That(blockedReference.Value!.Error, Is.EqualTo("#REF!"));
        Assert.That(HasDiagnostic(blockedReference, "formulaCalculation.crossSheetReferenceBlocked"), Is.True);
        Assert.That(blockedUnsafe.IsValid, Is.False);
        Assert.That(blockedUnsafe.Value!.Error, Is.EqualTo("#BLOCKED!"));
        Assert.That(blockedUnsafe.UnsafeFunctions, Does.Contain("WEBSERVICE"));
        Assert.That(future.IsValid, Is.True);
        Assert.That(future.Value!.Number, Is.EqualTo(3));
        Assert.That(future.FutureFunctions, Does.Contain("_XLFN.SUM"));
        Assert.That(HasDiagnostic(future, "formulaCalculation.futureSyntax"), Is.True);
    }

    [Test]
    public void CalculateWorkbook_PreservesUnsupportedCachedValuesAndCalculatesCrossSheetDependents()
    {
        XlsxWorkbook workbook = CreateWorkbook("Inputs", "Results");
        workbook.Worksheets[0].SetNumberValue("A1", 5);
        workbook.Worksheets[1].SetFormulaCachedValue("A1", "Inputs!A1*3", "", "");
        workbook.Worksheets[1].SetFormulaCachedValue("B1", "MYSTERY(1)", "99", "n");
        XlsxFormulaService service = new XlsxFormulaService();

        XlsxFormulaCalculationResult result = service.CalculateWorkbook(workbook, new XlsxFormulaCalculationOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CalculatedFormulaCount, Is.EqualTo(1));
        Assert.That(result.PreservedFormulaCount, Is.EqualTo(1));
        Assert.That(result.UnsupportedFunctions, Does.Contain("MYSTERY"));
        Assert.That(workbook.Worksheets[1].GetCell("A1")!.Value, Is.EqualTo("15"));
        Assert.That(workbook.Worksheets[1].GetCell("B1")!.Value, Is.EqualTo("99"));
    }

    static XlsxWorkbook CreateWorkbook(params string[] sheetNames)
    {
        XlsxWorkbook workbook = new XlsxWorkbook();
        uint sheetId = 1;
        foreach (string sheetName in sheetNames)
        {
            XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
            sheet.Name = sheetName;
            sheet.SheetId = sheetId;
            workbook.WorkbookPart.Sheets.Add(sheet);
            workbook.Worksheets.Add(new XlsxWorksheetPart());
            sheetId = sheetId + 1;
        }
        return workbook;
    }

    static bool HasDiagnostic(XlsxFormulaCalculationResult result, string code)
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
