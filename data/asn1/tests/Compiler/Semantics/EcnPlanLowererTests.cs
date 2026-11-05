using asn1;

namespace asn1.Tests;

public class EcnPlanLowererTests
{
    [Test]
    public void LowersPrimitivePropertiesIntoBoundedPositionAwareSteps()
    {
        EcnFixture fixture = Compile(
            "PrimitivePlan ENCODING-DEFINITIONS ::= BEGIN\n"
                + "integer #INT ::= { ENCODING-SPACE, SIZE 16, "
                + "RANGE -128..127, ALIGNED TO NEXT octet, "
                + "BYTE-ORDER big-endian }\n"
                + "END");

        EcnExecutablePlan plan = Plan(fixture, "integer");
        EcnExecutablePlanStep length = plan.Steps.Single(step =>
            step.Kind == EcnPlanStepKind.Length
                && step.Name == "SIZE");
        EcnExecutablePlanStep alignment = plan.Steps.Single(step =>
            step.Kind == EcnPlanStepKind.Alignment);
        EcnExecutablePlanStep range = plan.Steps.Single(step =>
            step.Name == "RANGE");

        Assert.Multiple(() =>
        {
            Assert.That(plan.StableId,
                Is.EqualTo("ecn:plan:PrimitivePlan:integer"));
            Assert.That(plan.Root?.Kind, Is.EqualTo(EcnPlanStepKind.Primitive));
            Assert.That(plan.Root?.Children, Has.Length.EqualTo(5));
            Assert.That(plan.Steps.Select(step => step.StableId).Distinct()
                .ToArray(), Has.Length.EqualTo(plan.Steps.Length));
            Assert.That(length.Bounds.Exact, Is.True);
            Assert.That(length.Bounds.MinimumBits, Is.EqualTo(16));
            Assert.That(length.Bounds.MaximumBytes, Is.EqualTo(2));
            Assert.That(plan.Bounds.Exact, Is.True);
            Assert.That(plan.Bounds.MaximumBits, Is.EqualTo(16));
            Assert.That(alignment.Dependencies.Select(item => item.Kind),
                Does.Contain(EcnPlanDependencyKind.BitPosition));
            Assert.That(alignment.Dependencies.Select(item => item.Kind),
                Does.Contain(EcnPlanDependencyKind.BytePosition));
            Assert.That(range.Bounds.LowerValue, Is.EqualTo("-128"));
            Assert.That(range.Bounds.UpperValue, Is.EqualTo("127"));
            Assert.That(plan.Buffer.MinimumBytes, Is.EqualTo(2));
            Assert.That(plan.Buffer.RequiresWholeValue, Is.True);
            Assert.That(plan.Buffer.RequiresRandomAccess, Is.True);
            Assert.That(plan.Buffer.Streamable, Is.False);
            Assert.That(plan.Steps.All(step => step.Trace.Span != null),
                Is.True);
            Assert.That(plan.Steps.Skip(2).All(step => step.Dependencies.Any(
                dependency => dependency.Kind
                    == EcnPlanDependencyKind.Ordering)), Is.True);
        });
    }

    [Test]
    public void LowersStructuresDeterminantsAndStandardEncodingDependencies()
    {
        EcnFixture fixture = Compile(
            "StructuredPlan ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Packet ::= #CONCATENATION { length #INT, value #OCTETS }\n"
                + "sized {< REFERENCE : determinant >} #OCTETS ::= {\n"
                + "  ENCODING-SPACE DETERMINED BY field-to-be-set "
                + "USING determinant }\n"
                + "packet #Packet ::= { ENCODE STRUCTURE {\n"
                + "  value USE sized {< length >} } }\n"
                + "standard #INT ::= { USE-SET DER }\n"
                + "END");

        EcnExecutablePlan packet = Plan(fixture, "packet");
        EcnExecutablePlanStep determinant = packet.Steps.Single(step =>
            step.Kind == EcnPlanStepKind.DeterminantWrite);
        EcnExecutablePlan standard = Plan(fixture, "standard");
        EcnExecutablePlanStep standardStep = standard.Steps.Single(step =>
            step.Kind == EcnPlanStepKind.StandardEncoding);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Root?.Kind, Is.EqualTo(EcnPlanStepKind.Sequence));
            Assert.That(packet.Steps.Count(step =>
                step.Kind == EcnPlanStepKind.Field), Is.EqualTo(3));
            Assert.That(determinant.Path, Is.EqualTo("value"));
            Assert.That(determinant.Dependencies.Single(dependency =>
                    dependency.Kind == EcnPlanDependencyKind.DeterminantValue)
                .Path, Is.EqualTo("length"));
            Assert.That(determinant.Buffer.RequiresBackpatch, Is.True);
            Assert.That(packet.Buffer.RequiresBackpatch, Is.True);
            Assert.That(packet.Buffer.Streamable, Is.False);
            Assert.That(standardStep.Dependencies.Single(dependency =>
                    dependency.Kind == EcnPlanDependencyKind.EncodingObjectSet)
                .StableTargetId, Is.EqualTo("x692:set:DER"));
            Assert.That(standardStep.Path, Is.EqualTo("$value"));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void LowersConversionsWithReversibilityAndBufferMetadata()
    {
        EcnFixture fixture = Compile(
            "ConversionPlan ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Wrapped {< #Element >} ::= #CONCATENATION { value #Element }\n"
                + "wrappedEncoding {< #Element >} #Wrapped {< #Element >} ::= { }\n"
                + "contained #OCTETS ::= { REPETITION-ENCODING { "
                + "REPLACE STRUCTURE WITH #Wrapped ENCODED BY wrappedEncoding } }\n"
                + "mapped #INT ::= { USE #INT MAPPING TRANSFORMS "
                + "{{ INT-TO-INT add:1 }, { INT-TO-INT multiply:2 }} }\n"
                + "open #OPEN-TYPE ::= { ENCODED WITH BER "
                + "UNKNOWN IS #OCTETS ENCODED WITH DER }\n"
                + "END");

        EcnExecutablePlan contained = Plan(fixture, "contained");
        EcnExecutablePlan mapped = Plan(fixture, "mapped");
        EcnExecutablePlanStep[] transforms = mapped.Steps.Where(step =>
            step.Kind == EcnPlanStepKind.Transformation).ToArray();
        EcnExecutablePlan open = Plan(fixture, "open");
        EcnExecutablePlanStep openStep = open.Steps.Single(step =>
            step.Kind == EcnPlanStepKind.OpenType);

        Assert.Multiple(() =>
        {
            Assert.That(contained.Steps.Any(step =>
                step.Kind == EcnPlanStepKind.Replacement), Is.True);
            Assert.That(contained.Buffer.RequiresWholeValue, Is.True);
            Assert.That(contained.Buffer.Streamable, Is.False);
            Assert.That(contained.Reversible, Is.True);
            Assert.That(transforms, Has.Length.EqualTo(2));
            Assert.That(transforms[1].Dependencies.Any(dependency =>
                dependency.Kind == EcnPlanDependencyKind.TransformResult
                    && dependency.SourceStepId == transforms[0].StableId),
                Is.True);
            Assert.That(transforms[1].RequiresRuntimeValidation, Is.True);
            Assert.That(mapped.Reversible, Is.True);
            Assert.That(openStep.Dependencies.Select(item => item.StableTargetId),
                Is.EqualTo(new[] { "x692:set:BER", "x692:set:DER" }));
            Assert.That(open.Buffer.RequiresLookahead, Is.True);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void GivesInheritedObjectsIndependentPlansAndSkipsInvalidObjects()
    {
        EcnFixture fixture = Compile(
            "PlanLifecycle ENCODING-DEFINITIONS ::= BEGIN\n"
                + "base #INT ::= { SIZE 8 }\n"
                + "copy #INT ::= base\n"
                + "invalid #BOOL ::= { ALPHABET \"AB\" }\n"
                + "END");

        EcnExecutablePlan basePlan = Plan(fixture, "base");
        EcnExecutablePlan copyPlan = Plan(fixture, "copy");
        EcnEncodingObjectDefinition invalid = ObjectDefinition(
            fixture,
            "invalid");

        Assert.Multiple(() =>
        {
            Assert.That(copyPlan, Is.Not.SameAs(basePlan));
            Assert.That(copyPlan.StableId,
                Is.EqualTo("ecn:plan:PlanLifecycle:copy"));
            Assert.That(copyPlan.Steps, Is.Not.SameAs(basePlan.Steps));
            Assert.That(copyPlan.Bounds.MaximumBits, Is.EqualTo(8));
            Assert.That(invalid.Valid, Is.False);
            Assert.That(invalid.Plan, Is.Null);
            Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item =>
                item.Code), Does.Contain("ASNECN0027"));
        });
    }

    private static EcnExecutablePlan Plan(EcnFixture fixture, string name)
    {
        return ObjectDefinition(fixture, name).Plan
            ?? throw new AssertionException(
                "The executable plan for '" + name + "' is missing.");
    }

    private static EcnEncodingObjectDefinition ObjectDefinition(
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
            "plans.edm",
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
