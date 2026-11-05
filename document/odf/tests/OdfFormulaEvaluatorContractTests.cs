namespace odf.Tests;

public class OdfFormulaEvaluatorContractTests
{
    [Test]
    public void EvaluateFormulaConvertsTypesAndBroadcastsArrays()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormulaEvaluationData data = new OdfFormulaEvaluationData();

        OdfFormulaResult converted = evaluator.EvaluateFormula(
            parser.ParseFormula("=\"2\"+TRUE()", 32),
            data);
        OdfFormulaResult array = evaluator.EvaluateFormula(
            parser.ParseFormula("={1;2|3;4}+10", 32),
            data);

        Assert.Multiple(() =>
        {
            Assert.That(converted.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(converted.Kind, Is.EqualTo(OdfValueKind.Float));
            Assert.That(converted.NumericValue, Is.EqualTo(3));
            Assert.That(array.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(array.Kind, Is.EqualTo(OdfValueKind.Array));
            Assert.That(array.RowCount, Is.EqualTo(2));
            Assert.That(array.ColumnCount, Is.EqualTo(2));
            Assert.That(
                array.Values.Select(value => value.NumericValue),
                Is.EqualTo(new[] { 11, 12, 13, 14 }));
        });
    }

    [Test]
    public void EvaluateFormulaPropagatesScalarAndArrayElementErrors()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormulaEvaluationData data = new OdfFormulaEvaluationData();

        OdfFormulaResult scalar = evaluator.EvaluateFormula(
            parser.ParseFormula("=1/0+5", 32),
            data);
        OdfFormulaResult array = evaluator.EvaluateFormula(
            parser.ParseFormula("={1;#N/A|3;4}+10", 32),
            data);

        Assert.Multiple(() =>
        {
            Assert.That(
                scalar.Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(array.Kind, Is.EqualTo(OdfValueKind.Array));
            Assert.That(array.Values[0].NumericValue, Is.EqualTo(11));
            Assert.That(
                array.Values[1].Error,
                Is.EqualTo(OdfFormulaError.NotAvailable));
            Assert.That(array.Values[2].NumericValue, Is.EqualTo(13));
            Assert.That(array.Values[3].NumericValue, Is.EqualTo(14));
        });
    }

    [Test]
    public void RecalculateFormulasTracksRangeAndNamedDependencies()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormulaCalculationCell first = Cell(
            "Sheet1.A1",
            null,
            Number(2),
            null);
        OdfFormulaCalculationCell second = Cell(
            "Sheet1.A2",
            null,
            Number(3),
            null);
        OdfFormulaCalculationCell named = Cell(
            "Sheet1.$$Tax",
            null,
            Number(4),
            null);
        OdfFormula formula = parser.ParseFormula(
            "=SUM([.A1:.A2])+Tax",
            32);
        OdfFormulaCalculationCell total = Cell(
            "Sheet1.B1",
            formula,
            null,
            null);
        OdfFormulaCalculationCell dependent = Cell(
            "Sheet1.C1",
            parser.ParseFormula("=[.B1]*2", 32),
            null,
            null);
        OdfFormulaCalculationData data = new OdfFormulaCalculationData();
        data.Cells = [dependent, named, second, total, first];

        OdfFormulaCalculationData returned =
            evaluator.RecalculateFormulas(data);

        Assert.Multiple(() =>
        {
            Assert.That(returned, Is.SameAs(data));
            Assert.That(total.Result!.NumericValue, Is.EqualTo(9));
            Assert.That(dependent.Result!.NumericValue, Is.EqualTo(18));
            Assert.That(
                formula.Dependencies.Select(item => item.Key),
                Is.EquivalentTo(new[] { "", "Sheet1.$$Tax" }));
            Assert.That(
                data.ScheduledKeys,
                Is.EquivalentTo(new[] { "Sheet1.B1", "Sheet1.C1" }));
        });

        first.Result = Number(10);
        data.ChangedKeys = ["Sheet1.A1"];
        evaluator.RecalculateFormulas(data);

        Assert.Multiple(() =>
        {
            Assert.That(total.Result!.NumericValue, Is.EqualTo(17));
            Assert.That(dependent.Result!.NumericValue, Is.EqualTo(34));
            Assert.That(
                data.ScheduledKeys,
                Is.EquivalentTo(
                    new[] { "Sheet1.A1", "Sheet1.B1", "Sheet1.C1" }));
            Assert.That(data.ChangedKeys, Is.Empty);
        });
    }

    [Test]
    public void RecalculateFormulasReportsCyclesAndPropagatesTheirErrors()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormulaCalculationCell first = Cell(
            "Sheet1.A1",
            parser.ParseFormula("=[.B1]+1", 32),
            null,
            null);
        OdfFormulaCalculationCell second = Cell(
            "Sheet1.B1",
            parser.ParseFormula("=[.A1]+1", 32),
            null,
            null);
        OdfCell cache = new OdfCell();
        OdfFormulaCalculationCell dependent = Cell(
            "Sheet1.C1",
            parser.ParseFormula("=[.A1]+1", 32),
            null,
            cache);
        OdfFormulaCalculationData data = new OdfFormulaCalculationData();
        data.Cells = [dependent, first, second];

        evaluator.RecalculateFormulas(data);

        Assert.Multiple(() =>
        {
            Assert.That(
                data.CircularKeys,
                Is.EquivalentTo(new[] { "Sheet1.A1", "Sheet1.B1" }));
            Assert.That(
                first.Result!.Error,
                Is.EqualTo(OdfFormulaError.CircularReference));
            Assert.That(
                second.Result!.Error,
                Is.EqualTo(OdfFormulaError.CircularReference));
            Assert.That(
                dependent.Result!.Error,
                Is.EqualTo(OdfFormulaError.CircularReference));
            Assert.That(
                cache.Value.Error,
                Is.EqualTo(OdfFormulaError.CircularReference));
            Assert.That(cache.Value.IsCachedFormulaResult, Is.True);
        });
    }

    [Test]
    public void RecalculateFormulasIteratesCircularReferencesToConvergence()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfFormulaCalculationCell first = Cell(
            "Sheet1.A1",
            parser.ParseFormula("=([.B1]+1)/2", 32),
            null,
            null);
        OdfFormulaCalculationCell second = Cell(
            "Sheet1.B1",
            parser.ParseFormula("=([.A1]+1)/2", 32),
            null,
            null);
        OdfFormulaCalculationCell dependent = Cell(
            "Sheet1.C1",
            parser.ParseFormula("=[.A1]+1", 32),
            null,
            null);
        OdfFormulaCalculationData data = new OdfFormulaCalculationData();
        data.Cells = [first, second, dependent];
        data.SpreadsheetSettings.IterationEnabled = true;
        data.SpreadsheetSettings.IterationSteps = 100;
        data.SpreadsheetSettings.MaximumDifference = 0.000000001;

        evaluator.RecalculateFormulas(data);

        Assert.Multiple(() =>
        {
            Assert.That(data.CircularKeys, Has.Length.EqualTo(2));
            Assert.That(first.Result!.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(second.Result!.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(first.Result.NumericValue, Is.EqualTo(1).Within(1e-8));
            Assert.That(second.Result.NumericValue, Is.EqualTo(1).Within(1e-8));
            Assert.That(
                dependent.Result!.NumericValue,
                Is.EqualTo(2).Within(1e-8));
            Assert.That(first.Dirty, Is.False);
            Assert.That(second.Dirty, Is.False);
            Assert.That(dependent.Dirty, Is.False);
        });
    }

    [Test]
    public void RecalculateFormulasRefreshesVolatileDependentsAndCaches()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        IOdfFormulaEvaluator evaluator = new OdfFormulaEvaluator();
        OdfCell volatileCache = new OdfCell();
        OdfCell dependentCache = new OdfCell();
        OdfFormulaCalculationCell random = Cell(
            "Sheet1.A1",
            parser.ParseFormula("=RAND()", 32),
            null,
            volatileCache);
        OdfFormulaCalculationCell stable = Cell(
            "Sheet1.B1",
            parser.ParseFormula("=40", 32),
            null,
            null);
        OdfFormulaCalculationCell dependent = Cell(
            "Sheet1.C1",
            parser.ParseFormula("=[.A1]+1", 32),
            null,
            dependentCache);
        OdfFormulaCalculationData data = new OdfFormulaCalculationData();
        data.Cells = [dependent, stable, random];
        data.EvaluationData.RandomValue = 0.25;

        evaluator.RecalculateFormulas(data);
        data.EvaluationData.RandomValue = 0.75;
        evaluator.RecalculateFormulas(data);

        Assert.Multiple(() =>
        {
            Assert.That(random.Result!.NumericValue, Is.EqualTo(0.75));
            Assert.That(dependent.Result!.NumericValue, Is.EqualTo(1.75));
            Assert.That(stable.Result!.NumericValue, Is.EqualTo(40));
            Assert.That(
                data.ScheduledKeys,
                Is.EquivalentTo(new[] { "Sheet1.A1", "Sheet1.C1" }));
            Assert.That(volatileCache.Value.NumericValue, Is.EqualTo(0.75));
            Assert.That(dependentCache.Value.NumericValue, Is.EqualTo(1.75));
            Assert.That(volatileCache.Value.IsCachedFormulaResult, Is.True);
            Assert.That(dependentCache.Value.IsCachedFormulaResult, Is.True);
            Assert.That(
                dependentCache.Value.Provenance,
                Is.EqualTo(OdfValueProvenance.Computed));
        });
    }

    static OdfFormulaCalculationCell Cell(
        string key,
        OdfFormula? formula,
        OdfFormulaResult? result,
        OdfCell? cell)
    {
        OdfFormulaCalculationCell value = new OdfFormulaCalculationCell();
        value.Key = key;
        value.OwnerSheet = "Sheet1";
        value.Formula = formula;
        value.Result = result;
        value.Cell = cell;
        return value;
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
