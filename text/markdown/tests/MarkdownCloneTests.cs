using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownCloneTests
{
    [Test]
    public void CloneNode_DeepCopiesNodeTree()
    {
        MarkdownNode paragraph = CreateParagraphTree();
        MarkdownNode clone = paragraph.CloneNode();

        paragraph.SetLiteral("changed");
        paragraph.SetAttribute("data", "changed");
        paragraph.Span.Start.Offset = 99;
        paragraph.GetChild(0).SetLiteral("changed child");

        Assert.That(clone, Is.Not.SameAs(paragraph));
        Assert.That(clone.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(clone.Literal, Is.EqualTo("paragraph"));
        Assert.That(clone.GetAttributeValue("data"), Is.EqualTo("value"));
        Assert.That(clone.Span.Start.Offset, Is.EqualTo(1));
        Assert.That(clone.Parent, Is.Null);
        Assert.That(clone.GetChildCount(), Is.EqualTo(2));
        Assert.That(clone.GetChild(0), Is.Not.SameAs(paragraph.GetChild(0)));
        Assert.That(clone.GetChild(0).Literal, Is.EqualTo("hello"));
        Assert.That(clone.GetChild(0).Parent, Is.SameAs(clone));
        Assert.That(clone.GetChild(1).GetChild(0).Parent, Is.SameAs(clone.GetChild(1)));
    }

    [Test]
    public void CloneShallow_CopiesNodeDataWithoutChildren()
    {
        MarkdownNode paragraph = CreateParagraphTree();

        MarkdownNode clone = paragraph.CloneShallow();

        Assert.That(clone.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(clone.Literal, Is.EqualTo("paragraph"));
        Assert.That(clone.GetAttributeValue("data"), Is.EqualTo("value"));
        Assert.That(clone.Span.Start.Offset, Is.EqualTo(1));
        Assert.That(clone.GetChildCount(), Is.EqualTo(0));
        Assert.That(clone.Parent, Is.Null);
    }

    [Test]
    public void CloneDocument_DeepCopiesRootAndDiagnostics()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = CreateParagraphTree();
        MarkdownSourceSpan diagnosticSpan = MarkdownSourceSpan.Create(1, 1, 2, 4, 1, 5);
        document.Root.AppendChild(paragraph);
        document.AddDiagnosticValues(MarkdownDiagnosticSeverity.Warning, "MD_TEST", "message", diagnosticSpan);

        MarkdownDocument clone = document.CloneDocument();

        document.Root.GetChild(0).SetAttribute("data", "changed");
        document.GetDiagnostics()[0].Span.Start.Offset = 99;

        Assert.That(clone, Is.Not.SameAs(document));
        Assert.That(clone.Root, Is.Not.SameAs(document.Root));
        Assert.That(clone.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(clone.Root.Parent, Is.Null);
        Assert.That(clone.Root.GetChild(0).Parent, Is.SameAs(clone.Root));
        Assert.That(clone.Root.GetChild(0).GetAttributeValue("data"), Is.EqualTo("value"));
        Assert.That(clone.HasDiagnostics(), Is.True);
        Assert.That(clone.GetDiagnostics()[0].Code, Is.EqualTo("MD_TEST"));
        Assert.That(clone.GetDiagnostics()[0].Span.Start.Offset, Is.EqualTo(1));
    }

    [Test]
    public void ClonedDocument_RendersSameMarkdownAsOriginal()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Title\n\nHello *world* and [link](/url)");

        MarkdownDocument clone = document.CloneDocument();

        Assert.That(MarkdownRenderer.Render(clone), Is.EqualTo(MarkdownRenderer.Render(document)));
        Assert.That(PlainTextRenderer.Render(clone), Is.EqualTo(PlainTextRenderer.Render(document)));
    }

    static MarkdownNode CreateParagraphTree()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(1, 1, 2, 12, 1, 13);
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph, span);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode emphasis = MarkdownNode.Create(MarkdownNodeKind.Emphasis);
        MarkdownNode emphasizedText = MarkdownNode.Create(MarkdownNodeKind.Text);

        paragraph.SetLiteral("paragraph");
        paragraph.SetAttribute("data", "value");
        text.SetLiteral("hello");
        emphasizedText.SetLiteral("world");
        emphasis.AppendChild(emphasizedText);
        paragraph.AppendChild(text);
        paragraph.AppendChild(emphasis);

        return paragraph;
    }
}
