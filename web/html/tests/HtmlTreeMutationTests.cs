
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeMutationTests
{
    [Test]
    public void AppendPrependAndInsertReorderExistingChildren()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("main");
        HtmlNode first = document.CreateElement("first");
        HtmlNode second = document.CreateElement("second");
        HtmlNode third = document.CreateElement("third");

        parent.Append(first);
        parent.Append(second);
        parent.Append(third);
        parent.Append(first);
        Assert.That(parent.GetChildren(), Is.EqualTo(new[]
        {
            second, third, first
        }));

        parent.Prepend(third);
        Assert.That(parent.GetChildren(), Is.EqualTo(new[]
        {
            third, second, first
        }));

        parent.InsertAfter(third, second);
        parent.InsertBefore(first, second);
        parent.InsertBefore(second, second);

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[]
            {
                first, second, third
            }));
            Assert.That(parent.GetFirstChild(), Is.SameAs(first));
            Assert.That(parent.GetLastChild(), Is.SameAs(third));
            Assert.That(first.GetPreviousSibling(), Is.Null);
            Assert.That(first.GetNextSibling(), Is.SameAs(second));
            Assert.That(second.GetPreviousSibling(), Is.SameAs(first));
            Assert.That(second.GetNextSibling(), Is.SameAs(third));
            Assert.That(third.GetPreviousSibling(), Is.SameAs(second));
            Assert.That(third.GetNextSibling(), Is.Null);
            Assert.That(new[] { first, second, third }
                .All(value => value.GetParent() == parent), Is.True);
        });
    }

    [Test]
    public void InsertionReparentsAcrossContainersAndRepairsBothLists()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode left = document.CreateElement("left");
        HtmlNode right = document.CreateElement("right");
        HtmlNode leftFirst = document.CreateElement("left-first");
        HtmlNode moved = document.CreateElement("moved");
        HtmlNode rightFirst = document.CreateElement("right-first");

        left.Append(leftFirst);
        left.Append(moved);
        right.Append(rightFirst);
        right.InsertBefore(moved, rightFirst);

        Assert.Multiple(() =>
        {
            Assert.That(left.GetChildren(), Is.EqualTo(new[] { leftFirst }));
            Assert.That(leftFirst.GetPreviousSibling(), Is.Null);
            Assert.That(leftFirst.GetNextSibling(), Is.Null);
            Assert.That(right.GetChildren(), Is.EqualTo(new[]
            {
                moved, rightFirst
            }));
            Assert.That(moved.GetParent(), Is.SameAs(right));
            Assert.That(moved.GetPreviousSibling(), Is.Null);
            Assert.That(moved.GetNextSibling(), Is.SameAs(rightFirst));
            Assert.That(rightFirst.GetPreviousSibling(), Is.SameAs(moved));
            Assert.That(moved.GetOwnerDocument(), Is.SameAs(document));
        });
    }

    [Test]
    public void ReplaceWithHandlesDetachedSiblingAndDescendantReplacements()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("parent");
        HtmlNode before = document.CreateElement("before");
        HtmlNode wrapper = document.CreateElement("wrapper");
        HtmlNode descendant = document.CreateElement("descendant");
        HtmlNode after = document.CreateElement("after");
        wrapper.Append(descendant);
        parent.Append(before);
        parent.Append(wrapper);
        parent.Append(after);

        wrapper.ReplaceWith(descendant);
        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[]
            {
                before, descendant, after
            }));
            Assert.That(wrapper.GetParent(), Is.Null);
            Assert.That(wrapper.GetChildren(), Is.Empty);
            Assert.That(descendant.GetParent(), Is.SameAs(parent));
        });

        HtmlNode detached = document.CreateElement("detached");
        descendant.ReplaceWith(detached);
        after.ReplaceWith(before);

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[]
            {
                detached, before
            }));
            Assert.That(descendant.GetParent(), Is.Null);
            Assert.That(after.GetParent(), Is.Null);
            Assert.That(detached.GetNextSibling(), Is.SameAs(before));
            Assert.That(before.GetPreviousSibling(), Is.SameAs(detached));
        });
    }

    [Test]
    public void RemoveAndEmptyDetachNodesAndClearLinks()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("parent");
        HtmlNode first = document.CreateElement("first");
        HtmlNode middle = document.CreateElement("middle");
        HtmlNode last = document.CreateElement("last");
        parent.Append(first);
        parent.Append(middle);
        parent.Append(last);

        middle.Remove();
        middle.Remove();

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[] { first, last }));
            Assert.That(first.GetNextSibling(), Is.SameAs(last));
            Assert.That(last.GetPreviousSibling(), Is.SameAs(first));
            Assert.That(middle.GetParent(), Is.Null);
            Assert.That(middle.GetPreviousSibling(), Is.Null);
            Assert.That(middle.GetNextSibling(), Is.Null);
        });

        parent.Empty();

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.Empty);
            Assert.That(parent.GetFirstChild(), Is.Null);
            Assert.That(parent.GetLastChild(), Is.Null);
            Assert.That(first.GetParent(), Is.Null);
            Assert.That(first.GetNextSibling(), Is.Null);
            Assert.That(last.GetParent(), Is.Null);
            Assert.That(last.GetPreviousSibling(), Is.Null);
        });
    }

    [Test]
    public void UnwrapPromotesChildrenAtTheWrappedNodesPosition()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("parent");
        HtmlNode before = document.CreateElement("before");
        HtmlNode wrapper = document.CreateElement("wrapper");
        HtmlNode first = document.CreateText("first");
        HtmlNode second = document.CreateElement("second");
        HtmlNode after = document.CreateElement("after");
        wrapper.Append(first);
        wrapper.Append(second);
        parent.Append(before);
        parent.Append(wrapper);
        parent.Append(after);

        wrapper.Unwrap();

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[]
            {
                before, first, second, after
            }));
            Assert.That(wrapper.GetParent(), Is.Null);
            Assert.That(wrapper.GetChildren(), Is.Empty);
            Assert.That(first.GetParent(), Is.SameAs(parent));
            Assert.That(first.GetPreviousSibling(), Is.SameAs(before));
            Assert.That(first.GetNextSibling(), Is.SameAs(second));
            Assert.That(second.GetNextSibling(), Is.SameAs(after));
        });
    }

    [Test]
    public void TemplateMutationsUseItsContentFragment()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("parent");
        HtmlNode template = document.CreateElement("template");
        HtmlNode first = document.CreateElement("first");
        HtmlNode second = document.CreateText("second");
        parent.Append(template);

        template.Append(second);
        template.Prepend(first);
        Assert.Throws<InvalidOperationException>(() => template.Append(template));

        Assert.Multiple(() =>
        {
            Assert.That(template.GetChildren(), Is.Empty);
            Assert.That(template.GetFirstChild(), Is.Null);
            Assert.That(template.GetTemplateContent().GetChildren(),
                Is.EqualTo(new[] { first, second }));
            Assert.That(first.GetParent(),
                Is.SameAs(template.GetTemplateContent()));
        });

        template.Unwrap();

        Assert.Multiple(() =>
        {
            Assert.That(parent.GetChildren(), Is.EqualTo(new[] { first, second }));
            Assert.That(template.GetParent(), Is.Null);
            Assert.That(template.GetTemplateContent().GetChildren(), Is.Empty);
            Assert.That(first.GetParent(), Is.SameAs(parent));
            Assert.That(second.GetParent(), Is.SameAs(parent));
        });
    }

    [Test]
    public void CycleOwnershipAndReferenceFailuresAreAtomic()
    {
        HtmlDocument document = CreateDocument();
        HtmlDocument foreignDocument = CreateDocument();
        HtmlNode ancestor = document.CreateElement("ancestor");
        HtmlNode parent = document.CreateElement("parent");
        HtmlNode child = document.CreateElement("child");
        HtmlNode leaf = document.CreateText("leaf");
        HtmlNode foreignParent = foreignDocument.CreateElement("foreign-parent");
        HtmlNode foreign = foreignDocument.CreateElement("foreign");
        HtmlNode unrelated = document.CreateElement("unrelated");
        ancestor.Append(parent);
        parent.Append(child);
        foreignParent.Append(foreign);

        Assert.Throws<InvalidOperationException>(() => child.Append(ancestor));
        Assert.Throws<InvalidOperationException>(() => parent.Append(parent));
        Assert.Throws<ArgumentException>(() => parent.Append(foreign));
        Assert.Throws<ArgumentException>(() => parent.InsertBefore(unrelated,
            ancestor));
        Assert.Throws<ArgumentNullException>(() => parent.Append(null));
        Assert.Throws<ArgumentNullException>(() => parent.InsertAfter(child, null));
        Assert.Throws<InvalidOperationException>(() => leaf.Append(
            document.CreateText("nested")));

        Assert.Multiple(() =>
        {
            Assert.That(ancestor.GetChildren(), Is.EqualTo(new[] { parent }));
            Assert.That(parent.GetChildren(), Is.EqualTo(new[] { child }));
            Assert.That(child.GetParent(), Is.SameAs(parent));
            Assert.That(ancestor.GetParent(), Is.Null);
            Assert.That(foreign.GetParent(), Is.SameAs(foreignParent));
            Assert.That(foreignParent.GetChildren(), Is.EqualTo(new[] { foreign }));
            Assert.That(unrelated.GetParent(), Is.Null);
        });
    }

    [Test]
    public void DocumentConstraintsAreCheckedBeforeAnyMutation()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = document.GetRoot();
        HtmlNode before = document.CreateComment("before");
        HtmlNode doctype = CreateDoctype(document);
        HtmlNode between = document.CreateComment("between");
        HtmlNode html = document.CreateElement("html");
        root.Append(before);
        root.Append(doctype);
        root.Append(between);
        root.Append(html);
        HtmlNode[] validChildren = root.GetChildren();

        Assert.Throws<InvalidOperationException>(() => root.Append(
            document.CreateElement("second")));
        Assert.Throws<InvalidOperationException>(() => root.Append(
            CreateDoctype(document)));
        Assert.Throws<InvalidOperationException>(() => root.Append(
            document.CreateText("text")));
        root.Append(document.CreateFragment());
        Assert.Throws<InvalidOperationException>(() => root.Append(doctype));
        Assert.Throws<InvalidOperationException>(() => root.Prepend(html));

        Assert.That(root.GetChildren(), Is.EqualTo(validChildren));
        Assert.That(validChildren.All(value => value.GetParent() == root), Is.True);

        HtmlNode replacement = document.CreateElement("replacement");
        html.ReplaceWith(replacement);
        HtmlNode head = document.CreateElement("head");
        HtmlNode body = document.CreateElement("body");
        replacement.Append(head);
        replacement.Append(body);
        HtmlNode[] beforeFailedUnwrap = root.GetChildren();

        Assert.Throws<InvalidOperationException>(() => replacement.Unwrap());
        Assert.Multiple(() =>
        {
            Assert.That(root.GetChildren(), Is.EqualTo(beforeFailedUnwrap));
            Assert.That(replacement.GetParent(), Is.SameAs(root));
            Assert.That(replacement.GetChildren(), Is.EqualTo(new[] { head, body }));
            Assert.That(head.GetParent(), Is.SameAs(replacement));
            Assert.That(body.GetParent(), Is.SameAs(replacement));
        });
    }

    [Test]
    public void StorageInitializationAlsoEnforcesDocumentConstraintsAtomically()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = document.GetRoot();
        HtmlNode html = document.CreateElement("html");
        HtmlNode doctype = CreateDoctype(document);

        Assert.Throws<InvalidOperationException>(() =>
            root.InitializeChildrenStorage(new[] { html, doctype }));

        Assert.Multiple(() =>
        {
            Assert.That(root.GetChildren(), Is.Empty);
            Assert.That(html.GetParent(), Is.Null);
            Assert.That(doctype.GetParent(), Is.Null);
        });
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlNode CreateDoctype(HtmlDocument document)
    {
        return document.CreateDocumentTypeStorage(true, "html", false, "",
            false, "", false, HtmlSourceSpan.CreateUnknown());
    }
}
