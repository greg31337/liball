using System;

namespace markdown;

internal class TextFormattingInlineRules
{
    internal const string TextFormattingRuleName = "text-formatting";

    internal static void RegisterTextFormattingInlineRules(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (inlineRules.Contains(TextFormattingRuleName))
        {
            return;
        }

        int textIndex = inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (textIndex < 0)
        {
            inlineRules.Add(TextFormattingRuleName, CreateTextFormattingRule());
        }
        else
        {
            inlineRules.Insert(textIndex, TextFormattingRuleName, CreateTextFormattingRule());
        }
    }

    internal static MarkdownInlineRule CreateTextFormattingRule()
    {
        return new MarkdownInlineRule(TextFormattingRuleName, TryParseTextFormatting);
    }

    internal static bool TryParseTextFormatting(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter())
        {
            return false;
        }

        MarkdownFeatureSet features = context.ParseContext.Pipeline.GetFeatures();
        if (features.Superscript && TryParseDelimitedInline(context, "^", MarkdownNodeKind.Superscript))
        {
            return true;
        }

        if (features.Subscript && TryParseDelimitedInline(context, "~", MarkdownNodeKind.Subscript))
        {
            return true;
        }

        if (features.InsertedText && TryParseDelimitedInline(context, "++", MarkdownNodeKind.InsertedText))
        {
            return true;
        }

        if (features.MarkedText && TryParseDelimitedInline(context, "==", MarkdownNodeKind.MarkedText))
        {
            return true;
        }

        return false;
    }

    internal static bool TryParseDelimitedInline(MarkdownInlineParseContext context, string delimiter, MarkdownNodeKind kind)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        MarkdownNodeKindFacts.ValidateInlineExtensionKind(kind);
        if (!StartsWithAt(context.Source, context.Position, delimiter))
        {
            return false;
        }

        if (delimiter == "~" && StartsWithAt(context.Source, context.Position, "~~"))
        {
            return false;
        }

        int contentStart = context.Position + delimiter.Length;
        int closingStart = FindClosingDelimiter(context.Source, contentStart, delimiter);
        if (closingStart <= contentStart)
        {
            return false;
        }

        if (!HasNonWhitespaceContent(context.Source, contentStart, closingStart))
        {
            return false;
        }

        AppendDelimitedInline(context, kind, delimiter, contentStart, closingStart);
        return true;
    }

    internal static void AppendDelimitedInline(MarkdownInlineParseContext context, MarkdownNodeKind kind, string delimiter, int contentStart, int contentEnd)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        MarkdownNodeKindFacts.ValidateInlineExtensionKind(kind);
        if (contentStart < 0 || contentStart > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("contentStart");
        }

        if (contentEnd < contentStart || contentEnd > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("contentEnd");
        }

        MarkdownNode node = MarkdownNode.Create(kind);
        MarkdownInlineParseContext innerContext = MarkdownInlineParseContext.Create(
            context.ParseContext,
            node,
            context.Source.Substring(contentStart, contentEnd - contentStart));
        innerContext.InLink = context.InLink;
        InlineParser.Run(innerContext);
        context.AppendInline(node);
        context.AdvanceBy(contentEnd + delimiter.Length - context.Position);
    }

    internal static int FindClosingDelimiter(string source, int startIndex, string delimiter)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (delimiter.Length == 0)
        {
            throw new ArgumentException("Delimiter cannot be empty.");
        }

        int index = startIndex;
        while (index < source.Length)
        {
            if (StartsWithAt(source, index, delimiter))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool StartsWithAt(string source, int index, string value)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (index < 0 || index > source.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (value.Length > source.Length - index)
        {
            return false;
        }

        int offset = 0;
        while (offset < value.Length)
        {
            if (source[index + offset] != value[offset])
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static bool HasNonWhitespaceContent(string source, int startIndex, int endIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (endIndex < startIndex || endIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        int index = startIndex;
        while (index < endIndex)
        {
            if (!MarkdownChar.IsSpaceOrTab(source[index]) && source[index] != '\n' && source[index] != '\r')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
