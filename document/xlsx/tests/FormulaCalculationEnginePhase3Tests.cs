using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaCalculationEnginePhase3Tests
{
    [Test]
    public void EvaluateFormula_CalculatesAdvancedStatisticalFunctions()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetNumberValue("A1", 1);
        part.SetNumberValue("A2", 2);
        part.SetNumberValue("A3", 3);
        part.SetNumberValue("A4", 4);

        XlsxFormulaCalculationResult result = XlsxFormulaCalculationEngine.CreateForWorksheet(part).EvaluateFormula("MEDIAN(A1:A4)+VAR.S(A1:A4)+STDEV.S(A1:A4)+PERCENTILE.INC(A1:A4,0.5)+LARGE(A1:A4,2)+SMALL(A1:A4,2)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(12.9576611).Within(0.000001));
    }

    [Test]
    public void EvaluateFormula_CalculatesFinancialFunctions()
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationEngine().EvaluateFormula("ROUND(PMT(0.01,12,1000),2)+ROUND(NPV(0.1,100,100),2)+FV(0,2,-50,-100)+PV(0,2,-50,0)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(384.7).Within(0.000001));
    }

    [Test]
    public void EvaluateFormula_CalculatesLetBindings()
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationEngine().EvaluateFormula("LET(rate,0.1,amount,100,amount+amount*rate)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(110));
    }

    [Test]
    public void EvaluateFormula_ReturnsLambdaMetadata()
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationEngine().EvaluateFormula("LAMBDA(x,y,x+y)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Lambda));
        Assert.That(result.Value.LambdaParameters.Count, Is.EqualTo(2));
        Assert.That(result.Value.LambdaParameters[0], Is.EqualTo("x"));
        Assert.That(result.Value.LambdaParameters[1], Is.EqualTo("y"));
        Assert.That(result.Value.Text, Is.EqualTo("x+y"));
        Assert.That(result.Value.LambdaBody, Is.Not.Null);
    }

    [Test]
    public void CalculateCellFormulaWithSpill_WritesDynamicArrayCells()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetFormulaCachedValue("B1", "SEQUENCE(2,2,1,1)", "", "");

        XlsxFormulaCalculationResult result = part.CalculateCellFormulaWithSpill("B1");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CalculatedFormulaCount, Is.EqualTo(1));
        Assert.That(result.CalculatedSpillCellCount, Is.EqualTo(3));
        Assert.That(part.GetCell("B1")!.Value, Is.EqualTo("1"));
        Assert.That(part.GetCell("B1")!.HasFormula, Is.True);
        Assert.That(part.GetCell("C1")!.Value, Is.EqualTo("2"));
        Assert.That(part.GetCell("B2")!.Value, Is.EqualTo("3"));
        Assert.That(part.GetCell("C2")!.Value, Is.EqualTo("4"));
    }

    [Test]
    public void CalculateCellFormulaWithSpill_ReportsBlockedSpillRanges()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetFormulaCachedValue("B1", "SEQUENCE(2,2,1,1)", "", "");
        part.SetTextValue("C1", "blocked");

        XlsxFormulaCalculationResult result = part.CalculateCellFormulaWithSpill("B1");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Error));
        Assert.That(result.Value.Error, Is.EqualTo("#SPILL!"));
        Assert.That(part.GetCell("B1")!.Value, Is.EqualTo("#SPILL!"));
        Assert.That(part.GetCell("B1")!.Type, Is.EqualTo("e"));
        Assert.That(part.GetCell("C1")!.Value, Is.EqualTo("blocked"));
    }
}
