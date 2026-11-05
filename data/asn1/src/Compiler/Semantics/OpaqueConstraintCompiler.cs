namespace asn1;

internal class OpaqueConstraintCompiler
{
    internal ValueResolver ValueResolver;
    internal DiagnosticBag Diagnostics;
    internal bool Success = true;

    internal OpaqueConstraintCompiler(ValueResolver valueResolver)
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
        System.Collections.Generic.List<UserDefinedConstraintMetadata> userDefined =
            new System.Collections.Generic.List<UserDefinedConstraintMetadata>();
        System.Collections.Generic.List<ConstraintExceptionMetadata> exceptions =
            new System.Collections.Generic.List<ConstraintExceptionMetadata>();
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        int existingIndex = 0;
        while (existingIndex < type.ConstraintTypes.Length)
        {
            constraintTypes.Add(type.ConstraintTypes[existingIndex]);
            existingIndex = existingIndex + 1;
        }

        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        System.Collections.Generic.List<NormalizedConstraintGraph> compiledGraphs =
            new System.Collections.Generic.List<NormalizedConstraintGraph>();
        if (type.EffectiveConstraint != null)
        {
            this.CompileGraph(
                type.EffectiveConstraint,
                type,
                type,
                userDefined,
                exceptions,
                constraintTypes,
                compiledNodes,
                compiledGraphs);
        }

        type.UserDefinedConstraints = userDefined.ToArray();
        type.ConstraintExceptions = exceptions.ToArray();
        type.ConstraintTypes = constraintTypes.ToArray();
        type.RequiresApplicationValidation = userDefined.Count > 0;
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
        System.Collections.Generic.List<UserDefinedConstraintMetadata> userDefined,
        System.Collections.Generic.List<ConstraintExceptionMetadata> exceptions,
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes,
        System.Collections.Generic.List<NormalizedConstraintGraph> compiledGraphs)
    {
        if (graph.Applications.Length > 0)
        {
            int applicationIndex = 0;
            while (applicationIndex < graph.Applications.Length)
            {
                this.CompileGraph(
                    graph.Applications[applicationIndex],
                    ownerType,
                    governingType,
                    userDefined,
                    exceptions,
                    constraintTypes,
                    compiledNodes,
                    compiledGraphs);
                applicationIndex = applicationIndex + 1;
            }

            return;
        }

        if (compiledGraphs.Contains(graph))
        {
            return;
        }

        compiledGraphs.Add(graph);
        ResolvedTypeSyntax effectiveGoverningType = graph.GoverningType == null
            ? governingType
            : graph.GoverningType;
        ConstraintExceptionMetadata? exception = this.CompileException(
            graph,
            ownerType,
            effectiveGoverningType,
            exceptions,
            constraintTypes);
        if (graph.RootPartition != null && graph.RootPartition.Expression != null)
        {
            this.CompileNode(
                graph.RootPartition.Expression,
                ownerType,
                effectiveGoverningType,
                exception,
                userDefined,
                exceptions,
                constraintTypes,
                compiledNodes,
                compiledGraphs);
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            this.CompileNode(
                graph.ExtensionPartition.Expression,
                ownerType,
                effectiveGoverningType,
                exception,
                userDefined,
                exceptions,
                constraintTypes,
                compiledNodes,
                compiledGraphs);
        }
    }

    internal void CompileNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax ownerType,
        ResolvedTypeSyntax governingType,
        ConstraintExceptionMetadata? exception,
        System.Collections.Generic.List<UserDefinedConstraintMetadata> userDefined,
        System.Collections.Generic.List<ConstraintExceptionMetadata> exceptions,
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes,
        System.Collections.Generic.List<NormalizedConstraintGraph> compiledGraphs)
    {
        ResolvedTypeSyntax effectiveGoverningType = node.GoverningType == null
            ? governingType
            : node.GoverningType;
        bool collectForOwner = node.Domain != null
            && ownerType.ConstraintDomain != null
            && node.Domain == ownerType.ConstraintDomain;
        if (node.Kind == AsnConstraintKind.UserDefined
            && node.SourceGeneralConstraint != null
            && !compiledNodes.Contains(node))
        {
            compiledNodes.Add(node);
            UserDefinedConstraintMetadata metadata = this.CompileUserDefinedNode(
                node,
                effectiveGoverningType,
                exception);
            node.CompiledUserDefinedConstraint = metadata;
            if (collectForOwner)
            {
                metadata.GoverningType = ownerType;
                userDefined.Add(metadata);
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.CompileNode(
                node.Children[childIndex],
                ownerType,
                effectiveGoverningType,
                exception,
                userDefined,
                exceptions,
                constraintTypes,
                compiledNodes,
                compiledGraphs);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.CompileGraph(
                node.NestedConstraints[nestedIndex],
                ownerType,
                effectiveGoverningType,
                userDefined,
                exceptions,
                constraintTypes,
                compiledNodes,
                compiledGraphs);
            nestedIndex = nestedIndex + 1;
        }
    }

    internal UserDefinedConstraintMetadata CompileUserDefinedNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        ConstraintExceptionMetadata? exception)
    {
        ParsedGeneralConstraintSyntax source = node.SourceGeneralConstraint
            ?? new ParsedGeneralConstraintSyntax();
        SymbolTableEntry? declaration = governingType.SourceDeclaration == null
            ? governingType.EffectiveDeclaration
            : governingType.SourceDeclaration;
        ParsedParameterSyntax[] formalParameters = declaration == null
            ? []
            : this.ParseFormalParameters(declaration);
        UserDefinedConstraintParameter[] parameters =
            new UserDefinedConstraintParameter[source.Parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            parameters[index] = this.CreateUserDefinedParameter(
                index,
                source.Parameters[index],
                declaration);
            index = index + 1;
        }

        return new UserDefinedConstraintMetadata
        {
            GoverningType = governingType,
            SourceNode = node,
            BodyTokens = this.CopyTokens(source.BodyTokens),
            BodyText = this.JoinTokenText(source.BodyTokens),
            Parameters = parameters,
            AssignmentFormalParameters = formalParameters,
            Exception = exception,
            ValidationRequirement = "An application-defined validator is required for this CONSTRAINED BY body.",
            HasExecutableMeaning = false,
            RequiresApplicationValidation = true
        };
    }

    internal UserDefinedConstraintParameter CreateUserDefinedParameter(
        int ordinal,
        AsnToken[] tokens,
        SymbolTableEntry? declaration)
    {
        int colon = this.FindTopLevelColon(tokens);
        AsnToken[] governor = colon < 0
            ? []
            : this.CopyTokenRange(tokens, 0, colon);
        AsnToken[] actual = colon < 0
            ? this.CopyTokens(tokens)
            : this.CopyTokenRange(tokens, colon + 1, tokens.Length);
        string formalName = "";
        bool formal = false;
        if (actual.Length == 1
            && declaration != null
            && this.ContainsName(declaration.ParameterNames, actual[0].Text))
        {
            formal = true;
            formalName = actual[0].Text;
        }

        return new UserDefinedConstraintParameter
        {
            Ordinal = ordinal,
            SourceTokens = this.CopyTokens(tokens),
            GovernorTokens = governor,
            ActualTokens = actual,
            Text = this.JoinTokenText(tokens),
            GovernorText = this.JoinTokenText(governor),
            ActualText = this.JoinTokenText(actual),
            FormalParameterName = formalName,
            HasGovernor = colon >= 0,
            ReferencesFormalParameter = formal
        };
    }

    internal int FindTopLevelColon(AsnToken[] tokens)
    {
        int braceDepth = 0;
        int parenthesisDepth = 0;
        int bracketDepth = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            AsnToken token = tokens[index];
            if (token.Kind == AsnTokenKind.LeftBrace)
            {
                braceDepth = braceDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBrace)
            {
                braceDepth = braceDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.LeftParenthesis)
            {
                parenthesisDepth = parenthesisDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightParenthesis)
            {
                parenthesisDepth = parenthesisDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.LeftBracket
                || token.Kind == AsnTokenKind.LeftVersionBracket)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBracket
                || token.Kind == AsnTokenKind.RightVersionBracket)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.Colon
                && braceDepth == 0
                && parenthesisDepth == 0
                && bracketDepth == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal ParsedParameterSyntax[] ParseFormalParameters(SymbolTableEntry declaration)
    {
        AsnToken[] tokens = declaration.GetSemanticDeclarationTokens();
        if (tokens.Length < 3 || tokens[1].Kind != AsnTokenKind.LeftBrace)
        {
            return [];
        }

        int depth = 0;
        int end = 1;
        while (end < tokens.Length)
        {
            if (tokens[end].Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (tokens[end].Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    end = end + 1;
                    break;
                }
            }

            end = end + 1;
        }

        AsnToken[] parameterTokens = this.CopyTokenRange(tokens, 1, end);
        ParameterListParseResult parsed = new ParameterizedParser(
            parameterTokens,
            this.Diagnostics,
            false).ParseFormalParameterListSyntax();
        return parsed.Success ? parsed.Parameters : [];
    }

    internal ConstraintExceptionMetadata? CompileException(
        NormalizedConstraintGraph graph,
        ResolvedTypeSyntax ownerType,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<ConstraintExceptionMetadata> exceptions,
        System.Collections.Generic.List<ResolvedTypeSyntax> constraintTypes)
    {
        ParsedConstraintSyntax? source = graph.SourceSyntax;
        if (source == null
            || source.ExceptionValueSyntax == null
            || graph.Exception == null)
        {
            return null;
        }

        SymbolTableEntry? declaration = governingType.SourceDeclaration == null
            ? governingType.EffectiveDeclaration
            : governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (declaration == null || module == null)
        {
            this.Success = false;
            return null;
        }

        bool implicitInteger = source.ExceptionTypeSyntax == null;
        ParsedTypeSyntax exceptionTypeSyntax = source.ExceptionTypeSyntax
            ?? new ParsedTypeSyntax
            {
                Type = new AsnType { Kind = AsnTypeKind.Integer }
            };
        ResolvedTypeSyntax exceptionType = this.ValueResolver.TypeResolver.ResolveNestedType(
            exceptionTypeSyntax,
            module,
            declaration,
            true);
        ConstraintNormalizer normalizer = new ConstraintNormalizer();
        normalizer.NormalizeType(
            exceptionType,
            new System.Collections.Generic.List<ResolvedTypeSyntax>());
        normalizer.CombineTypeConstraints(
            exceptionType,
            new System.Collections.Generic.List<ResolvedTypeSyntax>());
        if (!this.ValueResolver.ResolveNamedValuesInType(
            exceptionType,
            new System.Collections.Generic.List<ResolvedTypeSyntax>()))
        {
            this.Success = false;
        }

        ResolvedValueSyntax exceptionValue = this.ValueResolver.ResolveNestedValueSyntax(
            source.ExceptionValueSyntax,
            exceptionType,
            module,
            declaration);
        if (!exceptionType.Success || !exceptionValue.Success)
        {
            this.Success = false;
        }

        this.AddConstraintType(constraintTypes, exceptionType);
        ConstraintExceptionMetadata metadata = new ConstraintExceptionMetadata
        {
            GoverningType = ownerType,
            ExceptionType = exceptionType,
            ExceptionValue = exceptionValue,
            SourceTypeSyntax = source.ExceptionTypeSyntax,
            SourceValueSyntax = source.ExceptionValueSyntax,
            SourceNode = graph.Exception,
            IdentifierText = this.FormatExceptionIdentifier(source.ExceptionValueSyntax),
            UsesImplicitIntegerType = implicitInteger,
            DefinedIdentifier = implicitInteger
                && source.ExceptionValueSyntax.Value.Kind == AsnValueKind.Defined
        };
        graph.CompiledException = metadata;
        exceptions.Add(metadata);
        source.Constraint.HasException = true;
        source.Constraint.ExceptionType = exceptionType.EffectiveType;
        source.Constraint.ExceptionValue = exceptionValue.EffectiveValue;
        return metadata;
    }

    internal string FormatExceptionIdentifier(ParsedValueSyntax syntax)
    {
        if (syntax.ReferenceName.Length > 0)
        {
            return syntax.ReferenceModule.Length == 0
                ? syntax.ReferenceName
                : syntax.ReferenceModule + "." + syntax.ReferenceName;
        }

        if (syntax.Value.HasIntegerValue)
        {
            return syntax.Value.IntegerValue.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return syntax.Value.TextValue;
    }

    internal void AddConstraintType(
        System.Collections.Generic.List<ResolvedTypeSyntax> types,
        ResolvedTypeSyntax type)
    {
        if (!types.Contains(type))
        {
            types.Add(type);
        }
    }

    internal bool ContainsName(string[] names, string expected)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnToken[] CopyTokens(AsnToken[] tokens)
    {
        return this.CopyTokenRange(tokens, 0, tokens.Length);
    }

    internal AsnToken[] CopyTokenRange(AsnToken[] tokens, int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = tokens[index];
            index = index + 1;
        }

        return result;
    }

    internal string JoinTokenText(AsnToken[] tokens)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < tokens.Length)
        {
            if (index > 0)
            {
                text.Append(" ");
            }

            text.Append(tokens[index].Text);
            index = index + 1;
        }

        return text.ToString();
    }
}
