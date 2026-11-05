using System;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorAdvancedCompilationTests
{
    const string SvgNamespaceUri = "http://www.w3.org/2000/svg";
    const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";

    [Test]
    public void StrictAndForgivingListsReportAndRetainDifferentResults()
    {
        HtmlSelectorsApi api = new HtmlSelectorsApi();
        string source = "div,\n::before, .hit";
        HtmlSelectorCompileResult strict = api.Compile(source);
        HtmlSelectorCompileResult forgiving =
            api.CompileForgiving(source);
        HtmlSelector selector = forgiving.GetSelector();
        HtmlSelectorCompileError error = forgiving.GetErrors()[0];
        HtmlSelectorSpecificity[] specificities =
            selector.GetSpecificities();
        specificities[0].TypeCount = 99;
        HtmlDocument document = Html.Parse(
            "<main><div id=first class=hit></div>"
            + "<span id=second class=hit></span></main>");

        Assert.Multiple(() =>
        {
            Assert.That(strict.IsSuccess(), Is.False);
            Assert.That(strict.GetSelector(), Is.Null);
            Assert.That(forgiving.IsSuccess(), Is.True);
            Assert.That(forgiving.IsPartial(), Is.True);
            Assert.That(forgiving.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.None));
            Assert.That(error.GetKind(), Is.EqualTo(
                HtmlSelectorCompileErrorKind.UnsupportedFeature));
            Assert.That(error.GetPosition().GetLine(), Is.EqualTo(2));
            Assert.That(error.GetPosition().GetColumn(), Is.EqualTo(1));
            Assert.That(selector.GetSpecificities().Select(value =>
                (value.IdCount, value.ClassCount, value.TypeCount)),
                Is.EqualTo(new[] { (0, 0, 1), (0, 1, 0) }));
            Assert.That(selector.FindAll(document.GetRoot()).Select(node =>
                node.GetAttribute("id")), Is.EqualTo(
                new[] { "first", "second" }));
        });
    }

    [Test]
    public void ForgivingListFailsWhenEveryBranchIsUnusable()
    {
        HtmlSelectorsApi api = new HtmlSelectorsApi();
        HtmlSelectorCompileResult result = api.CompileForgiving(
            "::before, svg|circle");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsPartial(), Is.False);
            Assert.That(result.GetSelector(), Is.Null);
            Assert.That(result.GetErrors().Select(value => value.GetKind()),
                Is.EqualTo(new[]
                {
                    HtmlSelectorCompileErrorKind.UnsupportedFeature,
                    HtmlSelectorCompileErrorKind.InvalidSyntax
                }));
        });
    }

    [Test]
    public void NamespaceBindingsMatchQualifiedElementsAndAttributes()
    {
        HtmlSelectorsApi api = new HtmlSelectorsApi();
        HtmlSelectorNamespaceBinding svg =
            new HtmlSelectorNamespaceBinding("svg", SvgNamespaceUri);
        HtmlSelectorNamespaceBinding xlink =
            new HtmlSelectorNamespaceBinding("xlink", XlinkNamespaceUri);
        HtmlSelectorCompileResult result = api.CompileWithNamespaces(
            "svg|circle, svg|use[xlink|href]",
            new[] { svg, xlink });
        HtmlSelector selector = result.GetSelector();
        svg.NamespaceUri = "urn:changed-after-compilation";
        HtmlDocument document = Html.Parse(
            "<main><svg><circle id=shape></circle>"
            + "<use id=reference xlink:href=#shape></use></svg>"
            + "<circle id=html></circle></main>");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(selector.FindAll(document.GetRoot()).Select(node =>
                node.GetAttribute("id")), Is.EqualTo(
                new[] { "shape", "reference" }));
            Assert.That(api.Compile("svg|circle").GetErrors()[0]
                .GetMessage(), Does.Contain("has not been declared"));
            Assert.Throws<ArgumentException>(() =>
                api.CompileWithNamespaces("svg|circle", new[]
                {
                    new HtmlSelectorNamespaceBinding("svg", SvgNamespaceUri),
                    new HtmlSelectorNamespaceBinding("svg", "urn:other")
                }));
        });
    }

    [Test]
    public void CompiledOperationsApplyPerQueryLimitsAndCancellation()
    {
        HtmlSelectorsApi api = new HtmlSelectorsApi();
        IHtmlSelector selector = api.Compile(
            ".hit, div, #one").GetSelector();
        HtmlDocument document = Html.Parse(
            "<main class=hit><div id=one class=hit><span></span></div>"
            + "<section id=two class=hit></section>"
            + "<aside id=three class=hit></aside></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode div = document.QuerySelector("div");
        HtmlNode span = document.QuerySelector("span");
        HtmlLimits results = HtmlLimits.CreateDefault();
        results.SetMaximumSelectorResults(2);
        HtmlLimits visits = HtmlLimits.CreateDefault();
        visits.SetMaximumVisits(1);
        CancellationTokenSource cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(selector.MatchesWithLimits(div, main,
                HtmlLimits.CreateDefault(), CancellationToken.None),
                Is.True);
            Assert.That(selector.Closest(span), Is.SameAs(div));
            Assert.That(selector.FindFirstWithLimits(main,
                HtmlLimits.CreateDefault(), CancellationToken.None),
                Is.SameAs(div));
            Assert.That(selector.FindAllWithLimits(main, results,
                CancellationToken.None).Select(node =>
                    node.GetAttribute("id")), Is.EqualTo(
                    new[] { "one", "two" }));
            Assert.Throws<InvalidOperationException>(() =>
                selector.ClosestWithLimits(span, visits,
                    CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() =>
                selector.FindAllWithLimits(main,
                    HtmlLimits.CreateDefault(), cancellation.Token));
        });
    }
}
