using System;

namespace markdown;

internal class AbbreviationBlockRules
{
    internal const string DefinitionRuleName = "abbreviation.definition";

    internal static void RegisterAbbreviationBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(DefinitionRuleName))
        {
            return;
        }

        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (paragraphIndex < 0)
        {
            blockRules.Add(DefinitionRuleName, CreateDefinitionRule());
        }
        else
        {
            blockRules.Insert(paragraphIndex, DefinitionRuleName, CreateDefinitionRule());
        }
    }

    internal static MarkdownBlockRule CreateDefinitionRule()
    {
        return new MarkdownBlockRule(DefinitionRuleName, TryParseAbbreviationDefinition);
    }

    internal static bool TryParseAbbreviationDefinition(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? line = context.GetCurrentLine();
        if (line == null || !IsAbbreviationDefinitionLine(line))
        {
            return false;
        }

        string label = GetAbbreviationLabel(line);
        string title = GetAbbreviationTitle(line);
        MarkdownNode definition = MarkdownNode.Create(MarkdownNodeKind.AbbreviationDefinition, line.ContentSpan);
        definition.SetLiteral(line.Text);
        definition.SetAttribute("label", label);
        definition.SetAttribute("title", title);

        context.ParseContext.Document.Root.AppendChild(definition);
        context.ParseContext.Abbreviations.Add(definition);
        context.NoteContentLine();
        context.AdvanceLine();
        return true;
    }

    internal static bool IsAbbreviationDefinitionLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetAbbreviationLabel(line).Length > 0 && GetAbbreviationTitle(line).Length > 0;
    }

    internal static string GetAbbreviationLabel(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int start = GetAbbreviationMarkerStart(line);
        if (start < 0)
        {
            return "";
        }

        int labelStart = start + 2;
        int index = labelStart;
        while (index + 1 < line.Text.Length)
        {
            if (line.Text[index] == ']' && line.Text[index + 1] == ':')
            {
                if (index > labelStart)
                {
                    return line.Text.Substring(labelStart, index - labelStart);
                }

                return "";
            }

            index = index + 1;
        }

        return "";
    }

    internal static string GetAbbreviationTitle(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerEnd = GetAbbreviationMarkerEnd(line);
        if (markerEnd < 0)
        {
            return "";
        }

        int index = markerEnd;
        while (index < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            index = index + 1;
        }

        int end = line.Text.Length;
        while (end > index && MarkdownChar.IsSpaceOrTab(line.Text[end - 1]))
        {
            end = end - 1;
        }

        return line.Text.Substring(index, end - index);
    }

    internal static int GetAbbreviationMarkerStart(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || line.Text.Length - scan.CharacterIndex <= 3)
        {
            return -1;
        }

        if (line.Text[scan.CharacterIndex] == '*' && line.Text[scan.CharacterIndex + 1] == '[')
        {
            return scan.CharacterIndex;
        }

        return -1;
    }

    internal static int GetAbbreviationMarkerEnd(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int start = GetAbbreviationMarkerStart(line);
        if (start < 0)
        {
            return -1;
        }

        int index = start + 2;
        while (index + 1 < line.Text.Length)
        {
            if (line.Text[index] == ']' && line.Text[index + 1] == ':')
            {
                if (index > start + 2)
                {
                    return index + 2;
                }

                return -1;
            }

            index = index + 1;
        }

        return -1;
    }
}
