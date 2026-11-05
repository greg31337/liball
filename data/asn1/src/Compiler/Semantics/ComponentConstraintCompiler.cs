namespace asn1;

internal class ComponentConstraintCompiler
{
    internal DiagnosticBag Diagnostics;
    internal bool Success = true;

    internal ComponentConstraintCompiler(DiagnosticBag diagnostics)
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
        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets =
            new System.Collections.Generic.List<NormalizedComponentConstraintSet>();
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        if (type.EffectiveConstraint != null)
        {
            this.CompileGraph(type.EffectiveConstraint, type, sets, compiledNodes);
        }

        type.ComponentConstraintSets = sets.ToArray();
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
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        ResolvedTypeSyntax effectiveGoverningType = graph.GoverningType == null
            ? governingType
            : graph.GoverningType;
        if (graph.RootPartition != null && graph.RootPartition.Expression != null)
        {
            this.CompileNode(
                graph.RootPartition.Expression,
                effectiveGoverningType,
                sets,
                compiledNodes);
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            this.CompileNode(
                graph.ExtensionPartition.Expression,
                effectiveGoverningType,
                sets,
                compiledNodes);
        }
    }

    internal void CompileNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        ResolvedTypeSyntax effectiveGoverningType = node.GoverningType == null
            ? governingType
            : node.GoverningType;
        if (node.Kind == AsnConstraintKind.WithComponents
            && !compiledNodes.Contains(node))
        {
            compiledNodes.Add(node);
            this.CompileMultipleComponents(
                node,
                effectiveGoverningType,
                sets,
                compiledNodes);
            return;
        }

        if (node.Kind == AsnConstraintKind.WithComponent
            && node.Name.Length == 0
            && !compiledNodes.Contains(node))
        {
            compiledNodes.Add(node);
            this.CompileCollectionComponent(
                node,
                effectiveGoverningType,
                sets,
                compiledNodes);
            return;
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.CompileNode(
                node.Children[childIndex],
                effectiveGoverningType,
                sets,
                compiledNodes);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.CompileGraph(
                node.NestedConstraints[nestedIndex],
                effectiveGoverningType,
                sets,
                compiledNodes);
            nestedIndex = nestedIndex + 1;
        }
    }

    internal void CompileCollectionComponent(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        ResolvedTypeSyntax? elementType = this.FindCollectionElementType(governingType);
        if ((governingType.EffectiveKind != AsnTypeKind.SequenceOf
                && governingType.EffectiveKind != AsnTypeKind.SetOf)
            || elementType == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidComponentConstraintType,
                "WITH COMPONENT can only constrain a SEQUENCE OF or SET OF element type.",
                node.SourceSyntax == null ? null : node.SourceSyntax.Span);
            return;
        }

        NormalizedConstraintGraph? valueConstraint = null;
        ParsedConstraintSyntax? nestedSyntax = node.SourceSyntax == null
            ? null
            : node.SourceSyntax.NestedConstraintSyntax;
        if (nestedSyntax != null)
        {
            valueConstraint = this.NormalizeNestedConstraint(
                nestedSyntax,
                elementType);
            node.NestedConstraints = [valueConstraint];
        }

        NormalizedComponentConstraint item = new NormalizedComponentConstraint
        {
            ComponentType = elementType,
            ValueConstraint = valueConstraint
        };
        NormalizedComponentConstraintSet set = new NormalizedComponentConstraintSet
        {
            GoverningType = governingType,
            SourceNode = node,
            Components = [item],
            CollectionElement = true,
            PartialSpecification = false
        };
        node.CompiledComponentConstraints = set;
        sets.Add(set);
        if (valueConstraint != null)
        {
            this.CompileGraph(
                valueConstraint,
                elementType,
                sets,
                compiledNodes);
        }
    }

    internal ResolvedTypeSyntax? FindCollectionElementType(ResolvedTypeSyntax type)
    {
        ResolvedTypeSyntax canonical = type.ConstraintDomain == null
            || type.ConstraintDomain.CanonicalType == null
            ? type
            : type.ConstraintDomain.CanonicalType;
        if ((canonical.EffectiveKind == AsnTypeKind.SequenceOf
                || canonical.EffectiveKind == AsnTypeKind.SetOf)
            && canonical.InnerType != null)
        {
            return canonical.InnerType;
        }

        return null;
    }

    internal void CompileMultipleComponents(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        if (governingType.EffectiveKind == AsnTypeKind.Real)
        {
            this.CompileRealComponents(
                node,
                governingType,
                sets,
                compiledNodes);
            return;
        }
        if (governingType.EffectiveKind == AsnTypeKind.Duration)
        {
            this.CompileDurationComponents(
                node,
                governingType,
                sets);
            return;
        }
        if (governingType.EffectiveKind != AsnTypeKind.Sequence
            && governingType.EffectiveKind != AsnTypeKind.Set
            && governingType.EffectiveKind != AsnTypeKind.Choice)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidComponentConstraintType,
                "WITH COMPONENTS can only constrain a SEQUENCE, SET, or CHOICE type.",
                node.SourceSyntax == null ? null : node.SourceSyntax.Span);
            return;
        }

        ParsedSubtypeElementSyntax? source = node.SourceSyntax;
        if (source == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidComponentConstraintType,
                "WITH COMPONENTS has no retained component specification.",
                null);
            return;
        }

        System.Collections.Generic.List<NormalizedComponentConstraint> components =
            new System.Collections.Generic.List<NormalizedComponentConstraint>();
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        int lastSequenceIndex = -1;
        int presentChoiceCount = 0;
        int sourceIndex = 0;
        while (sourceIndex < source.NamedConstraints.Length)
        {
            ParsedNamedConstraintSyntax named = source.NamedConstraints[sourceIndex];
            int componentIndex = this.FindComponentIndex(governingType, named.Name);
            if (names.Contains(named.Name))
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.DuplicateComponentConstraint,
                    "Component '" + named.Name + "' is constrained more than once.",
                    named.Span);
            }
            else
            {
                names.Add(named.Name);
            }

            if (componentIndex < 0)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.UnknownComponentConstraint,
                    "Component '" + named.Name + "' does not exist in the constrained type.",
                    named.Span);
                components.Add(new NormalizedComponentConstraint
                {
                    Name = named.Name,
                    Presence = this.ParsePresence(named.Presence),
                    SourceSyntax = named
                });
                sourceIndex = sourceIndex + 1;
                continue;
            }

            if (governingType.EffectiveKind == AsnTypeKind.Sequence
                && componentIndex < lastSequenceIndex)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.ComponentConstraintOrder,
                    "SEQUENCE component constraints must follow component declaration order.",
                    named.Span);
            }

            lastSequenceIndex = componentIndex;
            ResolvedComponentSyntax component = governingType.Components[componentIndex];
            ComponentPresenceConstraintKind presence = this.ParsePresence(named.Presence);
            if (!this.ValidatePresence(
                governingType.EffectiveKind,
                component,
                presence,
                named.Span))
            {
                presence = ComponentPresenceConstraintKind.Unspecified;
            }

            if (governingType.EffectiveKind == AsnTypeKind.Choice
                && presence == ComponentPresenceConstraintKind.Present)
            {
                presentChoiceCount = presentChoiceCount + 1;
                if (presentChoiceCount > 1)
                {
                    this.AddDiagnostic(
                        ConstraintDiagnosticCodes.InvalidPresenceConstraint,
                        "A CHOICE WITH COMPONENTS constraint can mark at most one alternative PRESENT.",
                        named.Span);
                }
            }

            if (!source.PartialSpecification
                && governingType.EffectiveKind != AsnTypeKind.Choice
                && presence == ComponentPresenceConstraintKind.Unspecified
                && component.Component != null
                && component.Component.Optional)
            {
                presence = ComponentPresenceConstraintKind.Present;
            }

            NormalizedConstraintGraph? valueConstraint = null;
            if (named.ValueConstraintSyntax != null
                && component.TypeResolution != null)
            {
                valueConstraint = this.NormalizeNestedConstraint(
                    named.ValueConstraintSyntax,
                    component.TypeResolution);
                if (sourceIndex < node.Children.Length)
                {
                    node.Children[sourceIndex].NestedConstraints = [valueConstraint];
                }
            }

            components.Add(new NormalizedComponentConstraint
            {
                Name = named.Name,
                Component = component,
                ComponentType = component.TypeResolution,
                Presence = presence,
                ValueConstraint = valueConstraint,
                SourceSyntax = named
            });
            sourceIndex = sourceIndex + 1;
        }

        if (!source.PartialSpecification)
        {
            this.AddImplicitAbsentComponents(governingType, names, components);
        }

        NormalizedComponentConstraintSet set = new NormalizedComponentConstraintSet
        {
            GoverningType = governingType,
            SourceNode = node,
            Components = components.ToArray(),
            CollectionElement = false,
            PartialSpecification = source.PartialSpecification
        };
        node.CompiledComponentConstraints = set;
        sets.Add(set);
        int compiledIndex = 0;
        while (compiledIndex < set.Components.Length)
        {
            NormalizedComponentConstraint component = set.Components[compiledIndex];
            if (component.ValueConstraint != null && component.ComponentType != null)
            {
                this.CompileGraph(
                    component.ValueConstraint,
                    component.ComponentType,
                    sets,
                    compiledNodes);
            }

            compiledIndex = compiledIndex + 1;
        }
    }

    internal void CompileRealComponents(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        ParsedSubtypeElementSyntax? source = node.SourceSyntax;
        if (source == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidComponentConstraintType,
                "REAL WITH COMPONENTS has no retained component specification.",
                null);
            return;
        }

        System.Collections.Generic.List<NormalizedComponentConstraint> components =
            new System.Collections.Generic.List<NormalizedComponentConstraint>();
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        int sourceIndex = 0;
        while (sourceIndex < source.NamedConstraints.Length)
        {
            ParsedNamedConstraintSyntax named =
                source.NamedConstraints[sourceIndex];
            bool known = named.Name == "mantissa"
                || named.Name == "base"
                || named.Name == "exponent";
            if (names.Contains(named.Name))
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.DuplicateComponentConstraint,
                    "REAL component '" + named.Name
                        + "' is constrained more than once.",
                    named.Span);
            }
            else
            {
                names.Add(named.Name);
            }
            if (!known)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.UnknownComponentConstraint,
                    "Component '" + named.Name
                        + "' does not exist in REAL.",
                    named.Span);
            }

            ResolvedTypeSyntax componentType = this.CreateRealComponentType();
            NormalizedConstraintGraph? valueConstraint = null;
            if (known && named.ValueConstraintSyntax != null)
            {
                valueConstraint = this.NormalizeNestedConstraint(
                    named.ValueConstraintSyntax,
                    componentType);
                if (sourceIndex < node.Children.Length)
                {
                    node.Children[sourceIndex].NestedConstraints =
                        [valueConstraint];
                }
            }
            components.Add(new NormalizedComponentConstraint
            {
                Name = named.Name,
                ComponentType = componentType,
                ValueConstraint = valueConstraint,
                SourceSyntax = named
            });
            sourceIndex = sourceIndex + 1;
        }

        NormalizedComponentConstraintSet set =
            new NormalizedComponentConstraintSet
            {
                GoverningType = governingType,
                SourceNode = node,
                Components = components.ToArray(),
                CollectionElement = false,
                PartialSpecification = source.PartialSpecification
            };
        node.CompiledComponentConstraints = set;
        sets.Add(set);
        int componentIndex = 0;
        while (componentIndex < set.Components.Length)
        {
            NormalizedComponentConstraint component =
                set.Components[componentIndex];
            if (component.ValueConstraint != null
                && component.ComponentType != null)
            {
                this.CompileGraph(
                    component.ValueConstraint,
                    component.ComponentType,
                    sets,
                    compiledNodes);
            }
            componentIndex = componentIndex + 1;
        }
    }

    internal ResolvedTypeSyntax CreateRealComponentType()
    {
        AsnType integer = new AsnType
        {
            Kind = AsnTypeKind.Integer
        };
        return new ResolvedTypeSyntax
        {
            SourceType = integer,
            EffectiveType = integer,
            EffectiveKind = AsnTypeKind.Integer,
            Success = true
        };
    }

    internal void CompileDurationComponents(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets)
    {
        ParsedSubtypeElementSyntax? source = node.SourceSyntax;
        if (source == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidComponentConstraintType,
                "DURATION WITH COMPONENTS has no retained component specification.",
                null);
            return;
        }

        System.Collections.Generic.List<NormalizedComponentConstraint> components =
            new System.Collections.Generic.List<NormalizedComponentConstraint>();
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        int sourceIndex = 0;
        while (sourceIndex < source.NamedConstraints.Length)
        {
            ParsedNamedConstraintSyntax named =
                source.NamedConstraints[sourceIndex];
            bool known = named.Name == "years"
                || named.Name == "months"
                || named.Name == "weeks"
                || named.Name == "days"
                || named.Name == "hours"
                || named.Name == "minutes"
                || named.Name == "seconds"
                || named.Name == "fractional-part";
            if (names.Contains(named.Name))
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.DuplicateComponentConstraint,
                    "DURATION component '" + named.Name
                        + "' is constrained more than once.",
                    named.Span);
            }
            else
            {
                names.Add(named.Name);
            }
            if (!known)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.UnknownComponentConstraint,
                    "Component '" + named.Name
                        + "' does not exist in DURATION.",
                    named.Span);
            }
            if (named.ValueConstraintSyntax != null)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.InvalidComponentConstraintType,
                    "DURATION component value constraints require application validation.",
                    named.Span);
            }
            components.Add(new NormalizedComponentConstraint
            {
                Name = named.Name,
                Presence = this.ParsePresence(named.Presence),
                SourceSyntax = named
            });
            sourceIndex = sourceIndex + 1;
        }

        NormalizedComponentConstraintSet set =
            new NormalizedComponentConstraintSet
            {
                GoverningType = governingType,
                SourceNode = node,
                Components = components.ToArray(),
                CollectionElement = false,
                PartialSpecification = source.PartialSpecification
            };
        node.CompiledComponentConstraints = set;
        sets.Add(set);
    }

    internal NormalizedConstraintGraph NormalizeNestedConstraint(
        ParsedConstraintSyntax syntax,
        ResolvedTypeSyntax componentType)
    {
        ConstraintDomainIdentity domain = componentType.ConstraintDomain == null
            ? new ConstraintNormalizer().GetDomain(componentType)
            : componentType.ConstraintDomain;
        return new ConstraintNormalizer().NormalizeConstraint(
            syntax,
            domain,
            componentType);
    }

    internal int FindComponentIndex(ResolvedTypeSyntax type, string name)
    {
        int index = 0;
        while (index < type.Components.Length)
        {
            AsnComponent? component = type.Components[index].Component;
            if (component != null && component.Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal ComponentPresenceConstraintKind ParsePresence(string presence)
    {
        switch (presence)
        {
            case "PRESENT":
                return ComponentPresenceConstraintKind.Present;
            case "ABSENT":
                return ComponentPresenceConstraintKind.Absent;
            case "OPTIONAL":
                return ComponentPresenceConstraintKind.Optional;
            default:
                return ComponentPresenceConstraintKind.Unspecified;
        }
    }

    internal bool ValidatePresence(
        AsnTypeKind governingKind,
        ResolvedComponentSyntax component,
        ComponentPresenceConstraintKind presence,
        AsnSourceSpan? span)
    {
        if (presence == ComponentPresenceConstraintKind.Unspecified)
        {
            return true;
        }

        if (governingKind == AsnTypeKind.Choice)
        {
            if (presence != ComponentPresenceConstraintKind.Optional)
            {
                return true;
            }

            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidPresenceConstraint,
                "OPTIONAL is not a valid presence constraint for a CHOICE alternative.",
                span);
            return false;
        }

        if (component.Component != null && component.Component.Optional)
        {
            return true;
        }

        this.AddDiagnostic(
            ConstraintDiagnosticCodes.InvalidPresenceConstraint,
            "Presence constraints can only be applied to OPTIONAL SEQUENCE or SET components.",
            span);
        return false;
    }

    internal void AddImplicitAbsentComponents(
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<string> explicitNames,
        System.Collections.Generic.List<NormalizedComponentConstraint> components)
    {
        int index = 0;
        while (index < governingType.Components.Length)
        {
            ResolvedComponentSyntax component = governingType.Components[index];
            AsnComponent? source = component.Component;
            bool canBeAbsent = governingType.EffectiveKind == AsnTypeKind.Choice
                || (source != null && source.Optional);
            if (source != null
                && canBeAbsent
                && !explicitNames.Contains(source.Name))
            {
                components.Add(new NormalizedComponentConstraint
                {
                    Name = source.Name,
                    Component = component,
                    ComponentType = component.TypeResolution,
                    Presence = ComponentPresenceConstraintKind.Absent,
                    Implicit = true
                });
            }

            index = index + 1;
        }
    }

    internal void AddDiagnostic(int code, string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Constraint,
            code,
            message,
            span,
            null);
        this.Success = false;
    }
}
