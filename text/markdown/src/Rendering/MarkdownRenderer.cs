using System;
using System.IO;

namespace markdown;

/// <summary>
/// Renders a Markdown document as normalized Markdown.
/// </summary>
internal class MarkdownRenderer
{
    /// <summary>
    /// Renders the document as normalized Markdown using the CommonMark preset.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string Render(MarkdownDocument document)
    {
        return Render(document, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>
    /// Renders the document as normalized Markdown using the supplied pipeline.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string Render(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        StringWriter writer = new StringWriter();
        Render(document, writer, pipeline);
        return writer.ToString();
    }

    /// <summary>
    /// Writes normalized Markdown without closing or disposing the destination writer.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="writer">The destination writer. The caller retains ownership of it.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    internal static void Render(MarkdownDocument document, TextWriter writer, MarkdownPipeline pipeline)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownWriter markdownWriter = MarkdownWriter.Create(
            writer,
            pipeline.GetOptions().LineEnding,
            pipeline.GetOptions().MaximumOutputSize);
        MarkdownRenderContext context = new MarkdownRenderContext(markdownWriter, pipeline);
        RenderNode(context, document.Root);
    }

    internal static void RenderNode(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (ShouldRenderTrivia(context, node))
        {
            RenderTrivia(context, node);
            return;
        }

        switch (node.Kind)
        {
            case MarkdownNodeKind.Document:
                RenderDocument(context, node);
                break;
            case MarkdownNodeKind.BlockQuote:
                RenderBlockQuote(context, node);
                break;
            case MarkdownNodeKind.List:
                RenderList(context, node);
                break;
            case MarkdownNodeKind.ListItem:
                RenderListItem(context, node);
                break;
            case MarkdownNodeKind.TaskListItem:
                RenderListItem(context, node);
                break;
            case MarkdownNodeKind.Paragraph:
                RenderParagraph(context, node);
                break;
            case MarkdownNodeKind.Heading:
                RenderHeading(context, node);
                break;
            case MarkdownNodeKind.ThematicBreak:
                RenderThematicBreak(context, node);
                break;
            case MarkdownNodeKind.IndentedCodeBlock:
                RenderCodeBlock(context, node);
                break;
            case MarkdownNodeKind.FencedCodeBlock:
                RenderCodeBlock(context, node);
                break;
            case MarkdownNodeKind.MathBlock:
                RenderMathBlock(context, node);
                break;
            case MarkdownNodeKind.DiagramBlock:
                RenderDiagramBlock(context, node);
                break;
            case MarkdownNodeKind.AdmonitionBlock:
                RenderAdmonitionBlock(context, node);
                break;
            case MarkdownNodeKind.CustomContainerBlock:
                RenderCustomContainerBlock(context, node);
                break;
            case MarkdownNodeKind.CustomFencedBlock:
                RenderCustomFencedBlock(context, node);
                break;
            case MarkdownNodeKind.DetailsBlock:
                RenderDetailsBlock(context, node);
                break;
            case MarkdownNodeKind.DetailsSummary:
                RenderDetailsSummary(context, node);
                break;
            case MarkdownNodeKind.HtmlBlock:
                RenderHtmlBlock(context, node);
                break;
            case MarkdownNodeKind.LinkReferenceDefinition:
                RenderLinkReferenceDefinition(context, node);
                break;
            case MarkdownNodeKind.FrontMatter:
                RenderFrontMatter(context, node);
                break;
            case MarkdownNodeKind.AbbreviationDefinition:
                RenderAbbreviationDefinition(context, node);
                break;
            case MarkdownNodeKind.FootnoteDefinition:
                RenderFootnoteDefinition(context, node);
                break;
            case MarkdownNodeKind.Text:
                RenderText(context, node);
                break;
            case MarkdownNodeKind.SoftLineBreak:
                RenderSoftLineBreak(context, node);
                break;
            case MarkdownNodeKind.HardLineBreak:
                RenderHardLineBreak(context, node);
                break;
            case MarkdownNodeKind.CodeSpan:
                RenderCodeSpan(context, node);
                break;
            case MarkdownNodeKind.MathSpan:
                RenderMathSpan(context, node);
                break;
            case MarkdownNodeKind.Emoji:
                RenderEmoji(context, node);
                break;
            case MarkdownNodeKind.CustomInline:
                RenderCustomInline(context, node);
                break;
            case MarkdownNodeKind.Emphasis:
                RenderEmphasis(context, node);
                break;
            case MarkdownNodeKind.StrongEmphasis:
                RenderStrongEmphasis(context, node);
                break;
            case MarkdownNodeKind.Strikethrough:
                RenderStrikethrough(context, node);
                break;
            case MarkdownNodeKind.Link:
                RenderLink(context, node);
                break;
            case MarkdownNodeKind.Image:
                RenderImage(context, node);
                break;
            case MarkdownNodeKind.HtmlInline:
                RenderHtmlInline(context, node);
                break;
            case MarkdownNodeKind.FootnoteReference:
                RenderFootnoteReference(context, node);
                break;
            case MarkdownNodeKind.Abbreviation:
                RenderAbbreviation(context, node);
                break;
            case MarkdownNodeKind.Superscript:
                RenderWrappedInline(context, node, "^", "^");
                break;
            case MarkdownNodeKind.Subscript:
                RenderWrappedInline(context, node, "~", "~");
                break;
            case MarkdownNodeKind.InsertedText:
                RenderWrappedInline(context, node, "++", "++");
                break;
            case MarkdownNodeKind.MarkedText:
                RenderWrappedInline(context, node, "==", "==");
                break;
            case MarkdownNodeKind.DefinitionList:
                RenderDefinitionList(context, node);
                break;
            case MarkdownNodeKind.DefinitionTerm:
                RenderDefinitionTerm(context, node);
                break;
            case MarkdownNodeKind.DefinitionDescription:
                RenderDefinitionDescription(context, node);
                break;
            case MarkdownNodeKind.Table:
                RenderTable(context, node);
                break;
            case MarkdownNodeKind.TableHead:
                RenderChildren(context, node);
                break;
            case MarkdownNodeKind.TableBody:
                RenderChildren(context, node);
                break;
            case MarkdownNodeKind.TableRow:
                RenderTableRow(context, node);
                break;
            case MarkdownNodeKind.TableCell:
                RenderTableCell(context, node);
                break;
            default:
                RenderChildren(context, node);
                break;
        }

        if (node.Data.AttachedAttributes.Names.Count > 0
            && (node.Kind != MarkdownNodeKind.Heading
                || node.Data.AttachedAttributes.IsBlock))
        {
            WriteAttachedAttributeList(context, node);
            if (node.Data.AttachedAttributes.IsBlock)
            {
                WriteLineEnding(context);
            }
        }
    }

    internal static bool ShouldRenderTrivia(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        return context.Options.TrackTrivia
            && node.HasTrivia()
            && node.Trivia != null
            && !node.Trivia.IsModified;
    }

    internal static void RenderTrivia(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write(node.GetTriviaText());
    }

    internal static void RenderChildren(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        int index = 0;
        while (index < node.GetChildCount())
        {
            RenderNode(context, node.GetChild(index));
            index = index + 1;
        }
    }

    internal static void RenderDocument(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (CanRenderModifiedDocumentFromTrivia(context, node))
        {
            RenderModifiedDocumentFromTrivia(context, node);
            return;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            RenderNode(context, node.GetChild(index));
            if (index + 1 < node.GetChildCount())
            {
                WriteLineEnding(context);
            }

            index = index + 1;
        }
    }

    internal static bool CanRenderModifiedDocumentFromTrivia(
        MarkdownRenderContext context,
        MarkdownNode node)
    {
        ValidateContextAndNode(context, node);
        if (!context.Options.TrackTrivia
            || node.Trivia == null
            || !node.Trivia.IsModified
            || node.Trivia.Text.Length != node.Trivia.Span.GetLength())
        {
            return false;
        }

        int previousEnd = node.Trivia.Span.Start.Offset;
        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if (child.Span.IsEmpty()
                || child.Span.Start.Offset < previousEnd
                || child.Span.End.Offset > node.Trivia.Span.End.Offset)
            {
                return false;
            }

            previousEnd = child.Span.End.Offset;
            index = index + 1;
        }

        return true;
    }

    internal static void RenderModifiedDocumentFromTrivia(
        MarkdownRenderContext context,
        MarkdownNode node)
    {
        ValidateContextAndNode(context, node);
        if (node.Trivia == null)
        {
            throw new ArgumentException("Document trivia is required for source-preserving rendering.");
        }

        string original = node.Trivia.Text;
        int sourceStart = node.Trivia.Span.Start.Offset;
        int cursor = 0;
        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            int childStart = child.Span.Start.Offset - sourceStart;
            int childEnd = child.Span.End.Offset - sourceStart;
            if (childStart > cursor)
            {
                context.Writer.Write(original.Substring(cursor, childStart - cursor));
            }

            string rendered = RenderNodeToString(context, child);
            if (child.Trivia == null || child.Trivia.IsModified)
            {
                rendered = RemoveOneTrailingLineEnding(rendered);
            }

            context.Writer.Write(rendered);
            cursor = childEnd;
            index = index + 1;
        }

        if (cursor < original.Length)
        {
            context.Writer.Write(original.Substring(cursor));
        }
    }

    internal static string RemoveOneTrailingLineEnding(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.EndsWith("\r\n", StringComparison.Ordinal))
        {
            return text.Substring(0, text.Length - 2);
        }

        if (text.EndsWith("\r", StringComparison.Ordinal)
            || text.EndsWith("\n", StringComparison.Ordinal))
        {
            return text.Substring(0, text.Length - 1);
        }

        return text;
    }

    internal static void RenderParagraph(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string content = RenderParagraphContentToString(context, node);
        WriteLiteralWithNormalizedLineEndings(context, EscapeParagraphLineStarts(content));
        WriteLineEnding(context);
    }

    internal static string RenderParagraphContentToString(
        MarkdownRenderContext context,
        MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        StringWriter writer = new StringWriter();
        MarkdownWriter markdownWriter = MarkdownWriter.Create(
            writer,
            context.Options.LineEnding,
            context.Options.MaximumOutputSize);
        MarkdownRenderContext childContext = new MarkdownRenderContext(markdownWriter, context.Pipeline);
        childContext.Options.MaximumLineWidth = context.Options.MaximumLineWidth;
        if (context.Options.MaximumLineWidth > 0)
        {
            RenderWrappedInlineContainer(childContext, node);
        }
        else
        {
            RenderInlineContainer(childContext, node);
        }

        return writer.ToString();
    }

    internal static void RenderWrappedInlineContainer(
        MarkdownRenderContext context,
        MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        MarkdownWrapState state = new MarkdownWrapState();
        if (node.GetChildCount() == 0)
        {
            WriteWrappedText(context, state, EscapeText(node.Literal));
            return;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if (child.Kind == MarkdownNodeKind.Text)
            {
                WriteWrappedText(context, state, EscapeText(child.Literal));
            }
            else if (child.Kind == MarkdownNodeKind.SoftLineBreak)
            {
                state.PendingSpace = false;
                WriteLineEnding(context);
                state.Column = 0;
            }
            else if (child.Kind == MarkdownNodeKind.HardLineBreak)
            {
                state.PendingSpace = false;
                RenderNode(context, child);
                state.Column = 0;
            }
            else
            {
                WriteWrappedAtom(context, state, RenderNodeToString(context, child));
            }

            index = index + 1;
        }
    }

    internal static void WriteWrappedText(
        MarkdownRenderContext context,
        MarkdownWrapState state,
        string text)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (state == null)
        {
            throw new ArgumentNullException("state");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (IsWrappingWhitespace(text[index]))
            {
                state.PendingSpace = true;
                index = index + 1;
            }
            else
            {
                int start = index;
                while (index < text.Length && !IsWrappingWhitespace(text[index]))
                {
                    index = index + 1;
                }

                WriteWrappedAtom(context, state, text.Substring(start, index - start));
            }
        }
    }

    internal static void WriteWrappedAtom(
        MarkdownRenderContext context,
        MarkdownWrapState state,
        string text)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (state == null)
        {
            throw new ArgumentNullException("state");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length == 0)
        {
            return;
        }

        int firstLineLength = GetFirstLineLength(text);
        if (state.PendingSpace && state.Column > 0)
        {
            if (state.Column + 1 + firstLineLength > context.Options.MaximumLineWidth)
            {
                WriteLineEnding(context);
                state.Column = 0;
            }
            else
            {
                context.Writer.Write(" ");
                state.Column = state.Column + 1;
            }
        }

        state.PendingSpace = false;
        string normalized = NormalizeLineEndings(text);
        int index = 0;
        while (index < normalized.Length)
        {
            if (normalized[index] == '\n')
            {
                WriteLineEnding(context);
                state.Column = 0;
            }
            else
            {
                context.Writer.Write(normalized[index].ToString());
                state.Column = state.Column + 1;
            }

            index = index + 1;
        }
    }

    internal static string RenderNodeToString(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        StringWriter writer = new StringWriter();
        MarkdownWriter markdownWriter = MarkdownWriter.Create(
            writer,
            context.Options.LineEnding,
            context.Options.MaximumOutputSize);
        MarkdownRenderContext childContext = new MarkdownRenderContext(markdownWriter, context.Pipeline);
        childContext.Options.MaximumLineWidth = 0;
        RenderNode(childContext, node);
        return writer.ToString();
    }

    internal static bool IsWrappingWhitespace(char character)
    {
        return character == ' '
            || character == '\t'
            || character == '\r'
            || character == '\n';
    }

    internal static int GetFirstLineLength(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length && text[index] != '\r' && text[index] != '\n')
        {
            index = index + 1;
        }

        return index;
    }

    internal static void RenderHeading(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        int level = HtmlRenderer.GetHeadingLevel(node);
        context.Writer.Write(new string('#', level));
        context.Writer.Write(" ");
        RenderInlineContainer(context, node);
        WriteExplicitHeadingAttributes(context, node);
        WriteLineEnding(context);
    }

    internal static void WriteExplicitHeadingAttributes(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.GetAttributeValue("explicitAttributes") != "true")
        {
            return;
        }

        string attributes = GetExplicitHeadingAttributeText(node);
        if (attributes.Length == 0)
        {
            return;
        }

        context.Writer.Write(" ");
        context.Writer.Write(attributes);
    }

    internal static string GetExplicitHeadingAttributeText(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        StringWriter writer = new StringWriter();
        writer.Write("{");
        bool wrote = false;
        string id = node.GetAttributeValue("id");
        if (id.Length > 0)
        {
            writer.Write("#");
            writer.Write(id);
            wrote = true;
        }

        string classes = node.GetAttributeValue("class");
        if (classes.Length > 0)
        {
            string[] parts = classes.Split(new char[] { ' ' });
            int index = 0;
            while (index < parts.Length)
            {
                if (parts[index].Length > 0)
                {
                    if (wrote)
                    {
                        writer.Write(" ");
                    }

                    writer.Write(".");
                    writer.Write(parts[index]);
                    wrote = true;
                }

                index = index + 1;
            }
        }

        int attributeIndex = 0;
        while (attributeIndex < node.GetAttributeCount())
        {
            MarkdownAttribute attribute = node.Attributes[attributeIndex];
            if (ShouldRenderExplicitHeadingAttribute(attribute.Name))
            {
                if (wrote)
                {
                    writer.Write(" ");
                }

                writer.Write(attribute.Name);
                writer.Write("=\"");
                writer.Write(EscapeHeadingAttributeValue(attribute.Value));
                writer.Write("\"");
                wrote = true;
            }

            attributeIndex = attributeIndex + 1;
        }

        writer.Write("}");
        if (!wrote)
        {
            return "";
        }

        return writer.ToString();
    }

    internal static bool ShouldRenderExplicitHeadingAttribute(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        return name != "level"
            && name != "id"
            && name != "class"
            && name != "explicitAttributes";
    }

    internal static string EscapeHeadingAttributeValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringWriter writer = new StringWriter();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '"' || value[index] == '\\')
            {
                writer.Write("\\");
            }

            writer.Write(value[index]);
            index = index + 1;
        }

        return writer.ToString();
    }

    internal static void WriteAttachedAttributeList(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("{");
        bool wrote = false;
        int index = 0;
        while (index < node.Data.AttachedAttributes.Names.Count)
        {
            string name = node.Data.AttachedAttributes.Names[index];
            string value = node.GetAttributeValue(name);
            if (wrote)
            {
                context.Writer.Write(" ");
            }

            if (name == "id")
            {
                context.Writer.Write("#");
                context.Writer.Write(value);
            }
            else if (name == "class")
            {
                WriteAttachedClasses(context, value);
            }
            else
            {
                context.Writer.Write(name);
                context.Writer.Write("=\"");
                context.Writer.Write(EscapeHeadingAttributeValue(value));
                context.Writer.Write("\"");
            }

            wrote = true;
            index = index + 1;
        }

        context.Writer.Write("}");
    }

    internal static void WriteAttachedClasses(MarkdownRenderContext context, string value)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string[] parts = value.Split(new char[] { ' ' });
        int index = 0;
        bool wrote = false;
        while (index < parts.Length)
        {
            if (parts[index].Length > 0)
            {
                if (wrote)
                {
                    context.Writer.Write(" ");
                }

                context.Writer.Write(".");
                context.Writer.Write(parts[index]);
                wrote = true;
            }

            index = index + 1;
        }
    }

    internal static void RenderBlockQuote(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string content = RenderChildrenToStringWithPrefix(context, node, 2);
        WritePrefixedLines(context, content, "> ", "> ");
    }

    internal static void RenderList(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        int start = HtmlRenderer.ParsePositiveInteger(node.GetAttributeValue("start"));
        if (start < 1)
        {
            start = 1;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if (child.Kind == MarkdownNodeKind.ListItem || child.Kind == MarkdownNodeKind.TaskListItem)
            {
                string marker = GetListItemMarker(node, index, start);
                WriteListItem(context, child, marker);
            }
            else
            {
                RenderNode(context, child);
            }

            index = index + 1;
        }
    }

    internal static void RenderListItem(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteListItem(context, node, "- ");
    }

    internal static void RenderThematicBreak(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("---");
        WriteLineEnding(context);
    }

    internal static void RenderCodeBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.Kind == MarkdownNodeKind.IndentedCodeBlock)
        {
            WritePrefixedLines(context, node.Literal, "    ", "    ");
            return;
        }

        string fence = GetCodeFence(node.Literal);
        string info = "";
        if (node.Kind == MarkdownNodeKind.FencedCodeBlock)
        {
            info = node.GetAttributeValue("info");
        }

        context.Writer.Write(fence);
        if (info.Length > 0)
        {
            context.Writer.Write(info);
        }

        WriteLineEnding(context);
        WriteLiteralWithNormalizedLineEndings(context, node.Literal);
        if (node.Literal.Length == 0 || !EndsWithLineEnding(node.Literal))
        {
            WriteLineEnding(context);
        }

        context.Writer.Write(fence);
        WriteLineEnding(context);
    }

    internal static void RenderMathBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("$$");
        WriteLineEnding(context);
        WriteLiteralWithNormalizedLineEndings(context, node.Literal);
        if (node.Literal.Length == 0 || !EndsWithLineEnding(node.Literal))
        {
            WriteLineEnding(context);
        }

        context.Writer.Write("$$");
        WriteLineEnding(context);
    }

    internal static void RenderDiagramBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string fence = GetCodeFence(node.Literal);
        string info = node.GetAttributeValue("info");
        context.Writer.Write(fence);
        if (info.Length > 0)
        {
            context.Writer.Write(info);
        }

        WriteLineEnding(context);
        WriteLiteralWithNormalizedLineEndings(context, node.Literal);
        if (node.Literal.Length == 0 || !EndsWithLineEnding(node.Literal))
        {
            WriteLineEnding(context);
        }

        context.Writer.Write(fence);
        WriteLineEnding(context);
    }

    internal static void RenderAdmonitionBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.GetAttributeValue("style") == "alert")
        {
            RenderAlertAdmonitionBlock(context, node);
        }
        else
        {
            RenderBangAdmonitionBlock(context, node);
        }
    }

    internal static void RenderAlertAdmonitionBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string type = node.GetAttributeValue("type").ToUpperInvariant();
        if (type.Length == 0)
        {
            type = "NOTE";
        }

        string firstLine = "[!" + type + "]";
        string title = node.GetAttributeValue("title");
        if (title.Length > 0 && title != AdmonitionBlockRules.ToTitleCase(type))
        {
            firstLine = firstLine + " " + title;
        }

        context.Writer.Write("> ");
        context.Writer.Write(firstLine);
        WriteLineEnding(context);
        string content = RenderChildrenToStringWithPrefix(context, node, 2);
        if (content.Length > 0)
        {
            WritePrefixedLines(context, content, "> ", "> ");
        }
    }

    internal static void RenderBangAdmonitionBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string type = node.GetAttributeValue("type");
        if (type.Length == 0)
        {
            type = "note";
        }

        context.Writer.Write("!!! ");
        context.Writer.Write(type);
        string title = node.GetAttributeValue("title");
        if (title.Length > 0 && title != AdmonitionBlockRules.ToTitleCase(type))
        {
            context.Writer.Write(" \"");
            context.Writer.Write(title);
            context.Writer.Write("\"");
        }

        WriteLineEnding(context);
        string content = RenderChildrenToStringWithPrefix(context, node, 4);
        if (content.Length > 0)
        {
            WritePrefixedLines(context, content, "    ", "    ");
        }
    }

    internal static void RenderCustomContainerBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("::: ");
        context.Writer.Write(node.GetAttributeValue("name"));
        string title = node.GetAttributeValue("title");
        if (title.Length > 0)
        {
            context.Writer.Write(" \"");
            context.Writer.Write(title);
            context.Writer.Write("\"");
        }

        WriteLineEnding(context);
        string content = RenderChildrenToString(context, node);
        if (content.Length > 0)
        {
            WriteLiteralWithNormalizedLineEndings(context, content);
        }

        context.Writer.Write(":::");
        WriteLineEnding(context);
    }

    internal static void RenderCustomFencedBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string fence = GetCodeFence(node.Literal);
        string info = node.GetAttributeValue("info");
        context.Writer.Write(fence);
        if (info.Length > 0)
        {
            context.Writer.Write(info);
        }

        WriteLineEnding(context);
        WriteLiteralWithNormalizedLineEndings(context, node.Literal);
        if (node.Literal.Length == 0 || !EndsWithLineEnding(node.Literal))
        {
            WriteLineEnding(context);
        }

        context.Writer.Write(fence);
        WriteLineEnding(context);
    }

    internal static void RenderDetailsBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("???");
        if (node.GetAttributeValue("open") == "true")
        {
            context.Writer.Write("+");
        }

        context.Writer.Write(" details");

        string summary = node.GetAttributeValue("summary");
        if (summary.Length > 0)
        {
            context.Writer.Write(" \"");
            context.Writer.Write(EscapeHeadingAttributeValue(summary));
            context.Writer.Write("\"");
        }

        WriteLineEnding(context);
        int index = 0;
        if (node.GetChildCount() > 0
            && node.GetChild(0).Kind == MarkdownNodeKind.DetailsSummary)
        {
            index = 1;
        }

        while (index < node.GetChildCount())
        {
            RenderNode(context, node.GetChild(index));
            index = index + 1;
        }

        context.Writer.Write("???");
        WriteLineEnding(context);
    }

    internal static void RenderDetailsSummary(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        RenderInlineContainer(context, node);
        WriteLineEnding(context);
    }

    internal static void RenderHtmlBlock(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteLiteralWithNormalizedLineEndings(context, node.Literal);
        WriteLineEnding(context);
    }

    internal static void RenderLinkReferenceDefinition(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("[");
        context.Writer.Write(EscapeText(node.GetAttributeValue("label")));
        context.Writer.Write("]: ");
        WriteLinkDestination(context, node.GetAttributeValue("destination"));
        string title = node.GetAttributeValue("title");
        if (title.Length > 0)
        {
            context.Writer.Write(" \"");
            context.Writer.Write(EscapeTitle(title));
            context.Writer.Write("\"");
        }

        WriteLineEnding(context);
    }

    internal static void RenderFrontMatter(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string delimiter = node.GetAttributeValue("delimiter");
        if (delimiter.Length == 0)
        {
            delimiter = "---";
        }

        context.Writer.Write(delimiter);
        WriteLineEnding(context);
        WriteLiteralWithNormalizedLineEndings(context, node.Literal);
        if (node.Literal.Length == 0 || !EndsWithLineEnding(node.Literal))
        {
            WriteLineEnding(context);
        }

        context.Writer.Write(delimiter);
        WriteLineEnding(context);
    }

    internal static void RenderFootnoteDefinition(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string label = node.GetAttributeValue("label");
        string firstPrefix = "[^" + label + "]: ";
        string content = "";
        if (node.GetChildCount() > 0)
        {
            content = RenderChildrenToStringWithPrefix(context, node, firstPrefix.Length);
        }
        else if (node.Literal.Length > 0)
        {
            content = EscapeText(node.Literal);
        }

        WritePrefixedLines(context, content, firstPrefix, "    ");
    }

    internal static void RenderText(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write(EscapeText(node.Literal));
    }

    internal static void RenderSoftLineBreak(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteLineEnding(context);
    }

    internal static void RenderHardLineBreak(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("\\");
        WriteLineEnding(context);
    }

    internal static void RenderCodeSpan(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string delimiter = GetCodeSpanDelimiter(node.Literal);
        bool padding = NeedsCodeSpanPadding(node.Literal);
        context.Writer.Write(delimiter);
        if (padding)
        {
            context.Writer.Write(" ");
        }

        context.Writer.Write(node.Literal);
        if (padding)
        {
            context.Writer.Write(" ");
        }

        context.Writer.Write(delimiter);
    }

    internal static void RenderMathSpan(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("$");
        context.Writer.Write(node.Literal);
        context.Writer.Write("$");
    }

    internal static void RenderEmoji(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.Literal.Length > 0)
        {
            context.Writer.Write(node.Literal);
        }
        else
        {
            context.Writer.Write(":");
            context.Writer.Write(node.GetAttributeValue("name"));
            context.Writer.Write(":");
        }
    }

    internal static void RenderCustomInline(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string opening = node.GetAttributeValue("openingDelimiter");
        string closing = node.GetAttributeValue("closingDelimiter");
        context.Writer.Write(opening);
        RenderChildren(context, node);
        context.Writer.Write(closing);
    }

    internal static void RenderEmphasis(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("*");
        RenderChildren(context, node);
        context.Writer.Write("*");
    }

    internal static void RenderStrongEmphasis(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("**");
        RenderChildren(context, node);
        context.Writer.Write("**");
    }

    internal static void RenderStrikethrough(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("~~");
        RenderChildren(context, node);
        context.Writer.Write("~~");
    }

    internal static void RenderLink(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("[");
        RenderChildren(context, node);
        context.Writer.Write("](");
        WriteLinkDestination(context, node.GetAttributeValue("destination"));
        WriteOptionalTitle(context, node);
        context.Writer.Write(")");
    }

    internal static void RenderImage(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("![");
        RenderChildren(context, node);
        context.Writer.Write("](");
        WriteLinkDestination(context, node.GetAttributeValue("destination"));
        WriteOptionalTitle(context, node);
        context.Writer.Write(")");
    }

    internal static void RenderHtmlInline(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write(node.Literal);
    }

    internal static void RenderFootnoteReference(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("[^");
        context.Writer.Write(node.GetAttributeValue("label"));
        context.Writer.Write("]");
    }

    internal static void RenderAbbreviation(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else
        {
            context.Writer.Write(EscapeText(node.Literal));
        }
    }

    internal static void RenderAbbreviationDefinition(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("*[");
        context.Writer.Write(node.GetAttributeValue("label"));
        context.Writer.Write("]: ");
        context.Writer.Write(node.GetAttributeValue("title"));
        WriteLineEnding(context);
    }

    internal static void RenderWrappedInline(MarkdownRenderContext context, MarkdownNode node, string openingDelimiter, string closingDelimiter)
    {
        ValidateContextAndNode(context, node);
        if (openingDelimiter == null)
        {
            throw new ArgumentNullException("openingDelimiter");
        }

        if (closingDelimiter == null)
        {
            throw new ArgumentNullException("closingDelimiter");
        }

        context.Writer.Write(openingDelimiter);
        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else
        {
            context.Writer.Write(EscapeText(node.Literal));
        }

        context.Writer.Write(closingDelimiter);
    }

    internal static void RenderDefinitionList(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        RenderChildren(context, node);
    }

    internal static void RenderDefinitionTerm(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        RenderInlineContainer(context, node);
        WriteLineEnding(context);
    }

    internal static void RenderDefinitionDescription(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string content = "";
        if (node.GetChildCount() > 0)
        {
            content = RenderChildrenToStringWithPrefix(context, node, 2);
        }
        else if (node.Literal.Length > 0)
        {
            content = EscapeText(node.Literal);
        }

        WritePrefixedLines(context, content, ": ", "  ");
    }

    internal static void RenderTable(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        MarkdownNode? headerRow = GetTableHeaderRow(node);
        if (headerRow == null)
        {
            RenderChildren(context, node);
            return;
        }

        RenderTableRow(context, headerRow);
        RenderTableDelimiterRow(context, headerRow);

        MarkdownNode? body = GetTableBody(node);
        if (body != null)
        {
            RenderChildren(context, body);
        }
    }

    internal static void RenderTableRow(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("|");
        int index = 0;
        while (index < node.GetChildCount())
        {
            context.Writer.Write(" ");
            RenderTableCell(context, node.GetChild(index));
            context.Writer.Write(" |");
            index = index + 1;
        }

        WriteLineEnding(context);
    }

    internal static void RenderTableCell(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write(GetTableCellMarkdown(context, node));
    }

    internal static void RenderTableDelimiterRow(MarkdownRenderContext context, MarkdownNode headerRow)
    {
        ValidateContextAndNode(context, headerRow);

        context.Writer.Write("|");
        int index = 0;
        while (index < headerRow.GetChildCount())
        {
            context.Writer.Write(" ");
            context.Writer.Write(GetTableDelimiterCell(headerRow.GetChild(index)));
            context.Writer.Write(" |");
            index = index + 1;
        }

        WriteLineEnding(context);
    }

    internal static MarkdownNode? GetTableHeaderRow(MarkdownNode table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        int index = 0;
        while (index < table.GetChildCount())
        {
            MarkdownNode child = table.GetChild(index);
            if (child.Kind == MarkdownNodeKind.TableHead && child.GetChildCount() > 0)
            {
                return child.GetChild(0);
            }

            index = index + 1;
        }

        return null;
    }

    internal static MarkdownNode? GetTableBody(MarkdownNode table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        int index = 0;
        while (index < table.GetChildCount())
        {
            MarkdownNode child = table.GetChild(index);
            if (child.Kind == MarkdownNodeKind.TableBody)
            {
                return child;
            }

            index = index + 1;
        }

        return null;
    }

    internal static string GetTableDelimiterCell(MarkdownNode cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        string alignment = cell.GetAttributeValue("align");
        if (alignment == "left")
        {
            return ":---";
        }

        if (alignment == "right")
        {
            return "---:";
        }

        if (alignment == "center")
        {
            return ":---:";
        }

        return "---";
    }

    internal static string GetTableCellMarkdown(MarkdownRenderContext context, MarkdownNode cell)
    {
        ValidateContextAndNode(context, cell);

        string content = "";
        if (cell.GetChildCount() > 0)
        {
            content = RenderChildrenToString(context, cell);
        }
        else
        {
            content = EscapeText(cell.Literal);
        }

        return EscapeTableCellContent(content);
    }

    internal static string EscapeTableCellContent(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        string normalized = NormalizeLineEndings(content);
        StringWriter writer = new StringWriter();
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character == '|')
            {
                writer.Write("\\|");
            }
            else if (character == '\n')
            {
                writer.Write(" ");
            }
            else
            {
                writer.Write(character);
            }

            index = index + 1;
        }

        return writer.ToString().Trim();
    }

    internal static void RenderInlineContainer(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else if (node.Literal.Length > 0)
        {
            context.Writer.Write(EscapeText(node.Literal));
        }
    }

    internal static void WriteLineEnding(MarkdownRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        context.Writer.WriteSpecificLineEnding(context.Options.LineEnding);
    }

    internal static void WriteListItem(MarkdownRenderContext context, MarkdownNode node, string marker)
    {
        ValidateContextAndNode(context, node);
        if (marker == null)
        {
            throw new ArgumentNullException("marker");
        }

        string firstPrefix = marker;
        if (node.Kind == MarkdownNodeKind.TaskListItem)
        {
            firstPrefix = marker + GetTaskListMarkdownMarker(node) + " ";
        }

        string content = RenderChildrenToStringWithPrefix(
            context,
            node,
            firstPrefix.Length);
        string continuation = new string(' ', firstPrefix.Length);
        WritePrefixedLines(context, content, firstPrefix, continuation);
    }

    internal static void WritePrefixedLines(MarkdownRenderContext context, string content, string firstPrefix, string continuationPrefix)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (firstPrefix == null)
        {
            throw new ArgumentNullException("firstPrefix");
        }

        if (continuationPrefix == null)
        {
            throw new ArgumentNullException("continuationPrefix");
        }

        string normalized = StripTrailingLineEnding(NormalizeLineEndings(content));
        if (normalized.Length == 0)
        {
            context.Writer.Write(TrimMarkerPadding(firstPrefix));
            WriteLineEnding(context);
            return;
        }

        string[] lines = normalized.Split(new char[] { '\n' });
        int index = 0;
        while (index < lines.Length)
        {
            if (index == 0)
            {
                context.Writer.Write(firstPrefix);
            }
            else
            {
                context.Writer.Write(continuationPrefix);
            }

            context.Writer.Write(lines[index]);
            WriteLineEnding(context);
            index = index + 1;
        }
    }

    internal static string RenderChildrenToString(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        StringWriter writer = new StringWriter();
        MarkdownWriter markdownWriter = new MarkdownWriter(writer);
        MarkdownRenderContext childContext = new MarkdownRenderContext(markdownWriter, context.Pipeline);
        childContext.Options.MaximumLineWidth = context.Options.MaximumLineWidth;
        RenderChildren(childContext, node);
        return writer.ToString();
    }

    internal static string RenderChildrenToStringWithPrefix(
        MarkdownRenderContext context,
        MarkdownNode node,
        int prefixWidth)
    {
        ValidateContextAndNode(context, node);
        if (prefixWidth < 0)
        {
            throw new ArgumentOutOfRangeException("prefixWidth");
        }

        StringWriter writer = new StringWriter();
        MarkdownWriter markdownWriter = new MarkdownWriter(writer);
        MarkdownRenderContext childContext = new MarkdownRenderContext(markdownWriter, context.Pipeline);
        childContext.Options.MaximumLineWidth = context.Options.MaximumLineWidth;
        if (childContext.Options.MaximumLineWidth > 0)
        {
            childContext.Options.MaximumLineWidth = childContext.Options.MaximumLineWidth - prefixWidth;
            if (childContext.Options.MaximumLineWidth < 1)
            {
                childContext.Options.MaximumLineWidth = 1;
            }
        }

        RenderChildren(childContext, node);
        return writer.ToString();
    }

    internal static string GetListItemMarker(MarkdownNode list, int index, int start)
    {
        if (list == null)
        {
            throw new ArgumentNullException("list");
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (list.GetAttributeValue("type") == "ordered")
        {
            return ((long)start + index).ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ". ";
        }

        return "- ";
    }

    internal static string GetTaskListMarkdownMarker(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (HtmlRenderer.IsTaskListItemChecked(node))
        {
            return "[x]";
        }

        return "[ ]";
    }

    internal static string GetCodeFence(string literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        int longest = GetLongestRun(literal, '`');
        if (longest < 3)
        {
            longest = 3;
        }
        else
        {
            longest = longest + 1;
        }

        return new string('`', longest);
    }

    internal static string GetCodeSpanDelimiter(string literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        return new string('`', GetLongestRun(literal, '`') + 1);
    }

    internal static bool NeedsCodeSpanPadding(string literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        return literal.Length == 0 || literal[0] == '`' || literal[literal.Length - 1] == '`';
    }

    internal static int GetLongestRun(string text, char character)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int longest = 0;
        int current = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == character)
            {
                current = current + 1;
                if (current > longest)
                {
                    longest = current;
                }
            }
            else
            {
                current = 0;
            }

            index = index + 1;
        }

        return longest;
    }

    internal static void WriteLinkDestination(MarkdownRenderContext context, string destination)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (NeedsAngleDestination(destination))
        {
            context.Writer.Write("<");
            context.Writer.Write(EscapeAngleDestination(destination));
            context.Writer.Write(">");
        }
        else
        {
            context.Writer.Write(EscapeBareDestination(destination));
        }
    }

    internal static void WriteOptionalTitle(MarkdownRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string title = node.GetAttributeValue("title");
        if (title.Length > 0)
        {
            context.Writer.Write(" \"");
            context.Writer.Write(EscapeTitle(title));
            context.Writer.Write("\"");
        }
    }

    internal static bool NeedsAngleDestination(string destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        int index = 0;
        while (index < destination.Length)
        {
            char character = destination[index];
            if (MarkdownChar.IsWhitespace(character)
                || character == '('
                || character == ')'
                || character == '<'
                || character == '>')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string EscapeText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringWriter writer = new StringWriter();
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (NeedsTextEscape(character))
            {
                writer.Write("\\");
            }

            writer.Write(character);
            index = index + 1;
        }

        return writer.ToString();
    }

    internal static bool NeedsTextEscape(char character)
    {
        return character == '\\'
            || character == '`'
            || character == '*'
            || character == '_'
            || character == '['
            || character == ']'
            || character == '<'
            || character == '>'
            || character == '&'
            || character == '!';
    }

    internal static string EscapeParagraphLineStarts(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string normalized = NormalizeLineEndings(text);
        StringWriter writer = new StringWriter();
        int lineStart = 0;
        int index = 0;
        while (index <= normalized.Length)
        {
            if (index == normalized.Length || normalized[index] == '\n')
            {
                string line = normalized.Substring(lineStart, index - lineStart);
                int escapeIndex = GetParagraphLineEscapeIndex(line);
                if (HasIndentedParagraphLine(line))
                {
                    if (line[0] == '\t')
                    {
                        writer.Write("&#9;");
                    }
                    else
                    {
                        writer.Write("&#32;");
                    }

                    writer.Write(line.Substring(1));
                }
                else if (escapeIndex < 0)
                {
                    writer.Write(line);
                }
                else
                {
                    writer.Write(line.Substring(0, escapeIndex));
                    writer.Write("\\");
                    writer.Write(line.Substring(escapeIndex));
                }

                if (index < normalized.Length)
                {
                    writer.Write("\n");
                }

                lineStart = index + 1;
            }

            index = index + 1;
        }

        return writer.ToString();
    }

    internal static bool HasIndentedParagraphLine(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (line.Length > 0 && line[0] == '\t')
        {
            return true;
        }

        return line.Length >= 4
            && line[0] == ' '
            && line[1] == ' '
            && line[2] == ' '
            && line[3] == ' ';
    }

    internal static int GetParagraphLineEscapeIndex(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int start = 0;
        while (start < line.Length && start < 3 && line[start] == ' ')
        {
            start = start + 1;
        }

        if (start >= line.Length || line[start] == '\\')
        {
            return -1;
        }

        char first = line[start];
        if (first == '#')
        {
            int end = start;
            while (end < line.Length && end - start < 6 && line[end] == '#')
            {
                end = end + 1;
            }

            if (end == line.Length || line[end] == ' ' || line[end] == '\t')
            {
                return start;
            }
        }

        if ((first == '>' || first == '-' || first == '+')
            && (start + 1 == line.Length
                || line[start + 1] == ' '
                || line[start + 1] == '\t'))
        {
            return start;
        }

        if (first == '~'
            && start + 2 < line.Length
            && line[start + 1] == '~'
            && line[start + 2] == '~')
        {
            return start;
        }

        if (first == '-' || first == '=' || first == '~')
        {
            int markerEnd = start;
            while (markerEnd < line.Length
                && (line[markerEnd] == first || line[markerEnd] == ' ' || line[markerEnd] == '\t'))
            {
                markerEnd = markerEnd + 1;
            }

            if (markerEnd == line.Length && CountParagraphMarker(line, start, first) >= 3)
            {
                return start;
            }
        }

        if (first >= '0' && first <= '9')
        {
            int digitEnd = start;
            while (digitEnd < line.Length
                && digitEnd - start < 9
                && line[digitEnd] >= '0'
                && line[digitEnd] <= '9')
            {
                digitEnd = digitEnd + 1;
            }

            if (digitEnd < line.Length
                && (line[digitEnd] == '.' || line[digitEnd] == ')')
                && digitEnd + 1 < line.Length
                && (line[digitEnd + 1] == ' ' || line[digitEnd + 1] == '\t'))
            {
                return digitEnd;
            }
        }

        return -1;
    }

    internal static int CountParagraphMarker(string line, int start, char marker)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int count = 0;
        int index = start;
        while (index < line.Length)
        {
            if (line[index] == marker)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static string EscapeBareDestination(string destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        return EscapeCharacters(destination, "\\()");
    }

    internal static string EscapeAngleDestination(string destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        return EscapeCharacters(destination, "\\>");
    }

    internal static string EscapeTitle(string title)
    {
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        return EscapeCharacters(title, "\\\"");
    }

    internal static string EscapeCharacters(string text, string characters)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (characters == null)
        {
            throw new ArgumentNullException("characters");
        }

        StringWriter writer = new StringWriter();
        int index = 0;
        while (index < text.Length)
        {
            if (ContainsCharacter(characters, text[index]))
            {
                writer.Write("\\");
            }

            writer.Write(text[index]);
            index = index + 1;
        }

        return writer.ToString();
    }

    internal static bool ContainsCharacter(string text, char character)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == character)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void WriteLiteralWithNormalizedLineEndings(MarkdownRenderContext context, string literal)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        string normalized = NormalizeLineEndings(literal);
        int index = 0;
        while (index < normalized.Length)
        {
            if (normalized[index] == '\n')
            {
                WriteLineEnding(context);
            }
            else
            {
                context.Writer.Write(normalized[index].ToString());
            }

            index = index + 1;
        }
    }

    internal static string NormalizeLineEndings(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    internal static bool EndsWithLineEnding(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return text.Length > 0 && (text[text.Length - 1] == '\n' || text[text.Length - 1] == '\r');
    }

    internal static string StripTrailingLineEnding(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length > 0 && text[text.Length - 1] == '\n')
        {
            return text.Substring(0, text.Length - 1);
        }

        return text;
    }

    internal static string TrimMarkerPadding(string marker)
    {
        if (marker == null)
        {
            throw new ArgumentNullException("marker");
        }

        int end = marker.Length;
        while (end > 0 && marker[end - 1] == ' ')
        {
            end = end - 1;
        }

        return marker.Substring(0, end);
    }

    internal static void ValidateContextAndNode(MarkdownRenderContext context, MarkdownNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }
    }
}
