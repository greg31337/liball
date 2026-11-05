namespace asn1;

internal class TypeContentsConstraintCompiler
{
    internal ValueResolver ValueResolver;
    internal DiagnosticBag Diagnostics;
    internal bool Success = true;

    internal TypeContentsConstraintCompiler(ValueResolver valueResolver)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.Diagnostics = valueResolver.Diagnostics;
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
        System.Collections.Generic.List<NormalizedTypeConstraint> typeConstraints =
            new System.Collections.Generic.List<NormalizedTypeConstraint>();
        System.Collections.Generic.List<InnerCodecMetadata> contentsConstraints =
            new System.Collections.Generic.List<InnerCodecMetadata>();
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        if (type.EffectiveConstraint != null)
        {
            this.CompileGraph(
                type.EffectiveConstraint,
                type,
                type,
                typeConstraints,
                contentsConstraints,
                constraintTypes,
                compiledNodes);
        }

        type.TypeConstraints = typeConstraints.ToArray();
        type.ContentsConstraints = contentsConstraints.ToArray();
        type.ConstraintTypes = constraintTypes.ToArray();
        type.EffectiveInnerCodec = contentsConstraints.Count == 0
            ? null
            : contentsConstraints[0];
        if (contentsConstraints.Count > 0
            && this.CountConstraintApplications(type.EffectiveConstraint) > 1)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.ContentsConstraintFollowed,
                "A contents-constrained BIT STRING or OCTET STRING cannot have further constraints.",
                contentsConstraints[0].SourceNode == null
                    || contentsConstraints[0].SourceNode?.SourceGeneralConstraint == null
                    ? null
                    : contentsConstraints[0].SourceNode?.SourceGeneralConstraint?.Span);
        }

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
        ResolvedTypeSyntax ownerType,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<NormalizedTypeConstraint> typeConstraints,
        System.Collections.Generic.List<InnerCodecMetadata> contentsConstraints,
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        ResolvedTypeSyntax effectiveGoverningType = graph.GoverningType == null
            ? governingType
            : graph.GoverningType;
        if (graph.RootPartition != null && graph.RootPartition.Expression != null)
        {
            this.CompileNode(
                graph.RootPartition.Expression,
                ownerType,
                effectiveGoverningType,
                typeConstraints,
                contentsConstraints,
                constraintTypes,
                compiledNodes);
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            this.CompileNode(
                graph.ExtensionPartition.Expression,
                ownerType,
                effectiveGoverningType,
                typeConstraints,
                contentsConstraints,
                constraintTypes,
                compiledNodes);
        }
    }

    internal void CompileNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax ownerType,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<NormalizedTypeConstraint> typeConstraints,
        System.Collections.Generic.List<InnerCodecMetadata> contentsConstraints,
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        ResolvedTypeSyntax effectiveGoverningType = node.GoverningType == null
            ? governingType
            : node.GoverningType;
        bool collectForOwner = node.Domain != null
            && ownerType.ConstraintDomain != null
            && node.Domain == ownerType.ConstraintDomain;
        ResolvedTypeSyntax constraintGoverningType = collectForOwner
            ? ownerType
            : effectiveGoverningType;
        if (!compiledNodes.Contains(node))
        {
            if (node.Kind == AsnConstraintKind.Type)
            {
                compiledNodes.Add(node);
                this.CompileTypeConstraint(
                    node,
                    constraintGoverningType,
                    collectForOwner,
                    typeConstraints,
                    constraintTypes);
            }
            else if (node.Kind == AsnConstraintKind.Contents)
            {
                compiledNodes.Add(node);
                this.CompileContentsConstraint(
                    node,
                    constraintGoverningType,
                    collectForOwner,
                    contentsConstraints,
                    constraintTypes);
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.CompileNode(
                node.Children[childIndex],
                ownerType,
                effectiveGoverningType,
                typeConstraints,
                contentsConstraints,
                constraintTypes,
                compiledNodes);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.CompileGraph(
                node.NestedConstraints[nestedIndex],
                ownerType,
                effectiveGoverningType,
                typeConstraints,
                contentsConstraints,
                constraintTypes,
                compiledNodes);
            nestedIndex = nestedIndex + 1;
        }
    }

    internal void CompileTypeConstraint(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        bool collectForOwner,
        System.Collections.Generic.List<NormalizedTypeConstraint> typeConstraints,
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes)
    {
        if (governingType.EffectiveKind != AsnTypeKind.OpenType)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidTypeConstraintType,
                "A type constraint can only be applied to an open type.",
                node.SourceSyntax == null ? null : node.SourceSyntax.Span);
            return;
        }

        ParsedTypeSyntax? syntax = node.SourceSyntax == null
            ? null
            : node.SourceSyntax.TypeSyntax;
        ResolvedTypeSyntax? constrainedType = this.ResolveConstraintType(
            syntax,
            governingType,
            node.SourceSyntax == null ? null : node.SourceSyntax.Span);
        if (constrainedType == null)
        {
            return;
        }

        NormalizedTypeConstraint constraint = new NormalizedTypeConstraint
        {
            GoverningType = governingType,
            ConstrainedType = constrainedType,
            SourceNode = node
        };
        node.CompiledTypeConstraint = constraint;
        if (collectForOwner)
        {
            typeConstraints.Add(constraint);
        }

        this.AddConstraintType(constraintTypes, constrainedType);
    }

    internal void CompileContentsConstraint(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        bool collectForOwner,
        System.Collections.Generic.List<InnerCodecMetadata> contentsConstraints,
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes)
    {
        if (governingType.EffectiveKind != AsnTypeKind.BitString
            && governingType.EffectiveKind != AsnTypeKind.OctetString)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidContentsConstraintType,
                "A contents constraint can only be applied to BIT STRING or OCTET STRING.",
                node.SourceGeneralConstraint == null
                    ? null
                    : node.SourceGeneralConstraint.Span);
            return;
        }

        if (governingType.EffectiveKind == AsnTypeKind.BitString
            && this.HasNamedBits(governingType))
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.ContentsConstraintNamedBits,
                "A contents constraint cannot be applied to a BIT STRING with named bits.",
                node.SourceGeneralConstraint == null
                    ? null
                    : node.SourceGeneralConstraint.Span);
            return;
        }

        ParsedGeneralConstraintSyntax? source = node.SourceGeneralConstraint;
        if (source == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.ContentsTypeResolution,
                "The contents constraint has no retained semantic syntax.",
                null);
            return;
        }

        ResolvedTypeSyntax? containedType = null;
        if (source.ContainedTypeSyntax != null)
        {
            containedType = this.ResolveConstraintType(
                source.ContainedTypeSyntax,
                governingType,
                source.Span);
            if (containedType == null)
            {
                return;
            }

            this.AddConstraintType(constraintTypes, containedType);
        }

        ResolvedValueSyntax? encodingValue = null;
        AsnObjectIdentifierValue? encodingIdentifier = null;
        if (source.EncodingValueSyntax != null)
        {
            encodingValue = this.ResolveEncodingValue(
                source.EncodingValueSyntax,
                governingType);
            encodingIdentifier = encodingValue == null
                ? null
                : encodingValue.ObjectIdentifierValue;
            if (encodingValue == null
                || !encodingValue.Success
                || encodingIdentifier == null)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.InvalidContentsEncodingIdentifier,
                    "ENCODED BY must resolve to a valid OBJECT IDENTIFIER value.",
                    source.EncodingValueSyntax.Span);
            }
        }

        InnerCodecMetadata metadata = new InnerCodecMetadata
        {
            ContainerType = governingType,
            ContainedType = containedType,
            EncodingValue = encodingValue,
            EncodingIdentifier = encodingIdentifier,
            SourceNode = node,
            HasEncodingIdentifier = source.EncodingValueSyntax != null,
            UsesContainerEncodingRules = containedType != null
                && source.EncodingValueSyntax == null,
            RequiresOctetAlignment = governingType.EffectiveKind == AsnTypeKind.OctetString
        };
        node.CompiledContentsConstraint = metadata;
        if (collectForOwner)
        {
            contentsConstraints.Add(metadata);
        }
    }

    internal ResolvedTypeSyntax? ResolveConstraintType(
        ParsedTypeSyntax? syntax,
        ResolvedTypeSyntax governingType,
        AsnSourceSpan? span)
    {
        SymbolTableEntry? declaration = governingType.SourceDeclaration == null
            ? governingType.EffectiveDeclaration
            : governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (syntax == null || declaration == null || module == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.ContentsTypeResolution,
                "The constrained or contained type could not be resolved in its module.",
                span);
            return null;
        }

        ResolvedTypeSyntax resolved = this.ValueResolver.TypeResolver.ResolveNestedType(
            syntax,
            module,
            declaration,
            true);
        if (!resolved.Success)
        {
            this.Success = false;
            return null;
        }

        ConstraintNormalizer normalizer = new ConstraintNormalizer();
        normalizer.NormalizeType(
            resolved,
            new System.Collections.Generic.List<ResolvedTypeSyntax>());
        normalizer.CombineTypeConstraints(
            resolved,
            new System.Collections.Generic.List<ResolvedTypeSyntax>());
        if (!this.ValueResolver.ResolveNamedValuesInType(
            resolved,
            new System.Collections.Generic.List<ResolvedTypeSyntax>()))
        {
            this.Success = false;
        }

        ComponentConstraintCompiler componentCompiler =
            new ComponentConstraintCompiler(this.Diagnostics);
        componentCompiler.CompileType(
            resolved,
            new System.Collections.Generic.List<ResolvedTypeSyntax>());
        if (!componentCompiler.Success)
        {
            this.Success = false;
        }

        return resolved;
    }

    internal ResolvedValueSyntax? ResolveEncodingValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax governingType)
    {
        SymbolTableEntry? declaration = governingType.SourceDeclaration == null
            ? governingType.EffectiveDeclaration
            : governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (declaration == null || module == null)
        {
            return null;
        }

        ParsedTypeSyntax objectIdentifierSyntax = new ParsedTypeSyntax
        {
            Type = new AsnType
            {
                Kind = AsnTypeKind.ObjectIdentifier
            }
        };
        ResolvedTypeSyntax objectIdentifierType =
            this.ValueResolver.TypeResolver.ResolveNestedType(
                objectIdentifierSyntax,
                module,
                declaration,
                true);
        return this.ValueResolver.ResolveNestedValueSyntax(
            syntax,
            objectIdentifierType,
            module,
            declaration);
    }

    internal void AddConstraintType(
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes,
        ResolvedTypeSyntax type)
    {
        if (!constraintTypes.Contains(type))
        {
            constraintTypes.Add(type);
        }
    }

    internal bool HasNamedBits(ResolvedTypeSyntax type)
    {
        ResolvedTypeSyntax canonical = type.ConstraintDomain == null
            || type.ConstraintDomain.CanonicalType == null
            ? type
            : type.ConstraintDomain.CanonicalType;
        return canonical.EffectiveType != null
            && canonical.EffectiveType.NamedValues.Length > 0;
    }

    internal int CountConstraintApplications(NormalizedConstraintGraph? graph)
    {
        if (graph == null)
        {
            return 0;
        }

        return graph.Applications.Length == 0 ? 1 : graph.Applications.Length;
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
