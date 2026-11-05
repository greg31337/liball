namespace asn1;

internal class CompilationGraphBuilder
{
    internal DiagnosticBag Diagnostics;

    internal CompilationGraphBuilder(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal CompilationGraph BuildGraph(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        CompilationGraph graph = new CompilationGraph();
        graph.Modules = this.CreateModuleNodes(tables);
        graph.ModuleEdges = this.CreateModuleEdges(graph.Modules);
        graph.ModuleGroups = this.CreateModuleGroups(graph.Modules, graph.ModuleEdges);
        graph.Assignments = this.CreateAssignmentNodes(tables);
        graph.AssignmentEdges = this.CreateAssignmentEdges(graph.Assignments, tables);
        graph.AssignmentGroups = this.CreateAssignmentGroups(
            graph.Assignments,
            graph.AssignmentEdges);
        graph.Success = this.DiagnoseIllegalCycles(graph.AssignmentGroups);
        return graph;
    }

    internal ModuleDependencyNode[] CreateModuleNodes(SymbolTable[] tables)
    {
        ModuleDependencyNode[] nodes = new ModuleDependencyNode[tables.Length];
        int index = 0;
        while (index < tables.Length)
        {
            nodes[index] = new ModuleDependencyNode
            {
                Module = tables[index],
                StableOrder = index
            };
            index = index + 1;
        }

        return nodes;
    }

    internal ModuleDependencyEdge[] CreateModuleEdges(ModuleDependencyNode[] nodes)
    {
        System.Collections.Generic.List<ModuleDependencyEdge> edges =
            new System.Collections.Generic.List<ModuleDependencyEdge>();
        int sourceIndex = 0;
        while (sourceIndex < nodes.Length)
        {
            SymbolTable? source = nodes[sourceIndex].Module;
            if (source != null)
            {
                int importIndex = 0;
                while (importIndex < source.ImportResolutions.Length)
                {
                    ImportResolution import = source.ImportResolutions[importIndex];
                    ModuleDependencyNode? target = this.FindModuleNode(nodes, import.TargetModule);
                    if (target != null
                        && !this.ContainsModuleEdge(edges, nodes[sourceIndex], target))
                    {
                        edges.Add(new ModuleDependencyEdge
                        {
                            Source = nodes[sourceIndex],
                            Target = target,
                            Import = import
                        });
                    }

                    importIndex = importIndex + 1;
                }
            }

            sourceIndex = sourceIndex + 1;
        }

        return edges.ToArray();
    }

    internal AssignmentDependencyNode[] CreateAssignmentNodes(SymbolTable[] tables)
    {
        int count = 0;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            count = count + tables[tableIndex].Symbols.Length;
            tableIndex = tableIndex + 1;
        }

        AssignmentDependencyNode[] nodes = new AssignmentDependencyNode[count];
        int nodeIndex = 0;
        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].Symbols.Length)
            {
                nodes[nodeIndex] = new AssignmentDependencyNode
                {
                    Module = tables[tableIndex],
                    Symbol = tables[tableIndex].Symbols[symbolIndex],
                    StableOrder = nodeIndex
                };
                nodeIndex = nodeIndex + 1;
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return nodes;
    }

    internal AssignmentDependencyEdge[] CreateAssignmentEdges(
        AssignmentDependencyNode[] nodes,
        SymbolTable[] tables)
    {
        System.Collections.Generic.List<AssignmentDependencyEdge> edges =
            new System.Collections.Generic.List<AssignmentDependencyEdge>();
        int sourceIndex = 0;
        while (sourceIndex < nodes.Length)
        {
            AssignmentDependencyNode source = nodes[sourceIndex];
            SymbolTableEntry? sourceSymbol = source.Symbol;
            if (source.Module != null && sourceSymbol != null)
            {
                int tokenIndex = 0;
                while (tokenIndex < sourceSymbol.ReferenceTokens.Length)
                {
                    AsnToken token = sourceSymbol.ReferenceTokens[tokenIndex];
                    if (this.IsReferenceToken(token)
                        && !this.IsScopedParameterReference(sourceSymbol, token))
                    {
                        int consumed = this.AddQualifiedReferenceEdges(
                            edges,
                            nodes,
                            source,
                            sourceSymbol.ReferenceTokens,
                            tokenIndex);
                        if (consumed == 0)
                        {
                            this.AddUnqualifiedReferenceEdges(edges, nodes, source, token);
                        }
                        else
                        {
                            tokenIndex = tokenIndex + consumed - 1;
                        }
                    }

                    tokenIndex = tokenIndex + 1;
                }
            }

            sourceIndex = sourceIndex + 1;
        }

        return edges.ToArray();
    }

    internal int AddQualifiedReferenceEdges(
        System.Collections.Generic.List<AssignmentDependencyEdge> edges,
        AssignmentDependencyNode[] nodes,
        AssignmentDependencyNode source,
        AsnToken[] tokens,
        int tokenIndex)
    {
        if (tokenIndex + 2 >= tokens.Length
            || tokens[tokenIndex + 1].Kind != AsnTokenKind.Dot
            || !this.IsReferenceToken(tokens[tokenIndex + 2])
            || source.Module == null)
        {
            return 0;
        }

        string moduleName = tokens[tokenIndex].Text;
        string symbolName = tokens[tokenIndex + 2].Text;
        int resolutionIndex = 0;
        while (resolutionIndex < source.Module.ImportResolutions.Length)
        {
            ImportResolution resolution = source.Module.ImportResolutions[resolutionIndex];
            if (resolution.TargetModule != null
                && resolution.TargetModule.ModuleName == moduleName)
            {
                this.AddBindingTargetEdges(
                    edges,
                    nodes,
                    source,
                    resolution.Symbols,
                    symbolName,
                    tokens[tokenIndex + 2]);
            }

            resolutionIndex = resolutionIndex + 1;
        }

        return 3;
    }

    internal void AddUnqualifiedReferenceEdges(
        System.Collections.Generic.List<AssignmentDependencyEdge> edges,
        AssignmentDependencyNode[] nodes,
        AssignmentDependencyNode source,
        AsnToken token)
    {
        int targetIndex = 0;
        while (targetIndex < nodes.Length)
        {
            if (nodes[targetIndex].Module == source.Module
                && nodes[targetIndex].Symbol != null
                && nodes[targetIndex].Symbol?.Name == token.Text)
            {
                this.AddAssignmentEdge(edges, source, nodes[targetIndex], token);
            }

            targetIndex = targetIndex + 1;
        }

        if (source.Module == null)
        {
            return;
        }

        int resolutionIndex = 0;
        while (resolutionIndex < source.Module.ImportResolutions.Length)
        {
            this.AddBindingTargetEdges(
                edges,
                nodes,
                source,
                source.Module.ImportResolutions[resolutionIndex].Symbols,
                token.Text,
                token);
            resolutionIndex = resolutionIndex + 1;
        }
    }

    internal void AddBindingTargetEdges(
        System.Collections.Generic.List<AssignmentDependencyEdge> edges,
        AssignmentDependencyNode[] nodes,
        AssignmentDependencyNode source,
        ImportedSymbolBinding[] bindings,
        string symbolName,
        AsnToken token)
    {
        int bindingIndex = 0;
        while (bindingIndex < bindings.Length)
        {
            ImportedSymbolBinding binding = bindings[bindingIndex];
            if (binding.Import != null
                && binding.Import.Name == symbolName
                && binding.Target != null)
            {
                AssignmentDependencyNode? target = this.FindAssignmentNode(nodes, binding.Target);
                if (target != null)
                {
                    this.AddAssignmentEdge(edges, source, target, token);
                }
            }

            bindingIndex = bindingIndex + 1;
        }
    }

    internal void AddAssignmentEdge(
        System.Collections.Generic.List<AssignmentDependencyEdge> edges,
        AssignmentDependencyNode source,
        AssignmentDependencyNode target,
        AsnToken token)
    {
        int index = 0;
        while (index < edges.Count)
        {
            if (edges[index].Source == source && edges[index].Target == target)
            {
                return;
            }

            index = index + 1;
        }

        edges.Add(new AssignmentDependencyEdge
        {
            Source = source,
            Target = target,
            ReferenceToken = token
        });
    }

    internal ModuleDependencyGroup[] CreateModuleGroups(
        ModuleDependencyNode[] nodes,
        ModuleDependencyEdge[] edges)
    {
        System.Collections.Generic.List<ModuleDependencyGroup> groups =
            new System.Collections.Generic.List<ModuleDependencyGroup>();
        bool[] assigned = new bool[nodes.Length];
        int index = 0;
        while (index < nodes.Length)
        {
            if (!assigned[index])
            {
                System.Collections.Generic.List<ModuleDependencyNode> members =
                    new System.Collections.Generic.List<ModuleDependencyNode>();
                int candidate = index;
                while (candidate < nodes.Length)
                {
                    if (!assigned[candidate]
                        && this.CanReachModule(nodes[index], nodes[candidate], nodes, edges)
                        && this.CanReachModule(nodes[candidate], nodes[index], nodes, edges))
                    {
                        members.Add(nodes[candidate]);
                        assigned[candidate] = true;
                    }

                    candidate = candidate + 1;
                }

                groups.Add(new ModuleDependencyGroup
                {
                    Nodes = members.ToArray(),
                    Recursive = members.Count > 1
                        || this.ContainsModuleEdgeArray(edges, nodes[index], nodes[index])
                });
            }

            index = index + 1;
        }

        ModuleDependencyGroup[] ordered = this.OrderModuleGroups(groups.ToArray(), edges);
        return ordered;
    }

    internal AssignmentDependencyGroup[] CreateAssignmentGroups(
        AssignmentDependencyNode[] nodes,
        AssignmentDependencyEdge[] edges)
    {
        System.Collections.Generic.List<AssignmentDependencyGroup> groups =
            new System.Collections.Generic.List<AssignmentDependencyGroup>();
        bool[] assigned = new bool[nodes.Length];
        int index = 0;
        while (index < nodes.Length)
        {
            if (!assigned[index])
            {
                System.Collections.Generic.List<AssignmentDependencyNode> members =
                    new System.Collections.Generic.List<AssignmentDependencyNode>();
                int candidate = index;
                while (candidate < nodes.Length)
                {
                    if (!assigned[candidate]
                        && this.CanReachAssignment(nodes[index], nodes[candidate], nodes, edges)
                        && this.CanReachAssignment(nodes[candidate], nodes[index], nodes, edges))
                    {
                        members.Add(nodes[candidate]);
                        assigned[candidate] = true;
                    }

                    candidate = candidate + 1;
                }

                bool recursive = members.Count > 1
                    || this.ContainsAssignmentEdge(edges, nodes[index], nodes[index]);
                groups.Add(new AssignmentDependencyGroup
                {
                    Nodes = members.ToArray(),
                    Recursive = recursive,
                    LegalTypeRecursion = recursive
                        && this.IsLegalTypeGroup(members, edges)
                });
            }

            index = index + 1;
        }

        return this.OrderAssignmentGroups(groups.ToArray(), edges);
    }

    internal ModuleDependencyGroup[] OrderModuleGroups(
        ModuleDependencyGroup[] groups,
        ModuleDependencyEdge[] edges)
    {
        ModuleDependencyGroup[] ordered = new ModuleDependencyGroup[groups.Length];
        bool[] selected = new bool[groups.Length];
        int order = 0;
        while (order < groups.Length)
        {
            int candidate = this.FindReadyModuleGroup(groups, edges, selected);
            selected[candidate] = true;
            groups[candidate].CompilationOrder = order;
            ordered[order] = groups[candidate];
            order = order + 1;
        }

        return ordered;
    }

    internal AssignmentDependencyGroup[] OrderAssignmentGroups(
        AssignmentDependencyGroup[] groups,
        AssignmentDependencyEdge[] edges)
    {
        AssignmentDependencyGroup[] ordered = new AssignmentDependencyGroup[groups.Length];
        bool[] selected = new bool[groups.Length];
        int order = 0;
        while (order < groups.Length)
        {
            int candidate = this.FindReadyAssignmentGroup(groups, edges, selected);
            selected[candidate] = true;
            groups[candidate].CompilationOrder = order;
            ordered[order] = groups[candidate];
            order = order + 1;
        }

        return ordered;
    }

    internal int FindReadyModuleGroup(
        ModuleDependencyGroup[] groups,
        ModuleDependencyEdge[] edges,
        bool[] selected)
    {
        int fallback = -1;
        int index = 0;
        while (index < groups.Length)
        {
            if (!selected[index])
            {
                if (fallback < 0)
                {
                    fallback = index;
                }

                if (!this.HasUnselectedModuleDependency(groups[index], groups, edges, selected))
                {
                    return index;
                }
            }

            index = index + 1;
        }

        return fallback;
    }

    internal int FindReadyAssignmentGroup(
        AssignmentDependencyGroup[] groups,
        AssignmentDependencyEdge[] edges,
        bool[] selected)
    {
        int fallback = -1;
        int index = 0;
        while (index < groups.Length)
        {
            if (!selected[index])
            {
                if (fallback < 0)
                {
                    fallback = index;
                }

                if (!this.HasUnselectedAssignmentDependency(groups[index], groups, edges, selected))
                {
                    return index;
                }
            }

            index = index + 1;
        }

        return fallback;
    }

    internal bool HasUnselectedModuleDependency(
        ModuleDependencyGroup group,
        ModuleDependencyGroup[] groups,
        ModuleDependencyEdge[] edges,
        bool[] selected)
    {
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            if (this.ContainsModuleNode(group, edges[edgeIndex].Source)
                && !this.ContainsModuleNode(group, edges[edgeIndex].Target))
            {
                int targetGroup = this.FindModuleGroup(groups, edges[edgeIndex].Target);
                if (targetGroup >= 0 && !selected[targetGroup])
                {
                    return true;
                }
            }

            edgeIndex = edgeIndex + 1;
        }

        return false;
    }

    internal bool HasUnselectedAssignmentDependency(
        AssignmentDependencyGroup group,
        AssignmentDependencyGroup[] groups,
        AssignmentDependencyEdge[] edges,
        bool[] selected)
    {
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            if (this.ContainsAssignmentNode(group, edges[edgeIndex].Source)
                && !this.ContainsAssignmentNode(group, edges[edgeIndex].Target))
            {
                int targetGroup = this.FindAssignmentGroup(groups, edges[edgeIndex].Target);
                if (targetGroup >= 0 && !selected[targetGroup])
                {
                    return true;
                }
            }

            edgeIndex = edgeIndex + 1;
        }

        return false;
    }

    internal bool CanReachModule(
        ModuleDependencyNode start,
        ModuleDependencyNode target,
        ModuleDependencyNode[] nodes,
        ModuleDependencyEdge[] edges)
    {
        if (start == target)
        {
            return true;
        }

        bool[] visited = new bool[nodes.Length];
        System.Collections.Generic.List<ModuleDependencyNode> pending =
            new System.Collections.Generic.List<ModuleDependencyNode>();
        pending.Add(start);
        while (pending.Count > 0)
        {
            ModuleDependencyNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            int currentIndex = this.FindModuleNodeIndex(nodes, current);
            if (currentIndex >= 0 && !visited[currentIndex])
            {
                visited[currentIndex] = true;
                int edgeIndex = 0;
                while (edgeIndex < edges.Length)
                {
                    ModuleDependencyNode? edgeTarget = edges[edgeIndex].Target;
                    if (edges[edgeIndex].Source == current && edgeTarget != null)
                    {
                        if (edgeTarget == target)
                        {
                            return true;
                        }

                        pending.Add(edgeTarget);
                    }

                    edgeIndex = edgeIndex + 1;
                }
            }
        }

        return false;
    }

    internal bool CanReachAssignment(
        AssignmentDependencyNode start,
        AssignmentDependencyNode target,
        AssignmentDependencyNode[] nodes,
        AssignmentDependencyEdge[] edges)
    {
        if (start == target)
        {
            return true;
        }

        bool[] visited = new bool[nodes.Length];
        System.Collections.Generic.List<AssignmentDependencyNode> pending =
            new System.Collections.Generic.List<AssignmentDependencyNode>();
        pending.Add(start);
        while (pending.Count > 0)
        {
            AssignmentDependencyNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            int currentIndex = this.FindAssignmentNodeIndex(nodes, current);
            if (currentIndex >= 0 && !visited[currentIndex])
            {
                visited[currentIndex] = true;
                int edgeIndex = 0;
                while (edgeIndex < edges.Length)
                {
                    AssignmentDependencyNode? edgeTarget = edges[edgeIndex].Target;
                    if (edges[edgeIndex].Source == current && edgeTarget != null)
                    {
                        if (edgeTarget == target)
                        {
                            return true;
                        }

                        pending.Add(edgeTarget);
                    }

                    edgeIndex = edgeIndex + 1;
                }
            }
        }

        return false;
    }

    internal bool IsLegalTypeGroup(
        System.Collections.Generic.List<AssignmentDependencyNode> members,
        AssignmentDependencyEdge[] edges)
    {
        if (this.IsObjectClassDefinitionGroup(members))
        {
            return true;
        }

        int index = 0;
        while (index < members.Count)
        {
            SymbolTableEntry? symbol = members[index].Symbol;
            if (symbol == null
                || (symbol.Parameterized
                    && !this.ContainsCandidateAssignmentKind(
                        symbol.CandidateKinds,
                        AsnAssignmentKind.Type)
                    && !this.HasNoParameterizedGovernor(symbol))
                || (!symbol.Parameterized
                    && (symbol.CandidateKinds.Length != 1
                        || symbol.CandidateKinds[0] != AsnAssignmentKind.Type)))
            {
                return false;
            }

            index = index + 1;
        }

        return this.ParameterizedCycleEdgesAreGuarded(members, edges);
    }

    internal bool IsObjectClassDefinitionGroup(
        System.Collections.Generic.List<AssignmentDependencyNode> members)
    {
        int index = 0;
        while (index < members.Count)
        {
            SymbolTableEntry? symbol = members[index].Symbol;
            if (symbol == null
                || symbol.Parameterized
                || !this.ContainsCandidateAssignmentKind(
                    symbol.CandidateKinds,
                    AsnAssignmentKind.ObjectClass)
                || !this.HasInlineObjectClassDefinition(symbol))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool HasInlineObjectClassDefinition(SymbolTableEntry symbol)
    {
        int index = 0;
        while (index < symbol.DeclarationTokens.Length
            && symbol.DeclarationTokens[index].Kind != AsnTokenKind.Assignment)
        {
            index = index + 1;
        }

        index = index + 1;
        return index < symbol.DeclarationTokens.Length
            && symbol.DeclarationTokens[index].Text == "CLASS";
    }

    internal bool HasNoParameterizedGovernor(SymbolTableEntry symbol)
    {
        if (symbol.DeclarationTokens.Length < 3
            || symbol.DeclarationTokens[1].Kind != AsnTokenKind.LeftBrace)
        {
            return false;
        }

        int depth = 1;
        int index = 2;
        while (index < symbol.DeclarationTokens.Length && depth > 0)
        {
            if (symbol.DeclarationTokens[index].Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (symbol.DeclarationTokens[index].Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        return index < symbol.DeclarationTokens.Length
            && symbol.DeclarationTokens[index].Kind == AsnTokenKind.Assignment;
    }

    internal bool ParameterizedCycleEdgesAreGuarded(
        System.Collections.Generic.List<AssignmentDependencyNode> members,
        AssignmentDependencyEdge[] edges)
    {
        int index = 0;
        while (index < edges.Length)
        {
            AssignmentDependencyEdge edge = edges[index];
            if (this.ContainsAssignmentMember(members, edge.Source)
                && this.ContainsAssignmentMember(members, edge.Target)
                && edge.Source != null
                && edge.Source.Symbol != null
                && edge.Source.Symbol.Parameterized
                && !this.IsGuardedParameterizedCycleReference(
                    edge.Source.Symbol,
                    edge.ReferenceToken))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ContainsAssignmentMember(
        System.Collections.Generic.List<AssignmentDependencyNode> members,
        AssignmentDependencyNode? candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        int index = 0;
        while (index < members.Count)
        {
            if (members[index] == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsGuardedParameterizedCycleReference(
        SymbolTableEntry source,
        AsnToken? reference)
    {
        if (reference == null)
        {
            return false;
        }

        int referenceIndex = 0;
        while (referenceIndex < source.DeclarationTokens.Length
            && source.DeclarationTokens[referenceIndex] != reference)
        {
            referenceIndex = referenceIndex + 1;
        }

        if (referenceIndex >= source.DeclarationTokens.Length)
        {
            return false;
        }

        int braceIndex = referenceIndex + 1;
        if (braceIndex >= source.DeclarationTokens.Length
            || source.DeclarationTokens[braceIndex].Kind != AsnTokenKind.LeftBrace)
        {
            return false;
        }

        int depth = 1;
        int index = braceIndex + 1;
        while (index < source.DeclarationTokens.Length && depth > 0)
        {
            if (source.DeclarationTokens[index].Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (source.DeclarationTokens[index].Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        while (index < source.DeclarationTokens.Length)
        {
            if (source.DeclarationTokens[index].Text == "OPTIONAL")
            {
                return true;
            }

            if (source.DeclarationTokens[index].Kind == AsnTokenKind.Comma)
            {
                break;
            }

            index = index + 1;
        }

        index = referenceIndex - 1;
        while (index >= 0
            && source.DeclarationTokens[index].Kind != AsnTokenKind.Assignment)
        {
            if (source.DeclarationTokens[index].Text == "CHOICE")
            {
                return true;
            }

            index = index - 1;
        }

        return false;
    }

    internal bool ContainsCandidateAssignmentKind(
        AsnAssignmentKind[] kinds,
        AsnAssignmentKind expected)
    {
        int index = 0;
        while (index < kinds.Length)
        {
            if (kinds[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool DiagnoseIllegalCycles(AssignmentDependencyGroup[] groups)
    {
        bool success = true;
        int index = 0;
        while (index < groups.Length)
        {
            if (groups[index].Recursive && !groups[index].LegalTypeRecursion)
            {
                success = false;
                AssignmentDependencyNode? first = groups[index].Nodes.Length == 0
                    ? null
                    : groups[index].Nodes[0];
                this.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Binding,
                    9,
                    "An assignment dependency cycle contains a value, object, object set, or parameterized assignment.",
                    first == null || first.Symbol == null ? null : first.Symbol.Span,
                    this.CreateCycleLocations(groups[index], first));
            }

            index = index + 1;
        }

        return success;
    }

    internal AsnDiagnosticRelatedLocation[] CreateCycleLocations(
        AssignmentDependencyGroup group,
        AssignmentDependencyNode? first)
    {
        int count = 0;
        int index = 0;
        while (index < group.Nodes.Length)
        {
            if (group.Nodes[index] != first
                && group.Nodes[index].Symbol != null
                && group.Nodes[index].Symbol?.Span != null)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        AsnDiagnosticRelatedLocation[] locations = new AsnDiagnosticRelatedLocation[count];
        int locationIndex = 0;
        index = 0;
        while (index < group.Nodes.Length)
        {
            SymbolTableEntry? symbol = group.Nodes[index].Symbol;
            if (group.Nodes[index] != first && symbol != null && symbol.Span != null)
            {
                locations[locationIndex] = new AsnDiagnosticRelatedLocation
                {
                    Message = "Another declaration in the cycle is here.",
                    Span = symbol.Span
                };
                locationIndex = locationIndex + 1;
            }

            index = index + 1;
        }

        return locations;
    }

    internal ModuleDependencyNode? FindModuleNode(
        ModuleDependencyNode[] nodes,
        SymbolTable? table)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index].Module == table)
            {
                return nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AssignmentDependencyNode? FindAssignmentNode(
        AssignmentDependencyNode[] nodes,
        SymbolTableEntry symbol)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index].Symbol == symbol)
            {
                return nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal int FindModuleNodeIndex(ModuleDependencyNode[] nodes, ModuleDependencyNode node)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == node)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindAssignmentNodeIndex(
        AssignmentDependencyNode[] nodes,
        AssignmentDependencyNode node)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == node)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool ContainsModuleEdge(
        System.Collections.Generic.List<ModuleDependencyEdge> edges,
        ModuleDependencyNode source,
        ModuleDependencyNode target)
    {
        int index = 0;
        while (index < edges.Count)
        {
            if (edges[index].Source == source && edges[index].Target == target)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsModuleEdgeArray(
        ModuleDependencyEdge[] edges,
        ModuleDependencyNode source,
        ModuleDependencyNode target)
    {
        int index = 0;
        while (index < edges.Length)
        {
            if (edges[index].Source == source && edges[index].Target == target)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsAssignmentEdge(
        AssignmentDependencyEdge[] edges,
        AssignmentDependencyNode source,
        AssignmentDependencyNode target)
    {
        int index = 0;
        while (index < edges.Length)
        {
            if (edges[index].Source == source && edges[index].Target == target)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsModuleNode(ModuleDependencyGroup group, ModuleDependencyNode? node)
    {
        int index = 0;
        while (index < group.Nodes.Length)
        {
            if (group.Nodes[index] == node)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsAssignmentNode(
        AssignmentDependencyGroup group,
        AssignmentDependencyNode? node)
    {
        int index = 0;
        while (index < group.Nodes.Length)
        {
            if (group.Nodes[index] == node)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int FindModuleGroup(ModuleDependencyGroup[] groups, ModuleDependencyNode? node)
    {
        int index = 0;
        while (index < groups.Length)
        {
            if (this.ContainsModuleNode(groups[index], node))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindAssignmentGroup(
        AssignmentDependencyGroup[] groups,
        AssignmentDependencyNode? node)
    {
        int index = 0;
        while (index < groups.Length)
        {
            if (this.ContainsAssignmentNode(groups[index], node))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool IsReferenceToken(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.ObjectReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.ObjectSetReference;
    }

    internal bool IsScopedParameterReference(
        SymbolTableEntry symbol,
        AsnToken token)
    {
        int declarationIndex = this.FindDeclarationTokenIndex(
            symbol.DeclarationTokens,
            token);
        if (!this.IsWithinDummyScope(symbol.DeclarationTokens, declarationIndex)
            || this.IsQualifiedReferencePart(
                symbol.DeclarationTokens,
                declarationIndex))
        {
            return false;
        }

        int index = 0;
        while (index < symbol.ParameterNames.Length)
        {
            if (symbol.ParameterNames[index] == token.Text)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsWithinDummyScope(AsnToken[] tokens, int tokenIndex)
    {
        int parameterEnd = this.FindParameterListEnd(tokens);
        int assignment = this.FindAssignmentIndex(tokens);
        return (tokenIndex >= 1 && tokenIndex <= parameterEnd)
            || tokenIndex > assignment;
    }

    internal bool IsQualifiedReferencePart(AsnToken[] tokens, int index)
    {
        if (index < 0)
        {
            return false;
        }

        if (index > 0 && tokens[index - 1].Kind == AsnTokenKind.Dot)
        {
            return true;
        }

        return index + 2 < tokens.Length
            && tokens[index + 1].Kind == AsnTokenKind.Dot
            && !this.IsFieldReference(tokens[index + 2]);
    }

    internal bool IsFieldReference(AsnToken token)
    {
        return token.Kind == AsnTokenKind.TypeFieldReference
            || token.Kind == AsnTokenKind.ValueFieldReference
            || token.Kind == AsnTokenKind.ValueSetFieldReference
            || token.Kind == AsnTokenKind.ObjectFieldReference
            || token.Kind == AsnTokenKind.ObjectSetFieldReference;
    }

    internal int FindDeclarationTokenIndex(AsnToken[] tokens, AsnToken target)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == target)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindParameterListEnd(AsnToken[] tokens)
    {
        if (tokens.Length < 3 || tokens[1].Kind != AsnTokenKind.LeftBrace)
        {
            return -1;
        }

        int depth = 0;
        int index = 1;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (tokens[index].Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return index;
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindAssignmentIndex(AsnToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == AsnTokenKind.Assignment)
            {
                return index;
            }

            index = index + 1;
        }

        return tokens.Length;
    }
}
