using System;
using System.Collections.Generic;

namespace markdown;

internal class MathBlockRules
{
    internal const string MathBlockRuleName = "math.block";

    internal static void RegisterMathBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(MathBlockRuleName))
        {
            return;
        }

        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (paragraphIndex < 0)
        {
            blockRules.Add(MathBlockRuleName, CreateMathBlockRule());
        }
        else
        {
            blockRules.Insert(paragraphIndex, MathBlockRuleName, CreateMathBlockRule());
        }
    }

    internal static MarkdownBlockRule CreateMathBlockRule()
    {
        return new MarkdownBlockRule(MathBlockRuleName, TryParseMathBlock);
    }

    internal static bool TryParseMathBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? openingLine = context.GetCurrentLine();
        if (openingLine == null || !IsMathBlockDelimiterLine(openingLine))
        {
            return false;
        }

        int closingIndex = FindClosingDelimiterIndex(context);
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
        MarkdownNode mathBlock = MarkdownNode.Create(
            MarkdownNodeKind.MathBlock,
            MarkdownSourceSpan.Create(openingLine.ContentSpan.Start.Copy(), closingLine.ContentSpan.End.Copy()));
        mathBlock.SetLiteral(CommonMarkBlockRules.JoinLines(lines));
        mathBlock.SetAttribute("delimiter", "$$");

        context.ParseContext.Document.Root.AppendChild(mathBlock);
        context.LineIndex = closingIndex + 1;
        context.NoteContentLine();
        return true;
    }

    internal static int FindClosingDelimiterIndex(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        int index = context.LineIndex + 1;
        while (index < context.Lines.Length)
        {
            if (IsMathBlockDelimiterLine(context.Lines[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsMathBlockDelimiterLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || line.Text.Length - scan.CharacterIndex <= 1)
        {
            return false;
        }

        if (line.Text[scan.CharacterIndex] != '$' || line.Text[scan.CharacterIndex + 1] != '$')
        {
            return false;
        }

        int index = scan.CharacterIndex + 2;
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
}
