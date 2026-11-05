namespace asn1;

internal class SymbolTableBuilder
{
    internal DiagnosticBag Diagnostics;

    internal SymbolTableBuilder(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal SymbolTable BuildTable(ModuleSyntax module)
    {
        if (module == null)
        {
            throw new System.ArgumentNullException("module");
        }

        System.Collections.Generic.List<SymbolTableEntry> symbols =
            new System.Collections.Generic.List<SymbolTableEntry>();
        int index = 0;
        while (index < module.AssignmentHeads.Length)
        {
            AssignmentHeadSyntax head = module.AssignmentHeads[index];
            AssignmentHeadAlternativeSyntax? declared = head.DeclaredReference;
            if (declared != null)
            {
                SymbolTableEntry symbol = new SymbolTableEntry
                {
                    Name = declared.Name,
                    QualifiedName = module.Name + "." + declared.Name,
                    CandidateKinds = this.CopyKinds(declared.CandidateKinds),
                    DeclarationOrder = symbols.Count,
                    Declaration = head,
                    DeclaredReference = declared,
                    Span = declared.Span
                };
                SymbolTableEntry? duplicate = this.FindOverlappingSymbol(symbols, symbol);
                if (duplicate != null)
                {
                    this.AddDuplicateDiagnostic(symbol, duplicate);
                }

                symbols.Add(symbol);
            }

            index = index + 1;
        }

        this.AttachDeclarationTokens(module, symbols);

        this.ApplyExportVisibility(module, symbols);
        SymbolTable table = new SymbolTable
        {
            ModuleName = module.Name,
            ObjectIdentifier = this.BuildDefinitiveObjectIdentifier(module),
            Iri = module.HasDefinitiveIri ? module.DefinitiveIri : "",
            VersionModule = module.HasVersion ? module.VersionModule : "",
            Version = module.HasVersion ? module.Version : "",
            Module = module,
            Symbols = symbols.ToArray()
        };
        table.TypeSymbols = this.SelectCategory(table.Symbols, AsnAssignmentKind.Type);
        table.ValueSymbols = this.SelectCategory(table.Symbols, AsnAssignmentKind.Value);
        table.ValueSetSymbols = this.SelectCategory(table.Symbols, AsnAssignmentKind.ValueSet);
        table.ObjectClassSymbols = this.SelectCategory(table.Symbols, AsnAssignmentKind.ObjectClass);
        table.ObjectSymbols = this.SelectCategory(table.Symbols, AsnAssignmentKind.Object);
        table.ObjectSetSymbols = this.SelectCategory(table.Symbols, AsnAssignmentKind.ObjectSet);
        module.SymbolTable = table;
        return table;
    }

    internal SymbolTable[] BuildTables(ModuleSyntax[] modules)
    {
        if (modules == null)
        {
            throw new System.ArgumentNullException("modules");
        }

        SymbolTable[] tables = new SymbolTable[modules.Length];
        int index = 0;
        while (index < modules.Length)
        {
            tables[index] = this.BuildTable(modules[index]);
            index = index + 1;
        }

        return tables;
    }

    internal void ApplyExportVisibility(
        ModuleSyntax module,
        System.Collections.Generic.List<SymbolTableEntry> symbols)
    {
        if (module.ExportsAll)
        {
            int symbolIndex = 0;
            while (symbolIndex < symbols.Count)
            {
                symbols[symbolIndex].Exported = true;
                symbolIndex = symbolIndex + 1;
            }

            return;
        }

        int exportIndex = 0;
        while (exportIndex < module.ExportedSymbols.Length)
        {
            ImportExportSymbolSyntax export = module.ExportedSymbols[exportIndex];
            bool found = false;
            int symbolIndex = 0;
            while (symbolIndex < symbols.Count)
            {
                SymbolTableEntry symbol = symbols[symbolIndex];
                if (symbol.Name == export.Name
                    && this.KindsOverlap(symbol.CandidateKinds, export.CandidateKinds))
                {
                    symbol.Exported = true;
                    if (symbol.ExportReference == null)
                    {
                        symbol.ExportReference = export;
                    }

                    found = true;
                }

                symbolIndex = symbolIndex + 1;
            }

            if (!found)
            {
                this.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Binding,
                    2,
                    "The exported symbol '" + export.Name + "' is not declared in module '"
                        + module.Name + "'.",
                    export.Span,
                    null);
            }

            exportIndex = exportIndex + 1;
        }
    }

    internal SymbolTableEntry? FindOverlappingSymbol(
        System.Collections.Generic.List<SymbolTableEntry> symbols,
        SymbolTableEntry candidate)
    {
        int index = 0;
        while (index < symbols.Count)
        {
            if (symbols[index].Name == candidate.Name
                && this.KindsOverlap(symbols[index].CandidateKinds, candidate.CandidateKinds))
            {
                return symbols[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void AddDuplicateDiagnostic(SymbolTableEntry duplicate, SymbolTableEntry original)
    {
        AsnDiagnosticRelatedLocation[] related = original.Span == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The first declaration is here.",
                Span = original.Span
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            1,
            "The symbol '" + duplicate.Name + "' is declared more than once in an overlapping category namespace.",
            duplicate.Span,
            related);
    }

    internal SymbolTableEntry[] SelectCategory(
        SymbolTableEntry[] symbols,
        AsnAssignmentKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < symbols.Length)
        {
            if (this.ContainsKind(symbols[index].CandidateKinds, kind))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        SymbolTableEntry[] selected = new SymbolTableEntry[count];
        int selectedIndex = 0;
        index = 0;
        while (index < symbols.Length)
        {
            if (this.ContainsKind(symbols[index].CandidateKinds, kind))
            {
                selected[selectedIndex] = symbols[index];
                selectedIndex = selectedIndex + 1;
            }

            index = index + 1;
        }

        return selected;
    }

    internal bool KindsOverlap(AsnAssignmentKind[] first, AsnAssignmentKind[] second)
    {
        int index = 0;
        while (index < first.Length)
        {
            if (this.ContainsKind(second, first[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsKind(AsnAssignmentKind[] kinds, AsnAssignmentKind expected)
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

    internal AsnAssignmentKind[] CopyKinds(AsnAssignmentKind[] kinds)
    {
        AsnAssignmentKind[] copy = new AsnAssignmentKind[kinds.Length];
        int index = 0;
        while (index < kinds.Length)
        {
            copy[index] = kinds[index];
            index = index + 1;
        }

        return copy;
    }

    internal string BuildDefinitiveObjectIdentifier(ModuleSyntax module)
    {
        if (!module.HasDefinitiveOid)
        {
            return "";
        }

        string result = "";
        int index = 0;
        while (index < module.DefinitiveOidComponents.Length)
        {
            DefinitiveOidComponentSyntax component = module.DefinitiveOidComponents[index];
            string value = component.HasNumber
                ? component.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : component.Name;
            result = result.Length == 0 ? value : result + "." + value;
            index = index + 1;
        }

        return result;
    }

    internal void AttachDeclarationTokens(
        ModuleSyntax module,
        System.Collections.Generic.List<SymbolTableEntry> symbols)
    {
        int symbolIndex = 0;
        while (symbolIndex < symbols.Count)
        {
            SymbolTableEntry symbol = symbols[symbolIndex];
            int start = this.FindDeclarationStart(module.BodyTokens, symbol);
            int assignment = this.FindAssignmentToken(module.BodyTokens, symbol, start);
            int end = symbolIndex + 1 < symbols.Count
                ? this.FindDeclarationStart(module.BodyTokens, symbols[symbolIndex + 1])
                : module.BodyTokens.Length;
            if (start >= 0 && assignment >= start && end > assignment)
            {
                symbol.DeclarationTokens = this.CopyTokenRange(module.BodyTokens, start, end);
                symbol.ReferenceTokens = this.CopyTokenRange(module.BodyTokens, start + 1, end);
                symbol.ParameterNames = this.FindParameterNames(module.BodyTokens, start, assignment);
                symbol.Parameterized = symbol.ParameterNames.Length > 0;
            }

            symbolIndex = symbolIndex + 1;
        }
    }

    internal int FindDeclarationStart(AsnToken[] tokens, SymbolTableEntry symbol)
    {
        if (symbol.DeclaredReference == null || symbol.DeclaredReference.Span == null)
        {
            return -1;
        }

        int offset = symbol.DeclaredReference.Span.Start.Offset;
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Span != null && tokens[index].Span?.Start.Offset == offset)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindAssignmentToken(AsnToken[] tokens, SymbolTableEntry symbol, int start)
    {
        if (symbol.Declaration == null || symbol.Declaration.AssignmentToken == null)
        {
            return -1;
        }

        int index = start;
        while (index >= 0 && index < tokens.Length)
        {
            if (tokens[index] == symbol.Declaration.AssignmentToken)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string[] FindParameterNames(AsnToken[] tokens, int start, int assignment)
    {
        if (start + 1 >= assignment || tokens[start + 1].Kind != AsnTokenKind.LeftBrace)
        {
            return [];
        }

        System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
        int depth = 0;
        int index = start + 1;
        while (index < assignment)
        {
            AsnToken token = tokens[index];
            if (token.Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
            }
            else if (depth == 1 && this.IsParameterNameToken(token))
            {
                AsnToken? following = index + 1 < assignment ? tokens[index + 1] : null;
                if (following == null
                    || following.Kind == AsnTokenKind.Comma
                    || following.Kind == AsnTokenKind.RightBrace)
                {
                    names.Add(token.Text);
                }
            }

            index = index + 1;
        }

        return names.ToArray();
    }

    internal bool IsParameterNameToken(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.ObjectReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.ObjectSetReference;
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
}
