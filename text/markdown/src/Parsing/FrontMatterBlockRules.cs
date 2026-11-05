using System;
using System.Collections.Generic;

namespace markdown;

internal class FrontMatterBlockRules
{
    internal const string FrontMatterRuleName = "front-matter";

    internal static void RegisterFrontMatterBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(FrontMatterRuleName))
        {
            return;
        }

        blockRules.Insert(0, FrontMatterRuleName, CreateFrontMatterRule());
    }

    internal static MarkdownBlockRule CreateFrontMatterRule()
    {
        return new MarkdownBlockRule(FrontMatterRuleName, TryParseFrontMatter);
    }

    internal static bool TryParseFrontMatter(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.ParseContext.Pipeline.GetFeatures().FrontMatter || context.LineIndex != 0)
        {
            return false;
        }

        MarkdownLine? openingLine = context.GetCurrentLine();
        if (openingLine == null || !IsFrontMatterOpeningLine(openingLine))
        {
            return false;
        }

        string delimiter = GetFrontMatterDelimiter(openingLine);
        int closingIndex = FindClosingDelimiterIndex(context, delimiter);
        if (closingIndex < 0)
        {
            return false;
        }

        List<string> lines = new List<string>();
        int index = context.LineIndex + 1;
        while (index < closingIndex)
        {
            lines.Add(context.Lines[index].Text);
            index = index + 1;
        }

        MarkdownLine closingLine = context.Lines[closingIndex];
        MarkdownNode frontMatter = MarkdownNode.Create(
            MarkdownNodeKind.FrontMatter,
            MarkdownSourceSpan.Create(openingLine.ContentSpan.Start.Copy(), closingLine.ContentSpan.End.Copy()));
        frontMatter.SetLiteral(CommonMarkBlockRules.JoinLines(lines));
        frontMatter.SetAttribute("delimiter", delimiter);
        frontMatter.SetAttribute("format", GetFrontMatterFormat(delimiter));

        context.ParseContext.Document.Root.AppendChild(frontMatter);
        context.LineIndex = closingIndex + 1;
        context.NoteContentLine();
        return true;
    }

    internal static int FindClosingDelimiterIndex(MarkdownBlockParseContext context, string delimiter)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        int index = context.LineIndex + 1;
        while (index < context.Lines.Length)
        {
            if (IsFrontMatterDelimiterLine(context.Lines[index], delimiter))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsFrontMatterOpeningLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return IsFrontMatterDelimiterLine(line, "---")
            || IsFrontMatterDelimiterLine(line, "+++");
    }

    internal static bool IsFrontMatterDelimiterLine(MarkdownLine line, string delimiter)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        if (line.Text.Length < delimiter.Length || !StartsWith(line.Text, delimiter))
        {
            return false;
        }

        int index = delimiter.Length;
        while (index < line.Text.Length)
        {
            if (!MarkdownChar.IsSpaceOrTab(line.Text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return line.Text.Length == delimiter.Length || index == line.Text.Length;
    }

    internal static string GetFrontMatterDelimiter(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (IsFrontMatterDelimiterLine(line, "---"))
        {
            return "---";
        }

        if (IsFrontMatterDelimiterLine(line, "+++"))
        {
            return "+++";
        }

        return "";
    }

    internal static string GetFrontMatterFormat(string delimiter)
    {
        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        if (delimiter == "---")
        {
            return "yaml";
        }

        if (delimiter == "+++")
        {
            return "toml";
        }

        return "";
    }

    internal static bool StartsWith(string text, string prefix)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (prefix.Length > text.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (text[index] != prefix[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
