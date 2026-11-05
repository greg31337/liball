using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicSafetyRobustnessTests
{
    [Test]
    public void DangerousAndDataUriVariantsFailClosedByDestinationRole()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownSafetyOperations safety = markdown.GetDataServices().CreateSafetyOperations();
        MarkdownPipeline pipeline = markdown.CreateCommonMarkPipeline();
        string[] dangerous = new string[]
        {
            "javascript:alert(1)",
            "JaVaScRiPt:alert(1)",
            "\u0001javascript:alert(1)\u007f",
            "java\tscript:alert(1)",
            "java\nscript:alert(1)",
            "java\rscript:alert(1)",
            "vbscript:msgbox(1)",
            "file:///etc/passwd"
        };

        foreach (string destination in dangerous)
        {
            MarkdownUriEvaluation link = safety.EvaluateLinkDestination(destination, pipeline);
            MarkdownUriEvaluation image = safety.EvaluateImageDestination(destination, pipeline);

            Assert.That(link.Classification.Kind, Is.EqualTo(MarkdownUriKind.Unsafe), destination);
            Assert.That(link.Allowed, Is.False, destination);
            Assert.That(image.Allowed, Is.False, destination);
        }

        string[] safeImageData = new string[]
        {
            "data:image/png;base64,AA==",
            "DATA:IMAGE/GIF;BASE64,AA==",
            "data:image/jpeg,bytes",
            "data:image/jpg,bytes",
            "data:image/webp,bytes",
            "data:image/avif,bytes",
            "da\nta:image/png;base64,AA=="
        };

        foreach (string destination in safeImageData)
        {
            MarkdownUriEvaluation link = safety.EvaluateLinkDestination(destination, pipeline);
            MarkdownUriEvaluation image = safety.EvaluateImageDestination(destination, pipeline);

            Assert.That(link.Classification.Kind, Is.EqualTo(MarkdownUriKind.Data), destination);
            Assert.That(link.Allowed, Is.False, destination);
            Assert.That(image.Allowed, Is.True, destination);
        }

        string[] rejectedData = new string[]
        {
            "data:image/svg+xml,<svg></svg>",
            "data:text/html,<script>alert(1)</script>",
            "data:,plain"
        };

        foreach (string destination in rejectedData)
        {
            Assert.That(safety.EvaluateLinkDestination(destination, pipeline).Allowed, Is.False, destination);
            Assert.That(safety.EvaluateImageDestination(destination, pipeline).Allowed, Is.False, destination);
        }
    }

    [Test]
    public void RawHtmlModesFilterEveryGfmDisallowedTagAndPreserveNearMatches()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownSafetyOperations safety = markdown.GetDataServices().CreateSafetyOperations();
        MarkdownPipeline escaped = markdown.CreateCommonMarkPipeline();
        MarkdownPipeline filtered = markdown.CreatePipelineBuilder().UseGfm().AllowRawHtml().Build();
        MarkdownPipeline rendered = markdown.CreatePipelineBuilder().AllowUnsafeHtml().Build();
        string[] names = new string[]
        {
            "title", "textarea", "style", "xmp", "iframe", "noembed", "noframes", "plaintext", "script"
        };

        foreach (string name in names)
        {
            string literal = "<" + name + ">unsafe</" + name + ">";
            MarkdownRawHtmlEvaluation evaluation = safety.EvaluateRawHtml(literal, filtered);

            Assert.That(evaluation.ContainsDisallowedTag, Is.True, name);
            Assert.That(evaluation.Decision, Is.EqualTo(MarkdownRawHtmlDecision.Filter), name);
            Assert.That(safety.RenderRawHtmlLiteral(literal, filtered), Does.StartWith("&lt;"), name);
        }

        const string mixed = "<script>bad</script><span>safe</span><scripted>near</scripted>";
        Assert.That(safety.RenderRawHtmlLiteral(mixed, escaped), Does.StartWith("&lt;script&gt;"));
        Assert.That(safety.RenderRawHtmlLiteral(mixed, filtered), Does.StartWith("&lt;script>"));
        Assert.That(safety.RenderRawHtmlLiteral(mixed, filtered), Does.Contain("<span>safe</span>"));
        Assert.That(safety.RenderRawHtmlLiteral(mixed, filtered), Does.Contain("<scripted>near</scripted>"));
        Assert.That(safety.RenderRawHtmlLiteral(mixed, rendered), Is.EqualTo(mixed));
    }

    [Test]
    public void MalformedUnicodeIsReplacedConsistentlyWithoutDamagingValidPairs()
    {
        IMarkdown markdown = new Markdown();
        string source = "\ud800A\udc00 😀 \0";
        string normalized = "\ufffdA\ufffd 😀 \ufffd";

        MarkdownDocument fromString = markdown.ParseSource(source);
        MarkdownDocument fromReader = markdown.ParseReader(new StringReader(source));
        MarkdownDocument fromMemory = markdown.ParseMemory(source.AsMemory());

        Assert.That(fromString.Source, Is.EqualTo(normalized));
        Assert.That(fromReader.Source, Is.EqualTo(normalized));
        Assert.That(fromMemory.Source, Is.EqualTo(normalized));
        Assert.That(fromString.Root.Span.End.Offset, Is.EqualTo(source.Length));
        Assert.That(markdown.RenderHtmlDocument(fromReader), Is.EqualTo(markdown.RenderHtmlDocument(fromString)));
        Assert.That(markdown.RenderHtmlDocument(fromMemory), Is.EqualTo(markdown.RenderHtmlDocument(fromString)));
        Assert.That(markdown.RenderHtmlDocument(fromString), Does.Contain("😀"));
    }

    [Test]
    public void NestingAndReferenceLimitsProduceStableDiagnosticsAndRecoverableDocuments()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetMaximumNestingDepth(8)
            .SetMaximumReferenceDefinitions(4)
            .Build();
        StringBuilder source = new StringBuilder();
        source.Append(new string('>', 40));
        source.Append(" nested\n\n");
        int index = 0;
        while (index < 20)
        {
            source.Append("[ref");
            source.Append(index);
            source.Append("]: /destination\n");
            index = index + 1;
        }

        MarkdownDocument first = markdown.ParseSourceWithPipeline(source.ToString(), pipeline);
        MarkdownDocument second = markdown.ParseSourceWithPipeline(source.ToString(), pipeline);

        Assert.That(HasDiagnostic(first, "MD_LIMIT_NESTING_DEPTH"), Is.True);
        Assert.That(HasDiagnostic(first, "MD_LIMIT_REFERENCE_DEFINITIONS"), Is.True);
        AssertDiagnosticsEqual(first, second);
        Assert.That(
            markdown.RenderHtmlDocumentWithPipeline(first, pipeline),
            Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(second, pipeline)));
    }

    [Test]
    public void PathologicalDelimiterBracketAndMalformedMarkdownRunsRecoverDeterministically()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreateCommonMarkPipeline();
        string[] sources = new string[]
        {
            new string('*', 512),
            new string('[', 512) + new string(']', 512),
            new string('_', 512) + " text " + new string('~', 512),
            "```language\nunclosed **strong [link](destination <span &unknown; `code",
            "[[[[[[[********____!!!!](<unterminated"
        };

        foreach (string source in sources)
        {
            AssertDeterministic(markdown, source, pipeline);
        }
    }

    [Test]
    public void SeededFuzzLikeInputsRecoverDeterministicallyThroughThePublicPipeline()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreateAdvancedPipeline();
        int seed = 1;
        int caseIndex = 0;

        while (caseIndex < 32)
        {
            StringBuilder source = new StringBuilder();
            int tokenIndex = 0;
            while (tokenIndex < 72)
            {
                seed = NextState(seed);
                source.Append(FuzzTokens()[PositiveModulo(seed, FuzzTokens().Length)]);
                seed = NextState(seed);
                source.Append(PositiveModulo(seed, 5) == 0 ? '\n' : ' ');
                tokenIndex = tokenIndex + 1;
            }

            AssertDeterministic(markdown, source.ToString(), pipeline);
            caseIndex = caseIndex + 1;
        }
    }

    private static void AssertDeterministic(IMarkdown markdown, string source, MarkdownPipeline pipeline)
    {
        MarkdownDocument first = markdown.ParseSourceWithPipeline(source, pipeline);
        MarkdownDocument second = markdown.ParseSourceWithPipeline(source, pipeline);

        Assert.That(second.Source, Is.EqualTo(first.Source));
        AssertDiagnosticsEqual(first, second);
        Assert.That(markdown.RenderHtmlDocumentWithPipeline(second, pipeline),
            Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(first, pipeline)));
        Assert.That(markdown.RenderMarkdownDocumentWithPipeline(second, pipeline),
            Is.EqualTo(markdown.RenderMarkdownDocumentWithPipeline(first, pipeline)));
        Assert.That(markdown.RenderPlainTextDocumentWithPipeline(second, pipeline),
            Is.EqualTo(markdown.RenderPlainTextDocumentWithPipeline(first, pipeline)));
        Assert.That(markdown.RenderDebugTreeDocumentWithPipeline(second, pipeline),
            Is.EqualTo(markdown.RenderDebugTreeDocumentWithPipeline(first, pipeline)));
        Assert.That(markdown.RenderXmlAstDocumentWithPipeline(second, pipeline),
            Is.EqualTo(markdown.RenderXmlAstDocumentWithPipeline(first, pipeline)));
    }

    private static void AssertDiagnosticsEqual(MarkdownDocument expected, MarkdownDocument actual)
    {
        MarkdownDiagnostic[] expectedDiagnostics = expected.GetDiagnostics();
        MarkdownDiagnostic[] actualDiagnostics = actual.GetDiagnostics();
        Assert.That(actualDiagnostics.Length, Is.EqualTo(expectedDiagnostics.Length));
        int index = 0;
        while (index < expectedDiagnostics.Length)
        {
            Assert.That(actualDiagnostics[index].Code, Is.EqualTo(expectedDiagnostics[index].Code));
            Assert.That(actualDiagnostics[index].Severity, Is.EqualTo(expectedDiagnostics[index].Severity));
            Assert.That(actualDiagnostics[index].Span.Start.Offset,
                Is.EqualTo(expectedDiagnostics[index].Span.Start.Offset));
            Assert.That(actualDiagnostics[index].Span.End.Offset,
                Is.EqualTo(expectedDiagnostics[index].Span.End.Offset));
            index = index + 1;
        }
    }

    private static bool HasDiagnostic(MarkdownDocument document, string code)
    {
        foreach (MarkdownDiagnostic diagnostic in document.GetDiagnostics())
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    private static int NextState(int state)
    {
        return unchecked(state * 1103515245 + 12345);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static string[] FuzzTokens()
    {
        return new string[]
        {
            "text", "# heading", "> quote", "- item", "```", "~~~ info", "`code", "**strong",
            "____", "[[link]", "[link](url", "![alt](image", "[ref]: /url", "<script>",
            "<!-- comment", "<https://example.test>", "&unknown;", "\\*", "| table |", "- [x] task",
            "~~strike", "==mark", "^sup", "~sub", "$math", "::: note", "\0", "\ud800", "\udc00", "😀"
        };
    }
}
