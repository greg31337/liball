using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownInlineLinkParseResult
{
    internal bool Success;
    internal int EndIndex;
    internal string Destination;
    internal string Title;

    internal MarkdownInlineLinkParseResult()
    {
        Success = false;
        EndIndex = -1;
        Destination = "";
        Title = "";
    }

    internal static MarkdownInlineLinkParseResult Create(bool success, int endIndex, string destination, string title)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        MarkdownInlineLinkParseResult result = new MarkdownInlineLinkParseResult();
        result.Success = success;
        result.EndIndex = endIndex;
        result.Destination = destination;
        result.Title = title;
        return result;
    }
}

internal class CommonMarkInlineRules
{
    internal const string CodeSpanRuleName = "commonmark.code-span";
    internal const string HardLineBreakRuleName = "commonmark.hard-line-break";
    internal const string BackslashEscapeRuleName = "commonmark.backslash-escape";
    internal const string EntityReferenceRuleName = "commonmark.entity-reference";
    internal const string AutolinkRuleName = "commonmark.autolink";
    internal const string HtmlInlineRuleName = "commonmark.html-inline";
    internal const string ImageRuleName = "commonmark.image";
    internal const string LinkRuleName = "commonmark.link";
    internal const string EmphasisRuleName = "commonmark.emphasis";
    internal const string SoftLineBreakRuleName = "commonmark.soft-line-break";
    internal const string TextRuleName = "commonmark.text";

    internal static void RegisterCommonMarkInlineRules(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        inlineRules.Set(CodeSpanRuleName, CreateCodeSpanRule());
        inlineRules.Set(HardLineBreakRuleName, CreateHardLineBreakRule());
        inlineRules.Set(BackslashEscapeRuleName, CreateBackslashEscapeRule());
        inlineRules.Set(EntityReferenceRuleName, CreateEntityReferenceRule());
        inlineRules.Set(AutolinkRuleName, CreateAutolinkRule());
        inlineRules.Set(HtmlInlineRuleName, CreateHtmlInlineRule());
        inlineRules.Set(ImageRuleName, CreateImageRule());
        inlineRules.Set(LinkRuleName, CreateLinkRule());
        inlineRules.Set(EmphasisRuleName, CreateEmphasisRule());
        inlineRules.Set(SoftLineBreakRuleName, CreateSoftLineBreakRule());
        inlineRules.Set(TextRuleName, CreateTextRule());
    }

    internal static MarkdownInlineRule CreateCodeSpanRule()
    {
        return new MarkdownInlineRule(CodeSpanRuleName, TryParseCodeSpan);
    }

    internal static MarkdownInlineRule CreateHardLineBreakRule()
    {
        return new MarkdownInlineRule(HardLineBreakRuleName, TryParseHardLineBreak);
    }

    internal static MarkdownInlineRule CreateSoftLineBreakRule()
    {
        return new MarkdownInlineRule(SoftLineBreakRuleName, TryParseSoftLineBreak);
    }

    internal static MarkdownInlineRule CreateBackslashEscapeRule()
    {
        return new MarkdownInlineRule(BackslashEscapeRuleName, TryParseBackslashEscape);
    }

    internal static MarkdownInlineRule CreateEntityReferenceRule()
    {
        return new MarkdownInlineRule(EntityReferenceRuleName, TryParseEntityReference);
    }

    internal static MarkdownInlineRule CreateAutolinkRule()
    {
        return new MarkdownInlineRule(AutolinkRuleName, TryParseAutolink);
    }

    internal static MarkdownInlineRule CreateHtmlInlineRule()
    {
        return new MarkdownInlineRule(HtmlInlineRuleName, TryParseHtmlInline);
    }

    internal static MarkdownInlineRule CreateImageRule()
    {
        return new MarkdownInlineRule(ImageRuleName, TryParseImage);
    }

    internal static MarkdownInlineRule CreateLinkRule()
    {
        return new MarkdownInlineRule(LinkRuleName, TryParseLink);
    }

    internal static MarkdownInlineRule CreateEmphasisRule()
    {
        return new MarkdownInlineRule(EmphasisRuleName, TryParseEmphasis);
    }

    internal static MarkdownInlineRule CreateTextRule()
    {
        return new MarkdownInlineRule(TextRuleName, TryParseText);
    }

    internal static bool TryParseCodeSpan(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter() || context.GetCurrentCharacter() != '`')
        {
            return false;
        }

        int openingLength = CountBacktickRun(context.Source, context.Position);
        int contentStart = context.Position + openingLength;
        int closingStart = FindClosingBacktickRun(context.Source, contentStart, openingLength);
        if (closingStart < 0)
        {
            MarkdownNode delimiterText = MarkdownNode.Create(MarkdownNodeKind.Text);
            delimiterText.SetLiteral(new string('`', openingLength));
            context.AppendInline(delimiterText);
            context.AdvanceBy(openingLength);
            return true;
        }

        string raw = context.Source.Substring(contentStart, closingStart - contentStart);
        MarkdownNode codeSpan = MarkdownNode.Create(MarkdownNodeKind.CodeSpan);
        codeSpan.SetLiteral(NormalizeCodeSpanLiteral(raw));
        context.AppendInline(codeSpan);
        context.AdvanceBy(closingStart + openingLength - context.Position);
        return true;
    }

    internal static bool TryParseHardLineBreak(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter())
        {
            return false;
        }

        if (context.GetCurrentCharacter() == '\\')
        {
            int backslashLineEndingLength = MarkdownChar.GetLineEndingLength(context.Source, context.Position + 1);
            if (backslashLineEndingLength > 0)
            {
                MarkdownNode hardLineBreak = MarkdownNode.Create(MarkdownNodeKind.HardLineBreak);
                context.AppendInline(hardLineBreak);
                context.AdvanceBy(1 + backslashLineEndingLength);
                return true;
            }
        }

        int lineEndingLength = MarkdownChar.GetLineEndingLength(context.Source, context.Position);
        if (lineEndingLength == 0)
        {
            return false;
        }

        if (!HasHardLineBreakSpaces(context.Source, context.Position))
        {
            return false;
        }

        TrimTrailingSpacesFromLastText(context.Container);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.HardLineBreak);
        context.AppendInline(node);
        context.AdvanceBy(lineEndingLength);
        return true;
    }

    internal static bool TryParseBackslashEscape(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter() || context.GetCurrentCharacter() != '\\')
        {
            return false;
        }

        int escapedIndex = context.Position + 1;
        if (escapedIndex >= context.Source.Length)
        {
            return false;
        }

        char escaped = context.Source[escapedIndex];
        if (!MarkdownChar.IsAsciiPunctuation(escaped))
        {
            return false;
        }

        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        text.SetLiteral(escaped.ToString());
        context.AppendInline(text);
        context.AdvanceBy(2);
        return true;
    }

    internal static bool TryParseEntityReference(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter() || context.GetCurrentCharacter() != '&')
        {
            return false;
        }

        MarkdownEntityDecodeResult result = MarkdownEntity.DecodeAt(context.Source, context.Position);
        if (!result.Success)
        {
            return false;
        }

        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        text.SetLiteral(result.Text);
        context.AppendInline(text);
        context.AdvanceBy(result.Length);
        return true;
    }

    internal static bool TryParseAutolink(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (IsLinkParsingProhibited(context))
        {
            return false;
        }

        if (!context.HasCurrentCharacter() || context.GetCurrentCharacter() != '<')
        {
            return false;
        }

        int endIndex = FindAutolinkEnd(context.Source, context.Position);
        if (endIndex < 0)
        {
            return false;
        }

        string text = context.Source.Substring(context.Position + 1, endIndex - context.Position - 1);
        if (IsUriAutolink(text))
        {
            AppendAutolink(context, text, text, endIndex);
            return true;
        }

        if (IsEmailAutolink(text))
        {
            AppendAutolink(context, text, "mailto:" + text, endIndex);
            return true;
        }

        return false;
    }

    internal static bool TryParseHtmlInline(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter() || context.GetCurrentCharacter() != '<')
        {
            return false;
        }

        int endIndex = FindHtmlInlineEnd(context.Source, context.Position);
        if (endIndex < 0)
        {
            return false;
        }

        AppendHtmlInline(context, context.Source.Substring(context.Position, endIndex - context.Position), endIndex);
        return true;
    }

    internal static bool TryParseImage(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter()
            || context.GetCurrentCharacter() != '!'
            || context.Position + 1 >= context.Source.Length
            || context.Source[context.Position + 1] != '[')
        {
            return false;
        }

        int labelEnd = FindLinkLabelEnd(context.Source, context.Position + 1);
        if (labelEnd < 0)
        {
            return false;
        }

        if (labelEnd + 1 < context.Source.Length && context.Source[labelEnd + 1] == '(')
        {
            MarkdownInlineLinkParseResult result = ParseInlineLinkDestinationAndTitle(context.Source, labelEnd + 1);
            if (!result.Success)
            {
                return false;
            }

            AppendImageInline(
                context,
                context.Position + 2,
                labelEnd,
                result.Destination,
                result.Title,
                result.EndIndex);
            return true;
        }

        return TryParseReferenceImage(context, labelEnd);
    }

    internal static bool TryParseLink(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (IsLinkParsingProhibited(context))
        {
            return false;
        }

        if (!context.HasCurrentCharacter() || context.GetCurrentCharacter() != '[')
        {
            return false;
        }

        int labelEnd = FindLinkLabelEnd(context.Source, context.Position);
        if (labelEnd < 0)
        {
            return false;
        }

        if (labelEnd + 1 < context.Source.Length && context.Source[labelEnd + 1] == '(')
        {
            MarkdownInlineLinkParseResult result = ParseInlineLinkDestinationAndTitle(context.Source, labelEnd + 1);
            if (result.Success)
            {
                if (LinkLabelContainsLink(context, context.Position + 1, labelEnd))
                {
                    return false;
                }

                AppendLinkInline(
                    context,
                    context.Position + 1,
                    labelEnd,
                    result.Destination,
                    result.Title,
                    result.EndIndex);
                return true;
            }
        }

        return TryParseReferenceLink(context, labelEnd);
    }

    internal static bool LinkLabelContainsLink(
        MarkdownInlineParseContext context,
        int labelStart,
        int labelEnd)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownNode container = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownInlineParseContext innerContext = MarkdownInlineParseContext.Create(
            context.ParseContext,
            container,
            context.Source.Substring(labelStart, labelEnd - labelStart));
        InlineParser.Run(innerContext);
        return ContainsLinkNode(container);
    }

    internal static bool ContainsLinkNode(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if ((child.Kind == MarkdownNodeKind.Link && child.GetAttributeValue("autolink") != "true")
                || ContainsLinkNode(child))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool TryParseReferenceImage(MarkdownInlineParseContext context, int labelEnd)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (labelEnd < 0 || labelEnd >= context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("labelEnd");
        }

        if (!context.HasCurrentCharacter()
            || context.GetCurrentCharacter() != '!'
            || context.Position + 1 >= context.Source.Length
            || context.Source[context.Position + 1] != '[')
        {
            return false;
        }

        string textLabel = context.Source.Substring(context.Position + 2, labelEnd - context.Position - 2);
        string referenceLabel = textLabel;
        int endIndex = labelEnd;

        if (labelEnd + 1 < context.Source.Length && context.Source[labelEnd + 1] == '[')
        {
            int referenceLabelEnd = FindLinkLabelEnd(context.Source, labelEnd + 1);
            if (referenceLabelEnd < 0)
            {
                return false;
            }

            referenceLabel = context.Source.Substring(labelEnd + 2, referenceLabelEnd - labelEnd - 2);
            if (referenceLabel.Length == 0)
            {
                referenceLabel = textLabel;
            }

            endIndex = referenceLabelEnd;
        }

        MarkdownNode? definition = FindLinkReferenceDefinition(context.ParseContext, referenceLabel);
        if (definition == null)
        {
            return false;
        }

        AppendReferenceImageInline(
            context,
            context.Position + 2,
            labelEnd,
            definition.GetAttributeValue("destination"),
            definition.GetAttributeValue("title"),
            referenceLabel,
            endIndex);
        return true;
    }

    internal static bool TryParseReferenceLink(MarkdownInlineParseContext context, int labelEnd)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (IsLinkParsingProhibited(context))
        {
            return false;
        }

        if (labelEnd < 0 || labelEnd >= context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("labelEnd");
        }

        if (!context.HasCurrentCharacter() || context.GetCurrentCharacter() != '[')
        {
            return false;
        }

        string textLabel = context.Source.Substring(context.Position + 1, labelEnd - context.Position - 1);
        string referenceLabel = textLabel;
        int endIndex = labelEnd;

        if (labelEnd + 1 < context.Source.Length && context.Source[labelEnd + 1] == '[')
        {
            int referenceLabelEnd = FindLinkLabelEnd(context.Source, labelEnd + 1);
            if (referenceLabelEnd < 0)
            {
                return false;
            }

            referenceLabel = context.Source.Substring(labelEnd + 2, referenceLabelEnd - labelEnd - 2);
            if (referenceLabel.Length == 0)
            {
                referenceLabel = textLabel;
            }

            endIndex = referenceLabelEnd;
        }

        MarkdownNode? definition = FindLinkReferenceDefinition(context.ParseContext, referenceLabel);
        if (definition == null)
        {
            return false;
        }

        if (LinkLabelContainsLink(context, context.Position + 1, labelEnd))
        {
            return false;
        }

        AppendReferenceLinkInline(
            context,
            context.Position + 1,
            labelEnd,
            definition.GetAttributeValue("destination"),
            definition.GetAttributeValue("title"),
            referenceLabel,
            endIndex);
        return true;
    }

    internal static MarkdownNode? FindLinkReferenceDefinition(MarkdownParseContext context, string label)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        return context.LinkReferences.Find(label);
    }

    internal static void AppendLinkInline(
        MarkdownInlineParseContext context,
        int labelStart,
        int labelEnd,
        string destination,
        string title,
        int endIndex)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (labelStart < 0 || labelStart > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("labelStart");
        }

        if (labelEnd < labelStart || labelEnd > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("labelEnd");
        }

        if (endIndex < labelEnd || endIndex >= context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        MarkdownNode link = MarkdownNode.Create(MarkdownNodeKind.Link);
        link.SetAttribute("destination", destination);
        if (title.Length > 0)
        {
            link.SetAttribute("title", title);
        }
        MarkdownInlineParseContext innerContext = MarkdownInlineParseContext.Create(
            context.ParseContext,
            link,
            context.Source.Substring(labelStart, labelEnd - labelStart));
        innerContext.InLink = true;
        InlineParser.Run(innerContext);
        context.AppendInline(link);
        context.AdvanceBy(endIndex + 1 - context.Position);
    }

    internal static void AppendImageInline(
        MarkdownInlineParseContext context,
        int labelStart,
        int labelEnd,
        string destination,
        string title,
        int endIndex)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (labelStart < 0 || labelStart > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("labelStart");
        }

        if (labelEnd < labelStart || labelEnd > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("labelEnd");
        }

        if (endIndex < labelEnd || endIndex >= context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        MarkdownNode image = MarkdownNode.Create(MarkdownNodeKind.Image);
        image.SetAttribute("destination", destination);
        if (title.Length > 0)
        {
            image.SetAttribute("title", title);
        }
        MarkdownInlineParseContext innerContext = MarkdownInlineParseContext.Create(
            context.ParseContext,
            image,
            context.Source.Substring(labelStart, labelEnd - labelStart));
        innerContext.InLink = context.InLink;
        InlineParser.Run(innerContext);
        context.AppendInline(image);
        context.AdvanceBy(endIndex + 1 - context.Position);
    }

    internal static void AppendReferenceLinkInline(
        MarkdownInlineParseContext context,
        int labelStart,
        int labelEnd,
        string destination,
        string title,
        string referenceLabel,
        int endIndex)
    {
        if (referenceLabel == null)
        {
            throw new ArgumentNullException("referenceLabel");
        }

        AppendLinkInline(context, labelStart, labelEnd, destination, title, endIndex);
        MarkdownNode? link = context.Container.GetLastChild();
        if (link != null && link.Kind == MarkdownNodeKind.Link)
        {
            link.SetAttribute("referenceLabel", referenceLabel);
        }
    }

    internal static void AppendReferenceImageInline(
        MarkdownInlineParseContext context,
        int labelStart,
        int labelEnd,
        string destination,
        string title,
        string referenceLabel,
        int endIndex)
    {
        if (referenceLabel == null)
        {
            throw new ArgumentNullException("referenceLabel");
        }

        AppendImageInline(context, labelStart, labelEnd, destination, title, endIndex);
        MarkdownNode? image = context.Container.GetLastChild();
        if (image != null && image.Kind == MarkdownNodeKind.Image)
        {
            image.SetAttribute("referenceLabel", referenceLabel);
        }
    }

    internal static void AppendAutolink(MarkdownInlineParseContext context, string text, string destination, int endIndex)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (endIndex < context.Position || endIndex >= context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        MarkdownNode link = MarkdownNode.Create(MarkdownNodeKind.Link);
        link.SetAttribute("destination", destination);
        link.SetAttribute("autolink", "true");
        MarkdownNode child = MarkdownNode.Create(MarkdownNodeKind.Text);
        child.SetLiteral(text);
        link.AppendChild(child);
        context.AppendInline(link);
        context.AdvanceBy(endIndex + 1 - context.Position);
    }

    internal static void AppendHtmlInline(MarkdownInlineParseContext context, string literal, int endIndex)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        if (endIndex < context.Position || endIndex > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        MarkdownNode html = MarkdownNode.Create(MarkdownNodeKind.HtmlInline);
        html.SetLiteral(literal);
        context.AppendInline(html);
        context.AdvanceBy(endIndex - context.Position);
    }

    internal static bool TryParseEmphasis(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter())
        {
            return false;
        }

        char marker = context.GetCurrentCharacter();
        if (marker != '*' && marker != '_')
        {
            return false;
        }

        int runLength = CountDelimiterRun(context.Source, context.Position, marker);
        if (!CanOpenDelimiterRun(context.Source, context.Position, marker, runLength))
        {
            MarkdownNode delimiterText = MarkdownNode.Create(MarkdownNodeKind.Text);
            delimiterText.SetLiteral(new string(marker, runLength));
            context.AppendInline(delimiterText);
            context.AdvanceBy(runLength);
            return true;
        }

        int closingStart = FindClosingEmphasisRun(
            context,
            context.Position + runLength,
            marker,
            runLength);
        if (closingStart < 0)
        {
            return false;
        }

        int closingLength = CountDelimiterRun(context.Source, closingStart, marker);
        if (runLength > closingLength)
        {
            int laterClosingStart = FindLastClosingEmphasisRun(
                context,
                closingStart + closingLength,
                context.Position,
                marker,
                runLength);
            if (laterClosingStart >= 0)
            {
                int laterClosingLength = CountDelimiterRun(context.Source, laterClosingStart, marker);
                int outerLength = runLength >= 2 && laterClosingLength >= 2 ? 2 : 1;
                AppendDelimitedInline(
                    context,
                    outerLength == 2 ? MarkdownNodeKind.StrongEmphasis : MarkdownNodeKind.Emphasis,
                    context.Position + outerLength,
                    laterClosingStart + laterClosingLength - outerLength,
                    outerLength);
                return true;
            }
        }

        if (runLength == closingLength)
        {
            int outerLength = runLength % 2 == 0 ? 2 : 1;
            AppendDelimitedInline(
                context,
                outerLength == 2 ? MarkdownNodeKind.StrongEmphasis : MarkdownNodeKind.Emphasis,
                context.Position + outerLength,
                closingStart + closingLength - outerLength,
                outerLength);
            return true;
        }

        if (closingLength > runLength)
        {
            int outerLength = runLength >= 2 ? 2 : 1;
            int usedClosingLength = runLength;
            if (runLength == 1
                && ContainsOpeningDelimiterRun(
                    context.Source,
                    context.Position + runLength,
                    closingStart,
                    marker))
            {
                usedClosingLength = closingLength;
            }

            AppendDelimitedInline(
                context,
                outerLength == 2 ? MarkdownNodeKind.StrongEmphasis : MarkdownNodeKind.Emphasis,
                context.Position + outerLength,
                closingStart + usedClosingLength - outerLength,
                outerLength);
            return true;
        }

        int delimiterLength = runLength >= 2 && closingLength >= 2 ? 2 : 1;
        int unusedOpeningLength = runLength - delimiterLength;
        if (unusedOpeningLength > 0)
        {
            MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
            text.SetLiteral(new string(marker, unusedOpeningLength));
            context.AppendInline(text);
        }

        AppendDelimitedInline(
            context,
            delimiterLength == 2 ? MarkdownNodeKind.StrongEmphasis : MarkdownNodeKind.Emphasis,
            context.Position + runLength,
            closingStart,
            delimiterLength);
        return true;
    }

    internal static int FindClosingEmphasisRun(
        MarkdownInlineParseContext context,
        int startIndex,
        char marker,
        int openingLength)
    {
        string source = context.Source;
        List<int> nestedDelimiterLengths = new List<int>();
        int index = startIndex;
        while (index < source.Length)
        {
            if (source[index] == '\\' && index + 1 < source.Length)
            {
                index = index + 2;
                continue;
            }

            if (source[index] == '`')
            {
                int backtickLength = CountBacktickRun(source, index);
                int backtickClose = FindClosingBacktickRun(source, index + backtickLength, backtickLength);
                if (backtickClose >= 0)
                {
                    index = backtickClose + backtickLength;
                    continue;
                }
            }

            if (source[index] == '<')
            {
                int autolinkEnd = FindAutolinkEnd(source, index);
                if (autolinkEnd > index)
                {
                    string autolinkText = source.Substring(index + 1, autolinkEnd - index - 1);
                    if (IsUriAutolink(autolinkText) || IsEmailAutolink(autolinkText))
                    {
                        index = autolinkEnd + 1;
                        continue;
                    }
                }

                int htmlEnd = FindHtmlInlineEnd(source, index);
                if (htmlEnd > index)
                {
                    index = htmlEnd;
                    continue;
                }
            }

            if (source[index] == '[')
            {
                int linkEnd = FindValidLinkEnd(context, index);
                if (linkEnd >= 0)
                {
                    index = linkEnd + 1;
                }
                else
                {
                    index = index + 1;
                }

                continue;
            }

            if (source[index] == ']')
            {
                index = index + 1;
                continue;
            }

            if (source[index] == marker)
            {
                int closingLength = CountDelimiterRun(source, index, marker);
                bool canOpen = CanOpenDelimiterRun(source, index, marker, closingLength);
                bool canClose = CanCloseDelimiterRun(source, index, marker, closingLength);
                if (canOpen && !canClose)
                {
                    nestedDelimiterLengths.Add(closingLength);
                    index = index + closingLength;
                    continue;
                }

                if (canClose && nestedDelimiterLengths.Count > 0)
                {
                    int remainingClosingLength = closingLength;
                    while (remainingClosingLength > 0 && nestedDelimiterLengths.Count > 0)
                    {
                        int lastIndex = nestedDelimiterLengths.Count - 1;
                        int nestedOpeningLength = nestedDelimiterLengths[lastIndex];
                        int usedLength = nestedOpeningLength >= 2 && remainingClosingLength >= 2 ? 2 : 1;
                        nestedOpeningLength = nestedOpeningLength - usedLength;
                        remainingClosingLength = remainingClosingLength - usedLength;
                        if (nestedOpeningLength == 0)
                        {
                            nestedDelimiterLengths.RemoveAt(lastIndex);
                        }
                        else
                        {
                            nestedDelimiterLengths[lastIndex] = nestedOpeningLength;
                        }
                    }

                    if (remainingClosingLength == 0 || nestedDelimiterLengths.Count > 0)
                    {
                        index = index + closingLength;
                        continue;
                    }

                    return index + closingLength - remainingClosingLength;
                }

                if (nestedDelimiterLengths.Count == 0
                    && canClose
                    && CanMatchDelimiterRuns(
                        source,
                        startIndex - openingLength,
                        openingLength,
                        index,
                        closingLength,
                        marker))
                {
                    return index;
                }

                index = index + closingLength;
                continue;
            }

            char otherMarker = marker == '*' ? '_' : '*';
            if (marker == '*' && source[index] == otherMarker)
            {
                int otherLength = CountDelimiterRun(source, index, otherMarker);
                if (otherLength >= 2
                    && CanOpenDelimiterRun(source, index, otherMarker, otherLength))
                {
                    int otherClose = FindClosingEmphasisRun(
                        context,
                        index + otherLength,
                        otherMarker,
                        otherLength);
                    if (otherClose >= 0)
                    {
                        index = otherClose + CountDelimiterRun(source, otherClose, otherMarker);
                        continue;
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool ContainsOpeningDelimiterRun(
        string source,
        int startIndex,
        int endIndex,
        char marker)
    {
        int index = startIndex;
        while (index < endIndex)
        {
            if (source[index] == marker)
            {
                int length = CountDelimiterRun(source, index, marker);
                if (CanOpenDelimiterRun(source, index, marker, length))
                {
                    return true;
                }

                index = index + length;
            }
            else
            {
                index = index + 1;
            }
        }

        return false;
    }

    internal static int FindLastClosingEmphasisRun(
        MarkdownInlineParseContext context,
        int startIndex,
        int openingStart,
        char marker,
        int openingLength)
    {
        string source = context.Source;
        int result = -1;
        int index = startIndex;
        while (index < source.Length)
        {
            if (source[index] == '\\' && index + 1 < source.Length)
            {
                index = index + 2;
                continue;
            }

            if (source[index] == '`')
            {
                int backtickLength = CountBacktickRun(source, index);
                int backtickClose = FindClosingBacktickRun(source, index + backtickLength, backtickLength);
                if (backtickClose >= 0)
                {
                    index = backtickClose + backtickLength;
                    continue;
                }
            }

            if (source[index] == '<')
            {
                int htmlEnd = FindHtmlInlineEnd(source, index);
                if (htmlEnd > index)
                {
                    index = htmlEnd;
                    continue;
                }
            }

            if (source[index] == '[')
            {
                int linkEnd = FindValidLinkEnd(context, index);
                if (linkEnd >= 0)
                {
                    index = linkEnd + 1;
                    continue;
                }
            }

            char otherMarker = marker == '*' ? '_' : '*';
            if (marker == '*' && source[index] == otherMarker)
            {
                int otherLength = CountDelimiterRun(source, index, otherMarker);
                if (otherLength >= 2
                    && CanOpenDelimiterRun(source, index, otherMarker, otherLength))
                {
                    int otherClose = FindClosingEmphasisRun(
                        context,
                        index + otherLength,
                        otherMarker,
                        otherLength);
                    if (otherClose >= 0)
                    {
                        index = otherClose + CountDelimiterRun(source, otherClose, otherMarker);
                        continue;
                    }
                }
            }

            if (source[index] == marker)
            {
                int closingLength = CountDelimiterRun(source, index, marker);
                if (CanCloseDelimiterRun(source, index, marker, closingLength)
                    && CanMatchDelimiterRuns(
                        source,
                        openingStart,
                        openingLength,
                        index,
                        closingLength,
                        marker))
                {
                    result = index;
                }

                index = index + closingLength;
                continue;
            }

            index = index + 1;
        }

        return result;
    }

    internal static int FindValidLinkEnd(MarkdownInlineParseContext context, int startIndex)
    {
        int labelEnd = FindLinkLabelEnd(context.Source, startIndex);
        if (labelEnd < 0)
        {
            return -1;
        }

        if (labelEnd + 1 < context.Source.Length && context.Source[labelEnd + 1] == '(')
        {
            MarkdownInlineLinkParseResult inlineResult = ParseInlineLinkDestinationAndTitle(
                context.Source,
                labelEnd + 1);
            if (inlineResult.Success)
            {
                return inlineResult.EndIndex;
            }
        }

        string textLabel = context.Source.Substring(startIndex + 1, labelEnd - startIndex - 1);
        if (labelEnd + 1 < context.Source.Length && context.Source[labelEnd + 1] == '[')
        {
            int referenceEnd = FindLinkLabelEnd(context.Source, labelEnd + 1);
            if (referenceEnd < 0)
            {
                return -1;
            }

            string referenceLabel = context.Source.Substring(labelEnd + 2, referenceEnd - labelEnd - 2);
            if (referenceLabel.Length == 0)
            {
                referenceLabel = textLabel;
            }

            if (FindLinkReferenceDefinition(context.ParseContext, referenceLabel) != null)
            {
                return referenceEnd;
            }

            return -1;
        }

        if (FindLinkReferenceDefinition(context.ParseContext, textLabel) != null)
        {
            return labelEnd;
        }

        return -1;
    }

    internal static bool CanMatchDelimiterRuns(
        string source,
        int openingStart,
        int openingLength,
        int closingStart,
        int closingLength,
        char marker)
    {
        bool openerCanClose = CanCloseDelimiterRun(source, openingStart, marker, openingLength);
        bool closerCanOpen = CanOpenDelimiterRun(source, closingStart, marker, closingLength);
        if (!openerCanClose && !closerCanOpen)
        {
            return true;
        }

        if ((openingLength + closingLength) % 3 != 0)
        {
            return true;
        }

        return openingLength % 3 == 0 && closingLength % 3 == 0;
    }

    internal static bool CanOpenDelimiterRun(string source, int startIndex, char marker, int runLength)
    {
        bool leftFlanking = IsLeftFlankingDelimiterRun(source, startIndex, runLength);
        if (marker == '*')
        {
            return leftFlanking;
        }

        bool rightFlanking = IsRightFlankingDelimiterRun(source, startIndex, runLength);
        return leftFlanking
            && (!rightFlanking || IsPunctuationBeforeDelimiterRun(source, startIndex));
    }

    internal static bool CanCloseDelimiterRun(string source, int startIndex, char marker, int runLength)
    {
        bool rightFlanking = IsRightFlankingDelimiterRun(source, startIndex, runLength);
        if (marker == '*')
        {
            return rightFlanking;
        }

        bool leftFlanking = IsLeftFlankingDelimiterRun(source, startIndex, runLength);
        return rightFlanking
            && (!leftFlanking || IsPunctuationAfterDelimiterRun(source, startIndex, runLength));
    }

    internal static bool IsLeftFlankingDelimiterRun(string source, int startIndex, int runLength)
    {
        int after = startIndex + runLength;
        bool afterWhitespace = after >= source.Length || MarkdownChar.IsWhitespace(source[after]);
        bool afterPunctuation = after < source.Length && MarkdownChar.IsPunctuation(source[after]);
        bool beforeWhitespace = startIndex == 0 || MarkdownChar.IsWhitespace(source[startIndex - 1]);
        bool beforePunctuation = startIndex > 0 && MarkdownChar.IsPunctuation(source[startIndex - 1]);
        return !afterWhitespace && (!afterPunctuation || beforeWhitespace || beforePunctuation);
    }

    internal static bool IsRightFlankingDelimiterRun(string source, int startIndex, int runLength)
    {
        int after = startIndex + runLength;
        bool afterWhitespace = after >= source.Length || MarkdownChar.IsWhitespace(source[after]);
        bool afterPunctuation = after < source.Length && MarkdownChar.IsPunctuation(source[after]);
        bool beforeWhitespace = startIndex == 0 || MarkdownChar.IsWhitespace(source[startIndex - 1]);
        bool beforePunctuation = startIndex > 0 && MarkdownChar.IsPunctuation(source[startIndex - 1]);
        return !beforeWhitespace && (!beforePunctuation || afterWhitespace || afterPunctuation);
    }

    internal static bool IsPunctuationBeforeDelimiterRun(string source, int startIndex)
    {
        return startIndex > 0 && MarkdownChar.IsPunctuation(source[startIndex - 1]);
    }

    internal static bool IsPunctuationAfterDelimiterRun(string source, int startIndex, int runLength)
    {
        int after = startIndex + runLength;
        return after < source.Length && MarkdownChar.IsPunctuation(source[after]);
    }

    internal static bool TryParseSoftLineBreak(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter())
        {
            return false;
        }

        int lineEndingLength = MarkdownChar.GetLineEndingLength(context.Source, context.Position);
        if (lineEndingLength == 0)
        {
            return false;
        }

        TrimTrailingSpacesFromLastText(context.Container);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.SoftLineBreak);
        context.AppendInline(node);
        context.AdvanceBy(lineEndingLength);
        return true;
    }

    internal static bool TryParseText(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (!context.HasCurrentCharacter())
        {
            return false;
        }

        int start = context.Position;
        int end = FindTextRunEnd(context.Source, start, context.ParseContext.Pipeline);
        if (end == start)
        {
            end = start + 1;
        }

        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        text.SetLiteral(context.Source.Substring(start, end - start));
        context.AppendInline(text);
        context.AdvanceBy(end - start);
        return true;
    }

    internal static int FindTextRunEnd(string source, int startIndex)
    {
        return FindTextRunEnd(source, startIndex, MarkdownPipeline.CommonMarkPreset());
    }

    internal static int FindTextRunEnd(string source, int startIndex, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (startIndex == source.Length)
        {
            return startIndex;
        }

        if (IsTextStoppingCharacter(source[startIndex]))
        {
            return startIndex;
        }

        string registeredRuleTriggers = GetRegisteredRuleTriggerCharacters(pipeline);
        int index = startIndex;
        while (index < source.Length && !IsTextStoppingCharacter(source[index]))
        {
            if (index > startIndex && registeredRuleTriggers.IndexOf(source[index]) >= 0)
            {
                return index;
            }

            if (index > startIndex && GfmInlineRules.IsPotentialBareAutolinkStart(source, index))
            {
                return index;
            }

            if (index > startIndex && CustomInlineDelimiterRules.IsPotentialDelimiterStart(pipeline, source, index))
            {
                return index;
            }

            index = index + 1;
        }

        return index;
    }

    internal static bool IsRegisteredRuleTrigger(MarkdownPipeline pipeline, char character)
    {
        return GetRegisteredRuleTriggerCharacters(pipeline).IndexOf(character) >= 0;
    }

    internal static string GetRegisteredRuleTriggerCharacters(MarkdownPipeline pipeline)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        string result = "";
        MarkdownRuleList rules = pipeline.InlineRules;
        int index = 0;
        while (index < rules.GetCount())
        {
            MarkdownRuleRegistration registration = rules.GetAt(index);
            IMarkdownInlineRule? rule = registration.Rule as IMarkdownInlineRule;
            if (rule != null)
            {
                string triggers = rule.GetTriggerCharacters();
                if (triggers == null)
                {
                    throw new InvalidOperationException("Inline rule trigger characters must not be null.");
                }

                result = result + triggers;
            }

            index = index + 1;
        }

        return result;
    }

    internal static bool IsTextStoppingCharacter(char character)
    {
        return character == '\n'
            || character == '\r'
            || character == '\\'
            || character == '`'
            || character == '&'
            || character == '*'
            || character == '_'
            || character == '['
            || character == ']'
            || character == '!'
            || character == '<'
            || character == '~'
            || character == '^'
            || character == '+'
            || character == '='
            || character == '$'
            || character == ':';
    }

    internal static bool HasHardLineBreakSpaces(string source, int lineEndingIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (lineEndingIndex < 0 || lineEndingIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("lineEndingIndex");
        }

        int count = 0;
        int index = lineEndingIndex - 1;
        while (index >= 0 && source[index] == ' ')
        {
            count = count + 1;
            index = index - 1;
        }

        return count >= 2;
    }

    internal static void TrimTrailingSpacesFromLastText(MarkdownNode container)
    {
        if (container == null)
        {
            throw new ArgumentNullException("container");
        }

        MarkdownNode? lastChild = container.GetLastChild();
        if (lastChild == null || lastChild.Kind != MarkdownNodeKind.Text)
        {
            return;
        }

        string literal = lastChild.Literal;
        int end = literal.Length;
        while (end > 0 && literal[end - 1] == ' ')
        {
            end = end - 1;
        }

        if (end == literal.Length)
        {
            return;
        }

        if (end == 0)
        {
            container.RemoveChild(lastChild);
        }
        else
        {
            lastChild.SetLiteral(literal.Substring(0, end));
        }
    }

    internal static int CountBacktickRun(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        int index = startIndex;
        while (index < source.Length && source[index] == '`')
        {
            index = index + 1;
        }

        return index - startIndex;
    }

    internal static int FindClosingBacktickRun(string source, int startIndex, int openingLength)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (openingLength <= 0)
        {
            throw new ArgumentOutOfRangeException("openingLength");
        }

        int index = startIndex;
        while (index < source.Length)
        {
            if (source[index] == '`')
            {
                int runLength = CountBacktickRun(source, index);
                if (runLength == openingLength)
                {
                    return index;
                }

                index = index + runLength;
            }
            else
            {
                index = index + 1;
            }
        }

        return -1;
    }

    internal static string NormalizeCodeSpanLiteral(string literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        string normalized = ReplaceLineEndingsWithSpaces(literal);
        if (normalized.Length >= 2
            && normalized[0] == ' '
            && normalized[normalized.Length - 1] == ' '
            && !IsAllSpaces(normalized))
        {
            return normalized.Substring(1, normalized.Length - 2);
        }

        return normalized;
    }

    internal static string ReplaceLineEndingsWithSpaces(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string result = "";
        int index = 0;
        while (index < text.Length)
        {
            int lineEndingLength = MarkdownChar.GetLineEndingLength(text, index);
            if (lineEndingLength > 0)
            {
                result = result + " ";
                index = index + lineEndingLength;
            }
            else
            {
                result = result + text[index].ToString();
                index = index + 1;
            }
        }

        return result;
    }

    internal static bool IsAllSpaces(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] != ' ')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int FindHtmlInlineEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex >= source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (source[startIndex] != '<')
        {
            return -1;
        }

        if (CommonMarkBlockRules.StartsWithAt(source, startIndex, "<!--"))
        {
            if (!IsValidHtmlCommentOpening(source, startIndex))
            {
                return FindHtmlInlineTerminator(source, startIndex + 4, ">");
            }

            return FindHtmlInlineTerminator(source, startIndex + 4, "-->");
        }

        if (CommonMarkBlockRules.StartsWithAt(source, startIndex, "<?"))
        {
            return FindHtmlInlineTerminator(source, startIndex + 2, "?>");
        }

        if (CommonMarkBlockRules.StartsWithAt(source, startIndex, "<![CDATA["))
        {
            return FindHtmlInlineTerminator(source, startIndex + 9, "]]>");
        }

        if (source.Length - startIndex > 2 && source[startIndex + 1] == '!' && CommonMarkBlockRules.IsAsciiUpper(source[startIndex + 2]))
        {
            return FindHtmlInlineTerminator(source, startIndex + 3, ">");
        }

        return FindHtmlInlineTagEnd(source, startIndex);
    }

    internal static int FindHtmlInlineTerminator(string source, int startIndex, string terminator)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (terminator == null)
        {
            throw new ArgumentNullException("terminator");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (terminator.Length == 0)
        {
            throw new ArgumentException("Terminator cannot be empty.");
        }

        int index = startIndex;
        while (index < source.Length)
        {
            if (CommonMarkBlockRules.StartsWithAt(source, index, terminator))
            {
                return index + terminator.Length;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindHtmlInlineTagEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex >= source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (source[startIndex] != '<')
        {
            return -1;
        }

        int index = startIndex + 1;
        bool closingTag = false;
        if (index < source.Length && source[index] == '/')
        {
            closingTag = true;
            index = index + 1;
        }

        if (index >= source.Length || !CommonMarkBlockRules.IsAsciiLetter(source[index]))
        {
            return -1;
        }

        index = index + 1;
        while (index < source.Length && CommonMarkBlockRules.IsHtmlTagNameCharacter(source[index]))
        {
            index = index + 1;
        }

        if (closingTag)
        {
            while (index < source.Length && MarkdownChar.IsWhitespace(source[index]))
            {
                index = index + 1;
            }

            if (index < source.Length && source[index] == '>')
            {
                return index + 1;
            }

            return -1;
        }

        while (index < source.Length)
        {
            if (source[index] == '>')
            {
                return index + 1;
            }

            if (source[index] == '/')
            {
                if (index + 1 < source.Length && source[index + 1] == '>')
                {
                    return index + 2;
                }

                return -1;
            }

            if (!MarkdownChar.IsWhitespace(source[index]))
            {
                return -1;
            }

            while (index < source.Length && MarkdownChar.IsWhitespace(source[index]))
            {
                index = index + 1;
            }

            if (index >= source.Length)
            {
                return -1;
            }

            if (source[index] == '>' || source[index] == '/')
            {
                continue;
            }

            if (!IsHtmlAttributeNameStart(source[index]))
            {
                return -1;
            }

            index = index + 1;
            while (index < source.Length && IsHtmlAttributeNameCharacter(source[index]))
            {
                index = index + 1;
            }

            int attributeEnd = index;
            while (index < source.Length && MarkdownChar.IsWhitespace(source[index]))
            {
                index = index + 1;
            }

            if (index < source.Length && source[index] == '=')
            {
                index = index + 1;
                while (index < source.Length && MarkdownChar.IsWhitespace(source[index]))
                {
                    index = index + 1;
                }

                if (index >= source.Length)
                {
                    return -1;
                }

                if (source[index] == '"' || source[index] == '\'')
                {
                    index = FindHtmlInlineQuotedAttributeEnd(source, index);
                    if (index < 0)
                    {
                        return -1;
                    }
                }
                else
                {
                    int valueStart = index;
                    while (index < source.Length && IsHtmlUnquotedAttributeValueCharacter(source[index]))
                    {
                        index = index + 1;
                    }

                    if (index == valueStart)
                    {
                        return -1;
                    }
                }
            }
            else
            {
                index = attributeEnd;
            }
        }

        return -1;
    }

    internal static int FindHtmlInlineQuotedAttributeEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex >= source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        char quote = source[startIndex];
        if (quote != '"' && quote != '\'')
        {
            return -1;
        }

        int index = startIndex + 1;
        while (index < source.Length)
        {
            if (source[index] == quote)
            {
                return index + 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsValidHtmlCommentOpening(string source, int startIndex)
    {
        int contentStart = startIndex + 4;
        if (contentStart >= source.Length || source[contentStart] == '>')
        {
            return false;
        }

        return contentStart + 1 >= source.Length
            || source[contentStart] != '-'
            || source[contentStart + 1] != '>';
    }

    internal static bool IsHtmlAttributeNameStart(char character)
    {
        return CommonMarkBlockRules.IsAsciiLetter(character)
            || character == '_'
            || character == ':';
    }

    internal static bool IsHtmlAttributeNameCharacter(char character)
    {
        return IsHtmlAttributeNameStart(character)
            || CommonMarkBlockRules.IsAsciiDigit(character)
            || character == '.'
            || character == '-';
    }

    internal static bool IsHtmlUnquotedAttributeValueCharacter(char character)
    {
        return !MarkdownChar.IsWhitespace(character)
            && character != '"'
            && character != '\''
            && character != '='
            && character != '<'
            && character != '>'
            && character != '`';
    }

    internal static int FindAutolinkEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex >= source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (source[startIndex] != '<')
        {
            return -1;
        }

        int index = startIndex + 1;
        while (index < source.Length)
        {
            if (source[index] == '>')
            {
                return index;
            }

            if (source[index] == '<'
                || MarkdownChar.IsLineEnding(source[index])
                || MarkdownChar.IsAsciiControl(source[index]))
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsUriAutolink(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int schemeEnd = GetAutolinkSchemeEnd(text);
        if (schemeEnd < 0)
        {
            return false;
        }

        int index = schemeEnd + 1;
        if (index >= text.Length)
        {
            return false;
        }

        while (index < text.Length)
        {
            if (!IsAutolinkUriCharacter(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int GetAutolinkSchemeEnd(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length == 0 || !MarkdownUriPolicy.IsAsciiLetter(text[0]))
        {
            return -1;
        }

        int index = 1;
        while (index < text.Length)
        {
            if (text[index] == ':')
            {
                int schemeLength = index;
                if (schemeLength >= 2 && schemeLength <= 32)
                {
                    return index;
                }

                return -1;
            }

            if (!MarkdownUriPolicy.IsSchemeCharacter(text[index]))
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsAutolinkUriCharacter(char character)
    {
        return character != '<'
            && character != '>'
            && !MarkdownChar.IsWhitespace(character)
            && !MarkdownChar.IsAsciiControl(character);
    }

    internal static bool IsEmailAutolink(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int atIndex = GetSingleAtSignIndex(text);
        if (atIndex <= 0 || atIndex + 1 >= text.Length)
        {
            return false;
        }

        int index = 0;
        while (index < atIndex)
        {
            if (!IsEmailLocalPartCharacter(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return IsEmailDomain(text, atIndex + 1);
    }

    internal static int GetSingleAtSignIndex(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int result = -1;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '@')
            {
                if (result >= 0)
                {
                    return -1;
                }

                result = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal static bool IsEmailLocalPartCharacter(char character)
    {
        return MarkdownUriPolicy.IsAsciiLetter(character)
            || character >= '0' && character <= '9'
            || character == '.'
            || character == '!'
            || character == '#'
            || character == '$'
            || character == '%'
            || character == '&'
            || character == '\''
            || character == '*'
            || character == '+'
            || character == '/'
            || character == '='
            || character == '?'
            || character == '^'
            || character == '_'
            || character == '`'
            || character == '{'
            || character == '|'
            || character == '}'
            || character == '~'
            || character == '-';
    }

    internal static bool IsEmailDomain(string text, int startIndex)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (startIndex < 0 || startIndex > text.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        int labelStart = startIndex;
        int index = startIndex;
        int dotCount = 0;
        int lastLabelStart = startIndex;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == '.')
            {
                if (!IsEmailDomainLabel(text, labelStart, index))
                {
                    return false;
                }

                if (index < text.Length)
                {
                    dotCount = dotCount + 1;
                }

                lastLabelStart = labelStart;
                labelStart = index + 1;
            }
            else if (!IsEmailDomainLabelCharacter(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        if (dotCount == 0)
        {
            return false;
        }

        return IsEmailTopLevelDomain(text, lastLabelStart);
    }

    internal static bool IsEmailDomainLabel(string text, int startIndex, int endIndex)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (startIndex < 0 || startIndex > text.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (endIndex < startIndex || endIndex > text.Length)
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        if (endIndex == startIndex)
        {
            return false;
        }

        if (text[startIndex] == '-' || text[endIndex - 1] == '-')
        {
            return false;
        }

        int index = startIndex;
        while (index < endIndex)
        {
            if (!IsEmailDomainLabelCharacter(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsEmailDomainLabelCharacter(char character)
    {
        return MarkdownUriPolicy.IsAsciiLetter(character)
            || character >= '0' && character <= '9'
            || character == '-';
    }

    internal static bool IsEmailTopLevelDomain(string text, int startIndex)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (startIndex < 0 || startIndex > text.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        int length = text.Length - startIndex;
        if (length < 2)
        {
            return false;
        }

        int index = startIndex;
        while (index < text.Length)
        {
            if (!MarkdownUriPolicy.IsAsciiLetter(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int FindLinkLabelEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex >= source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (source[startIndex] != '[')
        {
            return -1;
        }

        int depth = 0;
        int index = startIndex + 1;
        while (index < source.Length)
        {
            if (source[index] == '\\' && index + 1 < source.Length)
            {
                index = index + 2;
            }
            else if (source[index] == '`')
            {
                int openingLength = CountBacktickRun(source, index);
                int closingStart = FindClosingBacktickRun(source, index + openingLength, openingLength);
                if (closingStart >= 0)
                {
                    index = closingStart + openingLength;
                }
                else
                {
                    index = index + openingLength;
                }
            }
            else if (source[index] == '<')
            {
                int autolinkEnd = FindAutolinkEnd(source, index);
                if (autolinkEnd >= 0
                    && (IsUriAutolink(source.Substring(index + 1, autolinkEnd - index - 1))
                        || IsEmailAutolink(source.Substring(index + 1, autolinkEnd - index - 1))))
                {
                    index = autolinkEnd + 1;
                }
                else
                {
                    int htmlEnd = FindHtmlInlineEnd(source, index);
                    if (htmlEnd >= 0)
                    {
                        index = htmlEnd;
                    }
                    else
                    {
                        index = index + 1;
                    }
                }
            }
            else if (source[index] == '[')
            {
                depth = depth + 1;
                index = index + 1;
            }
            else if (source[index] == ']')
            {
                if (depth == 0)
                {
                    return index;
                }

                depth = depth - 1;
                index = index + 1;
            }
            else
            {
                index = index + 1;
            }
        }

        return -1;
    }

    internal static MarkdownInlineLinkParseResult ParseInlineLinkDestinationAndTitle(string source, int openParenIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (openParenIndex < 0 || openParenIndex >= source.Length)
        {
            throw new ArgumentOutOfRangeException("openParenIndex");
        }

        if (source[openParenIndex] != '(')
        {
            return new MarkdownInlineLinkParseResult();
        }

        int index = SkipInlineLinkWhitespace(source, openParenIndex + 1);
        if (index < source.Length && source[index] == ')')
        {
            return MarkdownInlineLinkParseResult.Create(true, index, "", "");
        }

        string destination = "";
        if (index < source.Length && source[index] == '<')
        {
            int destinationEnd = FindInlineLinkAngleDestinationEnd(source, index);
            if (destinationEnd < 0)
            {
                return new MarkdownInlineLinkParseResult();
            }

            destination = DecodeLinkAttributeText(source.Substring(index + 1, destinationEnd - index - 1));
            index = destinationEnd + 1;
        }
        else
        {
            int destinationEnd = FindInlineLinkBareDestinationEnd(source, index);
            if (destinationEnd < 0 || destinationEnd == index)
            {
                return new MarkdownInlineLinkParseResult();
            }

            destination = DecodeLinkAttributeText(source.Substring(index, destinationEnd - index));
            index = destinationEnd;
        }

        int whitespaceStart = index;
        index = SkipInlineLinkWhitespace(source, index);
        if (index < source.Length && source[index] == ')')
        {
            return MarkdownInlineLinkParseResult.Create(true, index, destination, "");
        }

        if (index == whitespaceStart)
        {
            return new MarkdownInlineLinkParseResult();
        }

        if (index >= source.Length)
        {
            return new MarkdownInlineLinkParseResult();
        }

        int titleEnd = FindInlineLinkTitleEnd(source, index);
        if (titleEnd < 0)
        {
            return new MarkdownInlineLinkParseResult();
        }

        string title = DecodeLinkAttributeText(source.Substring(index + 1, titleEnd - index - 1));
        index = SkipInlineLinkWhitespace(source, titleEnd + 1);
        if (index < source.Length && source[index] == ')')
        {
            return MarkdownInlineLinkParseResult.Create(true, index, destination, title);
        }

        return new MarkdownInlineLinkParseResult();
    }

    internal static string DecodeLinkAttributeText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string result = "";
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\\'
                && index + 1 < text.Length
                && MarkdownChar.IsAsciiPunctuation(text[index + 1]))
            {
                result = result + text[index + 1].ToString();
                index = index + 2;
            }
            else if (text[index] == '&')
            {
                MarkdownEntityDecodeResult entity = MarkdownEntity.DecodeAt(text, index);
                if (entity.Success)
                {
                    result = result + entity.Text;
                    index = index + entity.Length;
                }
                else
                {
                    result = result + text[index].ToString();
                    index = index + 1;
                }
            }
            else
            {
                result = result + text[index].ToString();
                index = index + 1;
            }
        }

        return result;
    }

    internal static int SkipInlineLinkWhitespace(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        int index = startIndex;
        while (index < source.Length
            && (MarkdownChar.IsSpaceOrTab(source[index]) || MarkdownChar.IsLineEnding(source[index])))
        {
            index = index + 1;
        }

        return index;
    }

    internal static int FindInlineLinkAngleDestinationEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex >= source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (source[startIndex] != '<')
        {
            return -1;
        }

        int index = startIndex + 1;
        while (index < source.Length)
        {
            if (MarkdownChar.IsLineEnding(source[index]) || source[index] == '<')
            {
                return -1;
            }

            if (source[index] == '>' && !CommonMarkBlockRules.IsEscapedAt(source, index))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindInlineLinkBareDestinationEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        int depth = 0;
        int index = startIndex;
        while (index < source.Length)
        {
            if (source[index] == '\\' && index + 1 < source.Length)
            {
                index = index + 2;
            }
            else if (MarkdownChar.IsSpaceOrTab(source[index]) || MarkdownChar.IsLineEnding(source[index]))
            {
                return index;
            }
            else if (source[index] == '(')
            {
                depth = depth + 1;
                index = index + 1;
            }
            else if (source[index] == ')')
            {
                if (depth == 0)
                {
                    return index;
                }

                depth = depth - 1;
                index = index + 1;
            }
            else
            {
                index = index + 1;
            }
        }

        return -1;
    }

    internal static int FindInlineLinkTitleEnd(string source, int startIndex)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex >= source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        char opener = source[startIndex];
        char closer = opener;
        if (opener == '(')
        {
            closer = ')';
        }
        else if (opener != '"' && opener != '\'')
        {
            return -1;
        }

        int index = startIndex + 1;
        while (index < source.Length)
        {
            if (source[index] == closer && !CommonMarkBlockRules.IsEscapedAt(source, index))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int CountDelimiterRun(string source, int startIndex, char marker)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (marker != '*' && marker != '_')
        {
            throw new ArgumentException("Delimiter marker must be '*' or '_'.");
        }

        int index = startIndex;
        while (index < source.Length && source[index] == marker)
        {
            index = index + 1;
        }

        return index - startIndex;
    }

    internal static int FindClosingDelimiterRun(string source, int startIndex, char marker, int delimiterLength)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (marker != '*' && marker != '_')
        {
            throw new ArgumentException("Delimiter marker must be '*' or '_'.");
        }

        if (delimiterLength <= 0)
        {
            throw new ArgumentOutOfRangeException("delimiterLength");
        }

        int index = startIndex;
        while (index < source.Length)
        {
            if (source[index] == marker)
            {
                int runLength = CountDelimiterRun(source, index, marker);
                if (runLength == delimiterLength && CanCloseDelimiter(source, index, marker, delimiterLength))
                {
                    return index;
                }

                index = index + runLength;
            }
            else
            {
                index = index + 1;
            }
        }

        return -1;
    }

    internal static bool CanOpenDelimiter(string source, int startIndex, char marker, int delimiterLength)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (marker != '*' && marker != '_')
        {
            throw new ArgumentException("Delimiter marker must be '*' or '_'.");
        }

        if (delimiterLength <= 0)
        {
            throw new ArgumentOutOfRangeException("delimiterLength");
        }

        int after = startIndex + delimiterLength;
        if (after >= source.Length || MarkdownChar.IsWhitespace(source[after]))
        {
            return false;
        }

        if (marker == '_' && startIndex > 0 && char.IsLetterOrDigit(source[startIndex - 1]) && char.IsLetterOrDigit(source[after]))
        {
            return false;
        }

        return true;
    }

    internal static bool CanCloseDelimiter(string source, int startIndex, char marker, int delimiterLength)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (startIndex < 0 || startIndex > source.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (marker != '*' && marker != '_')
        {
            throw new ArgumentException("Delimiter marker must be '*' or '_'.");
        }

        if (delimiterLength <= 0)
        {
            throw new ArgumentOutOfRangeException("delimiterLength");
        }

        if (startIndex == 0 || MarkdownChar.IsWhitespace(source[startIndex - 1]))
        {
            return false;
        }

        int after = startIndex + delimiterLength;
        if (marker == '_' && after < source.Length && char.IsLetterOrDigit(source[startIndex - 1]) && char.IsLetterOrDigit(source[after]))
        {
            return false;
        }

        return true;
    }

    internal static void AppendDelimitedInline(
        MarkdownInlineParseContext context,
        MarkdownNodeKind kind,
        int contentStart,
        int contentEnd,
        int delimiterLength)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (kind != MarkdownNodeKind.Emphasis && kind != MarkdownNodeKind.StrongEmphasis)
        {
            throw new ArgumentException("Node kind must be emphasis or strong emphasis.");
        }

        if (contentStart < 0 || contentStart > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("contentStart");
        }

        if (contentEnd < contentStart || contentEnd > context.Source.Length)
        {
            throw new ArgumentOutOfRangeException("contentEnd");
        }

        if (delimiterLength <= 0)
        {
            throw new ArgumentOutOfRangeException("delimiterLength");
        }

        MarkdownNode node = MarkdownNode.Create(kind);
        MarkdownInlineParseContext innerContext = MarkdownInlineParseContext.Create(
            context.ParseContext,
            node,
            context.Source.Substring(contentStart, contentEnd - contentStart));
        innerContext.InLink = context.InLink;
        InlineParser.Run(innerContext);
        context.AppendInline(node);
        context.AdvanceBy(contentEnd + delimiterLength - context.Position);
    }

    internal static bool IsLinkParsingProhibited(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return context.InLink;
    }
}
