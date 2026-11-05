namespace asn1;

internal class TimeSettingsCompiler
{
    internal DiagnosticBag Diagnostics;
    internal bool Success = true;

    internal TimeSettingsCompiler(DiagnosticBag diagnostics)
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
        System.Collections.Generic.List<TimePropertySettings> settings =
            new System.Collections.Generic.List<TimePropertySettings>();
        System.Collections.Generic.List<NormalizedConstraintNode> settingsNodes =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        if (type.EffectiveConstraint != null)
        {
            this.CompileGraph(type.EffectiveConstraint, settings, settingsNodes);
        }

        type.TimeSettings = settings.ToArray();
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
        System.Collections.Generic.List<TimePropertySettings> settings,
        System.Collections.Generic.List<NormalizedConstraintNode> settingsNodes)
    {
        if (graph.RootPartition != null && graph.RootPartition.Expression != null)
        {
            this.CompileNode(graph.RootPartition.Expression, settings, settingsNodes);
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            this.CompileNode(graph.ExtensionPartition.Expression, settings, settingsNodes);
        }
    }

    internal void CompileNode(
        NormalizedConstraintNode node,
        System.Collections.Generic.List<TimePropertySettings> settings,
        System.Collections.Generic.List<NormalizedConstraintNode> settingsNodes)
    {
        if (node.Kind == AsnConstraintKind.PropertySettings
            && !settingsNodes.Contains(node))
        {
            settingsNodes.Add(node);
            TimeSettingsParseResult parsed = new TimeSettingsParser(node.Text).ParseSettings();
            if (!parsed.Success || parsed.Settings == null)
            {
                this.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Constraint,
                    parsed.DiagnosticCode,
                    "Invalid SETTINGS at character " + parsed.ErrorOffset.ToString()
                        + ": " + parsed.ErrorMessage,
                    node.SourceSyntax == null ? null : node.SourceSyntax.Span,
                    null);
                this.Success = false;
            }
            else
            {
                parsed.Settings.SourceNode = node;
                node.CompiledTimeSettings = parsed.Settings;
                settings.Add(parsed.Settings);
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.CompileNode(node.Children[childIndex], settings, settingsNodes);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.CompileGraph(node.NestedConstraints[nestedIndex], settings, settingsNodes);
            nestedIndex = nestedIndex + 1;
        }
    }
}
