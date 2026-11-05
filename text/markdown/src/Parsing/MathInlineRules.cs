using System;

namespace markdown;

internal class MathInlineRules
{
    internal const string MathSpanRuleName = "math.span";

    internal static void RegisterMathInlineRules(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (inlineRules.Contains(MathSpanRuleName))
        {
            return;
        }

        int textIndex = inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (textIndex < 0)
        {
            inlineRules.Add(MathSpanRuleName, CreateMathSpanRule());
        }
        else
        {
            inlineRules.Insert(textIndex, MathSpanRuleName, CreateMathSpanRule());
        }
    }

    internal static MarkdownInlineRule CreateMathSpanRule()
    {
        return new MarkdownInlineRule(MathSpanRuleName, TryParseMathSpan);
    }

    internal static bool TryParseMathSpan(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter()
            || context.Source[context.Position] != '$'
            || IsAdjacentToDollar(context.Source, context.Position)
            || TextFormattingInlineRules.StartsWithAt(context.Source, context.Position, "$$"))
        {
            return false;
        }

        int contentStart = context.Position + 1;
        int closingIndex = FindClosingDollar(context.Source, contentStart);
        if (closingIndex <= contentStart)
        {
            return false;
        }

        if (!HasValidMathSpanContent(context.Source, contentStart, closingIndex))
        {
            return false;
        }

        if (IsAdjacentToDollar(context.Source, closingIndex))
        {
            return false;
        }

        MarkdownNode mathSpan = MarkdownNode.Create(MarkdownNodeKind.MathSpan);
        mathSpan.SetLiteral(context.Source.Substring(contentStart, closingIndex - contentStart));
        mathSpan.SetAttribute("delimiter", "$");
        context.AppendInline(mathSpan);
        context.AdvanceBy(closingIndex + 1 - context.Position);
        return true;
    }

    internal static int FindClosingDollar(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        int index = startIndex;
        while (index < source.Length)
        {
            if (source[index] == '$')
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

    internal static bool HasValidMathSpanContent(string source, int startIndex, int endIndex)
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

        if (startIndex == endIndex)
        {
            return false;
        }

        if (IsMathSpanBoundaryWhitespace(source[startIndex]) || IsMathSpanBoundaryWhitespace(source[endIndex - 1]))
        {
            return false;
        }

        return true;
    }

    internal static bool IsMathSpanBoundaryWhitespace(char character)
    {
        return MarkdownChar.IsSpaceOrTab(character) || character == '\n' || character == '\r';
    }

    internal static bool IsAdjacentToDollar(string source, int index)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (index < 0 || index > source.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (index > 0 && source[index - 1] == '$')
        {
            return true;
        }

        if (index + 1 < source.Length && source[index + 1] == '$')
        {
            return true;
        }

        return false;
    }
}
