using System;

namespace markdown;

internal class BlockParser
{
    internal const string InvalidRuleDiagnosticCode = "MD_BLOCK_RULE_INVALID";
    internal const string NoProgressDiagnosticCode = "MD_BLOCK_RULE_NO_PROGRESS";

    internal static void Parse(MarkdownParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownBlockParseContext blockContext = MarkdownBlockParseContext.Create(context);
        Run(blockContext);
    }

    internal static void Run(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        while (context.HasCurrentLine())
        {
            int beforeLineIndex = context.LineIndex;
            bool matched = TryApplyRules(context);

            if (!matched)
            {
                context.NoteContentLine();
                context.AdvanceLine();
            }
            else if (context.HasCurrentLine() && context.LineIndex == beforeLineIndex)
            {
                context.ParseContext.AddDiagnosticValues(
                    MarkdownDiagnosticSeverity.Error,
                    NoProgressDiagnosticCode,
                    "Block rule matched without advancing the current line.",
                    GetCurrentSpan(context));
                context.AdvanceLine();
            }
        }
    }

    internal static bool TryApplyRules(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownRuleList rules = context.ParseContext.Pipeline.GetBlockRules();
        int index = 0;

        while (index < rules.GetCount())
        {
            MarkdownRuleRegistration registration = rules.GetAt(index);
            IMarkdownBlockRule? blockRule = registration.Rule as IMarkdownBlockRule;
            if (blockRule != null)
            {
                if (blockRule.TryParse(context))
                {
                    return true;
                }
            }
            else
            {
                context.ParseContext.AddDiagnosticValues(
                    MarkdownDiagnosticSeverity.Error,
                    InvalidRuleDiagnosticCode,
                    "Registered block rule does not implement IMarkdownBlockRule.",
                    GetCurrentSpan(context));
            }

            index = index + 1;
        }

        return false;
    }

    internal static MarkdownSourceSpan GetCurrentSpan(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? line = context.GetCurrentLine();
        if (line == null)
        {
            return context.ParseContext.Document.Root.Span.Copy();
        }

        return line.LineSpan.Copy();
    }
}
