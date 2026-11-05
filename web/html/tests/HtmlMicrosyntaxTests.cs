
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMicrosyntaxTests
{
    [Test]
    public void ParsesAndSerializesSpaceSeparatedTokens()
    {
        string[] tokens = HtmlTokenLists.ParseSpaceSeparated(
            " alpha\tbeta\nalpha\u00a0gamma ");

        Assert.Multiple(() =>
        {
            Assert.That(tokens, Is.EqualTo(new[]
            {
                "alpha", "beta", "alpha\u00a0gamma"
            }));
            Assert.That(HtmlTokenLists.Contains(tokens, "alpha"), Is.True);
            Assert.That(HtmlTokenLists.Contains(tokens, "Alpha"), Is.False);
            Assert.That(HtmlTokenLists.ContainsAsciiInsensitive(tokens,
                "ALPHA"), Is.True);
            Assert.That(HtmlTokenLists.SerializeSpaceSeparated(tokens),
                Is.EqualTo("alpha beta alpha\u00a0gamma"));
        });
    }

    [Test]
    public void ParsesCommaSeparatedTokensIncludingEmptyEntries()
    {
        string[] tokens = HtmlTokenLists.ParseCommaSeparated(
            " a ,b,, d d , ");

        Assert.Multiple(() =>
        {
            Assert.That(tokens, Is.EqualTo(new[]
            {
                "a", "b", "", "d d", ""
            }));
            Assert.That(HtmlTokenLists.ParseCommaSeparated(""),
                Is.EqualTo(new[] { "" }));
            Assert.That(HtmlTokenLists.SerializeCommaSeparated(tokens),
                Is.EqualTo("a, b, , d d, "));
            Assert.Throws<ArgumentException>(() =>
                HtmlTokenLists.SerializeCommaSeparated(new[] { " spaced " }));
            Assert.Throws<ArgumentException>(() =>
                HtmlTokenLists.SerializeSpaceSeparated(new[] { "two words" }));
        });
    }

    [Test]
    public void NodeTokenConveniencesUseFreshAttributeSnapshots()
    {
        HtmlNode link = Html.Parse(
            "<a id=link rel='tag  next tag' accept='text/html, image/png'>")
            .FindById("link");
        string[] original = link.GetSpaceSeparatedTokens("rel");

        link.SetAttribute("rel", "changed");

        Assert.Multiple(() =>
        {
            Assert.That(original, Is.EqualTo(new[]
            {
                "tag", "next", "tag"
            }));
            Assert.That(link.GetSpaceSeparatedTokens("rel"),
                Is.EqualTo(new[] { "changed" }));
            Assert.That(link.GetCommaSeparatedTokens("accept"),
                Is.EqualTo(new[] { "text/html", "image/png" }));
            Assert.That(link.GetSpaceSeparatedTokens("missing"), Is.Empty);
        });
    }

    [Test]
    public void ParsesAndSerializesWidthDensityAndDefaultSrcsetCandidates()
    {
        HtmlSrcsetResult result = HtmlSrcset.Parse(
            "small.png 400w, data:image/png;base64,AAAA 2x, plain.png");
        HtmlSrcsetCandidate[] candidates = result.GetCandidates();
        candidates[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.True);
            Assert.That(result.GetErrorCount(), Is.Zero);
            Assert.That(result.GetCandidates().Select(value => value.GetUrl()),
                Is.EqualTo(new[]
                {
                    "small.png", "data:image/png;base64,AAAA", "plain.png"
                }));
            Assert.That(result.GetCandidates()[0].HasWidth(), Is.True);
            Assert.That(result.GetCandidates()[0].GetWidth(), Is.EqualTo(400));
            Assert.That(result.GetCandidates()[1].HasDensity(), Is.True);
            Assert.That(result.GetCandidates()[1].GetDensity(), Is.EqualTo(2));
            Assert.That(HtmlSrcset.Serialize(result.GetCandidates()),
                Is.EqualTo(
                    "small.png 400w, data:image/png;base64,AAAA 2x, plain.png"));
        });
    }

    [Test]
    public void RejectsMalformedSrcsetCandidatesAndKeepsRecoverableOnes()
    {
        HtmlSrcsetResult result = HtmlSrcset.Parse(
            "good.png 1x, zero.png 0w, bad.png 1q, "
            + "duplicate.png 1x 2x, tail.png,,");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.False);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.GetErrorCount(), Is.EqualTo(4));
            Assert.That(result.GetCandidates().Select(value => value.GetUrl()),
                Is.EqualTo(new[] { "good.png", "tail.png" }));
            Assert.That(HtmlSrcset.Parse(",, okay.png").GetErrorCount(),
                Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() =>
                HtmlSrcsetCandidate.Create("two words"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HtmlSrcsetCandidate.CreateDensity("image.png", 0));
        });
    }

    [Test]
    public void SrcsetHonorsLimitsCancellationAndArgumentValidation()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumAttributeValueCharacters(5);
        HtmlLimits tokenLimits = HtmlLimits.CreateDefault();
        tokenLimits.SetMaximumTokens(1);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        HtmlSrcsetResult limited = HtmlSrcset.Parse("long.png", limits,
            CancellationToken.None);
        HtmlSrcsetResult candidateLimited = HtmlSrcset.Parse("a.png, b.png",
            tokenLimits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(limited.IsComplete(), Is.False);
            Assert.That(limited.WasLimitExceeded(), Is.True);
            Assert.That(limited.GetCandidates(), Is.Empty);
            Assert.That(candidateLimited.WasLimitExceeded(), Is.True);
            Assert.That(candidateLimited.GetCandidates().Select(value =>
                value.GetUrl()), Is.EqualTo(new[] { "a.png" }));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlSrcset.Parse("a.png", HtmlLimits.CreateDefault(),
                    source.Token));
            Assert.Throws<ArgumentNullException>(() => HtmlSrcset.Parse(null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSrcset.Parse("a.png", null, CancellationToken.None));
        });
    }

    [TestCase("5", 5, false, "")]
    [TestCase(" 5.9 ; URL = 'next.html' ignored", 5, true, "next.html")]
    [TestCase(".5, target.html", 0, true, "target.html")]
    [TestCase("0;\"quoted path\" trailing", 0, true, "quoted path")]
    public void ExtractsDeclarativeRefreshDelayAndUrl(string value,
        long delay, bool hasUrl, string url)
    {
        HtmlRefreshResult result = HtmlRefresh.Parse(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDelaySeconds(), Is.EqualTo(delay));
            Assert.That(result.HasUrl(), Is.EqualTo(hasUrl));
            Assert.That(result.GetUrl(), Is.EqualTo(url));
        });
    }

    [Test]
    public void RefreshRejectsMalformedValuesAndHonorsLimits()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumAttributeValueCharacters(4);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlRefresh.Parse("").IsSuccess(), Is.False);
            Assert.That(HtmlRefresh.Parse("soon;url=next").IsSuccess(),
                Is.False);
            Assert.That(HtmlRefresh.Parse("5x;url=next").IsSuccess(),
                Is.False);
            Assert.That(HtmlRefresh.Parse("12345", limits,
                CancellationToken.None).WasLimitExceeded(), Is.True);
            Assert.Throws<OperationCanceledException>(() =>
                HtmlRefresh.Parse("1", HtmlLimits.CreateDefault(),
                    source.Token));
            Assert.Throws<ArgumentNullException>(() => HtmlRefresh.Parse(null));
        });
    }

    [Test]
    public void ParsesIntegerAndDimensionRecoveryRules()
    {
        HtmlIntegerResult integer = HtmlNumbers.ParseInteger(" \t+12junk");
        HtmlDimensionResult percentage = HtmlNumbers.ParseDimension(" 10.25%");
        HtmlDimensionResult recoveredLength =
            HtmlNumbers.ParseDimension("20px");

        Assert.Multiple(() =>
        {
            Assert.That(integer.IsSuccess(), Is.True);
            Assert.That(integer.GetValue(), Is.EqualTo(12));
            Assert.That(HtmlNumbers.ParseNonNegativeInteger("-1").IsSuccess(),
                Is.False);
            Assert.That(percentage.IsSuccess(), Is.True);
            Assert.That(percentage.GetValue(), Is.EqualTo(10.25));
            Assert.That(percentage.IsPercentage(), Is.True);
            Assert.That(recoveredLength.GetValue(), Is.EqualTo(20));
            Assert.That(recoveredLength.GetUnit(),
                Is.EqualTo(HtmlDimensionUnit.Length));
            Assert.That(HtmlNumbers.ParseDimension("-2").IsSuccess(), Is.False);
            Assert.That(HtmlNumbers.ParseDimension("   ").IsSuccess(), Is.False);
        });
    }

    [Test]
    public void AppliesTableSpanDefaultsAndClamps()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlNumbers.ParseColumnSpan("2"), Is.EqualTo(2));
            Assert.That(HtmlNumbers.ParseColumnSpan("0"), Is.EqualTo(1));
            Assert.That(HtmlNumbers.ParseColumnSpan("2000"), Is.EqualTo(1000));
            Assert.That(HtmlNumbers.ParseColumnSpan("999999999999"),
                Is.EqualTo(1000));
            Assert.That(HtmlNumbers.ParseColumnSpan("-999999999999"),
                Is.EqualTo(1));
            Assert.That(HtmlNumbers.ParseColumnSpan("bad"), Is.EqualTo(1));
            Assert.That(HtmlNumbers.ParseRowSpan("0"), Is.Zero);
            Assert.That(HtmlNumbers.ParseRowSpan("70000"), Is.EqualTo(65534));
            Assert.That(HtmlNumbers.ParseRowSpan("-999999999999"),
                Is.EqualTo(1));
            Assert.That(HtmlNumbers.ParseRowSpan("3rows"), Is.EqualTo(3));
        });
    }

    [Test]
    public void ComputesOrderedListNumberingFromLiveDomState()
    {
        HtmlDocument document = Html.Parse(
            "<ol id=list reversed type=A><li id=one><ol><li>nested</ol>"
            + "<li id=two value=7><li id=three></ol>");
        HtmlNode list = document.FindById("list");

        Assert.Multiple(() =>
        {
            Assert.That(HtmlNumbers.GetOrderedListStart(list), Is.EqualTo(3));
            Assert.That(HtmlNumbers.GetOrderedListOrdinals(list),
                Is.EqualTo(new[] { 3, 7, 6 }));
            Assert.That(HtmlNumbers.GetOrderedListMarkerStyle(list),
                Is.EqualTo(HtmlOrderedListMarkerStyle.UpperAlpha));
        });

        list.SetAttribute("start", " -2 trailing");
        document.FindById("two").SetAttribute("value", "invalid");
        list.SetAttribute("type", "unknown");

        Assert.Multiple(() =>
        {
            Assert.That(HtmlNumbers.GetOrderedListStart(list), Is.EqualTo(-2));
            Assert.That(HtmlNumbers.GetOrderedListOrdinals(list),
                Is.EqualTo(new[] { -2, -3, -4 }));
            Assert.That(HtmlNumbers.GetOrderedListMarkerStyle(list),
                Is.EqualTo(HtmlOrderedListMarkerStyle.Decimal));
            Assert.Throws<ArgumentException>(() =>
                HtmlNumbers.GetOrderedListStart(document.GetBody()));
        });
    }
}
