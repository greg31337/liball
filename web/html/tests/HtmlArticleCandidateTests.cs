using System;
using System.Linq;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlArticleCandidateTests
{
    [Test]
    public void PositiveAndNegativeClassHintsSelectArticleContent()
    {
        string prose = Prose();
        HtmlDocument document = Html.Parse(
            "<div id='article-content'><p>" + prose + "</p></div>"
            + "<div class='sidebar related'><p>" + prose + prose
            + "</p></div>");

        HtmlArticleCandidateSelection selection = Select(document);

        Assert.Multiple(() =>
        {
            Assert.That(selection.IsSuccess(), Is.True);
            Assert.That(selection.IsComplete(), Is.True);
            Assert.That(selection.GetCandidate(),
                Is.SameAs(document.FindById("article-content")));
            Assert.That(selection.GetTopCandidates()[0].GetRawScore(),
                Is.GreaterThan(25));
        });
    }

    [Test]
    public void ParagraphScorePropagatesToParentAndGrandparent()
    {
        HtmlDocument document = Html.Parse(
            "<div id=grand><section id=parent><p>" + Prose()
            + "</p></section></div>");
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();
        options.SetWeightClassesAndIds(false);

        HtmlArticleCandidateSelection selection =
            HtmlArticleExtractor.SelectCandidate(document, options,
                CancellationToken.None);
        HtmlArticleCandidate parent = FindCandidate(selection, "parent");
        HtmlArticleCandidate grand = FindCandidate(selection, "grand");

        Assert.Multiple(() =>
        {
            Assert.That(selection.IsSuccess(), Is.True);
            Assert.That(selection.GetCandidate(),
                Is.SameAs(document.FindById("parent")));
            Assert.That(parent.GetRawScore(), Is.GreaterThan(20));
            Assert.That(grand.GetRawScore(), Is.GreaterThan(5));
            Assert.That(parent.GetRawScore(),
                Is.GreaterThan(grand.GetRawScore()));
        });
    }

    [Test]
    public void AggregatesQualifyingSiblingsInDocumentOrder()
    {
        string prose = Prose();
        HtmlDocument document = Html.Parse(
            "<div id=first class='article-content'><p>" + prose
            + "</p></div><div id=second class='article-content'><p>"
            + prose + "</p></div><div id=promo class='promo sidebar'>"
            + "<p><a href='/promo'>" + new string('x', 500)
            + "</a></p></div>");

        HtmlArticleCandidateSelection selection = Select(document);
        HtmlNode[] siblings = selection.GetSiblings();
        siblings[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(selection.GetSiblings().Select(value =>
                value.GetAttribute("id")), Is.EqualTo(new[]
                {
                    "first", "second"
                }));
            Assert.That(selection.GetSiblings().Any(value =>
                value.GetAttribute("id") == "promo"), Is.False);
        });
    }

    [Test]
    public void LinkDensityReducesCandidateScore()
    {
        HtmlDocument document = Html.Parse(
            "<div id=linked><p><a href='/many'>" + new string('l', 300)
            + "</a>" + new string('n', 60) + ", end.</p></div>"
            + "<div id=plain><p>" + new string('p', 120)
            + ", useful text.</p></div>");
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();
        options.SetScoreThreshold(0);
        options.SetMaximumTopCandidates(10);

        HtmlArticleCandidateSelection selection =
            HtmlArticleExtractor.SelectCandidate(document, options,
                CancellationToken.None);
        HtmlArticleCandidate linked = FindCandidate(selection, "linked");
        HtmlArticleCandidate plain = FindCandidate(selection, "plain");

        Assert.Multiple(() =>
        {
            Assert.That(selection.GetCandidate(),
                Is.SameAs(document.FindById("plain")));
            Assert.That(linked.GetLinkDensity(), Is.GreaterThan(0.8));
            Assert.That(linked.GetScore(), Is.LessThan(plain.GetScore()));
        });
    }

    [Test]
    public void ClassWeightingCanBeDisabledAndRankingIsStable()
    {
        string prose = Prose();
        HtmlDocument document = Html.Parse(
            "<div id=neutral><p>" + prose + "</p></div>"
            + "<div id=weighted class='articleBody'><p>" + prose
            + "</p></div>");
        HtmlArticleOptions weightedOptions = HtmlArticleOptions.CreateDefault();
        weightedOptions.SetScoreThreshold(0);
        HtmlArticleOptions unweightedOptions = weightedOptions.Copy();
        unweightedOptions.SetWeightClassesAndIds(false);

        HtmlArticleCandidateSelection weighted =
            HtmlArticleExtractor.SelectCandidate(document, weightedOptions,
                CancellationToken.None);
        HtmlArticleCandidateSelection unweighted =
            HtmlArticleExtractor.SelectCandidate(document, unweightedOptions,
                CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(weighted.GetCandidate(),
                Is.SameAs(document.FindById("weighted")));
            Assert.That(unweighted.GetCandidate(),
                Is.SameAs(document.FindById("neutral")));
        });
    }

    [Test]
    public void HonorsTopCandidateScoreAndWorkLimits()
    {
        HtmlDocument document = Html.Parse(
            "<article id=article><p>" + Prose() + "</p></article>"
            + "<section><p>" + Prose() + "</p></section>");
        HtmlArticleOptions capped = HtmlArticleOptions.CreateDefault();
        capped.SetMaximumTopCandidates(2);
        HtmlArticleCandidateSelection cappedSelection =
            HtmlArticleExtractor.SelectCandidate(document, capped,
                CancellationToken.None);
        HtmlArticleOptions scoreLimited = capped.Copy();
        scoreLimited.SetScoreThreshold(1000);
        HtmlArticleCandidateSelection scoreSelection =
            HtmlArticleExtractor.SelectCandidate(document, scoreLimited,
                CancellationToken.None);
        HtmlArticleOptions workLimited = capped.Copy();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumHasWork(1);
        workLimited.SetLimits(limits);
        HtmlArticleCandidateSelection workSelection =
            HtmlArticleExtractor.SelectCandidate(document, workLimited,
                CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(cappedSelection.GetTopCandidates(), Has.Length.EqualTo(2));
            Assert.That(scoreSelection.IsSuccess(), Is.False);
            Assert.That(scoreSelection.IsComplete(), Is.True);
            Assert.That(scoreSelection.GetTopCandidates(), Has.Length.EqualTo(2));
            Assert.That(workSelection.IsSuccess(), Is.False);
            Assert.That(workSelection.IsComplete(), Is.False);
            Assert.That(workSelection.GetCandidate(), Is.Null);
        });
    }

    [Test]
    public void SelectionHonorsCancellationAndValidatesArguments()
    {
        HtmlDocument document = Html.Parse("<article>" + Prose()
            + "</article>");
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                HtmlArticleExtractor.SelectCandidate(document,
                    HtmlArticleOptions.CreateDefault(), source.Token));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlArticleExtractor.SelectCandidate(null,
                    HtmlArticleOptions.CreateDefault(),
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlArticleExtractor.SelectCandidate(document, null,
                    CancellationToken.None));
        });
    }

    static HtmlArticleCandidateSelection Select(HtmlDocument document)
    {
        return HtmlArticleExtractor.SelectCandidate(document,
            HtmlArticleOptions.CreateDefault(), CancellationToken.None);
    }

    static HtmlArticleCandidate FindCandidate(
        HtmlArticleCandidateSelection selection, string id)
    {
        return selection.GetTopCandidates().Single(value =>
            value.GetNode().GetAttribute("id") == id);
    }

    static string Prose()
    {
        return new string('a', 260) + ",,,,,,,,,, useful ending.";
    }
}
