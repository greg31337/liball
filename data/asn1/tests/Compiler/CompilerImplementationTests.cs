using asn1;

namespace asn1.Tests;

public class CompilerImplementationTests
{
    [Test]
    public void CompileRunsAllPhasesAndGeneratesFromTheLoweredCompilation()
    {
        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits =
                [
                    Source(
                        "client.asn",
                        "Client DEFINITIONS ::= BEGIN "
                            + "IMPORTS Base FROM Library; "
                            + "Use ::= SEQUENCE { value Base } END"),
                    Source(
                        "library.asn",
                        "Library DEFINITIONS ::= BEGIN "
                            + "EXPORTS Base; Base ::= INTEGER END")
                ],
                GenerationOptions = new AsnGenerationOptions
                {
                    NamespaceName = "Generated.Orchestration",
                    Target = AsnCodeGenerationTarget.Combined,
                    IncludeSourceMappings = true
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result.Diagnostics));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.LexerResults, Has.Length.EqualTo(2));
            Assert.That(result.ParserResults, Has.Length.EqualTo(2));
            Assert.That(result.LexerResults.All(item => item.Complete), Is.True);
            Assert.That(result.ParserResults.All(item => item.Complete), Is.True);
            Assert.That(result.Tokens, Is.Not.Empty);
            Assert.That(result.ValidationResult?.Success, Is.True);
            Assert.That(result.ReflectionCatalog, Is.Not.Null);
            Assert.That(result.Modules.Select(item => item.Name),
                Is.EqualTo(new[] { "Library", "Client" }));
            Assert.That(result.Assignments.Select(item => item.Name),
                Is.EqualTo(new[] { "Base", "Use" }));
            Assert.That(result.GeneratedFiles, Is.Not.Empty);
            Assert.That(result.GeneratedFiles.All(item =>
                item.RelativePath.StartsWith("Generated/Orchestration/")),
                Is.True);
            Assert.That(result.GeneratedFiles.SelectMany(item =>
                item.SourceSpans).Select(item => item.Start.SourceName),
                Does.Contain("client.asn"));
        });
    }

    [Test]
    public void CompilePreservesLexicalAndParsedArtifactsAfterSyntaxFailure()
    {
        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits =
                [
                    Source(
                        "partial.asn",
                        "Partial DEFINITIONS ::= BEGIN Good ::= INTEGER")
                ],
                GenerationOptions = new AsnGenerationOptions
                {
                    NamespaceName = "Generated.Partial",
                    Target = AsnCodeGenerationTarget.ModelOnly
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Category),
                Does.Contain(AsnDiagnosticCategory.Syntax));
            Assert.That(result.Tokens, Is.Not.Empty);
            Assert.That(result.LexerResults, Has.Length.EqualTo(1));
            Assert.That(result.LexerResults[0].Complete, Is.True);
            Assert.That(result.ParserResults, Has.Length.EqualTo(1));
            Assert.That(result.ParserResults[0].Complete, Is.False);
            Assert.That(result.Modules.Select(item => item.Name),
                Does.Contain("Partial"));
            Assert.That(result.Assignments.Select(item => item.Name),
                Does.Contain("Good"));
            Assert.That(result.ValidationResult, Is.Null);
            Assert.That(result.ReflectionCatalog, Is.Null);
            Assert.That(result.GeneratedFiles, Is.Empty);
        });
    }

    [Test]
    public void CompilePreservesClassifiedArtifactsAfterSemanticFailure()
    {
        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits =
                [
                    Source(
                        "broken.asn",
                        "Broken DEFINITIONS ::= BEGIN Bad ::= Missing END")
                ],
                GenerationOptions = new AsnGenerationOptions
                {
                    NamespaceName = "Generated.Broken",
                    Target = AsnCodeGenerationTarget.ModelOnly
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationResult, Is.Not.Null);
            Assert.That(result.ValidationResult?.Success, Is.False);
            Assert.That(result.Modules.Single().Name, Is.EqualTo("Broken"));
            Assert.That(result.Assignments.Single().Name, Is.EqualTo("Bad"));
            Assert.That(result.Assignments.Single().Kind,
                Is.EqualTo(AsnAssignmentKind.Type));
            Assert.That(result.Diagnostics.Any(item =>
                item.Category == AsnDiagnosticCategory.Binding
                    || item.Category == AsnDiagnosticCategory.Semantic),
                Is.True);
            Assert.That(result.ReflectionCatalog, Is.Null);
            Assert.That(result.GeneratedFiles, Is.Empty);
        });
    }

    [Test]
    public void GenerationFailureRetainsSuccessfulValidationAndReflection()
    {
        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits =
                [
                    Source(
                        "valid.asn",
                        "Valid DEFINITIONS ::= BEGIN Number ::= INTEGER END")
                ],
                GenerationOptions = new AsnGenerationOptions
                {
                    NamespaceName = "",
                    Target = AsnCodeGenerationTarget.ModelOnly
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationResult?.Success, Is.True);
            Assert.That(result.ReflectionCatalog, Is.Not.Null);
            Assert.That(result.Modules.Single().Name, Is.EqualTo("Valid"));
            Assert.That(result.GeneratedFiles, Is.Empty);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new[] { "ASNGEN0002" }));
        });
    }

    [Test]
    public void FailureDiagnosticsAreStableAcrossInputOrder()
    {
        AsnSourceUnit alpha = Source(
            "alpha.asn",
            "Alpha DEFINITIONS ::= BEGIN A ::= MissingA END");
        AsnSourceUnit zeta = Source(
            "zeta.asn",
            "Zeta DEFINITIONS ::= BEGIN Z ::= MissingZ END");
        CompilerImplementation compiler = new CompilerImplementation();

        AsnCompilationResult forward = compiler.Compile(
            new AsnCompilationInput { SourceUnits = [zeta, alpha] });
        AsnCompilationResult reverse = compiler.Compile(
            new AsnCompilationInput { SourceUnits = [alpha, zeta] });

        Assert.That(
            forward.Diagnostics.Select(Signature),
            Is.EqualTo(reverse.Diagnostics.Select(Signature)));
        Assert.That(forward.Diagnostics.Select(item =>
            item.Span?.Start.SourceName),
            Is.EqualTo(new[] { "alpha.asn", "zeta.asn" }));
    }

    [Test]
    public void CompileCapturesAnOrderedImmutablePerRegistrySnapshot()
    {
        MutableInstructionRegistry registry = new MutableInstructionRegistry
        {
            Registrations =
            [
                Registration("ZETA", "handler.zeta"),
                Registration("ALPHA", "handler.alpha")
            ],
            HandlerKeywords = ["ALPHA", "ZETA"],
            MutateRegistrationsOnResolve = true
        };

        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits =
                [
                    Source(
                        "registry.asn",
                        "Registry DEFINITIONS ::= BEGIN Number ::= INTEGER "
                            + "ENCODING-CONTROL PER [ALPHA] Number END")
                ],
                PerInstructionRegistry = registry
            });
        registry.Registrations[0].Keyword = "CHANGED-AFTER-COMPILE";

        Assert.That(result.Success, Is.True, Format(result.Diagnostics));
        Assert.That(result.EncodingPlans, Is.Not.Empty);
        Assert.That(result.EncodingPlans.All(plan =>
                plan.PerInstructionRegistrations.Select(item => item.Keyword)
                    .SequenceEqual(new[] { "ALPHA", "ZETA" })),
            Is.True);
        Assert.That(result.EncodingPlans.All(plan =>
                plan.PerInstructionRegistrations.Select(item => item.HandlerId)
                    .SequenceEqual(new[] { "handler.alpha", "handler.zeta" })),
            Is.True);
    }

    [Test]
    public void RegistryValidationDiagnosticsAreDeterministic()
    {
        AsnPerInstructionRegistration[] registrations =
        [
            Registration("MISSING", "handler.missing"),
            Registration("DUPLICATE", "handler.zeta"),
            Registration("", "handler.empty"),
            Registration("NO-ID", ""),
            Registration("DUPLICATE", "handler.alpha")
        ];
        MutableInstructionRegistry forwardRegistry = new MutableInstructionRegistry
        {
            Registrations = registrations,
            HandlerKeywords = ["DUPLICATE"]
        };
        MutableInstructionRegistry reverseRegistry = new MutableInstructionRegistry
        {
            Registrations = registrations.Reverse().ToArray(),
            HandlerKeywords = ["DUPLICATE"]
        };

        AsnCompilationResult forward = CompileWithRegistry(forwardRegistry);
        AsnCompilationResult reverse = CompileWithRegistry(reverseRegistry);

        Assert.Multiple(() =>
        {
            Assert.That(forward.Success, Is.False);
            Assert.That(forward.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new[]
                {
                    "ASNPER0002",
                    "ASNPER0003",
                    "ASNPER0004",
                    "ASNPER0005"
                }));
            Assert.That(forward.Diagnostics.Select(Signature),
                Is.EqualTo(reverse.Diagnostics.Select(Signature)));
        });
    }

    private static AsnCompilationResult CompileWithRegistry(
        IPerInstructionRegistry registry)
    {
        return new CompilerImplementation().Compile(new AsnCompilationInput
        {
            SourceUnits =
            [
                Source(
                    "registry-validation.asn",
                    "RegistryValidation DEFINITIONS ::= BEGIN Value ::= INTEGER END")
            ],
            PerInstructionRegistry = registry
        });
    }

    private static AsnPerInstructionRegistration Registration(
        string keyword,
        string handlerId)
    {
        return new AsnPerInstructionRegistration
        {
            Keyword = keyword,
            HandlerId = handlerId,
            SupportsUnaligned = true
        };
    }

    private static AsnSourceUnit Source(string name, string text)
    {
        return new AsnSourceUnit
        {
            Name = name,
            Text = text
        };
    }

    private static string Signature(AsnDiagnostic diagnostic)
    {
        return diagnostic.Code + ":" + diagnostic.Message + ":"
            + diagnostic.Span?.Start.SourceName + ":"
            + diagnostic.Span?.Start.Offset.ToString();
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(Signature));
    }

    private class MutableInstructionRegistry :
        IPerInstructionRegistry,
        IPerInstructionHandler
    {
        public AsnPerInstructionRegistration[] Registrations = [];
        public string[] HandlerKeywords = [];
        public bool MutateRegistrationsOnResolve;
        public bool Mutated;

        public void RegisterHandler(
            AsnPerInstructionRegistration registration,
            IPerInstructionHandler handler)
        {
            throw new NotSupportedException();
        }

        public IPerInstructionHandler? ResolveHandler(string keyword)
        {
            if (this.MutateRegistrationsOnResolve && !this.Mutated)
            {
                this.Mutated = true;
                int index = 0;
                while (index < this.Registrations.Length)
                {
                    this.Registrations[index].Keyword = "MUTATED";
                    this.Registrations[index].HandlerId = "mutated.handler";
                    index = index + 1;
                }
            }

            return this.HandlerKeywords.Contains(keyword) ? this : null;
        }

        public AsnPerInstructionRegistration[] GetRegistrations()
        {
            return this.Registrations;
        }

        public AsnDiagnostic[] ValidateInstruction(
            AsnEncodingInstruction instruction,
            AsnType type)
        {
            return [];
        }

        public AsnEncodingPlanStep[] ApplyInstruction(
            AsnEncodingInstruction instruction,
            AsnType type,
            AsnEncodingPlan plan)
        {
            return [];
        }
    }
}
