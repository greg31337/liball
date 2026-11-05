using System;

namespace markdown;

internal class MarkdownSafetyPolicyEvaluator
{
    internal static MarkdownUriEvaluation EvaluateUri(
        MarkdownNode node,
        string? destination,
        bool isImage,
        MarkdownOptions options)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        MarkdownUriEvaluation evaluation = new MarkdownUriEvaluation();
        if (destination == null)
        {
            return evaluation;
        }

        evaluation.InputProvided = true;
        string rewritten = destination;
        IMarkdownDestinationRewriter? rewriter = isImage
            ? options.ImageUriRewriter
            : options.LinkUriRewriter;
        if (rewriter != null)
        {
            string? replacement = rewriter.RewriteDestination(node, destination);
            if (replacement == null)
            {
                evaluation.Rewritten = true;
                return evaluation;
            }

            rewritten = replacement;
        }

        evaluation.Destination = rewritten;
        evaluation.Rewritten = rewritten != destination;
        evaluation.Classification = MarkdownUriPolicy.Classify(rewritten);
        bool allowed = options.AllowUnsafeLinks
            || (isImage
                ? evaluation.Classification.SafeForImage
                : evaluation.Classification.SafeForLink);
        IMarkdownUriPolicy? policy = isImage ? options.ImageUriPolicy : options.LinkUriPolicy;
        if (policy != null)
        {
            MarkdownUriPolicyContext context = new MarkdownUriPolicyContext();
            context.Node = node;
            context.Destination = rewritten;
            context.Classification = evaluation.Classification;
            context.IsImage = isImage;
            allowed = policy.AllowUri(context);
        }

        evaluation.Allowed = allowed;
        return evaluation;
    }

    internal static MarkdownUriEvaluation ClassifyOrDefault(string? destination)
    {
        MarkdownUriEvaluation evaluation = new MarkdownUriEvaluation();
        if (destination == null)
        {
            return evaluation;
        }

        evaluation.InputProvided = true;
        evaluation.Destination = destination;
        evaluation.Classification = MarkdownUriPolicy.Classify(destination);
        evaluation.Allowed = evaluation.Classification.SafeForLink;
        return evaluation;
    }

    internal static MarkdownRawHtmlEvaluation EvaluateRawHtml(
        MarkdownNode node,
        string? literal,
        MarkdownOptions options)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        MarkdownRawHtmlEvaluation evaluation = new MarkdownRawHtmlEvaluation();
        if (literal == null)
        {
            return evaluation;
        }

        evaluation.InputProvided = true;
        evaluation.ContainsDisallowedTag = ContainsDisallowedTag(literal);
        MarkdownRawHtmlDecision decision = GetDefaultRawHtmlDecision(options);
        if (options.RawHtmlPolicy != null)
        {
            MarkdownRawHtmlPolicyContext context = new MarkdownRawHtmlPolicyContext();
            context.Node = node;
            context.Literal = literal;
            context.DefaultDecision = decision;
            context.Options = options.Copy();
            decision = options.RawHtmlPolicy.DecideRawHtml(context);
        }

        evaluation.Decision = NormalizeRawHtmlDecision(decision);
        return evaluation;
    }

    internal static MarkdownRawHtmlDecision GetDefaultRawHtmlDecision(MarkdownOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (!options.AllowRawHtml && !options.AllowUnsafeHtml)
        {
            return MarkdownRawHtmlDecision.Escape;
        }

        if (options.Features.GfmDisallowedRawHtml && !options.AllowUnsafeHtml)
        {
            return MarkdownRawHtmlDecision.Filter;
        }

        return MarkdownRawHtmlDecision.Render;
    }

    internal static MarkdownRawHtmlDecision NormalizeRawHtmlDecision(MarkdownRawHtmlDecision decision)
    {
        if (decision == MarkdownRawHtmlDecision.Escape
            || decision == MarkdownRawHtmlDecision.Render
            || decision == MarkdownRawHtmlDecision.Filter
            || decision == MarkdownRawHtmlDecision.Suppress)
        {
            return decision;
        }

        return MarkdownRawHtmlDecision.Escape;
    }

    internal static bool ContainsDisallowedTag(string literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        int index = 0;
        while (index < literal.Length)
        {
            if (HtmlRenderer.IsGfmDisallowedRawHtmlTag(literal, index))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
