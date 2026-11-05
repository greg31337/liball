using System;

namespace markdown;

internal class EmojiInlineRules
{
    internal const string EmojiRuleName = "emoji.shortcode";

    internal static void RegisterEmojiInlineRules(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (inlineRules.Contains(EmojiRuleName))
        {
            return;
        }

        int textIndex = inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (textIndex < 0)
        {
            inlineRules.Add(EmojiRuleName, CreateEmojiRule());
        }
        else
        {
            inlineRules.Insert(textIndex, EmojiRuleName, CreateEmojiRule());
        }
    }

    internal static MarkdownInlineRule CreateEmojiRule()
    {
        return new MarkdownInlineRule(EmojiRuleName, TryParseEmoji);
    }

    internal static bool TryParseEmoji(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter() || context.Source[context.Position] != ':')
        {
            return false;
        }

        int closingIndex = FindEmojiClosingColon(context.Source, context.Position);
        if (closingIndex < 0)
        {
            return false;
        }

        string name = context.Source.Substring(context.Position + 1, closingIndex - context.Position - 1);
        if (!IsValidEmojiName(name))
        {
            return false;
        }

        string replacement = context.ParseContext.Pipeline.GetOptions().EmojiShortcodes.GetReplacement(name);
        MarkdownNode emoji = MarkdownNode.Create(MarkdownNodeKind.Emoji);
        emoji.SetLiteral(":" + name + ":");
        emoji.SetAttribute("name", name);
        if (replacement.Length > 0)
        {
            emoji.SetAttribute("replacement", replacement);
        }

        context.AppendInline(emoji);
        context.AdvanceBy(closingIndex + 1 - context.Position);
        return true;
    }

    internal static int FindEmojiClosingColon(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (startIndex >= source.Length || source[startIndex] != ':')
        {
            return -1;
        }

        int index = startIndex + 1;
        while (index < source.Length)
        {
            if (source[index] == ':')
            {
                return index;
            }

            if (source[index] == '\n' || source[index] == '\r' || MarkdownChar.IsSpaceOrTab(source[index]))
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsValidEmojiName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if (!char.IsLetterOrDigit(character) && character != '_' && character != '-' && character != '+')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
