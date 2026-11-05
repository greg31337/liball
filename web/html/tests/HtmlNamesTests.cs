
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlNamesTests
{
    [Test]
    public void RecognizesRepresentativeElementContentCategories()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlNames.IsKnownElement("selectedcontent"), Is.True);
            Assert.That(HtmlNames.IsKnownElement("not-an-html-element"), Is.False);
            Assert.That(HtmlNames.IsMetadataElement("title"), Is.True);
            Assert.That(HtmlNames.IsMetadataElement("article"), Is.False);
            Assert.That(HtmlNames.IsFlowElement("ARTICLE"), Is.True);
            Assert.That(HtmlNames.IsSectioningElement("nav"), Is.True);
            Assert.That(HtmlNames.IsHeadingElement("hgroup"), Is.True);
            Assert.That(HtmlNames.IsPhrasingElement("span"), Is.True);
            Assert.That(HtmlNames.IsPhrasingElement("section"), Is.False);
            Assert.That(HtmlNames.IsEmbeddedElement("picture"), Is.True);
            Assert.That(HtmlNames.IsInteractiveElement("button"), Is.True);
            Assert.That(HtmlNames.IsPalpableElement("main"), Is.True);
            Assert.That(HtmlNames.IsScriptSupportingElement("template"), Is.True);
        });
    }

    [Test]
    public void RecognizesEveryCurrentVoidElement()
    {
        string[] names =
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"
        };

        foreach (string name in names)
        {
            Assert.That(HtmlNames.IsVoidElement(name), Is.True, name);
        }

        Assert.That(HtmlNames.IsVoidElement("param"), Is.False);
        Assert.That(HtmlNames.IsVoidElement("div"), Is.False);
    }

    [Test]
    public void RecognizesTextHandlingAndTreeBuilderSets()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlNames.IsRawTextElement("SCRIPT"), Is.True);
            Assert.That(HtmlNames.IsRawTextElement("textarea"), Is.False);
            Assert.That(HtmlNames.IsEscapableRawTextElement("textarea"), Is.True);
            Assert.That(HtmlNames.IsEscapableRawTextElement("style"), Is.False);
            Assert.That(HtmlNames.IsPreformattedElement("pre"), Is.True);
            Assert.That(HtmlNames.IsPreformattedElement("span"), Is.False);
            Assert.That(HtmlNames.IsFormattingElement("nobr"), Is.True);
            Assert.That(HtmlNames.IsFormattingElement("stronger"), Is.False);
            Assert.That(HtmlNames.IsSpecialElement("search"), Is.True);
            Assert.That(HtmlNames.IsSpecialElement("option"), Is.False);
        });
    }

    [Test]
    public void RecognizesEachHtmlScopeBoundaryVariant()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlNames.IsScopingElement("applet"), Is.True);
            Assert.That(HtmlNames.IsScopingElement("ol"), Is.False);
            Assert.That(HtmlNames.IsListItemScopingElement("ol"), Is.True);
            Assert.That(HtmlNames.IsListItemScopingElement("button"), Is.False);
            Assert.That(HtmlNames.IsButtonScopingElement("button"), Is.True);
            Assert.That(HtmlNames.IsTableScopingElement("table"), Is.True);
            Assert.That(HtmlNames.IsTableScopingElement("td"), Is.False);
        });
    }

    [Test]
    public void RecognizesRepresentativeAttributeCategories()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlNames.IsBooleanAttribute("DISABLED"), Is.True);
            Assert.That(HtmlNames.IsBooleanAttribute("href"), Is.False);
            Assert.That(HtmlNames.IsUrlAttribute("href"), Is.True);
            Assert.That(HtmlNames.IsUrlAttribute("class"), Is.False);
            Assert.That(HtmlNames.IsSpaceSeparatedAttribute("rel"), Is.True);
            Assert.That(HtmlNames.IsCommaSeparatedAttribute("srcset"), Is.True);
            Assert.That(HtmlNames.IsEnumeratedAttribute("contenteditable"), Is.True);
            Assert.That(HtmlNames.IsEnumeratedAttribute("checked"), Is.False);
        });
    }
}
