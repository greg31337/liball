using System;
using System.Collections.Generic;

namespace markdown;

internal class CommonMarkBlockRules
{
    internal const string BlankLineRuleName = "commonmark.blank-line";
    internal const string BlockQuoteRuleName = "commonmark.block-quote";
    internal const string BulletListRuleName = "commonmark.bullet-list";
    internal const string AtxHeadingRuleName = "commonmark.atx-heading";
    internal const string ThematicBreakRuleName = "commonmark.thematic-break";
    internal const string FencedCodeBlockRuleName = "commonmark.fenced-code-block";
    internal const string HtmlBlockRuleName = "commonmark.html-block";
    internal const string IndentedCodeBlockRuleName = "commonmark.indented-code-block";
    internal const string LinkReferenceDefinitionRuleName = "commonmark.link-reference-definition";
    internal const string OrderedListRuleName = "commonmark.ordered-list";
    internal const string ParagraphRuleName = "commonmark.paragraph";

    internal static void RegisterCommonMarkBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        blockRules.Set(BlankLineRuleName, CreateBlankLineRule());
        blockRules.Set(BlockQuoteRuleName, CreateBlockQuoteRule());
        blockRules.Set(BulletListRuleName, CreateBulletListRule());
        blockRules.Set(AtxHeadingRuleName, CreateAtxHeadingRule());
        blockRules.Set(ThematicBreakRuleName, CreateThematicBreakRule());
        blockRules.Set(FencedCodeBlockRuleName, CreateFencedCodeBlockRule());
        blockRules.Set(HtmlBlockRuleName, CreateHtmlBlockRule());
        blockRules.Set(IndentedCodeBlockRuleName, CreateIndentedCodeBlockRule());
        blockRules.Set(LinkReferenceDefinitionRuleName, CreateLinkReferenceDefinitionRule());
        blockRules.Set(OrderedListRuleName, CreateOrderedListRule());
        blockRules.Set(ParagraphRuleName, CreateParagraphRule());
    }

    internal static MarkdownBlockRule CreateBlankLineRule()
    {
        return new MarkdownBlockRule(BlankLineRuleName, TryParseBlankLine);
    }

    internal static MarkdownBlockRule CreateBlockQuoteRule()
    {
        return new MarkdownBlockRule(BlockQuoteRuleName, TryParseBlockQuote);
    }

    internal static MarkdownBlockRule CreateBulletListRule()
    {
        return new MarkdownBlockRule(BulletListRuleName, TryParseBulletList);
    }

    internal static MarkdownBlockRule CreateParagraphRule()
    {
        return new MarkdownBlockRule(ParagraphRuleName, TryParseParagraph);
    }

    internal static MarkdownBlockRule CreateAtxHeadingRule()
    {
        return new MarkdownBlockRule(AtxHeadingRuleName, TryParseAtxHeading);
    }

    internal static MarkdownBlockRule CreateThematicBreakRule()
    {
        return new MarkdownBlockRule(ThematicBreakRuleName, TryParseThematicBreak);
    }

    internal static MarkdownBlockRule CreateFencedCodeBlockRule()
    {
        return new MarkdownBlockRule(FencedCodeBlockRuleName, TryParseFencedCodeBlock);
    }

    internal static MarkdownBlockRule CreateHtmlBlockRule()
    {
        return new MarkdownBlockRule(HtmlBlockRuleName, TryParseHtmlBlock);
    }

    internal static MarkdownBlockRule CreateIndentedCodeBlockRule()
    {
        return new MarkdownBlockRule(IndentedCodeBlockRuleName, TryParseIndentedCodeBlock);
    }

    internal static MarkdownBlockRule CreateLinkReferenceDefinitionRule()
    {
        return new MarkdownBlockRule(LinkReferenceDefinitionRuleName, TryParseLinkReferenceDefinition);
    }

    internal static MarkdownBlockRule CreateOrderedListRule()
    {
        return new MarkdownBlockRule(OrderedListRuleName, TryParseOrderedList);
    }

    internal static bool TryParseBlankLine(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? line = context.GetCurrentLine();
        if (line == null)
        {
            return false;
        }

        if (!IsBlankLine(line))
        {
            return false;
        }

        context.NoteBlankLine(line);
        context.AdvanceLine();
        return true;
    }

    internal static bool TryParseBlockQuote(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || !IsBlockQuoteLine(firstLine))
        {
            return false;
        }

        List<string> innerLines = new List<string>();
        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();

        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null || !IsBlockQuoteLine(line))
            {
                if (line != null
                    && !IsBlankLine(line)
                    && !IsParagraphInterruptingBlockLine(line)
                    && CanContinueLazily(innerLines, context.ParseContext.Pipeline))
                {
                    if (IsSetextHeadingUnderline(line))
                    {
                        innerLines.Add(EscapeSetextHeadingUnderline(line));
                    }
                    else
                    {
                        innerLines.Add(line.Text);
                    }
                    end = line.ContentSpan.End.Copy();
                    context.AdvanceLine();
                    continue;
                }

                break;
            }

            innerLines.Add(RemoveBlockQuoteMarker(line));
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        MarkdownNode blockQuote = MarkdownNode.Create(
            MarkdownNodeKind.BlockQuote,
            MarkdownSourceSpan.Create(start, end));
        MarkdownDocument innerDocument = MarkdownParser.Parse(JoinLines(innerLines), context.ParseContext.Pipeline);
        while (innerDocument.Root.GetChildCount() > 0)
        {
            MarkdownNode child = innerDocument.Root.GetChild(0);
            if (child.Kind == MarkdownNodeKind.LinkReferenceDefinition)
            {
                context.ParseContext.LinkReferences.Add(child);
            }

            blockQuote.AppendChild(child);
        }

        context.ParseContext.Document.Root.AppendChild(blockQuote);
        context.NoteContentLine();
        return true;
    }

    internal static string EscapeSetextHeadingUnderline(MarkdownLine line)
    {
        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        return line.Text.Substring(0, scan.CharacterIndex)
            + "\\"
            + line.Text.Substring(scan.CharacterIndex);
    }

    internal static bool TryParseBulletList(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || !IsBulletListItemLine(firstLine))
        {
            return false;
        }

        string marker = GetBulletListItemMarker(firstLine);
        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();
        MarkdownNode list = MarkdownNode.Create(MarkdownNodeKind.List);
        list.SetAttribute("type", "bullet");
        list.SetAttribute("marker", marker);
        bool loose = false;

        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null
                || !IsBulletListItemLine(line)
                || GetBulletListItemMarker(line) != marker)
            {
                break;
            }

            MarkdownNode item = MarkdownNode.Create(MarkdownNodeKind.ListItem, line.ContentSpan);
            item.SetAttribute("marker", GetBulletListItemMarker(line));
            List<string> itemLines = new List<string>();
            itemLines.Add(RemoveBulletListItemMarker(line));
            int contentIndent = GetBulletListItemContentIndent(line);

            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
            while (context.HasCurrentLine())
            {
                MarkdownLine? continuationLine = context.GetCurrentLine();
                if (continuationLine == null)
                {
                    break;
                }

                if (IsBlankLine(continuationLine))
                {
                    if (itemLines.Count == 1
                        && !HasNonBlankText(itemLines[0])
                        && !IsBlankFollowedByBulletListItem(context, marker))
                    {
                        break;
                    }

                    if (!ShouldConsumeListItemBlankLine(
                        context,
                        contentIndent,
                        false,
                        marker,
                        GetBulletListItemMarkerColumn(firstLine)))
                    {
                        break;
                    }

                    itemLines.Add("");
                    end = continuationLine.ContentSpan.End.Copy();
                    context.AdvanceLine();
                    continue;
                }

                if (IsBulletListItemLine(continuationLine)
                    && GetBulletListItemMarkerColumn(continuationLine) < contentIndent)
                {
                    break;
                }

                if (GetLineIndentColumn(continuationLine) < contentIndent)
                {
                    if (IsParagraphInterruptingBlockLine(continuationLine))
                    {
                        break;
                    }

                    if (CanContinueLazily(itemLines, context.ParseContext.Pipeline))
                    {
                        itemLines.Add(continuationLine.Text);
                        end = continuationLine.ContentSpan.End.Copy();
                        context.AdvanceLine();
                        continue;
                    }

                    break;
                }

                itemLines.Add(RemoveOptionalIndentColumns(continuationLine, contentIndent));
                end = continuationLine.ContentSpan.End.Copy();
                context.AdvanceLine();
            }

            GfmBlockRules.TryApplyTaskListMarker(item, itemLines, context.ParseContext.Pipeline);
            MarkdownDocument itemDocument = MarkdownParser.Parse(JoinLines(itemLines), context.ParseContext.Pipeline);
            if (HasLooseListItemSpacing(itemLines, context.ParseContext.Pipeline)
                || itemLines.Count > 1
                    && EndsWithBlankLine(itemLines)
                    && IsCurrentBulletListItem(context, marker))
            {
                loose = true;
            }

            while (itemDocument.Root.GetChildCount() > 0)
            {
                MarkdownNode child = itemDocument.Root.GetChild(0);
                item.AppendChild(child);
            }

            list.AppendChild(item);
        }

        if (loose)
        {
            list.SetAttribute("tight", "false");
            list.SetAttribute("loose", "true");
        }
        else
        {
            list.SetAttribute("tight", "true");
            list.SetAttribute("loose", "false");
        }

        list.Span = MarkdownSourceSpan.Create(start, end);
        context.ParseContext.Document.Root.AppendChild(list);
        context.NoteContentLine();
        return true;
    }

    internal static bool TryParseFencedCodeBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? openingLine = context.GetCurrentLine();
        if (openingLine == null || !IsFencedCodeBlockOpeningLine(openingLine))
        {
            return false;
        }

        char marker = GetFencedCodeBlockFenceMarker(openingLine);
        int fenceLength = GetFencedCodeBlockFenceLength(openingLine);
        int fenceIndent = GetFencedCodeBlockIndent(openingLine);
        string infoString = CommonMarkInlineRules.DecodeLinkAttributeText(
            GetFencedCodeBlockInfoString(openingLine));
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

            if (IsFencedCodeBlockClosingLine(line, marker, fenceLength))
            {
                end = line.ContentSpan.End.Copy();
                context.AdvanceLine();
                break;
            }

            lines.Add(RemoveOptionalIndentColumns(line, fenceIndent));
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        MarkdownNode codeBlock = MarkdownNode.Create(
            MarkdownNodeKind.FencedCodeBlock,
            MarkdownSourceSpan.Create(start, end));
        codeBlock.SetLiteral(JoinLines(lines));
        codeBlock.SetAttribute("fenceMarker", marker.ToString());
        codeBlock.SetAttribute("fenceLength", fenceLength.ToString());
        if (infoString.Length > 0)
        {
            codeBlock.SetAttribute("info", infoString);
        }

        context.ParseContext.Document.Root.AppendChild(codeBlock);
        context.NoteContentLine();
        return true;
    }

    internal static bool TryParseHtmlBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null)
        {
            return false;
        }

        int blockType = GetHtmlBlockType(firstLine);
        if (blockType == 0)
        {
            return false;
        }

        string type1TagName = GetHtmlBlockType1TagName(firstLine);
        List<string> lines = new List<string>();
        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();

        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null)
            {
                break;
            }

            if (blockType == 6 || blockType == 7)
            {
                if (IsBlankLine(line))
                {
                    break;
                }
            }

            lines.Add(line.Text);
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();

            if (blockType >= 1 && blockType <= 5 && IsHtmlBlockEndLine(line, blockType, type1TagName))
            {
                break;
            }
        }

        MarkdownNode htmlBlock = MarkdownNode.Create(
            MarkdownNodeKind.HtmlBlock,
            MarkdownSourceSpan.Create(start, end));
        htmlBlock.SetLiteral(JoinLines(lines));
        context.ParseContext.Document.Root.AppendChild(htmlBlock);
        context.NoteContentLine();
        return true;
    }

    internal static bool TryParseIndentedCodeBlock(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || !IsIndentedCodeLine(firstLine))
        {
            return false;
        }

        List<string> lines = new List<string>();
        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();

        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null)
            {
                break;
            }

            if (IsIndentedCodeLine(line))
            {
                lines.Add(RemoveIndentedCodePrefix(line));
                end = line.ContentSpan.End.Copy();
                context.AdvanceLine();
            }
            else if (IsBlankLine(line))
            {
                lines.Add(RemoveIndentedCodePrefix(line));
                end = line.ContentSpan.End.Copy();
                context.AdvanceLine();
            }
            else
            {
                break;
            }
        }

        while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        MarkdownNode codeBlock = MarkdownNode.Create(
            MarkdownNodeKind.IndentedCodeBlock,
            MarkdownSourceSpan.Create(start, end));
        codeBlock.SetLiteral(JoinLines(lines));
        context.ParseContext.Document.Root.AppendChild(codeBlock);
        context.NoteContentLine();
        return true;
    }

    internal static bool TryParseLinkReferenceDefinition(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? line = context.GetCurrentLine();
        if (line == null)
        {
            return false;
        }

        int availableLineCount = 0;
        while (context.LineIndex + availableLineCount < context.Lines.Length
            && !IsBlankLine(context.Lines[context.LineIndex + availableLineCount]))
        {
            availableLineCount = availableLineCount + 1;
        }

        MarkdownLine? definitionLine = null;
        int consumedLineCount = availableLineCount;
        while (consumedLineCount > 0 && definitionLine == null)
        {
            MarkdownLine candidate = CreateLinkReferenceDefinitionCandidate(context, consumedLineCount);
            if (IsLinkReferenceDefinitionLine(candidate))
            {
                definitionLine = candidate;
            }
            else
            {
                consumedLineCount = consumedLineCount - 1;
            }
        }

        if (definitionLine == null)
        {
            return false;
        }

        string label = GetLinkReferenceDefinitionLabel(definitionLine);
        string destination = GetLinkReferenceDefinitionDestination(definitionLine);
        string title = GetLinkReferenceDefinitionTitle(definitionLine);

        MarkdownNode definition = MarkdownNode.Create(MarkdownNodeKind.LinkReferenceDefinition, definitionLine.ContentSpan);
        definition.SetLiteral(definitionLine.Text);
        definition.SetAttribute("label", label);
        definition.SetAttribute("normalizedLabel", NormalizeLinkReferenceLabel(label));
        definition.SetAttribute("destination", destination);
        if (title.Length > 0)
        {
            definition.SetAttribute("title", title);
        }

        context.ParseContext.Document.Root.AppendChild(definition);
        context.ParseContext.LinkReferences.Add(definition);
        context.NoteContentLine();
        int advanceIndex = 0;
        while (advanceIndex < consumedLineCount)
        {
            context.AdvanceLine();
            advanceIndex = advanceIndex + 1;
        }
        return true;
    }

    internal static MarkdownLine CreateLinkReferenceDefinitionCandidate(
        MarkdownBlockParseContext context,
        int lineCount)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (lineCount < 1 || context.LineIndex + lineCount > context.Lines.Length)
        {
            throw new ArgumentOutOfRangeException("lineCount");
        }

        MarkdownLine firstLine = context.Lines[context.LineIndex];
        MarkdownLine lastLine = context.Lines[context.LineIndex + lineCount - 1];
        string text = "";
        int index = 0;
        while (index < lineCount)
        {
            if (index > 0)
            {
                text = text + "\n";
            }

            text = text + context.Lines[context.LineIndex + index].Text;
            index = index + 1;
        }

        MarkdownSourceSpan span = MarkdownSourceSpan.Create(
            firstLine.ContentSpan.Start.Copy(),
            lastLine.ContentSpan.End.Copy());
        return MarkdownLine.Create(firstLine.LineNumber, text, "", span, span);
    }

    internal static bool TryParseOrderedList(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || !IsOrderedListItemLine(firstLine))
        {
            return false;
        }

        string marker = GetOrderedListItemMarker(firstLine);
        int startNumber = GetOrderedListItemNumber(firstLine);
        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();
        MarkdownNode list = MarkdownNode.Create(MarkdownNodeKind.List);
        list.SetAttribute("type", "ordered");
        list.SetAttribute("start", startNumber.ToString());
        list.SetAttribute("marker", marker);
        bool loose = false;

        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null
                || !IsOrderedListItemLine(line)
                || GetOrderedListItemMarker(line) != marker)
            {
                break;
            }

            MarkdownNode item = MarkdownNode.Create(MarkdownNodeKind.ListItem, line.ContentSpan);
            item.SetAttribute("number", GetOrderedListItemNumber(line).ToString());
            item.SetAttribute("marker", GetOrderedListItemMarker(line));
            List<string> itemLines = new List<string>();
            itemLines.Add(RemoveOrderedListItemMarker(line));
            int contentIndent = GetOrderedListItemContentIndent(line);

            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
            while (context.HasCurrentLine())
            {
                MarkdownLine? continuationLine = context.GetCurrentLine();
                if (continuationLine == null)
                {
                    break;
                }

                if (IsBlankLine(continuationLine))
                {
                    if (itemLines.Count == 1
                        && !HasNonBlankText(itemLines[0])
                        && !IsBlankFollowedByOrderedListItem(context, marker))
                    {
                        break;
                    }

                    if (!ShouldConsumeListItemBlankLine(
                        context,
                        contentIndent,
                        true,
                        marker,
                        GetOrderedListItemMarkerColumn(firstLine)))
                    {
                        break;
                    }

                    itemLines.Add("");
                    end = continuationLine.ContentSpan.End.Copy();
                    context.AdvanceLine();
                    continue;
                }

                if (IsOrderedListItemLine(continuationLine)
                    && GetOrderedListItemMarkerColumn(continuationLine) < contentIndent)
                {
                    break;
                }

                if (GetLineIndentColumn(continuationLine) < contentIndent)
                {
                    if (IsParagraphInterruptingBlockLine(continuationLine))
                    {
                        break;
                    }

                    if (CanContinueLazily(itemLines, context.ParseContext.Pipeline))
                    {
                        itemLines.Add(continuationLine.Text);
                        end = continuationLine.ContentSpan.End.Copy();
                        context.AdvanceLine();
                        continue;
                    }

                    break;
                }

                itemLines.Add(RemoveOptionalIndentColumns(continuationLine, contentIndent));
                end = continuationLine.ContentSpan.End.Copy();
                context.AdvanceLine();
            }

            GfmBlockRules.TryApplyTaskListMarker(item, itemLines, context.ParseContext.Pipeline);
            MarkdownDocument itemDocument = MarkdownParser.Parse(JoinLines(itemLines), context.ParseContext.Pipeline);
            if (HasLooseListItemSpacing(itemLines, context.ParseContext.Pipeline)
                || itemLines.Count > 1
                    && EndsWithBlankLine(itemLines)
                    && IsCurrentOrderedListItem(context, marker))
            {
                loose = true;
            }

            while (itemDocument.Root.GetChildCount() > 0)
            {
                MarkdownNode child = itemDocument.Root.GetChild(0);
                item.AppendChild(child);
            }

            list.AppendChild(item);
        }

        if (loose)
        {
            list.SetAttribute("tight", "false");
            list.SetAttribute("loose", "true");
        }
        else
        {
            list.SetAttribute("tight", "true");
            list.SetAttribute("loose", "false");
        }

        list.Span = MarkdownSourceSpan.Create(start, end);
        context.ParseContext.Document.Root.AppendChild(list);
        context.NoteContentLine();
        return true;
    }

    internal static bool TryParseParagraph(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? firstLine = context.GetCurrentLine();
        if (firstLine == null || IsBlankLine(firstLine))
        {
            return false;
        }

        string literal = "";
        MarkdownSourcePosition start = firstLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = firstLine.ContentSpan.End.Copy();

        while (context.HasCurrentLine())
        {
            MarkdownLine? line = context.GetCurrentLine();
            if (line == null || IsBlankLine(line))
            {
                break;
            }

            int setextLevel = GetSetextHeadingLevel(line);
            if (literal.Length > 0 && setextLevel > 0)
            {
                MarkdownNode heading = MarkdownNode.Create(
                    MarkdownNodeKind.Heading,
                    MarkdownSourceSpan.Create(start, line.ContentSpan.End.Copy()));
                heading.SetLiteral(literal.TrimEnd(' ', '\t'));
                heading.SetAttribute("level", setextLevel.ToString());
                HeadingAttributeParser.TryApplyHeadingAttributes(heading, context.ParseContext.Pipeline);
                context.ParseContext.Document.Root.AppendChild(heading);
                context.NoteContentLine();
                context.AdvanceLine();
                return true;
            }

            if (literal.Length > 0
                && (IsParagraphInterruptingBlockLine(line)
                    || GenericAttributeRules.IsStandaloneAttributeLine(
                        line,
                        context.ParseContext.Pipeline)))
            {
                break;
            }

            if (literal.Length > 0)
            {
                literal = literal + "\n";
            }

            string paragraphLine = line.Text;
            if (literal.Length == 0)
            {
                paragraphLine = RemoveOptionalIndentColumns(line, 3);
            }
            else
            {
                paragraphLine = paragraphLine.TrimStart(' ', '\t');
            }

            literal = literal + paragraphLine;
            end = line.ContentSpan.End.Copy();
            context.AdvanceLine();
        }

        literal = literal.TrimEnd(' ', '\t');
        MarkdownNode paragraph = MarkdownNode.Create(
            MarkdownNodeKind.Paragraph,
            MarkdownSourceSpan.Create(start, end));
        paragraph.SetLiteral(literal);
        context.ParseContext.Document.Root.AppendChild(paragraph);
        context.NoteContentLine();
        return true;
    }

    internal static bool TryParseAtxHeading(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? line = context.GetCurrentLine();
        if (line == null)
        {
            return false;
        }

        int level = GetAtxHeadingLevel(line);
        if (level == 0)
        {
            return false;
        }

        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading, line.ContentSpan);
        heading.SetLiteral(GetAtxHeadingText(line));
        heading.SetAttribute("level", level.ToString());
        HeadingAttributeParser.TryApplyHeadingAttributes(heading, context.ParseContext.Pipeline);
        context.ParseContext.Document.Root.AppendChild(heading);
        context.NoteContentLine();
        context.AdvanceLine();
        return true;
    }

    internal static int GetAtxHeadingLevel(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return 0;
        }

        int index = scan.CharacterIndex;
        int level = 0;
        while (index < line.Text.Length && line.Text[index] == '#')
        {
            level = level + 1;
            index = index + 1;
        }

        if (level < 1 || level > 6)
        {
            return 0;
        }

        if (index < line.Text.Length && !MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            return 0;
        }

        return level;
    }

    internal static string GetAtxHeadingText(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int level = GetAtxHeadingLevel(line);
        if (level == 0)
        {
            return "";
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int index = scan.CharacterIndex + level;
        bool hadOpeningSpace = false;

        while (index < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[index]))
        {
            hadOpeningSpace = true;
            index = index + 1;
        }

        int contentStart = index;
        int contentEnd = line.Text.Length;
        while (contentEnd > contentStart && MarkdownChar.IsSpaceOrTab(line.Text[contentEnd - 1]))
        {
            contentEnd = contentEnd - 1;
        }

        if (contentEnd > contentStart && line.Text[contentEnd - 1] == '#')
        {
            int closingStart = contentEnd - 1;
            while (closingStart > contentStart && line.Text[closingStart - 1] == '#')
            {
                closingStart = closingStart - 1;
            }

            bool hasClosingPrefix = false;
            if (closingStart == contentStart)
            {
                hasClosingPrefix = hadOpeningSpace;
            }
            else if (MarkdownChar.IsSpaceOrTab(line.Text[closingStart - 1]))
            {
                hasClosingPrefix = true;
            }

            if (hasClosingPrefix)
            {
                contentEnd = closingStart;
                while (contentEnd > contentStart && MarkdownChar.IsSpaceOrTab(line.Text[contentEnd - 1]))
                {
                    contentEnd = contentEnd - 1;
                }
            }
        }

        return line.Text.Substring(contentStart, contentEnd - contentStart);
    }

    internal static int GetSetextHeadingLevel(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return 0;
        }

        char marker = line.Text[scan.CharacterIndex];
        if (marker != '=' && marker != '-')
        {
            return 0;
        }

        int index = scan.CharacterIndex;
        while (index < line.Text.Length && line.Text[index] == marker)
        {
            index = index + 1;
        }

        while (index < line.Text.Length)
        {
            if (!MarkdownChar.IsSpaceOrTab(line.Text[index]))
            {
                return 0;
            }

            index = index + 1;
        }

        if (marker == '=')
        {
            return 1;
        }

        return 2;
    }

    internal static bool IsSetextHeadingUnderline(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetSetextHeadingLevel(line) > 0;
    }

    internal static bool IsBlockQuoteLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return false;
        }

        return line.Text[scan.CharacterIndex] == '>';
    }

    internal static string RemoveBlockQuoteMarker(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (!IsBlockQuoteLine(line))
        {
            return line.Text;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int index = scan.CharacterIndex + 1;
        if (index < line.Text.Length && MarkdownChar.IsSpace(line.Text[index]))
        {
            index = index + 1;
        }
        else if (index < line.Text.Length && MarkdownChar.IsTab(line.Text[index]))
        {
            int tabWidth = MarkdownIndentation.GetColumnsToNextTabStop(scan.Column + 1);
            int remainingColumns = tabWidth - 1;
            int sourceColumn = scan.Column + 1 + tabWidth;
            int remainingIndex = index + 1;
            while (remainingIndex < line.Text.Length
                && MarkdownChar.IsSpaceOrTab(line.Text[remainingIndex]))
            {
                int followingColumn = MarkdownIndentation.AdvanceColumn(
                    sourceColumn,
                    line.Text[remainingIndex]);
                remainingColumns = remainingColumns + followingColumn - sourceColumn;
                sourceColumn = followingColumn;
                remainingIndex = remainingIndex + 1;
            }

            return new string(' ', remainingColumns) + line.Text.Substring(remainingIndex);
        }

        return line.Text.Substring(index);
    }

    internal static bool IsFencedCodeBlockOpeningLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetFencedCodeBlockFenceLength(line) >= 3;
    }

    internal static char GetFencedCodeBlockFenceMarker(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return '\0';
        }

        char marker = line.Text[scan.CharacterIndex];
        if (marker == '`' || marker == '~')
        {
            return marker;
        }

        return '\0';
    }

    internal static int GetFencedCodeBlockFenceLength(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        char marker = GetFencedCodeBlockFenceMarker(line);
        if (marker == '\0')
        {
            return 0;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int index = scan.CharacterIndex;
        int length = 0;
        while (index < line.Text.Length && line.Text[index] == marker)
        {
            length = length + 1;
            index = index + 1;
        }

        if (length < 3)
        {
            return 0;
        }

        if (marker == '`')
        {
            while (index < line.Text.Length)
            {
                if (line.Text[index] == '`')
                {
                    return 0;
                }

                index = index + 1;
            }
        }

        return length;
    }

    internal static int GetFencedCodeBlockIndent(MarkdownLine line)
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

        return scan.Column;
    }

    internal static string GetFencedCodeBlockInfoString(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int fenceLength = GetFencedCodeBlockFenceLength(line);
        if (fenceLength == 0)
        {
            return "";
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int start = scan.CharacterIndex + fenceLength;
        while (start < line.Text.Length && MarkdownChar.IsSpaceOrTab(line.Text[start]))
        {
            start = start + 1;
        }

        int end = line.Text.Length;
        while (end > start && MarkdownChar.IsSpaceOrTab(line.Text[end - 1]))
        {
            end = end - 1;
        }

        return line.Text.Substring(start, end - start);
    }

    internal static bool IsFencedCodeBlockClosingLine(MarkdownLine line, char marker, int openingFenceLength)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if ((marker != '`' && marker != '~') || openingFenceLength < 3)
        {
            return false;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return false;
        }

        int index = scan.CharacterIndex;
        int length = 0;
        while (index < line.Text.Length && line.Text[index] == marker)
        {
            length = length + 1;
            index = index + 1;
        }

        if (length < openingFenceLength)
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

    internal static bool IsHtmlBlockStartLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetHtmlBlockType(line) > 0;
    }

    internal static int GetHtmlBlockType(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return 0;
        }

        int index = scan.CharacterIndex;
        if (GetHtmlBlockType1TagName(line).Length > 0)
        {
            return 1;
        }

        if (StartsWithAt(line.Text, index, "<!--")
            && CommonMarkInlineRules.IsValidHtmlCommentOpening(line.Text, index))
        {
            return 2;
        }

        if (StartsWithAt(line.Text, index, "<?"))
        {
            return 3;
        }

        if (StartsWithAt(line.Text, index, "<!") && index + 2 < line.Text.Length && IsAsciiUpper(line.Text[index + 2]))
        {
            return 4;
        }

        if (StartsWithAt(line.Text, index, "<![CDATA["))
        {
            return 5;
        }

        if (IsHtmlBlockType6StartLine(line))
        {
            return 6;
        }

        if (IsHtmlBlockType7StartLine(line))
        {
            return 7;
        }

        return 0;
    }

    internal static string GetHtmlBlockType1TagName(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3)
        {
            return "";
        }

        if (StartsHtmlType1Tag(line.Text, scan.CharacterIndex, "script"))
        {
            return "script";
        }

        if (StartsHtmlType1Tag(line.Text, scan.CharacterIndex, "pre"))
        {
            return "pre";
        }

        if (StartsHtmlType1Tag(line.Text, scan.CharacterIndex, "style"))
        {
            return "style";
        }

        if (StartsHtmlType1Tag(line.Text, scan.CharacterIndex, "textarea"))
        {
            return "textarea";
        }

        return "";
    }

    internal static bool IsHtmlBlockEndLine(MarkdownLine line, int blockType, string type1TagName)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (type1TagName == null)
        {
            throw new ArgumentNullException("type1TagName");
        }

        if (blockType == 1)
        {
            return ContainsOrdinalIgnoreCase(line.Text, "</" + type1TagName + ">");
        }

        if (blockType == 2)
        {
            return line.Text.IndexOf("-->") >= 0;
        }

        if (blockType == 3)
        {
            return line.Text.IndexOf("?>") >= 0;
        }

        if (blockType == 4)
        {
            return line.Text.IndexOf(">") >= 0;
        }

        if (blockType == 5)
        {
            return line.Text.IndexOf("]]>") >= 0;
        }

        if (blockType == 6 || blockType == 7)
        {
            return IsBlankLine(line);
        }

        return false;
    }

    internal static bool IsHtmlBlockType6StartLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        string tagName = GetHtmlStartOrClosingTagName(line);
        if (tagName.Length == 0)
        {
            return false;
        }

        return IsHtmlBlockTagName(tagName);
    }

    internal static bool IsHtmlBlockType7StartLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return false;
        }

        int tagEnd = GetCompleteHtmlTagEndIndex(line.Text, scan.CharacterIndex);
        if (tagEnd < 0)
        {
            return false;
        }

        int index = tagEnd + 1;
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

    internal static string GetHtmlStartOrClosingTagName(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length || line.Text[scan.CharacterIndex] != '<')
        {
            return "";
        }

        int index = scan.CharacterIndex + 1;
        if (index < line.Text.Length && line.Text[index] == '/')
        {
            index = index + 1;
        }

        if (index >= line.Text.Length || !IsAsciiLetter(line.Text[index]))
        {
            return "";
        }

        int nameStart = index;
        index = index + 1;
        while (index < line.Text.Length && IsHtmlTagNameCharacter(line.Text[index]))
        {
            index = index + 1;
        }

        if (index < line.Text.Length)
        {
            char next = line.Text[index];
            if (!MarkdownChar.IsSpaceOrTab(next) && next != '>' && next != '/')
            {
                return "";
            }
        }

        return line.Text.Substring(nameStart, index - nameStart).ToLowerInvariant();
    }

    internal static bool IsHtmlBlockTagName(string tagName)
    {
        if (tagName == null)
        {
            throw new ArgumentNullException("tagName");
        }

        string name = tagName.ToLowerInvariant();
        return name == "address"
            || name == "article"
            || name == "aside"
            || name == "base"
            || name == "basefont"
            || name == "blockquote"
            || name == "body"
            || name == "caption"
            || name == "center"
            || name == "col"
            || name == "colgroup"
            || name == "dd"
            || name == "details"
            || name == "dialog"
            || name == "dir"
            || name == "div"
            || name == "dl"
            || name == "dt"
            || name == "fieldset"
            || name == "figcaption"
            || name == "figure"
            || name == "footer"
            || name == "form"
            || name == "frame"
            || name == "frameset"
            || name == "h1"
            || name == "h2"
            || name == "h3"
            || name == "h4"
            || name == "h5"
            || name == "h6"
            || name == "head"
            || name == "header"
            || name == "hr"
            || name == "html"
            || name == "iframe"
            || name == "legend"
            || name == "li"
            || name == "link"
            || name == "main"
            || name == "menu"
            || name == "menuitem"
            || name == "nav"
            || name == "noframes"
            || name == "ol"
            || name == "optgroup"
            || name == "option"
            || name == "p"
            || name == "param"
            || name == "search"
            || name == "section"
            || name == "summary"
            || name == "table"
            || name == "tbody"
            || name == "td"
            || name == "tfoot"
            || name == "th"
            || name == "thead"
            || name == "title"
            || name == "tr"
            || name == "track"
            || name == "ul";
    }

    internal static bool IsLinkReferenceDefinitionLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetLinkReferenceDefinitionEndIndex(line) >= 0;
    }

    internal static string GetLinkReferenceDefinitionLabel(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int labelEnd = GetLinkReferenceDefinitionLabelEndIndex(line);
        if (labelEnd < 0)
        {
            return "";
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        return line.Text.Substring(scan.CharacterIndex + 1, labelEnd - scan.CharacterIndex - 1);
    }

    internal static string GetLinkReferenceDefinitionDestination(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int destinationStart = GetLinkReferenceDefinitionDestinationStartIndex(line);
        int destinationEnd = GetLinkReferenceDefinitionDestinationEndIndex(line);
        if (destinationStart < 0 || destinationEnd < 0)
        {
            return "";
        }

        if (line.Text[destinationStart] == '<')
        {
            return CommonMarkInlineRules.DecodeLinkAttributeText(
                line.Text.Substring(destinationStart + 1, destinationEnd - destinationStart - 1));
        }

        return CommonMarkInlineRules.DecodeLinkAttributeText(line.Text.Substring(destinationStart, destinationEnd - destinationStart));
    }

    internal static string GetLinkReferenceDefinitionTitle(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int titleStart = GetLinkReferenceDefinitionTitleStartIndex(line);
        int definitionEnd = GetLinkReferenceDefinitionEndIndex(line);
        if (titleStart < 0 || definitionEnd < 0 || titleStart >= line.Text.Length)
        {
            return "";
        }

        char opener = line.Text[titleStart];
        char closer = opener;
        if (opener == '(')
        {
            closer = ')';
        }

        int titleEnd = titleStart + 1;
        while (titleEnd < line.Text.Length)
        {
            if (line.Text[titleEnd] == closer && !IsEscapedAt(line.Text, titleEnd))
            {
                return CommonMarkInlineRules.DecodeLinkAttributeText(line.Text.Substring(titleStart + 1, titleEnd - titleStart - 1));
            }

            titleEnd = titleEnd + 1;
        }

        return "";
    }

    internal static string NormalizeLinkReferenceLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        string result = "";
        bool pendingSpace = false;
        int index = 0;
        while (index < label.Length)
        {
            char character = label[index];
            if (MarkdownChar.IsWhitespace(character))
            {
                if (result.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    result = result + " ";
                    pendingSpace = false;
                }

                if (character == '\u00df' || character == '\u1e9e')
                {
                    result = result + "ss";
                }
                else
                {
                    result = result + character.ToString().ToLowerInvariant();
                }
            }

            index = index + 1;
        }

        return result;
    }

    internal static int GetLinkReferenceDefinitionEndIndex(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int titleStart = GetLinkReferenceDefinitionTitleStartIndex(line);
        if (titleStart < 0)
        {
            return -1;
        }

        if (titleStart >= line.Text.Length)
        {
            return line.Text.Length;
        }

        char opener = line.Text[titleStart];
        if (opener != '"' && opener != '\'' && opener != '(')
        {
            return -1;
        }

        char closer = opener;
        if (opener == '(')
        {
            closer = ')';
        }

        int index = titleStart + 1;
        while (index < line.Text.Length)
        {
            if (line.Text[index] == closer && !IsEscapedAt(line.Text, index))
            {
                index = index + 1;
                while (index < line.Text.Length && MarkdownChar.IsWhitespace(line.Text[index]))
                {
                    index = index + 1;
                }

                if (index == line.Text.Length)
                {
                    return index;
                }

                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int GetLinkReferenceDefinitionTitleStartIndex(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int destinationEnd = GetLinkReferenceDefinitionDestinationEndIndex(line);
        if (destinationEnd < 0)
        {
            return -1;
        }

        int index = destinationEnd;
        if (index < line.Text.Length && line.Text[index] == '>')
        {
            index = index + 1;
        }

        int whitespaceStart = index;
        while (index < line.Text.Length && MarkdownChar.IsWhitespace(line.Text[index]))
        {
            index = index + 1;
        }

        if (index >= line.Text.Length)
        {
            return line.Text.Length;
        }

        if (index == whitespaceStart)
        {
            return -1;
        }

        char opener = line.Text[index];
        if (opener == '"' || opener == '\'' || opener == '(')
        {
            return index;
        }

        return -1;
    }

    internal static int GetLinkReferenceDefinitionDestinationStartIndex(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int labelEnd = GetLinkReferenceDefinitionLabelEndIndex(line);
        if (labelEnd < 0)
        {
            return -1;
        }

        int index = labelEnd + 2;
        while (index < line.Text.Length && MarkdownChar.IsWhitespace(line.Text[index]))
        {
            index = index + 1;
        }

        if (index >= line.Text.Length)
        {
            return -1;
        }

        return index;
    }

    internal static int GetLinkReferenceDefinitionDestinationEndIndex(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int destinationStart = GetLinkReferenceDefinitionDestinationStartIndex(line);
        if (destinationStart < 0)
        {
            return -1;
        }

        if (line.Text[destinationStart] == '<')
        {
            int index = destinationStart + 1;
            while (index < line.Text.Length)
            {
                if (MarkdownChar.IsLineEnding(line.Text[index]) || line.Text[index] == '<')
                {
                    return -1;
                }

                if (line.Text[index] == '>' && !IsEscapedAt(line.Text, index))
                {
                    return index;
                }

                index = index + 1;
            }

            return -1;
        }

        int end = destinationStart;
        while (end < line.Text.Length)
        {
            if (line.Text[end] == '\\' && end + 1 < line.Text.Length)
            {
                end = end + 2;
            }
            else if (MarkdownChar.IsWhitespace(line.Text[end]))
            {
                return end;
            }
            else
            {
                end = end + 1;
            }
        }

        if (end == destinationStart)
        {
            return -1;
        }

        return end;
    }

    internal static int GetLinkReferenceDefinitionLabelEndIndex(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length || line.Text[scan.CharacterIndex] != '[')
        {
            return -1;
        }

        int labelStart = scan.CharacterIndex + 1;
        int index = labelStart;
        while (index < line.Text.Length)
        {
            if (line.Text[index] == '[' && !IsEscapedAt(line.Text, index))
            {
                return -1;
            }

            if (line.Text[index] == ']' && !IsEscapedAt(line.Text, index))
            {
                if (index + 1 < line.Text.Length && line.Text[index + 1] == ':')
                {
                    int labelLength = index - labelStart;
                    string label = line.Text.Substring(labelStart, labelLength);
                    if (labelLength > 0
                        && labelLength <= 999
                        && NormalizeLinkReferenceLabel(label).Length > 0)
                    {
                        return index;
                    }

                    return -1;
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsOrderedListItemLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetOrderedListItemMarkerIndex(line) >= 0;
    }

    internal static int GetOrderedListItemNumber(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetOrderedListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return 0;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        int number = 0;
        int index = scan.CharacterIndex;
        while (index < markerIndex)
        {
            number = number * 10 + (line.Text[index] - '0');
            index = index + 1;
        }

        return number;
    }

    internal static string GetOrderedListItemMarker(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetOrderedListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return "";
        }

        return line.Text[markerIndex].ToString();
    }

    internal static string RemoveOrderedListItemMarker(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetOrderedListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return line.Text;
        }

        return RemoveListItemMarkerText(line.Text, markerIndex);
    }

    internal static int GetOrderedListItemContentStartIndex(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetOrderedListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return -1;
        }

        return GetListItemContentStartIndex(line.Text, markerIndex);
    }

    internal static int GetOrderedListItemMarkerColumn(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (!IsOrderedListItemLine(line))
        {
            return -1;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        return scan.Column;
    }

    internal static int GetOrderedListItemContentIndent(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetOrderedListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return -1;
        }

        return GetListItemContentIndent(line.Text, markerIndex);
    }

    internal static int GetOrderedListItemMarkerIndex(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length || !IsAsciiDigit(line.Text[scan.CharacterIndex]))
        {
            return -1;
        }

        int index = scan.CharacterIndex;
        int digitCount = 0;
        while (index < line.Text.Length && IsAsciiDigit(line.Text[index]))
        {
            digitCount = digitCount + 1;
            if (digitCount > 9)
            {
                return -1;
            }

            index = index + 1;
        }

        if (index >= line.Text.Length)
        {
            return -1;
        }

        char marker = line.Text[index];
        if (marker != '.' && marker != ')')
        {
            return -1;
        }

        int afterMarker = index + 1;
        if (afterMarker < line.Text.Length && !MarkdownChar.IsSpaceOrTab(line.Text[afterMarker]))
        {
            return -1;
        }

        return index;
    }

    internal static bool IsBulletListItemLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (IsThematicBreakLine(line))
        {
            return false;
        }

        return GetBulletListItemMarkerIndex(line) >= 0;
    }

    internal static string GetBulletListItemMarker(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetBulletListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return "";
        }

        return line.Text[markerIndex].ToString();
    }

    internal static string RemoveBulletListItemMarker(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetBulletListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return line.Text;
        }

        return RemoveListItemMarkerText(line.Text, markerIndex);
    }

    internal static int GetBulletListItemContentStartIndex(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetBulletListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return -1;
        }

        return GetListItemContentStartIndex(line.Text, markerIndex);
    }

    internal static int GetListItemContentStartIndex(string text, int markerIndex)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int markerEnd = markerIndex + 1;
        if (markerEnd >= text.Length)
        {
            return text.Length;
        }

        int startColumn = GetColumnAtIndex(text, markerEnd);
        int column = startColumn;
        int index = markerEnd;
        while (index < text.Length && MarkdownChar.IsSpaceOrTab(text[index]))
        {
            column = MarkdownIndentation.AdvanceColumn(column, text[index]);
            index = index + 1;
        }

        if (index >= text.Length)
        {
            return text.Length;
        }

        if (column - startColumn <= 4)
        {
            return index;
        }

        return markerEnd + 1;
    }

    internal static string RemoveListItemMarkerText(string text, int markerIndex)
    {
        int markerEnd = markerIndex + 1;
        if (markerEnd >= text.Length)
        {
            return "";
        }

        int markerEndColumn = GetColumnAtIndex(text, markerEnd);
        int column = markerEndColumn;
        int index = markerEnd;
        while (index < text.Length && MarkdownChar.IsSpaceOrTab(text[index]))
        {
            column = MarkdownIndentation.AdvanceColumn(column, text[index]);
            index = index + 1;
        }

        if (index >= text.Length)
        {
            return "";
        }

        int padding = column - markerEndColumn;
        if (padding <= 4)
        {
            return text.Substring(index);
        }

        return new string(' ', padding - 1) + text.Substring(index);
    }

    internal static int GetListItemContentIndent(string text, int markerIndex)
    {
        int markerEnd = markerIndex + 1;
        int markerEndColumn = GetColumnAtIndex(text, markerEnd);
        int column = markerEndColumn;
        int index = markerEnd;
        while (index < text.Length && MarkdownChar.IsSpaceOrTab(text[index]))
        {
            column = MarkdownIndentation.AdvanceColumn(column, text[index]);
            index = index + 1;
        }

        if (index >= text.Length || column - markerEndColumn > 4)
        {
            return markerEndColumn + 1;
        }

        return column;
    }

    internal static int GetBulletListItemMarkerColumn(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (!IsBulletListItemLine(line))
        {
            return -1;
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        return scan.Column;
    }

    internal static int GetBulletListItemContentIndent(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int markerIndex = GetBulletListItemMarkerIndex(line);
        if (markerIndex < 0)
        {
            return -1;
        }

        return GetListItemContentIndent(line.Text, markerIndex);
    }

    internal static int GetBulletListItemMarkerIndex(MarkdownLine line)
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

        char marker = line.Text[scan.CharacterIndex];
        if (marker != '-' && marker != '+' && marker != '*')
        {
            return -1;
        }

        int afterMarker = scan.CharacterIndex + 1;
        if (afterMarker < line.Text.Length && !MarkdownChar.IsSpaceOrTab(line.Text[afterMarker]))
        {
            return -1;
        }

        return scan.CharacterIndex;
    }

    internal static bool IsIndentedCodeLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.IsBlank)
        {
            return false;
        }

        return scan.Column >= 4;
    }

    internal static string RemoveIndentedCodePrefix(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int index = 0;
        int column = 0;
        while (index < line.Text.Length && column < 4)
        {
            char character = line.Text[index];
            if (MarkdownChar.IsSpace(character))
            {
                column = column + 1;
                index = index + 1;
            }
            else if (MarkdownChar.IsTab(character))
            {
                column = MarkdownIndentation.AdvanceColumn(column, character);
                index = index + 1;
            }
            else
            {
                return line.Text;
            }
        }

        return line.Text.Substring(index);
    }

    internal static string RemoveOptionalIndentColumns(MarkdownLine line, int columns)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (columns < 0)
        {
            throw new ArgumentOutOfRangeException("columns");
        }

        int index = 0;
        int column = 0;
        while (index < line.Text.Length && column < columns)
        {
            char character = line.Text[index];
            if (MarkdownChar.IsSpace(character))
            {
                column = column + 1;
                index = index + 1;
            }
            else if (MarkdownChar.IsTab(character))
            {
                int nextColumn = MarkdownIndentation.AdvanceColumn(column, character);
                if (nextColumn > columns)
                {
                    int remainingColumns = nextColumn - columns;
                    int remainingIndex = index + 1;
                    int sourceColumn = nextColumn;
                    while (remainingIndex < line.Text.Length
                        && MarkdownChar.IsSpaceOrTab(line.Text[remainingIndex]))
                    {
                        int followingColumn = MarkdownIndentation.AdvanceColumn(
                            sourceColumn,
                            line.Text[remainingIndex]);
                        remainingColumns = remainingColumns + followingColumn - sourceColumn;
                        sourceColumn = followingColumn;
                        remainingIndex = remainingIndex + 1;
                    }

                    return new string(' ', remainingColumns) + line.Text.Substring(remainingIndex);
                }

                column = nextColumn;
                index = index + 1;
            }
            else
            {
                break;
            }
        }

        return line.Text.Substring(index);
    }

    internal static bool ShouldConsumeListItemBlankLine(
        MarkdownBlockParseContext context,
        int contentIndent,
        bool ordered,
        string marker,
        int markerColumn)
    {
        int index = context.LineIndex + 1;
        while (index < context.Lines.Length && IsBlankLine(context.Lines[index]))
        {
            index = index + 1;
        }

        if (index >= context.Lines.Length)
        {
            return false;
        }

        MarkdownLine next = context.Lines[index];
        if (ordered
            && IsOrderedListItemLine(next)
            && GetOrderedListItemMarker(next) == marker)
        {
            return true;
        }

        if (!ordered
            && IsBulletListItemLine(next)
            && GetBulletListItemMarker(next) == marker)
        {
            return true;
        }

        return GetLineIndentColumn(next) >= contentIndent;
    }

    internal static bool IsBlankFollowedByBulletListItem(
        MarkdownBlockParseContext context,
        string marker)
    {
        int index = context.LineIndex + 1;
        while (index < context.Lines.Length && IsBlankLine(context.Lines[index]))
        {
            index = index + 1;
        }

        return index < context.Lines.Length
            && IsBulletListItemLine(context.Lines[index])
            && GetBulletListItemMarker(context.Lines[index]) == marker;
    }

    internal static bool IsCurrentBulletListItem(MarkdownBlockParseContext context, string marker)
    {
        MarkdownLine? line = context.GetCurrentLine();
        return line != null
            && IsBulletListItemLine(line)
            && GetBulletListItemMarker(line) == marker;
    }

    internal static bool IsBlankFollowedByOrderedListItem(
        MarkdownBlockParseContext context,
        string marker)
    {
        int index = context.LineIndex + 1;
        while (index < context.Lines.Length && IsBlankLine(context.Lines[index]))
        {
            index = index + 1;
        }

        return index < context.Lines.Length
            && IsOrderedListItemLine(context.Lines[index])
            && GetOrderedListItemMarker(context.Lines[index]) == marker;
    }

    internal static bool IsCurrentOrderedListItem(MarkdownBlockParseContext context, string marker)
    {
        MarkdownLine? line = context.GetCurrentLine();
        return line != null
            && IsOrderedListItemLine(line)
            && GetOrderedListItemMarker(line) == marker;
    }

    internal static bool EndsWithBlankLine(List<string> lines)
    {
        return lines.Count > 0 && lines[lines.Count - 1].Length == 0;
    }

    internal static bool HasLooseListItemSpacing(List<string> lines, MarkdownPipeline pipeline)
    {
        MarkdownDocument fullDocument = MarkdownParser.Parse(JoinLines(lines), pipeline);
        int fullCount = fullDocument.Root.GetChildCount();
        if (fullCount < 2)
        {
            return false;
        }

        List<string> prefix = new List<string>();
        int index = 0;
        while (index < lines.Count)
        {
            if (lines[index].Length == 0)
            {
                MarkdownDocument prefixDocument = MarkdownParser.Parse(JoinLines(prefix), pipeline);
                if (prefixDocument.Root.GetChildCount() < fullCount)
                {
                    return true;
                }
            }

            prefix.Add(lines[index]);
            index = index + 1;
        }

        return false;
    }

    internal static bool TryParseThematicBreak(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? line = context.GetCurrentLine();
        if (line == null)
        {
            return false;
        }

        if (!IsThematicBreakLine(line))
        {
            return false;
        }

        MarkdownNode thematicBreak = MarkdownNode.Create(MarkdownNodeKind.ThematicBreak, line.ContentSpan);
        context.ParseContext.Document.Root.AppendChild(thematicBreak);
        context.NoteContentLine();
        context.AdvanceLine();
        return true;
    }

    internal static bool IsThematicBreakLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        if (scan.Column > 3 || scan.CharacterIndex >= line.Text.Length)
        {
            return false;
        }

        char marker = line.Text[scan.CharacterIndex];
        if (marker != '-' && marker != '*' && marker != '_')
        {
            return false;
        }

        int markerCount = 0;
        int index = scan.CharacterIndex;
        while (index < line.Text.Length)
        {
            char character = line.Text[index];
            if (character == marker)
            {
                markerCount = markerCount + 1;
            }
            else if (!MarkdownChar.IsSpaceOrTab(character))
            {
                return false;
            }

            index = index + 1;
        }

        return markerCount >= 3;
    }

    internal static bool IsBlankLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        return scan.IsBlank;
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
            return false;
        }

        if (value.Length > text.Length - index)
        {
            return false;
        }

        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            if (text[index + valueIndex] != value[valueIndex])
            {
                return false;
            }

            valueIndex = valueIndex + 1;
        }

        return true;
    }

    internal static bool StartsWithAtIgnoreCase(string text, int index, string value)
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
            return false;
        }

        if (value.Length > text.Length - index)
        {
            return false;
        }

        return string.Compare(text, index, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0;
    }

    internal static bool ContainsOrdinalIgnoreCase(string text, string value)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool StartsHtmlType1Tag(string text, int index, string tagName)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (tagName == null)
        {
            throw new ArgumentNullException("tagName");
        }

        string prefix = "<" + tagName;
        if (!StartsWithAtIgnoreCase(text, index, prefix))
        {
            return false;
        }

        int nextIndex = index + prefix.Length;
        if (nextIndex >= text.Length)
        {
            return true;
        }

        char next = text[nextIndex];
        return MarkdownChar.IsSpaceOrTab(next) || next == '>';
    }

    internal static int GetCompleteHtmlTagEndIndex(string text, int index)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (index < 0 || index >= text.Length || text[index] != '<')
        {
            return -1;
        }

        int end = CommonMarkInlineRules.FindHtmlInlineTagEnd(text, index);
        if (end < 0)
        {
            return -1;
        }

        return end - 1;
    }

    internal static bool IsAsciiUpper(char character)
    {
        return character >= 'A' && character <= 'Z';
    }

    internal static bool IsAsciiLetter(char character)
    {
        return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
    }

    internal static bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal static bool IsHtmlTagNameCharacter(char character)
    {
        return IsAsciiLetter(character) || IsAsciiDigit(character) || character == '-';
    }

    internal static bool IsEscapedAt(string text, int index)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (index < 0 || index >= text.Length)
        {
            return false;
        }

        int slashCount = 0;
        int current = index - 1;
        while (current >= 0 && text[current] == '\\')
        {
            slashCount = slashCount + 1;
            current = current - 1;
        }

        return slashCount % 2 == 1;
    }

    internal static int GetLineIndentColumn(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(line.Text);
        return scan.Column;
    }

    internal static int GetColumnAtIndex(string text, int endIndex)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (endIndex < 0)
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        int index = 0;
        int column = 0;
        while (index < text.Length && index < endIndex)
        {
            column = MarkdownIndentation.AdvanceColumn(column, text[index]);
            index = index + 1;
        }

        return column;
    }

    internal static string JoinLines(List<string> lines)
    {
        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }

        string result = "";
        int index = 0;
        while (index < lines.Count)
        {
            if (index > 0)
            {
                result = result + "\n";
            }

            result = result + lines[index];
            index = index + 1;
        }

        return result;
    }

    internal static bool CanContinueLazily(List<string> lines, MarkdownPipeline pipeline)
    {
        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (lines.Count == 0)
        {
            return false;
        }

        string lastLine = lines[lines.Count - 1];
        if (lastLine.Length == 0)
        {
            return false;
        }

        MarkdownDocument document = MarkdownParser.Parse(JoinLines(lines), pipeline);
        int childCount = document.Root.GetChildCount();
        if (childCount == 0)
        {
            return false;
        }

        MarkdownNode lastChild = document.Root.GetChild(childCount - 1);
        return EndsWithParagraph(lastChild);
    }

    internal static bool EndsWithParagraph(MarkdownNode node)
    {
        if (node.Kind == MarkdownNodeKind.Paragraph)
        {
            return true;
        }

        int childCount = node.GetChildCount();
        if (childCount == 0)
        {
            return false;
        }

        return EndsWithParagraph(node.GetChild(childCount - 1));
    }

    internal static bool IsParagraphInterruptingBlockLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (IsBlockQuoteLine(line))
        {
            return true;
        }

        if (GetAtxHeadingLevel(line) > 0)
        {
            return true;
        }

        if (IsThematicBreakLine(line))
        {
            return true;
        }

        if (IsFencedCodeBlockOpeningLine(line))
        {
            return true;
        }

        int htmlBlockType = GetHtmlBlockType(line);
        if (htmlBlockType >= 1 && htmlBlockType <= 6)
        {
            return true;
        }

        if (IsBulletListItemLine(line) && HasNonBlankText(RemoveBulletListItemMarker(line)))
        {
            return true;
        }

        if (IsOrderedListItemLine(line)
            && GetOrderedListItemNumber(line) == 1
            && HasNonBlankText(RemoveOrderedListItemMarker(line)))
        {
            return true;
        }

        return false;
    }

    internal static bool HasNonBlankText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!MarkdownChar.IsSpaceOrTab(text[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
