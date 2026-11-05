namespace asn1;

internal class DeterministicIrOrderer
{
    internal SymbolTable[] OrderModules(
        SymbolTable[] tables,
        CompilationGraph graph)
    {
        SymbolTable[] result = new SymbolTable[tables.Length];
        bool[] emitted = new bool[tables.Length];
        int order = 0;
        while (order < result.Length)
        {
            int candidate = this.FindReadyModule(tables, graph, emitted);
            if (candidate < 0)
            {
                candidate = this.FindSmallestModule(tables, emitted);
            }

            result[order] = tables[candidate];
            emitted[candidate] = true;
            order = order + 1;
        }

        return result;
    }

    internal int FindReadyModule(
        SymbolTable[] tables,
        CompilationGraph graph,
        bool[] emitted)
    {
        int selected = -1;
        int index = 0;
        while (index < tables.Length)
        {
            if (!emitted[index]
                && !this.HasPendingModuleDependency(tables[index], tables, graph, emitted)
                && (selected < 0
                    || this.CompareModules(tables[index], tables[selected]) < 0))
            {
                selected = index;
            }

            index = index + 1;
        }

        return selected;
    }

    internal int FindSmallestModule(SymbolTable[] tables, bool[] emitted)
    {
        int selected = -1;
        int index = 0;
        while (index < tables.Length)
        {
            if (!emitted[index]
                && (selected < 0
                    || this.CompareModules(tables[index], tables[selected]) < 0))
            {
                selected = index;
            }

            index = index + 1;
        }

        return selected;
    }

    internal bool HasPendingModuleDependency(
        SymbolTable source,
        SymbolTable[] tables,
        CompilationGraph graph,
        bool[] emitted)
    {
        int edgeIndex = 0;
        while (edgeIndex < graph.ModuleEdges.Length)
        {
            ModuleDependencyEdge edge = graph.ModuleEdges[edgeIndex];
            SymbolTable? edgeSource = edge.Source == null ? null : edge.Source.Module;
            SymbolTable? target = edge.Target == null ? null : edge.Target.Module;
            if (edgeSource == source && target != null && target != source)
            {
                int targetIndex = this.FindModuleIndex(tables, target);
                if (targetIndex >= 0 && !emitted[targetIndex])
                {
                    return true;
                }
            }

            edgeIndex = edgeIndex + 1;
        }

        return false;
    }

    internal int FindModuleIndex(SymbolTable[] tables, SymbolTable target)
    {
        int index = 0;
        while (index < tables.Length)
        {
            if (tables[index] == target)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int CompareModules(SymbolTable left, SymbolTable right)
    {
        int comparison = System.String.CompareOrdinal(left.ModuleName, right.ModuleName);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = System.String.CompareOrdinal(
            left.ObjectIdentifier,
            right.ObjectIdentifier);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = System.String.CompareOrdinal(left.Iri, right.Iri);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = System.String.CompareOrdinal(left.VersionModule, right.VersionModule);
        return comparison != 0
            ? comparison
            : System.String.CompareOrdinal(left.Version, right.Version);
    }

    internal SymbolTableEntry[] OrderAssignments(
        SymbolTable module,
        CompilationGraph graph)
    {
        SymbolTableEntry[] symbols = module.Symbols;
        SymbolTableEntry[] result = new SymbolTableEntry[symbols.Length];
        bool[] emitted = new bool[symbols.Length];
        int order = 0;
        while (order < result.Length)
        {
            int candidate = this.FindReadyAssignment(
                module,
                symbols,
                graph,
                emitted);
            if (candidate < 0)
            {
                candidate = this.FindSmallestAssignment(symbols, emitted);
            }

            result[order] = symbols[candidate];
            emitted[candidate] = true;
            order = order + 1;
        }

        return result;
    }

    internal int FindReadyAssignment(
        SymbolTable module,
        SymbolTableEntry[] symbols,
        CompilationGraph graph,
        bool[] emitted)
    {
        int selected = -1;
        int index = 0;
        while (index < symbols.Length)
        {
            if (!emitted[index]
                && !this.HasPendingAssignmentDependency(
                    module,
                    symbols[index],
                    symbols,
                    graph,
                    emitted)
                && (selected < 0
                    || this.CompareAssignments(symbols[index], symbols[selected]) < 0))
            {
                selected = index;
            }

            index = index + 1;
        }

        return selected;
    }

    internal int FindSmallestAssignment(
        SymbolTableEntry[] symbols,
        bool[] emitted)
    {
        int selected = -1;
        int index = 0;
        while (index < symbols.Length)
        {
            if (!emitted[index]
                && (selected < 0
                    || this.CompareAssignments(symbols[index], symbols[selected]) < 0))
            {
                selected = index;
            }

            index = index + 1;
        }

        return selected;
    }

    internal bool HasPendingAssignmentDependency(
        SymbolTable module,
        SymbolTableEntry source,
        SymbolTableEntry[] symbols,
        CompilationGraph graph,
        bool[] emitted)
    {
        int edgeIndex = 0;
        while (edgeIndex < graph.AssignmentEdges.Length)
        {
            AssignmentDependencyEdge edge = graph.AssignmentEdges[edgeIndex];
            SymbolTableEntry? edgeSource = edge.Source == null
                ? null
                : edge.Source.Symbol;
            SymbolTable? targetModule = edge.Target == null ? null : edge.Target.Module;
            SymbolTableEntry? target = edge.Target == null ? null : edge.Target.Symbol;
            if (edgeSource == source
                && targetModule == module
                && target != null
                && target != source)
            {
                int targetIndex = this.FindAssignmentIndex(symbols, target);
                if (targetIndex >= 0 && !emitted[targetIndex])
                {
                    return true;
                }
            }

            edgeIndex = edgeIndex + 1;
        }

        return false;
    }

    internal int FindAssignmentIndex(
        SymbolTableEntry[] symbols,
        SymbolTableEntry target)
    {
        int index = 0;
        while (index < symbols.Length)
        {
            if (symbols[index] == target)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int CompareAssignments(SymbolTableEntry left, SymbolTableEntry right)
    {
        int comparison = System.String.CompareOrdinal(left.QualifiedName, right.QualifiedName);
        if (comparison != 0)
        {
            return comparison;
        }

        return ((int)left.Kind).CompareTo((int)right.Kind);
    }
}
