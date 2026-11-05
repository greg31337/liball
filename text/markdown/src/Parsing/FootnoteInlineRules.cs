using System;

namespace markdown;

internal class FootnoteInlineRules
{
    internal const string ReferenceRuleName = "footnote.reference";

    internal static void RegisterFootnoteInlineRules(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (inlineRules.Contains(ReferenceRuleName))
        {
            return;
        }

        int linkIndex = inlineRules.IndexOf(CommonMarkInlineRules.LinkRuleName);
        int textIndex = inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (linkIndex >= 0)
        {
            inlineRules.Insert(linkIndex, ReferenceRuleName, CreateReferenceRule());
        }
        else if (textIndex >= 0)
        {
            inlineRules.Insert(textIndex, ReferenceRuleName, CreateReferenceRule());
        }
        else
        {
            inlineRules.Add(ReferenceRuleName, CreateReferenceRule());
        }
    }

    internal static MarkdownInlineRule CreateReferenceRule()
    {
        return new MarkdownInlineRule(ReferenceRuleName, TryParseFootnoteReference);
    }

    internal static bool TryParseFootnoteReference(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter()
            || context.Source.Length - context.Position <= 2
            || context.Source[context.Position] != '['
            || context.Source[context.Position + 1] != '^')
        {
            return false;
        }

        int endIndex = FindFootnoteReferenceEnd(context.Source, context.Position);
        if (endIndex < 0)
        {
            return false;
        }

        string label = context.Source.Substring(context.Position + 2, endIndex - context.Position - 2);
        if (!IsValidFootnoteReferenceLabel(label))
        {
            return false;
        }

        MarkdownNode reference = MarkdownNode.Create(MarkdownNodeKind.FootnoteReference);
        reference.SetLiteral("[^" + label + "]");
        reference.SetAttribute("label", label);
        reference.SetAttribute("normalizedLabel", MarkdownFootnoteDefinitionMap.NormalizeLabel(label));
        if (context.ParseContext.Footnotes.Contains(label))
        {
            reference.SetAttribute("resolved", "true");
        }
        else
        {
            reference.SetAttribute("resolved", "false");
        }

        context.AppendInline(reference);
        context.AdvanceBy(endIndex + 1 - context.Position);
        return true;
    }

    internal static int FindFootnoteReferenceEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (source.Length - startIndex <= 2 || source[startIndex] != '[' || source[startIndex + 1] != '^')
        {
            return -1;
        }

        int index = startIndex + 2;
        while (index < source.Length)
        {
            if (source[index] == ']')
            {
                return index;
            }

            if (source[index] == '\n' || source[index] == '\r')
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsValidFootnoteReferenceLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        if (label.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < label.Length)
        {
            if (label[index] == '[' || label[index] == ']')
            {
                return false;
            }

            index = index + 1;
        }

        return MarkdownFootnoteDefinitionMap.NormalizeLabel(label).Length > 0;
    }
}
