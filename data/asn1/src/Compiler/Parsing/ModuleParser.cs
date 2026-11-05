namespace asn1;

internal class ModuleParser
{
    internal TokenCursor Cursor;
    internal DiagnosticBag Diagnostics;

    internal ModuleParser(TokenCursor cursor)
    {
        this.Cursor = cursor == null ? throw new System.ArgumentNullException("cursor") : cursor;
        this.Diagnostics = cursor.Diagnostics;
    }

    internal ModuleParseResult ParseModule()
    {
        int startPosition = this.Cursor.GetPosition();
        TokenCursorResult first = this.Cursor.PeekNext(0);
        if (first.Status == TokenCursorStatus.NeedMoreData)
        {
            return this.CreateResult(ModuleParseStatus.NeedMoreData, null, startPosition);
        }

        if (first.Status == TokenCursorStatus.EndOfInput)
        {
            return this.CreateResult(ModuleParseStatus.EndOfInput, null, startPosition);
        }

        AsnToken? moduleToken = first.Token;
        if (moduleToken == null || !this.IsModuleReference(moduleToken))
        {
            this.AddExpectedModuleReference(moduleToken);
            return this.CreateResult(ModuleParseStatus.Invalid, null, startPosition);
        }

        this.Cursor.ReadNext();
        ModuleSyntax module = new ModuleSyntax
        {
            Name = moduleToken.Text
        };

        TokenCursorResult current = this.Cursor.PeekNext(0);
        if (current.Status == TokenCursorStatus.NeedMoreData)
        {
            return this.CreateResult(ModuleParseStatus.NeedMoreData, module, startPosition);
        }

        if (current.Token != null && current.Token.Kind == AsnTokenKind.LeftBrace)
        {
            if (!this.ParseDefinitiveOid(module))
            {
                return this.FailAtModuleBoundary(module, startPosition);
            }

            current = this.Cursor.PeekNext(0);
            if (current.Token != null && current.Token.Kind == AsnTokenKind.CharacterStringLiteral)
            {
                module.HasDefinitiveIri = true;
                module.DefinitiveIri = this.DecodeCharacterString(current.Token.Text);
                this.Cursor.ReadNext();
            }
        }

        if (!this.ExpectText(AsnTokenKind.ReservedWord, "DEFINITIONS"))
        {
            return this.FailAtModuleBoundary(module, startPosition);
        }

        if (!this.ParseHeaderDefaults(module))
        {
            return this.FailAtModuleBoundary(module, startPosition);
        }

        if (!this.ExpectKind(AsnTokenKind.Assignment))
        {
            return this.FailAtModuleBoundary(module, startPosition);
        }

        if (!this.ExpectText(AsnTokenKind.ReservedWord, "BEGIN"))
        {
            return this.FailAtModuleBoundary(module, startPosition);
        }

        if (!this.ParseExports(module))
        {
            return this.FailAtModuleBoundary(module, startPosition);
        }

        if (!this.ParseImports(module))
        {
            return this.FailAtModuleBoundary(module, startPosition);
        }

        current = this.Cursor.PeekNext(0);
        if (this.IsText(current.Token, "EXPORTS") || this.IsText(current.Token, "IMPORTS"))
        {
            this.AddSyntaxDiagnostic(
                11,
                "EXPORTS must precede IMPORTS and each clause can appear only once.",
                current.Token);
            return this.FailAtModuleBoundary(module, startPosition);
        }

        module.BodyTokens = this.CollectBodyTokens();
        AssignmentHeadParseResult assignments = new AssignmentHeadParser(this.Diagnostics).ParseHeads(module.BodyTokens);
        module.AssignmentHeads = assignments.Heads;
        module.Recoveries = this.CombineRecoveries(module.Recoveries, assignments.Recoveries);
        if (!assignments.Success)
        {
            return this.FailAtModuleBoundary(module, startPosition);
        }

        System.Collections.Generic.List<EncodingControlSectionSyntax> sections =
            new System.Collections.Generic.List<EncodingControlSectionSyntax>();
        current = this.Cursor.PeekNext(0);
        while (this.IsText(current.Token, "ENCODING-CONTROL"))
        {
            sections.Add(this.ParseEncodingControlSection(sections));
            current = this.Cursor.PeekNext(0);
        }

        module.EncodingControlSections = sections.ToArray();
        current = this.Cursor.ExpectTokenText(AsnTokenKind.ReservedWord, "END");
        if (current.Status != TokenCursorStatus.Token || current.Token == null)
        {
            return this.CreateResult(
                current.Status == TokenCursorStatus.NeedMoreData
                    ? ModuleParseStatus.NeedMoreData
                    : ModuleParseStatus.Invalid,
                module,
                startPosition);
        }

        module.Span = this.CreateSpan(moduleToken, current.Token);
        return this.CreateResult(ModuleParseStatus.Parsed, module, startPosition);
    }

    internal ModuleCollectionParseResult ParseModuleList()
    {
        int startPosition = this.Cursor.GetPosition();
        System.Collections.Generic.List<ModuleSyntax> modules =
            new System.Collections.Generic.List<ModuleSyntax>();
        bool invalid = false;
        while (true)
        {
            int before = this.Cursor.GetPosition();
            ModuleParseResult current = this.ParseModule();
            if ((current.Status == ModuleParseStatus.Parsed
                    || current.Status == ModuleParseStatus.Invalid)
                && current.Module != null)
            {
                modules.Add(current.Module);
            }

            if (current.Status == ModuleParseStatus.Parsed)
            {
                continue;
            }

            if (current.Status == ModuleParseStatus.Invalid)
            {
                invalid = true;
                TokenCursorResult boundary = this.Cursor.PeekNext(0);
                if (this.IsText(boundary.Token, "END"))
                {
                    AsnToken? end = this.Cursor.ReadNext().Token;
                    if (current.Module != null && end != null)
                    {
                        current.Module.Span = this.CreateSpanFromModule(current.Module, end);
                    }
                }

                if (this.Cursor.GetPosition() > before)
                {
                    continue;
                }
            }

            ModuleParseStatus status = current.Status == ModuleParseStatus.EndOfInput
                ? invalid ? ModuleParseStatus.Invalid : ModuleParseStatus.Parsed
                : current.Status;
            return new ModuleCollectionParseResult
            {
                Status = status,
                Modules = modules.ToArray(),
                ConsumedTokenCount = this.Cursor.GetPosition() - startPosition
            };
        }
    }

    internal bool ParseExports(ModuleSyntax module)
    {
        TokenCursorResult current = this.Cursor.PeekNext(0);
        if (!this.IsText(current.Token, "EXPORTS"))
        {
            return true;
        }

        module.ExportsSpecified = true;
        module.ExportsAll = false;
        this.Cursor.ReadNext();
        current = this.Cursor.PeekNext(0);
        if (this.IsText(current.Token, "ALL"))
        {
            module.ExportsAll = true;
            this.Cursor.ReadNext();
        }
        else if (current.Token != null && current.Token.Kind != AsnTokenKind.Semicolon)
        {
            SymbolListParseResult symbols = this.ParseSymbolList(";");
            if (!symbols.Success)
            {
                return false;
            }

            module.ExportedSymbols = symbols.Symbols;
        }

        return this.ExpectKind(AsnTokenKind.Semicolon);
    }

    internal bool ParseImports(ModuleSyntax module)
    {
        TokenCursorResult current = this.Cursor.PeekNext(0);
        if (!this.IsText(current.Token, "IMPORTS"))
        {
            return true;
        }

        module.ImportsSpecified = true;
        this.Cursor.ReadNext();
        System.Collections.Generic.List<ImportedModuleSyntax> imports =
            new System.Collections.Generic.List<ImportedModuleSyntax>();
        current = this.Cursor.PeekNext(0);
        while (current.Token != null && current.Token.Kind != AsnTokenKind.Semicolon)
        {
            SymbolListParseResult symbols = this.ParseSymbolList("FROM");
            if (!symbols.Success || !this.ExpectText(AsnTokenKind.ReservedWord, "FROM"))
            {
                module.ImportedModules = imports.ToArray();
                return false;
            }

            TokenCursorResult moduleReference = this.Cursor.PeekNext(0);
            if (moduleReference.Token == null || !this.IsModuleReference(moduleReference.Token))
            {
                this.AddSyntaxDiagnostic(9, "Expected a module reference after FROM.", moduleReference.Token);
                module.ImportedModules = imports.ToArray();
                return false;
            }

            this.Cursor.ReadNext();
            ImportedModuleSyntax imported = new ImportedModuleSyntax
            {
                ModuleName = moduleReference.Token.Text,
                Symbols = symbols.Symbols
            };
            imported.AssignedIdentifier = this.ParseAssignedModuleIdentifier();
            AsnToken? last = this.GetImportedModuleEndToken(imported, moduleReference.Token);
            imported.Span = symbols.Symbols.Length == 0 || last == null
                ? moduleReference.Token.Span
                : this.CreateSpanFromSymbol(symbols.Symbols[0], last);
            imports.Add(imported);
            current = this.Cursor.PeekNext(0);
        }

        module.ImportedModules = imports.ToArray();
        return this.ExpectKind(AsnTokenKind.Semicolon);
    }

    internal SymbolListParseResult ParseSymbolList(string terminalText)
    {
        System.Collections.Generic.List<ImportExportSymbolSyntax> symbols =
            new System.Collections.Generic.List<ImportExportSymbolSyntax>();
        TokenCursorResult current = this.Cursor.PeekNext(0);
        while (!this.IsText(current.Token, terminalText))
        {
            if (current.Token == null || !this.IsSymbolReference(current.Token))
            {
                this.AddSyntaxDiagnostic(7, "Expected an exported or imported symbol reference.", current.Token);
                return new SymbolListParseResult
                {
                    Symbols = symbols.ToArray()
                };
            }

            AsnToken first = current.Token;
            this.Cursor.ReadNext();
            ImportExportSymbolSyntax symbol = new ImportExportSymbolSyntax
            {
                Name = first.Text,
                CandidateKinds = this.GetCandidateAssignmentKinds(first),
                Span = first.Span
            };
            TokenCursorResult left = this.Cursor.PeekNext(0);
            if (left.Token != null && left.Token.Kind == AsnTokenKind.LeftBrace)
            {
                TokenCursorResult right = this.Cursor.PeekNext(1);
                if (right.Token != null && right.Token.Kind == AsnTokenKind.RightBrace)
                {
                    this.Cursor.ReadNext();
                    this.Cursor.ReadNext();
                    symbol.Parameterized = true;
                    symbol.Span = this.CreateSpan(first, right.Token);
                }
            }

            symbols.Add(symbol);
            current = this.Cursor.PeekNext(0);
            if (current.Token != null && current.Token.Kind == AsnTokenKind.Comma)
            {
                this.Cursor.ReadNext();
                current = this.Cursor.PeekNext(0);
                if (this.IsText(current.Token, terminalText))
                {
                    this.AddSyntaxDiagnostic(7, "Expected a symbol reference after ','.", current.Token);
                    return new SymbolListParseResult
                    {
                        Symbols = symbols.ToArray()
                    };
                }

                continue;
            }

            if (!this.IsText(current.Token, terminalText))
            {
                this.AddSyntaxDiagnostic(
                    8,
                    "Expected ',' or " + terminalText + " after a symbol reference.",
                    current.Token);
                return new SymbolListParseResult
                {
                    Symbols = symbols.ToArray()
                };
            }
        }

        if (symbols.Count == 0)
        {
            this.AddSyntaxDiagnostic(7, "Expected an exported or imported symbol reference.", current.Token);
        }

        return new SymbolListParseResult
        {
            Success = symbols.Count > 0,
            Symbols = symbols.ToArray()
        };
    }

    internal AssignedModuleIdentifierSyntax? ParseAssignedModuleIdentifier()
    {
        TokenCursorResult current = this.Cursor.PeekNext(0);
        if (current.Token != null && current.Token.Kind == AsnTokenKind.LeftBrace)
        {
            return this.ParseAssignedObjectIdentifier();
        }

        if (current.Token != null && current.Token.Kind == AsnTokenKind.CharacterStringLiteral)
        {
            this.Cursor.ReadNext();
            return new AssignedModuleIdentifierSyntax
            {
                HasIri = true,
                Iri = this.DecodeCharacterString(current.Token.Text),
                Span = current.Token.Span
            };
        }

        if (this.IsExternalDefinedValueStart(current.Token))
        {
            AsnToken module = current.Token == null
                ? throw new System.InvalidOperationException("The defined-value module is missing.")
                : current.Token;
            this.Cursor.ReadNext();
            this.Cursor.ReadNext();
            TokenCursorResult value = this.Cursor.ReadNext();
            AsnToken valueToken = value.Token == null
                ? throw new System.InvalidOperationException("The defined value is missing.")
                : value.Token;
            return new AssignedModuleIdentifierSyntax
            {
                HasDefinedValue = true,
                DefinedValueModule = module.Text,
                DefinedValue = valueToken.Text,
                Span = this.CreateSpan(module, valueToken)
            };
        }

        if (this.ShouldParseSimpleDefinedValue(current.Token))
        {
            AsnToken value = current.Token == null
                ? throw new System.InvalidOperationException("The defined value is missing.")
                : current.Token;
            this.Cursor.ReadNext();
            return new AssignedModuleIdentifierSyntax
            {
                HasDefinedValue = true,
                DefinedValue = value.Text,
                Span = value.Span
            };
        }

        return null;
    }

    internal AssignedModuleIdentifierSyntax? ParseAssignedObjectIdentifier()
    {
        TokenCursorResult left = this.Cursor.ReadNext();
        System.Collections.Generic.List<AssignedOidComponentSyntax> components =
            new System.Collections.Generic.List<AssignedOidComponentSyntax>();
        TokenCursorResult current = this.Cursor.PeekNext(0);
        while (current.Token != null && current.Token.Kind != AsnTokenKind.RightBrace)
        {
            AssignedOidComponentSyntax? component = this.ParseAssignedOidComponent();
            if (component == null)
            {
                this.AddSyntaxDiagnostic(10, "The assigned module identifier is invalid.", current.Token);
                return null;
            }

            components.Add(component);
            current = this.Cursor.PeekNext(0);
        }

        if (components.Count == 0 || current.Token == null)
        {
            this.AddSyntaxDiagnostic(10, "The assigned module identifier is invalid.", current.Token);
            return null;
        }

        TokenCursorResult right = this.Cursor.ReadNext();
        return new AssignedModuleIdentifierSyntax
        {
            HasObjectIdentifier = true,
            ObjectIdentifierComponents = components.ToArray(),
            Span = left.Token == null || right.Token == null
                ? null
                : this.CreateSpan(left.Token, right.Token)
        };
    }

    internal AssignedOidComponentSyntax? ParseAssignedOidComponent()
    {
        TokenCursorResult first = this.Cursor.PeekNext(0);
        if (first.Token == null)
        {
            return null;
        }

        if (first.Token.Kind == AsnTokenKind.IntegerLiteral)
        {
            this.Cursor.ReadNext();
            return new AssignedOidComponentSyntax
            {
                Number = System.Numerics.BigInteger.Parse(
                    first.Token.Text,
                    System.Globalization.CultureInfo.InvariantCulture),
                HasNumber = true,
                Span = first.Token.Span
            };
        }

        if (this.IsExternalDefinedValueStart(first.Token))
        {
            AsnToken module = first.Token;
            this.Cursor.ReadNext();
            this.Cursor.ReadNext();
            TokenCursorResult value = this.Cursor.ReadNext();
            if (value.Token == null)
            {
                return null;
            }

            return new AssignedOidComponentSyntax
            {
                DefinedValueModule = module.Text,
                DefinedValue = value.Token.Text,
                HasDefinedValue = true,
                Span = this.CreateSpan(module, value.Token)
            };
        }

        if (first.Token.Kind != AsnTokenKind.Identifier
            && first.Token.Kind != AsnTokenKind.ValueReference)
        {
            return null;
        }

        this.Cursor.ReadNext();
        AssignedOidComponentSyntax component = new AssignedOidComponentSyntax
        {
            Name = first.Token.Text,
            HasName = true,
            DefinedValue = first.Token.Text,
            HasDefinedValue = true,
            Span = first.Token.Span
        };
        TokenCursorResult left = this.Cursor.PeekNext(0);
        if (left.Token == null || left.Token.Kind != AsnTokenKind.LeftParenthesis)
        {
            return component;
        }

        component.HasDefinedValue = false;
        component.DefinedValue = "";
        this.Cursor.ReadNext();
        if (!this.ParseAssignedNumberForm(component))
        {
            return null;
        }

        TokenCursorResult right = this.Cursor.ExpectToken(AsnTokenKind.RightParenthesis);
        if (right.Status != TokenCursorStatus.Token || right.Token == null)
        {
            return null;
        }

        component.Span = this.CreateSpan(first.Token, right.Token);
        return component;
    }

    internal bool ParseAssignedNumberForm(AssignedOidComponentSyntax component)
    {
        TokenCursorResult current = this.Cursor.PeekNext(0);
        if (current.Token == null)
        {
            return false;
        }

        if (current.Token.Kind == AsnTokenKind.IntegerLiteral)
        {
            component.Number = System.Numerics.BigInteger.Parse(
                current.Token.Text,
                System.Globalization.CultureInfo.InvariantCulture);
            component.HasNumber = true;
            this.Cursor.ReadNext();
            return true;
        }

        if (this.IsExternalDefinedValueStart(current.Token))
        {
            AsnToken module = current.Token;
            this.Cursor.ReadNext();
            this.Cursor.ReadNext();
            TokenCursorResult value = this.Cursor.ReadNext();
            if (value.Token == null)
            {
                return false;
            }

            component.NumberDefinedValueModule = module.Text;
            component.NumberDefinedValue = value.Token.Text;
            component.HasNumberDefinedValue = true;
            return true;
        }

        if (current.Token.Kind == AsnTokenKind.Identifier
            || current.Token.Kind == AsnTokenKind.ValueReference)
        {
            component.NumberDefinedValue = current.Token.Text;
            component.HasNumberDefinedValue = true;
            this.Cursor.ReadNext();
            return true;
        }

        return false;
    }

    internal bool IsExternalDefinedValueStart(AsnToken? token)
    {
        if (token == null || !this.IsModuleReference(token))
        {
            return false;
        }

        TokenCursorResult dot = this.Cursor.PeekNext(1);
        TokenCursorResult value = this.Cursor.PeekNext(2);
        return dot.Token != null
            && dot.Token.Kind == AsnTokenKind.Dot
            && value.Token != null
            && (value.Token.Kind == AsnTokenKind.Identifier
                || value.Token.Kind == AsnTokenKind.ValueReference);
    }

    internal bool ShouldParseSimpleDefinedValue(AsnToken? token)
    {
        if (token == null
            || (token.Kind != AsnTokenKind.Identifier
                && token.Kind != AsnTokenKind.ValueReference))
        {
            return false;
        }

        TokenCursorResult following = this.Cursor.PeekNext(1);
        return following.Token == null
            || (following.Token.Kind != AsnTokenKind.Comma
                && !this.IsText(following.Token, "FROM"));
    }

    internal AsnToken? GetImportedModuleEndToken(ImportedModuleSyntax imported, AsnToken moduleReference)
    {
        if (imported.AssignedIdentifier == null || imported.AssignedIdentifier.Span == null)
        {
            return moduleReference;
        }

        return new AsnToken
        {
            Span = imported.AssignedIdentifier.Span
        };
    }

    internal AsnSourceSpan? CreateSpanFromSymbol(ImportExportSymbolSyntax symbol, AsnToken last)
    {
        if (symbol.Span == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = symbol.Span.Start,
            End = last.Span.End
        };
    }

    internal bool IsSymbolReference(AsnToken token)
    {
        return token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.ObjectReference
            || token.Kind == AsnTokenKind.ObjectSetReference
            || token.Kind == AsnTokenKind.Identifier;
    }

    internal AsnAssignmentKind[] GetCandidateAssignmentKinds(AsnToken token)
    {
        if (token.Kind == AsnTokenKind.ValueReference)
        {
            return [AsnAssignmentKind.Value];
        }

        if (token.Kind == AsnTokenKind.ObjectReference)
        {
            return [AsnAssignmentKind.Object];
        }

        if (token.Kind == AsnTokenKind.ObjectSetReference)
        {
            return [AsnAssignmentKind.ObjectSet];
        }

        if (token.Kind == AsnTokenKind.Identifier)
        {
            return [AsnAssignmentKind.Value, AsnAssignmentKind.Object];
        }

        if (token.Kind == AsnTokenKind.ObjectClassReference)
        {
            return [
                AsnAssignmentKind.ObjectClass,
                AsnAssignmentKind.Type,
                AsnAssignmentKind.ValueSet,
                AsnAssignmentKind.ObjectSet
            ];
        }

        return [AsnAssignmentKind.Type, AsnAssignmentKind.ValueSet, AsnAssignmentKind.ObjectSet];
    }

    internal bool ParseDefinitiveOid(ModuleSyntax module)
    {
        TokenCursorResult left = this.Cursor.ExpectToken(AsnTokenKind.LeftBrace);
        if (left.Status != TokenCursorStatus.Token)
        {
            return false;
        }

        module.HasDefinitiveOid = true;
        System.Collections.Generic.List<DefinitiveOidComponentSyntax> components =
            new System.Collections.Generic.List<DefinitiveOidComponentSyntax>();
        TokenCursorResult current = this.Cursor.PeekNext(0);
        while (current.Token != null && current.Token.Kind != AsnTokenKind.RightBrace)
        {
            DefinitiveOidComponentSyntax? component = this.ParseDefinitiveOidComponent();
            if (component == null)
            {
                this.Cursor.RecoverTo(new RecoverySet(
                    "definitive identifier",
                    [
                        new RecoveryPoint(AsnTokenKind.RightBrace, ""),
                        new RecoveryPoint(AsnTokenKind.EndOfInput, "")
                    ]));
                module.DefinitiveOidComponents = components.ToArray();
                return false;
            }

            components.Add(component);
            current = this.Cursor.PeekNext(0);
        }

        module.DefinitiveOidComponents = components.ToArray();
        if (components.Count == 0)
        {
            this.AddSyntaxDiagnostic(2, "A definitive object identifier requires at least one component.", current.Token);
        }

        bool hasRightBrace = this.ExpectKind(AsnTokenKind.RightBrace);
        return components.Count > 0 && hasRightBrace;
    }

    internal DefinitiveOidComponentSyntax? ParseDefinitiveOidComponent()
    {
        TokenCursorResult first = this.Cursor.PeekNext(0);
        if (first.Token == null)
        {
            this.AddSyntaxDiagnostic(3, "Expected a definitive object identifier component.", null);
            return null;
        }

        if (first.Token.Kind == AsnTokenKind.IntegerLiteral)
        {
            this.Cursor.ReadNext();
            return new DefinitiveOidComponentSyntax
            {
                Number = System.Numerics.BigInteger.Parse(
                    first.Token.Text,
                    System.Globalization.CultureInfo.InvariantCulture),
                HasNumber = true,
                Span = first.Token.Span
            };
        }

        if (first.Token.Kind != AsnTokenKind.Identifier)
        {
            this.AddSyntaxDiagnostic(3, "Expected a definitive object identifier component.", first.Token);
            return null;
        }

        this.Cursor.ReadNext();
        DefinitiveOidComponentSyntax component = new DefinitiveOidComponentSyntax
        {
            Name = first.Token.Text,
            HasName = true,
            Span = first.Token.Span
        };
        TokenCursorResult next = this.Cursor.PeekNext(0);
        if (next.Token == null || next.Token.Kind != AsnTokenKind.LeftParenthesis)
        {
            return component;
        }

        this.Cursor.ReadNext();
        TokenCursorResult number = this.Cursor.ExpectToken(AsnTokenKind.IntegerLiteral);
        if (number.Status != TokenCursorStatus.Token || number.Token == null)
        {
            return null;
        }

        component.Number = System.Numerics.BigInteger.Parse(
            number.Token.Text,
            System.Globalization.CultureInfo.InvariantCulture);
        component.HasNumber = true;
        TokenCursorResult right = this.Cursor.ExpectToken(AsnTokenKind.RightParenthesis);
        if (right.Status != TokenCursorStatus.Token || right.Token == null)
        {
            return null;
        }

        component.Span = this.CreateSpan(first.Token, right.Token);
        return component;
    }

    internal bool ParseHeaderDefaults(ModuleSyntax module)
    {
        TokenCursorResult current = this.Cursor.PeekNext(0);
        if (this.IsEncodingReference(current.Token))
        {
            TokenCursorResult following = this.Cursor.PeekNext(1);
            if (this.IsText(following.Token, "INSTRUCTIONS"))
            {
                module.EncodingReference = current.Token == null ? "TAG" : current.Token.Text;
                module.EncodingReferenceSpecified = true;
                this.Cursor.ReadNext();
                this.Cursor.ReadNext();
            }
        }

        current = this.Cursor.PeekNext(0);
        if (this.IsText(current.Token, "EXPLICIT")
            || this.IsText(current.Token, "IMPLICIT")
            || this.IsText(current.Token, "AUTOMATIC"))
        {
            module.TagDefaultSpecified = true;
            if (this.IsText(current.Token, "IMPLICIT"))
            {
                module.TagDefault = AsnTaggingMode.Implicit;
            }
            else if (this.IsText(current.Token, "AUTOMATIC"))
            {
                module.TagDefault = AsnTaggingMode.Automatic;
            }
            else
            {
                module.TagDefault = AsnTaggingMode.Explicit;
            }

            this.Cursor.ReadNext();
            if (!this.ExpectText(AsnTokenKind.ReservedWord, "TAGS"))
            {
                return false;
            }
        }

        current = this.Cursor.PeekNext(0);
        if (this.IsText(current.Token, "EXTENSIBILITY"))
        {
            this.Cursor.ReadNext();
            if (!this.ExpectText(AsnTokenKind.ReservedWord, "IMPLIED"))
            {
                return false;
            }

            module.ExtensibilityImplied = true;
        }

        return true;
    }

    internal AsnToken[] CollectBodyTokens()
    {
        System.Collections.Generic.List<AsnToken> tokens = new System.Collections.Generic.List<AsnToken>();
        bool macroPending = false;
        int macroDepth = 0;
        TokenCursorResult current = this.Cursor.PeekNext(0);
        while (current.Status == TokenCursorStatus.Token
            && (macroDepth > 0 || !this.IsText(current.Token, "END"))
            && (macroDepth > 0 || !this.IsText(current.Token, "ENCODING-CONTROL")))
        {
            if (current.Token != null)
            {
                tokens.Add(current.Token);
                if (macroDepth == 0 && current.Token.Text == "MACRO")
                {
                    macroPending = true;
                }
                else if (macroPending && current.Token.Text == "BEGIN")
                {
                    macroPending = false;
                    macroDepth = 1;
                }
                else if (macroDepth > 0 && current.Token.Text == "BEGIN")
                {
                    macroDepth = macroDepth + 1;
                }
                else if (macroDepth > 0 && current.Token.Text == "END")
                {
                    macroDepth = macroDepth - 1;
                }
            }

            this.Cursor.ReadNext();
            current = this.Cursor.PeekNext(0);
        }

        return tokens.ToArray();
    }

    internal EncodingControlSectionSyntax ParseEncodingControlSection(
        System.Collections.Generic.List<EncodingControlSectionSyntax> previousSections)
    {
        TokenCursorResult marker = this.Cursor.ReadNext();
        TokenCursorResult reference = this.Cursor.PeekNext(0);
        EncodingControlSectionSyntax section = new EncodingControlSectionSyntax();
        if (!this.IsEncodingReference(reference.Token))
        {
            this.AddSyntaxDiagnostic(4, "Expected an encoding reference after ENCODING-CONTROL.", reference.Token);
            if (reference.Token != null)
            {
                section.Recoveries = [new ParsedRecoverySyntax
                {
                    Boundary = "encoding control",
                    SkippedTokens = [reference.Token],
                    Span = reference.Token.Span
                }];
            }
        }
        else if (reference.Token != null)
        {
            section.EncodingReference = reference.Token.Text;
            this.Cursor.ReadNext();
            if (section.EncodingReference == "TAG")
            {
                this.AddSyntaxDiagnostic(5, "TAG cannot identify an encoding-control section.", reference.Token);
            }

            if (this.ContainsEncodingReference(previousSections, section.EncodingReference))
            {
                this.AddSyntaxDiagnostic(
                    6,
                    "The encoding-control reference '" + section.EncodingReference + "' is duplicated.",
                    reference.Token);
            }
        }

        AsnToken[] significantTokens = this.CollectBodyTokens();
        TokenCursorResult boundary = this.Cursor.PeekNext(0);
        AsnToken? sectionStart = reference.Token == null ? marker.Token : reference.Token;
        section.InstructionTokens = sectionStart == null
            ? significantTokens
            : this.IncludeSectionTrivia(significantTokens, sectionStart, boundary.Token);
        if (section.EncodingReference == "XER")
        {
            XerEncodingInstructionParseResult parsed =
                new XerEncodingInstructionParser(section.InstructionTokens).ParseSection();
            section.Instructions = parsed.Instructions;
            if (!parsed.Success)
            {
                this.AddSyntaxDiagnostic(12, parsed.ErrorMessage, parsed.ErrorToken);
            }
        }
        else if (section.EncodingReference == "PER")
        {
            PerEncodingInstructionParseResult parsed =
                new PerEncodingInstructionParser(section.InstructionTokens).ParseSection();
            section.Instructions = parsed.Instructions;
            if (!parsed.Success)
            {
                this.AddSyntaxDiagnostic(14, parsed.ErrorMessage, parsed.ErrorToken);
            }
        }
        else if (section.EncodingReference == "ECN")
        {
            EcnParseResult parsed = new EcnSyntaxParser(
                section.InstructionTokens).ParseAssignmentListSyntax();
            section.EcnAssignments = parsed.Assignments;
            if (!parsed.Success)
            {
                this.AddSyntaxDiagnostic(
                    13,
                    parsed.ErrorMessage,
                    parsed.ErrorToken);
            }
        }

        AsnToken? endToken = section.InstructionTokens.Length == 0
            ? reference.Token
            : section.InstructionTokens[section.InstructionTokens.Length - 1];
        if (marker.Token != null && endToken != null)
        {
            section.Span = this.CreateSpan(marker.Token, endToken);
        }

        return section;
    }

    internal AsnToken[] IncludeSectionTrivia(
        AsnToken[] significantTokens,
        AsnToken sectionStart,
        AsnToken? boundary)
    {
        System.Collections.Generic.List<AsnToken> combined =
            new System.Collections.Generic.List<AsnToken>();
        int index = 0;
        while (index < significantTokens.Length)
        {
            combined.Add(significantTokens[index]);
            index = index + 1;
        }

        AsnToken[] trivia = this.Cursor.GetTriviaTokens();
        index = 0;
        while (index < trivia.Length)
        {
            if (this.IsTokenBetween(trivia[index], sectionStart, boundary))
            {
                this.InsertTokenInSourceOrder(combined, trivia[index]);
            }

            index = index + 1;
        }

        return combined.ToArray();
    }

    internal bool IsTokenBetween(AsnToken token, AsnToken sectionStart, AsnToken? boundary)
    {
        if (token.Span == null || sectionStart.Span == null)
        {
            return false;
        }

        if (token.Span.Start.SourceName != sectionStart.Span.Start.SourceName
            || token.Span.Start.Offset < sectionStart.Span.End.Offset)
        {
            return false;
        }

        return boundary == null
            || boundary.Span == null
            || token.Span.End.Offset <= boundary.Span.Start.Offset;
    }

    internal void InsertTokenInSourceOrder(
        System.Collections.Generic.List<AsnToken> tokens,
        AsnToken token)
    {
        int insertionIndex = tokens.Count;
        AsnSourceSpan? tokenSpan = token.Span;
        if (tokenSpan != null)
        {
            int index = 0;
            while (index < tokens.Count)
            {
                AsnSourceSpan? candidateSpan = tokens[index].Span;
                if (candidateSpan != null
                    && candidateSpan.Start.Offset > tokenSpan.Start.Offset)
                {
                    insertionIndex = index;
                    index = tokens.Count;
                }
                else
                {
                    index = index + 1;
                }
            }
        }

        tokens.Insert(insertionIndex, token);
    }

    internal bool ContainsEncodingReference(
        System.Collections.Generic.List<EncodingControlSectionSyntax> sections,
        string encodingReference)
    {
        int index = 0;
        while (index < sections.Count)
        {
            if (sections[index].EncodingReference == encodingReference)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsModuleReference(AsnToken token)
    {
        return token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ObjectClassReference;
    }

    internal bool IsEncodingReference(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.EncodingReference
                || token.Kind == AsnTokenKind.ObjectClassReference
                || token.Text == "TAG"
                || token.Text == "XER"
                || token.Text == "PER"
                || token.Text == "ECN");
    }

    internal bool IsText(AsnToken? token, string text)
    {
        return token != null && token.Text == text;
    }

    internal bool ExpectText(AsnTokenKind kind, string text)
    {
        TokenCursorResult result = this.Cursor.ExpectTokenText(kind, text);
        return result.Status == TokenCursorStatus.Token;
    }

    internal bool ExpectKind(AsnTokenKind kind)
    {
        TokenCursorResult result = this.Cursor.ExpectToken(kind);
        return result.Status == TokenCursorStatus.Token;
    }

    internal ModuleParseResult FailAtModuleBoundary(ModuleSyntax module, int startPosition)
    {
        int recoveryStart = this.Cursor.GetPosition();
        this.Cursor.RecoverTo(new RecoverySetCatalog().ModuleBoundary);
        int recoveryEnd = this.Cursor.GetPosition();
        if (recoveryEnd > recoveryStart)
        {
            module.Recoveries = this.AppendRecovery(
                module.Recoveries,
                "module",
                this.CopyCursorTokenRange(recoveryStart, recoveryEnd));
        }

        return this.CreateResult(ModuleParseStatus.Invalid, module, startPosition);
    }

    internal ParsedRecoverySyntax[] AppendRecovery(
        ParsedRecoverySyntax[] recoveries,
        string boundary,
        AsnToken[] skipped)
    {
        ParsedRecoverySyntax[] result = new ParsedRecoverySyntax[recoveries.Length + 1];
        int index = 0;
        while (index < recoveries.Length)
        {
            result[index] = recoveries[index];
            index = index + 1;
        }

        result[index] = new ParsedRecoverySyntax
        {
            Boundary = boundary,
            SkippedTokens = skipped,
            Span = skipped.Length == 0
                ? null
                : this.CreateSpan(skipped[0], skipped[skipped.Length - 1])
        };
        return result;
    }

    internal ParsedRecoverySyntax[] CombineRecoveries(
        ParsedRecoverySyntax[] first,
        ParsedRecoverySyntax[] second)
    {
        ParsedRecoverySyntax[] result = new ParsedRecoverySyntax[first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            result[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            result[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return result;
    }

    internal AsnToken[] CopyCursorTokenRange(int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = this.Cursor.Tokens[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnSourceSpan? CreateSpanFromModule(ModuleSyntax module, AsnToken end)
    {
        if (module.Span != null)
        {
            return new AsnSourceSpan { Start = module.Span.Start, End = end.Span == null
                ? module.Span.End
                : end.Span.End };
        }

        if (end.Span == null)
        {
            return null;
        }

        AsnSourcePosition start = end.Span.Start;
        if (module.BodyTokens.Length > 0 && module.BodyTokens[0].Span != null)
        {
            start = module.BodyTokens[0].Span?.Start ?? start;
        }

        return new AsnSourceSpan { Start = start, End = end.Span.End };
    }

    internal void AddExpectedModuleReference(AsnToken? token)
    {
        this.AddSyntaxDiagnostic(1, "Expected a module reference.", token);
    }

    internal void AddSyntaxDiagnostic(int number, string message, AsnToken? token)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Syntax,
            number,
            message,
            token == null ? null : token.Span,
            null);
    }

    internal string DecodeCharacterString(string text)
    {
        if (text.Length < 2)
        {
            return "";
        }

        return text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
    }

    internal AsnSourceSpan? CreateSpan(AsnToken first, AsnToken last)
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

    internal ModuleParseResult CreateResult(
        ModuleParseStatus status,
        ModuleSyntax? module,
        int startPosition)
    {
        return new ModuleParseResult
        {
            Status = status,
            Module = module,
            ConsumedTokenCount = this.Cursor.GetPosition() - startPosition
        };
    }
}
