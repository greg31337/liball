namespace odf.Tests;

public class OdfFormulaApiTests
{
    [Test]
    public void ParserApiParsesAndFormatsPreservedAndCanonicalFormulaText()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        string source = "of:=  sum ( 1 ; 2 * ( 3 + 4 ) )  ";

        OdfFormula formula = parser.ParseFormula(source, 32);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(
                parser.FormatFormulaPreserved(formula),
                Is.EqualTo(source));
            Assert.That(
                parser.FormatFormulaCanonical(formula),
                Is.EqualTo("of:=SUM(1;2*(3+4))"));
        });
    }

    [Test]
    public void ParserApiRebasesCopiedMovedAndStructurallyEditedReferences()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();

        string copied = parser.RebaseFormulaCopy(
            "of:=[.A1]+[.$B2]+[.C$3]",
            "Sheet1",
            2,
            1);
        string moved = parser.RebaseFormulaMovedRange(
            "of:=[.A1]+[.B2]",
            "Sheet1",
            "Sheet1",
            0,
            0,
            2,
            2,
            2,
            3);
        string rows = parser.RebaseFormulaRows(
            "of:=[.A1:.C3]",
            "Sheet1",
            "Sheet1",
            1,
            0,
            2);
        string columns = parser.RebaseFormulaColumns(
            rows,
            "Sheet1",
            "Sheet1",
            1,
            0,
            1);
        string renamed = parser.RebaseFormulaSheetRename(
            "of:=['Old Sheet'.A1]",
            "Calc",
            "Old Sheet",
            "New Sheet");
        string deleted = parser.RebaseFormulaSheetDeletion(
            renamed,
            "Calc",
            "New Sheet");
        string stable = "of:=[First.A1:Third.B2]";
        string inserted = parser.PreserveFormulaSheetInsertion(
            stable,
            "Second",
            1);
        string reordered = parser.PreserveFormulaSheetReorder(
            inserted,
            "Third",
            0);

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.EqualTo("of:=[.B3]+[.$B4]+[.D$3]"));
            Assert.That(moved, Is.EqualTo("of:=[.D3]+[.E4]"));
            Assert.That(rows, Is.EqualTo("of:=[.A1:.C5]"));
            Assert.That(columns, Is.EqualTo("of:=[.A1:.D5]"));
            Assert.That(renamed, Is.EqualTo("of:=['New Sheet'.A1]"));
            Assert.That(deleted, Is.EqualTo("of:=#REF!"));
            Assert.That(reordered, Is.SameAs(stable));
        });
    }

    [Test]
    public void EvaluatorApiUsesCallerSuppliedExternalAndVolatileValues()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormulaEvaluationData data = new OdfFormulaEvaluationData();
        data.RandomValue = 0.25;
        data.ExternalFunctionNames = ["DDE"];
        data.ExternalFunctionResults = [Number(7)];

        OdfFormulaResult result = evaluator.EvaluateFormula(
            parser.ParseFormula("=RAND()+DDE(\"s\";\"t\";\"i\")", 32),
            data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(result.NumericValue, Is.EqualTo(7.25));
        });
    }

    [Test]
    public void EvaluatorApiCombinesReferencesRangesOperatorsAndFunctions()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormulaEvaluationData data = new OdfFormulaEvaluationData();
        data.OwnerSheet = "Sheet1";
        data.ReferenceKeys = ["Sheet1.A1", "Sheet1.A2"];
        data.ReferenceValues = [Number(2), Number(3)];
        OdfFormula formula = parser.ParseFormula(
            "=SUM([.A1:.A2])+ROUND(2.5;0)",
            32);

        OdfFormulaResult result = evaluator.EvaluateFormula(formula, data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(result.NumericValue, Is.EqualTo(8));
            Assert.That(formula.Result, Is.SameAs(result));
        });
    }

    [Test]
    public void EvaluatorApiUsesLazyBranchesAndReportsUnknownFunctions()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormulaResult selected = evaluator.EvaluateFormula(
            parser.ParseFormula("=IF(FALSE();1/0;42)", 32),
            new OdfFormulaEvaluationData());
        OdfFormulaResult unsupported = evaluator.EvaluateFormula(
            parser.ParseFormula("=VENDOR.MISSING(1)", 32),
            new OdfFormulaEvaluationData());

        Assert.Multiple(() =>
        {
            Assert.That(selected.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(selected.NumericValue, Is.EqualTo(42));
            Assert.That(
                unsupported.Error,
                Is.EqualTo(OdfFormulaError.UnsupportedFunction));
        });
    }

    [Test]
    public void EvaluatorApiRecalculatesChangedDependenciesAndCachedValues()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfCell cachedCell = new OdfCell();
        OdfFormulaCalculationCell input = CalculationCell(
            "Sheet1.A1",
            null,
            Number(2),
            null);
        OdfFormulaCalculationCell intermediate = CalculationCell(
            "Sheet1.B1",
            parser.ParseFormula("=[.A1]+1", 32),
            null,
            null);
        OdfFormulaCalculationCell output = CalculationCell(
            "Sheet1.C1",
            parser.ParseFormula("=[.B1]*2", 32),
            null,
            cachedCell);
        OdfFormulaCalculationData data = new OdfFormulaCalculationData();
        data.Cells = [input, intermediate, output];

        evaluator.RecalculateFormulas(data);

        Assert.Multiple(() =>
        {
            Assert.That(intermediate.Result!.NumericValue, Is.EqualTo(3));
            Assert.That(output.Result!.NumericValue, Is.EqualTo(6));
            Assert.That(cachedCell.Value.NumericValue, Is.EqualTo(6));
            Assert.That(cachedCell.Value.IsCachedFormulaResult, Is.True);
            Assert.That(data.ScheduledKeys, Is.EquivalentTo(
                new[] { "Sheet1.B1", "Sheet1.C1" }));
        });

        input.Result = Number(10);
        data.ChangedKeys = ["Sheet1.A1"];
        evaluator.RecalculateFormulas(data);

        Assert.Multiple(() =>
        {
            Assert.That(intermediate.Result!.NumericValue, Is.EqualTo(11));
            Assert.That(output.Result!.NumericValue, Is.EqualTo(22));
            Assert.That(cachedCell.Value.NumericValue, Is.EqualTo(22));
            Assert.That(data.ScheduledKeys, Is.EquivalentTo(
                new[] { "Sheet1.A1", "Sheet1.B1", "Sheet1.C1" }));
        });
    }

    static OdfFormulaCalculationCell CalculationCell(
        string key,
        OdfFormula? formula,
        OdfFormulaResult? result,
        OdfCell? cell)
    {
        OdfFormulaCalculationCell calculationCell =
            new OdfFormulaCalculationCell();
        calculationCell.Key = key;
        calculationCell.OwnerSheet = "Sheet1";
        calculationCell.Formula = formula;
        calculationCell.Result = result;
        calculationCell.Cell = cell;
        return calculationCell;
    }

    static OdfFormulaResult Number(double value)
    {
        OdfFormulaResult result = new OdfFormulaResult();
        result.Kind = OdfValueKind.Float;
        result.NumericValue = value;
        result.IsValid = true;
        return result;
    }
}
