using System;
using System.Collections.Generic;

namespace markdown;

internal class DiagramBlockRules
{
    internal const string DiagramBlockRuleName = "diagram.block";

    internal static void RegisterDiagramBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(DiagramBlockRuleName))
        {
            return;
        }

        int fencedIndex = blockRules.IndexOf(CommonMarkBlockRules.FencedCodeBlockRuleName);
        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (fencedIndex >= 0)
        {
            blockRules.Insert(fencedIndex, DiagramBlockRuleName, CreateDiagramBlockRule());
        }
        else if (paragraphIndex >= 0)
        {
            blockRules.Insert(paragraphIndex, DiagramBlockRuleName, CreateDiagramBlockRule());
        }
        else
        {
            blockRules.Add(DiagramBlockRuleName, CreateDiagramBlockRule());
        }
    }

    internal static MarkdownBlockRule CreateDiagramBlockRule()
    {
        return new MarkdownBlockRule(DiagramBlockRuleName, TryParseDiagramBlock);
    }

    internal static bool TryParseDiagramBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? openingLine = context.GetCurrentLine();
        if (openingLine == null || !CommonMarkBlockRules.IsFencedCodeBlockOpeningLine(openingLine))
        {
            return false;
        }

        string info = CommonMarkBlockRules.GetFencedCodeBlockInfoString(openingLine);
        string diagramType = GetDiagramType(info);
        if (diagramType.Length == 0)
        {
            return false;
        }

        char marker = CommonMarkBlockRules.GetFencedCodeBlockFenceMarker(openingLine);
        int fenceLength = CommonMarkBlockRules.GetFencedCodeBlockFenceLength(openingLine);
        int fenceIndent = CommonMarkBlockRules.GetFencedCodeBlockIndent(openingLine);
        MarkdownSourcePosition start = openingLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = openingLine.ContentSpan.End.Copy();
        List<string> lines = new List<string>();

        context.AdvanceLine();
        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null)
            {
                break;
            }

            if (CommonMarkBlockRules.IsFencedCodeBlockClosingLine(line, marker, fenceLength))
            {
                end = line.ContentSpan.End.Copy();
                context.AdvanceLine();
                break;
            }

            lines.Add(CommonMarkBlockRules.RemoveOptionalIndentColumns(line, fenceIndent));
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        MarkdownNode diagram = MarkdownNode.Create(
            MarkdownNodeKind.DiagramBlock,
            MarkdownSourceSpan.Create(start, end));
        diagram.SetLiteral(CommonMarkBlockRules.JoinLines(lines));
        diagram.SetAttribute("info", info);
        diagram.SetAttribute("diagramType", diagramType);

        context.ParseContext.Document.Root.AppendChild(diagram);
        context.NoteContentLine();
        return true;
    }

    internal static string GetDiagramType(string info)
    {
        if (info == null)
        {
            throw new ArgumentNullException("info");
        }

        string language = HtmlRenderer.GetCodeInfoLanguage(info).ToLowerInvariant();
        if (IsDiagramType(language))
        {
            return language;
        }

        return "";
    }

    internal static bool IsDiagramType(string language)
    {
        if (language == null)
        {
            throw new ArgumentNullException("language");
        }

        string normalized = language.ToLowerInvariant();
        return normalized == "mermaid"
            || normalized == "plantuml"
            || normalized == "dot"
            || normalized == "graphviz"
            || normalized == "d2";
    }
}
