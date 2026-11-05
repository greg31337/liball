using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownTypedNodeTests
{
    [Test]
    public void Document_CreatesDocumentRoot()
    {
        MarkdownDocument document = new MarkdownDocument();

        Assert.That(document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(document.GetRoot(), Is.SameAs(document.Root));
    }

    [Test]
    public void Document_CanWrapExistingDocumentNode()
    {
        MarkdownNode root = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownDocument document = MarkdownDocument.Create(root);

        Assert.That(document.Root, Is.SameAs(root));
    }

    [Test]
    public void Document_RejectsNonDocumentRoot()
    {
        Assert.That(() => MarkdownDocument.Create(MarkdownNode.Create(MarkdownNodeKind.Paragraph)), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownDocument.Create((MarkdownNode)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownDocument.Create((MarkdownSourceSpan)null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void BlockNode_CreatesBlockKinds()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(1, 1, 2, 5, 1, 6);
        MarkdownBlockNode block = MarkdownBlockNode.Create(MarkdownNodeKind.Heading, span);

        span.Start.Offset = 99;

        Assert.That(block.Node.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(block.Node.Span.Start.Offset, Is.EqualTo(1));
        Assert.That(block.GetNode(), Is.SameAs(block.Node));
    }

    [Test]
    public void BlockNode_CanWrapExistingBlockNode()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownBlockNode block = MarkdownBlockNode.Create(node);

        Assert.That(block.Node, Is.SameAs(node));
    }

    [Test]
    public void BlockNode_RejectsNonBlockKinds()
    {
        Assert.That(() => MarkdownBlockNode.Create(MarkdownNodeKind.Text), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownBlockNode.Create((MarkdownNode)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownBlockNode.Create(MarkdownNodeKind.Paragraph, null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void InlineNode_CreatesInlineKindsAndLiteralText()
    {
        MarkdownInlineNode inline = MarkdownInlineNode.Create(MarkdownNodeKind.Text, "hello");

        Assert.That(inline.Node.Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(inline.Node.Literal, Is.EqualTo("hello"));
        Assert.That(inline.GetNode(), Is.SameAs(inline.Node));
    }

    [Test]
    public void InlineNode_CanWrapExistingInlineNode()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.CodeSpan);
        MarkdownInlineNode inline = MarkdownInlineNode.Create(node);

        Assert.That(inline.Node, Is.SameAs(node));
    }

    [Test]
    public void InlineNode_RejectsNonInlineKinds()
    {
        Assert.That(() => MarkdownInlineNode.Create(MarkdownNodeKind.Paragraph), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownInlineNode.Create((MarkdownNode)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineNode.Create(MarkdownNodeKind.Text, (string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineNode.Create(MarkdownNodeKind.Text, (MarkdownSourceSpan)null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ExtensionNode_CreatesExtensionKinds()
    {
        MarkdownExtensionNode extension = MarkdownExtensionNode.Create("gfm", MarkdownNodeKind.Table);

        Assert.That(extension.ExtensionName, Is.EqualTo("gfm"));
        Assert.That(extension.Node.Kind, Is.EqualTo(MarkdownNodeKind.Table));
        Assert.That(extension.GetNode(), Is.SameAs(extension.Node));
    }

    [Test]
    public void ExtensionNode_CanWrapExistingExtensionNode()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Strikethrough);
        MarkdownExtensionNode extension = MarkdownExtensionNode.Create("gfm", node);

        Assert.That(extension.Node, Is.SameAs(node));
        Assert.That(extension.ExtensionName, Is.EqualTo("gfm"));
    }

    [Test]
    public void ExtensionNode_RejectsNonExtensionKinds()
    {
        Assert.That(() => MarkdownExtensionNode.Create("core", MarkdownNodeKind.Paragraph), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownExtensionNode.Create(null!, MarkdownNodeKind.Table), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownExtensionNode.Create("gfm", null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void NodeKindFacts_ClassifyKinds()
    {
        Assert.That(MarkdownNodeKindFacts.IsDocumentKind(MarkdownNodeKind.Document), Is.True);
        Assert.That(MarkdownNodeKindFacts.IsBlockKind(MarkdownNodeKind.Paragraph), Is.True);
        Assert.That(MarkdownNodeKindFacts.IsBlockKind(MarkdownNodeKind.Text), Is.False);
        Assert.That(MarkdownNodeKindFacts.IsInlineKind(MarkdownNodeKind.Text), Is.True);
        Assert.That(MarkdownNodeKindFacts.IsInlineKind(MarkdownNodeKind.Paragraph), Is.False);
        Assert.That(MarkdownNodeKindFacts.IsExtensionKind(MarkdownNodeKind.Table), Is.True);
        Assert.That(MarkdownNodeKindFacts.IsExtensionKind(MarkdownNodeKind.Paragraph), Is.False);
    }
}
