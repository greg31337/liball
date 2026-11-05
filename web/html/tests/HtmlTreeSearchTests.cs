
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeSearchTests
{
    [Test]
    public void DocumentNameAndAttributeHelpersReturnOrderedSnapshots()
    {
        HtmlDocument document = Html.Parse(
            "<form><input name=group data-code=A>"
            + "<input name=Group><button name=group data-code=B></button></form>");
        HtmlNode form = document.GetBody().GetFirstChild();
        HtmlNode first = form.GetFirstChild();
        HtmlNode second = first.GetNextSibling();
        HtmlNode button = second.GetNextSibling();

        HtmlNode[] named = document.FindByName("group");
        HtmlNode[] present = document.FindByAttribute("DATA-CODE");
        HtmlNode[] valued = document.FindByAttributeValue("data-code", "B");
        named[0] = second;
        present[0] = second;

        Assert.Multiple(() =>
        {
            Assert.That(document.FindByName("group"), Is.EqualTo(new[]
            {
                first, button
            }));
            Assert.That(document.FindByName("Group"), Is.EqualTo(new[]
            {
                second
            }));
            Assert.That(document.FindByAttribute("data-code"), Is.EqualTo(
                new[] { first, button }));
            Assert.That(valued, Is.EqualTo(new[] { button }));
            Assert.That(document.FindByAttributeValue("data-code", "b"), Is.Empty);
        });
    }

    [Test]
    public void ElementQueriesAreScopedToDescendantsAndUseDomNameRules()
    {
        HtmlDocument document = Html.Parse(
            "<main id=scope class=chosen><section><x-item id=inside class=chosen>"
            + "</x-item></section></main><x-item id=outside class=chosen></x-item>");
        HtmlNode main = document.GetBody().GetFirstChild();
        HtmlNode section = main.GetFirstChild();
        HtmlNode inside = section.GetFirstChild();

        HtmlNode svgLower = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "widget");
        HtmlNode svgUpper = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "WIDGET");
        section.Append(svgLower);
        section.Append(svgUpper);

        Assert.Multiple(() =>
        {
            Assert.That(main.FindById("scope"), Is.Null);
            Assert.That(main.FindById("inside"), Is.SameAs(inside));
            Assert.That(main.FindByClass("chosen"), Is.EqualTo(new[]
            {
                inside
            }));
            Assert.That(main.FindByTag("X-ITEM"), Is.EqualTo(new[]
            {
                inside
            }));
            Assert.That(main.FindByTag("widget"), Is.EqualTo(new[]
            {
                svgLower
            }));
            Assert.That(main.FindByTag("WIDGET"), Is.EqualTo(new[]
            {
                svgUpper
            }));
            Assert.That(section.FindByTagWithLimits("*", 2, 10), Is.EqualTo(new[]
            {
                inside, svgLower
            }));
        });
    }

    [Test]
    public void PredicateQueriesVisitEveryNodeKindInDepthFirstOrder()
    {
        HtmlDocument document = Html.Parse(
            "<main>lead<section><!--note--><b>deep</b></section>tail</main>");
        HtmlNode main = document.GetBody().GetFirstChild();
        HtmlNode section = main.GetChildElements()[0];
        HtmlNode bold = section.GetChildElements()[0];
        List<string> visited = new List<string>();

        HtmlNode first = main.FindFirst(node =>
        {
            visited.Add(node.GetName());
            return node == bold;
        });
        HtmlNode[] text = main.FindAll(node =>
            node.GetKind() == HtmlNodeKind.Text);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(bold));
            Assert.That(visited, Is.EqualTo(new[]
            {
                "#text", "section", "#comment", "b"
            }));
            Assert.That(text.Select(node => node.GetData()), Is.EqualTo(new[]
            {
                "lead", "deep", "tail"
            }));
            Assert.That(document.FindFirst(node => node == main),
                Is.SameAs(main));
            Assert.That(document.FindAll(node =>
                node.GetKind() == HtmlNodeKind.Document), Is.Empty);
        });
    }

    [Test]
    public void ResultAndVisitLimitsBoundPredicateWork()
    {
        HtmlDocument document = Html.Parse("<main><a>one</a><b>two</b></main>");
        HtmlNode main = document.GetBody().GetFirstChild();
        int predicateCalls = 0;

        HtmlNode[] limited = main.FindAllWithLimits(node =>
        {
            predicateCalls++;
            return true;
        }, 2, 10);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Select(node => node.GetName()), Is.EqualTo(
                new[] { "a", "#text" }));
            Assert.That(predicateCalls, Is.EqualTo(2));
            Assert.That(main.FindFirstWithLimit(node => node.GetName() == "#text", 2),
                Is.SameAs(main.GetFirstChild().GetFirstChild()));
        });

        predicateCalls = 0;
        Assert.Throws<InvalidOperationException>(() => main.FindAllWithLimits(node =>
        {
            predicateCalls++;
            return false;
        }, 10, 2));
        Assert.That(predicateCalls, Is.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => main.FindFirstWithLimit(
                node => node.GetName() == "b", 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => main.FindAllWithLimits(
                node => true, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => main.FindAllWithLimits(
                node => true, 10, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => main.FindByTagWithLimits(
                "a", 1, 0));
        });
    }

    [Test]
    public void ClosestIncludesSelfAndWalksTheCapturedAncestorChain()
    {
        HtmlDocument document = Html.Parse(
            "<main><section><span>text</span></section></main>");
        HtmlNode main = document.GetBody().GetFirstChild();
        HtmlNode section = main.GetFirstChild();
        HtmlNode span = section.GetFirstChild();
        HtmlNode text = span.GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.That(span.Closest(node => node == span), Is.SameAs(span));
            Assert.That(text.Closest(node => node == section),
                Is.SameAs(section));
            Assert.That(section.Closest(node => node.GetName() == "aside"),
                Is.Null);
            Assert.Throws<InvalidOperationException>(() => text.ClosestWithLimit(
                node => node == main, 2));
        });

        HtmlNode found = text.Closest(node =>
        {
            if (node == span)
            {
                span.Remove();
            }

            return node == section;
        });

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.SameAs(section));
            Assert.That(span.GetParent(), Is.Null);
        });
    }

    [Test]
    public void SearchResultsRemainSnapshotsAfterTreeMutation()
    {
        HtmlDocument document = Html.Parse(
            "<main><div id=first></div><div id=second></div></main>");
        HtmlNode main = document.GetBody().GetFirstChild();
        HtmlNode first = main.GetFirstChild();
        HtmlNode second = first.GetNextSibling();
        HtmlNode[] snapshot = main.FindByTag("div");

        main.InsertBefore(second, first);
        snapshot[0] = main;

        Assert.Multiple(() =>
        {
            Assert.That(main.FindByTag("div"), Is.EqualTo(new[]
            {
                second, first
            }));
            Assert.That(snapshot[1], Is.SameAs(second));
        });
    }

    [Test]
    public void SearchArgumentsAreValidatedBeforePredicatesRun()
    {
        HtmlDocument document = Html.Parse("<main></main>");
        HtmlNode main = document.GetBody().GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => main.FindFirst(null));
            Assert.Throws<ArgumentNullException>(() => main.FindAll(null));
            Assert.Throws<ArgumentNullException>(() => main.Closest(
                (Func<HtmlNode, bool>)null));
            Assert.Throws<ArgumentNullException>(() => main.FindById(null));
            Assert.Throws<ArgumentNullException>(() => main.FindByTag(null));
            Assert.Throws<ArgumentNullException>(() => main.FindByName(null));
            Assert.Throws<ArgumentNullException>(() =>
                main.FindByAttribute(null));
            Assert.Throws<ArgumentNullException>(() =>
                main.FindByAttributeValue("data-x", null));
            Assert.Throws<ArgumentException>(() => main.FindByClass(""));
            Assert.Throws<ArgumentException>(() =>
                main.FindByClass("two words"));
            Assert.Throws<ArgumentNullException>(() =>
                document.FindFirst(null));
        });
    }
}
