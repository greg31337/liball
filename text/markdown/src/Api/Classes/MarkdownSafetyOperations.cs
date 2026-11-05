using System;
using System.IO;

namespace markdown;

/// <summary>
/// Provides exception-less URI classification and explicit raw-HTML safety decisions.
/// </summary>
public class MarkdownSafetyOperations : IMarkdownSafetyOperations
{
    /// <summary>Initializes safety evaluation operations.</summary>
    public MarkdownSafetyOperations()
    {
    }

    /// <summary>Evaluates a possibly null link destination using a supplied pipeline.</summary>
    /// <param name="destination">The destination, or <c>null</c>.</param>
    /// <param name="pipeline">The safety and rewrite settings.</param>
    /// <returns>The evaluation result.</returns>
    public MarkdownUriEvaluation EvaluateLinkDestination(string? destination, MarkdownPipeline pipeline)
    {
        ValidatePipeline(pipeline);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Link);
        return MarkdownSafetyPolicyEvaluator.EvaluateUri(node, destination, false, pipeline.GetOptions());
    }

    /// <summary>Evaluates a possibly null image destination using a supplied pipeline.</summary>
    /// <param name="destination">The destination, or <c>null</c>.</param>
    /// <param name="pipeline">The safety and rewrite settings.</param>
    /// <returns>The evaluation result.</returns>
    public MarkdownUriEvaluation EvaluateImageDestination(string? destination, MarkdownPipeline pipeline)
    {
        ValidatePipeline(pipeline);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Image);
        return MarkdownSafetyPolicyEvaluator.EvaluateUri(node, destination, true, pipeline.GetOptions());
    }

    /// <summary>Classifies a possibly null URI without throwing for missing or unsafe input.</summary>
    /// <param name="destination">The destination, or <c>null</c>.</param>
    /// <returns>The evaluation containing the safe-default classification.</returns>
    public MarkdownUriEvaluation ClassifyDestinationOrDefault(string? destination)
    {
        return MarkdownSafetyPolicyEvaluator.ClassifyOrDefault(destination);
    }

    /// <summary>Evaluates a possibly null raw HTML literal using a supplied pipeline.</summary>
    /// <param name="literal">The raw HTML literal, or <c>null</c>.</param>
    /// <param name="pipeline">The safety settings.</param>
    /// <returns>The explicit filtering decision.</returns>
    public MarkdownRawHtmlEvaluation EvaluateRawHtml(string? literal, MarkdownPipeline pipeline)
    {
        ValidatePipeline(pipeline);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.HtmlInline);
        return MarkdownSafetyPolicyEvaluator.EvaluateRawHtml(node, literal, pipeline.GetOptions());
    }

    /// <summary>Applies the selected raw-HTML decision with output-size enforcement.</summary>
    /// <param name="literal">The raw HTML literal, or <c>null</c>.</param>
    /// <param name="pipeline">The safety and output-limit settings.</param>
    /// <returns>The bounded rendered text.</returns>
    public string RenderRawHtmlLiteral(string? literal, MarkdownPipeline pipeline)
    {
        ValidatePipeline(pipeline);
        if (literal == null)
        {
            return "";
        }

        StringWriter writer = new StringWriter();
        HtmlWriter htmlWriter = HtmlWriter.Create(writer, pipeline.GetOptions().MaximumOutputSize);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.HtmlInline);
        MarkdownRawHtmlEvaluation evaluation = MarkdownSafetyPolicyEvaluator.EvaluateRawHtml(
            node,
            literal,
            pipeline.GetOptions());
        HtmlRenderer.WriteRawHtmlWithDecision(htmlWriter, literal, evaluation.Decision);
        return writer.ToString();
    }

    internal static void ValidatePipeline(MarkdownPipeline pipeline)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }
    }
}
