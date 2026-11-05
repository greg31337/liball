using System;

namespace markdown;

internal class GfmInlineRules
{
    internal const string StrikethroughRuleName = "gfm.strikethrough";
    internal const string BareAutolinkRuleName = "gfm.bare-autolink";

    internal static void RegisterGfmInlineRules(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        RegisterGfmStrikethroughRule(inlineRules);
        RegisterGfmBareAutolinkRule(inlineRules);
    }

    internal static void RegisterGfmStrikethroughRule(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (inlineRules.Contains(StrikethroughRuleName))
        {
            return;
        }

        int textIndex = inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (textIndex < 0)
        {
            inlineRules.Add(StrikethroughRuleName, CreateStrikethroughRule());
        }
        else
        {
            inlineRules.Insert(textIndex, StrikethroughRuleName, CreateStrikethroughRule());
        }
    }

    internal static void RegisterGfmBareAutolinkRule(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (inlineRules.Contains(BareAutolinkRuleName))
        {
            return;
        }

        int textIndex = inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (textIndex < 0)
        {
            inlineRules.Add(BareAutolinkRuleName, CreateBareAutolinkRule());
        }
        else
        {
            inlineRules.Insert(textIndex, BareAutolinkRuleName, CreateBareAutolinkRule());
        }
    }

    internal static MarkdownInlineRule CreateStrikethroughRule()
    {
        return new MarkdownInlineRule(StrikethroughRuleName, TryParseStrikethrough);
    }

    internal static MarkdownInlineRule CreateBareAutolinkRule()
    {
        return new MarkdownInlineRule(BareAutolinkRuleName, TryParseBareAutolink);
    }

    internal static bool TryParseStrikethrough(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter()
            || context.Source[context.Position] != '~'
            || context.Position > 0 && context.Source[context.Position - 1] == '~')
        {
            return false;
        }

        int delimiterLength = 1;
        if (context.Position + 1 < context.Source.Length && context.Source[context.Position + 1] == '~')
        {
            delimiterLength = 2;
        }

        if (context.Position + delimiterLength < context.Source.Length
            && context.Source[context.Position + delimiterLength] == '~')
        {
            return false;
        }

        if (delimiterLength == 1
            && context.ParseContext.Pipeline.GetFeatures().Subscript)
        {
            return false;
        }

        int contentStart = context.Position + delimiterLength;
        int closingStart = FindClosingStrikethroughDelimiter(context.Source, contentStart);
        if (closingStart <= contentStart)
        {
            return false;
        }

        AppendStrikethroughInline(context, contentStart, closingStart);
        return true;
    }

    internal static void AppendStrikethroughInline(MarkdownInlineParseContext context, int contentStart, int contentEnd)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (contentStart < 0 || contentStart > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("contentStart");
        }

        if (contentEnd < contentStart || contentEnd > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("contentEnd");
        }

        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Strikethrough);
        MarkdownInlineParseContext innerContext = MarkdownInlineParseContext.Create(
            context.ParseContext,
            node,
            context.Source.Substring(contentStart, contentEnd - contentStart));
        innerContext.InLink = context.InLink;
        InlineParser.Run(innerContext);
        context.AppendInline(node);
        int delimiterLength = contentStart - context.Position;
        context.AdvanceBy(contentEnd + delimiterLength - context.Position);
    }

    internal static int FindClosingStrikethroughDelimiter(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        int delimiterLength = 1;
        if (startIndex >= 2 && source[startIndex - 1] == '~' && source[startIndex - 2] == '~')
        {
            delimiterLength = 2;
        }

        int index = startIndex;
        while (index < source.Length)
        {
            bool hasDelimiter = source[index] == '~';
            if (delimiterLength == 2)
            {
                hasDelimiter = hasDelimiter
                    && index + 1 < source.Length
                    && source[index + 1] == '~';
            }

            bool precededByTilde = index > startIndex && source[index - 1] == '~';
            bool followedByTilde = index + delimiterLength < source.Length
                && source[index + delimiterLength] == '~';
            if (hasDelimiter && !precededByTilde && !followedByTilde)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool TryParseBareAutolink(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter() || CommonMarkInlineRules.IsLinkParsingProhibited(context))
        {
            return false;
        }

        if (!HasBareAutolinkBoundary(context.Source, context.Position))
        {
            return false;
        }

        MarkdownBareAutolinkMatch match = FindBareAutolink(context.Source, context.Position);
        if (!match.Success)
        {
            return false;
        }

        CommonMarkInlineRules.AppendAutolink(context, match.Text, match.Destination, match.EndIndex);
        return true;
    }

    internal static MarkdownBareAutolinkMatch FindBareAutolink(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        MarkdownBareAutolinkMatch protocol = FindBareProtocolAutolink(source, startIndex);
        if (protocol.Success)
        {
            return protocol;
        }

        MarkdownBareAutolinkMatch url = FindBareUrlAutolink(source, startIndex);
        if (url.Success)
        {
            return url;
        }

        return FindBareEmailAutolink(source, startIndex);
    }

    internal static MarkdownBareAutolinkMatch FindBareProtocolAutolink(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        bool mailto = StartsWithIgnoreCaseAt(source, startIndex, "mailto:");
        bool xmpp = StartsWithIgnoreCaseAt(source, startIndex, "xmpp:");
        if (!mailto && !xmpp)
        {
            return new MarkdownBareAutolinkMatch();
        }

        int prefixLength = 5;
        if (mailto)
        {
            prefixLength = 7;
        }

        int addressStart = startIndex + prefixLength;
        int addressEnd = addressStart;
        while (addressEnd < source.Length && IsBareEmailCharacter(source[addressEnd]))
        {
            addressEnd = addressEnd + 1;
        }

        while (addressEnd > addressStart && source[addressEnd - 1] == '.')
        {
            addressEnd = addressEnd - 1;
        }

        string address = source.Substring(addressStart, addressEnd - addressStart);
        if (!IsGfmBareEmailAddress(address))
        {
            return new MarkdownBareAutolinkMatch();
        }

        int end = addressEnd;
        if (xmpp && end < source.Length && source[end] == '/')
        {
            int resourceEnd = end + 1;
            while (resourceEnd < source.Length && IsXmppResourceCharacter(source[resourceEnd]))
            {
                resourceEnd = resourceEnd + 1;
            }

            if (resourceEnd > end + 1)
            {
                end = resourceEnd;
            }
        }

        string text = source.Substring(startIndex, end - startIndex);
        return MarkdownBareAutolinkMatch.Create(true, end - 1, text, text);
    }

    internal static MarkdownBareAutolinkMatch FindBareUrlAutolink(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        bool www = StartsWithIgnoreCaseAt(source, startIndex, "www.");
        bool http = StartsWithIgnoreCaseAt(source, startIndex, "http://");
        bool https = StartsWithIgnoreCaseAt(source, startIndex, "https://");
        if (!www && !http && !https)
        {
            return new MarkdownBareAutolinkMatch();
        }

        int end = FindBareUrlEnd(source, startIndex);
        end = TrimBareUrlEnd(source, startIndex, end);
        if (end <= startIndex)
        {
            return new MarkdownBareAutolinkMatch();
        }

        string text = source.Substring(startIndex, end - startIndex);
        string destination = text;
        if (www)
        {
            destination = "http://" + text;
        }

        return MarkdownBareAutolinkMatch.Create(true, end - 1, text, destination);
    }

    internal static MarkdownBareAutolinkMatch FindBareEmailAutolink(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        int end = startIndex;
        while (end < source.Length && IsBareEmailCharacter(source[end]))
        {
            end = end + 1;
        }

        while (end > startIndex && IsTrailingBareEmailPunctuation(source[end - 1]))
        {
            end = end - 1;
        }

        if (end <= startIndex)
        {
            return new MarkdownBareAutolinkMatch();
        }

        string text = source.Substring(startIndex, end - startIndex);
        if (!IsGfmBareEmailAddress(text))
        {
            return new MarkdownBareAutolinkMatch();
        }

        return MarkdownBareAutolinkMatch.Create(true, end - 1, text, "mailto:" + text);
    }

    internal static bool IsPotentialBareAutolinkStart(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (startIndex >= source.Length)
        {
            return false;
        }

        return StartsWithIgnoreCaseAt(source, startIndex, "http://")
            || StartsWithIgnoreCaseAt(source, startIndex, "https://")
            || StartsWithIgnoreCaseAt(source, startIndex, "www.")
            || StartsWithIgnoreCaseAt(source, startIndex, "mailto:")
            || StartsWithIgnoreCaseAt(source, startIndex, "xmpp:")
            || CouldStartBareEmail(source, startIndex);
    }

    internal static bool CouldStartBareEmail(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (startIndex >= source.Length || !CommonMarkInlineRules.IsEmailLocalPartCharacter(source[startIndex]))
        {
            return false;
        }

        int index = startIndex;
        while (index < source.Length && IsBareEmailCharacter(source[index]))
        {
            if (source[index] == '@')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasBareAutolinkBoundary(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (startIndex == 0)
        {
            return true;
        }

        char before = source[startIndex - 1];
        return MarkdownChar.IsWhitespace(before)
            || before == '('
            || before == '['
            || before == '<'
            || before == '"'
            || before == '\'';
    }

    internal static int FindBareUrlEnd(string source, int startIndex)
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
            char character = source[index];
            if (character == '<' || MarkdownChar.IsWhitespace(character) || MarkdownChar.IsAsciiControl(character))
            {
                return index;
            }

            index = index + 1;
        }

        return index;
    }

    internal static int TrimBareUrlEnd(string source, int startIndex, int endIndex)
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

        int end = endIndex;
        if (end > startIndex && source[end - 1] == ';')
        {
            int entityStart = end - 2;
            while (entityStart >= startIndex && IsAsciiAlphaNumeric(source[entityStart]))
            {
                entityStart = entityStart - 1;
            }

            if (entityStart >= startIndex
                && source[entityStart] == '&'
                && entityStart < end - 2)
            {
                end = entityStart;
            }
        }

        while (end > startIndex && IsTrailingBareUrlPunctuation(source[end - 1]))
        {
            end = end - 1;
        }

        while (end > startIndex && source[end - 1] == ')' && HasMoreClosingParensThanOpening(source, startIndex, end))
        {
            end = end - 1;
        }

        return end;
    }

    internal static bool HasMoreClosingParensThanOpening(string source, int startIndex, int endIndex)
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

        int open = 0;
        int close = 0;
        int index = startIndex;
        while (index < endIndex)
        {
            if (source[index] == '(')
            {
                open = open + 1;
            }
            else if (source[index] == ')')
            {
                close = close + 1;
            }

            index = index + 1;
        }

        return close > open;
    }

    internal static bool IsBareEmailCharacter(char character)
    {
        return IsAsciiAlphaNumeric(character)
            || character == '.'
            || character == '-'
            || character == '_'
            || character == '+'
            || character == '@';
    }

    internal static bool IsGfmBareEmailAddress(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int atIndex = CommonMarkInlineRules.GetSingleAtSignIndex(text);
        if (atIndex <= 0 || atIndex + 1 >= text.Length)
        {
            return false;
        }

        int index = 0;
        while (index < atIndex)
        {
            char character = text[index];
            if (!IsAsciiAlphaNumeric(character)
                && character != '.'
                && character != '-'
                && character != '_'
                && character != '+')
            {
                return false;
            }

            index = index + 1;
        }

        bool hasDot = false;
        index = atIndex + 1;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '.')
            {
                if (index == atIndex + 1 || text[index - 1] == '.')
                {
                    return false;
                }

                hasDot = true;
            }
            else if (!IsAsciiAlphaNumeric(character)
                && character != '-'
                && character != '_')
            {
                return false;
            }

            index = index + 1;
        }

        char last = text[text.Length - 1];
        return hasDot && last != '.' && last != '-' && last != '_';
    }

    internal static bool IsXmppResourceCharacter(char character)
    {
        return IsAsciiAlphaNumeric(character)
            || character == '@'
            || character == '.';
    }

    internal static bool IsAsciiAlphaNumeric(char character)
    {
        return MarkdownUriPolicy.IsAsciiLetter(character)
            || character >= '0' && character <= '9';
    }

    internal static bool IsTrailingBareUrlPunctuation(char character)
    {
        return character == '.'
            || character == ','
            || character == ':'
            || character == ';'
            || character == '!'
            || character == '?';
    }

    internal static bool IsTrailingBareEmailPunctuation(char character)
    {
        return character == '.'
            || character == ','
            || character == ':'
            || character == ';'
            || character == '!'
            || character == '?';
    }

    internal static bool StartsWithIgnoreCaseAt(string text, int startIndex, string value)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (startIndex < 0 || startIndex > text.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (value.Length > text.Length - startIndex)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char left = char.ToLowerInvariant(text[startIndex + index]);
            char right = char.ToLowerInvariant(value[index]);
            if (left != right)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}

internal class MarkdownBareAutolinkMatch
{
    internal bool Success;
    internal int EndIndex;
    internal string Text;
    internal string Destination;

    internal MarkdownBareAutolinkMatch()
    {
        Success = false;
        EndIndex = -1;
        Text = "";
        Destination = "";
    }

    internal static MarkdownBareAutolinkMatch Create(bool success, int endIndex, string text, string destination)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        MarkdownBareAutolinkMatch match = new MarkdownBareAutolinkMatch();
        match.Success = success;
        match.EndIndex = endIndex;
        match.Text = text;
        match.Destination = destination;
        return match;
    }
}
