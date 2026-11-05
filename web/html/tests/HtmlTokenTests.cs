
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenTests
{
    [Test]
    public void TokenFactoriesExposeEveryTokenKindAndSourceSpan()
    {
        HtmlSourceSpan span = CreateSpan(1, 4);
        HtmlAttribute[] noAttributes = Array.Empty<HtmlAttribute>();

        HtmlToken[] tokens =
        [
            HtmlToken.CreateDoctype("html", false, span),
            HtmlToken.CreateStartTag("p", noAttributes, false, span),
            HtmlToken.CreateEndTag("p", noAttributes, false, span),
            HtmlToken.CreateComment("note", span),
            HtmlToken.CreateCharacter("text", span),
            HtmlToken.CreateEndOfFile(span)
        ];

        Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(new[]
        {
            HtmlTokenKind.Doctype,
            HtmlTokenKind.StartTag,
            HtmlTokenKind.EndTag,
            HtmlTokenKind.Comment,
            HtmlTokenKind.Character,
            HtmlTokenKind.EndOfFile
        }));
        Assert.That(tokens.All(token => ReferenceEquals(token.GetSourceSpan(), span)), Is.True);
        Assert.That(tokens[3].GetData(), Is.EqualTo("note"));
        Assert.That(tokens[4].GetData(), Is.EqualTo("text"));
        Assert.That(tokens[1].GetName(), Is.EqualTo("p"));
    }

    [Test]
    public void TagAttributesPreserveOrderMetadataAndSnapshotIsolation()
    {
        HtmlSourceSpan tokenSpan = CreateSpan(0, 20);
        HtmlSourceSpan firstSpan = CreateSpan(3, 9);
        HtmlSourceSpan secondSpan = CreateSpan(10, 19);
        HtmlAttribute first = HtmlAttribute.Create("id", "main", firstSpan);
        HtmlAttribute second = HtmlAttribute.CreateNamespaced(
            "xlink:href", "href", "xlink", "http://www.w3.org/1999/xlink", "target", secondSpan);
        HtmlAttribute[] supplied = [first, second];

        HtmlToken token = HtmlToken.CreateStartTag("svg", supplied, false, tokenSpan);
        supplied[0] = second;
        HtmlAttribute[] firstSnapshot = token.GetAttributes();
        firstSnapshot[1] = first;
        HtmlAttribute[] secondSnapshot = token.GetAttributes();

        Assert.Multiple(() =>
        {
            Assert.That(secondSnapshot, Is.EqualTo(new[] { first, second }));
            Assert.That(second.GetName(), Is.EqualTo("xlink:href"));
            Assert.That(second.GetLocalName(), Is.EqualTo("href"));
            Assert.That(second.GetPrefix(), Is.EqualTo("xlink"));
            Assert.That(second.GetNamespaceUri(), Is.EqualTo("http://www.w3.org/1999/xlink"));
            Assert.That(second.GetValue(), Is.EqualTo("target"));
            Assert.That(second.GetSourceSpan(), Is.SameAs(secondSpan));
        });
    }

    [Test]
    public void DoctypeDistinguishesMissingIdentifiersFromEmptyIdentifiers()
    {
        HtmlSourceSpan span = CreateSpan(0, 10);
        HtmlToken missing = HtmlToken.CreateDoctype(
            false, "", false, "", false, "", true, span);
        HtmlToken empty = HtmlToken.CreateDoctype(
            true, "", true, "", true, "", false, span);

        Assert.Multiple(() =>
        {
            Assert.That(missing.HasName(), Is.False);
            Assert.That(missing.HasPublicIdentifier(), Is.False);
            Assert.That(missing.HasSystemIdentifier(), Is.False);
            Assert.That(missing.IsForceQuirks(), Is.True);
            Assert.That(empty.HasName(), Is.True);
            Assert.That(empty.HasPublicIdentifier(), Is.True);
            Assert.That(empty.HasSystemIdentifier(), Is.True);
            Assert.That(empty.GetPublicIdentifier(), Is.Empty);
            Assert.That(empty.GetSystemIdentifier(), Is.Empty);
            Assert.That(empty.IsForceQuirks(), Is.False);
        });
    }

    [Test]
    public void SelfClosingStartTagAcknowledgementIsExplicitAndIdempotent()
    {
        HtmlSourceSpan span = CreateSpan(0, 5);
        HtmlToken start = HtmlToken.CreateStartTag(
            "br", Array.Empty<HtmlAttribute>(), true, span);
        HtmlToken normal = HtmlToken.CreateStartTag(
            "div", Array.Empty<HtmlAttribute>(), false, span);
        HtmlToken end = HtmlToken.CreateEndTag(
            "br", Array.Empty<HtmlAttribute>(), true, span);

        Assert.That(start.RequiresSelfClosingAcknowledgement(), Is.True);
        start.AcknowledgeSelfClosing();
        start.AcknowledgeSelfClosing();
        Assert.That(start.IsSelfClosingAcknowledged(), Is.True);
        Assert.That(start.RequiresSelfClosingAcknowledgement(), Is.False);
        Assert.Throws<InvalidOperationException>(normal.AcknowledgeSelfClosing);
        Assert.Throws<InvalidOperationException>(end.AcknowledgeSelfClosing);
    }

    [Test]
    public void ReadResultsKeepTokenStatusAndDiagnosticSnapshotsConsistent()
    {
        HtmlSourceSpan span = CreateSpan(0, 1);
        HtmlToken token = HtmlToken.CreateCharacter("x", span);
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.TokenizerUnexpectedCharacter,
            HtmlDiagnosticSeverity.Warning,
            "unexpected",
            span);
        HtmlDiagnostic[] supplied = [diagnostic];

        HtmlTokenReadResult available = HtmlTokenReadResult.CreateToken(token, supplied);
        supplied[0] = null;
        HtmlDiagnostic[] snapshot = available.GetDiagnostics();
        snapshot[0] = null;
        HtmlTokenReadResult pending = HtmlTokenReadResult.CreateStatus(
            HtmlTokenReadStatus.NeedMoreInput);

        Assert.Multiple(() =>
        {
            Assert.That(available.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.TokenAvailable));
            Assert.That(available.HasToken(), Is.True);
            Assert.That(available.GetToken(), Is.SameAs(token));
            Assert.That(available.GetDiagnostics(), Is.EqualTo(new[] { diagnostic }));
            Assert.That(available.IsPartial(), Is.False);
            Assert.That(available.WereDiagnosticsTruncated(), Is.False);
            Assert.That(available.GetDroppedDiagnosticCount(), Is.Zero);
            Assert.That(pending.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput));
            Assert.That(pending.HasToken(), Is.False);
            Assert.That(pending.GetToken(), Is.Null);
        });
    }

    [Test]
    public void ReadResultsValidatePartialAndDiagnosticTruncationMetadata()
    {
        HtmlSourceSpan span = CreateSpan(0, 1);
        HtmlToken token = HtmlToken.CreateCharacter("x", span);
        HtmlTokenReadResult partial = HtmlTokenReadResult.CreateToken(token,
            Array.Empty<HtmlDiagnostic>(), true, true, 2);
        HtmlTokenReadResult limited = HtmlTokenReadResult.CreateStatus(
            HtmlTokenReadStatus.LimitExceeded, Array.Empty<HtmlDiagnostic>(),
            true, true, 2);

        Assert.Multiple(() =>
        {
            Assert.That(partial.IsPartial(), Is.True);
            Assert.That(partial.WereDiagnosticsTruncated(), Is.True);
            Assert.That(partial.GetDroppedDiagnosticCount(), Is.EqualTo(2));
            Assert.That(limited.IsPartial(), Is.True);
            Assert.That(limited.WereDiagnosticsTruncated(), Is.True);
            Assert.That(limited.GetDroppedDiagnosticCount(), Is.EqualTo(2));
        });

        Assert.Throws<ArgumentException>(() => HtmlTokenReadResult.CreateStatus(
            HtmlTokenReadStatus.Complete, Array.Empty<HtmlDiagnostic>(),
            true, false, 0));
        Assert.Throws<ArgumentException>(() => HtmlTokenReadResult.CreateToken(token,
            Array.Empty<HtmlDiagnostic>(), false, false, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlTokenReadResult.CreateToken(token,
            Array.Empty<HtmlDiagnostic>(), false, false, -1));
    }

    [Test]
    public void TokenContractsRejectInconsistentConstruction()
    {
        HtmlSourceSpan span = CreateSpan(0, 1);

        Assert.Throws<ArgumentException>(() => HtmlAttribute.Create("", "value", span));
        Assert.Throws<ArgumentException>(() => HtmlToken.CreateStartTag(
            "", Array.Empty<HtmlAttribute>(), false, span));
        Assert.Throws<ArgumentException>(() => HtmlToken.CreateStartTag(
            "p", new HtmlAttribute[] { null }, false, span));
        Assert.Throws<ArgumentException>(() => HtmlToken.CreateDoctype(
            false, "html", false, "", false, "", false, span));
        Assert.Throws<ArgumentException>(() => HtmlTokenReadResult.CreateStatus(
            HtmlTokenReadStatus.TokenAvailable));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlTokenReadResult.CreateStatus(
            (HtmlTokenReadStatus)99));
    }

    static HtmlSourceSpan CreateSpan(int start, int end)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(start, 1, start + 1),
            HtmlSourcePosition.Create(end, 1, end + 1));
    }
}
