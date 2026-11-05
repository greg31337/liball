using asn1;

namespace asn1.Tests;

public class DiagnosticBagTests
{
    [TestCase(AsnDiagnosticCategory.General, "ASN0007")]
    [TestCase(AsnDiagnosticCategory.Source, "ASNSRC0007")]
    [TestCase(AsnDiagnosticCategory.Lexical, "ASNL0007")]
    [TestCase(AsnDiagnosticCategory.Syntax, "ASNP0007")]
    [TestCase(AsnDiagnosticCategory.Binding, "ASNB0007")]
    [TestCase(AsnDiagnosticCategory.Semantic, "ASNS0007")]
    [TestCase(AsnDiagnosticCategory.Constraint, "ASNC0007")]
    [TestCase(AsnDiagnosticCategory.Generation, "ASNGEN0007")]
    [TestCase(AsnDiagnosticCategory.Encoding, "ASNE0007")]
    [TestCase(AsnDiagnosticCategory.Decoding, "ASND0007")]
    [TestCase(AsnDiagnosticCategory.Canonicality, "ASNCAN0007")]
    [TestCase(AsnDiagnosticCategory.Resource, "ASNR0007")]
    [TestCase(AsnDiagnosticCategory.Ecn, "ASNECN0007")]
    [TestCase(AsnDiagnosticCategory.Xsd, "ASNXSD0007")]
    [TestCase(AsnDiagnosticCategory.InputOutput, "ASNIO0007")]
    public void CategoryCodesUseStablePhasePrefixes(AsnDiagnosticCategory category, string expected)
    {
        Assert.That(DiagnosticBag.CreateCode(category, 7), Is.EqualTo(expected));
    }

    [Test]
    public void WarningEscalationIsAppliedWhenDiagnosticsEnterTheBag()
    {
        DiagnosticBag normal = new DiagnosticBag(false, new AsnResourceLimits());
        DiagnosticBag escalating = new DiagnosticBag(true, new AsnResourceLimits());

        normal.TryAddDiagnostic(AsnDiagnosticSeverity.Warning, AsnDiagnosticCategory.Semantic, 1, "warning", null, null);
        escalating.TryAddDiagnostic(AsnDiagnosticSeverity.Warning, AsnDiagnosticCategory.Semantic, 1, "warning", null, null);

        Assert.That(normal.GetDiagnostics()[0].Severity, Is.EqualTo(AsnDiagnosticSeverity.Warning));
        Assert.That(normal.ContainsErrors(), Is.False);
        Assert.That(escalating.GetDiagnostics()[0].Severity, Is.EqualTo(AsnDiagnosticSeverity.Error));
        Assert.That(escalating.ContainsErrors(), Is.True);
    }

    [Test]
    public void DiagnosticSnapshotsUseDeterministicSourceAndCodeOrder()
    {
        DiagnosticBag bag = new DiagnosticBag(false, new AsnResourceLimits());

        bag.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.Syntax, 2, "b", CreateSpan("b.asn", 0, 1), null);
        bag.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.Semantic, 1, "semantic", CreateSpan("a.asn", 10, 11), null);
        bag.TryAddDiagnostic(AsnDiagnosticSeverity.Warning, AsnDiagnosticCategory.Lexical, 2, "second", CreateSpan("a.asn", 2, 3), null);
        bag.TryAddDiagnostic(AsnDiagnosticSeverity.Warning, AsnDiagnosticCategory.Lexical, 1, "first", CreateSpan("a.asn", 2, 3), null);
        bag.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.Resource, 1, "global", null, null);

        AsnDiagnostic[] diagnostics = bag.GetDiagnostics();

        Assert.That(diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[]
        {
            "ASNR0001",
            "ASNL0001",
            "ASNL0002",
            "ASNS0001",
            "ASNP0002"
        }));
    }

    [Test]
    public void DuplicateDiagnosticsMergeUniqueRelatedLocationsAndSnapshotInputs()
    {
        DiagnosticBag bag = new DiagnosticBag(false, new AsnResourceLimits());
        AsnSourceSpan primary = CreateSpan("main.asn", 10, 12);
        AsnDiagnosticRelatedLocation later = CreateRelated("later", "z.asn", 20, 21);
        AsnDiagnosticRelatedLocation earlier = CreateRelated("earlier", "a.asn", 5, 6);

        Assert.That(bag.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            4,
            "duplicate",
            primary,
            [later]), Is.True);
        Assert.That(bag.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            4,
            "duplicate",
            primary,
            [later, earlier]), Is.True);

        primary.Start.SourceName = "changed.asn";
        later.Message = "changed";

        AsnDiagnostic[] firstSnapshot = bag.GetDiagnostics();

        Assert.That(bag.GetCount(), Is.EqualTo(1));
        Assert.That(firstSnapshot[0].Span?.Start.SourceName, Is.EqualTo("main.asn"));
        Assert.That(firstSnapshot[0].RelatedLocations.Select(location => location.Message), Is.EqualTo(new string[] { "earlier", "later" }));

        firstSnapshot[0].Message = "mutated snapshot";
        firstSnapshot[0].RelatedLocations[0].Message = "mutated relation";

        AsnDiagnostic[] secondSnapshot = bag.GetDiagnostics();
        Assert.That(secondSnapshot[0].Message, Is.EqualTo("duplicate"));
        Assert.That(secondSnapshot[0].RelatedLocations[0].Message, Is.EqualTo("earlier"));
    }

    [Test]
    public void RecoveryLimitCountsUniqueDiagnosticsAndRejectsFurtherRecovery()
    {
        AsnResourceLimits limits = new AsnResourceLimits { MaximumDiagnostics = 2 };
        DiagnosticBag bag = new DiagnosticBag(false, limits);
        AsnSourceSpan span = CreateSpan("input.asn", 0, 1);

        Assert.That(bag.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.Syntax, 1, "one", span, null), Is.True);
        Assert.That(bag.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.Syntax, 1, "one", span, null), Is.True);
        Assert.That(bag.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.Syntax, 2, "two", span, null), Is.True);
        Assert.That(bag.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.Syntax, 3, "three", span, null), Is.False);

        Assert.That(bag.GetCount(), Is.EqualTo(2));
        Assert.That(bag.CanContinueRecovery(), Is.False);
    }

    [Test]
    public void UnlimitedDiagnosticsRequireTheExplicitUnlimitedFlag()
    {
        AsnResourceLimits boundedLimits = new AsnResourceLimits { MaximumDiagnostics = 0 };
        AsnResourceLimits unlimitedLimits = new AsnResourceLimits { MaximumDiagnostics = 0, Unlimited = true };
        DiagnosticBag bounded = new DiagnosticBag(false, boundedLimits);
        DiagnosticBag unlimited = new DiagnosticBag(false, unlimitedLimits);

        Assert.That(bounded.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.General, 1, "blocked", null, null), Is.False);
        Assert.That(bounded.CanContinueRecovery(), Is.False);

        Assert.That(unlimited.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.General, 1, "one", null, null), Is.True);
        Assert.That(unlimited.TryAddDiagnostic(AsnDiagnosticSeverity.Error, AsnDiagnosticCategory.General, 2, "two", null, null), Is.True);
        Assert.That(unlimited.GetCount(), Is.EqualTo(2));
        Assert.That(unlimited.CanContinueRecovery(), Is.True);
    }

    private static AsnSourceSpan CreateSpan(string sourceName, int startOffset, int endOffset)
    {
        return new AsnSourceSpan
        {
            Start = new AsnSourcePosition
            {
                SourceName = sourceName,
                Offset = startOffset,
                Line = 1,
                Column = startOffset + 1
            },
            End = new AsnSourcePosition
            {
                SourceName = sourceName,
                Offset = endOffset,
                Line = 1,
                Column = endOffset + 1
            }
        };
    }

    private static AsnDiagnosticRelatedLocation CreateRelated(string message, string sourceName, int startOffset, int endOffset)
    {
        return new AsnDiagnosticRelatedLocation
        {
            Message = message,
            Span = CreateSpan(sourceName, startOffset, endOffset)
        };
    }
}
