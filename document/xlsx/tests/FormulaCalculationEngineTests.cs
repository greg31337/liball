using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaCalculationEngineTests
{
    [Test]
    public void EvaluateFormula_CalculatesArithmeticWithCoercion()
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationEngine().EvaluateFormula("1+2*3-\"4\"/2+TRUE");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(6));
    }

    [Test]
    public void EvaluateFormula_CalculatesComparisonsAndConcatenation()
    {
        XlsxFormulaCalculationEngine engine = new XlsxFormulaCalculationEngine();
        XlsxFormulaCalculationResult comparison = engine.EvaluateFormula("2*3>=5");
        XlsxFormulaCalculationResult concatenation = engine.EvaluateFormula("\"A\"&1&TRUE");

        Assert.That(comparison.IsValid, Is.True);
        Assert.That(comparison.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Boolean));
        Assert.That(comparison.Value.Boolean, Is.True);
        Assert.That(concatenation.IsValid, Is.True);
        Assert.That(concatenation.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Text));
        Assert.That(concatenation.Value.Text, Is.EqualTo("A1TRUE"));
    }

    [Test]
    public void EvaluateFormula_PropagatesSpreadsheetErrors()
    {
        XlsxFormulaCalculationResult division = new XlsxFormulaCalculationEngine().EvaluateFormula("10/0");
        XlsxFormulaCalculationResult literal = new XlsxFormulaCalculationEngine().EvaluateFormula("#REF!+1");

        Assert.That(division.IsValid, Is.True);
        Assert.That(division.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Error));
        Assert.That(division.Value.Error, Is.EqualTo("#DIV/0!"));
        Assert.That(literal.IsValid, Is.True);
        Assert.That(literal.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Error));
        Assert.That(literal.Value.Error, Is.EqualTo("#REF!"));
    }

    [Test]
    public void EvaluateFormula_ResolvesReferencesRangesAndAggregates()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetNumberValue("A1", 1);
        part.SetNumberValue("A2", 2);
        part.SetNumberValue("B1", 3);
        part.SetTextValue("B2", "ignored");
        XlsxFormulaCalculationEngine engine = XlsxFormulaCalculationEngine.CreateForWorksheet(part);

        XlsxFormulaCalculationResult result = engine.EvaluateFormula("SUM(A1:B2)+AVERAGE(A1:A2)+COUNT(A1:B2)+MIN(A1:B1)+MAX(A1:B1)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(14.5));
    }

    [Test]
    public void CalculateCellFormula_WritesCachedFormulaValue()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetNumberValue("A1", 4);
        part.SetNumberValue("A2", 6);
        part.SetFormulaCachedValue("B1", "SUM(A1:A2)", "", "");

        XlsxFormulaCalculationResult result = part.CalculateCellFormula("B1");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CalculatedFormulaCount, Is.EqualTo(1));
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(10));
        Assert.That(part.GetCell("B1")!.Value, Is.EqualTo("10"));
        Assert.That(part.GetCell("B1")!.FormulaInfo.CachedValue, Is.EqualTo("10"));
        Assert.That(part.GetCell("B1")!.FormulaInfo.CachedType, Is.EqualTo(""));
    }

    [Test]
    public void CalculateAllFormulas_UpdatesBooleanTextAndErrorCachedValues()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetFormulaCachedValue("A1", "1=1", "", "");
        part.SetFormulaCachedValue("A2", "\"x\"&2", "", "");
        part.SetFormulaCachedValue("A3", "1/0", "", "");

        XlsxFormulaCalculationResult result = part.CalculateAllFormulas();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CalculatedFormulaCount, Is.EqualTo(3));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("1"));
        Assert.That(part.GetCell("A1")!.Type, Is.EqualTo("b"));
        Assert.That(part.GetCell("A2")!.Value, Is.EqualTo("x2"));
        Assert.That(part.GetCell("A2")!.Type, Is.EqualTo("str"));
        Assert.That(part.GetCell("A3")!.Value, Is.EqualTo("#DIV/0!"));
        Assert.That(part.GetCell("A3")!.Type, Is.EqualTo("e"));
    }

    [Test]
    public void EvaluateFormula_ReportsUnsupportedFunctionsAsNameErrors()
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationEngine().EvaluateFormula("UNSUPPORTED(1)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Error));
        Assert.That(result.Value.Error, Is.EqualTo("#NAME?"));
    }
}
