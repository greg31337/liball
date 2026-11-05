using asn1;

namespace asn1.Tests;

public class EcnPlanValidatorTests
{
    [Test]
    public void AcceptsCompleteDeterministicDecodablePlans()
    {
        EcnFixture fixture = Compile(
            "ValidPlan ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Packet ::= #CONCATENATION { length #INT, value #OCTETS }\n"
                + "packet #Packet ::= { SIZE 16 }\n"
                + "END");

        EcnExecutablePlan plan = Plan(fixture, "packet");

        Assert.Multiple(() =>
        {
            Assert.That(plan.Valid, Is.True);
            Assert.That(plan.Deterministic, Is.True);
            Assert.That(plan.Decodable, Is.True);
            Assert.That(plan.Complete, Is.True);
            Assert.That(plan.ValidationIssues, Is.Empty);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void RejectsIncompleteCoverageAndIllegalPlanKinds()
    {
        EcnFixture fixture = Compile(
            "InvalidShape ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Packet ::= #CONCATENATION { first #INT, second #INT }\n"
                + "packet #Packet ::= { }\n"
                + "END");
        EcnEncodingObjectDefinition definition = Definition(fixture, "packet");
        EcnExecutablePlan plan = Plan(fixture, "packet");
        EcnExecutablePlanStep second = plan.Steps.Single(step =>
            step.Trace.SemanticKind == "field" && step.Name == "second");
        second.Name = "missing";
        plan.Root!.Kind = EcnPlanStepKind.Primitive;
        DiagnosticBag diagnostics = Validate(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Valid, Is.False);
            Assert.That(plan.Valid, Is.False);
            Assert.That(plan.Complete, Is.False);
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0055"));
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0056"));
            Assert.That(plan.ValidationIssues.Select(issue => issue.Code),
                Does.Contain(EcnDiagnosticCodes.IncompleteFieldCoverage));
        });
    }

    [Test]
    public void RejectsUnsatisfiableConditionsAndMissingDeterminants()
    {
        EcnFixture fixture = Compile(
            "InvalidControls ENCODING-DEFINITIONS ::= BEGIN\n"
                + "conditional {< REFERENCE : enabled >} #CONDITIONAL-INT ::= { IF enabled }\n"
                + "sized {< REFERENCE : length >} #OCTETS ::= {\n"
                + "  ENCODING-SPACE DETERMINED BY field-to-be-used USING length }\n"
                + "END");
        EcnEncodingObjectDefinition conditional = Definition(
            fixture,
            "conditional");
        conditional.Constructor!.Conditions[0].Expression = "FALSE";
        EcnExecutablePlan sized = Plan(fixture, "sized");
        EcnExecutablePlanStep determinant = sized.Steps.Single(step =>
            step.Kind == EcnPlanStepKind.DeterminantRead);
        determinant.Dependencies[0].Path = "";
        DiagnosticBag diagnostics = Validate(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0057"));
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0058"));
            Assert.That(Plan(fixture, "conditional").Decodable, Is.False);
            Assert.That(sized.Decodable, Is.False);
            Assert.That(determinant.Valid, Is.False);
        });
    }

    [Test]
    public void RejectsRecursiveDependenciesAndInvalidBounds()
    {
        EcnFixture fixture = Compile(
            "InvalidMetadata ENCODING-DEFINITIONS ::= BEGIN\n"
                + "value #INT ::= { SIZE 8, ALIGNED TO NEXT bit }\n"
                + "END");
        EcnExecutablePlan plan = Plan(fixture, "value");
        EcnExecutablePlanStep alignment = plan.Steps.Single(step =>
            step.Kind == EcnPlanStepKind.Alignment);
        alignment.Dependencies[0].SourceStepId = alignment.StableId;
        alignment.Dependencies[0].MustPrecede = true;
        plan.Bounds.MinimumBits = 16;
        plan.Bounds.MaximumBits = 8;
        DiagnosticBag diagnostics = Validate(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0053"));
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0059"));
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0060"));
            Assert.That(plan.Deterministic, Is.False);
            Assert.That(plan.Valid, Is.False);
        });
    }

    [Test]
    public void RejectsNonDecodableOpenTypesAndAmbiguousAlternatives()
    {
        EcnFixture fixture = Compile(
            "InvalidSelection ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Choice ::= #ALTERNATIVES { first #INT, second #OCTETS }\n"
                + "choice #Choice ::= { }\n"
                + "open #OPEN-TYPE ::= { ENCODED WITH BER }\n"
                + "END");
        EcnExecutablePlan choice = Plan(fixture, "choice");
        EcnExecutablePlanStep[] alternatives = choice.Steps.Where(step =>
            step.Trace.SemanticKind == "field").ToArray();
        alternatives[1].Path = alternatives[0].Path;
        EcnExecutablePlan open = Plan(fixture, "open");
        EcnExecutablePlanStep openStep = open.Steps.Single(step =>
            step.Kind == EcnPlanStepKind.OpenType);
        openStep.Dependencies = [];
        DiagnosticBag diagnostics = Validate(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0054"));
            Assert.That(Codes(diagnostics), Does.Contain("ASNECN0061"));
            Assert.That(open.Decodable, Is.False);
            Assert.That(choice.Deterministic, Is.False);
            Assert.That(choice.Decodable, Is.False);
        });
    }

    private static DiagnosticBag Validate(EcnFixture fixture)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(
            false,
            new AsnResourceLimits());
        new EcnPlanValidator(diagnostics).ValidateTables([fixture.Table]);
        return diagnostics;
    }

    private static string[] Codes(DiagnosticBag diagnostics)
    {
        return diagnostics.GetDiagnostics().Select(item => item.Code).ToArray();
    }

    private static EcnExecutablePlan Plan(EcnFixture fixture, string name)
    {
        return Definition(fixture, name).Plan
            ?? throw new AssertionException(
                "The executable plan for '" + name + "' is missing.");
    }

    private static EcnEncodingObjectDefinition Definition(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObject)
                ?.ObjectDefinition
            ?? throw new AssertionException(
                "The encoding object '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "validation.edm",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                break;
            }
            if (result.Status == IncrementalLexStatus.NeedMoreData)
            {
                throw new AssertionException(
                    "Final ECN input requested more data.");
            }
            if (result.Token != null)
            {
                tokens.Add(result.Token);
            }
        }

        EcnParseResult parsed = new EcnSyntaxParser(tokens.ToArray())
            .ParseModuleSyntax();
        Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
        Assert.That(session.GetDiagnostics(), Is.Empty);
        EcnModuleSyntax module = parsed.Module
            ?? throw new AssertionException("No ECN module was parsed.");
        EcnSymbolTable table = new EcnSymbolTableBuilder(session.Diagnostics)
            .BuildTable(module, []);
        return new EcnFixture
        {
            Diagnostics = session.Diagnostics,
            Table = table
        };
    }

    private class EcnFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal EcnSymbolTable Table = null!;
    }
}
