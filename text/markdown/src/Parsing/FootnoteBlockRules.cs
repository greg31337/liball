using System;
using System.Collections.Generic;

namespace markdown;

internal class FootnoteBlockRules
{
    internal const string DefinitionRuleName = "footnote.definition";

    internal static void RegisterFootnoteBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(DefinitionRuleName))
        {
            return;
        }

        int linkReferenceIndex = blockRules.IndexOf(CommonMarkBlockRules.LinkReferenceDefinitionRuleName);
        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (linkReferenceIndex >= 0)
        {
            blockRules.Insert(linkReferenceIndex, DefinitionRuleName, CreateDefinitionRule());
        }
        else if (paragraphIndex >= 0)
        {
            blockRules.Insert(paragraphIndex, DefinitionRuleName, CreateDefinitionRule());
        }
        else
        {
            blockRules.Add(DefinitionRuleName, CreateDefinitionRule());
        }
    }

    internal static MarkdownBlockRule CreateDefinitionRule()
    {
        return new MarkdownBlockRule(DefinitionRuleName, TryParseFootnoteDefinition);
    }

    internal static bool TryParseFootnoteDefinition(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || !IsFootnoteDefinitionLine(firstLine))
        {
            return false;
        }

        string label = GetFootnoteDefinitionLabel(firstLine);
        List<string> contentLines = new List<string>();
        contentLines.Add(GetFootnoteDefinitionContent(firstLine));
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

            if (CommonMarkBlockRules.GetLineIndentColumn(line) < 4)
            {
                break;
            }

            contentLines.Add(CommonMarkBlockRules.RemoveOptionalIndentColumns(line, 4));
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        MarkdownNode definition = MarkdownNode.Create(
            MarkdownNodeKind.FootnoteDefinition,
            MarkdownSourceSpan.Create(start, end));
        definition.SetAttribute("label", label);
        definition.SetAttribute("normalizedLabel", MarkdownFootnoteDefinitionMap.NormalizeLabel(label));
        definition.SetLiteral(CommonMarkBlockRules.JoinLines(contentLines));

        MarkdownDocument contentDocument = MarkdownParser.Parse(definition.Literal, context.ParseContext.Pipeline);
        while (contentDocument.Root.GetChildCount() > 0)
        {
            MarkdownNode child = contentDocument.Root.GetChild(0);
            definition.AppendChild(child);
        }

        context.ParseContext.Document.Root.AppendChild(definition);
        context.ParseContext.Footnotes.Add(definition);
        context.NoteContentLine();
        return true;
    }

    internal static bool IsFootnoteDefinitionLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetFootnoteDefinitionLabel(line).Length > 0;
    }

    internal static string GetFootnoteDefinitionLabel(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerStart = GetFootnoteDefinitionMarkerStart(line);
        if (markerStart < 0)
        {
            return "";
        }

        int labelStart = markerStart + 2;
        int index = labelStart;
        while (index < line.Text.Length)
        {
            if (line.Text[index] == ']')
            {
                if (index + 1 < line.Text.Length && line.Text[index + 1] == ':' && index > labelStart)
                {
                    return line.Text.Substring(labelStart, index - labelStart);
                }

                return "";
            }

            index = index + 1;
        }

        return "";
    }

    internal static string GetFootnoteDefinitionContent(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerEnd = GetFootnoteDefinitionMarkerEnd(line);
        if (markerEnd < 0)
        {
            return "";
        }

        int index = markerEnd;
        while (index < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            index = index + 1;
        }

        return line.Text.Substring(index);
    }

    internal static int GetFootnoteDefinitionMarkerStart(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || line.Text.Length - scan.CharacterIndex <= 2)
        {
            return -1;
        }

        if (line.Text[scan.CharacterIndex] == '[' && line.Text[scan.CharacterIndex + 1] == '^')
        {
            return scan.CharacterIndex;
        }

        return -1;
    }

    internal static int GetFootnoteDefinitionMarkerEnd(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerStart = GetFootnoteDefinitionMarkerStart(line);
        if (markerStart < 0)
        {
            return -1;
        }

        int index = markerStart + 2;
        while (index + 1 < line.Text.Length)
        {
            if (line.Text[index] == ']' && line.Text[index + 1] == ':')
            {
                if (index > markerStart + 2)
                {
                    return index + 2;
                }

                return -1;
            }

            index = index + 1;
        }

        return -1;
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
