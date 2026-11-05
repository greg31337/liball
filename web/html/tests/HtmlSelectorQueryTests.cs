using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorQueryTests
{
    [Test]
    public void MatchesCompiledSelectorsAndObservablePseudoClasses()
    {
        HtmlDocument document = Html.Parse(
            "<main><section id=target class='card active' lang=en>"
            + "<a href=/x><span class=label></span></a>"
            + "</section><section class=card></section></main>");
        HtmlNode target = document.FindById("target");
        HtmlNode span = target.QuerySelector("span");
        HtmlSelector compiled = Compile(
            "main > section.card:is(.active,.selected)"
            + ":lang(en):has(> a > span.label)");

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Matches(target, target), Is.True);
            Assert.That(target.Matches(
                "main > section.card:not(.selected):has(a:any-link)"),
                Is.True);
            Assert.That(target.Matches(":scope.card"), Is.True);
            Assert.That(span.Matches("section span.label"), Is.True);
            Assert.That(target.GetNextSibling().Matches(".active"),
                Is.False);
            Assert.That(document.GetBody().Matches("body:root"), Is.False);
        });
    }

    [Test]
    public void ClosestIncludesSelfAndUsesTheOriginalScope()
    {
        HtmlDocument document = Html.Parse(
            "<main class=outer><section class=middle>"
            + "<span class=leaf></span></section></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode section = main.GetFirstChild();
        HtmlNode span = section.GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.That(span.ClosestSelector(".leaf"), Is.SameAs(span));
            Assert.That(span.ClosestSelector("main.outer:has(.leaf)"),
                Is.SameAs(main));
            Assert.That(span.ClosestSelector(":scope"), Is.SameAs(span));
            Assert.That(span.ClosestSelector("aside"), Is.Null);
        });
    }

    [Test]
    public void FirstAndAllQueriesAreScopedOrderedAndDeduplicated()
    {
        HtmlDocument document = Html.Parse(
            "<div class=hit id=outside></div><main id=scope>"
            + "<section class=hit id=first></section>"
            + "<p><span class=hit id=second></span></p>"
            + "<section id=third></section></main>"
            + "<div class=hit id=after></div>");
        HtmlNode main = document.FindById("scope");
        HtmlNode first = document.FindById("first");
        HtmlNode second = document.FindById("second");
        HtmlNode third = document.FindById("third");

        HtmlNode[] found = main.QuerySelectorAll(
            "#second, section, .hit, #first");
        found[0] = main;

        Assert.Multiple(() =>
        {
            Assert.That(main.QuerySelector(
                "#second, section, .hit, #first"), Is.SameAs(first));
            Assert.That(main.QuerySelectorAll(
                "#second, section, .hit, #first"), Is.EqualTo(new[]
                {
                    first, second, third
                }));
            Assert.That(document.QuerySelector(".hit").GetAttribute("id"),
                Is.EqualTo("outside"));
            Assert.That(main.QuerySelectorAll("main, #outside, #after"),
                Is.Empty);
        });
    }

    [Test]
    public void ScopeConstrainsSubjectsButNotOtherSelectorComponents()
    {
        HtmlDocument document = Html.Parse(
            "<body><article><main><section class=inside></section>"
            + "</main></article><section class=outside></section></body>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode inside = main.GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.That(main.QuerySelector("body .inside"),
                Is.SameAs(inside));
            Assert.That(main.QuerySelector(":scope > .inside"),
                Is.SameAs(inside));
            Assert.That(main.QuerySelector(":scope"), Is.Null);
            Assert.That(main.QuerySelectorAll("section"),
                Is.EqualTo(new[] { inside }));
        });
    }

    [Test]
    public void NamespaceWildcardsAndNoNamespaceAreMatched()
    {
        HtmlDocument document = Html.Parse(
            "<main><circle id=html></circle><svg>"
            + "<circle id=svg></circle></svg></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode html = document.FindById("html");
        HtmlNode svg = document.FindById("svg");

        Assert.Multiple(() =>
        {
            Assert.That(main.QuerySelectorAll("circle"), Is.EqualTo(new[]
            {
                html, svg
            }));
            Assert.That(main.QuerySelectorAll("*|circle"), Is.EqualTo(new[]
            {
                html, svg
            }));
            Assert.That(main.QuerySelectorAll("|circle"), Is.Empty);
            Assert.Throws<ArgumentException>(() =>
                main.QuerySelector("svg|circle"));
        });
    }

    [Test]
    public void DetachedSubtreesUseTheSameScopedDocumentOrderBehavior()
    {
        HtmlDocument document = Html.Parse("<main></main>");
        HtmlNode fragment = document.CreateFragment();
        HtmlNode first = document.CreateElement("div");
        HtmlNode wrapper = document.CreateElement("section");
        HtmlNode second = document.CreateElement("div");
        first.SetAttribute("class", "hit");
        second.SetAttribute("class", "hit");
        wrapper.Append(second);
        fragment.Append(first);
        fragment.Append(wrapper);

        Assert.That(fragment.QuerySelectorAll("section .hit, div.hit"),
            Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void CandidateIndexesRefreshAfterMutation()
    {
        HtmlDocument document = Html.Parse(
            "<main><div id=old class=before></div></main>");
        HtmlNode node = document.FindById("old");
        HtmlSelector selector = Compile("#new, .after");

        Assert.That(selector.FindAll(document.GetRoot()), Is.Empty);
        node.SetAttribute("id", "new");
        node.SetAttribute("class", "after");

        Assert.Multiple(() =>
        {
            Assert.That(selector.FindFirst(document.GetRoot()),
                Is.SameAs(node));
            Assert.That(selector.FindAll(document.GetRoot()),
                Is.EqualTo(new[] { node }));
        });
    }

    [Test]
    public void IdCandidateSeedingKeepsDuplicateIdsInDocumentOrder()
    {
        HtmlDocument document = Html.Parse(
            "<main><div id=same></div><section id=same></section></main>");
        HtmlNode main = document.QuerySelector("main");

        Assert.That(main.QuerySelectorAll("#same"), Is.EqualTo(
            main.GetChildElements()));
    }

    [Test]
    public void ResultVisitWorkAndCancellationLimitsTerminateQueries()
    {
        HtmlDocument document = Html.Parse(
            "<main><i class=x></i><i class=x></i><i class=x></i></main>");
        HtmlLimits resultLimits = HtmlLimits.CreateDefault();
        resultLimits.SetMaximumSelectorResults(2);
        HtmlLimits visitLimits = HtmlLimits.CreateDefault();
        visitLimits.SetMaximumVisits(1);
        HtmlLimits workLimits = HtmlLimits.CreateDefault();
        workLimits.SetMaximumHasWork(1);
        HtmlSelector limitedResults = Compile(".x", resultLimits);
        HtmlSelector limitedVisits = Compile(".x", visitLimits);
        HtmlSelector limitedWork = Compile(".x", workLimits);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(limitedResults.FindAll(document.GetRoot()),
                Has.Length.EqualTo(2));
            Assert.Throws<InvalidOperationException>(() =>
                limitedVisits.FindAll(document.GetRoot()));
            Assert.Throws<InvalidOperationException>(() =>
                limitedWork.FindFirst(document.GetRoot()));
            Assert.Throws<OperationCanceledException>(() =>
                limitedResults.FindAllWithCancellation(document.GetRoot(), source.Token));
        });
    }

    [Test]
    public void PublicSelectorQueryArgumentsAreValidated()
    {
        HtmlDocument document = Html.Parse("<main></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlSelector selector = Compile("main");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                selector.Matches(null, main));
            Assert.Throws<ArgumentNullException>(() =>
                selector.Matches(main, null));
            Assert.Throws<ArgumentNullException>(() =>
                selector.FindFirst(null));
            Assert.Throws<ArgumentNullException>(() =>
                selector.FindAll(null));
            Assert.Throws<ArgumentNullException>(() => main.Matches(null));
            Assert.Throws<ArgumentNullException>(() =>
                main.QuerySelector(null));
            Assert.Throws<ArgumentException>(() => main.Matches("div>"));
        });
    }

    static HtmlSelector Compile(string source)
    {
        return Compile(source, HtmlLimits.CreateDefault());
    }

    static HtmlSelector Compile(string source, HtmlLimits limits)
    {
        HtmlSelectorCompileResult result = HtmlSelectors.Compile(source,
            limits);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetSelector();
    }
}
