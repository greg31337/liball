using System;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownSafetyPolicyPublicTests
{
    [Test]
    public void FacadeProvidesExceptionlessClassificationForMissingAndUnsafeInput()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownSafetyOperations safety = ((IMarkdownDataServices)markdown).CreateSafetyOperations();
        MarkdownPipeline pipeline = markdown.CreateCommonMarkPipeline();

        MarkdownUriEvaluation missing = safety.ClassifyDestinationOrDefault(null);
        MarkdownUriEvaluation unsafeLink = safety.EvaluateLinkDestination(
            "javascript:alert(1)",
            pipeline);
        MarkdownUriEvaluation dataLink = safety.EvaluateLinkDestination(
            "data:image/png;base64,abc",
            pipeline);
        MarkdownUriEvaluation dataImage = safety.EvaluateImageDestination(
            "data:image/png;base64,abc",
            pipeline);

        Assert.That(safety, Is.TypeOf<MarkdownSafetyOperations>());
        Assert.That(missing.InputProvided, Is.False);
        Assert.That(missing.Allowed, Is.False);
        Assert.That(unsafeLink.InputProvided, Is.True);
        Assert.That(unsafeLink.Classification.Kind, Is.EqualTo(MarkdownUriKind.Unsafe));
        Assert.That(unsafeLink.Allowed, Is.False);
        Assert.That(dataLink.Allowed, Is.False);
        Assert.That(dataImage.Allowed, Is.True);
    }

    [Test]
    public void LinkAndImageRewriteHooksRunBeforeIndependentUriPolicies()
    {
        IMarkdown markdown = new Markdown();
        RecordingPolicy linkPolicy = new RecordingPolicy(true);
        RecordingPolicy imagePolicy = new RecordingPolicy(false);
        FixedDestinationRewriter linkRewriter = new FixedDestinationRewriter("javascript:changed");
        FixedDestinationRewriter imageRewriter = new FixedDestinationRewriter("https://images.test/image.png");
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .RewriteLinkUrisWith(linkRewriter)
            .RewriteImageUrisWith(imageRewriter)
            .SetLinkUriPolicy(linkPolicy)
            .SetImageUriPolicy(imagePolicy)
            .Build();

        string html = markdown.RenderHtmlSourceWithPipeline(
            "[link](original) ![image](original)",
            pipeline);

        Assert.That(
            html,
            Is.EqualTo("<p><a href=\"javascript:changed\">link</a> <img alt=\"image\"></p>\n"));
        Assert.That(linkPolicy.LastKind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(linkPolicy.LastClassification, Is.EqualTo(MarkdownUriKind.Unsafe));
        Assert.That(linkPolicy.LastIsImage, Is.False);
        Assert.That(imagePolicy.LastKind, Is.EqualTo(MarkdownNodeKind.Image));
        Assert.That(imagePolicy.LastClassification, Is.EqualTo(MarkdownUriKind.Absolute));
        Assert.That(imagePolicy.LastIsImage, Is.True);
        Assert.That(linkRewriter.LastKind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(imageRewriter.LastKind, Is.EqualTo(MarkdownNodeKind.Image));
    }

    [Test]
    public void ClearingUriHooksRestoresBuiltInSafeDefaults()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder()
            .RewriteLinkUrisWith(new FixedDestinationRewriter("javascript:changed"))
            .SetLinkUriPolicy(new RecordingPolicy(true));
        builder.ClearLinkUriRewriter().ClearLinkUriPolicy();

        MarkdownPipeline pipeline = builder.Build();
        string safe = markdown.RenderHtmlSourceWithPipeline("[link](page)", pipeline);
        string unsafeLink = markdown.RenderHtmlSourceWithPipeline(
            "[link](javascript:original)",
            pipeline);

        Assert.That(safe, Is.EqualTo("<p><a href=\"page\">link</a></p>\n"));
        Assert.That(unsafeLink, Is.EqualTo("<p><a>link</a></p>\n"));
    }

    [Test]
    public void RawHtmlEvaluationReportsEscapeFilterRenderAndSuppressDecisions()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownSafetyOperations safety = ((IMarkdownDataServices)markdown).CreateSafetyOperations();
        MarkdownPipeline escaped = markdown.CreateCommonMarkPipeline();
        MarkdownPipeline filtered = markdown.CreatePipelineBuilder()
            .UseGfm()
            .AllowRawHtml()
            .Build();
        MarkdownPipeline rendered = markdown.CreatePipelineBuilder()
            .AllowUnsafeHtml()
            .Build();
        MarkdownPipeline suppressed = markdown.CreatePipelineBuilder()
            .SetRawHtmlPolicy(new FixedRawHtmlPolicy(MarkdownRawHtmlDecision.Suppress))
            .Build();

        MarkdownRawHtmlEvaluation escapeEvaluation = safety.EvaluateRawHtml("<script>", escaped);
        MarkdownRawHtmlEvaluation filterEvaluation = safety.EvaluateRawHtml("<script>", filtered);
        MarkdownRawHtmlEvaluation renderEvaluation = safety.EvaluateRawHtml("<script>", rendered);
        MarkdownRawHtmlEvaluation suppressEvaluation = safety.EvaluateRawHtml("<script>", suppressed);

        Assert.That(escapeEvaluation.Decision, Is.EqualTo(MarkdownRawHtmlDecision.Escape));
        Assert.That(filterEvaluation.Decision, Is.EqualTo(MarkdownRawHtmlDecision.Filter));
        Assert.That(filterEvaluation.ContainsDisallowedTag, Is.True);
        Assert.That(renderEvaluation.Decision, Is.EqualTo(MarkdownRawHtmlDecision.Render));
        Assert.That(suppressEvaluation.Decision, Is.EqualTo(MarkdownRawHtmlDecision.Suppress));
        Assert.That(safety.RenderRawHtmlLiteral("<script>", escaped), Is.EqualTo("&lt;script&gt;"));
        Assert.That(safety.RenderRawHtmlLiteral("<script>", filtered), Is.EqualTo("&lt;script>"));
        Assert.That(safety.RenderRawHtmlLiteral("<script>", rendered), Is.EqualTo("<script>"));
        Assert.That(safety.RenderRawHtmlLiteral("<script>", suppressed), Is.EqualTo(""));
    }

    [Test]
    public void RawHtmlPolicyControlsDocumentRenderingAndInvalidDecisionsFailClosed()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline renderPipeline = markdown.CreatePipelineBuilder()
            .SetRawHtmlPolicy(new FixedRawHtmlPolicy(MarkdownRawHtmlDecision.Render))
            .Build();
        MarkdownPipeline suppressPipeline = markdown.CreatePipelineBuilder()
            .AllowUnsafeHtml()
            .SetRawHtmlPolicy(new FixedRawHtmlPolicy(MarkdownRawHtmlDecision.Suppress))
            .Build();
        MarkdownPipeline invalidPipeline = markdown.CreatePipelineBuilder()
            .SetRawHtmlPolicy(new InvalidRawHtmlPolicy())
            .Build();

        Assert.That(
            markdown.RenderHtmlSourceWithPipeline("text <b>raw</b>", renderPipeline),
            Is.EqualTo("<p>text <b>raw</b></p>\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline("text <b>raw</b>", suppressPipeline),
            Is.EqualTo("<p>text raw</p>\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline("text <b>raw</b>", invalidPipeline),
            Is.EqualTo("<p>text &lt;b&gt;raw&lt;/b&gt;</p>\n"));
    }

    [Test]
    public void SafetyRenderingRetainsConfiguredOutputLimit()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownSafetyOperations safety = ((IMarkdownDataServices)markdown).CreateSafetyOperations();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .AllowUnsafeHtml()
            .SetMaximumOutputSize(4)
            .Build();

        Assert.That(
            () => safety.RenderRawHtmlLiteral("<strong>", pipeline),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void SafetyConfigurationValidatesCollaboratorsButNotExpectedUnsafeValues()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownSafetyOperations safety = ((IMarkdownDataServices)markdown).CreateSafetyOperations();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();

        Assert.That(() => builder.SetLinkUriPolicy(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetImageUriPolicy(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.RewriteLinkUrisWith(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.RewriteImageUrisWith(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetRawHtmlPolicy(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => safety.EvaluateLinkDestination("safe", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => safety.EvaluateRawHtml("<b>", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(safety.EvaluateRawHtml(null, markdown.CreateCommonMarkPipeline()).InputProvided, Is.False);
        Assert.That(safety.RenderRawHtmlLiteral(null, markdown.CreateCommonMarkPipeline()), Is.EqualTo(""));
    }

    private sealed class FixedDestinationRewriter : IMarkdownDestinationRewriter
    {
        private readonly string destination;
        public MarkdownNodeKind LastKind;

        public FixedDestinationRewriter(string destination)
        {
            this.destination = destination;
            LastKind = MarkdownNodeKind.Unknown;
        }

        public string RewriteDestination(MarkdownNode node, string currentDestination)
        {
            LastKind = node.Kind;
            return destination;
        }
    }

    private sealed class RecordingPolicy : IMarkdownUriPolicy
    {
        private readonly bool allowed;
        public MarkdownNodeKind LastKind;
        public MarkdownUriKind LastClassification;
        public bool LastIsImage;

        public RecordingPolicy(bool allowed)
        {
            this.allowed = allowed;
            LastKind = MarkdownNodeKind.Unknown;
            LastClassification = MarkdownUriKind.Empty;
            LastIsImage = false;
        }

        public bool AllowUri(MarkdownUriPolicyContext context)
        {
            LastKind = context.Node.Kind;
            LastClassification = context.Classification.Kind;
            LastIsImage = context.IsImage;
            return allowed;
        }
    }

    private sealed class FixedRawHtmlPolicy : IMarkdownRawHtmlPolicy
    {
        private readonly MarkdownRawHtmlDecision decision;

        public FixedRawHtmlPolicy(MarkdownRawHtmlDecision decision)
        {
            this.decision = decision;
        }

        public MarkdownRawHtmlDecision DecideRawHtml(MarkdownRawHtmlPolicyContext context)
        {
            return decision;
        }
    }

    private sealed class InvalidRawHtmlPolicy : IMarkdownRawHtmlPolicy
    {
        public MarkdownRawHtmlDecision DecideRawHtml(MarkdownRawHtmlPolicyContext context)
        {
            return (MarkdownRawHtmlDecision)99;
        }
    }
}
