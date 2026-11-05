using asn1;

namespace asn1.Tests;

public class EcnCompilerIntegrationTests
{
    [Test]
    public void CompilesPublicPlansAndAttachesThemToReflectionMetadata()
    {
        AsnModule schema = SchemaModule();
        AsnCompilationResult result = new EcnCompiler(
            new EcnCompilerImplementation()).CompileEcn(
                Source(
                    "Integrated ENCODING-DEFINITIONS ::= BEGIN\n"
                        + "#Byte ::= #INT\n"
                        + "scalar #Byte ::= { ENCODING-SPACE, SIZE 8 }\n"
                        + "END"),
                [schema]);

        AsnEncodingPlan plan = result.EncodingPlans.Single();
        AsnModuleDescriptor reflectedSchema = result.ReflectionCatalog
            ?.Modules.Single(module => module.Name == "Schema")
            ?? throw new AssertionException("The schema module was not reflected.");
        AsnTypeDescriptor reflectedScalar = reflectedSchema.Types.Single(
            type => type.Name == "scalar");
        AsnModuleDescriptor reflectedEcn = result.ReflectionCatalog.Modules
            .Single(module => module.Name == "Integrated");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.ValidationResult?.Success, Is.True);
            Assert.That(result.LexerResults.Single().Complete, Is.True);
            Assert.That(result.ParserResults.Single().Complete, Is.True);
            Assert.That(result.Modules.Select(module => module.Name),
                Is.EqualTo(new[] { "Schema", "Integrated" }));
            Assert.That(plan.EncodingRules, Is.EqualTo(AsnEncodingRules.Ecn));
            Assert.That(plan.Steps.Single().Kind,
                Is.EqualTo(AsnPlanStepKind.Primitive));
            Assert.That(plan.Steps.Single().Children.Any(step =>
                step.Kind == AsnPlanStepKind.Length), Is.True);
            Assert.That(plan.Instructions.All(instruction =>
                instruction.Kind == AsnInstructionKind.Ecn), Is.True);
            Assert.That(plan.EcnClasses.Select(value => value.Name),
                Does.Contain("#Byte"));
            Assert.That(plan.EcnObjects.Select(value => value.Name),
                Does.Contain("scalar"));
            Assert.That(reflectedScalar.Encodings.Single().Plan,
                Is.SameAs(plan));
            Assert.That(reflectedEcn.Encodings.Single().Plan,
                Is.SameAs(plan));
            Assert.That(reflectedEcn.SourceModule?.EncodingInstructions,
                Is.EqualTo(plan.Instructions));
        });
    }

    [Test]
    public void ReportsParserAndSemanticDiagnosticsThroughCompilationResult()
    {
        EcnCompiler compiler = new EcnCompiler(new EcnCompilerImplementation());
        AsnCompilationResult parsed = compiler.CompileEcn(
            Source("Broken ENCODING-DEFINITIONS ::= BEGIN scalar"),
            []);
        AsnCompilationResult semantic = compiler.CompileEcn(
            Source(
                "Invalid ENCODING-DEFINITIONS ::= BEGIN\n"
                    + "#Logical ::= #BOOL\n"
                    + "bad #Logical ::= { ALPHABET \"AB\" }\n"
                    + "END"),
            []);
        AsnDiagnostic parserDiagnostic = parsed.Diagnostics.Single(value =>
            value.Code == "ASNECN0062");
        AsnDiagnostic semanticDiagnostic = semantic.Diagnostics.First(value =>
            value.Severity == AsnDiagnosticSeverity.Error
                && value.Category == AsnDiagnosticCategory.Ecn);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.False);
            Assert.That(parsed.Diagnostics.Select(value => value.Code),
                Does.Contain("ASNECN0062"));
            Assert.That(parsed.ParserResults.Single().Diagnostics,
                Has.Length.EqualTo(1));
            Assert.That(parserDiagnostic.Span, Is.Not.Null);
            Assert.That(parserDiagnostic.ValuePath, Is.EqualTo("$syntax"));
            Assert.That(parserDiagnostic.PlanStepId,
                Is.EqualTo("ecn:syntax"));
            Assert.That(parserDiagnostic.HasBitOffset, Is.False);
            Assert.That(parsed.ReflectionCatalog, Is.Null);
            Assert.That(semantic.Success, Is.False);
            Assert.That(semantic.Diagnostics.Any(value =>
                value.Category == AsnDiagnosticCategory.Ecn), Is.True);
            Assert.That(semanticDiagnostic.Span, Is.Not.Null);
            Assert.That(semanticDiagnostic.ValuePath, Is.Not.Empty);
            Assert.That(semanticDiagnostic.PlanStepId, Is.Not.Empty);
            Assert.That(semanticDiagnostic.RelatedLocations, Is.Not.Empty);
            Assert.That(semanticDiagnostic.RelatedLocations.Any(value =>
                value.Message.Contains("declaration", StringComparison.Ordinal)),
                Is.True);
            Assert.That(semantic.ReflectionCatalog, Is.Null);
        });
    }

    [Test]
    public void EmitsEcnClassesAndObjectsWithGeneratedPlanMetadata()
    {
        AsnCompilationResult compiled = new EcnCompilerImplementation()
            .CompileEcn(
                Source(
                    "Generated ENCODING-DEFINITIONS ::= BEGIN\n"
                        + "#Byte ::= #INT\n"
                        + "scalar #Byte ::= { ENCODING-SPACE, SIZE 8 }\n"
                        + "END"),
                []);

        string expression = new DescriptorEmitter().CreatePlanExpression(
            compiled.EncodingPlans.Single());

        Assert.Multiple(() =>
        {
            Assert.That(expression,
                Does.Contain("EcnClasses = [new AsnEcnClass"));
            Assert.That(expression,
                Does.Contain("EcnObjects = [new AsnEcnObject"));
            Assert.That(expression,
                Does.Contain("AsnPlanStepKind.Length"));
            Assert.That(expression,
                Does.Contain("AsnInstructionKind.Ecn"));
        });
    }

    private static AsnModule SchemaModule()
    {
        return new AsnModule
        {
            Name = "Schema",
            Assignments =
            [
                new AsnAssignment
                {
                    Name = "scalar",
                    Kind = AsnAssignmentKind.Type,
                    Type = new AsnType
                    {
                        Name = "scalar",
                        Kind = AsnTypeKind.Integer
                    }
                }
            ]
        };
    }

    private static AsnSourceUnit Source(string text)
    {
        return new AsnSourceUnit
        {
            Name = "integration.edm",
            Text = text
        };
    }
}
