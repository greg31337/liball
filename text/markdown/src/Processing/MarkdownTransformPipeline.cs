using System;

namespace markdown;

/// <summary>
/// Applies the transforms registered in a pipeline in registration order.
/// </summary>
internal class MarkdownTransformPipeline
{
    internal const string InvalidRuleDiagnosticCode = "MD_TRANSFORM_RULE_INVALID";

    /// <summary>
    /// Applies each registered transform in order and returns the same document instance.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument Apply(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownTransformContext context = new MarkdownTransformContext(document, pipeline);
        Run(context);
        return document;
    }

    internal static MarkdownDocument Apply(MarkdownParseContext parseContext)
    {
        if (parseContext == null)
        {
            throw new ArgumentNullException("parseContext");
        }

        MarkdownTransformContext context = MarkdownTransformContext.Create(parseContext);
        Run(context);
        return parseContext.Document;
    }

    internal static void Run(MarkdownTransformContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownRuleList rules = context.Pipeline.GetTransformRules();
        int index = 0;
        while (index < rules.GetCount())
        {
            MarkdownRuleRegistration registration = rules.GetAt(index);
            IMarkdownTransform? transform = registration.Rule as IMarkdownTransform;
            if (transform != null)
            {
                transform.Apply(context.Document, context);
            }
            else
            {
                context.AddDiagnosticValues(
                    MarkdownDiagnosticSeverity.Error,
                    InvalidRuleDiagnosticCode,
                    "Registered transform rule does not implement IMarkdownTransform.",
                    context.Document.Root.Span);
            }

            index = index + 1;
        }
    }
}
