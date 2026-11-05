
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeCloneAndContentTests
{
    [Test]
    public void ShallowAndDeepClonePreserveValuesWithoutSharingNodes()
    {
        HtmlSourceSpan span = CreateSpan(0, 30);
        HtmlAttribute attribute = HtmlAttribute.Create("id", "source", span);
        HtmlDocument document = CreateDocument(span);
        HtmlNode rootElement = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "main", "main", "",
            new[] { attribute }, span);
        HtmlNode text = document.CreateTextStorage("text", span);
        HtmlNode template = document.CreateTemplateStorage(
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode templateChild = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "inside", "inside", "",
            Array.Empty<HtmlAttribute>(), span);
        template.GetTemplateContent().InitializeChildrenStorage(
            new[] { templateChild });
        rootElement.InitializeChildrenStorage(new[] { text, template });
        document.GetRoot().InitializeChildrenStorage(new[] { rootElement });

        HtmlNode shallow = rootElement.Clone(false);
        HtmlNode deepDocument = document.GetRoot().Clone(true);
        HtmlNode deep = deepDocument.GetFirstChild();
        HtmlNode deepText = deep.GetChildren()[0];
        HtmlNode deepTemplate = deep.GetChildren()[1];
        HtmlNode deepTemplateChild = deepTemplate.GetTemplateContent()
            .GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.That(shallow.GetParent(), Is.Null);
            Assert.That(shallow.GetChildren(), Is.Empty);
            Assert.That(shallow.GetOwnerDocument(), Is.SameAs(document));
            Assert.That(shallow.GetAttributes().Select(value => value.GetValue()),
                Is.EqualTo(new[] { "source" }));
            Assert.That(shallow.GetAttributes()[0], Is.Not.SameAs(attribute));
            Assert.That(shallow.GetAttributes()[0].GetSourceSpan(), Is.SameAs(span));
            Assert.That(deepDocument.GetKind(), Is.EqualTo(HtmlNodeKind.Document));
            Assert.That(deepDocument.GetOwnerDocument(), Is.Null);
            Assert.That(deepDocument, Is.Not.SameAs(document.GetRoot()));
            Assert.That(deep, Is.Not.SameAs(rootElement));
            Assert.That(deep.GetParent(), Is.SameAs(deepDocument));
            Assert.That(deep.GetOwnerDocument(), Is.Not.SameAs(document));
            Assert.That(deepText.GetOwnerDocument(),
                Is.SameAs(deep.GetOwnerDocument()));
            Assert.That(deepText.GetData(), Is.EqualTo("text"));
            Assert.That(deepText, Is.Not.SameAs(text));
            Assert.That(deepTemplate.GetChildren(), Is.Empty);
            Assert.That(deepTemplate.GetTemplateContent(), Is.Not.Null);
            Assert.That(deepTemplate.GetTemplateContent(),
                Is.Not.SameAs(template.GetTemplateContent()));
            Assert.That(deepTemplateChild.GetName(), Is.EqualTo("inside"));
            Assert.That(deepTemplateChild, Is.Not.SameAs(templateChild));
            Assert.That(deepTemplateChild.GetParent(),
                Is.SameAs(deepTemplate.GetTemplateContent()));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });

        deepText.SetData("changed");
        deep.SetAttribute("id", "clone");

        Assert.Multiple(() =>
        {
            Assert.That(text.GetData(), Is.EqualTo("text"));
            Assert.That(rootElement.GetAttribute("id"), Is.EqualTo("source"));
            Assert.That(deepText.GetData(), Is.EqualTo("changed"));
            Assert.That(deep.GetAttribute("id"), Is.EqualTo("clone"));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
            Assert.That(deep.GetOwnerDocument().GetMutationVersionStorage(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void ImportCreatesOwnedCrossDocumentCopies()
    {
        HtmlSourceSpan span = CreateSpan(2, 20);
        HtmlDocument sourceDocument = CreateDocument(span);
        HtmlDocument targetDocument = CreateDocument(span);
        HtmlAttribute attribute = HtmlAttribute.CreateNamespaced("xlink:href",
            "href", "xlink", "http://www.w3.org/1999/xlink", "target", span);
        HtmlNode source = sourceDocument.CreateElementStorage(HtmlNamespace.Svg,
            "http://www.w3.org/2000/svg", "svg:a", "a", "svg",
            new[] { attribute }, span);
        HtmlNode child = sourceDocument.CreateTextStorage("label", span);
        source.InitializeChildrenStorage(new[] { child });

        HtmlNode shallow = targetDocument.Import(source, false);
        HtmlNode deep = targetDocument.Import(source, true);

        Assert.Multiple(() =>
        {
            Assert.That(shallow.GetOwnerDocument(), Is.SameAs(targetDocument));
            Assert.That(shallow.GetChildren(), Is.Empty);
            Assert.That(deep.GetOwnerDocument(), Is.SameAs(targetDocument));
            Assert.That(deep.GetFirstChild().GetOwnerDocument(),
                Is.SameAs(targetDocument));
            Assert.That(deep.GetFirstChild().GetData(), Is.EqualTo("label"));
            Assert.That(deep.GetFirstChild(), Is.Not.SameAs(child));
            Assert.That(deep.GetNamespaceUri(),
                Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(deep.GetAttributes()[0], Is.Not.SameAs(attribute));
            Assert.That(deep.GetAttributes()[0].GetNamespaceUri(),
                Is.EqualTo("http://www.w3.org/1999/xlink"));
            Assert.That(deep.GetSourceSpan(), Is.SameAs(span));
            Assert.That(targetDocument.GetMutationVersionStorage(), Is.Zero);
            Assert.That(source.GetFirstChild(), Is.SameAs(child));
        });

        targetDocument.GetRoot().Append(deep);

        Assert.Multiple(() =>
        {
            Assert.That(targetDocument.GetDocumentElement(), Is.SameAs(deep));
            Assert.That(targetDocument.GetMutationVersionStorage(), Is.EqualTo(1));
            Assert.That(source.GetParent(), Is.Null);
        });

        Assert.Throws<ArgumentNullException>(() =>
            targetDocument.Import(null, true));
        Assert.Throws<InvalidOperationException>(() =>
            targetDocument.Import(sourceDocument.GetRoot(), true));
    }

    [Test]
    public void ClonePreservesFragmentCommentAndDoctypeState()
    {
        HtmlSourceSpan span = CreateSpan(0, 12);
        HtmlDocument document = CreateDocument(span);
        HtmlNode fragment = document.CreateFragmentStorage(span);
        HtmlNode comment = document.CreateCommentStorage("note", span);
        HtmlNode text = document.CreateTextStorage("text", span);
        fragment.InitializeChildrenStorage(new[] { comment, text });
        HtmlNode doctype = document.CreateDocumentTypeStorage(true, "html",
            true, "public", true, "system", true, span);

        HtmlNode shallowFragment = fragment.Clone(false);
        HtmlNode deepFragment = fragment.Clone(true);
        HtmlNode clonedDoctype = doctype.Clone(true);

        Assert.Multiple(() =>
        {
            Assert.That(shallowFragment.GetKind(),
                Is.EqualTo(HtmlNodeKind.Fragment));
            Assert.That(shallowFragment.GetChildren(), Is.Empty);
            Assert.That(deepFragment.GetChildren(), Has.Length.EqualTo(2));
            Assert.That(deepFragment.GetChildren()[0].GetKind(),
                Is.EqualTo(HtmlNodeKind.Comment));
            Assert.That(deepFragment.GetChildren()[0].GetData(),
                Is.EqualTo("note"));
            Assert.That(deepFragment.GetChildren()[1].GetData(),
                Is.EqualTo("text"));
            Assert.That(deepFragment.GetChildren()[0], Is.Not.SameAs(comment));
            Assert.That(clonedDoctype.GetKind(),
                Is.EqualTo(HtmlNodeKind.DocumentType));
            Assert.That(clonedDoctype.GetName(), Is.EqualTo("html"));
            Assert.That(clonedDoctype.HasPublicIdentifierStorage(), Is.True);
            Assert.That(clonedDoctype.GetPublicIdentifierStorage(),
                Is.EqualTo("public"));
            Assert.That(clonedDoctype.HasSystemIdentifierStorage(), Is.True);
            Assert.That(clonedDoctype.GetSystemIdentifierStorage(),
                Is.EqualTo("system"));
            Assert.That(clonedDoctype.IsForceQuirksStorage(), Is.True);
            Assert.That(clonedDoctype.GetSourceSpan(), Is.SameAs(span));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void FragmentInsertionConsumesChildrenInOrder()
    {
        HtmlSourceSpan span = CreateSpan(0, 20);
        HtmlDocument document = CreateDocument(span);
        HtmlNode parent = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "parent", "parent", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode before = document.CreateElement("before");
        HtmlNode after = document.CreateElement("after");
        HtmlNode fragment = document.CreateFragmentStorage(span);
        HtmlNode first = document.CreateText("first");
        HtmlNode second = document.CreateElement("second");
        parent.InitializeChildrenStorage(new[] { before, after });
        fragment.InitializeChildrenStorage(new[] { first, second });

        parent.InsertBefore(fragment, after);

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[]
            {
                before, first, second, after
            }));
            Assert.That(fragment.GetChildren(), Is.Empty);
            Assert.That(fragment.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(parent.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(first.GetParent(), Is.SameAs(parent));
            Assert.That(first.GetPreviousSibling(), Is.SameAs(before));
            Assert.That(first.GetNextSibling(), Is.SameAs(second));
            Assert.That(second.GetNextSibling(), Is.SameAs(after));
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));
        });

        HtmlNode empty = document.CreateFragment();
        parent.Append(empty);
        Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));
    }

    [Test]
    public void ReplaceWithFragmentSplicesChildrenAndConsumesEmptyFragments()
    {
        HtmlDocument document = CreateDocument(HtmlSourceSpan.CreateUnknown());
        HtmlNode parent = document.CreateElement("parent");
        HtmlNode before = document.CreateElement("before");
        HtmlNode replaced = document.CreateElement("replaced");
        HtmlNode after = document.CreateElement("after");
        HtmlNode fragment = document.CreateFragment();
        HtmlNode first = document.CreateElement("first");
        HtmlNode second = document.CreateText("second");
        parent.InitializeChildrenStorage(new[] { before, replaced, after });
        fragment.InitializeChildrenStorage(new[] { first, second });

        replaced.ReplaceWith(fragment);

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[]
            {
                before, first, second, after
            }));
            Assert.That(replaced.GetParent(), Is.Null);
            Assert.That(fragment.GetChildren(), Is.Empty);
            Assert.That(first.GetParent(), Is.SameAs(parent));
            Assert.That(second.GetParent(), Is.SameAs(parent));
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));
        });

        HtmlNode empty = document.CreateFragment();
        first.ReplaceWith(empty);

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[]
            {
                before, second, after
            }));
            Assert.That(first.GetParent(), Is.Null);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
        });
    }

    [Test]
    public void DocumentFragmentConstraintsAreAtomic()
    {
        HtmlSourceSpan span = CreateSpan(0, 20);
        HtmlDocument invalidDocument = CreateDocument(span);
        HtmlNode invalidFragment = invalidDocument.CreateFragmentStorage(span);
        HtmlNode first = invalidDocument.CreateElement("first");
        HtmlNode second = invalidDocument.CreateElement("second");
        invalidFragment.InitializeChildrenStorage(new[] { first, second });

        Assert.Throws<InvalidOperationException>(() =>
            invalidDocument.GetRoot().Append(invalidFragment));

        Assert.Multiple(() =>
        {
            Assert.That(invalidDocument.GetRoot().GetChildren(), Is.Empty);
            Assert.That(invalidFragment.GetChildren(),
                Is.EqualTo(new[] { first, second }));
            Assert.That(first.GetParent(), Is.SameAs(invalidFragment));
            Assert.That(second.GetParent(), Is.SameAs(invalidFragment));
            Assert.That(invalidDocument.GetMutationVersionStorage(), Is.Zero);
            Assert.That(invalidDocument.GetRoot().GetSourceSpan().IsKnown(), Is.True);
            Assert.That(invalidFragment.GetSourceSpan().IsKnown(), Is.True);
        });

        HtmlDocument validDocument = CreateDocument(span);
        HtmlNode validFragment = validDocument.CreateFragmentStorage(span);
        HtmlNode comment = validDocument.CreateComment("before");
        HtmlNode html = validDocument.CreateElement("html");
        validFragment.InitializeChildrenStorage(new[] { comment, html });

        validDocument.GetRoot().Append(validFragment);

        Assert.Multiple(() =>
        {
            Assert.That(validDocument.GetRoot().GetChildren(),
                Is.EqualTo(new[] { comment, html }));
            Assert.That(validFragment.GetChildren(), Is.Empty);
            Assert.That(validDocument.GetDocumentElement(), Is.SameAs(html));
            Assert.That(validDocument.GetMutationVersionStorage(), Is.EqualTo(1));
        });
    }

    [Test]
    public void SetTextContentReplacesContainerChildrenAndCharacterData()
    {
        HtmlSourceSpan span = CreateSpan(0, 20);
        HtmlDocument document = CreateDocument(span);
        HtmlNode parent = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "parent", "parent", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode oldText = document.CreateTextStorage("old", span);
        HtmlNode oldElement = document.CreateElement("old-element");
        parent.InitializeChildrenStorage(new[] { oldText, oldElement });

        parent.SetTextContent("replacement");
        HtmlNode replacement = parent.GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Has.Length.EqualTo(1));
            Assert.That(replacement.GetKind(), Is.EqualTo(HtmlNodeKind.Text));
            Assert.That(replacement.GetData(), Is.EqualTo("replacement"));
            Assert.That(replacement.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(oldText.GetParent(), Is.Null);
            Assert.That(oldElement.GetParent(), Is.Null);
            Assert.That(parent.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));
        });

        parent.SetTextContent("replacement");
        Assert.Multiple(() =>
        {
            Assert.That(parent.GetFirstChild(), Is.Not.SameAs(replacement));
            Assert.That(replacement.GetParent(), Is.Null);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(2));
        });

        HtmlNode latest = parent.GetFirstChild();
        parent.SetTextContent("");
        parent.SetTextContent("");

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.Empty);
            Assert.That(latest.GetParent(), Is.Null);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(3));
        });

        HtmlNode comment = document.CreateCommentStorage("note", span);
        comment.SetTextContent("changed");
        Assert.That(comment.GetData(), Is.EqualTo("changed"));
        Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(4));

        HtmlNode doctype = CreateDoctype(document, span);
        doctype.SetTextContent("ignored");
        document.GetRoot().SetTextContent("ignored");
        Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(4));
    }

    [Test]
    public void TemplateTextContentTargetsItsContentFragment()
    {
        HtmlSourceSpan span = CreateSpan(0, 15);
        HtmlDocument document = CreateDocument(span);
        HtmlNode template = document.CreateTemplateStorage(
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode oldChild = document.CreateElement("old");
        template.GetTemplateContent().InitializeChildrenStorage(
            new[] { oldChild });

        template.SetTextContent("inside");

        Assert.Multiple(() =>
        {
            Assert.That(template.GetChildren(), Is.Empty);
            Assert.That(template.GetTemplateContent().GetChildren(), Has.Length.EqualTo(1));
            Assert.That(template.GetTemplateContent().GetFirstChild().GetData(),
                Is.EqualTo("inside"));
            Assert.That(oldChild.GetParent(), Is.Null);
            Assert.That(template.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));
        });
    }

    [Test]
    public void NormalizeRemovesEmptyTextAndMergesAdjacentTextRecursively()
    {
        HtmlSourceSpan span = CreateSpan(0, 30);
        HtmlDocument document = CreateDocument(span);
        HtmlNode parent = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "parent", "parent", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode empty = document.CreateTextStorage("", span);
        HtmlNode first = document.CreateTextStorage("a", span);
        HtmlNode between = document.CreateTextStorage("", span);
        HtmlNode second = document.CreateTextStorage("b", span);
        HtmlNode comment = document.CreateCommentStorage("break", span);
        HtmlNode nested = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "nested", "nested", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode nestedFirst = document.CreateTextStorage("c", span);
        HtmlNode nestedSecond = document.CreateTextStorage("d", span);
        HtmlNode template = document.CreateTemplateStorage(
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode templateFirst = document.CreateTextStorage("e", span);
        HtmlNode templateSecond = document.CreateTextStorage("f", span);
        nested.InitializeChildrenStorage(new[] { nestedFirst, nestedSecond });
        template.GetTemplateContent().InitializeChildrenStorage(
            new[] { templateFirst, templateSecond });
        parent.InitializeChildrenStorage(new[]
        {
            empty, first, between, second, comment, nested, template
        });

        parent.Normalize();

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[]
            {
                first, comment, nested, template
            }));
            Assert.That(first.GetData(), Is.EqualTo("ab"));
            Assert.That(first.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(empty.GetParent(), Is.Null);
            Assert.That(between.GetParent(), Is.Null);
            Assert.That(second.GetParent(), Is.Null);
            Assert.That(nested.GetChildren(), Is.EqualTo(new[] { nestedFirst }));
            Assert.That(nestedFirst.GetData(), Is.EqualTo("cd"));
            Assert.That(nestedSecond.GetParent(), Is.Null);
            Assert.That(template.GetChildren(), Is.Empty);
            Assert.That(template.GetTemplateContent().GetChildren(),
                Is.EqualTo(new[] { templateFirst }));
            Assert.That(templateFirst.GetData(), Is.EqualTo("ef"));
            Assert.That(templateSecond.GetParent(), Is.Null);
            Assert.That(parent.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(nested.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(template.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));
        });

        parent.Normalize();
        Assert.That(document.GetMutationVersionStorage(), Is.EqualTo(1));

        document.Freeze();
        Assert.Throws<InvalidOperationException>(() => parent.Normalize());
    }

    static HtmlDocument CreateDocument(HtmlSourceSpan span)
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), span);
    }

    static HtmlNode CreateDoctype(HtmlDocument document, HtmlSourceSpan span)
    {
        return document.CreateDocumentTypeStorage(true, "html", false, "",
            false, "", false, span);
    }

    static HtmlSourceSpan CreateSpan(int start, int end)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(start, 1, start + 1),
            HtmlSourcePosition.Create(end, 1, end + 1));
    }
}
