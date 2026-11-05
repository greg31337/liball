using System;
using System.Collections.Generic;

namespace markdown;

internal class CustomContainerBlockRules
{
    internal const string CustomContainerBlockRuleName = "custom-container.block";

    internal static void RegisterCustomContainerBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(CustomContainerBlockRuleName))
        {
            return;
        }

        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (paragraphIndex < 0)
        {
            blockRules.Add(CustomContainerBlockRuleName, CreateCustomContainerBlockRule());
        }
        else
        {
            blockRules.Insert(paragraphIndex, CustomContainerBlockRuleName, CreateCustomContainerBlockRule());
        }
    }

    internal static MarkdownBlockRule CreateCustomContainerBlockRule()
    {
        return new MarkdownBlockRule(CustomContainerBlockRuleName, TryParseCustomContainerBlock);
    }

    internal static bool TryParseCustomContainerBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? openingLine = context.GetCurrentLine();
        if (openingLine == null || !IsContainerOpeningLine(openingLine))
        {
            return false;
        }

        string name = GetContainerName(openingLine);
        if (!context.ParseContext.Pipeline.GetOptions().CustomContainers.Contains(name))
        {
            return false;
        }

        string title = GetContainerTitle(openingLine);
        int fenceLength = GetContainerFenceLength(openingLine);
        List<string> lines = new List<string>();
        MarkdownSourcePosition start = openingLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = openingLine.ContentSpan.End.Copy();

        context.AdvanceLine();
        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null)
            {
                break;
            }

            if (IsContainerClosingLine(line, fenceLength))
            {
                end = line.ContentSpan.End.Copy();
                context.AdvanceLine();
                break;
            }

            lines.Add(line.Text);
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        MarkdownNode container = CreateContainerNode(name, title, MarkdownSourceSpan.Create(start, end), lines, context.ParseContext.Pipeline);
        context.ParseContext.Document.Root.AppendChild(container);
        context.NoteContentLine();
        return true;
    }

    internal static MarkdownNode CreateContainerNode(string name, string title, MarkdownSourceSpan span, List<string> lines, MarkdownPipeline pipeline)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownNode container = MarkdownNode.Create(MarkdownNodeKind.CustomContainerBlock, span);
        string literal = CommonMarkBlockRules.JoinLines(lines);
        container.SetLiteral(literal);
        container.SetAttribute("name", name);
        if (title.Length > 0)
        {
            container.SetAttribute("title", title);
        }

        MarkdownDocument contentDocument = MarkdownParser.Parse(literal, pipeline);
        while (contentDocument.Root.GetChildCount() > 0)
        {
            MarkdownNode child = contentDocument.Root.GetChild(0);
            container.AppendChild(child);
        }

        return container;
    }

    internal static bool IsContainerOpeningLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetContainerName(line).Length > 0;
    }

    internal static int GetContainerFenceLength(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length || line.Text[scan.CharacterIndex] != ':')
        {
            return 0;
        }

        int index = scan.CharacterIndex;
        int count = 0;
        while (index < line.Text.Length && line.Text[index] == ':')
        {
            count = count + 1;
            index = index + 1;
        }

        if (count < 3)
        {
            return 0;
        }

        return count;
    }

    internal static string GetContainerName(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int fenceLength = GetContainerFenceLength(line);
        if (fenceLength == 0)
        {
            return "";
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int index = scan.CharacterIndex + fenceLength;
        if (index >= line.Text.Length || !MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            return "";
        }

        while (index < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            index = index + 1;
        }

        int start = index;
        while (index < line.Text.Length && IsContainerNameCharacter(line.Text[index]))
        {
            index = index + 1;
        }

        if (index == start)
        {
            return "";
        }

        return line.Text.Substring(start, index - start);
    }

    internal static string GetContainerTitle(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        string name = GetContainerName(line);
        if (name.Length == 0)
        {
            return "";
        }

        int nameIndex = line.Text.IndexOf(name);
        string title = "";
        if (nameIndex >= 0)
        {
            title = AdmonitionBlockRules.TrimSpaces(line.Text.Substring(nameIndex + name.Length));
        }

        if (title.Length >= 2 && title[0] == '"' && title[title.Length - 1] == '"')
        {
            title = title.Substring(1, title.Length - 2);
        }

        return title;
    }

    internal static bool IsContainerClosingLine(MarkdownLine line, int openingFenceLength)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (openingFenceLength < 3)
        {
            throw new ArgumentOutOfRangeException("openingFenceLength");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length || line.Text[scan.CharacterIndex] != ':')
        {
            return false;
        }

        int index = scan.CharacterIndex;
        int count = 0;
        while (index < line.Text.Length && line.Text[index] == ':')
        {
            count = count + 1;
            index = index + 1;
        }

        if (count < openingFenceLength)
        {
            return false;
        }

        while (index < line.Text.Length)
        {
            if (!MarkdownChar.IsSpaceOrTab(line.Text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsContainerNameCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character == '-' || character == '_';
    }

    internal static bool IsValidContainerName(string name)
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
            if (!IsContainerNameCharacter(name[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
