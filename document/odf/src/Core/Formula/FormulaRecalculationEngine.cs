namespace odf;

internal static class FormulaRecalculationEngine
{
    internal static void Recalculate(
        FormulaCalculationGraph graph,
        IFormulaCalculationContext context,
        OdfSpreadsheetSettings settings)
    {
        if (graph == null || context == null || settings == null)
        {
            throw new OdfFormulaException(
                "A graph, calculation context, and settings are required.");
        }
        FormulaCalculationNode[] schedule =
            FormulaCalculationScheduler.CreateSchedule(graph);
        int index = 0;
        while (index < schedule.Length
            && !schedule[index].IsCircular)
        {
            FormulaCalculationNode prerequisite = schedule[index];
            if (prerequisite.Dirty)
            {
                CalculateNode(graph, prerequisite, context, settings);
            }
            index = index + 1;
        }
        if (settings.IterationEnabled)
        {
            RecalculateCircularNodes(graph, schedule, context, settings);
        }
        else
        {
            SetCircularErrors(graph, settings);
        }
        index = 0;
        while (index < schedule.Length)
        {
            FormulaCalculationNode node = schedule[index];
            if (!node.IsCircular && node.Dirty)
            {
                CalculateNode(graph, node, context, settings);
            }
            index = index + 1;
        }
    }

    static void SetCircularErrors(
        FormulaCalculationGraph graph,
        OdfSpreadsheetSettings settings)
    {
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            FormulaCalculationNode node = graph.Nodes[index];
            if (node.IsCircular && node.Dirty)
            {
                AssignResult(
                    node,
                    FormulaValueModel.CreateError(
                        OdfFormulaError.CircularReference),
                    settings);
                node.Dirty = false;
            }
            index = index + 1;
        }
    }

    static void RecalculateCircularNodes(
        FormulaCalculationGraph graph,
        FormulaCalculationNode[] schedule,
        IFormulaCalculationContext context,
        OdfSpreadsheetSettings settings)
    {
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            FormulaCalculationNode node = graph.Nodes[index];
            if (node.IsCircular
                && (node.Result == null
                    || node.Result.Error
                        == OdfFormulaError.CircularReference))
            {
                node.Result = FormulaValueModel.CreateNumber(0);
            }
            index = index + 1;
        }
        long steps = settings.IterationSteps > 0
            ? settings.IterationSteps
            : 1;
        long step = 0;
        bool converged = false;
        while (step < steps && !converged)
        {
            converged = true;
            index = 0;
            while (index < schedule.Length)
            {
                FormulaCalculationNode node = schedule[index];
                if (node.IsCircular && node.Dirty)
                {
                    OdfFormulaResult previous = node.Result
                        ?? FormulaValueModel.CreateNumber(0);
                    OdfFormulaResult next = EvaluateNode(
                        graph,
                        node,
                        context);
                    AssignResult(node, next, settings);
                    if (!ResultsConverged(
                        previous,
                        next,
                        settings.MaximumDifference))
                    {
                        converged = false;
                    }
                }
                index = index + 1;
            }
            step = step + 1;
        }
        index = 0;
        while (index < graph.Nodes.Length)
        {
            if (graph.Nodes[index].IsCircular)
            {
                graph.Nodes[index].Dirty = false;
            }
            index = index + 1;
        }
    }

    static void CalculateNode(
        FormulaCalculationGraph graph,
        FormulaCalculationNode node,
        IFormulaCalculationContext context,
        OdfSpreadsheetSettings settings)
    {
        if (node.Formula != null)
        {
            AssignResult(
                node,
                EvaluateNode(graph, node, context),
                settings);
        }
        node.Dirty = false;
    }

    static OdfFormulaResult EvaluateNode(
        FormulaCalculationGraph graph,
        FormulaCalculationNode node,
        IFormulaCalculationContext context)
    {
        FormulaCalculationNode[] dependencies = DependenciesFor(graph, node);
        int index = 0;
        while (index < dependencies.Length)
        {
            OdfFormulaResult? dependencyResult = dependencies[index].Result;
            if (dependencyResult == null)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.NotAvailable);
            }
            if (dependencyResult.Error != OdfFormulaError.None)
            {
                return FormulaValueModel.CreateError(dependencyResult.Error);
            }
            index = index + 1;
        }
        OdfFormulaResult result = context.EvaluateFormula(node, dependencies);
        return result == null
            ? FormulaValueModel.CreateError(OdfFormulaError.Value)
            : result;
    }

    static FormulaCalculationNode[] DependenciesFor(
        FormulaCalculationGraph graph,
        FormulaCalculationNode node)
    {
        FormulaCalculationNode[] result =
            new FormulaCalculationNode[node.DependencyKeys.Length];
        int count = 0;
        int index = 0;
        while (index < node.DependencyKeys.Length)
        {
            FormulaCalculationNode? dependency = FormulaDependencyGraph.FindNode(
                graph,
                node.DependencyKeys[index]);
            if (dependency != null)
            {
                result[count] = dependency;
                count = count + 1;
            }
            index = index + 1;
        }
        if (count == result.Length)
        {
            return result;
        }
        FormulaCalculationNode[] trimmed = new FormulaCalculationNode[count];
        index = 0;
        while (index < count)
        {
            trimmed[index] = result[index];
            index = index + 1;
        }
        return trimmed;
    }

    static bool ResultsConverged(
        OdfFormulaResult left,
        OdfFormulaResult right,
        double maximumDifference)
    {
        if (left.Error != right.Error || left.Kind != right.Kind)
        {
            return false;
        }
        if (left.Kind == OdfValueKind.Array
            || left.Kind == OdfValueKind.Reference)
        {
            if (left.RowCount != right.RowCount
                || left.ColumnCount != right.ColumnCount
                || left.Values.Length != right.Values.Length)
            {
                return false;
            }
            int index = 0;
            while (index < left.Values.Length)
            {
                if (!ResultsConverged(
                    left.Values[index],
                    right.Values[index],
                    maximumDifference))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        if (left.Kind == OdfValueKind.Float
            || left.Kind == OdfValueKind.Decimal
            || left.Kind == OdfValueKind.Percentage
            || left.Kind == OdfValueKind.Currency
            || left.Kind == OdfValueKind.Date
            || left.Kind == OdfValueKind.DateTime
            || left.Kind == OdfValueKind.Time
            || left.Kind == OdfValueKind.Duration)
        {
            double difference = global::System.Math.Abs(
                left.NumericValue - right.NumericValue);
            return difference <= maximumDifference;
        }
        if (left.Kind == OdfValueKind.String)
        {
            return left.StringValue == right.StringValue;
        }
        if (left.Kind == OdfValueKind.Boolean)
        {
            return left.BooleanValue == right.BooleanValue;
        }
        return left.Error == right.Error;
    }

    static void AssignResult(
        FormulaCalculationNode node,
        OdfFormulaResult result,
        OdfSpreadsheetSettings settings)
    {
        node.Result = result;
        if (node.Formula != null)
        {
            node.Formula.Result = result;
        }
        FormulaCachedValueUpdater.Update(node, result, settings);
    }
}
