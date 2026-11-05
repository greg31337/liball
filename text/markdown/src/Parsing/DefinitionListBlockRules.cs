using System;
using System.Collections.Generic;

namespace markdown;

internal class DefinitionListBlockRules
{
    internal const string DefinitionListRuleName = "definition-list";

    internal static void RegisterDefinitionListBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(DefinitionListRuleName))
        {
            return;
        }

        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (paragraphIndex < 0)
        {
            blockRules.Add(DefinitionListRuleName, CreateDefinitionListRule());
        }
        else
        {
            blockRules.Insert(paragraphIndex, DefinitionListRuleName, CreateDefinitionListRule());
        }
    }

    internal static MarkdownBlockRule CreateDefinitionListRule()
    {
        return new MarkdownBlockRule(DefinitionListRuleName, TryParseDefinitionList);
    }

    internal static bool TryParseDefinitionList(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!IsDefinitionListStart(context))
        {
            return false;
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null)
        {
            return false;
        }

        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();
        MarkdownNode list = MarkdownNode.Create(MarkdownNodeKind.DefinitionList);

        while (context.HasCurrentLine() && IsDefinitionListStart(context))
        {
            MarkdownLine? termLine = context.GetCurrentLine();
            if (termLine == null)
            {
                break;
            }

            MarkdownNode term = MarkdownNode.Create(MarkdownNodeKind.DefinitionTerm, termLine.ContentSpan);
            term.SetLiteral(termLine.Text);
            list.AppendChild(term);
            end = termLine.ContentSpan.End.Copy();
            context.AdvanceLine();

            while (context.HasCurrentLine())
            {
                MarkdownLine? descriptionLine = context.GetCurrentLine();
                if (descriptionLine == null || !IsDefinitionDescriptionLine(descriptionLine))
                {
                    break;
                }

                MarkdownNode description = ParseDefinitionDescription(context);
                list.AppendChild(description);
                end = description.Span.End.Copy();
            }
        }

        list.Span = MarkdownSourceSpan.Create(start, end);
        context.ParseContext.Document.Root.AppendChild(list);
        context.NoteContentLine();
        return true;
    }

    internal static MarkdownNode ParseDefinitionDescription(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || !IsDefinitionDescriptionLine(firstLine))
        {
            throw new ArgumentException("Current line is not a definition description line.");
        }

        List<string> contentLines = new List<string>();
        contentLines.Add(GetDefinitionDescriptionContent(firstLine));
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
                if (!HasIndentedContinuationAfterBlank(context))
                {
                    break;
                }

                contentLines.Add("");
                end = line.ContentSpan.End.Copy();
                context.AdvanceLine();
                continue;
            }

            if (IsDefinitionDescriptionLine(line) || IsDefinitionListStart(context))
            {
                break;
            }

            if (CommonMarkBlockRules.GetLineIndentColumn(line) < 4)
            {
                break;
            }

            contentLines.Add(CommonMarkBlockRules.RemoveOptionalIndentColumns(line, 4));
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        MarkdownNode description = MarkdownNode.Create(
            MarkdownNodeKind.DefinitionDescription,
            MarkdownSourceSpan.Create(start, end));
        description.SetLiteral(CommonMarkBlockRules.JoinLines(contentLines));

        MarkdownDocument contentDocument = MarkdownParser.Parse(description.Literal, context.ParseContext.Pipeline);
        while (contentDocument.Root.GetChildCount() > 0)
        {
            MarkdownNode child = contentDocument.Root.GetChild(0);
            description.AppendChild(child);
        }

        return description;
    }

    internal static bool IsDefinitionListStart(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? line = context.GetCurrentLine();
        if (line == null || CommonMarkBlockRules.IsBlankLine(line) || IsDefinitionDescriptionLine(line))
        {
            return false;
        }

        int nextIndex = context.LineIndex + 1;
        if (nextIndex >= context.Lines.Length)
        {
            return false;
        }

        return IsDefinitionDescriptionLine(context.Lines[nextIndex]);
    }

    internal static bool IsDefinitionDescriptionLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetDefinitionDescriptionMarkerStart(line) >= 0;
    }

    internal static int GetDefinitionDescriptionMarkerStart(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return -1;
        }

        if (line.Text[scan.CharacterIndex] == ':')
        {
            return scan.CharacterIndex;
        }

        return -1;
    }

    internal static string GetDefinitionDescriptionContent(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerStart = GetDefinitionDescriptionMarkerStart(line);
        if (markerStart < 0)
        {
            return "";
        }

        int index = markerStart + 1;
        while (index < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            index = index + 1;
        }

        return line.Text.Substring(index);
    }

    internal static bool HasIndentedContinuationAfterBlank(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        int index = context.LineIndex + 1;
        while (index < context.Lines.Length)
        {
            MarkdownLine line = context.Lines[index];
            if (CommonMarkBlockRules.IsBlankLine(line))
            {
                index = index + 1;
                continue;
            }

            return CommonMarkBlockRules.GetLineIndentColumn(line) >= 4;
        }

        return false;
    }
}
