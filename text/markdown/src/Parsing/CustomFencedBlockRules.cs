using System;
using System.Collections.Generic;

namespace markdown;

internal class CustomFencedBlockRules
{
    internal const string CustomFencedBlockRuleName = "custom-fenced.block";

    internal static void RegisterCustomFencedBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(CustomFencedBlockRuleName))
        {
            return;
        }

        int diagramIndex = blockRules.IndexOf(DiagramBlockRules.DiagramBlockRuleName);
        int fencedIndex = blockRules.IndexOf(CommonMarkBlockRules.FencedCodeBlockRuleName);
        if (diagramIndex >= 0)
        {
            blockRules.Insert(diagramIndex, CustomFencedBlockRuleName, CreateCustomFencedBlockRule());
        }
        else if (fencedIndex >= 0)
        {
            blockRules.Insert(fencedIndex, CustomFencedBlockRuleName, CreateCustomFencedBlockRule());
        }
        else
        {
            blockRules.Add(CustomFencedBlockRuleName, CreateCustomFencedBlockRule());
        }
    }

    internal static MarkdownBlockRule CreateCustomFencedBlockRule()
    {
        return new MarkdownBlockRule(CustomFencedBlockRuleName, TryParseCustomFencedBlock);
    }

    internal static bool TryParseCustomFencedBlock(MarkdownBlockParseContext context)
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
        string infoKey = HtmlRenderer.GetCodeInfoLanguage(info).ToLowerInvariant();
        if (infoKey.Length > 0 && !IsValidInfoKey(infoKey))
        {
            return false;
        }

        if (infoKey.Length == 0 || !context.ParseContext.Pipeline.GetOptions().CustomFencedBlocks.Contains(infoKey))
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

        MarkdownNode custom = MarkdownNode.Create(MarkdownNodeKind.CustomFencedBlock, MarkdownSourceSpan.Create(start, end));
        custom.SetLiteral(CommonMarkBlockRules.JoinLines(lines));
        custom.SetAttribute("info", info);
        custom.SetAttribute("infoKey", infoKey);
        context.ParseContext.Document.Root.AppendChild(custom);
        context.NoteContentLine();
        return true;
    }

    internal static bool IsValidInfoKey(string infoKey)
    {
        if (infoKey == null)
        {
            throw new ArgumentNullException("infoKey");
        }

        return EmojiInlineRules.IsValidEmojiName(infoKey);
    }
}
