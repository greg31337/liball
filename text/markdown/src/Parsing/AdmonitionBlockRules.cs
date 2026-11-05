using System;
using System.Collections.Generic;

namespace markdown;

internal class AdmonitionBlockRules
{
    internal const string AdmonitionBlockRuleName = "admonition.block";

    internal static void RegisterAdmonitionBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(AdmonitionBlockRuleName))
        {
            return;
        }

        int blockQuoteIndex = blockRules.IndexOf(CommonMarkBlockRules.BlockQuoteRuleName);
        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (blockQuoteIndex >= 0)
        {
            blockRules.Insert(blockQuoteIndex, AdmonitionBlockRuleName, CreateAdmonitionBlockRule());
        }
        else if (paragraphIndex >= 0)
        {
            blockRules.Insert(paragraphIndex, AdmonitionBlockRuleName, CreateAdmonitionBlockRule());
        }
        else
        {
            blockRules.Add(AdmonitionBlockRuleName, CreateAdmonitionBlockRule());
        }
    }

    internal static MarkdownBlockRule CreateAdmonitionBlockRule()
    {
        return new MarkdownBlockRule(AdmonitionBlockRuleName, TryParseAdmonitionBlock);
    }

    internal static bool TryParseAdmonitionBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (TryParseAlertBlock(context))
        {
            return true;
        }

        return TryParseBangAdmonitionBlock(context);
    }

    internal static bool TryParseAlertBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || !IsAlertStartLine(firstLine))
        {
            return false;
        }

        string type = GetAlertType(firstLine);
        string title = GetAlertTitle(firstLine);
        List<string> contentLines = new List<string>();
        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();

        context.AdvanceLine();
        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null || !CommonMarkBlockRules.IsBlockQuoteLine(line))
            {
                break;
            }

            contentLines.Add(CommonMarkBlockRules.RemoveBlockQuoteMarker(line));
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        MarkdownNode admonition = CreateAdmonitionNode("alert", type, title, MarkdownSourceSpan.Create(start, end), contentLines, context.ParseContext.Pipeline);
        context.ParseContext.Document.Root.AppendChild(admonition);
        context.NoteContentLine();
        return true;
    }

    internal static bool TryParseBangAdmonitionBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || !IsBangAdmonitionStartLine(firstLine))
        {
            return false;
        }

        string type = GetBangAdmonitionType(firstLine);
        string title = GetBangAdmonitionTitle(firstLine);
        List<string> contentLines = new List<string>();
        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();

        context.AdvanceLine();
        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null)
            {
                break;
            }

            if (CommonMarkBlockRules.IsBlankLine(line))
            {
                contentLines.Add("");
                end = line.ContentSpan.End.Copy();
                context.AdvanceLine();
                continue;
            }

            if (CommonMarkBlockRules.GetLineIndentColumn(line) < 4)
            {
                break;
            }

            contentLines.Add(CommonMarkBlockRules.RemoveOptionalIndentColumns(line, 4));
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        TrimTrailingBlankLines(contentLines);
        MarkdownNode admonition = CreateAdmonitionNode("admonition", type, title, MarkdownSourceSpan.Create(start, end), contentLines, context.ParseContext.Pipeline);
        context.ParseContext.Document.Root.AppendChild(admonition);
        context.NoteContentLine();
        return true;
    }

    internal static MarkdownNode CreateAdmonitionNode(string style, string type, string title, MarkdownSourceSpan span, List<string> contentLines, MarkdownPipeline pipeline)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (contentLines == null)
        {
            throw new ArgumentNullException("contentLines");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownNode admonition = MarkdownNode.Create(MarkdownNodeKind.AdmonitionBlock, span);
        string literal = CommonMarkBlockRules.JoinLines(contentLines);
        admonition.SetLiteral(literal);
        admonition.SetAttribute("style", style);
        admonition.SetAttribute("type", type);
        admonition.SetAttribute("title", title);

        MarkdownDocument contentDocument = MarkdownParser.Parse(literal, pipeline);
        while (contentDocument.Root.GetChildCount() > 0)
        {
            MarkdownNode child = contentDocument.Root.GetChild(0);
            admonition.AppendChild(child);
        }

        return admonition;
    }

    internal static bool IsAlertStartLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (!CommonMarkBlockRules.IsBlockQuoteLine(line))
        {
            return false;
        }

        string type = GetAlertType(line);
        return IsAlertType(type);
    }

    internal static string GetAlertType(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        string content = TrimLeadingSpaces(CommonMarkBlockRules.RemoveBlockQuoteMarker(line));
        if (content.Length < 4 || content[0] != '[' || content[1] != '!')
        {
            return "";
        }

        int index = 2;
        while (index < content.Length && content[index] != ']')
        {
            index = index + 1;
        }

        if (index >= content.Length || index == 2)
        {
            return "";
        }

        return content.Substring(2, index - 2).ToLowerInvariant();
    }

    internal static string GetAlertTitle(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        string content = TrimLeadingSpaces(CommonMarkBlockRules.RemoveBlockQuoteMarker(line));
        int close = content.IndexOf(']');
        if (close < 0 || close + 1 >= content.Length)
        {
            return ToTitleCase(GetAlertType(line));
        }

        string title = TrimSpaces(content.Substring(close + 1));
        if (title.Length > 0)
        {
            return title;
        }

        return ToTitleCase(GetAlertType(line));
    }

    internal static bool IsAlertType(string type)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        string normalized = type.ToLowerInvariant();
        return normalized == "note"
            || normalized == "tip"
            || normalized == "important"
            || normalized == "warning"
            || normalized == "caution";
    }

    internal static bool IsBangAdmonitionStartLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetBangAdmonitionType(line).Length > 0;
    }

    internal static string GetBangAdmonitionType(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || !StartsWithAt(line.Text, scan.CharacterIndex, "!!!"))
        {
            return "";
        }

        int index = scan.CharacterIndex + 3;
        if (index >= line.Text.Length || !MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            return "";
        }

        while (index < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            index = index + 1;
        }

        int start = index;
        while (index < line.Text.Length && IsAdmonitionTypeCharacter(line.Text[index]))
        {
            index = index + 1;
        }

        if (index == start)
        {
            return "";
        }

        return line.Text.Substring(start, index - start).ToLowerInvariant();
    }

    internal static string GetBangAdmonitionTitle(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        string type = GetBangAdmonitionType(line);
        if (type.Length == 0)
        {
            return "";
        }

        int typeIndex = line.Text.IndexOf(type, StringComparison.OrdinalIgnoreCase);
        string title = "";
        if (typeIndex >= 0)
        {
            title = TrimSpaces(line.Text.Substring(typeIndex + type.Length));
        }

        if (title.Length >= 2 && title[0] == '"' && title[title.Length - 1] == '"')
        {
            title = title.Substring(1, title.Length - 2);
        }

        if (title.Length > 0)
        {
            return title;
        }

        return ToTitleCase(type);
    }

    internal static bool IsAdmonitionTypeCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character == '-' || character == '_';
    }

    internal static void TrimTrailingBlankLines(List<string> lines)
    {
        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }

        while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }
    }

    internal static string TrimLeadingSpaces(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length && MarkdownChar.IsSpaceOrTab(text[index]))
        {
            index = index + 1;
        }

        return text.Substring(index);
    }

    internal static string TrimSpaces(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int start = 0;
        while (start < text.Length && MarkdownChar.IsSpaceOrTab(text[start]))
        {
            start = start + 1;
        }

        int end = text.Length;
        while (end > start && MarkdownChar.IsSpaceOrTab(text[end - 1]))
        {
            end = end - 1;
        }

        return text.Substring(start, end - start);
    }

    internal static string ToTitleCase(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length == 0)
        {
            return "";
        }

        return char.ToUpperInvariant(text[0]).ToString() + text.Substring(1).ToLowerInvariant();
    }

    internal static bool StartsWithAt(string text, int index, string value)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (value.Length > text.Length - index)
        {
            return false;
        }

        int offset = 0;
        while (offset < value.Length)
        {
            if (text[index + offset] != value[offset])
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }
}
