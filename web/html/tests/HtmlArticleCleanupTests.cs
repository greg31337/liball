using System;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlArticleCleanupTests
{
    [Test]
    public void RemovesStructuralNoiseVisibilityHintsAndUnlikelyBlocks()
    {
        HtmlDocument document = Html.Parse(
            "<article id='article-content'>"
            + "<nav id=nav>Navigation</nav>"
            + "<div id=advert class=advertisement>Advertisement</div>"
            + "<div id=articleSide class=article-sidebar>Sidebar</div>"
            + "<form id=form><input value=subscribe></form>"
            + "<div id=side role=' complementary '>Sidebar</div>"
            + "<p id=hidden hidden>Hidden</p>"
            + "<p id=aria aria-hidden='TRUE'>Aria hidden</p>"
            + "<p id=styled style='DISPLAY: none !important'>Styled</p>"
            + "<p id=heading class=heading>Visible heading class</p>"
            + "<p id=visible>" + Prose() + "</p></article>");
        string original = HtmlSerializer.Serialize(document.GetRoot());

        HtmlArticleCleanupResult result = Clean(document,
            HtmlArticleOptions.CreateDefault());
        string cleaned = HtmlSerializer.SerializeChildren(
            result.GetContent());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.GetRemovedNodeCount(), Is.GreaterThanOrEqualTo(8));
            Assert.That(cleaned, Does.Contain("id=\"visible\""));
            Assert.That(cleaned, Does.Contain("id=\"heading\""));
            Assert.That(cleaned, Does.Not.Contain("id=\"nav\""));
            Assert.That(cleaned, Does.Not.Contain("id=\"advert\""));
            Assert.That(cleaned, Does.Not.Contain("id=\"articleSide\""));
            Assert.That(cleaned, Does.Not.Contain("id=\"form\""));
            Assert.That(cleaned, Does.Not.Contain("id=\"side\""));
            Assert.That(cleaned, Does.Not.Contain("id=\"hidden\""));
            Assert.That(cleaned, Does.Not.Contain("id=\"aria\""));
            Assert.That(cleaned, Does.Not.Contain("id=\"styled\""));
            Assert.That(HtmlSerializer.Serialize(document.GetRoot()),
                Is.EqualTo(original));
            Assert.That(result.GetContent().GetOwnerDocument(),
                Is.Not.SameAs(document));
        });
    }

    [Test]
    public void RemovesRepeatedHeadingsAndEmptyNodes()
    {
        HtmlDocument document = Html.Parse(
            "<article id=article><h1>Shared heading</h1>"
            + "<section><h2>SHARED HEADING</h2><p><span></span></p>"
            + "<div>   </div><p>" + Prose() + "</p></section></article>");

        HtmlArticleCleanupResult result = Clean(document,
            HtmlArticleOptions.CreateDefault());
        HtmlNode content = result.GetContent();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(content.FindByTag("h1"), Has.Length.EqualTo(1));
            Assert.That(content.FindByTag("h2"), Is.Empty);
            Assert.That(content.FindByTag("span"), Is.Empty);
            Assert.That(content.FindByTag("div"), Is.Empty);
            Assert.That(content.FindByTag("p"), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void RemovesLinkHeavyBlocksConditionally()
    {
        HtmlDocument document = Html.Parse(
            "<article id=article><div id=links>"
            + "<a href=/one>First destination</a> "
            + "<a href=/two>Second destination</a> "
            + "<a href=/three>Third destination</a></div>"
            + "<p id=prose>" + Prose() + "</p></article>");

        HtmlArticleCleanupResult result = Clean(document,
            HtmlArticleOptions.CreateDefault());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetContent().FindById("links"), Is.Null);
            Assert.That(result.GetContent().FindById("prose"), Is.Not.Null);
        });
    }

    [Test]
    public void CleanupFlagsRetainOptionalCandidatesAndLinkBlocks()
    {
        HtmlDocument document = Html.Parse(
            "<article id=article><div id=side class=sidebar>Side notes</div>"
            + "<div id=links><a href=/one>First destination</a>"
            + "<a href=/two>Second destination</a></div>"
            + "<p>" + Prose() + "</p></article>");
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();
        options.SetRemoveUnlikelyCandidates(false);
        options.SetCleanConditionally(false);

        HtmlArticleCleanupResult result = Clean(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetContent().FindById("side"), Is.Not.Null);
            Assert.That(result.GetContent().FindById("links"), Is.Not.Null);
        });
    }

    [Test]
    public void KeepsMeaningfulNonTextContentDuringCleanup()
    {
        HtmlDocument document = Html.Parse(
            "<article id=article><figure><img src=image.jpg>"
            + "<figcaption>Caption</figcaption></figure>"
            + "<table><tr><td>Cell</td></tr></table>"
            + "<pre><code>sample()</code></pre>"
            + "<math><mi>x</mi></math></article>");

        HtmlArticleCleanupResult result = Clean(document,
            HtmlArticleOptions.CreateDefault());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetContent().FindByTag("figure"), Has.Length.EqualTo(1));
            Assert.That(result.GetContent().FindByTag("table"), Has.Length.EqualTo(1));
            Assert.That(result.GetContent().FindByTag("code"), Has.Length.EqualTo(1));
            Assert.That(result.GetContent().FindByTag("math"), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CleanupHonorsLimitsCancellationAndFrozenSources()
    {
        HtmlDocument document = Html.Parse(
            "<article id=article><p>" + Prose() + "</p></article>");
        HtmlArticleCandidateSelection selection = Selection(document);
        document.Freeze();
        HtmlArticleCleanupResult frozenResult =
            HtmlArticleExtractor.CleanCandidate(selection,
                HtmlArticleOptions.CreateDefault(), CancellationToken.None);
        HtmlArticleOptions limited = HtmlArticleOptions.CreateDefault();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(1);
        limited.SetLimits(limits);
        HtmlArticleCleanupResult limitedResult =
            HtmlArticleExtractor.CleanCandidate(selection, limited,
                CancellationToken.None);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(frozenResult.IsSuccess(), Is.True);
            Assert.That(limitedResult.IsSuccess(), Is.False);
            Assert.That(limitedResult.IsComplete(), Is.False);
            Assert.That(limitedResult.GetContent(), Is.Null);
            Assert.Throws<OperationCanceledException>(() =>
                HtmlArticleExtractor.CleanCandidate(selection,
                    HtmlArticleOptions.CreateDefault(), source.Token));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlArticleExtractor.CleanCandidate(null,
                    HtmlArticleOptions.CreateDefault(),
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlArticleExtractor.CleanCandidate(selection, null,
                    CancellationToken.None));
        });
    }

    static HtmlArticleCleanupResult Clean(HtmlDocument document,
        HtmlArticleOptions options)
    {
        return HtmlArticleExtractor.CleanCandidate(Selection(document),
            options, CancellationToken.None);
    }

    static HtmlArticleCandidateSelection Selection(HtmlDocument document)
    {
        HtmlNode candidate = document.FindById("article");
        if (candidate == null)
        {
            candidate = document.FindById("article-content");
        }

        return new HtmlArticleCandidateSelection(true, true, candidate,
            new HtmlNode[]
            {
                candidate
            }, new HtmlArticleCandidate[0]);
    }

    static string Prose()
    {
        return new string('a', 260) + ",,,,,,,,,, useful ending.";
    }
}
