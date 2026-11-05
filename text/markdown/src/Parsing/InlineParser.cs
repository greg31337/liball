using System;

namespace markdown;

internal class InlineParser
{
    internal const string InvalidRuleDiagnosticCode = "MD_INLINE_RULE_INVALID";
    internal const string NoProgressDiagnosticCode = "MD_INLINE_RULE_NO_PROGRESS";

    internal static void Parse(MarkdownParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        ParseNode(context, context.Document.Root);
    }

    internal static void ParseNode(MarkdownParseContext context, MarkdownNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (IsInlineContainer(node) && node.Literal.Length > 0 && node.GetChildCount() == 0)
        {
            MarkdownInlineParseContext inlineContext = MarkdownInlineParseContext.Create(context, node);
            Run(inlineContext);
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            ParseNode(context, node.GetChild(index));
            index = index + 1;
        }
    }

    internal static void Run(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        while (context.HasCurrentCharacter())
        {
            int beforePosition = context.Position;
            bool matched = TryApplyRules(context);

            if (!matched)
            {
                context.Advance();
            }
            else if (context.HasCurrentCharacter() && context.Position == beforePosition)
            {
                context.ParseContext.AddDiagnosticValues(
                    MarkdownDiagnosticSeverity.Error,
                    NoProgressDiagnosticCode,
                    "Inline rule matched without advancing the current character.",
                    GetCurrentSpan(context));
                context.Advance();
            }
        }
    }

    internal static bool TryApplyRules(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownRuleList rules = context.ParseContext.Pipeline.GetInlineRules();
        int index = 0;

        while (index < rules.GetCount())
        {
            MarkdownRuleRegistration registration = rules.GetAt(index);
            IMarkdownInlineRule? inlineRule = registration.Rule as IMarkdownInlineRule;
            if (inlineRule != null)
            {
                if (inlineRule.TryParse(context))
                {
                    return true;
                }
            }
            else
            {
                context.ParseContext.AddDiagnosticValues(
                    MarkdownDiagnosticSeverity.Error,
                    InvalidRuleDiagnosticCode,
                    "Registered inline rule does not implement IMarkdownInlineRule.",
                    GetCurrentSpan(context));
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsInlineContainer(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return node.Kind == MarkdownNodeKind.Paragraph
            || node.Kind == MarkdownNodeKind.Heading
            || node.Kind == MarkdownNodeKind.DetailsSummary
            || node.Kind == MarkdownNodeKind.DefinitionTerm
            || node.Kind == MarkdownNodeKind.TableCell;
    }

    internal static MarkdownSourceSpan GetCurrentSpan(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return context.Container.Span.Copy();
    }
}
