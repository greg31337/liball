using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownMalformedInputRecoveryTests
{
    [Test]
    public void Parser_RecoversUnclosedFencedCodeBlockAtEndOfInput()
    {
        MarkdownDocument document = MarkdownParser.Parse("```csharp\nConsole.WriteLine(1);");

        Assert.That(document.HasErrors(), Is.False);
        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(document.Root.GetChild(0).GetAttributeValue("info"), Is.EqualTo("csharp"));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("Console.WriteLine(1);"));
        Assert.That(Markdown.ToHtml(document), Is.EqualTo(
            "<pre><code class=\"language-csharp\">Console.WriteLine(1);\n" +
            "</code></pre>\n"));
    }

    [Test]
    public void Parser_RecoversInvalidLinkReferenceDefinitionAsParagraphText()
    {
        string source = "[foo]: /url \"unterminated\n[foo]";

        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(document.HasErrors(), Is.False);
        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(paragraph.Literal, Is.EqualTo(source));
        Assert.That(ContainsNodeKind(document.Root, MarkdownNodeKind.LinkReferenceDefinition), Is.False);
        Assert.That(ContainsNodeKind(document.Root, MarkdownNodeKind.Link), Is.False);
    }

    [Test]
    public void Parser_RecoversMalformedInlineMixWithoutDiagnostics()
    {
        string source = "a [broken](url ![image](url <span &unknown; `code";
        string html = "";

        Assert.That(() => html = Markdown.ToHtml(source), Throws.Nothing);

        MarkdownDocument document = MarkdownParser.Parse(source);
        Assert.That(document.HasErrors(), Is.False);
        Assert.That(ContainsNodeKind(document.Root, MarkdownNodeKind.Link), Is.False);
        Assert.That(ContainsNodeKind(document.Root, MarkdownNodeKind.Image), Is.False);
        Assert.That(ContainsNodeKind(document.Root, MarkdownNodeKind.HtmlInline), Is.False);
        Assert.That(html, Does.Contain("&lt;span"));
        Assert.That(html, Does.Not.Contain("<span"));
    }

    [Test]
    public void Parser_RecoversImbalancedDelimiterRunsWithoutThrowing()
    {
        string source = "[[[[[[[********____!!!!";
        string html = "";
        string markdown = "";
        string plainText = "";

        Assert.That(() => html = Markdown.ToHtml(source), Throws.Nothing);
        Assert.That(() => markdown = Markdown.ToMarkdown(source), Throws.Nothing);
        Assert.That(() => plainText = Markdown.ToPlainText(source), Throws.Nothing);

        MarkdownDocument document = MarkdownParser.Parse(source);
        Assert.That(document.HasErrors(), Is.False);
        Assert.That(ContainsNodeKind(document.Root, MarkdownNodeKind.Link), Is.False);
        Assert.That(html, Does.Contain("[[[[[[["));
        Assert.That(markdown, Does.Contain("\\[\\[\\[\\[\\[\\[\\["));
        Assert.That(plainText, Does.Contain("[[[[[[["));
    }

    public static bool ContainsNodeKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return true;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            if (ContainsNodeKind(node.GetChild(index), kind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
