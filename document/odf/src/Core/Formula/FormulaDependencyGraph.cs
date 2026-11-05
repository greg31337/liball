using System.Collections.Generic;

namespace odf;

internal class FormulaCalculationNode
{
    internal string Key = "";
    internal string OwnerSheet = "";
    internal OdfFormula? Formula;
    internal OdfFormulaResult? Result;
    internal string[] DependencyKeys = [];
    internal string[] DependentKeys = [];
    internal OdfCell? Cell;
    internal bool Dirty;
    internal bool IsCircular;
    internal bool IsVolatile;
}

internal class FormulaCalculationGraph
{
    internal FormulaCalculationNode[] Nodes = [];
    internal FormulaCalculationNode[] CalculationOrder = [];
    internal FormulaCalculationNode[] ScheduledNodes = [];
    internal string[] CircularKeys = [];
    internal bool RecalculateAll;
}

internal interface IFormulaCalculationContext
{
    OdfFormulaResult EvaluateFormula(
        FormulaCalculationNode node,
        FormulaCalculationNode[] dependencies);
}

internal static class FormulaDependencyGraph
{
    internal static FormulaCalculationGraph Build(
        FormulaCalculationNode[] nodes)
    {
        FormulaCalculationGraph result = new FormulaCalculationGraph();
        if (nodes == null)
        {
            return result;
        }
        result.Nodes = CopyNodes(nodes);
        PopulateDependencies(result);
        PopulateDependents(result);
        MarkCircularNodes(result);
        result.CalculationOrder = CreateCalculationOrder(result);
        result.CircularKeys = CollectCircularKeys(result);
        return result;
    }

    internal static void MarkDirty(
        FormulaCalculationGraph graph,
        string key)
    {
        if (graph == null)
        {
            return;
        }
        FormulaCalculationNode? start = FindNode(graph, key);
        if (start == null)
        {
            return;
        }
        List<FormulaCalculationNode> pending =
            new List<FormulaCalculationNode>();
        pending.Add(start);
        int pendingIndex = 0;
        while (pendingIndex < pending.Count)
        {
            FormulaCalculationNode current = pending[pendingIndex];
            current.Dirty = true;
            int index = 0;
            while (index < current.DependentKeys.Length)
            {
                FormulaCalculationNode? dependent = FindNode(
                    graph,
                    current.DependentKeys[index]);
                if (dependent != null && !PendingContains(pending, dependent))
                {
                    pending.Add(dependent);
                }
                index = index + 1;
            }
            pendingIndex = pendingIndex + 1;
        }
    }

    internal static FormulaCalculationNode? FindNode(
        FormulaCalculationGraph graph,
        string key)
    {
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            if (graph.Nodes[index].Key == key)
            {
                return graph.Nodes[index];
            }
            index = index + 1;
        }
        return null;
    }

    static FormulaCalculationNode[] CopyNodes(
        FormulaCalculationNode[] source)
    {
        FormulaCalculationNode[] result =
            new FormulaCalculationNode[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }

    static void PopulateDependencies(FormulaCalculationGraph graph)
    {
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            FormulaCalculationNode node = graph.Nodes[index];
            List<string> keys = new List<string>();
            node.IsVolatile = false;
            if (node.Formula != null)
            {
                node.Dirty = true;
                node.IsVolatile = ContainsVolatileFunction(
                    node.Formula.Root);
                OdfFormulaDependency[] dependencies =
                    FormulaDependencyExtractor.Extract(
                        node.Formula,
                        node.OwnerSheet);
                int dependencyIndex = 0;
                while (dependencyIndex < dependencies.Length)
                {
                    AddMatchingKeys(graph, dependencies[dependencyIndex], keys);
                    dependencyIndex = dependencyIndex + 1;
                }
            }
            node.DependencyKeys = keys.ToArray();
            index = index + 1;
        }
    }

    static bool ContainsVolatileFunction(OdfFormulaNode? node)
    {
        if (node == null)
        {
            return false;
        }
        if (node.Kind == OdfFormulaNodeKind.FunctionCall
            && node.Function != null
            && node.Function.IsVolatile)
        {
            return true;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            if (ContainsVolatileFunction(node.Children[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void AddMatchingKeys(
        FormulaCalculationGraph graph,
        OdfFormulaDependency dependency,
        List<string> keys)
    {
        if (dependency.IsExternal)
        {
            return;
        }
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            FormulaCalculationNode candidate = graph.Nodes[index];
            bool matches = dependency.Key.Length > 0
                ? candidate.Key == dependency.Key
                : DependencyContainsKey(graph, dependency, candidate.Key);
            if (matches && !ContainsKey(keys, candidate.Key))
            {
                keys.Add(candidate.Key);
            }
            index = index + 1;
        }
    }

    static bool DependencyContainsKey(
        FormulaCalculationGraph graph,
        OdfFormulaDependency dependency,
        string key)
    {
        OdfFormulaReference? reference = dependency.Reference;
        if (reference == null || !reference.HasEnd)
        {
            return false;
        }
        OdfCellAddressValue address =
            OdfCellAddressParser.ParseCellAddress(key);
        if (!address.IsValid)
        {
            return false;
        }
        string startSheet = reference.Start.EffectiveSheetName;
        if (startSheet.Length == 0)
        {
            startSheet = dependency.OwnerSheet;
        }
        string endSheet = reference.End.EffectiveSheetName.Length > 0
            ? reference.End.EffectiveSheetName
            : startSheet;
        if (startSheet.Length > 0
            && !SheetIsInSpan(
                graph,
                address.SheetName,
                startSheet,
                endSheet))
        {
            return false;
        }
        return AxisContains(
                address.ColumnIndex,
                reference.Start.ColumnNumber,
                reference.End.ColumnNumber,
                reference.Start.HasColumn)
            && AxisContains(
                address.Row,
                reference.Start.RowNumber,
                reference.End.RowNumber,
                reference.Start.HasRow);
    }

    static bool SheetIsInSpan(
        FormulaCalculationGraph graph,
        string candidate,
        string first,
        string second)
    {
        if (first == second)
        {
            return candidate == first;
        }
        int firstIndex = FindSheetIndex(graph, first);
        int secondIndex = FindSheetIndex(graph, second);
        int candidateIndex = FindSheetIndex(graph, candidate);
        if (firstIndex < 0 || secondIndex < 0 || candidateIndex < 0)
        {
            return candidate == first || candidate == second;
        }
        int low = firstIndex < secondIndex ? firstIndex : secondIndex;
        int high = firstIndex > secondIndex ? firstIndex : secondIndex;
        return candidateIndex >= low && candidateIndex <= high;
    }

    static int FindSheetIndex(
        FormulaCalculationGraph graph,
        string sheet)
    {
        List<string> sheets = new List<string>();
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            OdfCellAddressValue address =
                OdfCellAddressParser.ParseCellAddress(graph.Nodes[index].Key);
            if (address.IsValid
                && !ContainsKey(sheets, address.SheetName))
            {
                if (address.SheetName == sheet)
                {
                    return sheets.Count;
                }
                sheets.Add(address.SheetName);
            }
            index = index + 1;
        }
        return -1;
    }

    static bool AxisContains(
        global::System.Numerics.BigInteger value,
        global::System.Numerics.BigInteger first,
        global::System.Numerics.BigInteger second,
        bool present)
    {
        if (!present)
        {
            return true;
        }
        global::System.Numerics.BigInteger low = first < second
            ? first
            : second;
        global::System.Numerics.BigInteger high = first > second
            ? first
            : second;
        return value >= low && value <= high;
    }

    static void PopulateDependents(FormulaCalculationGraph graph)
    {
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            List<string> dependents = new List<string>();
            int candidateIndex = 0;
            while (candidateIndex < graph.Nodes.Length)
            {
                FormulaCalculationNode candidate = graph.Nodes[candidateIndex];
                if (ArrayContains(candidate.DependencyKeys, graph.Nodes[index].Key))
                {
                    dependents.Add(candidate.Key);
                }
                candidateIndex = candidateIndex + 1;
            }
            graph.Nodes[index].DependentKeys = dependents.ToArray();
            index = index + 1;
        }
    }

    static void MarkCircularNodes(FormulaCalculationGraph graph)
    {
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            bool[] visited = new bool[graph.Nodes.Length];
            graph.Nodes[index].IsCircular = ReachesNode(
                graph,
                index,
                index,
                visited);
            index = index + 1;
        }
    }

    static bool ReachesNode(
        FormulaCalculationGraph graph,
        int targetIndex,
        int currentIndex,
        bool[] visited)
    {
        if (visited[currentIndex])
        {
            return false;
        }
        visited[currentIndex] = true;
        FormulaCalculationNode current = graph.Nodes[currentIndex];
        int index = 0;
        while (index < current.DependencyKeys.Length)
        {
            int dependencyIndex = FindNodeIndex(
                graph,
                current.DependencyKeys[index]);
            if (dependencyIndex == targetIndex)
            {
                return true;
            }
            if (dependencyIndex >= 0
                && ReachesNode(
                    graph,
                    targetIndex,
                    dependencyIndex,
                    visited))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static FormulaCalculationNode[] CreateCalculationOrder(
        FormulaCalculationGraph graph)
    {
        List<FormulaCalculationNode> ordered =
            new List<FormulaCalculationNode>();
        bool[] added = new bool[graph.Nodes.Length];
        int index = 0;
        bool progress = true;
        while (progress)
        {
            progress = false;
            index = 0;
            while (index < graph.Nodes.Length)
            {
                if (!added[index]
                    && !graph.Nodes[index].IsCircular
                    && DependenciesExcludeUnorderedCycles(
                        graph,
                        graph.Nodes[index],
                        added)
                    && DependenciesAreOrdered(graph, graph.Nodes[index], added))
                {
                    ordered.Add(graph.Nodes[index]);
                    added[index] = true;
                    progress = true;
                }
                index = index + 1;
            }
        }
        index = 0;
        while (index < graph.Nodes.Length)
        {
            if (graph.Nodes[index].IsCircular)
            {
                ordered.Add(graph.Nodes[index]);
                added[index] = true;
            }
            index = index + 1;
        }
        progress = true;
        while (ordered.Count < graph.Nodes.Length && progress)
        {
            progress = false;
            index = 0;
            while (index < graph.Nodes.Length)
            {
                if (!added[index]
                    && DependenciesAreOrdered(graph, graph.Nodes[index], added))
                {
                    ordered.Add(graph.Nodes[index]);
                    added[index] = true;
                    progress = true;
                }
                index = index + 1;
            }
        }
        index = 0;
        while (index < graph.Nodes.Length)
        {
            if (!added[index])
            {
                ordered.Add(graph.Nodes[index]);
            }
            index = index + 1;
        }
        return ordered.ToArray();
    }

    static bool DependenciesExcludeUnorderedCycles(
        FormulaCalculationGraph graph,
        FormulaCalculationNode node,
        bool[] added)
    {
        int index = 0;
        while (index < node.DependencyKeys.Length)
        {
            int dependency = FindNodeIndex(graph, node.DependencyKeys[index]);
            if (dependency >= 0
                && graph.Nodes[dependency].IsCircular
                && !added[dependency])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool DependenciesAreOrdered(
        FormulaCalculationGraph graph,
        FormulaCalculationNode node,
        bool[] added)
    {
        int index = 0;
        while (index < node.DependencyKeys.Length)
        {
            int dependency = FindNodeIndex(graph, node.DependencyKeys[index]);
            if (dependency >= 0 && !added[dependency])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static string[] CollectCircularKeys(FormulaCalculationGraph graph)
    {
        List<string> keys = new List<string>();
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            if (graph.Nodes[index].IsCircular)
            {
                keys.Add(graph.Nodes[index].Key);
            }
            index = index + 1;
        }
        return keys.ToArray();
    }

    static int FindNodeIndex(
        FormulaCalculationGraph graph,
        string key)
    {
        int index = 0;
        while (index < graph.Nodes.Length)
        {
            if (graph.Nodes[index].Key == key)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool ContainsKey(List<string> keys, string key)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (keys[index] == key)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ArrayContains(string[] values, string value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool PendingContains(
        List<FormulaCalculationNode> nodes,
        FormulaCalculationNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == candidate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
