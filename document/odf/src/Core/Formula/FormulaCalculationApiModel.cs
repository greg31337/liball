namespace odf;

internal class FormulaPublicCalculationContext : IFormulaCalculationContext
{
    internal OdfFormulaEvaluationData Data = new OdfFormulaEvaluationData();
    internal FormulaCalculationGraph Graph = new FormulaCalculationGraph();

    public OdfFormulaResult EvaluateFormula(
        FormulaCalculationNode node,
        FormulaCalculationNode[] dependencies)
    {
        if (node.Formula == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        Data.OwnerSheet = node.OwnerSheet;
        Data.CurrentAddress = node.Key;
        Data.CurrentValue = node.Result;
        return FormulaExpressionEvaluator.Evaluate(
            node.Formula,
            Data,
            node.OwnerSheet,
            Graph);
    }
}

internal static class FormulaCalculationApiModel
{
    internal static OdfFormulaCalculationData Recalculate(
        OdfFormulaCalculationData data)
    {
        RequireData(data);
        PrepareFormulas(data);
        FormulaCalculationGraph graph;
        FormulaCalculationGraph? existingGraph = data.CalculationGraph;
        if (existingGraph != null && CanReuseGraph(data))
        {
            graph = existingGraph;
            SynchronizeGraph(data, graph);
        }
        else
        {
            graph = BuildGraph(data);
            data.CalculationGraph = graph;
        }
        graph.RecalculateAll = data.RecalculateAll;
        int index = 0;
        while (index < data.ChangedKeys.Length)
        {
            FormulaDependencyGraph.MarkDirty(
                graph,
                data.ChangedKeys[index]);
            index = index + 1;
        }
        data.EvaluationData.SpreadsheetSettings =
            data.SpreadsheetSettings;
        FormulaPublicCalculationContext context =
            new FormulaPublicCalculationContext();
        context.Data = data.EvaluationData;
        context.Graph = graph;
        FormulaRecalculationEngine.Recalculate(
            graph,
            context,
            data.SpreadsheetSettings);
        CopyResults(data, graph);
        data.RecalculateAll = false;
        data.ChangedKeys = [];
        return data;
    }

    static void RequireData(OdfFormulaCalculationData data)
    {
        if (data == null
            || data.Cells == null
            || data.EvaluationData == null
            || data.SpreadsheetSettings == null)
        {
            throw new OdfFormulaException(
                "Calculation cells, evaluation data, and settings are required.");
        }
        if (data.EvaluationData.MaximumDepth <= 0)
        {
            throw new OdfFormulaException(
                "The formula nesting limit must be positive.");
        }
    }

    static void PrepareFormulas(OdfFormulaCalculationData data)
    {
        int index = 0;
        while (index < data.Cells.Length)
        {
            OdfFormulaCalculationCell cell = data.Cells[index];
            if (cell == null || cell.Key.Length == 0)
            {
                throw new OdfFormulaException(
                    "Every calculation cell requires a unique key.");
            }
            int prior = 0;
            while (prior < index)
            {
                if (data.Cells[prior].Key == cell.Key)
                {
                    throw new OdfFormulaException(
                        "Every calculation cell requires a unique key.");
                }
                prior = prior + 1;
            }
            if (cell.Formula == null
                && cell.Cell != null
                && cell.Cell.Formula.Length > 0)
            {
                cell.Formula = FormulaParser.Parse(
                    cell.Cell.Formula,
                    data.EvaluationData.MaximumDepth);
            }
            index = index + 1;
        }
    }

    static bool CanReuseGraph(OdfFormulaCalculationData data)
    {
        FormulaCalculationGraph? graph = data.CalculationGraph;
        if (graph == null || graph.Nodes.Length != data.Cells.Length)
        {
            return false;
        }
        int index = 0;
        while (index < data.Cells.Length)
        {
            OdfFormulaCalculationCell cell = data.Cells[index];
            if (cell.CalculationNode == null
                || graph.Nodes[index] != cell.CalculationNode
                || cell.CalculationNode.Key != cell.Key
                || cell.CalculationNode.OwnerSheet != cell.OwnerSheet
                || cell.CalculationNode.Formula != cell.Formula)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static FormulaCalculationGraph BuildGraph(
        OdfFormulaCalculationData data)
    {
        FormulaCalculationNode[] nodes =
            new FormulaCalculationNode[data.Cells.Length];
        int index = 0;
        while (index < data.Cells.Length)
        {
            OdfFormulaCalculationCell cell = data.Cells[index];
            FormulaCalculationNode node = new FormulaCalculationNode();
            node.Key = cell.Key;
            node.OwnerSheet = cell.OwnerSheet;
            node.Formula = cell.Formula;
            node.Result = cell.Result;
            node.Cell = cell.Cell;
            node.Dirty = cell.Dirty;
            cell.CalculationNode = node;
            nodes[index] = node;
            index = index + 1;
        }
        return FormulaDependencyGraph.Build(nodes);
    }

    static void SynchronizeGraph(
        OdfFormulaCalculationData data,
        FormulaCalculationGraph graph)
    {
        int index = 0;
        while (index < data.Cells.Length)
        {
            OdfFormulaCalculationCell cell = data.Cells[index];
            FormulaCalculationNode node = graph.Nodes[index];
            node.Cell = cell.Cell;
            if (cell.Result != null && cell.Result != node.Result)
            {
                node.Result = cell.Result;
            }
            if (cell.Dirty)
            {
                FormulaDependencyGraph.MarkDirty(graph, cell.Key);
            }
            index = index + 1;
        }
    }

    static void CopyResults(
        OdfFormulaCalculationData data,
        FormulaCalculationGraph graph)
    {
        data.Results = new OdfFormulaResult[data.Cells.Length];
        int index = 0;
        while (index < data.Cells.Length)
        {
            OdfFormulaResult result = graph.Nodes[index].Result
                ?? FormulaValueModel.CreateError(
                    OdfFormulaError.NotAvailable);
            data.Cells[index].Result = graph.Nodes[index].Result;
            data.Cells[index].Dirty = graph.Nodes[index].Dirty;
            data.Results[index] = result;
            index = index + 1;
        }
        data.ScheduledKeys = new string[graph.ScheduledNodes.Length];
        index = 0;
        while (index < graph.ScheduledNodes.Length)
        {
            data.ScheduledKeys[index] = graph.ScheduledNodes[index].Key;
            index = index + 1;
        }
        data.CircularKeys = new string[graph.CircularKeys.Length];
        global::System.Array.Copy(
            graph.CircularKeys,
            data.CircularKeys,
            graph.CircularKeys.Length);
    }
}
