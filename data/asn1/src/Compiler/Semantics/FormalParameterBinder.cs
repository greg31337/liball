namespace asn1;

internal class FormalParameterBinder
{
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];
    internal bool Success = true;

    internal FormalParameterBinder(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool BindParameters(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].Symbols.Length)
            {
                SymbolTableEntry symbol = tables[tableIndex].Symbols[symbolIndex];
                symbol.FormalParameterSyntax = [];
                symbol.FormalParameters = [];
                if (symbol.Parameterized)
                {
                    this.BindAssignmentParameters(symbol, tables[tableIndex]);
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal void BindAssignmentParameters(
        SymbolTableEntry symbol,
        SymbolTable module)
    {
        AsnToken[] tokens = this.GetParameterListTokens(symbol);
        ParameterListParseResult parsed = new ParameterizedParser(
            tokens,
            this.Diagnostics,
            false).ParseFormalParameterListSyntax();
        if (!parsed.Success || parsed.ConsumedTokenCount != tokens.Length)
        {
            this.AddDiagnostic(
                FormalParameterDiagnosticCodes.InvalidParameterList,
                "The formal parameter list for '" + symbol.QualifiedName
                    + "' could not be bound.",
                symbol.Span);
            return;
        }

        symbol.FormalParameterSyntax = parsed.Parameters;
        symbol.FormalParameters = this.CopyPublicParameters(parsed.Parameters);
        symbol.ParameterNames = this.CopyParameterNames(parsed.Parameters);
        this.ValidateDuplicateNames(parsed.Parameters);
        this.BindParameterKinds(parsed.Parameters, module);
        this.BindScopedReferences(symbol, parsed.Parameters);
    }

    internal void ValidateDuplicateNames(ParsedParameterSyntax[] parameters)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            int earlier = 0;
            while (earlier < index)
            {
                if (parameters[earlier].Parameter.Name
                    == parameters[index].Parameter.Name)
                {
                    this.AddDiagnostic(
                        FormalParameterDiagnosticCodes.DuplicateDummyReference,
                        "The dummy reference '" + parameters[index].Parameter.Name
                            + "' is declared more than once in the formal parameter list.",
                        parameters[index].Span);
                    break;
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal void BindParameterKinds(
        ParsedParameterSyntax[] parameters,
        SymbolTable module)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            ParsedParameterSyntax parameter = parameters[index];
            parameter.DummyGovernor = null;
            parameter.DummyGovernorName = "";
            parameter.BindingFailed = false;
            if (parameter.GovernorTokens.Length == 0)
            {
                this.BindUngovernedParameter(parameter);
            }
            else
            {
                parameter.Parameter.Kind = AsnParameterKind.Unknown;
                parameter.Parameter.GovernorType = null;
                parameter.Parameter.GovernorObjectClass = null;
            }

            index = index + 1;
        }

        bool changed = true;
        int pass = 0;
        while (changed && pass <= parameters.Length)
        {
            changed = false;
            index = 0;
            while (index < parameters.Length)
            {
                ParsedParameterSyntax parameter = parameters[index];
                if (parameter.GovernorTokens.Length > 0
                    && parameter.Parameter.Kind == AsnParameterKind.Unknown
                    && !parameter.BindingFailed
                    && this.TryBindGovernedParameter(parameter, parameters, module))
                {
                    changed = true;
                }

                index = index + 1;
            }

            pass = pass + 1;
        }

        index = 0;
        while (index < parameters.Length)
        {
            ParsedParameterSyntax parameter = parameters[index];
            if (parameter.Parameter.Kind == AsnParameterKind.Unknown
                && !parameter.BindingFailed)
            {
                this.AddDiagnostic(
                    FormalParameterDiagnosticCodes.UnknownGovernor,
                    "The governor for dummy reference '" + parameter.Parameter.Name
                        + "' could not be resolved as a type or object class.",
                    parameter.Span);
            }

            index = index + 1;
        }
    }

    internal void BindUngovernedParameter(ParsedParameterSyntax parameter)
    {
        AsnToken dummy = parameter.SourceTokens[parameter.SourceTokens.Length - 1];
        if (dummy.Text.Length == 1
            && dummy.Text[0] >= 'A'
            && dummy.Text[0] <= 'Z')
        {
            parameter.Parameter.Kind = AsnParameterKind.Type;
            parameter.CandidateKinds = [AsnParameterKind.Type];
            return;
        }

        if (dummy.Kind == AsnTokenKind.ObjectClassReference
            && dummy.Text.Length > 1)
        {
            parameter.Parameter.Kind = AsnParameterKind.ObjectClass;
            parameter.CandidateKinds = [AsnParameterKind.ObjectClass];
            return;
        }

        if (dummy.Kind == AsnTokenKind.TypeReference)
        {
            parameter.Parameter.Kind = AsnParameterKind.Type;
            parameter.CandidateKinds = [AsnParameterKind.Type];
            return;
        }

        parameter.Parameter.Kind = AsnParameterKind.Unknown;
        parameter.BindingFailed = true;
        this.AddDiagnostic(
            FormalParameterDiagnosticCodes.InvalidDummyReference,
            "An ungoverned dummy reference must denote a type or object class.",
            parameter.Span);
    }

    internal bool TryBindGovernedParameter(
        ParsedParameterSyntax parameter,
        ParsedParameterSyntax[] parameters,
        SymbolTable module)
    {
        FormalGovernorResolution governor = this.ResolveGovernor(
            parameter,
            parameters,
            module);
        if (governor.Ambiguous)
        {
            parameter.BindingFailed = true;
            this.AddDiagnostic(
                FormalParameterDiagnosticCodes.UnknownGovernor,
                "The governor for dummy reference '" + parameter.Parameter.Name
                    + "' is ambiguous.",
                parameter.Span);
            return false;
        }

        if (governor.InvalidDummy)
        {
            parameter.BindingFailed = true;
            this.AddDiagnostic(
                FormalParameterDiagnosticCodes.InvalidDummyGovernor,
                "The dummy governor for '" + parameter.Parameter.Name
                    + "' must be an ungoverned type or object-class parameter.",
                parameter.Span);
            return false;
        }

        if (governor.Kind == AsnParameterKind.Unknown)
        {
            return false;
        }

        bool lower = this.IsLowerDummy(parameter);
        if (governor.Kind == AsnParameterKind.Type)
        {
            parameter.Parameter.Kind = lower
                ? AsnParameterKind.Value
                : AsnParameterKind.ValueSet;
            parameter.GovernorTypeSyntax = governor.TypeSyntax;
            parameter.GovernorObjectClassSyntax = null;
            parameter.Parameter.GovernorType = governor.TypeSyntax == null
                ? null
                : governor.TypeSyntax.Type;
        }
        else
        {
            parameter.Parameter.Kind = lower
                ? AsnParameterKind.Object
                : AsnParameterKind.ObjectSet;
            parameter.GovernorTypeSyntax = null;
            parameter.GovernorObjectClassSyntax = governor.ObjectClassSyntax;
            parameter.Parameter.GovernorObjectClass = governor.ObjectClassSyntax == null
                ? null
                : governor.ObjectClassSyntax.ObjectClass;
        }

        parameter.CandidateKinds = [parameter.Parameter.Kind];
        parameter.DummyGovernor = governor.DummyParameter;
        parameter.DummyGovernorName = governor.DummyParameter == null
            ? ""
            : governor.DummyParameter.Parameter.Name;
        return true;
    }

    internal FormalGovernorResolution ResolveGovernor(
        ParsedParameterSyntax parameter,
        ParsedParameterSyntax[] parameters,
        SymbolTable module)
    {
        ParsedParameterSyntax? dummy = this.FindDummyGovernor(
            parameter.GovernorTokens,
            parameters);
        if (dummy != null)
        {
            if (dummy.GovernorTokens.Length > 0)
            {
                return new FormalGovernorResolution
                {
                    DummyParameter = dummy,
                    InvalidDummy = true
                };
            }

            return new FormalGovernorResolution
            {
                Kind = dummy.Parameter.Kind,
                DummyParameter = dummy
            };
        }

        FormalGovernorResolution global = this.ResolveGlobalGovernor(
            parameter.GovernorTokens,
            module);
        if (global.Kind != AsnParameterKind.Unknown || global.Ambiguous)
        {
            return global;
        }

        TypeParseResult type = new TypeParser(
            parameter.GovernorTokens,
            this.Diagnostics,
            false).ParseTypeSyntax();
        ObjectClassParseResult objectClass = new ObjectClassParser(
            parameter.GovernorTokens,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        bool typeSuccess = type.Success
            && type.Syntax != null
            && type.ConsumedTokenCount == parameter.GovernorTokens.Length;
        bool classSuccess = objectClass.Success
            && objectClass.Syntax != null
            && objectClass.ConsumedTokenCount == parameter.GovernorTokens.Length;
        if (typeSuccess && !classSuccess)
        {
            return new FormalGovernorResolution
            {
                Kind = AsnParameterKind.Type,
                TypeSyntax = type.Syntax
            };
        }

        if (classSuccess && !typeSuccess)
        {
            return new FormalGovernorResolution
            {
                Kind = AsnParameterKind.ObjectClass,
                ObjectClassSyntax = objectClass.Syntax
            };
        }

        return global;
    }

    internal FormalGovernorResolution ResolveGlobalGovernor(
        AsnToken[] tokens,
        SymbolTable module)
    {
        string moduleName = "";
        string name = "";
        if (tokens.Length == 1 && this.IsReference(tokens[0]))
        {
            name = tokens[0].Text;
        }
        else if (tokens.Length == 3
            && this.IsReference(tokens[0])
            && tokens[1].Kind == AsnTokenKind.Dot
            && this.IsReference(tokens[2]))
        {
            moduleName = tokens[0].Text;
            name = tokens[2].Text;
        }
        else
        {
            return new FormalGovernorResolution();
        }

        System.Collections.Generic.List<SymbolTableEntry> matches =
            new System.Collections.Generic.List<SymbolTableEntry>();
        this.AddLocalGovernorMatches(matches, module, moduleName, name);
        if (moduleName.Length > 0 || matches.Count == 0)
        {
            this.AddImportedGovernorMatches(matches, module, moduleName, name);
        }

        if (matches.Count != 1)
        {
            return new FormalGovernorResolution { Ambiguous = matches.Count > 1 };
        }

        SymbolTableEntry target = matches[0];
        if (target.Kind == AsnAssignmentKind.Type)
        {
            TypeParseResult type = new TypeParser(
                tokens,
                this.Diagnostics,
                false).ParseTypeSyntax();
            return new FormalGovernorResolution
            {
                Kind = AsnParameterKind.Type,
                TypeSyntax = type.Syntax
            };
        }

        ObjectClassParseResult objectClass = new ObjectClassParser(
            tokens,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        return new FormalGovernorResolution
        {
            Kind = AsnParameterKind.ObjectClass,
            ObjectClassSyntax = objectClass.Syntax
        };
    }

    internal void AddLocalGovernorMatches(
        System.Collections.Generic.List<SymbolTableEntry> matches,
        SymbolTable module,
        string moduleName,
        string name)
    {
        if (moduleName.Length > 0 && moduleName != module.ModuleName)
        {
            return;
        }

        int index = 0;
        while (index < module.Symbols.Length)
        {
            SymbolTableEntry symbol = module.Symbols[index];
            if (symbol.Name == name
                && (symbol.Kind == AsnAssignmentKind.Type
                    || symbol.Kind == AsnAssignmentKind.ObjectClass))
            {
                matches.Add(symbol);
            }

            index = index + 1;
        }
    }

    internal void AddImportedGovernorMatches(
        System.Collections.Generic.List<SymbolTableEntry> matches,
        SymbolTable module,
        string moduleName,
        string name)
    {
        if (moduleName == module.ModuleName)
        {
            return;
        }

        int resolutionIndex = 0;
        while (resolutionIndex < module.ImportResolutions.Length)
        {
            ImportResolution import = module.ImportResolutions[resolutionIndex];
            if (moduleName.Length == 0
                || (import.TargetModule != null
                    && import.TargetModule.ModuleName == moduleName))
            {
                int bindingIndex = 0;
                while (bindingIndex < import.Symbols.Length)
                {
                    ImportedSymbolBinding binding = import.Symbols[bindingIndex];
                    if (binding.Import != null
                        && binding.Import.Name == name
                        && binding.Target != null
                        && (binding.Kind == AsnAssignmentKind.Type
                            || binding.Kind == AsnAssignmentKind.ObjectClass)
                        && !matches.Contains(binding.Target))
                    {
                        matches.Add(binding.Target);
                    }

                    bindingIndex = bindingIndex + 1;
                }
            }

            resolutionIndex = resolutionIndex + 1;
        }
    }

    internal ParsedParameterSyntax? FindDummyGovernor(
        AsnToken[] governor,
        ParsedParameterSyntax[] parameters)
    {
        if (governor.Length != 1)
        {
            return null;
        }

        int index = 0;
        while (index < parameters.Length)
        {
            if (parameters[index].Parameter.Name == governor[0].Text)
            {
                return parameters[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void BindScopedReferences(
        SymbolTableEntry symbol,
        ParsedParameterSyntax[] parameters)
    {
        int parameterIndex = 0;
        while (parameterIndex < parameters.Length)
        {
            System.Collections.Generic.List<AsnToken> references =
                new System.Collections.Generic.List<AsnToken>();
            int tokenIndex = 0;
            while (tokenIndex < symbol.DeclarationTokens.Length)
            {
                AsnToken token = symbol.DeclarationTokens[tokenIndex];
                if (token.Text == parameters[parameterIndex].Parameter.Name
                    && this.IsWithinDummyScope(symbol, tokenIndex)
                    && !this.IsDummyDeclaration(parameters[parameterIndex], token)
                    && !this.IsQualifiedReferencePart(
                        symbol.DeclarationTokens,
                        tokenIndex))
                {
                    references.Add(token);
                }

                tokenIndex = tokenIndex + 1;
            }

            parameters[parameterIndex].ReferenceTokens = references.ToArray();
            parameterIndex = parameterIndex + 1;
        }
    }

    internal bool IsWithinDummyScope(SymbolTableEntry symbol, int tokenIndex)
    {
        int parameterEnd = this.FindParameterListEnd(symbol.DeclarationTokens);
        int assignment = this.FindAssignmentIndex(symbol.DeclarationTokens);
        return (tokenIndex >= 1 && tokenIndex <= parameterEnd)
            || tokenIndex > assignment;
    }

    internal bool IsDummyDeclaration(ParsedParameterSyntax parameter, AsnToken token)
    {
        return parameter.SourceTokens.Length > 0
            && parameter.SourceTokens[parameter.SourceTokens.Length - 1] == token;
    }

    internal bool IsQualifiedReferencePart(AsnToken[] tokens, int index)
    {
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

    internal bool IsLowerDummy(ParsedParameterSyntax parameter)
    {
        string name = parameter.Parameter.Name;
        return name.Length > 0 && name[0] >= 'a' && name[0] <= 'z';
    }

    internal bool IsReference(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.ObjectReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.ObjectSetReference;
    }

    internal AsnToken[] GetParameterListTokens(SymbolTableEntry symbol)
    {
        int end = this.FindParameterListEnd(symbol.DeclarationTokens);
        if (end < 1)
        {
            return [];
        }

        return this.CopyTokenRange(symbol.DeclarationTokens, 1, end + 1);
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

    internal AsnParameter[] CopyPublicParameters(ParsedParameterSyntax[] parameters)
    {
        AsnParameter[] result = new AsnParameter[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            result[index] = parameters[index].Parameter;
            index = index + 1;
        }

        return result;
    }

    internal string[] CopyParameterNames(ParsedParameterSyntax[] parameters)
    {
        string[] result = new string[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            result[index] = parameters[index].Parameter.Name;
            index = index + 1;
        }

        return result;
    }

    internal AsnToken[] CopyTokenRange(AsnToken[] tokens, int start, int end)
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
