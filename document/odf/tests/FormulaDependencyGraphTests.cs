using System.Collections.Generic;

namespace odf.Tests;

public class FormulaDependencyGraphTests
{
    [Test]
    public void ExtractsAndDeduplicatesCellRangeNameAndExternalDependencies()
    {
        OdfFormula formula = FormulaParser.Parse(
            "=SUM([.A1];[.$A$1];[$Sheet2.$B$2:.$C$3];Named;"
            + "['book.ods'#Sheet3.D4])",
            32);

        OdfFormulaDependency[] dependencies =
            FormulaDependencyExtractor.Extract(formula, "Sheet1");

        Assert.Multiple(() =>
        {
            Assert.That(dependencies, Has.Length.EqualTo(4));
            Assert.That(dependencies.Count(item =>
                item.Key == "Sheet1.A1"), Is.EqualTo(1));
            Assert.That(dependencies.Any(item =>
                item.Key == "Sheet1.$$Named"), Is.True);
            Assert.That(dependencies.Any(item =>
                item.IsRange && item.Reference!.Start.SheetName == "Sheet2"),
                Is.True);
            Assert.That(dependencies.Any(item => item.IsExternal), Is.True);
            Assert.That(formula.Dependencies, Is.SameAs(dependencies));
        });
    }

    [Test]
    public void ExpandsRangeDependenciesAgainstKnownGraphCells()
    {
        FormulaCalculationNode first = ConstantNode("Sheet1.A1", 1);
        FormulaCalculationNode second = ConstantNode("Sheet1.A2", 2);
        FormulaCalculationNode third = ConstantNode("Sheet1.A3", 3);
        FormulaCalculationNode other = ConstantNode("Sheet2.A2", 20);
        FormulaCalculationNode sum = FormulaNode(
            "Sheet1.B1",
            "Sheet1",
            "=SUM([.A1:.A3])");
        FormulaCalculationNode middleSheet = ConstantNode("Sheet2.A1", 4);
        FormulaCalculationNode lastSheet = ConstantNode("Sheet3.A1", 5);
        FormulaCalculationNode threeDimensional = FormulaNode(
            "Sheet3.B1",
            "Sheet3",
            "=SUM([Sheet1.A1:Sheet3.A1])");

        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [
                first,
                second,
                third,
                other,
                sum,
                middleSheet,
                lastSheet,
                threeDimensional
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(sum.DependencyKeys,
                Is.EqualTo(new[] { "Sheet1.A1", "Sheet1.A2", "Sheet1.A3" }));
            Assert.That(threeDimensional.DependencyKeys,
                Is.EqualTo(new[] { "Sheet1.A1", "Sheet2.A1", "Sheet3.A1" }));
        });
    }

    [Test]
    public void CalculatesTopologicallyAndPropagatesDirtyState()
    {
        FormulaCalculationNode first = ConstantNode("Sheet1.A1", 2);
        FormulaCalculationNode second = FormulaNode(
            "Sheet1.B1",
            "Sheet1",
            "=[.A1]+1");
        FormulaCalculationNode third = FormulaNode(
            "Sheet1.C1",
            "Sheet1",
            "=[.B1]*2");
        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [third, first, second]);
        TestFormulaCalculationContext context =
            new TestFormulaCalculationContext();
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();

        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(second.Result!.NumericValue, Is.EqualTo(3));
            Assert.That(third.Result!.NumericValue, Is.EqualTo(6));
            Assert.That(context.EvaluatedKeys,
                Is.EqualTo(new[] { "Sheet1.B1", "Sheet1.C1" }));
        });

        context.EvaluatedKeys.Clear();
        first.Result = FormulaValueModel.CreateNumber(10);
        second.Dirty = true;
        FormulaDependencyGraph.MarkDirty(graph, "Sheet1.A1");

        Assert.Multiple(() =>
        {
            Assert.That(first.Dirty, Is.True);
            Assert.That(second.Dirty, Is.True);
            Assert.That(third.Dirty, Is.True);
        });

        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(second.Result!.NumericValue, Is.EqualTo(11));
            Assert.That(third.Result!.NumericValue, Is.EqualTo(22));
            Assert.That(first.Dirty, Is.False);
            Assert.That(second.Dirty, Is.False);
            Assert.That(third.Dirty, Is.False);
        });
    }

    [Test]
    public void MarksOnlyActualCycleMembersAndPropagatesCircularErrors()
    {
        FormulaCalculationNode first = FormulaNode(
            "Sheet1.A1",
            "Sheet1",
            "=[.B1]+1");
        FormulaCalculationNode second = FormulaNode(
            "Sheet1.B1",
            "Sheet1",
            "=[.A1]+1");
        FormulaCalculationNode dependent = FormulaNode(
            "Sheet1.C1",
            "Sheet1",
            "=[.A1]+1");
        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [first, second, dependent]);
        TestFormulaCalculationContext context =
            new TestFormulaCalculationContext();
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();

        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(graph.CircularKeys,
                Is.EquivalentTo(new[] { "Sheet1.A1", "Sheet1.B1" }));
            Assert.That(first.IsCircular, Is.True);
            Assert.That(second.IsCircular, Is.True);
            Assert.That(dependent.IsCircular, Is.False);
            Assert.That(first.Result!.Error,
                Is.EqualTo(OdfFormulaError.CircularReference));
            Assert.That(second.Result!.Error,
                Is.EqualTo(OdfFormulaError.CircularReference));
            Assert.That(dependent.Result!.Error,
                Is.EqualTo(OdfFormulaError.CircularReference));
            Assert.That(context.EvaluatedKeys, Is.Empty);
        });
    }

    [Test]
    public void IteratesCircularCalculationsUntilTheConfiguredConvergence()
    {
        FormulaCalculationNode first = FormulaNode(
            "Sheet1.A1",
            "Sheet1",
            "=([.B1]+1)/2");
        FormulaCalculationNode second = FormulaNode(
            "Sheet1.B1",
            "Sheet1",
            "=([.A1]+1)/2");
        FormulaCalculationNode dependent = FormulaNode(
            "Sheet1.C1",
            "Sheet1",
            "=[.A1]+1");
        FormulaCalculationGraph graph = FormulaDependencyGraph.Build(
            [first, second, dependent]);
        TestFormulaCalculationContext context =
            new TestFormulaCalculationContext();
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();
        settings.IterationEnabled = true;
        settings.IterationSteps = 100;
        settings.MaximumDifference = 0.000000001;

        FormulaRecalculationEngine.Recalculate(graph, context, settings);

        Assert.Multiple(() =>
        {
            Assert.That(first.Result!.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(second.Result!.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(first.Result.NumericValue, Is.EqualTo(1).Within(1e-8));
            Assert.That(second.Result.NumericValue, Is.EqualTo(1).Within(1e-8));
            Assert.That(dependent.Result!.NumericValue,
                Is.EqualTo(2).Within(1e-8));
            Assert.That(context.EvaluatedKeys.Count, Is.LessThan(100));
            Assert.That(first.Dirty, Is.False);
            Assert.That(second.Dirty, Is.False);
            Assert.That(dependent.Dirty, Is.False);
        });
    }

    private static FormulaCalculationNode ConstantNode(
        string key,
        double value)
    {
        FormulaCalculationNode result = new FormulaCalculationNode();
        result.Key = key;
        result.Result = FormulaValueModel.CreateNumber(value);
        return result;
    }

    private static FormulaCalculationNode FormulaNode(
        string key,
        string ownerSheet,
        string formula)
    {
        FormulaCalculationNode result = new FormulaCalculationNode();
        result.Key = key;
        result.OwnerSheet = ownerSheet;
        result.Formula = FormulaParser.Parse(formula, 32);
        return result;
    }
}

internal class TestFormulaCalculationContext : IFormulaCalculationContext
{
    internal List<string> EvaluatedKeys = new List<string>();

    public OdfFormulaResult EvaluateFormula(
        FormulaCalculationNode node,
        FormulaCalculationNode[] dependencies)
    {
        EvaluatedKeys.Add(node.Key);
        double value = 0;
        int index = 0;
        while (index < dependencies.Length)
        {
            value = value + dependencies[index].Result!.NumericValue;
            index = index + 1;
        }
        string source = node.Formula == null ? "" : node.Formula.Source;
        if (source.Contains("/2", global::System.StringComparison.Ordinal))
        {
            return FormulaValueModel.CreateNumber((value + 1) / 2);
        }
        if (source.Contains("*2", global::System.StringComparison.Ordinal))
        {
            return FormulaValueModel.CreateNumber(value * 2);
        }
        return FormulaValueModel.CreateNumber(value + 1);
    }
}
