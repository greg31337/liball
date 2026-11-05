using System.Collections.Generic;

namespace odf;

internal static class FormulaCalculationScheduler
{
    internal static FormulaCalculationNode[] CreateSchedule(
        FormulaCalculationGraph graph)
    {
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            FormulaCalculationNode node = graph.Nodes[index];
            if (node.Formula != null
                && (graph.RecalculateAll
                    || node.IsVolatile
                    || node.Formula.ForcedRecalculation))
            {
                node.Dirty = true;
            }
            index = index + 1;
        }
        index = 0;
        while (index < graph.Nodes.Length)
        {
            if (graph.Nodes[index].Dirty)
            {
                FormulaDependencyGraph.MarkDirty(
                    graph,
                    graph.Nodes[index].Key);
            }
            index = index + 1;
        }
        List<FormulaCalculationNode> scheduled =
            new List<FormulaCalculationNode>();
        index = 0;
        while (index < graph.CalculationOrder.Length)
        {
            if (graph.CalculationOrder[index].Dirty)
            {
                scheduled.Add(graph.CalculationOrder[index]);
            }
            index = index + 1;
        }
        graph.RecalculateAll = false;
        graph.ScheduledNodes = scheduled.ToArray();
        return graph.ScheduledNodes;
    }
}
