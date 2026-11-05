namespace asn1;

internal class EcnCompilerImplementation : IEcnCompiler
{
    public AsnCompilationResult CompileEcn(
        AsnSourceUnit source,
        System.ReadOnlySpan<AsnModule> modules)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }
        AsnModule[] contextModules = modules.ToArray();
        this.ValidateModules(contextModules);
        IncrementalLexSession lexer = new IncrementalLexSession(
            source.Name,
            false,
            new AsnResourceLimits());
        lexer.SupplyTextChunk(source.Text, true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        bool complete = false;
        while (!complete)
        {
            IncrementalLexResult next = lexer.ScanNext();
            if (next.Status == IncrementalLexStatus.EndOfInput)
            {
                complete = true;
            }
            else if (next.Status == IncrementalLexStatus.NeedMoreData)
            {
                lexer.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Ecn,
                    EcnDiagnosticCodes.IntegrationError,
                    "The final ECN source ended before lexical analysis completed.",
                    source.GetSpan(source.Text.Length, source.Text.Length),
                    null);
                complete = true;
            }
            else if (next.Token != null)
            {
                tokens.Add(next.Token);
            }
        }

        EcnParseResult parsed = new EcnSyntaxParser(tokens.ToArray())
            .ParseModuleSyntax();
        AsnDiagnostic? parseDiagnostic = null;
        if (!parsed.Success || parsed.Module == null)
        {
            parseDiagnostic = new AsnDiagnostic
            {
                Severity = AsnDiagnosticSeverity.Error,
                Category = AsnDiagnosticCategory.Ecn,
                Code = DiagnosticBag.CreateCode(
                    AsnDiagnosticCategory.Ecn,
                    EcnDiagnosticCodes.IntegrationError),
                Message = parsed.ErrorMessage.Length == 0
                    ? "The ECN source could not be parsed."
                    : parsed.ErrorMessage,
                Span = parsed.ErrorToken == null
                    ? source.GetSpan(source.Text.Length, source.Text.Length)
                    : parsed.ErrorToken.Span
            };
        }
        AsnDiagnostic[] lexicalDiagnostics = lexer.GetDiagnostics();
        if (parseDiagnostic != null || this.ContainsErrors(lexicalDiagnostics))
        {
            AsnDiagnostic[] failedDiagnostics = parseDiagnostic == null
                ? lexicalDiagnostics
                : this.AppendDiagnostic(lexicalDiagnostics, parseDiagnostic);
            new EcnDiagnosticContextEnricher().EnrichDiagnostics(
                failedDiagnostics,
                null,
                parsed.Module,
                "syntax");
            return this.CreateFailedResult(
                contextModules,
                tokens.ToArray(),
                lexicalDiagnostics,
                failedDiagnostics,
                parseDiagnostic);
        }

        EcnModuleSyntax module = parsed.Module
            ?? throw new System.InvalidOperationException();
        EcnSymbolTable table = new EcnSymbolTableBuilder(lexer.Diagnostics)
            .BuildTable(module, contextModules);
        AsnDiagnostic[] diagnostics = new EcnDiagnosticContextEnricher()
            .EnrichDiagnostics(
                lexer.GetDiagnostics(),
                table,
                module,
                "semantic");
        EcnPublicPlanBuilder publicPlans = new EcnPublicPlanBuilder();
        AsnEncodingPlan[] plans = publicPlans.BuildPlans(table);
        AsnModule ecnModule = new AsnModule
        {
            Name = module.Name,
            EncodingInstructions = this.CollectPlanInstructions(plans),
            Span = publicPlans.CloneSpan(module.Span)
        };
        AsnModule[] resultModules = this.AppendModule(
            contextModules,
            ecnModule);
        AsnAssignment[] assignments = this.FlattenAssignments(contextModules);
        bool success = !this.ContainsErrors(diagnostics);
        AsnReflectionCatalog? catalog = success
            ? new EcnReflectionCatalogIntegrator().BuildIntegratedCatalog(
                table,
                contextModules,
                plans)
            : null;
        return new AsnCompilationResult
        {
            Success = success,
            Diagnostics = diagnostics,
            Tokens = tokens.ToArray(),
            Assignments = assignments,
            Modules = resultModules,
            EncodingPlans = plans,
            ReflectionCatalog = catalog,
            LexerResults =
            [
                new AsnLexerResult
                {
                    Tokens = tokens.ToArray(),
                    Diagnostics = lexicalDiagnostics,
                    ConsumedCharacterCount = lexer.GetConsumedCharacterCount(),
                    Complete = true
                }
            ],
            ParserResults =
            [
                new AsnParserResult
                {
                    Modules = [ecnModule],
                    Diagnostics = [],
                    ConsumedTokenCount = tokens.Count,
                    Complete = true
                }
            ],
            ValidationResult = new AsnValidationResult
            {
                Success = success,
                Modules = resultModules,
                Assignments = assignments,
                Diagnostics = diagnostics
            }
        };
    }

    internal void ValidateModules(AsnModule[] modules)
    {
        int index = 0;
        while (index < modules.Length)
        {
            if (modules[index] == null)
            {
                throw new System.ArgumentException(
                    "ASN.1 context modules cannot contain null entries.",
                    "modules");
            }
            index = index + 1;
        }
    }

    internal bool ContainsErrors(AsnDiagnostic[] diagnostics)
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

    internal AsnCompilationResult CreateFailedResult(
        AsnModule[] modules,
        AsnToken[] tokens,
        AsnDiagnostic[] lexicalDiagnostics,
        AsnDiagnostic[] diagnostics,
        AsnDiagnostic? parseDiagnostic)
    {
        return new AsnCompilationResult
        {
            Diagnostics = diagnostics,
            Tokens = tokens,
            Modules = modules,
            Assignments = this.FlattenAssignments(modules),
            LexerResults =
            [
                new AsnLexerResult
                {
                    Tokens = tokens,
                    Diagnostics = lexicalDiagnostics,
                    Complete = true
                }
            ],
            ParserResults =
            [
                new AsnParserResult
                {
                    Diagnostics = parseDiagnostic == null
                        ? []
                        : [parseDiagnostic],
                    ConsumedTokenCount = tokens.Length
                }
            ]
        };
    }

    internal AsnDiagnostic[] AppendDiagnostic(
        AsnDiagnostic[] source,
        AsnDiagnostic value)
    {
        AsnDiagnostic[] result = new AsnDiagnostic[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        result[source.Length] = value;
        return result;
    }

    internal AsnModule[] AppendModule(AsnModule[] source, AsnModule value)
    {
        AsnModule[] result = new AsnModule[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        result[source.Length] = value;
        return result;
    }

    internal AsnAssignment[] FlattenAssignments(AsnModule[] modules)
    {
        System.Collections.Generic.List<AsnAssignment> result =
            new System.Collections.Generic.List<AsnAssignment>();
        int moduleIndex = 0;
        while (moduleIndex < modules.Length)
        {
            int assignmentIndex = 0;
            while (assignmentIndex < modules[moduleIndex].Assignments.Length)
            {
                result.Add(modules[moduleIndex].Assignments[assignmentIndex]);
                assignmentIndex = assignmentIndex + 1;
            }
            moduleIndex = moduleIndex + 1;
        }
        return result.ToArray();
    }

    internal AsnEncodingInstruction[] CollectPlanInstructions(
        AsnEncodingPlan[] plans)
    {
        System.Collections.Generic.List<AsnEncodingInstruction> result =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        int planIndex = 0;
        while (planIndex < plans.Length)
        {
            int instructionIndex = 0;
            while (instructionIndex < plans[planIndex].Instructions.Length)
            {
                result.Add(plans[planIndex].Instructions[instructionIndex]);
                instructionIndex = instructionIndex + 1;
            }
            planIndex = planIndex + 1;
        }
        return result.ToArray();
    }
}
