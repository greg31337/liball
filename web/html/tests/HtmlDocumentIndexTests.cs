
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDocumentIndexTests
{
    [Test]
    public void SearchIndexesAreBuiltLazilySharedAndMutationVersioned()
    {
        HtmlDocument document = Html.Parse(
            "<main><div id=duplicate class='alpha beta alpha'></div>"
            + "<span id=duplicate class=Alpha></span></main>");
        HtmlNode main = document.GetBody().GetFirstChild();
        HtmlNode first = main.GetFirstChild();
        HtmlNode second = first.GetNextSibling();

        Assert.Multiple(() =>
        {
            Assert.That(document.HasCurrentSearchIndexStorage(), Is.False);
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.Zero);
            Assert.That(document.FindById("duplicate"), Is.SameAs(first));
            Assert.That(document.HasCurrentSearchIndexStorage(), Is.True);
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.EqualTo(1));
        });

        Assert.Multiple(() =>
        {
            Assert.That(document.FindByClass("alpha"), Is.EqualTo(new[]
            {
                first
            }));
            Assert.That(document.FindByClass("Alpha"), Is.EqualTo(new[]
            {
                second
            }));
            Assert.That(document.FindByTag("DIV"), Is.EqualTo(new[]
            {
                first
            }));
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.EqualTo(1));
        });

        first.SetAttribute("id", "duplicate");
        Assert.Multiple(() =>
        {
            Assert.That(document.HasCurrentSearchIndexStorage(), Is.True);
            Assert.That(document.FindById("duplicate"), Is.SameAs(first));
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.EqualTo(1));
        });

        first.SetAttribute("id", "changed");
        Assert.That(document.HasCurrentSearchIndexStorage(), Is.False);

        Assert.Multiple(() =>
        {
            Assert.That(document.FindById("duplicate"), Is.SameAs(second));
            Assert.That(document.FindById("changed"), Is.SameAs(first));
            Assert.That(document.HasCurrentSearchIndexStorage(), Is.True);
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.EqualTo(2));
        });
    }

    [Test]
    public void DuplicateIdsFollowCurrentDocumentOrderAfterTreeMutations()
    {
        HtmlDocument document = Html.Parse(
            "<main><section id=same></section><article id=same></article></main>");
        HtmlNode main = document.GetBody().GetFirstChild();
        HtmlNode section = main.GetFirstChild();
        HtmlNode article = section.GetNextSibling();

        Assert.That(document.FindById("same"), Is.SameAs(section));

        main.InsertBefore(article, section);
        Assert.Multiple(() =>
        {
            Assert.That(document.HasCurrentSearchIndexStorage(), Is.False);
            Assert.That(document.FindById("same"), Is.SameAs(article));
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.EqualTo(2));
        });

        article.Remove();
        Assert.That(document.FindById("same"), Is.SameAs(section));

        section.Remove();
        Assert.Multiple(() =>
        {
            Assert.That(document.FindById("same"), Is.Null);
            Assert.That(document.FindByTag("section"), Is.Empty);
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.EqualTo(4));
        });
    }

    [Test]
    public void TagSearchCombinesHtmlAndExactForeignNamesInDocumentOrder()
    {
        HtmlDocument document = Html.Parse("<!doctype html><body></body>");
        HtmlNode body = document.GetBody();
        HtmlNode htmlFirst = document.CreateElement("Widget");
        HtmlNode foreignLower = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "widget");
        HtmlNode foreignUpper = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "WIDGET");
        HtmlNode htmlLast = document.CreateElement("widget");
        body.Append(htmlFirst);
        body.Append(foreignLower);
        body.Append(foreignUpper);
        body.Append(htmlLast);

        HtmlNode[] lower = document.FindByTag("widget");
        HtmlNode[] upper = document.FindByTag("WIDGET");
        HtmlNode[] mixed = document.FindByTag("Widget");
        HtmlNode[] all = document.FindByTag("*");
        lower[0] = foreignUpper;
        all[0] = foreignUpper;

        Assert.Multiple(() =>
        {
            Assert.That(document.FindByTag("widget"), Is.EqualTo(new[]
            {
                htmlFirst, foreignLower, htmlLast
            }));
            Assert.That(upper, Is.EqualTo(new[]
            {
                htmlFirst, foreignUpper, htmlLast
            }));
            Assert.That(mixed, Is.EqualTo(new[]
            {
                htmlFirst, htmlLast
            }));
            Assert.That(document.FindByTag("*"), Is.EqualTo(new[]
            {
                document.GetDocumentElement(), document.GetHead(), body,
                htmlFirst, foreignLower, foreignUpper, htmlLast
            }));
            Assert.That(document.FindByTag("missing"), Is.Empty);
        });
    }

    [Test]
    public void ClassAndIdSearchUseTokensAndExcludeTemplateAndDetachedContent()
    {
        HtmlDocument document = Html.Parse(
            "<main><div id=outside class='one\ttwo one'></div></main>");
        HtmlNode main = document.GetBody().GetFirstChild();
        HtmlNode outside = main.GetFirstChild();
        HtmlNode template = document.CreateElement("template");
        HtmlNode inside = document.CreateElement("i");
        inside.SetAttribute("id", "inside");
        inside.SetAttribute("class", "one two");
        template.Append(inside);
        document.GetBody().Prepend(template);

        Assert.Multiple(() =>
        {
            Assert.That(document.FindById("inside"), Is.Null);
            Assert.That(document.FindById("outside"), Is.SameAs(outside));
            Assert.That(document.FindByClass("one"), Is.EqualTo(new[]
            {
                outside
            }));
            Assert.That(document.FindByClass("two"), Is.EqualTo(new[]
            {
                outside
            }));
            Assert.That(document.FindByClass("One"), Is.Empty);
            Assert.That(document.FindByTag("i"), Is.Empty);
            Assert.That(inside.GetParent(),
                Is.SameAs(template.GetTemplateContent()));
        });

        outside.AddClass("three");
        Assert.That(document.FindByClass("three"), Is.EqualTo(new[]
        {
            outside
        }));

        outside.Remove();
        Assert.Multiple(() =>
        {
            Assert.That(document.FindById("outside"), Is.Null);
            Assert.That(document.FindByClass("one"), Is.Empty);
            Assert.That(document.FindByClass("three"), Is.Empty);
        });
    }

    [Test]
    public void SearchArgumentsAreValidatedBeforeBuildingAnIndex()
    {
        HtmlDocument document = Html.Parse("<p>x</p>");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => document.FindById(null));
            Assert.Throws<ArgumentNullException>(() => document.FindByTag(null));
            Assert.Throws<ArgumentNullException>(() => document.FindByClass(null));
            Assert.Throws<ArgumentException>(() => document.FindByClass(""));
            Assert.Throws<ArgumentException>(() => document.FindByClass("two words"));
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.Zero);
        });
    }

    [Test]
    public void FrozenDocumentBuildsOneIndexForConcurrentReaders()
    {
        HtmlDocument document = Html.Parse(
            "<main><div id=target class=active></div><div class=active></div></main>");
        HtmlNode target = document.GetBody().GetFirstChild().GetFirstChild();
        bool[] results = new bool[128];
        document.Freeze();

        Parallel.For(0, results.Length, index =>
        {
            results[index] = document.FindById("target") == target
                && document.FindByClass("active").Length == 2
                && document.FindByTag("div").Length == 2;
        });

        Assert.Multiple(() =>
        {
            Assert.That(results, Is.All.True);
            Assert.That(document.HasCurrentSearchIndexStorage(), Is.True);
            Assert.That(document.GetSearchIndexBuildCountStorage(), Is.EqualTo(1));
        });
    }
}
