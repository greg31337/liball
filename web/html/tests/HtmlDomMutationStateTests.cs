
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDomMutationStateTests
{
    [Test]
    public void HtmlAttributesUseAsciiInsensitiveNamesAndPreserveOrder()
    {
        HtmlSourceSpan span = CreateSpan(0, 20);
        HtmlAttribute id = HtmlAttribute.Create("ID", "one", span);
        HtmlAttribute link = HtmlAttribute.CreateNamespaced("xlink:href", "href",
            "xlink", "http://www.w3.org/1999/xlink", "target", span);
        HtmlDocument document = CreateDocument(span);
        HtmlNode element = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "div", "div", "",
            new[] { id, link }, span);

        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttribute("id"), Is.EqualTo("one"));
            Assert.That(element.GetAttribute("Id"), Is.EqualTo("one"));
            Assert.That(element.HasAttribute("iD"), Is.True);
            Assert.That(element.GetAttributeInNamespace(
                "http://www.w3.org/1999/xlink", "href"),
                Is.EqualTo("target"));
            Assert.That(element.GetAttribute("missing"), Is.Null);
        });

        element.SetAttribute("id", "one");
        Assert.Multiple(() =>
        {
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
            Assert.That(element.GetSourceSpan().IsKnown(), Is.True);
            Assert.That(element.GetAttributes()[0], Is.SameAs(id));
        });

        element.SetAttribute("TITLE", "hello");
        element.SetAttribute("Id", "two");
        HtmlAttribute[] changed = element.GetAttributes();

        Assert.Multiple(() =>
        {
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
            Assert.That(changed.Select(value => value.GetName()),
                Is.EqualTo(new[] { "ID", "xlink:href", "title" }));
            Assert.That(changed.Select(value => value.GetValue()),
                Is.EqualTo(new[] { "two", "target", "hello" }));
            Assert.That(element.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(changed[0].GetSourceSpan().IsKnown(), Is.False);
            Assert.That(changed[1].GetSourceSpan(), Is.SameAs(span));
            Assert.That(changed[2].GetSourceSpan().IsKnown(), Is.False);
        });

        Assert.That(element.RemoveAttribute("missing"), Is.False);
        Assert.That(element.RemoveAttribute("TiTlE"), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(3));
            Assert.That(element.GetAttributes().Select(value => value.GetName()),
                Is.EqualTo(new[] { "ID", "xlink:href" }));
            Assert.That(element.GetAttribute("title"), Is.Null);
        });
    }

    [Test]
    public void ForeignAttributeNamesRemainCaseSensitive()
    {
        HtmlDocument document = CreateDocument(HtmlSourceSpan.CreateUnknown());
        HtmlNode svg = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "svg");

        svg.SetAttribute("viewBox", "0 0 10 10");
        svg.SetAttribute("viewbox", "lowercase");

        Assert.Multiple(() =>
        {
            Assert.That(svg.GetAttribute("viewBox"), Is.EqualTo("0 0 10 10"));
            Assert.That(svg.GetAttribute("viewbox"), Is.EqualTo("lowercase"));
            Assert.That(svg.GetAttribute("VIEWBOX"), Is.Null);
            Assert.That(svg.HasAttribute("viewBox"), Is.True);
            Assert.That(svg.RemoveAttribute("VIEWBOX"), Is.False);
            Assert.That(svg.GetAttributes().Select(value => value.GetName()),
                Is.EqualTo(new[] { "viewBox", "viewbox" }));
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ClassMutationUsesOrderedCaseSensitiveHtmlTokens()
    {
        HtmlSourceSpan span = CreateSpan(0, 10);
        HtmlAttribute classes = HtmlAttribute.Create("class",
            " alpha\tbeta alpha ", span);
        HtmlDocument document = CreateDocument(span);
        HtmlNode element = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "div", "div", "",
            new[] { classes }, span);

        Assert.Multiple(() =>
        {
            Assert.That(element.HasClass("alpha"), Is.True);
            Assert.That(element.HasClass("beta"), Is.True);
            Assert.That(element.HasClass("Alpha"), Is.False);
        });

        element.AddClass("alpha");
        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttribute("class"),
                Is.EqualTo(" alpha\tbeta alpha "));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
            Assert.That(element.GetSourceSpan().IsKnown(), Is.True);
        });

        element.AddClass("gamma");
        element.RemoveClass("beta");
        element.RemoveClass("missing");

        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttribute("class"), Is.EqualTo("alpha gamma"));
            Assert.That(element.HasClass("alpha"), Is.True);
            Assert.That(element.HasClass("beta"), Is.False);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
            Assert.That(element.GetAttributes()[0].GetSourceSpan().IsKnown(),
                Is.False);
        });

        element.RemoveClass("alpha");
        element.RemoveClass("gamma");

        Assert.Multiple(() =>
        {
            Assert.That(element.HasAttribute("class"), Is.True);
            Assert.That(element.GetAttribute("class"), Is.Empty);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(4));
        });

        Assert.Throws<ArgumentNullException>(() => element.AddClass(null));
        Assert.Throws<ArgumentException>(() => element.AddClass(""));
        Assert.Throws<ArgumentException>(() => element.RemoveClass("two words"));
        Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(4));
    }

    [Test]
    public void CharacterDataMutationInvalidatesOnlyChangedNodes()
    {
        HtmlSourceSpan span = CreateSpan(2, 8);
        HtmlDocument document = CreateDocument(span);
        HtmlNode text = document.CreateTextStorage("value", span);
        HtmlNode comment = document.CreateCommentStorage("note", span);
        HtmlNode element = document.CreateElement("div");

        text.SetData("value");
        Assert.Multiple(() =>
        {
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
            Assert.That(text.GetSourceSpan().IsKnown(), Is.True);
        });

        text.SetData("changed");
        comment.SetData("");

        Assert.Multiple(() =>
        {
            Assert.That(text.GetData(), Is.EqualTo("changed"));
            Assert.That(comment.GetData(), Is.Empty);
            Assert.That(text.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(comment.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
        });

        Assert.Throws<ArgumentNullException>(() => text.SetData(null));
        Assert.Throws<InvalidOperationException>(() => element.SetData("invalid"));
        Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
    }

    [Test]
    public void TreeChangesAdvanceOneVersionAndInvalidateChangedContainers()
    {
        HtmlSourceSpan span = CreateSpan(0, 30);
        HtmlDocument document = CreateDocument(span);
        HtmlNode oldParent = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "old", "old", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode newParent = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "new", "new", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode child = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "child", "child", "",
            Array.Empty<HtmlAttribute>(), span);
        oldParent.InitializeChildrenStorage(new[] { child });

        newParent.Append(child);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));
            Assert.That(oldParent.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(newParent.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(child.GetSourceSpan().IsKnown(), Is.True);
            Assert.That(oldParent.GetChildren(), Is.Empty);
            Assert.That(newParent.GetChildren(), Is.EqualTo(new[] { child }));
        });

        newParent.Append(child);
        newParent.InsertBefore(child, child);
        Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));

        child.Remove();
        child.Remove();
        Assert.Multiple(() =>
        {
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
            Assert.That(newParent.GetChildren(), Is.Empty);
            Assert.That(child.GetParent(), Is.Null);
        });
    }

    [Test]
    public void FailedTreePreflightPreservesVersionAndKnownSource()
    {
        HtmlSourceSpan span = CreateSpan(0, 10);
        HtmlDocument document = CreateDocument(span);
        HtmlNode html = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "html", "html", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode second = document.CreateElement("second");
        document.GetRoot().InitializeChildrenStorage(new[] { html });

        Assert.Throws<InvalidOperationException>(() =>
            document.GetRoot().Append(second));

        Assert.Multiple(() =>
        {
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
            Assert.That(document.GetRoot().GetSourceSpan().IsKnown(), Is.True);
            Assert.That(document.GetRoot().GetChildren(), Is.EqualTo(new[] { html }));
            Assert.That(second.GetParent(), Is.Null);
        });
    }

    [Test]
    public void FreezeTransitivelyRejectsEveryMutationWithoutChangingState()
    {
        HtmlSourceSpan span = CreateSpan(0, 20);
        HtmlDocument document = CreateDocument(span);
        HtmlNode parent = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "parent", "parent", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode child = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "child", "child", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode text = document.CreateTextStorage("text", span);
        HtmlNode detached = document.CreateElement("detached");
        parent.InitializeChildrenStorage(new[] { child, text });

        document.Freeze();
        document.Freeze();

        Assert.Throws<InvalidOperationException>(() => parent.Append(detached));
        Assert.Throws<InvalidOperationException>(() => parent.Empty());
        Assert.Throws<InvalidOperationException>(() => child.Remove());
        Assert.Throws<InvalidOperationException>(() => child.ReplaceWith(detached));
        Assert.Throws<InvalidOperationException>(() => child.SetAttribute("id", "x"));
        Assert.Throws<InvalidOperationException>(() => child.RemoveAttribute("missing"));
        Assert.Throws<InvalidOperationException>(() => child.AddClass("active"));
        Assert.Throws<InvalidOperationException>(() => child.RemoveClass("missing"));
        Assert.Throws<InvalidOperationException>(() => text.SetData("changed"));
        Assert.Throws<InvalidOperationException>(() => detached.Remove());

        Assert.Multiple(() =>
        {
            Assert.That(document.IsFrozen(), Is.True);
            Assert.That(document.IsFrozenStorage(), Is.True);
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
            Assert.That(parent.GetChildren(), Is.EqualTo(new[] { child, text }));
            Assert.That(child.GetAttributes(), Is.Empty);
            Assert.That(text.GetData(), Is.EqualTo("text"));
            Assert.That(parent.GetSourceSpan().IsKnown(), Is.True);
            Assert.That(child.GetSourceSpan().IsKnown(), Is.True);
            Assert.That(text.GetSourceSpan().IsKnown(), Is.True);
            Assert.That(detached.GetParent(), Is.Null);
        });
    }

    [Test]
    public void AttributeMutationValidatesElementAndNameContracts()
    {
        HtmlDocument document = CreateDocument(HtmlSourceSpan.CreateUnknown());
        HtmlNode element = document.CreateElement("div");
        HtmlNode text = document.CreateText("text");

        Assert.Throws<ArgumentNullException>(() => element.GetAttribute(null));
        Assert.Throws<ArgumentNullException>(() => element.SetAttribute("id", null));
        Assert.Throws<ArgumentException>(() => element.SetAttribute("", "value"));
        Assert.Throws<ArgumentException>(() => element.SetAttribute("bad name", "value"));
        Assert.Throws<InvalidOperationException>(() => text.SetAttribute("id", "value"));
        Assert.Throws<InvalidOperationException>(() => text.RemoveAttribute("id"));

        Assert.Multiple(() =>
        {
            Assert.That(text.GetAttribute("id"), Is.Null);
            Assert.That(text.HasAttribute("id"), Is.False);
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    static HtmlDocument CreateDocument(HtmlSourceSpan span)
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), span);
    }

    static HtmlSourceSpan CreateSpan(int start, int end)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(start, 1, start + 1),
            HtmlSourcePosition.Create(end, 1, end + 1));
    }
}
