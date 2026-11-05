namespace asn1;

internal class CompilerImplementation : IAsnCompiler
{
    internal XsdSchemaImporterImplementation? XsdImporter =
        new XsdSchemaImporterImplementation();

    public AsnCompilationResult Compile(AsnCompilationInput input)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        AsnCompilerOptions options = input.CompilerOptions
            ?? new AsnCompilerOptions();
        CompilationDiagnosticSet diagnostics = new CompilationDiagnosticSet
        {
            TreatWarningsAsErrors = options.TreatWarningsAsErrors
        };
        PerInstructionRegistrySnapshotResult perRegistry =
            new PerInstructionRegistrySnapshotBuilder().CreateSnapshot(
                input.PerInstructionRegistry);
        diagnostics.AddDiagnostics(perRegistry.Diagnostics);
        System.Collections.Generic.List<AsnSourceUnit> sources =
            new System.Collections.Generic.List<AsnSourceUnit>();
        this.AppendInputSources(input.SourceUnits, sources);
        AsnXsdImportResult[] xsdResults = this.ImportXsdSources(
            input,
            sources,
            diagnostics);

        System.Collections.Generic.List<CompilerSourceParseResult> parsedSources =
            new System.Collections.Generic.List<CompilerSourceParseResult>();
        System.Collections.Generic.List<ModuleSyntax> syntaxModules =
            new System.Collections.Generic.List<ModuleSyntax>();
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            CompilerSourceParseResult parsed = this.ParseSource(
                sources[sourceIndex],
                options);
            if (xsdResults.Length > 0 && this.XsdImporter != null)
            {
                new XsdCompilerIntegrator().AttachSourceMappings(
                    sources[sourceIndex],
                    parsed.Modules,
                    this.XsdImporter.LastModuleGenerationResult);
            }
            parsedSources.Add(parsed);
            this.AppendSyntaxModules(parsed.Modules, syntaxModules);
            this.AppendTokens(parsed.LexerResult.Tokens, tokens);
            diagnostics.AddDiagnostics(parsed.LexerResult.Diagnostics);
            diagnostics.AddDiagnostics(parsed.ParserResult.Diagnostics);
            sourceIndex = sourceIndex + 1;
        }

        CompilerPublicModelBuilder publicModels =
            new CompilerPublicModelBuilder();
        AsnModule[] parsedModules = publicModels.BuildParsedModules(
            syntaxModules.ToArray());
        AsnCompilationResult result = this.CreateParsedResult(
            parsedSources,
            parsedModules,
            tokens,
            xsdResults,
            diagnostics,
            publicModels);
        if (diagnostics.HasErrors())
        {
            return result;
        }

        DiagnosticBag semanticDiagnostics = new DiagnosticBag(
            options.TreatWarningsAsErrors,
            new AsnResourceLimits());
        semanticDiagnostics.ApplyCompilerOptions(options);
        ModuleSyntax[] syntaxModuleArray = syntaxModules.ToArray();
        SymbolTable[] tables = new SymbolTableBuilder(semanticDiagnostics)
            .BuildTables(syntaxModuleArray);
        tables = new XsdCompilerIntegrator().AddSupportTable(
            tables,
            syntaxModuleArray,
            xsdResults.Length > 0 && xsdResults[0].Success);
        new ImportResolver(semanticDiagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(
            semanticDiagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification =
            new AssignmentClassifier(semanticDiagnostics)
                .ClassifyAssignments(tables, graph);
        bool semanticSuccess = graph.Success && classification.Success;
        if (semanticSuccess)
        {
            TypeResolutionResult types = new TypeResolver(
                semanticDiagnostics).ResolveTypes(tables);
            semanticSuccess = types.Success;
        }

        if (semanticSuccess)
        {
            ValueResolutionResult values = new ValueResolver(
                semanticDiagnostics).ResolveValues(tables);
            semanticSuccess = values.Success;
        }

        if (semanticSuccess)
        {
            XerInstructionResolutionResult xerInstructions =
                new XerInstructionResolver(semanticDiagnostics)
                    .ResolveInstructions(tables);
            semanticSuccess = xerInstructions.Success;
        }

        if (semanticSuccess)
        {
            PerInstructionResolutionResult perInstructions =
                new PerInstructionResolver(semanticDiagnostics)
                    .ResolveInstructions(tables);
            semanticSuccess = perInstructions.Success;
        }

        if (semanticSuccess)
        {
            semanticSuccess = new PerInstructionCompiler(
                semanticDiagnostics,
                perRegistry.Snapshot,
                options.EncodingRules).CompileTypes(tables);
        }

        if (semanticSuccess)
        {
            TagResolutionResult tags = new TagResolver(
                semanticDiagnostics).ResolveTags(tables);
            semanticSuccess = tags.Success;
        }

        if (semanticSuccess)
        {
            AutomaticTaggingResult automaticTags = new AutomaticTagger(
                semanticDiagnostics).ApplyAutomaticTags(tables);
            semanticSuccess = automaticTags.Success;
        }

        AsnDiagnostic[] validationDiagnostics =
            semanticDiagnostics.GetDiagnostics();
        diagnostics.AddDiagnostics(validationDiagnostics);
        AsnModule[] tableModules = publicModels.BuildTableModules(tables);
        AsnValidationResult validation = new AsnValidationResult
        {
            Success = semanticSuccess && !semanticDiagnostics.ContainsErrors(),
            Modules = tableModules,
            Assignments = publicModels.FlattenAssignments(tableModules),
            Diagnostics = validationDiagnostics
        };
        result.ValidationResult = validation;
        result.Modules = tableModules;
        result.Assignments = validation.Assignments;
        result.Diagnostics = diagnostics.GetDiagnostics();
        if (!validation.Success || diagnostics.HasErrors())
        {
            return result;
        }

        EncodingNeutralIrLowerer lowerer = new EncodingNeutralIrLowerer
        {
            PerEncodingRules = options.EncodingRules
        };
        LoweredCompilationIr compilation = lowerer.LowerCompilation(tables);
        compilation.PerInstructionRegistrations =
            perRegistry.Snapshot.GetRegistrations();
        int perModuleIndex = 0;
        while (perModuleIndex < compilation.Modules.Length)
        {
            compilation.Modules[perModuleIndex].PerInstructionRegistrations =
                perRegistry.Snapshot.GetRegistrations();
            perModuleIndex = perModuleIndex + 1;
        }
        AsnReflectionCatalog catalog = new ReflectionCatalogBuilder()
            .BuildCatalog(compilation);
        AsnModule[] validatedModules = this.GetCatalogModules(catalog);
        validation.Modules = validatedModules;
        validation.Assignments = publicModels.FlattenAssignments(
            validatedModules);
        result.ReflectionCatalog = catalog;
        result.Modules = validatedModules;
        result.Assignments = validation.Assignments;
        result.EncodingPlans = publicModels.CollectEncodingPlans(catalog);
        this.AttachPerInstructionRegistrations(
            result.EncodingPlans,
            perRegistry.Snapshot);

        if (input.GenerationOptions != null)
        {
            if (this.ValidateGenerationOptions(
                input.GenerationOptions,
                diagnostics))
            {
                CSharpGenerator generator = new CSharpGenerator();
                CSharpGenerationResult generation =
                    generator.GenerateCompilation(
                        compilation,
                        input.GenerationOptions);
                result.GeneratedFiles = generator.CreateFilesFromResult(
                    generation);
                diagnostics.AddDiagnostics(generation.Diagnostics);
            }
        }

        result.Diagnostics = diagnostics.GetDiagnostics();
        result.Success = !diagnostics.HasErrors();
        return result;
    }

    internal void AttachPerInstructionRegistrations(
        AsnEncodingPlan[] plans,
        PerInstructionRegistrySnapshot snapshot)
    {
        int index = 0;
        while (index < plans.Length)
        {
            plans[index].PerInstructionRegistrations =
                snapshot.GetRegistrations();
            index = index + 1;
        }
    }

    internal void AppendInputSources(
        AsnSourceUnit[]? inputSources,
        System.Collections.Generic.List<AsnSourceUnit> destination)
    {
        if (inputSources == null)
        {
            return;
        }

        int index = 0;
        while (index < inputSources.Length)
        {
            AsnSourceUnit source = inputSources[index];
            if (source == null)
            {
                throw new System.ArgumentException(
                    "Source units cannot contain null entries.",
                    "input");
            }

            destination.Add(source);
            index = index + 1;
        }
    }

    internal AsnXsdImportResult[] ImportXsdSources(
        AsnCompilationInput input,
        System.Collections.Generic.List<AsnSourceUnit> sources,
        CompilationDiagnosticSet diagnostics)
    {
        if (input.XsdInputs == null || input.XsdInputs.Length == 0)
        {
            return [];
        }

        if (this.XsdImporter == null)
        {
            AsnDiagnostic unavailable = new AsnDiagnostic
            {
                Severity = AsnDiagnosticSeverity.Error,
                Category = AsnDiagnosticCategory.Xsd,
                Code = "ASNXSD0001",
                Message = "XML Schema inputs require a configured schema importer."
            };
            diagnostics.AddDiagnostic(unavailable);
            return
            [
                new AsnXsdImportResult
                {
                    Success = false,
                    Diagnostics = [unavailable]
                }
            ];
        }

        AsnXsdImportResult imported = this.XsdImporter.ImportSchemas(
            input.XsdInputs,
            input.XsdImportOptions ?? new AsnXsdImportOptions());
        if (imported == null)
        {
            throw new System.InvalidOperationException(
                "The XML Schema importer returned no result.");
        }

        this.AppendInputSources(imported.SourceUnits, sources);
        diagnostics.AddDiagnostics(imported.Diagnostics);
        if (!imported.Success && !this.ContainsError(imported.Diagnostics))
        {
            diagnostics.AddDiagnostic(new AsnDiagnostic
            {
                Severity = AsnDiagnosticSeverity.Error,
                Category = AsnDiagnosticCategory.Xsd,
                Code = "ASNXSD0002",
                Message = "XML Schema import failed without an error diagnostic."
            });
        }

        return [imported];
    }

    internal CompilerSourceParseResult ParseSource(
        AsnSourceUnit source,
        AsnCompilerOptions options)
    {
        IncrementalLexSession lexer = new IncrementalLexSession(
            source.Name,
            options.TreatWarningsAsErrors,
            new AsnResourceLimits());
        lexer.UseCompilerOptions(options);
        lexer.SupplyTextChunk(source.Text, true);
        TokenCursor cursor = new TokenCursor(lexer, 4);
        ModuleCollectionParseResult parsed = new ModuleParser(cursor)
            .ParseModuleList();
        AsnDiagnostic[] phaseDiagnostics = lexer.GetDiagnostics();
        CompilerPublicModelBuilder publicModels =
            new CompilerPublicModelBuilder();
        AsnModule[] modules = publicModels.BuildParsedModules(parsed.Modules);
        return new CompilerSourceParseResult
        {
            Modules = parsed.Modules,
            LexerResult = new AsnLexerResult
            {
                Tokens = this.MergeTokens(cursor),
                Diagnostics = this.SelectLexerDiagnostics(phaseDiagnostics),
                ConsumedCharacterCount = lexer.GetConsumedCharacterCount(),
                Complete = this.HasEndToken(cursor)
            },
            ParserResult = new AsnParserResult
            {
                Modules = modules,
                Assignments = publicModels.FlattenAssignments(modules),
                Diagnostics = this.SelectParserDiagnostics(phaseDiagnostics),
                ConsumedTokenCount = parsed.ConsumedTokenCount,
                Complete = parsed.Status == ModuleParseStatus.Parsed
            }
        };
    }

    internal AsnToken[] MergeTokens(TokenCursor cursor)
    {
        System.Collections.Generic.List<AsnToken> merged =
            new System.Collections.Generic.List<AsnToken>();
        int index = 0;
        while (index < cursor.Tokens.Count)
        {
            merged.Add(cursor.Tokens[index]);
            index = index + 1;
        }

        AsnToken[] trivia = cursor.GetTriviaTokens();
        index = 0;
        while (index < trivia.Length)
        {
            merged.Add(trivia[index]);
            index = index + 1;
        }

        int insertionIndex = 1;
        while (insertionIndex < merged.Count)
        {
            AsnToken current = merged[insertionIndex];
            int position = insertionIndex;
            while (position > 0
                && this.CompareTokenPosition(
                    merged[position - 1],
                    current) > 0)
            {
                merged[position] = merged[position - 1];
                position = position - 1;
            }

            merged[position] = current;
            insertionIndex = insertionIndex + 1;
        }

        return merged.ToArray();
    }

    internal int CompareTokenPosition(AsnToken left, AsnToken right)
    {
        int leftOffset = left.Span == null ? int.MaxValue : left.Span.Start.Offset;
        int rightOffset = right.Span == null ? int.MaxValue : right.Span.Start.Offset;
        if (leftOffset != rightOffset)
        {
            return leftOffset < rightOffset ? -1 : 1;
        }

        int leftEnd = left.Span == null ? int.MaxValue : left.Span.End.Offset;
        int rightEnd = right.Span == null ? int.MaxValue : right.Span.End.Offset;
        if (leftEnd != rightEnd)
        {
            return leftEnd < rightEnd ? -1 : 1;
        }

        return (int)left.Kind - (int)right.Kind;
    }

    internal AsnDiagnostic[] SelectLexerDiagnostics(
        AsnDiagnostic[] diagnostics)
    {
        System.Collections.Generic.List<AsnDiagnostic> result =
            new System.Collections.Generic.List<AsnDiagnostic>();
        int index = 0;
        while (index < diagnostics.Length)
        {
            AsnDiagnosticCategory category = diagnostics[index].Category;
            if (category == AsnDiagnosticCategory.Lexical
                || category == AsnDiagnosticCategory.Source
                || category == AsnDiagnosticCategory.Resource)
            {
                result.Add(diagnostics[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal AsnDiagnostic[] SelectParserDiagnostics(
        AsnDiagnostic[] diagnostics)
    {
        System.Collections.Generic.List<AsnDiagnostic> result =
            new System.Collections.Generic.List<AsnDiagnostic>();
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Category == AsnDiagnosticCategory.Syntax)
            {
                result.Add(diagnostics[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool HasEndToken(TokenCursor cursor)
    {
        return cursor.Tokens.Count > 0
            && cursor.Tokens[cursor.Tokens.Count - 1].Kind
                == AsnTokenKind.EndOfInput;
    }

    internal bool ContainsError(AsnDiagnostic[] diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Severity == AsnDiagnosticSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AppendSyntaxModules(
        ModuleSyntax[] modules,
        System.Collections.Generic.List<ModuleSyntax> destination)
    {
        int index = 0;
        while (index < modules.Length)
        {
            destination.Add(modules[index]);
            index = index + 1;
        }
    }

    internal void AppendTokens(
        AsnToken[] source,
        System.Collections.Generic.List<AsnToken> destination)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal AsnCompilationResult CreateParsedResult(
        System.Collections.Generic.List<CompilerSourceParseResult> parsedSources,
        AsnModule[] modules,
        System.Collections.Generic.List<AsnToken> tokens,
        AsnXsdImportResult[] xsdResults,
        CompilationDiagnosticSet diagnostics,
        CompilerPublicModelBuilder publicModels)
    {
        AsnLexerResult[] lexerResults =
            new AsnLexerResult[parsedSources.Count];
        AsnParserResult[] parserResults =
            new AsnParserResult[parsedSources.Count];
        int index = 0;
        while (index < parsedSources.Count)
        {
            lexerResults[index] = parsedSources[index].LexerResult;
            parserResults[index] = parsedSources[index].ParserResult;
            index = index + 1;
        }

        return new AsnCompilationResult
        {
            Diagnostics = diagnostics.GetDiagnostics(),
            Tokens = tokens.ToArray(),
            Modules = modules,
            Assignments = publicModels.FlattenAssignments(modules),
            LexerResults = lexerResults,
            ParserResults = parserResults,
            XsdImportResults = xsdResults
        };
    }

    internal AsnModule[] GetCatalogModules(AsnReflectionCatalog catalog)
    {
        AsnModule[] modules = new AsnModule[catalog.Modules.Length];
        int index = 0;
        while (index < modules.Length)
        {
            modules[index] = catalog.Modules[index].SourceModule
                ?? throw new System.InvalidOperationException(
                    "The reflection module has no source module.");
            index = index + 1;
        }

        return modules;
    }

    internal bool ValidateGenerationOptions(
        AsnGenerationOptions options,
        CompilationDiagnosticSet diagnostics)
    {
        bool valid = true;
        if (string.IsNullOrWhiteSpace(options.NamespaceName))
        {
            diagnostics.AddDiagnostic(new AsnDiagnostic
            {
                Severity = AsnDiagnosticSeverity.Error,
                Category = AsnDiagnosticCategory.Generation,
                Code = "ASNGEN0002",
                Message = "Generation requires a non-empty namespace name."
            });
            valid = false;
        }

        int target = (int)options.Target;
        if (target < (int)AsnCodeGenerationTarget.ModelOnly
            || target > (int)AsnCodeGenerationTarget.Combined)
        {
            diagnostics.AddDiagnostic(new AsnDiagnostic
            {
                Severity = AsnDiagnosticSeverity.Error,
                Category = AsnDiagnosticCategory.Generation,
                Code = "ASNGEN0003",
                Message = "The requested generation target is not supported."
            });
            valid = false;
        }

        return valid;
    }
}
