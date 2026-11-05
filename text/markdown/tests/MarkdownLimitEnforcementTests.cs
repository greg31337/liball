using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownLimitEnforcementTests
{
    [Test]
    public void Parse_AddsDiagnosticWhenNestingDepthExceedsPipelineLimit()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetMaximumNestingDepth(1)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("paragraph", pipeline);

        Assert.That(document.HasErrors(), Is.True);
        Assert.That(HasDiagnostic(document, MarkdownParser.NestingDepthDiagnosticCode), Is.True);
    }

    [Test]
    public void Parse_AddsDiagnosticWhenReferenceDefinitionsExceedPipelineLimit()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetMaximumReferenceDefinitions(1)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("[a]: /a\n[b]: /b", pipeline);

        Assert.That(document.HasErrors(), Is.True);
        Assert.That(HasDiagnostic(document, MarkdownParser.ReferenceDefinitionDiagnosticCode), Is.True);
    }

    [Test]
    public void HtmlRenderer_ThrowsWhenOutputExceedsPipelineLimit()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetMaximumOutputSize(4)
            .Build();

        Assert.That(
            () => Markdown.ToHtml("text", pipeline),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void MarkdownRenderer_ThrowsWhenOutputExceedsPipelineLimit()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetMaximumOutputSize(4)
            .Build();

        Assert.That(
            () => Markdown.ToMarkdown("text", pipeline),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void PlainTextRenderer_ThrowsWhenOutputExceedsPipelineLimit()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetMaximumOutputSize(3)
            .Build();

        Assert.That(
            () => Markdown.ToPlainText("text", pipeline),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void MarkdownWriter_ThrowsWhenGeneratedOutputExceedsLimit()
    {
        StringWriter output = new StringWriter();
        MarkdownWriter writer = MarkdownWriter.Create(output, 3);

        writer.Write("abc");

        Assert.That(
            () => writer.Write("d"),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(output.ToString(), Is.EqualTo("abc"));
    }

    [Test]
    public void LimitHelpers_RejectNullArguments()
    {
        Assert.That(
            () => MarkdownParser.AddReferenceDefinitionLimitDiagnostics(null!),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => MarkdownParser.AddNestingDepthLimitDiagnostics(null!),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => MarkdownParser.AddNestingDepthLimitDiagnostics(
                MarkdownParser.CreateContext("", MarkdownPipeline.CommonMarkPreset()),
                null!,
                0,
                1),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownOutputSizeLimiter(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new MarkdownOutputSizeLimiter(1).Add(null!), Throws.TypeOf<ArgumentNullException>());
    }

    public static bool HasDiagnostic(MarkdownDocument document, string code)
    {
        MarkdownDiagnostic[] diagnostics = document.GetDiagnostics();
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
