using System.Text;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownFuzzLikeGeneratedInputTests
{
    [Test]
    public void GeneratedCommonMarkInputs_ParseAndRenderWithoutThrowing()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();

        RunGeneratedInputs(pipeline, 100, 64);
    }

    [Test]
    public void GeneratedGfmInputs_ParseAndRenderWithoutThrowing()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.GfmPreset();

        RunGeneratedInputs(pipeline, 60, 72);
    }

    [Test]
    public void GeneratedAdvancedInputs_ParseAndRenderWithoutThrowing()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();

        RunGeneratedInputs(pipeline, 60, 72);
    }

    public static void RunGeneratedInputs(MarkdownPipeline pipeline, int caseCount, int tokenCount)
    {
        int index = 0;
        while (index < caseCount)
        {
            string source = GenerateSource(index + 1, tokenCount);

            Assert.That(() => ExercisePublicPipeline(source, pipeline), Throws.Nothing, source);

            index = index + 1;
        }
    }

    public static void ExercisePublicPipeline(string source, MarkdownPipeline pipeline)
    {
        MarkdownDocument document = MarkdownParser.Parse(source, pipeline);

        Assert.That(document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(document.Root.Span.End.Offset, Is.GreaterThanOrEqualTo(0));
        Assert.That(HtmlRenderer.Render(document, pipeline), Is.Not.Null);
        Assert.That(MarkdownRenderer.Render(document, pipeline), Is.Not.Null);
        Assert.That(PlainTextRenderer.Render(document, pipeline), Is.Not.Null);
        Assert.That(DebugTreeRenderer.Render(document), Is.Not.Null);
        Assert.That(XmlAstRenderer.Render(document), Is.Not.Null);
    }

    public static string GenerateSource(int seed, int tokenCount)
    {
        string[] tokens = CreateTokenPool();
        StringBuilder builder = new StringBuilder();
        int state = seed;
        int index = 0;

        while (index < tokenCount)
        {
            state = NextState(state);
            builder.Append(tokens[PositiveModulo(state, tokens.Length)]);
            state = NextState(state);
            AppendGeneratedSeparator(builder, state);
            index = index + 1;
        }

        return builder.ToString();
    }

    public static void AppendGeneratedSeparator(StringBuilder builder, int state)
    {
        int choice = PositiveModulo(state, 9);
        if (choice == 0)
        {
            builder.Append('\n');
        }
        else if (choice == 1)
        {
            builder.Append("\n\n");
        }
        else if (choice == 2)
        {
            builder.Append("\r\n");
        }
        else if (choice == 3)
        {
            builder.Append('\t');
        }
        else
        {
            builder.Append(' ');
        }
    }

    public static int NextState(int state)
    {
        long next = (long)state * 1103515245 + 12345;
        long wrapped = next % 4294967296;
        if (wrapped > int.MaxValue)
        {
            wrapped = wrapped - 4294967296;
        }
        else if (wrapped < int.MinValue)
        {
            wrapped = wrapped + 4294967296;
        }

        return (int)wrapped;
    }

    public static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        if (result < 0)
        {
            result = result + modulo;
        }

        return result;
    }

    public static string[] CreateTokenPool()
    {
        return new string[]
        {
            "text",
            "# heading",
            "######",
            "> quote",
            "- item",
            "1. item",
            "```",
            "~~~ info",
            "`code",
            "`` code `",
            "*em",
            "**strong",
            "__",
            "[link]",
            "[link](url",
            "[link](<url with space>)",
            "![alt](image",
            "[ref]: /url",
            "[ref]: /url \"title",
            "<span",
            "<!-- comment",
            "<https://example.com>",
            "<not an autolink>",
            "&amp;",
            "&unknown;",
            "\\*",
            "| table |",
            "| --- |",
            "- [x] task",
            "~~strike",
            "==mark",
            "^sup",
            "~sub",
            "$math",
            "::: note",
            "\0"
        };
    }
}
