using System.Collections.Generic;

namespace odf.Tests;

public class FormulaCalculationSchedulingTests
{
    [Test]
    public void SchedulesVolatileForcedAndDependentFormulasOnEveryPass()
    {
        FormulaCalculationNode volatileNode = FormulaNode(
            "Sheet1.A1",
            "=IF(TRUE();RAND();0)");
        FormulaCalculationNode ordinary = FormulaNode("Sheet1.B1", "=10");
        FormulaCalculationNode dependent = FormulaNode(
            "Sheet1.C1",
            "=[.A1]+1");
        FormulaCalculationNode forced = FormulaNode("Sheet1.D1", "==20");
        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [dependent, ordinary, forced, volatileNode]);
        SchedulingFormulaContext context = new SchedulingFormulaContext();
        context.Results[volatileNode.Key] = Number(1);
        context.Results[ordinary.Key] = Number(10);
        context.Results[forced.Key] = Number(20);
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();

        FormulaRecalculationEngine.Recalculate(graph, context, settings);
        context.EvaluatedKeys.Clear();
        context.Results[volatileNode.Key] = Number(2);
        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(volatileNode.IsVolatile, Is.True);
            Assert.That(context.EvaluatedKeys,
                Is.EquivalentTo(new[]
                {
                    "Sheet1.A1",
                    "Sheet1.C1",
                    "Sheet1.D1"
                }));
            Assert.That(graph.ScheduledNodes.Select(node => node.Key),
                Is.EquivalentTo(new[]
                {
                    "Sheet1.A1",
                    "Sheet1.C1",
                    "Sheet1.D1"
                }));
            Assert.That(context.EvaluatedKeys.IndexOf("Sheet1.A1"),
                Is.LessThan(context.EvaluatedKeys.IndexOf("Sheet1.C1")));
            Assert.That(dependent.Result!.NumericValue, Is.EqualTo(3));
            Assert.That(ordinary.Result!.NumericValue, Is.EqualTo(10));
        });

        context.EvaluatedKeys.Clear();
        graph.RecalculateAll = true;
        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(context.EvaluatedKeys,
                Is.EquivalentTo(new[]
                {
                    "Sheet1.A1",
                    "Sheet1.B1",
                    "Sheet1.C1",
                    "Sheet1.D1"
                }));
            Assert.That(graph.RecalculateAll, Is.False);
        });
    }

    [Test]
    public void UpdatesCellCachesForCalculatedScalarKinds()
    {
        FormulaCalculationNode number = FormulaNodeWithCell(
            "Sheet1.A1",
            "=12.5");
        FormulaCalculationNode text = FormulaNodeWithCell(
            "Sheet1.A2",
            "=\"hello\"");
        FormulaCalculationNode logical = FormulaNodeWithCell(
            "Sheet1.A3",
            "=TRUE()");
        FormulaCalculationNode date = FormulaNodeWithCell(
            "Sheet1.A4",
            "=NOW()");
        FormulaCalculationNode duration = FormulaNodeWithCell(
            "Sheet1.A5",
            "=TIME(12;0;0)");
        FormulaCalculationNode complex = FormulaNodeWithCell(
            "Sheet1.A6",
            "=COMPLEX(3;4)");
        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [number, text, logical, date, duration, complex]);
        SchedulingFormulaContext context = new SchedulingFormulaContext();
        context.Results[number.Key] = Number(12.5);
        context.Results[text.Key] = FormulaValueModel.CreateText("hello");
        context.Results[logical.Key] = FormulaValueModel.CreateBoolean(true);
        OdfFormulaResult dateResult = Number(1.5);
        dateResult.Kind = OdfValueKind.DateTime;
        context.Results[date.Key] = dateResult;
        OdfFormulaResult timeResult = Number(0.5);
        timeResult.Kind = OdfValueKind.Time;
        context.Results[duration.Key] = timeResult;
        context.Results[complex.Key] = FormulaValueModel.CreateComplex("3+4i");
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();
        settings.NullDate = "2024-01-01";

        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            AssertCache(number, OdfValueKind.Float, "float");
            Assert.That(number.Cell!.Value.NumericValue, Is.EqualTo(12.5));
            AssertCache(text, OdfValueKind.String, "string");
            Assert.That(text.Cell!.Value.StringValue, Is.EqualTo("hello"));
            AssertCache(logical, OdfValueKind.Boolean, "boolean");
            Assert.That(logical.Cell!.Value.BooleanValue, Is.True);
            AssertCache(date, OdfValueKind.DateTime, "date");
            Assert.That(date.Cell!.Value.DateValue, Is.Not.Empty);
            AssertCache(duration, OdfValueKind.Duration, "time");
            Assert.That(duration.Cell!.Value.DurationValue, Is.EqualTo("PT12H"));
            AssertCache(complex, OdfValueKind.String, "string");
            Assert.That(complex.Cell!.Value.StringValue, Is.EqualTo("3+4i"));
        });
    }

    [Test]
    public void PropagatesErrorsWithoutEvaluationAndReplacesCachedErrors()
    {
        FormulaCalculationNode source = FormulaNodeWithCell(
            "Sheet1.A1",
            "=1/0");
        FormulaCalculationNode dependent = FormulaNodeWithCell(
            "Sheet1.B1",
            "=[.A1]+1");
        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [dependent, source]);
        SchedulingFormulaContext context = new SchedulingFormulaContext();
        context.Results[source.Key] = FormulaValueModel.CreateError(
            OdfFormulaError.DivisionByZero);
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();

        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(context.EvaluatedKeys,
                Is.EqualTo(new[] { "Sheet1.A1" }));
            Assert.That(dependent.Result!.Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(dependent.Cell!.Value.Kind,
                Is.EqualTo(OdfValueKind.Error));
            Assert.That(dependent.Cell.Value.Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(dependent.Cell.Value.Provenance,
                Is.EqualTo(OdfValueProvenance.Computed));
        });

        context.EvaluatedKeys.Clear();
        context.Results[source.Key] = Number(4);
        FormulaDependencyGraph.MarkDirty(graph, source.Key);
        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(context.EvaluatedKeys,
                Is.EqualTo(new[] { "Sheet1.A1", "Sheet1.B1" }));
            Assert.That(dependent.Result!.NumericValue, Is.EqualTo(5));
            Assert.That(dependent.Cell!.Value.Kind,
                Is.EqualTo(OdfValueKind.Float));
            Assert.That(dependent.Cell.Value.Error,
                Is.EqualTo(OdfFormulaError.None));
            Assert.That(dependent.Cell.Value.NumericValue, Is.EqualTo(5));
        });
    }

    [Test]
    public void ReportsUnavailableForMissingDependenciesAndVolatileInputs()
    {
        FormulaCalculationNode missing = new FormulaCalculationNode();
        missing.Key = "Sheet1.A1";
        FormulaCalculationNode dependent = FormulaNodeWithCell(
            "Sheet1.B1",
            "=[.A1]+1");
        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [missing, dependent]);
        SchedulingFormulaContext calculation = new SchedulingFormulaContext();
        FormulaRecalculationEngine.Recalculate(
            graph,
            calculation,
            new OdfSpreadsheetSettings());
        FormulaFunctionContext functions = new FormulaFunctionContext();
        OdfFormulaResult missingRandom = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.RAND,
            [],
            functions);
        functions.RandomValue = 0.25;
        OdfFormulaResult firstRandom = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.RAND,
            [],
            functions);
        OdfFormulaResult secondRandom = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.RAND,
            [],
            functions);

        Assert.Multiple(() =>
        {
            Assert.That(calculation.EvaluatedKeys, Is.Empty);
            Assert.That(dependent.Result!.Error,
                Is.EqualTo(OdfFormulaError.NotAvailable));
            Assert.That(dependent.Cell!.Value.Error,
                Is.EqualTo(OdfFormulaError.NotAvailable));
            Assert.That(missingRandom.Error,
                Is.EqualTo(OdfFormulaError.NotAvailable));
            Assert.That(firstRandom.NumericValue, Is.EqualTo(0.25));
            Assert.That(secondRandom.NumericValue, Is.EqualTo(0.25));
        });
    }

    [Test]
    public void RecoversCircularCachesWhenIterationIsLaterEnabled()
    {
        FormulaCalculationNode first = FormulaNodeWithCell(
            "Sheet1.A1",
            "=([.B1]+1)/2");
        FormulaCalculationNode second = FormulaNodeWithCell(
            "Sheet1.B1",
            "=([.A1]+1)/2");
        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [first, second]);
        TestFormulaCalculationContext context =
            new TestFormulaCalculationContext();
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();

        FormulaRecalculationEngine.Recalculate(graph, context, settings);
        settings.IterationEnabled = true;
        settings.IterationSteps = 100;
        settings.MaximumDifference = 0.000000001;
        FormulaDependencyGraph.MarkDirty(graph, first.Key);
        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(first.Result!.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(second.Result!.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(first.Result.NumericValue, Is.EqualTo(1).Within(1E-8));
            Assert.That(second.Result.NumericValue, Is.EqualTo(1).Within(1E-8));
            Assert.That(first.Cell!.Value.Kind,
                Is.EqualTo(OdfValueKind.Float));
            Assert.That(second.Cell!.Value.Kind,
                Is.EqualTo(OdfValueKind.Float));
        });
    }

    static FormulaCalculationNode FormulaNode(string key, string source)
    {
        FormulaCalculationNode result = new FormulaCalculationNode();
        result.Key = key;
        result.OwnerSheet = "Sheet1";
        result.Formula = FormulaParser.Parse(source, 32);
        return result;
    }

    static FormulaCalculationNode FormulaNodeWithCell(
        string key,
        string source)
    {
        FormulaCalculationNode result = FormulaNode(key, source);
        result.Cell = new OdfCell();
        result.Cell.Formula = source;
        return result;
    }

    static OdfFormulaResult Number(double value)
    {
        return FormulaValueModel.CreateNumber(value);
    }

    static void AssertCache(
        FormulaCalculationNode node,
        OdfValueKind kind,
        string valueType)
    {
        Assert.That(node.Cell, Is.Not.Null);
        Assert.That(node.Cell!.Value.Kind, Is.EqualTo(kind));
        Assert.That(node.Cell.Value.ValueType, Is.EqualTo(valueType));
        Assert.That(node.Cell.Value.IsCachedFormulaResult, Is.True);
        Assert.That(node.Cell.Value.IsValid, Is.True);
        Assert.That(node.Cell.Value.Provenance,
            Is.EqualTo(OdfValueProvenance.Computed));
    }
}

internal class SchedulingFormulaContext : IFormulaCalculationContext
{
    internal Dictionary<string, OdfFormulaResult> Results =
        new Dictionary<string, OdfFormulaResult>();
    internal List<string> EvaluatedKeys = new List<string>();

    public OdfFormulaResult EvaluateFormula(
        FormulaCalculationNode node,
        FormulaCalculationNode[] dependencies)
    {
        EvaluatedKeys.Add(node.Key);
        if (Results.ContainsKey(node.Key))
        {
            return Results[node.Key];
        }
        double value = 1;
        int index = 0;
        while (index < dependencies.Length)
        {
            value = value + dependencies[index].Result!.NumericValue;
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(value);
    }
}
