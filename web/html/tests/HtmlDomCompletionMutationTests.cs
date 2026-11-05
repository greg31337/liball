namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDomCompletionMutationTests
{
    const string XlinkNamespace = "http://www.w3.org/1999/xlink";
    const string SvgNamespace = "http://www.w3.org/2000/svg";

    [Test]
    public void FirstAndLastElementChildrenSkipOtherNodeKinds()
    {
        HtmlDocument document = new HtmlApi().ParseString(
            "<main>text<span></span><!--comment--><em></em>tail</main>");
        HtmlNode main = document.QuerySelector("main");

        Assert.Multiple(() =>
        {
            Assert.That(main.GetFirstElementChild().GetName(),
                Is.EqualTo("span"));
            Assert.That(main.GetLastElementChild().GetName(),
                Is.EqualTo("em"));
            Assert.That(document.CreateElement("empty").GetFirstElementChild(),
                Is.Null);
        });
    }

    [Test]
    public void NamespacedAttributesCanBeCreatedUpdatedAndRemovedByExpandedName()
    {
        HtmlDocument document = new HtmlApi().ParseString("<svg><a></a></svg>");
        HtmlNode link = document.QuerySelector("a");
        HtmlAttribute created = document.CreateAttributeInNamespace(
            XlinkNamespace, "xlink:href", "one");
        HtmlAttribute plain = document.CreateAttribute("data-id", "42");

        link.SetAttributeInNamespace(XlinkNamespace, "xlink:href", "one");
        link.SetAttributeInNamespace(XlinkNamespace, "link:href", "two");

        Assert.Multiple(() =>
        {
            Assert.That(created.GetLocalName(), Is.EqualTo("href"));
            Assert.That(created.GetPrefix(), Is.EqualTo("xlink"));
            Assert.That(plain.GetNamespaceUri(), Is.Empty);
            Assert.That(link.HasAttributeInNamespace(XlinkNamespace, "href"),
                Is.True);
            Assert.That(link.GetAttributeInNamespace(XlinkNamespace, "href"),
                Is.EqualTo("two"));
            Assert.That(link.GetAttributes().Count(attribute =>
                attribute.GetNamespaceUri() == XlinkNamespace
                && attribute.GetLocalName() == "href"), Is.EqualTo(1));
            Assert.That(link.GetAttributes().Single().GetName(),
                Is.EqualTo("link:href"));
        });

        Assert.Throws<ArgumentException>(() => link.SetAttributeInNamespace(
            "", "bad:href", "unchanged"));
        Assert.That(link.GetAttributeInNamespace(XlinkNamespace, "href"),
            Is.EqualTo("two"));
        Assert.That(link.RemoveAttributeInNamespace(XlinkNamespace, "href"),
            Is.True);
        Assert.That(link.RemoveAttributeInNamespace(XlinkNamespace, "href"),
            Is.False);
    }

    [Test]
    public void ClassAndDatasetOperationsNormalizeTokensAndNames()
    {
        HtmlDocument document = new HtmlApi().ParseString(
            "<div class='alpha alpha beta' data-user-id=7></div>");
        HtmlNode element = document.QuerySelector("div");

        Assert.That(element.GetClassTokens(),
            Is.EqualTo(new[] { "alpha", "beta" }));
        Assert.That(element.ReplaceClass("alpha", "beta"), Is.True);
        Assert.That(element.ToggleClass("gamma", true), Is.True);
        Assert.That(element.ToggleClass("gamma", false), Is.False);
        element.SetDatasetValue("itemCount", "3");

        Assert.Multiple(() =>
        {
            Assert.That(element.GetClassTokens(),
                Is.EqualTo(new[] { "beta" }));
            Assert.That(element.GetDatasetValue("userId"), Is.EqualTo("7"));
            Assert.That(element.HasDatasetValue("itemCount"), Is.True);
            Assert.That(element.GetAttribute("data-item-count"),
                Is.EqualTo("3"));
            Assert.That(element.GetDatasetNames(),
                Is.EqualTo(new[] { "userId", "itemCount" }));
        });

        Assert.Throws<ArgumentException>(() =>
            element.SetDatasetValue("bad-name", "x"));
        Assert.That(element.RemoveDatasetValue("itemCount"), Is.True);
        Assert.That(element.HasDatasetValue("itemCount"), Is.False);
    }

    [Test]
    public void RenamePreservesContentAndMovesChildrenAcrossTemplateStorage()
    {
        HtmlDocument document = new HtmlApi().ParseString(
            "<section id=one><span>x</span></section><div><b>y</b></div>");
        HtmlNode section = document.QuerySelector("section");
        HtmlNode convertible = document.QuerySelector("div");
        HtmlNode child = convertible.QuerySelector("b");

        section.Rename("ARTICLE");
        section.RenameInNamespace(SvgNamespace, "svg:g");
        convertible.Rename("template");

        Assert.Multiple(() =>
        {
            Assert.That(section.GetName(), Is.EqualTo("svg:g"));
            Assert.That(section.GetLocalName(), Is.EqualTo("g"));
            Assert.That(section.GetNamespaceUri(), Is.EqualTo(SvgNamespace));
            Assert.That(section.GetAttribute("id"), Is.EqualTo("one"));
            Assert.That(section.GetTextContent(), Is.EqualTo("x"));
            Assert.That(convertible.GetChildren(), Is.Empty);
            Assert.That(convertible.GetTemplateContent().GetChildren(),
                Is.EqualTo(new[] { child }));
        });

        convertible.Rename("div");
        Assert.Multiple(() =>
        {
            Assert.That(convertible.GetTemplateContent(), Is.Null);
            Assert.That(convertible.GetChildren(), Is.EqualTo(new[] { child }));
            Assert.That(child.GetParent(), Is.SameAs(convertible));
        });
    }

    [Test]
    public void WrapReplaceChildAndDetachRepairEveryRelationship()
    {
        HtmlDocument document = new HtmlApi().ParseString(
            "<main><p>text</p><aside></aside></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlNode aside = document.QuerySelector("aside");
        HtmlNode wrapper = document.CreateElement("section");
        HtmlNode leading = document.CreateElement("strong");
        wrapper.Append(leading);

        Assert.That(paragraph.Wrap(wrapper), Is.SameAs(wrapper));
        HtmlNode fragment = document.CreateFragment();
        HtmlNode bold = document.CreateElement("b");
        HtmlNode italic = document.CreateElement("i");
        fragment.Append(bold);
        fragment.Append(italic);
        HtmlNode replaced = main.ReplaceChild(fragment, aside);
        HtmlNode detached = bold.Detach();

        Assert.Multiple(() =>
        {
            Assert.That(wrapper.GetChildren(),
                Is.EqualTo(new[] { leading, paragraph }));
            Assert.That(paragraph.GetParent(), Is.SameAs(wrapper));
            Assert.That(replaced, Is.SameAs(aside));
            Assert.That(aside.GetParent(), Is.Null);
            Assert.That(fragment.GetChildren(), Is.Empty);
            Assert.That(detached, Is.SameAs(bold));
            Assert.That(bold.GetParent(), Is.Null);
            Assert.That(main.GetChildren(), Is.EqualTo(new[] { wrapper, italic }));
            Assert.That(HtmlDomInvariant.FindViolation(document, aside, bold,
                fragment), Is.Empty);
        });
    }

    [Test]
    public void InnerAndOuterHtmlUseContextSensitiveFragmentParsing()
    {
        HtmlDocument document = new HtmlApi().ParseString(
            "<main><table></table><textarea></textarea><p></p><i>end</i>"
            + "<template></template></main>");
        HtmlNode table = document.QuerySelector("table");
        HtmlNode textarea = document.QuerySelector("textarea");
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlNode template = document.QuerySelector("template");

        table.SetInnerHtml("<tr><td>x");
        textarea.SetInnerHtml("<b>&amp;");
        template.SetInnerHtml("<span>inside</span>");
        paragraph.SetOuterHtml("<b>one</b>tail");

        Assert.Multiple(() =>
        {
            Assert.That(table.GetInnerHtml(),
                Is.EqualTo("<tbody><tr><td>x</td></tr></tbody>"));
            Assert.That(textarea.GetTextContent(), Is.EqualTo("<b>&"));
            Assert.That(template.GetTemplateContent().GetInnerHtml(),
                Is.EqualTo("<span>inside</span>"));
            Assert.That(document.QuerySelector("main").GetInnerHtml(),
                Does.Contain("<b>one</b>tail<i>end</i>"));
            Assert.That(paragraph.GetParent(), Is.Null);
        });
    }

    [Test]
    public void NamedCloneAndImportVariantsExposeOwnershipAndDepth()
    {
        HtmlDocument source = new HtmlApi().ParseString(
            "<article><span>x</span></article>");
        HtmlDocument target = new HtmlApi().ParseString("");
        HtmlNode article = source.QuerySelector("article");

        HtmlNode shallowClone = article.CloneShallow();
        HtmlNode deepClone = article.CloneDeep();
        HtmlNode shallowImport = target.ImportShallow(article);
        HtmlNode deepImport = target.ImportDeep(article);

        Assert.Multiple(() =>
        {
            Assert.That(shallowClone.GetChildren(), Is.Empty);
            Assert.That(deepClone.GetTextContent(), Is.EqualTo("x"));
            Assert.That(shallowImport.GetChildren(), Is.Empty);
            Assert.That(deepImport.GetTextContent(), Is.EqualTo("x"));
            Assert.That(deepImport.GetOwnerDocument(), Is.SameAs(target));
            Assert.That(deepImport.QuerySelector("span").GetOwnerDocument(),
                Is.SameAs(target));
        });
    }

    [Test]
    public void FrozenTreeRejectsEveryNewMutationWithoutPartialChanges()
    {
        HtmlDocument document = new HtmlApi().ParseString(
            "<main><p class=one data-state=ready>text</p><i></i></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlNode italic = document.QuerySelector("i");
        HtmlNode replacement = document.CreateElement("b");
        HtmlNode wrapper = document.CreateElement("section");
        string before = document.GetRoot().GetOuterHtml();
        long version = document.GetMutationVersion();
        document.Freeze();

        Assert.Throws<InvalidOperationException>(() =>
            paragraph.SetAttributeInNamespace(XlinkNamespace, "xlink:href", "x"));
        Assert.Throws<InvalidOperationException>(() =>
            paragraph.RemoveAttributeInNamespace("", "class"));
        Assert.Throws<InvalidOperationException>(() =>
            paragraph.ToggleClass("two", true));
        Assert.Throws<InvalidOperationException>(() =>
            paragraph.SetDatasetValue("state", "changed"));
        Assert.Throws<InvalidOperationException>(() => paragraph.Rename("div"));
        Assert.Throws<InvalidOperationException>(() => paragraph.Wrap(wrapper));
        Assert.Throws<InvalidOperationException>(() => paragraph.Detach());
        Assert.Throws<InvalidOperationException>(() =>
            main.ReplaceChild(replacement, italic));
        Assert.Throws<InvalidOperationException>(() =>
            paragraph.SetInnerHtml("<b>changed</b>"));
        Assert.Throws<InvalidOperationException>(() =>
            paragraph.SetOuterHtml("<b>changed</b>"));

        Assert.Multiple(() =>
        {
            Assert.That(document.GetRoot().GetOuterHtml(), Is.EqualTo(before));
            Assert.That(document.GetMutationVersion(), Is.EqualTo(version));
            Assert.That(wrapper.GetParent(), Is.Null);
            Assert.That(replacement.GetParent(), Is.Null);
            Assert.That(paragraph.CloneDeep().GetTextContent(),
                Is.EqualTo("text"));
        });
    }
}
