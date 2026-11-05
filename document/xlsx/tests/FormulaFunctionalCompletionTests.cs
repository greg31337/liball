using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaFunctionalCompletionTests
{
    [Test]
    public void FormulaService_ResolvesGlobalLocalNestedAndRangeDefinedNames()
    {
        XlsxWorkbook workbook = CreateWorkbook("Inputs", "Results");
        workbook.Worksheets[0].SetNumberValue("A1", 10d);
        workbook.Worksheets[0].SetNumberValue("A2", 20d);
        AddDefinedName(workbook, "Taxable", "Inputs!$A$1:$A$2", -1);
        AddDefinedName(workbook, "Gross", "=SUM(Taxable)", -1);
        AddDefinedName(workbook, "TaxRate", "0.1", -1);
        AddDefinedName(workbook, "TaxRate", "0.5", 1);
        XlsxFormulaService service = new XlsxFormulaService();

        XlsxFormulaCalculationResult inputResult = service.CalculateFormula(workbook, "Inputs", "Gross*TaxRate", new XlsxFormulaCalculationOptions());
        XlsxFormulaCalculationResult resultResult = service.CalculateFormula(workbook, "Results", "Gross*TaxRate", new XlsxFormulaCalculationOptions());

        AssertNumber(inputResult, 3d);
        AssertNumber(resultResult, 15d);

        workbook.Worksheets[1].SetFormulaCachedValue("A1", "Gross*TaxRate", "", "");
        XlsxFormulaCalculationResult recalculation = service.CalculateWorkbook(workbook, new XlsxFormulaCalculationOptions());

        Assert.That(recalculation.IsValid, Is.True, Diagnostics(recalculation));
        Assert.That(recalculation.CalculatedFormulaCount, Is.EqualTo(1));
        Assert.That(workbook.Worksheets[1].GetCell("A1")!.Value, Is.EqualTo("15"));
    }

    [Test]
    public void FormulaService_AppliesPoliciesAndDetectsCircularDefinedNames()
    {
        XlsxWorkbook workbook = CreateWorkbook("Inputs", "Results");
        workbook.Worksheets[0].SetNumberValue("A1", 8d);
        AddDefinedName(workbook, "UnsafeName", "WEBSERVICE(\"https://example.test\")", -1);
        AddDefinedName(workbook, "VolatileName", "TODAY()", -1);
        AddDefinedName(workbook, "CrossSheetName", "Inputs!A1", -1);
        AddDefinedName(workbook, "ExternalName", "[Book.xlsx]Sheet1!A1", -1);
        AddDefinedName(workbook, "FirstName", "SecondName", -1);
        AddDefinedName(workbook, "SecondName", "FirstName", -1);
        XlsxFormulaService service = new XlsxFormulaService();

        XlsxFormulaCalculationResult unsafeResult = service.CalculateFormula(workbook, "Results", "UnsafeName", new XlsxFormulaCalculationOptions());
        XlsxFormulaCalculationOptions volatileOptions = new XlsxFormulaCalculationOptions();
        volatileOptions.AllowVolatileFunctions = false;
        XlsxFormulaCalculationResult volatileResult = service.CalculateFormula(workbook, "Results", "VolatileName", volatileOptions);
        XlsxFormulaCalculationOptions crossSheetOptions = new XlsxFormulaCalculationOptions();
        crossSheetOptions.AllowCrossSheetReferences = false;
        XlsxFormulaCalculationResult crossSheetResult = service.CalculateFormula(workbook, "Results", "CrossSheetName", crossSheetOptions);
        XlsxFormulaCalculationResult externalResult = service.CalculateFormula(workbook, "Results", "ExternalName", new XlsxFormulaCalculationOptions());
        XlsxFormulaCalculationResult circularResult = service.CalculateFormula(workbook, "Results", "FirstName", new XlsxFormulaCalculationOptions());

        AssertError(unsafeResult, "#BLOCKED!", "formulaCalculation.unsafeFunctionBlocked");
        Assert.That(unsafeResult.UnsafeFunctions, Does.Contain("WEBSERVICE"));
        Assert.That(unsafeResult.UnsupportedFunctions, Does.Contain("WEBSERVICE"));
        AssertError(volatileResult, "#BLOCKED!", "formulaCalculation.volatileFunctionBlocked");
        Assert.That(volatileResult.VolatileFunctions, Does.Contain("TODAY"));
        AssertError(crossSheetResult, "#REF!", "formulaCalculation.crossSheetReferenceBlocked");
        Assert.That(externalResult.IsValid, Is.True, Diagnostics(externalResult));
        Assert.That(externalResult.Value!.Error, Is.EqualTo("#REF!"));
        Assert.That(externalResult.ExternalReferences, Does.Contain("[Book.xlsx]Sheet1!A1"));
        AssertError(circularResult, "#NAME?", "formulaCalculation.definedNameCircular");
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

    static void AddDefinedName(XlsxWorkbook workbook, string name, string formula, int localSheetId)
    {
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = name;
        definedName.Text = formula;
        if (localSheetId >= 0)
        {
            definedName.LocalSheetId = localSheetId;
            definedName.HasLocalSheetId = true;
        }
        workbook.WorkbookPart.DefinedNames.Add(definedName);
    }

    static void AssertNumber(XlsxFormulaCalculationResult result, double expected)
    {
        Assert.That(result.IsValid, Is.True, Diagnostics(result));
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(expected).Within(0.0000001d));
    }

    static void AssertError(XlsxFormulaCalculationResult result, string error, string diagnosticCode)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Error));
        Assert.That(result.Value.Error, Is.EqualTo(error));
        Assert.That(HasDiagnostic(result, diagnosticCode), Is.True, Diagnostics(result));
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

    static string Diagnostics(XlsxFormulaCalculationResult result)
    {
        string text = "";
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            text = text + diagnostic.Code + ": " + diagnostic.Message + "\n";
        }
        return text;
    }
}
