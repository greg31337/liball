using asn1;

namespace asn1.Tests;

public class PerInstructionCompilerTests
{
    [Test]
    public void CompilerCarriesFinalInstructionsAndHandlerStepsThroughGeneration()
    {
        TestRegistry registry = new TestRegistry
        {
            Registrations =
            [
                Registration("CUSTOM", "handler.custom", true, true, [])
            ]
        };

        AsnCompilationResult result = Compile(
            "Lowering DEFINITIONS ::= BEGIN "
                + "Value ::= [PER:CUSTOM detail] INTEGER END",
            AsnEncodingRules.Uper,
            registry,
            true);
        AsnTypeDescriptor type = result.ReflectionCatalog?.Modules.Single()
            .Assignments.Single().Type ?? throw new InvalidOperationException();
        AsnEncodingDescriptor encoding = type.Encodings.Single();
        string generated = string.Join(
            "\n",
            result.GeneratedFiles.Select(file => file.Content));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result));
            Assert.That(encoding.EncodingRules, Is.EqualTo(AsnEncodingRules.Uper));
            Assert.That(encoding.Instructions.Single().Name, Is.EqualTo("CUSTOM"));
            Assert.That(encoding.Instructions.Single().Arguments,
                Is.EqualTo(new[] { "detail" }));
            Assert.That(encoding.Plan?.Steps.Single().Kind,
                Is.EqualTo(AsnPlanStepKind.Replacement));
            Assert.That(encoding.Plan?.Steps.Single().Path,
                Is.EqualTo("CUSTOM-step"));
            Assert.That(encoding.Plan?.PerInstructionRegistrations.Single()
                .HandlerId, Is.EqualTo("handler.custom"));
            Assert.That(registry.ValidationCount, Is.EqualTo(1));
            Assert.That(registry.ApplicationCount, Is.EqualTo(1));
            Assert.That(generated, Does.Contain("AsnInstructionKind.PerApplication"));
            Assert.That(generated, Does.Contain("AsnPlanStepKind.Replacement"));
            Assert.That(generated, Does.Contain("handler.custom"));
            Assert.That(generated, Does.Contain("detail"));
        });
    }

    [Test]
    public void CompilerReportsUnknownApplicabilityConflictAndReversibilityFailures()
    {
        TestRegistry registry = new TestRegistry
        {
            Registrations =
            [
                Registration("UNSUPPORTED", "handler.unsupported", false, true, []),
                Registration("FIRST", "handler.first", true, true, ["SECOND"]),
                Registration("SECOND", "handler.second", true, true, []),
                Registration("ONEWAY", "handler.oneway", true, false, []),
                Registration("INVALID", "handler.invalid", true, true, [])
            ],
            InvalidKeyword = "INVALID"
        };

        AsnCompilationResult result = Compile(
            "Failures DEFINITIONS ::= BEGIN "
                + "Value ::= [PER:UNKNOWN] [PER:UNSUPPORTED] "
                + "[PER:FIRST] [PER:SECOND] [PER:ONEWAY] "
                + "[PER:INVALID] INTEGER END",
            AsnEncodingRules.Uper,
            registry,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain("ASNE0188"));
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain("ASNE0189"));
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain("ASNE0190"));
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain("ASNE0191"));
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain("APP0001"));
            Assert.That(result.Diagnostics.All(item => item.Span != null), Is.True);
            Assert.That(result.ReflectionCatalog, Is.Null);
        });
    }

    [Test]
    public void AlignedPerIgnoresFinalInstructionsWithoutARegistry()
    {
        AsnCompilationResult result = Compile(
            "Aligned DEFINITIONS ::= BEGIN "
                + "Value ::= [PER:UNKNOWN detail] INTEGER END",
            AsnEncodingRules.Aper,
            null,
            false);
        AsnEncodingDescriptor encoding = result.ReflectionCatalog?.Modules.Single()
            .Assignments.Single().Type?.Encodings.Single()
            ?? throw new InvalidOperationException();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result));
            Assert.That(encoding.EncodingRules, Is.EqualTo(AsnEncodingRules.Aper));
            Assert.That(encoding.Instructions.Single().Name, Is.EqualTo("UNKNOWN"));
            Assert.That(encoding.Plan?.Steps, Is.Empty);
        });
    }

    [Test]
    public void ComponentsOfKeepsOccurrenceSpecificPerLowering()
    {
        TestRegistry registry = new TestRegistry
        {
            Registrations =
            [
                Registration("CUSTOM", "handler.custom", true, true, [])
            ]
        };
        AsnCompilationResult result = Compile(
            "Occurrences DEFINITIONS ::= BEGIN "
                + "Base ::= SEQUENCE { value [PER:CUSTOM original] INTEGER }; "
                + "Derived ::= SEQUENCE { COMPONENTS OF Base } "
                + "ENCODING-CONTROL PER [CUSTOM derived] Derived.value END",
            AsnEncodingRules.Uper,
            registry,
            false);
        AsnTypeDescriptor derived = result.ReflectionCatalog?.Modules.Single()
            .Assignments.Single(item => item.Name == "Derived").Type
            ?? throw new InvalidOperationException();
        AsnEncodingDescriptor occurrence = derived.Fields.Single().Encodings.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result));
            Assert.That(occurrence.Instructions.Single().Arguments,
                Is.EqualTo(new[] { "derived" }));
            Assert.That(occurrence.Plan?.Steps.Single().Path,
                Is.EqualTo("CUSTOM-step"));
        });
    }

    private static AsnCompilationResult Compile(
        string text,
        AsnEncodingRules rules,
        IPerInstructionRegistry? registry,
        bool generate)
    {
        return new CompilerImplementation().Compile(new AsnCompilationInput
        {
            SourceUnits = [new AsnSourceUnit { Name = "per-compiler.asn", Text = text }],
            CompilerOptions = new AsnCompilerOptions { EncodingRules = rules },
            PerInstructionRegistry = registry,
            GenerationOptions = generate
                ? new AsnGenerationOptions
                {
                    NamespaceName = "Generated.PerInstructions",
                    Target = AsnCodeGenerationTarget.DescriptorOnly
                }
                : null
        });
    }

    private static AsnPerInstructionRegistration Registration(
        string keyword,
        string handlerId,
        bool supportsUnaligned,
        bool reversible,
        string[] conflicts)
    {
        return new AsnPerInstructionRegistration
        {
            Keyword = keyword,
            HandlerId = handlerId,
            SupportsUnaligned = supportsUnaligned,
            Reversible = reversible,
            ConflictingKeywords = conflicts
        };
    }

    private static string Format(AsnCompilationResult result)
    {
        return string.Join(
            " | ",
            result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }

    private class TestRegistry : IPerInstructionRegistry, IPerInstructionHandler
    {
        public AsnPerInstructionRegistration[] Registrations = [];
        public string InvalidKeyword = "";
        public int ValidationCount;
        public int ApplicationCount;

        public void RegisterHandler(
            AsnPerInstructionRegistration registration,
            IPerInstructionHandler handler)
        {
            throw new NotSupportedException();
        }

        public IPerInstructionHandler? ResolveHandler(string keyword)
        {
            return this.Registrations.Any(item => item.Keyword == keyword)
                ? this
                : null;
        }

        public AsnPerInstructionRegistration[] GetRegistrations()
        {
            return this.Registrations;
        }

        public AsnDiagnostic[] ValidateInstruction(
            AsnEncodingInstruction instruction,
            AsnType type)
        {
            this.ValidationCount = this.ValidationCount + 1;
            return instruction.Name == this.InvalidKeyword
                ?
                [
                    new AsnDiagnostic
                    {
                        Severity = AsnDiagnosticSeverity.Error,
                        Category = AsnDiagnosticCategory.Encoding,
                        Code = "APP0001",
                        Message = "The application handler rejected this type."
                    }
                ]
                : [];
        }

        public AsnEncodingPlanStep[] ApplyInstruction(
            AsnEncodingInstruction instruction,
            AsnType type,
            AsnEncodingPlan plan)
        {
            this.ApplicationCount = this.ApplicationCount + 1;
            return
            [
                new AsnEncodingPlanStep
                {
                    Kind = AsnPlanStepKind.Replacement,
                    Instruction = instruction,
                    Path = instruction.Name + "-step"
                }
            ];
        }
    }
}
