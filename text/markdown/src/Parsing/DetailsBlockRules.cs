using System;
using System.Collections.Generic;

namespace markdown;

internal class DetailsBlockRules
{
    internal const string DetailsBlockRuleName = "details.block";

    internal static void RegisterDetailsBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(DetailsBlockRuleName))
        {
            return;
        }

        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (paragraphIndex >= 0)
        {
            blockRules.Insert(paragraphIndex, DetailsBlockRuleName, CreateDetailsBlockRule());
        }
        else
        {
            blockRules.Add(DetailsBlockRuleName, CreateDetailsBlockRule());
        }
    }

    internal static MarkdownBlockRule CreateDetailsBlockRule()
    {
        return new MarkdownBlockRule(DetailsBlockRuleName, TryParseDetailsBlock);
    }

    internal static bool TryParseDetailsBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? openingLine = context.GetCurrentLine();
        if (openingLine == null || !IsDetailsOpeningLine(openingLine))
        {
            return false;
        }

        int fenceLength = GetFenceLength(openingLine);
        string summary = GetSummary(openingLine);
        bool isOpen = IsInitiallyOpen(openingLine);
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

            if (IsClosingLine(line, fenceLength))
            {
                end = line.ContentSpan.End.Copy();
                context.AdvanceLine();
                break;
            }

            lines.Add(line.Text);
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        MarkdownNode details = CreateDetailsNode(
            summary,
            isOpen,
            MarkdownSourceSpan.Create(start, end),
            openingLine.ContentSpan,
            lines,
            context.ParseContext.Pipeline);
        context.ParseContext.Document.Root.AppendChild(details);
        context.NoteContentLine();
        return true;
    }

    internal static MarkdownNode CreateDetailsNode(
        string summary,
        bool isOpen,
        MarkdownSourceSpan span,
        MarkdownSourceSpan summarySpan,
        List<string> lines,
        MarkdownPipeline pipeline)
    {
        if (summary == null)
        {
            throw new ArgumentNullException("summary");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (summarySpan == null)
        {
            throw new ArgumentNullException("summarySpan");
        }

        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownNode details = MarkdownNode.Create(MarkdownNodeKind.DetailsBlock, span);
        string literal = CommonMarkBlockRules.JoinLines(lines);
        details.SetLiteral(literal);
        details.SetAttribute("summary", summary);
        details.SetAttribute("open", isOpen ? "true" : "false");

        MarkdownNode summaryNode = MarkdownNode.Create(MarkdownNodeKind.DetailsSummary, summarySpan);
        summaryNode.SetLiteral(summary);
        summaryNode.SetAttribute("summary", summary);
        summaryNode.SetAttribute("open", isOpen ? "true" : "false");
        details.AppendChild(summaryNode);

        MarkdownDocument contentDocument = MarkdownParser.Parse(literal, pipeline);
        while (contentDocument.Root.GetChildCount() > 0)
        {
            MarkdownNode child = contentDocument.Root.GetChild(0);
            details.AppendChild(child);
        }

        return details;
    }

    internal static bool IsDetailsOpeningLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetMarkerEnd(line) >= 0;
    }

    internal static bool IsInitiallyOpen(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerEnd = GetMarkerEnd(line);
        if (markerEnd < 0)
        {
            return false;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int index = scan.CharacterIndex + GetFenceLength(line);
        return index < line.Text.Length && line.Text[index] == '+';
    }

    internal static string GetSummary(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerEnd = GetMarkerEnd(line);
        if (markerEnd < 0)
        {
            return "";
        }

        string summary = AdmonitionBlockRules.TrimSpaces(line.Text.Substring(markerEnd));
        if (summary.Length >= 2
            && ((summary[0] == '"' && summary[summary.Length - 1] == '"')
                || (summary[0] == '\'' && summary[summary.Length - 1] == '\'')))
        {
            summary = summary.Substring(1, summary.Length - 2);
        }

        return summary;
    }

    internal static int GetMarkerEnd(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int fenceLength = GetFenceLength(line);
        if (fenceLength < 3)
        {
            return -1;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int index = scan.CharacterIndex + fenceLength;
        if (index < line.Text.Length && line.Text[index] == '+')
        {
            index = index + 1;
        }

        if (index >= line.Text.Length || !MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            return -1;
        }

        while (index < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            index = index + 1;
        }

        const string marker = "details";
        if (line.Text.Length - index < marker.Length
            || line.Text.Substring(index, marker.Length) != marker)
        {
            return -1;
        }

        index = index + marker.Length;

        if (index < line.Text.Length && !MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            return -1;
        }

        return index;
    }

    internal static int GetFenceLength(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3)
        {
            return 0;
        }

        int index = scan.CharacterIndex;
        while (index < line.Text.Length && line.Text[index] == '?')
        {
            index = index + 1;
        }

        return index - scan.CharacterIndex;
    }

    internal static bool IsClosingLine(MarkdownLine line, int fenceLength)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (fenceLength < 3)
        {
            throw new ArgumentOutOfRangeException("fenceLength");
        }

        int length = GetFenceLength(line);
        if (length < fenceLength)
        {
            return false;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int index = scan.CharacterIndex + length;
        while (index < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            index = index + 1;
        }

        return index == line.Text.Length;
    }
}
