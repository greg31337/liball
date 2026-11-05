namespace asn1;

internal class ParameterizedSubstituter
{
    internal DiagnosticBag Diagnostics;

    internal ParameterizedSubstituter(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal ParameterSubstitutionResult SubstituteReference(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTable actualModule,
        SymbolTableEntry actualDeclaration,
        SymbolTable targetModule)
    {
        if (reference == null)
        {
            throw new System.ArgumentNullException("reference");
        }

        if (actualModule == null)
        {
            throw new System.ArgumentNullException("actualModule");
        }

        if (actualDeclaration == null)
        {
            throw new System.ArgumentNullException("actualDeclaration");
        }

        if (targetModule == null)
        {
            throw new System.ArgumentNullException("targetModule");
        }

        SymbolTableEntry? target = reference.Target;
        if (!reference.Validated || target == null)
        {
            return new ParameterSubstitutionResult
            {
                FailureMessage = "Only a validated parameterized reference can be substituted."
            };
        }

        if (target.FormalParameterSyntax.Length
            != reference.ActualParameterSyntax.Length)
        {
            return new ParameterSubstitutionResult
            {
                Kind = target.Kind,
                FailureMessage = "The formal and actual parameter counts differ."
            };
        }

        int assignmentIndex = this.FindAssignmentTokenIndex(target.DeclarationTokens);
        if (assignmentIndex < 0)
        {
            return new ParameterSubstitutionResult
            {
                Kind = target.Kind,
                FailureMessage = "The parameterized assignment has no assignment operator."
            };
        }

        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        System.Collections.Generic.List<SubstitutionTokenOrigin> origins =
            new System.Collections.Generic.List<SubstitutionTokenOrigin>();
        int sourceEnd = this.FindDeclarationTokenEnd(target.DeclarationTokens);
        int index = 0;
        while (index < sourceEnd)
        {
            AsnToken sourceToken = target.DeclarationTokens[index];
            SubstitutionFormalMatch match = index > assignmentIndex
                ? this.FindFormalReferenceMatch(
                    sourceToken,
                    target.FormalParameterSyntax,
                    reference.ActualParameterSyntax)
                : new SubstitutionFormalMatch();
            if (match.Formal == null || match.Actual == null)
            {
                this.AppendTemplateToken(
                    tokens,
                    origins,
                    sourceToken,
                    targetModule,
                    target);
            }
            else
            {
                this.AppendActualTokens(
                    tokens,
                    origins,
                    match,
                    actualModule,
                    actualDeclaration);
            }

            index = index + 1;
        }

        return this.ParseSubstitutedAssignment(
            tokens.ToArray(),
            origins.ToArray(),
            target,
            targetModule);
    }

    internal ParameterSubstitutionResult ParseSubstitutedAssignment(
        AsnToken[] substitutedTokens,
        SubstitutionTokenOrigin[] origins,
        SymbolTableEntry target,
        SymbolTable targetModule)
    {
        int assignmentIndex = this.FindAssignmentTokenIndex(substitutedTokens);
        ParameterizedAssignmentParseResult parsed = new ParameterizedParser(
            substitutedTokens,
            this.Diagnostics,
            false).ParseParameterizedAssignmentSyntax(target.Kind);
        bool success = parsed.Success
            && parsed.Syntax != null
            && parsed.ConsumedTokenCount == substitutedTokens.Length;
        if (success && parsed.Syntax != null
            && (target.Kind == AsnAssignmentKind.Object
                || target.Kind == AsnAssignmentKind.ObjectSet))
        {
            success = this.ReparseGovernedInformationAssignment(
                parsed.Syntax,
                target,
                targetModule,
                substitutedTokens,
                assignmentIndex);
        }

        return new ParameterSubstitutionResult
        {
            Kind = target.Kind,
            Tokens = substitutedTokens,
            Origins = origins,
            AssignmentSyntax = parsed.Syntax,
            FailureMessage = success
                ? ""
                : "The substituted assignment is not valid for its ASN.1 category; "
                    + parsed.ConsumedTokenCount.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + " of " + substitutedTokens.Length.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + " tokens were consumed.",
            Success = success
        };
    }

    internal bool ReparseGovernedInformationAssignment(
        ParsedParameterizedAssignmentSyntax assignment,
        SymbolTableEntry target,
        SymbolTable targetModule,
        AsnToken[] tokens,
        int assignmentIndex)
    {
        ParsedObjectClassSyntax? governor = this.ResolveSubstitutionObjectClass(
            assignment.GovernorObjectClassSyntax,
            targetModule);
        if (governor == null)
        {
            return assignment.GovernorObjectClassSyntax != null;
        }

        AsnToken[] rightSide = this.CopySubstitutionRightSide(
            tokens,
            assignmentIndex + 1);
        if (target.Kind == AsnAssignmentKind.Object)
        {
            InformationObjectParseResult parsed = new InformationObjectParser(
                rightSide,
                this.Diagnostics,
                false).ParseObjectSyntax(governor);
            if (!parsed.Success
                || parsed.Syntax == null
                || parsed.ConsumedTokenCount != rightSide.Length)
            {
                return false;
            }

            assignment.ObjectSyntax = parsed.Syntax;
            assignment.Assignment.Object = parsed.Syntax.Object;
            assignment.GovernorObjectClassSyntax = governor;
            assignment.Assignment.ObjectClass = governor.ObjectClass;
            return true;
        }

        InformationObjectSetParseResult objectSet = new InformationObjectParser(
            rightSide,
            this.Diagnostics,
            false).ParseObjectSetSyntax(governor);
        if (!objectSet.Success
            || objectSet.Syntax == null
            || objectSet.ConsumedTokenCount != rightSide.Length)
        {
            return false;
        }

        assignment.ObjectSetSyntax = objectSet.Syntax;
        assignment.Assignment.ObjectSet = objectSet.Syntax.ObjectSet;
        assignment.GovernorObjectClassSyntax = governor;
        assignment.Assignment.ObjectClass = governor.ObjectClass;
        return true;
    }

    internal ParsedObjectClassSyntax? ResolveSubstitutionObjectClass(
        ParsedObjectClassSyntax? syntax,
        SymbolTable module)
    {
        if (syntax == null || syntax.ReferenceName.Length == 0)
        {
            return syntax;
        }

        SymbolTableEntry? symbol = this.FindSubstitutionObjectClassSymbol(
            module,
            syntax.ReferenceModule,
            syntax.ReferenceName);
        if (symbol == null)
        {
            return syntax;
        }

        if (symbol.ResolvedObjectClassSyntax != null)
        {
            return symbol.ResolvedObjectClassSyntax;
        }

        int assignmentIndex = this.FindAssignmentTokenIndex(symbol.DeclarationTokens);
        if (assignmentIndex < 0)
        {
            return syntax;
        }

        AsnToken[] rightSide = this.CopySubstitutionRightSide(
            symbol.DeclarationTokens,
            assignmentIndex + 1);
        ObjectClassParseResult parsed = new ObjectClassParser(
            rightSide,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        return parsed.Success
            && parsed.Syntax != null
            && parsed.ConsumedTokenCount == rightSide.Length
            ? parsed.Syntax
            : syntax;
    }

    internal SymbolTableEntry? FindSubstitutionObjectClassSymbol(
        SymbolTable module,
        string moduleName,
        string name)
    {
        if (moduleName.Length == 0 || moduleName == module.ModuleName)
        {
            SymbolTableEntry[] local = module.FindSymbols(
                name,
                AsnAssignmentKind.ObjectClass);
            if (local.Length == 1)
            {
                return local[0];
            }

            if (moduleName.Length == 0 && local.Length > 0)
            {
                return null;
            }
        }

        SymbolTableEntry? result = null;
        int count = 0;
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
                        && binding.Target.Name == name
                        && binding.Kind == AsnAssignmentKind.ObjectClass)
                    {
                        result = binding.Target;
                        count = count + 1;
                    }

                    bindingIndex = bindingIndex + 1;
                }
            }

            importIndex = importIndex + 1;
        }

        return count == 1 ? result : null;
    }

    internal AsnToken[] CopySubstitutionRightSide(AsnToken[] tokens, int start)
    {
        int end = this.FindDeclarationTokenEnd(tokens);
        AsnToken[] result = new AsnToken[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = tokens[start + index];
            index = index + 1;
        }

        return result;
    }

    internal int FindAssignmentTokenIndex(AsnToken[] tokens)
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

        return -1;
    }

    internal int FindDeclarationTokenEnd(AsnToken[] tokens)
    {
        int end = tokens.Length;
        while (end > 0 && tokens[end - 1].Kind == AsnTokenKind.Semicolon)
        {
            end = end - 1;
        }

        return end;
    }

    internal SubstitutionFormalMatch FindFormalReferenceMatch(
        AsnToken token,
        ParsedParameterSyntax[] formals,
        ParsedActualParameterSyntax[] actuals)
    {
        int index = 0;
        while (index < formals.Length && index < actuals.Length)
        {
            if (this.ContainsReferenceToken(formals[index].ReferenceTokens, token))
            {
                return new SubstitutionFormalMatch
                {
                    Formal = formals[index],
                    Actual = actuals[index],
                    Index = index
                };
            }

            index = index + 1;
        }

        return new SubstitutionFormalMatch();
    }

    internal bool ContainsReferenceToken(AsnToken[] references, AsnToken token)
    {
        int index = 0;
        while (index < references.Length)
        {
            if (references[index] == token)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AppendTemplateToken(
        System.Collections.Generic.List<AsnToken> tokens,
        System.Collections.Generic.List<SubstitutionTokenOrigin> origins,
        AsnToken source,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        AsnToken copy = this.CopySourceToken(source);
        tokens.Add(copy);
        origins.Add(new SubstitutionTokenOrigin
        {
            Token = copy,
            SourceToken = source,
            Module = module,
            Declaration = declaration
        });
    }

    internal void AppendActualTokens(
        System.Collections.Generic.List<AsnToken> tokens,
        System.Collections.Generic.List<SubstitutionTokenOrigin> origins,
        SubstitutionFormalMatch match,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        ParsedParameterSyntax formal = match.Formal
            ?? throw new System.InvalidOperationException();
        ParsedActualParameterSyntax actual = match.Actual
            ?? throw new System.InvalidOperationException();
        int start = this.FindActualTokenStart(formal, actual.SourceTokens);
        int end = this.FindActualTokenEnd(formal, actual.SourceTokens);
        int index = start;
        while (index < end)
        {
            AsnToken source = actual.SourceTokens[index];
            AsnToken copy = this.CopySourceToken(source);
            tokens.Add(copy);
            origins.Add(new SubstitutionTokenOrigin
            {
                Token = copy,
                SourceToken = source,
                Module = module,
                Declaration = declaration,
                FormalParameter = formal,
                ActualParameter = actual,
                Inserted = true
            });
            index = index + 1;
        }
    }

    internal int FindActualTokenStart(
        ParsedParameterSyntax formal,
        AsnToken[] tokens)
    {
        return this.ShouldSpliceSetBody(formal, tokens) ? 1 : 0;
    }

    internal int FindActualTokenEnd(
        ParsedParameterSyntax formal,
        AsnToken[] tokens)
    {
        return this.ShouldSpliceSetBody(formal, tokens)
            ? tokens.Length - 1
            : tokens.Length;
    }

    internal bool ShouldSpliceSetBody(
        ParsedParameterSyntax formal,
        AsnToken[] tokens)
    {
        return (formal.Parameter.Kind == AsnParameterKind.ValueSet
                || formal.Parameter.Kind == AsnParameterKind.ObjectSet)
            && tokens.Length >= 2
            && tokens[0].Kind == AsnTokenKind.LeftBrace
            && tokens[tokens.Length - 1].Kind == AsnTokenKind.RightBrace;
    }

    internal AsnToken CopySourceToken(AsnToken source)
    {
        return new AsnToken
        {
            Kind = source.Kind,
            Text = source.Text,
            Span = source.Span
        };
    }
}
