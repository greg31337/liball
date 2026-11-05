using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaCalculationEnginePhase2Tests
{
    [Test]
    public void EvaluateFormula_CalculatesLogicalFunctionsLazily()
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationEngine().EvaluateFormula("IF(AND(TRUE,1,NOT(FALSE)),\"yes\",\"no\")&IFERROR(1/0,\"err\")");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Text));
        Assert.That(result.Value.Text, Is.EqualTo("yeserr"));
    }

    [Test]
    public void EvaluateFormula_CalculatesTextFunctions()
    {
        XlsxFormulaCalculationEngine engine = new XlsxFormulaCalculationEngine();
        XlsxFormulaCalculationResult concat = engine.EvaluateFormula("CONCAT(UPPER(LEFT(\"abc\",2)),LOWER(RIGHT(\"XY\",1)),MID(\"abcd\",2,2))");
        XlsxFormulaCalculationResult joined = engine.EvaluateFormula("TEXTJOIN(\"-\",TRUE,\"a\",\"\",\"b\")");
        XlsxFormulaCalculationResult length = engine.EvaluateFormula("LEN(TRIM(\"  abc  \"))");

        Assert.That(concat.IsValid, Is.True);
        Assert.That(concat.Value!.Text, Is.EqualTo("ABybc"));
        Assert.That(joined.IsValid, Is.True);
        Assert.That(joined.Value!.Text, Is.EqualTo("a-b"));
        Assert.That(length.IsValid, Is.True);
        Assert.That(length.Value!.Number, Is.EqualTo(3));
    }

    [Test]
    public void EvaluateFormula_CalculatesDateTimeAndMathFunctions()
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationEngine().EvaluateFormula("YEAR(DATE(2024,2,29))+MONTH(DATE(2024,2,29))+DAY(DATE(2024,2,29))+ROUND(ABS(-2.345),2)+INT(3.9)+POWER(2,3)+SQRT(9)+MOD(10,3)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(2072.35).Within(0.0000001));
    }

    [Test]
    public void EvaluateFormula_CalculatesCommonLookupFunctions()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "a");
        part.SetTextValue("A2", "b");
        part.SetNumberValue("B1", 10);
        part.SetNumberValue("B2", 20);
        part.SetNumberValue("C1", 100);
        part.SetNumberValue("C2", 200);

        XlsxFormulaCalculationResult result = XlsxFormulaCalculationEngine.CreateForWorksheet(part).EvaluateFormula("VLOOKUP(\"b\",A1:C2,3,FALSE)+XLOOKUP(\"a\",A1:A2,C1:C2)+MATCH(\"b\",A1:A2,0)+INDEX(A1:C2,2,2)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(322));
    }

    [Test]
    public void EvaluateFormula_CalculatesSequenceTransposeAndFilterArrays()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetNumberValue("A1", 5);
        part.SetNumberValue("A2", 10);
        part.SetNumberValue("A3", 15);
        part.SetBooleanValue("B1", true);
        part.SetBooleanValue("B2", false);
        part.SetBooleanValue("B3", true);
        XlsxFormulaCalculationEngine engine = XlsxFormulaCalculationEngine.CreateForWorksheet(part);

        XlsxFormulaCalculationResult sequence = engine.EvaluateFormula("SUM(SEQUENCE(3,2,1,1))+INDEX(TRANSPOSE(SEQUENCE(2,2,1,1)),1,2)");
        XlsxFormulaCalculationResult filter = engine.EvaluateFormula("SUM(FILTER(A1:A3,B1:B3))");

        Assert.That(sequence.IsValid, Is.True);
        Assert.That(sequence.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(sequence.Value.Number, Is.EqualTo(24));
        Assert.That(filter.IsValid, Is.True);
        Assert.That(filter.Value!.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Number));
        Assert.That(filter.Value.Number, Is.EqualTo(20));
    }

    [Test]
    public void CalculateAllFormulas_WritesPhase2CachedValues()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetFormulaCachedValue("A1", "TEXTJOIN(\"/\",TRUE,\"x\",\"y\")", "", "");
        part.SetFormulaCachedValue("A2", "IF(2>1,DATE(2024,1,1),0)", "", "");

        XlsxFormulaCalculationResult result = part.CalculateAllFormulas();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CalculatedFormulaCount, Is.EqualTo(2));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("x/y"));
        Assert.That(part.GetCell("A1")!.Type, Is.EqualTo("str"));
        Assert.That(part.GetCell("A2")!.Value, Is.EqualTo("45292"));
        Assert.That(part.GetCell("A2")!.Type, Is.EqualTo(""));
    }
}
