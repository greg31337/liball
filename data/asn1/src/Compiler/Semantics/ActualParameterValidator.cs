namespace asn1;

internal class ActualParameterValidator
{
    internal ValueResolver ValueResolver;
    internal TypeResolver TypeResolver;
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];
    internal InformationObjectBinder ObjectBinder;
    internal ObjectSetEvaluator ObjectSetEvaluator;
    internal ParameterInstantiationEngine Instantiator;
    internal bool Success = true;
    internal bool CreateInstantiations = true;

    internal ActualParameterValidator(ValueResolver valueResolver)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.TypeResolver = valueResolver.TypeResolver;
        this.Diagnostics = valueResolver.Diagnostics;
        this.ObjectBinder = new InformationObjectBinder(valueResolver);
        this.ObjectSetEvaluator = new ObjectSetEvaluator(valueResolver, this.ObjectBinder);
        this.Instantiator = new ParameterInstantiationEngine(this);
    }

    internal bool ValidateReferences(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        this.ObjectBinder.Tables = tables;
        this.ObjectSetEvaluator.Tables = tables;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].Symbols.Length)
            {
                this.ValidateSymbolReferences(
                    tables[tableIndex].Symbols[symbolIndex],
                    tables[tableIndex]);
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal void ValidateSymbolReferences(
        SymbolTableEntry source,
        SymbolTable module)
    {
        System.Collections.Generic.List<ParsedParameterizedReferenceSyntax> references =
            new System.Collections.Generic.List<ParsedParameterizedReferenceSyntax>();
        AsnToken[] tokens = source.DeclarationTokens;
        int index = this.FindReferenceScanStart(source);
        while (index < tokens.Length)
        {
            this.ValidateParameterizedReferenceUse(tokens, index, module);
            ParameterizedTargetResolution target = this.ResolveReferenceTarget(
                tokens,
                index,
                module);
            if (target.Target != null)
            {
                ActualParameterTokenList actuals = this.SplitActualParameters(
                    tokens,
                    target.BraceIndex);
                if (actuals.Success)
                {
                    ParsedParameterizedReferenceSyntax reference =
                        this.ValidateReferenceArguments(
                            source,
                            module,
                            target,
                            actuals);
                    references.Add(reference);
                }
            }

            index = index + 1;
        }

        source.ParameterizedReferences = references.ToArray();
    }

    internal void ValidateParameterizedReferenceUse(
        AsnToken[] tokens,
        int index,
        SymbolTable module)
    {
        if (!this.IsReferenceToken(tokens[index])
            || (index > 0 && tokens[index - 1].Kind == AsnTokenKind.Dot))
        {
            return;
        }

        ParameterizedTargetResolution resolution = this.ResolveAnyReferenceTarget(
            tokens,
            index,
            module);
        SymbolTableEntry? target = resolution.Target;
        if (target == null)
        {
            return;
        }

        bool hasActuals = resolution.BraceIndex >= 0;
        if (target.Parameterized == hasActuals)
        {
            return;
        }

        this.AddDiagnostic(
            ActualParameterDiagnosticCodes.ParameterizedReferenceMisuse,
            target.Parameterized
                ? "The parameterized assignment '" + target.QualifiedName
                    + "' must be referenced with an actual-parameter list."
                : "The non-parameterized assignment '" + target.QualifiedName
                    + "' cannot be referenced with an actual-parameter list.",
            tokens[resolution.NameIndex].Span);
    }

    internal ParameterizedTargetResolution ResolveAnyReferenceTarget(
        AsnToken[] tokens,
        int index,
        SymbolTable module)
    {
        if (!this.IsReferenceToken(tokens[index]))
        {
            return new ParameterizedTargetResolution();
        }

        string moduleName = "";
        int nameIndex = index;
        if (index + 2 < tokens.Length
            && tokens[index + 1].Kind == AsnTokenKind.Dot
            && this.IsReferenceToken(tokens[index + 2]))
        {
            moduleName = tokens[index].Text;
            nameIndex = index + 2;
        }

        int braceIndex = nameIndex + 1 < tokens.Length
            && tokens[nameIndex + 1].Kind == AsnTokenKind.LeftBrace
            ? nameIndex + 1
            : -1;
        return new ParameterizedTargetResolution
        {
            Target = this.FindAnyTarget(
                module,
                moduleName,
                tokens[nameIndex].Text),
            ModuleName = moduleName,
            ReferenceName = tokens[nameIndex].Text,
            StartIndex = index,
            NameIndex = nameIndex,
            BraceIndex = braceIndex
        };
    }

    internal int FindReferenceScanStart(SymbolTableEntry source)
    {
        if (!source.Parameterized
            || source.DeclarationTokens.Length < 2
            || source.DeclarationTokens[1].Kind != AsnTokenKind.LeftBrace)
        {
            return 1;
        }

        int depth = 0;
        int index = 1;
        while (index < source.DeclarationTokens.Length)
        {
            if (source.DeclarationTokens[index].Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (source.DeclarationTokens[index].Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return index + 1;
                }
            }

            index = index + 1;
        }

        return source.DeclarationTokens.Length;
    }

    internal ParameterizedTargetResolution ResolveReferenceTarget(
        AsnToken[] tokens,
        int index,
        SymbolTable module)
    {
        if (!this.IsReferenceToken(tokens[index]))
        {
            return new ParameterizedTargetResolution();
        }

        string moduleName = "";
        int nameIndex = index;
        int braceIndex = index + 1;
        if (index + 3 < tokens.Length
            && tokens[index + 1].Kind == AsnTokenKind.Dot
            && this.IsReferenceToken(tokens[index + 2])
            && tokens[index + 3].Kind == AsnTokenKind.LeftBrace)
        {
            moduleName = tokens[index].Text;
            nameIndex = index + 2;
            braceIndex = index + 3;
        }
        else if (braceIndex >= tokens.Length
            || tokens[braceIndex].Kind != AsnTokenKind.LeftBrace)
        {
            return new ParameterizedTargetResolution();
        }

        SymbolTableEntry? target = this.FindParameterizedTarget(
            module,
            moduleName,
            tokens[nameIndex].Text);
        return new ParameterizedTargetResolution
        {
            Target = target,
            ModuleName = moduleName,
            ReferenceName = tokens[nameIndex].Text,
            StartIndex = index,
            NameIndex = nameIndex,
            BraceIndex = braceIndex
        };
    }

    internal SymbolTableEntry? FindParameterizedTarget(
        SymbolTable module,
        string moduleName,
        string name)
    {
        System.Collections.Generic.List<SymbolTableEntry> matches =
            new System.Collections.Generic.List<SymbolTableEntry>();
        if (moduleName.Length == 0 || moduleName == module.ModuleName)
        {
            int localIndex = 0;
            while (localIndex < module.Symbols.Length)
            {
                SymbolTableEntry local = module.Symbols[localIndex];
                if (local.Parameterized && local.Name == name)
                {
                    matches.Add(local);
                }

                localIndex = localIndex + 1;
            }
        }

        if (moduleName.Length == 0 && matches.Count > 0)
        {
            return matches.Count == 1 ? matches[0] : null;
        }

        int importIndex = 0;
        while (importIndex < module.ImportResolutions.Length)
        {
            ImportResolution import = module.ImportResolutions[importIndex];
            if (moduleName.Length == 0
                || (import.TargetModule != null
                    && import.TargetModule.ModuleName == moduleName))
            {
                int bindingIndex = 0;
                while (bindingIndex < import.Symbols.Length)
                {
                    ImportedSymbolBinding binding = import.Symbols[bindingIndex];
                    if (binding.Target != null
                        && binding.Target.Parameterized
                        && binding.Target.Name == name
                        && !matches.Contains(binding.Target))
                    {
                        matches.Add(binding.Target);
                    }

                    bindingIndex = bindingIndex + 1;
                }
            }

            importIndex = importIndex + 1;
        }

        return matches.Count == 1 ? matches[0] : null;
    }

    internal SymbolTableEntry? FindAnyTarget(
        SymbolTable module,
        string moduleName,
        string name)
    {
        System.Collections.Generic.List<SymbolTableEntry> matches =
            new System.Collections.Generic.List<SymbolTableEntry>();
        if (moduleName.Length == 0 || moduleName == module.ModuleName)
        {
            int localIndex = 0;
            while (localIndex < module.Symbols.Length)
            {
                SymbolTableEntry local = module.Symbols[localIndex];
                if (local.Name == name)
                {
                    matches.Add(local);
                }

                localIndex = localIndex + 1;
            }
        }

        if (moduleName.Length == 0 && matches.Count > 0)
        {
            return matches.Count == 1 ? matches[0] : null;
        }

        int importIndex = 0;
        while (importIndex < module.ImportResolutions.Length)
        {
            ImportResolution import = module.ImportResolutions[importIndex];
            if (moduleName.Length == 0
                || (import.TargetModule != null
                    && import.TargetModule.ModuleName == moduleName))
            {
                int bindingIndex = 0;
                while (bindingIndex < import.Symbols.Length)
                {
                    SymbolTableEntry? target = import.Symbols[bindingIndex].Target;
                    if (target != null
                        && target.Name == name
                        && !matches.Contains(target))
                    {
                        matches.Add(target);
                    }

                    bindingIndex = bindingIndex + 1;
                }
            }

            importIndex = importIndex + 1;
        }

        return matches.Count == 1 ? matches[0] : null;
    }

    internal ActualParameterTokenList SplitActualParameters(
        AsnToken[] tokens,
        int braceIndex)
    {
        if (braceIndex < 0
            || braceIndex >= tokens.Length
            || tokens[braceIndex].Kind != AsnTokenKind.LeftBrace)
        {
            return new ActualParameterTokenList();
        }

        System.Collections.Generic.List<AsnToken[]> items =
            new System.Collections.Generic.List<AsnToken[]>();
        int braceDepth = 1;
        int parenthesisDepth = 0;
        int bracketDepth = 0;
        int itemStart = braceIndex + 1;
        int index = itemStart;
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
                if (braceDepth == 0)
                {
                    if (index > itemStart)
                    {
                        items.Add(this.CopyTokenSlice(tokens, itemStart, index));
                    }

                    return new ActualParameterTokenList
                    {
                        Items = items.ToArray(),
                        EndIndex = index,
                        Success = true
                    };
                }
            }
            else if (token.Kind == AsnTokenKind.LeftParenthesis)
            {
                parenthesisDepth = parenthesisDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightParenthesis)
            {
                parenthesisDepth = parenthesisDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.LeftBracket)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBracket)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.Comma
                && braceDepth == 1
                && parenthesisDepth == 0
                && bracketDepth == 0)
            {
                items.Add(this.CopyTokenSlice(tokens, itemStart, index));
                itemStart = index + 1;
            }

            index = index + 1;
        }

        return new ActualParameterTokenList();
    }

    internal AsnToken[] CopyTokenSlice(AsnToken[] tokens, int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = tokens[start + index];
            index = index + 1;
        }

        return result;
    }

    internal ParsedParameterizedReferenceSyntax ValidateReferenceArguments(
        SymbolTableEntry source,
        SymbolTable module,
        ParameterizedTargetResolution resolution,
        ActualParameterTokenList tokens)
    {
        SymbolTableEntry target = resolution.Target
            ?? throw new System.InvalidOperationException();
        ParsedParameterSyntax[] formals = target.FormalParameterSyntax;
        ParsedActualParameterSyntax?[] parsed =
            new ParsedActualParameterSyntax?[tokens.Items.Length];
        AsnSourceSpan? span = this.CreateReferenceSpan(
            source.DeclarationTokens[resolution.NameIndex],
            source.DeclarationTokens[tokens.EndIndex]);
        if (tokens.Items.Length != formals.Length)
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.ArityMismatch,
                "The parameterized reference '" + target.QualifiedName
                    + "' requires " + formals.Length.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + " actual parameters, but " + tokens.Items.Length.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + " were supplied.",
                span);
            if (tokens.Items.Length < formals.Length)
            {
                int missingIndex = tokens.Items.Length;
                while (missingIndex < formals.Length)
                {
                    this.AddDiagnostic(
                        ActualParameterDiagnosticCodes.MissingActualParameter,
                        "The parameterized reference '" + target.QualifiedName
                            + "' is missing actual parameter '"
                            + formals[missingIndex].Parameter.Name + "'.",
                        span);
                    missingIndex = missingIndex + 1;
                }
            }
            else
            {
                int excessIndex = formals.Length;
                while (excessIndex < tokens.Items.Length)
                {
                    AsnToken[] excess = tokens.Items[excessIndex];
                    this.AddDiagnostic(
                        ActualParameterDiagnosticCodes.ExcessActualParameter,
                        "The parameterized reference '" + target.QualifiedName
                            + "' has an excess actual parameter at position "
                            + (excessIndex + 1).ToString(
                                System.Globalization.CultureInfo.InvariantCulture)
                            + ".",
                        excess.Length == 0 ? span : excess[0].Span);
                    excessIndex = excessIndex + 1;
                }
            }
        }

        int common = tokens.Items.Length < formals.Length
            ? tokens.Items.Length
            : formals.Length;
        int index = 0;
        while (index < common)
        {
            AsnParameterKind kind = formals[index].Parameter.Kind;
            if (kind == AsnParameterKind.Type
                || kind == AsnParameterKind.ObjectClass)
            {
                parsed[index] = this.ParseActualCategory(
                    tokens.Items[index],
                    formals[index],
                    target);
            }

            index = index + 1;
        }

        index = 0;
        while (index < common)
        {
            AsnParameterKind kind = formals[index].Parameter.Kind;
            if (kind != AsnParameterKind.Type
                && kind != AsnParameterKind.ObjectClass)
            {
                ParsedParameterSyntax? effective = this.CreateEffectiveFormal(
                    formals[index],
                    formals,
                    parsed,
                    module,
                    source);
                if (effective != null)
                {
                    parsed[index] = this.ParseActualCategory(
                        tokens.Items[index],
                        effective,
                        target);
                }
            }

            index = index + 1;
        }

        ParsedActualParameterSyntax[] actuals = this.CopyParsedActuals(parsed);
        ParsedParameterizedReferenceSyntax reference =
            new ParsedParameterizedReferenceSyntax
            {
                Kind = this.MapAssignmentKind(target.Kind),
                ReferenceModule = resolution.ModuleName,
                ReferenceName = resolution.ReferenceName,
                ActualParameterSyntax = actuals,
                ActualParameters = this.CopyPublicActuals(actuals),
                SourceTokens = this.CopyTokenSlice(
                    source.DeclarationTokens,
                    resolution.NameIndex,
                    tokens.EndIndex + 1),
                Target = target,
                Span = span
            };
        bool semanticSuccess = actuals.Length == common;
        if (semanticSuccess)
        {
            semanticSuccess = this.ValidateActualSemantics(
                reference,
                source,
                module,
                target,
                formals,
                parsed);
        }

        reference.Validated = semanticSuccess
            && tokens.Items.Length == formals.Length;
        if (reference.Validated)
        {
            SymbolTable? targetModule = this.ValueResolver.FindDeclaringModule(target);
            if (targetModule == null)
            {
                reference.Validated = false;
            }
            else
            {
                reference.Substitution = new ParameterizedSubstituter(this.Diagnostics)
                    .SubstituteReference(reference, module, source, targetModule);
                if (!reference.Substitution.Success)
                {
                    reference.Validated = false;
                    this.AddDiagnostic(
                        ActualParameterDiagnosticCodes.PostSubstitutionInvalidSchema,
                        "The actual parameters for '" + target.QualifiedName
                            + "' could not be substituted: "
                            + reference.Substitution.FailureMessage,
                        reference.Span);
                }
                else if (this.CreateInstantiations)
                {
                    reference.Instantiation = this.Instantiator.InstantiateReference(
                        reference,
                        module,
                        source,
                        targetModule);
                    if (!reference.Instantiation.Success)
                    {
                        reference.Validated = false;
                        int code = reference.Instantiation.FailureKind
                            == ParameterInstantiationFailureKind.Recursion
                            ? ActualParameterDiagnosticCodes.RecursiveInstantiation
                            : reference.Instantiation.FailureKind
                                == ParameterInstantiationFailureKind.InvalidSchema
                                ? ActualParameterDiagnosticCodes.PostSubstitutionInvalidSchema
                                : ActualParameterDiagnosticCodes.InstantiationFailure;
                        this.AddDiagnostic(
                            code,
                            "The parameterized reference '" + target.QualifiedName
                                + "' could not be instantiated: "
                                + reference.Instantiation.FailureMessage,
                            reference.Span);
                    }
                }
            }
        }

        return reference;
    }

    internal ParsedActualParameterSyntax? ParseActualCategory(
        AsnToken[] tokens,
        ParsedParameterSyntax formal,
        SymbolTableEntry target)
    {
        ParameterizedParser parser = new ParameterizedParser(
            tokens,
            this.Diagnostics,
            false);
        ParsedActualParameterSyntax? actual = parser.ParseActualParameterTokens(
            tokens,
            formal.Parameter.Kind,
            formal);
        if (actual != null)
        {
            return actual;
        }

        ParsedActualParameterSyntax? unclassified = new ParameterizedParser(
            tokens,
            this.Diagnostics,
            false).ParseUnclassifiedActual(tokens);
        bool candidate = unclassified != null
            && this.ContainsParameterKind(
                unclassified.CandidateKinds,
                formal.Parameter.Kind);
        this.AddDiagnostic(
            candidate
                ? ActualParameterDiagnosticCodes.GovernorMismatch
                : ActualParameterDiagnosticCodes.CategoryMismatch,
            candidate
                ? "An actual parameter for '" + target.QualifiedName
                    + "' does not satisfy the notation required by its governor."
                : "An actual parameter for '" + target.QualifiedName
                    + "' does not match the category of its formal parameter.",
            tokens.Length == 0 ? target.Span : tokens[0].Span);
        return null;
    }

    internal ParsedParameterSyntax? CreateEffectiveFormal(
        ParsedParameterSyntax formal,
        ParsedParameterSyntax[] formals,
        ParsedActualParameterSyntax?[] actuals,
        SymbolTable sourceModule,
        SymbolTableEntry source)
    {
        if (formal.DummyGovernor == null)
        {
            return formal;
        }

        int governorIndex = this.FindFormalParameterIndex(
            formals,
            formal.DummyGovernor);
        ParsedActualParameterSyntax? governorActual = governorIndex < 0
            || governorIndex >= actuals.Length
            ? null
            : actuals[governorIndex];
        if (governorActual == null)
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.GovernorMismatch,
                "The actual governor for formal parameter '"
                    + formal.Parameter.Name + "' could not be determined.",
                formal.Span);
            return null;
        }

        ParsedParameterSyntax effective = new ParsedParameterSyntax
        {
            Parameter = new AsnParameter
            {
                Name = formal.Parameter.Name,
                Kind = formal.Parameter.Kind
            },
            Span = formal.Span
        };
        if (formal.Parameter.Kind == AsnParameterKind.Value
            || formal.Parameter.Kind == AsnParameterKind.ValueSet)
        {
            ResolvedTypeSyntax? type = this.ResolveActualTypeGovernor(
                governorActual,
                sourceModule,
                source);
            if (type == null || !type.Success || type.EffectiveType == null)
            {
                this.AddDiagnostic(
                    ActualParameterDiagnosticCodes.GovernorMismatch,
                    "The actual governor for formal parameter '"
                        + formal.Parameter.Name + "' is not a resolvable type.",
                    governorActual.Span);
                return null;
            }

            effective.Parameter.GovernorType = type.EffectiveType;
            effective.GovernorTypeSyntax = governorActual.TypeSyntax;
        }
        else
        {
            ParsedObjectClassSyntax? objectClass = this.ResolveActualClassGovernor(
                governorActual,
                sourceModule);
            if (objectClass == null)
            {
                this.AddDiagnostic(
                    ActualParameterDiagnosticCodes.GovernorMismatch,
                    "The actual governor for formal parameter '"
                        + formal.Parameter.Name + "' is not a resolvable object class.",
                    governorActual.Span);
                return null;
            }

            effective.Parameter.GovernorObjectClass = objectClass.ObjectClass;
            effective.GovernorObjectClassSyntax = objectClass;
        }

        return effective;
    }

    internal int FindFormalParameterIndex(
        ParsedParameterSyntax[] formals,
        ParsedParameterSyntax formal)
    {
        int index = 0;
        while (index < formals.Length)
        {
            if (formals[index] == formal)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal ParsedActualParameterSyntax[] CopyParsedActuals(
        ParsedActualParameterSyntax?[] parsed)
    {
        int count = 0;
        int index = 0;
        while (index < parsed.Length)
        {
            if (parsed[index] != null)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        ParsedActualParameterSyntax[] result =
            new ParsedActualParameterSyntax[count];
        int resultIndex = 0;
        index = 0;
        while (index < parsed.Length)
        {
            if (parsed[index] != null)
            {
                result[resultIndex] = parsed[index]
                    ?? throw new System.InvalidOperationException();
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal AsnActualParameter[] CopyPublicActuals(
        ParsedActualParameterSyntax[] actuals)
    {
        AsnActualParameter[] result = new AsnActualParameter[actuals.Length];
        int index = 0;
        while (index < actuals.Length)
        {
            result[index] = actuals[index].Parameter;
            index = index + 1;
        }

        return result;
    }

    internal bool ValidateActualSemantics(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTableEntry source,
        SymbolTable sourceModule,
        SymbolTableEntry target,
        ParsedParameterSyntax[] formals,
        ParsedActualParameterSyntax?[] actuals)
    {
        SymbolTable? targetModule = this.ValueResolver.FindDeclaringModule(target);
        if (targetModule == null)
        {
            return false;
        }

        bool success = true;
        int index = 0;
        while (index < formals.Length && index < actuals.Length)
        {
            ParsedActualParameterSyntax? actual = actuals[index];
            if (actual != null)
            {
                if (!this.ValidateOneActual(
                    formals[index],
                    formals,
                    actuals,
                    actual,
                    source,
                    sourceModule,
                    target,
                    targetModule,
                    reference))
                {
                    success = false;
                }
            }

            index = index + 1;
        }

        return success;
    }

    internal bool ValidateOneActual(
        ParsedParameterSyntax formal,
        ParsedParameterSyntax[] formals,
        ParsedActualParameterSyntax?[] allActuals,
        ParsedActualParameterSyntax actual,
        SymbolTableEntry source,
        SymbolTable sourceModule,
        SymbolTableEntry target,
        SymbolTable targetModule,
        ParsedParameterizedReferenceSyntax reference)
    {
        if (formal.Parameter.Kind == AsnParameterKind.Type)
        {
            ResolvedTypeSyntax? type = this.ResolveActualTypeGovernor(
                actual,
                sourceModule,
                source);
            return type != null && type.Success;
        }

        if (formal.Parameter.Kind == AsnParameterKind.ObjectClass)
        {
            return this.ResolveActualClassGovernor(actual, sourceModule) != null;
        }

        if (formal.Parameter.Kind == AsnParameterKind.Value
            || formal.Parameter.Kind == AsnParameterKind.ValueSet)
        {
            ResolvedTypeSyntax? governor = this.ResolveFormalTypeGovernor(
                formal,
                formals,
                allActuals,
                source,
                sourceModule,
                target,
                targetModule);
            if (governor == null || !governor.Success)
            {
                this.AddDiagnostic(
                    ActualParameterDiagnosticCodes.GovernorMismatch,
                    "The governing type for actual parameter '"
                        + formal.Parameter.Name + "' could not be resolved.",
                    actual.Span);
                return false;
            }

            return formal.Parameter.Kind == AsnParameterKind.Value
                ? this.ValidateValueArgument(
                    actual,
                    governor,
                    sourceModule,
                    source)
                : this.ValidateValueSetArgument(
                    actual,
                    governor,
                    source);
        }

        ParsedObjectClassSyntax? objectClass = this.ResolveFormalClassGovernor(
            formal,
            formals,
            allActuals,
            sourceModule,
            targetModule);
        if (objectClass == null)
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.GovernorMismatch,
                "The governing object class for actual parameter '"
                    + formal.Parameter.Name + "' could not be resolved.",
                actual.Span);
            return false;
        }

        return formal.Parameter.Kind == AsnParameterKind.Object
            ? this.ValidateObjectArgument(
                actual,
                objectClass,
                sourceModule,
                source)
            : this.ValidateObjectSetArgument(
                actual,
                objectClass,
                sourceModule,
                source,
                reference);
    }

    internal ResolvedTypeSyntax? ResolveActualTypeGovernor(
        ParsedActualParameterSyntax actual,
        SymbolTable module,
        SymbolTableEntry source)
    {
        if (actual.TypeSyntax == null)
        {
            return null;
        }

        ResolvedTypeSyntax type = this.TypeResolver.ResolveNestedType(
            actual.TypeSyntax,
            module,
            source,
            true);
        this.PrepareStandaloneType(type);
        return type;
    }

    internal ParsedObjectClassSyntax? ResolveActualClassGovernor(
        ParsedActualParameterSyntax actual,
        SymbolTable module)
    {
        return actual.ObjectClassSyntax == null
            ? null
            : this.ResolveObjectClassReference(actual.ObjectClassSyntax, module);
    }

    internal ResolvedTypeSyntax? ResolveFormalTypeGovernor(
        ParsedParameterSyntax formal,
        ParsedParameterSyntax[] formals,
        ParsedActualParameterSyntax?[] actuals,
        SymbolTableEntry source,
        SymbolTable sourceModule,
        SymbolTableEntry target,
        SymbolTable targetModule)
    {
        if (formal.DummyGovernor != null)
        {
            int index = this.FindFormalParameterIndex(
                formals,
                formal.DummyGovernor);
            if (index < 0 || index >= actuals.Length || actuals[index] == null)
            {
                return null;
            }

            return this.ResolveActualTypeGovernor(
                actuals[index] ?? throw new System.InvalidOperationException(),
                sourceModule,
                source);
        }

        if (formal.GovernorTypeSyntax == null)
        {
            return null;
        }

        ResolvedTypeSyntax type = this.TypeResolver.ResolveNestedType(
            formal.GovernorTypeSyntax,
            targetModule,
            target,
            true);
        this.PrepareStandaloneType(type);
        return type;
    }

    internal ParsedObjectClassSyntax? ResolveFormalClassGovernor(
        ParsedParameterSyntax formal,
        ParsedParameterSyntax[] formals,
        ParsedActualParameterSyntax?[] actuals,
        SymbolTable sourceModule,
        SymbolTable targetModule)
    {
        if (formal.DummyGovernor != null)
        {
            int index = this.FindFormalParameterIndex(
                formals,
                formal.DummyGovernor);
            if (index < 0 || index >= actuals.Length || actuals[index] == null)
            {
                return null;
            }

            return this.ResolveActualClassGovernor(
                actuals[index] ?? throw new System.InvalidOperationException(),
                sourceModule);
        }

        return formal.GovernorObjectClassSyntax == null
            ? null
            : this.ResolveObjectClassReference(
                formal.GovernorObjectClassSyntax,
                targetModule);
    }

    internal ParsedObjectClassSyntax? ResolveObjectClassReference(
        ParsedObjectClassSyntax syntax,
        SymbolTable module)
    {
        if (syntax.ParameterizedReference != null)
        {
            return null;
        }

        if (syntax.ReferenceName.Length == 0)
        {
            return syntax;
        }

        ParsedObjectClassSyntax? useful = this.ValueResolver.UsefulObjectClasses
            .ResolveUsefulClass(syntax.ReferenceModule, syntax.ReferenceName);
        if (useful != null)
        {
            return useful;
        }

        SymbolTableEntry? symbol = this.ObjectBinder.FindSymbol(
            module,
            syntax.ReferenceModule,
            syntax.ReferenceName,
            AsnAssignmentKind.ObjectClass);
        return symbol == null ? null : symbol.ResolvedObjectClassSyntax;
    }

    internal void PrepareStandaloneType(ResolvedTypeSyntax type)
    {
        if (type.ConstraintDomain == null)
        {
            ConstraintNormalizer normalizer = new ConstraintNormalizer();
            normalizer.NormalizeType(
                type,
                new System.Collections.Generic.List<ResolvedTypeSyntax>());
            normalizer.CombineTypeConstraints(
                type,
                new System.Collections.Generic.List<ResolvedTypeSyntax>());
        }

        new ConstraintSolver(this.ValueResolver).SolveType(type);
        new ConstraintValidator(this.ValueResolver).ValidateType(type);
    }

    internal bool ValidateValueArgument(
        ParsedActualParameterSyntax actual,
        ResolvedTypeSyntax governor,
        SymbolTable module,
        SymbolTableEntry source)
    {
        if (actual.ValueSyntax == null)
        {
            return false;
        }

        ResolvedValueSyntax value = this.ValueResolver.ResolveNestedValueSyntax(
            actual.ValueSyntax,
            governor,
            module,
            source);
        if (!value.Success || value.EffectiveValue == null)
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.GovernorMismatch,
                "The actual value cannot be resolved under its formal governor.",
                actual.Span);
            return false;
        }

        ResolvedTypeSyntax? referenceType = value.ReferenceTarget == null
            ? null
            : value.ReferenceTarget.GoverningType;
        if (referenceType != null
            && !this.AreTypesCompatible(governor, referenceType))
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.TypeCompatibility,
                "The actual value's governing type is incompatible with the formal governor.",
                actual.Span);
            return false;
        }

        AsnRuntimeValue runtime = this.CreateRuntimeValue(value);
        RuntimeConstraintEvaluationResult constraint =
            new RuntimeConstraintEvaluator().EvaluateResolvedTree(
                runtime,
                governor,
                "$actual");
        if (!constraint.Success)
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.ConstraintViolation,
                "The actual value does not satisfy the formal governor's constraints.",
                actual.Span);
            return false;
        }

        return true;
    }

    internal bool ValidateValueSetArgument(
        ParsedActualParameterSyntax actual,
        ResolvedTypeSyntax governor,
        SymbolTableEntry source)
    {
        if (actual.ValueSetSyntax == null || governor.EffectiveType == null)
        {
            return false;
        }

        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = governor.EffectiveType,
            ConstraintSyntax = [actual.ValueSetSyntax],
            Span = actual.Span
        };
        ResolvedTypeSyntax subtype = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = syntax.Type,
            EffectiveType = governor.EffectiveType,
            EffectiveKind = governor.EffectiveKind,
            SourceDeclaration = source,
            EffectiveDeclaration = source,
            AliasTarget = governor,
            Success = true
        };
        syntax.Resolution = subtype;
        ConstraintNormalizer normalizer = new ConstraintNormalizer();
        normalizer.NormalizeType(
            subtype,
            new System.Collections.Generic.List<ResolvedTypeSyntax>());
        normalizer.CombineTypeConstraints(
            subtype,
            new System.Collections.Generic.List<ResolvedTypeSyntax>());
        ConstraintSolver solver = new ConstraintSolver(this.ValueResolver);
        solver.SolveType(subtype);
        ConstraintValidator validator = new ConstraintValidator(this.ValueResolver);
        validator.ValidateType(subtype);
        if (!validator.Success
            || !subtype.ConstraintValidationSuccess
            || subtype.EffectiveConstraintEmpty)
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.ConstraintViolation,
                "The actual value set is not a valid subtype of the formal governor.",
                actual.Span);
            return false;
        }

        return true;
    }

    internal bool ValidateObjectArgument(
        ParsedActualParameterSyntax actual,
        ParsedObjectClassSyntax governor,
        SymbolTable module,
        SymbolTableEntry source)
    {
        ParsedInformationObjectSyntax? syntax = actual.ObjectSyntax;
        if (syntax == null)
        {
            return false;
        }

        if (syntax.ReferenceName.Length > 0
            && syntax.ParameterizedReference == null)
        {
            SymbolTableEntry? symbol = this.ObjectBinder.FindSymbol(
                module,
                syntax.ReferenceModule,
                syntax.ReferenceName,
                AsnAssignmentKind.Object);
            ParsedInformationObjectSyntax? resolved = symbol == null
                ? null
                : symbol.DeclaredObjectSyntax;
            if (resolved == null
                || resolved.Object.ObjectClass != governor.ObjectClass)
            {
                this.AddDiagnostic(
                    ActualParameterDiagnosticCodes.ObjectClassCompatibility,
                    "The actual object is governed by an incompatible object class.",
                    actual.Span);
                return false;
            }

            return true;
        }

        if (!this.ObjectBinder.BindObjectValue(
            syntax,
            module,
            source,
            governor))
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.ConstraintViolation,
                "The actual object does not satisfy its governing object class.",
                actual.Span);
            return false;
        }

        syntax.Object.ObjectClass = governor.ObjectClass;
        return true;
    }

    internal bool ValidateObjectSetArgument(
        ParsedActualParameterSyntax actual,
        ParsedObjectClassSyntax governor,
        SymbolTable module,
        SymbolTableEntry source,
        ParsedParameterizedReferenceSyntax reference)
    {
        ParsedInformationObjectSetSyntax? syntax = actual.ObjectSetSyntax;
        if (syntax == null)
        {
            return false;
        }

        ParsedObjectSetElementSyntax? root = syntax.RootElement;
        if (root != null
            && root.Kind == "ObjectSetReference"
            && root.ReferenceName.Length > 0)
        {
            if (this.IsCompatibleFormalObjectSet(
                source,
                root.ReferenceName,
                governor,
                module))
            {
                syntax.ObjectSet.ObjectClass = governor.ObjectClass;
                return true;
            }

            SymbolTableEntry? symbol = this.ObjectBinder.FindSymbol(
                module,
                root.ReferenceModule,
                root.ReferenceName,
                AsnAssignmentKind.ObjectSet);
            ParsedInformationObjectSetSyntax? resolved = symbol == null
                ? null
                : symbol.DeclaredObjectSetSyntax;
            if (resolved == null
                || resolved.ObjectSet.ObjectClass != governor.ObjectClass)
            {
                this.AddDiagnostic(
                    ActualParameterDiagnosticCodes.ObjectClassCompatibility,
                    "The actual object set is governed by an incompatible object class.",
                    actual.Span);
                return false;
            }

            return true;
        }

        syntax.ObjectSet.ObjectClass = governor.ObjectClass;
        string prefix = "actual-object-set:"
            + (reference.Span == null
                ? source.QualifiedName
                : reference.Span.Start.SourceName + ":"
                    + reference.Span.Start.Offset.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
        if (!this.ObjectSetEvaluator.EvaluateInlineSet(
            syntax,
            module,
            source,
            governor,
            prefix))
        {
            this.AddDiagnostic(
                ActualParameterDiagnosticCodes.ConstraintViolation,
                "The actual object set does not satisfy its governing object class.",
                actual.Span);
            return false;
        }

        return true;
    }

    internal bool IsCompatibleFormalObjectSet(
        SymbolTableEntry source,
        string name,
        ParsedObjectClassSyntax governor,
        SymbolTable module)
    {
        int index = 0;
        while (index < source.FormalParameters.Length)
        {
            AsnParameter parameter = source.FormalParameters[index];
            ParsedParameterSyntax? syntax = index < source.FormalParameterSyntax.Length
                ? source.FormalParameterSyntax[index]
                : null;
            ParsedObjectClassSyntax? formalGovernor = syntax == null
                || syntax.GovernorObjectClassSyntax == null
                ? null
                : this.ResolveObjectClassReference(
                    syntax.GovernorObjectClassSyntax,
                    module);
            if (parameter.Name == name
                && parameter.Kind == AsnParameterKind.ObjectSet
                && formalGovernor != null
                && formalGovernor.ObjectClass == governor.ObjectClass)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AreTypesCompatible(
        ResolvedTypeSyntax expected,
        ResolvedTypeSyntax actual)
    {
        if (expected.EffectiveType == actual.EffectiveType)
        {
            return true;
        }

        return expected.EffectiveKind != AsnTypeKind.Unknown
            && expected.EffectiveKind == actual.EffectiveKind;
    }

    internal AsnRuntimeValue CreateRuntimeValue(ResolvedValueSyntax value)
    {
        AsnValue source = value.EffectiveValue ?? new AsnValue();
        AsnRuntimeValue result = new AsnRuntimeValue
        {
            Kind = this.MapRuntimeKind(source.Kind),
            Value = source,
            HasIntegerValue = source.HasIntegerValue,
            IntegerValue = source.IntegerValue,
            TextValue = source.TextValue,
            BitStringValue = value.BitStringValue,
            Octets = value.Octets,
            ObjectIdentifierValue = value.ObjectIdentifierValue,
            RealValue = source.RealValue,
            TimeValue = source.TimeValue
        };
        AsnRuntimeValue[] items = new AsnRuntimeValue[value.Items.Length];
        int index = 0;
        while (index < items.Length)
        {
            items[index] = this.CreateRuntimeValue(value.Items[index]);
            index = index + 1;
        }

        result.Items = items;
        AsnRuntimeFieldValue[] fields =
            new AsnRuntimeFieldValue[value.ComponentValues.Length];
        index = 0;
        while (index < fields.Length)
        {
            ResolvedComponentValueSyntax component = value.ComponentValues[index];
            fields[index] = new AsnRuntimeFieldValue
            {
                Name = component.Name,
                Present = component.Present,
                PresentDefault = component.PresentWithDefaultValue,
                Value = component.Value == null
                    ? null
                    : this.CreateRuntimeValue(component.Value)
            };
            index = index + 1;
        }

        result.Fields = fields;
        return result;
    }

    internal AsnRuntimeValueKind MapRuntimeKind(AsnValueKind kind)
    {
        switch (kind)
        {
            case AsnValueKind.Boolean:
                return AsnRuntimeValueKind.Boolean;
            case AsnValueKind.Integer:
                return AsnRuntimeValueKind.Integer;
            case AsnValueKind.Enumerated:
                return AsnRuntimeValueKind.Enumerated;
            case AsnValueKind.Real:
                return AsnRuntimeValueKind.Real;
            case AsnValueKind.BitString:
                return AsnRuntimeValueKind.BitString;
            case AsnValueKind.OctetString:
                return AsnRuntimeValueKind.OctetString;
            case AsnValueKind.Null:
                return AsnRuntimeValueKind.Null;
            case AsnValueKind.CharacterString:
                return AsnRuntimeValueKind.CharacterString;
            case AsnValueKind.ObjectIdentifier:
                return AsnRuntimeValueKind.ObjectIdentifier;
            case AsnValueKind.RelativeObjectIdentifier:
                return AsnRuntimeValueKind.RelativeObjectIdentifier;
            case AsnValueKind.ObjectIdentifierIri:
                return AsnRuntimeValueKind.ObjectIdentifierIri;
            case AsnValueKind.RelativeObjectIdentifierIri:
                return AsnRuntimeValueKind.RelativeObjectIdentifierIri;
            case AsnValueKind.Sequence:
                return AsnRuntimeValueKind.Sequence;
            case AsnValueKind.Set:
                return AsnRuntimeValueKind.Set;
            case AsnValueKind.SequenceOf:
                return AsnRuntimeValueKind.SequenceOf;
            case AsnValueKind.SetOf:
                return AsnRuntimeValueKind.SetOf;
            case AsnValueKind.Choice:
                return AsnRuntimeValueKind.Choice;
            case AsnValueKind.OpenType:
                return AsnRuntimeValueKind.OpenType;
            case AsnValueKind.External:
                return AsnRuntimeValueKind.External;
            case AsnValueKind.EmbeddedPdv:
                return AsnRuntimeValueKind.EmbeddedPdv;
            case AsnValueKind.InstanceOf:
                return AsnRuntimeValueKind.InstanceOf;
            case AsnValueKind.Time:
                return AsnRuntimeValueKind.Time;
            default:
                return AsnRuntimeValueKind.Unknown;
        }
    }

    internal AsnParameterKind MapAssignmentKind(AsnAssignmentKind kind)
    {
        switch (kind)
        {
            case AsnAssignmentKind.Type:
                return AsnParameterKind.Type;
            case AsnAssignmentKind.Value:
                return AsnParameterKind.Value;
            case AsnAssignmentKind.ValueSet:
                return AsnParameterKind.ValueSet;
            case AsnAssignmentKind.ObjectClass:
                return AsnParameterKind.ObjectClass;
            case AsnAssignmentKind.Object:
                return AsnParameterKind.Object;
            case AsnAssignmentKind.ObjectSet:
                return AsnParameterKind.ObjectSet;
            default:
                return AsnParameterKind.Unknown;
        }
    }

    internal bool ContainsParameterKind(
        AsnParameterKind[] kinds,
        AsnParameterKind expected)
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

    internal AsnSourceSpan? CreateReferenceSpan(AsnToken first, AsnToken last)
    {
        if (first.Span == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = first.Span.Start,
            End = last.Span.End
        };
    }

    internal void AddDiagnostic(int code, string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            code,
            message,
            span,
            null);
        this.Success = false;
    }
}
