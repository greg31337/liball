using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownParsedAttributeList
{
    internal List<string> Tokens;
    internal int Length;

    internal MarkdownParsedAttributeList()
    {
        Tokens = new List<string>();
        Length = 0;
    }
}

internal class MarkdownGenericAttributeInlineRule : IMarkdownInlineRule
{
    public string GetName()
    {
        return GenericAttributeRules.InlineRuleName;
    }

    public string GetTriggerCharacters()
    {
        return "{";
    }

    public bool TryParse(MarkdownInlineParseContext context)
    {
        return GenericAttributeRules.TryParseInlineAttributes(context);
    }
}

internal class GenericAttributeRules
{
    internal const string BlockRuleName = "attribute.block";
    internal const string InlineRuleName = "attribute.inline";

    internal static void RegisterBlockRule(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(BlockRuleName))
        {
            return;
        }

        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (paragraphIndex < 0)
        {
            blockRules.Add(BlockRuleName, new MarkdownBlockRule(BlockRuleName, TryParseBlockAttributes));
        }
        else
        {
            blockRules.Insert(paragraphIndex, BlockRuleName, new MarkdownBlockRule(BlockRuleName, TryParseBlockAttributes));
        }
    }

    internal static void RegisterInlineRule(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (inlineRules.Contains(InlineRuleName))
        {
            return;
        }

        int textIndex = inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (textIndex < 0)
        {
            inlineRules.Add(InlineRuleName, new MarkdownGenericAttributeInlineRule());
        }
        else
        {
            inlineRules.Insert(textIndex, InlineRuleName, new MarkdownGenericAttributeInlineRule());
        }
    }

    internal static bool TryParseBlockAttributes(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.ParseContext.Pipeline.GetFeatures().Attributes || context.LastLineWasBlank)
        {
            return false;
        }

        MarkdownLine? line = context.GetCurrentLine();
        if (line == null || context.ParseContext.Document.Root.GetChildCount() == 0)
        {
            return false;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3)
        {
            return false;
        }

        string text = line.Text.Substring(scan.CharacterIndex);
        MarkdownParsedAttributeList? parsed = ParseAttributeList(text);
        if (parsed == null || parsed.Length != text.Length)
        {
            return false;
        }

        MarkdownNode target = context.ParseContext.Document.Root.GetChild(
            context.ParseContext.Document.Root.GetChildCount() - 1);
        ApplyTokens(target, parsed.Tokens, true);
        target.SetSourceSpan(MarkdownSourceSpan.Create(target.Span.Start, line.ContentSpan.End));
        context.AdvanceLine();
        context.NoteContentLine();
        return true;
    }

    internal static bool IsStandaloneAttributeLine(MarkdownLine line, MarkdownPipeline pipeline)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (!pipeline.GetFeatures().Attributes)
        {
            return false;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3)
        {
            return false;
        }

        string text = line.Text.Substring(scan.CharacterIndex);
        MarkdownParsedAttributeList? parsed = ParseAttributeList(text);
        return parsed != null && parsed.Length == text.Length;
    }

    internal static bool TryParseInlineAttributes(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.ParseContext.Pipeline.GetFeatures().Attributes
            || context.GetCurrentCharacter() != '{'
            || context.Container.GetChildCount() == 0)
        {
            return false;
        }

        MarkdownParsedAttributeList? parsed = ParseAttributeList(context.GetRemainingText());
        if (parsed == null)
        {
            return false;
        }

        MarkdownNode target = context.Container.GetChild(context.Container.GetChildCount() - 1);
        ApplyTokens(target, parsed.Tokens, false);
        context.AdvanceBy(parsed.Length);
        return true;
    }

    internal static MarkdownParsedAttributeList? ParseAttributeList(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length < 3 || text[0] != '{')
        {
            return null;
        }

        int closing = text.IndexOf('}', 1);
        if (closing < 2)
        {
            return null;
        }

        string content = text.Substring(1, closing - 1);
        List<string> tokens = HeadingAttributeParser.TokenizeAttributeBlock(content);
        if (!HeadingAttributeParser.AreAttributeTokens(tokens))
        {
            return null;
        }

        MarkdownParsedAttributeList parsed = new MarkdownParsedAttributeList();
        parsed.Tokens = tokens;
        parsed.Length = closing + 1;
        return parsed;
    }

    internal static void ApplyTokens(MarkdownNode node, List<string> tokens, bool isBlock)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        int index = 0;
        while (index < tokens.Count)
        {
            HeadingAttributeParser.ApplyAttributeToken(node, tokens[index]);
            index = index + 1;
        }
        node.Data.AttachedAttributes.IsBlock = isBlock;
    }
}
