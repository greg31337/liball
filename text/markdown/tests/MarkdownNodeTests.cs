using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownNodeTests
{
    [Test]
    public void Node_DefaultsToUnknownWithEmptyStorage()
    {
        MarkdownNode node = new MarkdownNode();

        Assert.That(node.Kind, Is.EqualTo(MarkdownNodeKind.Unknown));
        Assert.That(node.Literal, Is.EqualTo(""));
        Assert.That(node.Parent, Is.Null);
        Assert.That(node.HasParent(), Is.False);
        Assert.That(node.HasChildren(), Is.False);
        Assert.That(node.HasAttributes(), Is.False);
        Assert.That(node.GetChildCount(), Is.EqualTo(0));
        Assert.That(node.GetAttributeCount(), Is.EqualTo(0));
        Assert.That(node.Span.IsEmpty(), Is.True);
    }

    [Test]
    public void Node_CanBeCreatedWithKind()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        Assert.That(node.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(node.GetChildCount(), Is.EqualTo(0));
    }

    [Test]
    public void Node_CopiesSpanFromConstructor()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(2, 1, 3, 8, 1, 9);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text, span);

        span.Start.Offset = 99;
        span.End.Offset = 100;

        Assert.That(node.Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(node.Span.Start.Offset, Is.EqualTo(2));
        Assert.That(node.Span.End.Offset, Is.EqualTo(8));
    }

    [Test]
    public void SetSourceSpan_CopiesSpan()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(4, 2, 1, 9, 2, 6);
        MarkdownNode node = new MarkdownNode();

        node.SetSourceSpan(span);
        span.Start.Offset = 99;

        Assert.That(node.Span.Start.Offset, Is.EqualTo(4));
        Assert.That(node.Span.End.Offset, Is.EqualTo(9));
    }

    [Test]
    public void SetLiteral_StoresText()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);

        node.SetLiteral("hello");

        Assert.That(node.Literal, Is.EqualTo("hello"));
    }

    [Test]
    public void ChildrenField_StoresChildNodesAndGetChildrenReturnsCopy()
    {
        MarkdownNode parent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode child = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        parent.Children.Add(child);
        MarkdownNode[] children = parent.GetChildren();
        children[0] = MarkdownNode.Create(MarkdownNodeKind.Text);

        Assert.That(parent.HasChildren(), Is.True);
        Assert.That(parent.GetChildCount(), Is.EqualTo(1));
        Assert.That(parent.GetChild(0), Is.SameAs(child));
    }

    [Test]
    public void ParentField_CanStoreParentReference()
    {
        MarkdownNode parent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode child = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        child.Parent = parent;

        Assert.That(child.HasParent(), Is.True);
        Assert.That(child.Parent, Is.SameAs(parent));
    }

    [Test]
    public void Node_RejectsInvalidArguments()
    {
        MarkdownNode node = new MarkdownNode();

        Assert.That(() => MarkdownNode.Create(MarkdownNodeKind.Text, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.SetSourceSpan(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.SetLiteral(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.GetChild(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => node.GetChild(0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
