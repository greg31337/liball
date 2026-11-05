using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerTests
{
    [Test]
    public void StringSanitizationReturnsCanonicalFragmentAndReport()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        builder.AllowElement("a");
        builder.AllowAttributeOnElement("href", "a");
        builder.AllowUrlScheme("https");

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(
            "<P onclick=x><A HREF='https://example.test'>safe</A>"
                + "<script>secret</script></P>",
            builder.Compile(), HtmlParseOptions.CreateDefault(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(result.GetNode().GetKind(),
                Is.EqualTo(HtmlNodeKind.Fragment));
            Assert.That(result.GetHtml(), Is.EqualTo(
                "<p><a href=\"https://example.test/\">safe</a></p>"));
            Assert.That(result.GetReport().GetReasonCount(
                HtmlSanitizerChangeReason.AttributeNotAllowed),
                Is.EqualTo(1));
            Assert.That(result.GetReport().GetReasonCount(
                HtmlSanitizerChangeReason.SensitiveElementDropped),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void DomSanitizationClonesByDefault()
    {
        HtmlNode original = ParseFragment(
            "<p title=keep onclick=remove>safe<script>secret</script></p>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        builder.AllowAttribute("title");
        HtmlDocument owner = original.GetOwnerDocument();
        long mutationVersion = owner.GetMutationVersionStorage();

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(original,
            builder.Compile(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetNode(), Is.Not.SameAs(original));
            Assert.That(result.GetHtml(),
                Is.EqualTo("<p title=\"keep\">safe</p>"));
            Assert.That(HtmlSerializer.SerializeChildren(original),
                Is.EqualTo("<p title=\"keep\" onclick=\"remove\">safe"
                    + "<script>secret</script></p>"));
            Assert.That(owner.GetMutationVersionStorage(),
                Is.EqualTo(mutationVersion));
        });
    }

    [Test]
    public void ExplicitInPlaceSanitizationMutatesAndReturnsInputNode()
    {
        HtmlNode original = ParseFragment(
            "<p onclick=remove>safe<script>secret</script></p>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");

        HtmlSanitizeResult result = HtmlSanitizer.SanitizeInPlace(original,
            builder.Compile(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetNode(), Is.SameAs(original));
            Assert.That(result.GetHtml(), Is.EqualTo("<p>safe</p>"));
            Assert.That(HtmlSerializer.SerializeChildren(original),
                Is.EqualTo("<p>safe</p>"));
        });
    }

    [Test]
    public void ParseLimitFailureExposesNoPartialDomOrHtml()
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(12);
        options.SetLimits(limits);

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(
            "<p>safe</p><p>must not escape</p>",
            HtmlSanitizerPolicy.CreateRichText().Compile(), options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetNode(), Is.Null);
            Assert.That(result.GetHtml(), Is.Empty);
            Assert.That(result.GetReport().WasLimitExceeded(), Is.True);
        });
    }

    [Test]
    public void DomSecurityLimitFailureIsFailClosedAndDoesNotMutateInput()
    {
        HtmlNode original = ParseFragment("<p onclick=x>safe</p>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumNodes(2);

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(original,
            builder.Compile(), limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetNode(), Is.Null);
            Assert.That(result.GetHtml(), Is.Empty);
            Assert.That(result.GetReport().WasLimitExceeded(), Is.True);
            Assert.That(HtmlSerializer.SerializeChildren(original),
                Is.EqualTo("<p onclick=\"x\">safe</p>"));
        });
    }

    [Test]
    public void OutputLimitUsesExactCanonicalLengthAndFailsClosed()
    {
        HtmlNode original = ParseFragment("<p>safe</p>");
        HtmlCompiledSanitizerPolicy policy =
            HtmlSanitizerPolicy.CreateRichText().Compile();
        HtmlLimits exactLimits = HtmlLimits.CreateDefault();
        exactLimits.SetMaximumOutputCharacters(11);
        HtmlLimits shortLimits = exactLimits.Copy();
        shortLimits.SetMaximumOutputCharacters(10);

        HtmlSanitizeResult exact = HtmlSanitizer.Sanitize(original, policy,
            exactLimits, CancellationToken.None);
        HtmlSanitizeResult shortResult = HtmlSanitizer.Sanitize(original,
            policy, shortLimits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exact.IsSuccess(), Is.True);
            Assert.That(exact.GetHtml(), Is.EqualTo("<p>safe</p>"));
            Assert.That(shortResult.IsSuccess(), Is.False);
            Assert.That(shortResult.GetNode(), Is.Null);
            Assert.That(shortResult.GetHtml(), Is.Empty);
            Assert.That(shortResult.GetReport().WasLimitExceeded(), Is.True);
            Assert.That(HtmlSerializer.SerializeChildren(original),
                Is.EqualTo("<p>safe</p>"));
        });
    }

    [Test]
    public void CancellationIsObservedBeforeMutation()
    {
        HtmlNode original = ParseFragment("<p onclick=x>safe</p>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(() => HtmlSanitizer.SanitizeInPlace(original,
            builder.Compile(), source.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(HtmlSerializer.SerializeChildren(original),
            Is.EqualTo("<p onclick=\"x\">safe</p>"));
    }

    [Test]
    public void HtmlFacadeUsesRichTextAndFailsClosed()
    {
        string result = Html.Sanitize(
            "<p onclick=x>safe<script>secret</script></p>");

        Assert.That(result, Is.EqualTo("<p>safe</p>"));
    }

    static HtmlNode ParseFragment(string html)
    {
        HtmlParseResult parsed = Html.ParseFragment(html, "body");
        Assert.That(parsed.IsSuccess(), Is.True);
        return parsed.GetFragment();
    }
}
