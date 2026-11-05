namespace asn1;

internal class PatternConstraintCompiler
{
    internal DiagnosticBag Diagnostics;
    internal bool Success = true;

    internal PatternConstraintCompiler(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool CompileTypes(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = tables[tableIndex].TypeSymbols[typeIndex].ResolvedType;
                if (type != null)
                {
                    this.CompileType(type, visited);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal void CompileType(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);
        System.Collections.Generic.List<PatternProgram> programs =
            new System.Collections.Generic.List<PatternProgram>();
        System.Collections.Generic.List<NormalizedConstraintNode> patternNodes =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        if (type.EffectiveConstraint != null)
        {
            this.CompileGraph(type.EffectiveConstraint, programs, patternNodes);
        }

        type.Patterns = programs.ToArray();
        if (type.AliasTarget != null)
        {
            this.CompileType(type.AliasTarget, visited);
        }

        if (type.InnerType != null)
        {
            this.CompileType(type.InnerType, visited);
        }

        if (type.SelectedComponent != null
            && type.SelectedComponent.TypeResolution != null)
        {
            this.CompileType(type.SelectedComponent.TypeResolution, visited);
        }

        int componentIndex = 0;
        while (componentIndex < type.Components.Length)
        {
            ResolvedTypeSyntax? componentType = type.Components[componentIndex].TypeResolution;
            if (componentType != null)
            {
                this.CompileType(componentType, visited);
            }

            componentIndex = componentIndex + 1;
        }

        int constraintIndex = 0;
        while (constraintIndex < type.ConstraintTypes.Length)
        {
            this.CompileType(type.ConstraintTypes[constraintIndex], visited);
            constraintIndex = constraintIndex + 1;
        }
    }

    internal void CompileGraph(
        NormalizedConstraintGraph graph,
        System.Collections.Generic.List<PatternProgram> programs,
        System.Collections.Generic.List<NormalizedConstraintNode> patternNodes)
    {
        if (graph.RootPartition != null && graph.RootPartition.Expression != null)
        {
            this.CompileNode(graph.RootPartition.Expression, programs, patternNodes);
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            this.CompileNode(graph.ExtensionPartition.Expression, programs, patternNodes);
        }
    }

    internal void CompileNode(
        NormalizedConstraintNode node,
        System.Collections.Generic.List<PatternProgram> programs,
        System.Collections.Generic.List<NormalizedConstraintNode> patternNodes)
    {
        if (node.Kind == AsnConstraintKind.Pattern && !patternNodes.Contains(node))
        {
            patternNodes.Add(node);
            PatternParseResult parsed = new PatternParser(node.Text).ParsePattern();
            if (!parsed.Success || parsed.Root == null)
            {
                this.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Constraint,
                    ConstraintDiagnosticCodes.InvalidPattern,
                    "Invalid PATTERN at character " + parsed.ErrorOffset.ToString()
                        + ": " + parsed.ErrorMessage,
                    node.SourceSyntax == null ? null : node.SourceSyntax.Span,
                    null);
                this.Success = false;
            }
            else
            {
                PatternProgram program = new PatternProgram
                {
                    Source = node.Text,
                    Root = parsed.Root,
                    SourceNode = node
                };
                node.CompiledPattern = program;
                programs.Add(program);
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.CompileNode(node.Children[childIndex], programs, patternNodes);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.CompileGraph(node.NestedConstraints[nestedIndex], programs, patternNodes);
            nestedIndex = nestedIndex + 1;
        }
    }
}
