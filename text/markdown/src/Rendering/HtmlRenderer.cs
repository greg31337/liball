using System;
using System.Collections.Generic;
using System.IO;

namespace markdown;

/// <summary>
/// Renders a Markdown document as HTML with the pipeline's safety and formatting rules.
/// </summary>
/// <remarks>
/// Raw HTML and unsafe destinations are suppressed by default. A supplied pipeline controls those safety choices, line endings, XHTML syntax, resource limits, and custom rendering hooks.
/// </remarks>
internal class HtmlRenderer
{
    /// <summary>
    /// Renders the document as HTML using the CommonMark preset.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string Render(MarkdownDocument document)
    {
        return Render(document, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>
    /// Renders the document as HTML using the supplied pipeline.
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
    /// Writes the document as HTML without closing or disposing the destination writer.
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

        HtmlWriter htmlWriter = HtmlWriter.Create(writer, pipeline.GetOptions().MaximumOutputSize);
        HtmlRenderContext context = new HtmlRenderContext(htmlWriter, pipeline);
        RenderNode(context, document.Root);
    }

    internal static void RenderEvents(
        MarkdownEvent[] events,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        if (events == null)
        {
            throw new ArgumentNullException("events");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownDocument document = MarkdownEventWriter.WriteAll(events);
        Render(document, writer, pipeline);
    }

    internal static void RenderNode(HtmlRenderContext context, MarkdownNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (TryRenderWithRules(context, node))
        {
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
                RenderTaskListItem(context, node);
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
            case MarkdownNodeKind.ExtensionBlock:
                RenderExtensionBlock(context, node);
                break;
            case MarkdownNodeKind.ExtensionInline:
                RenderExtensionInline(context, node);
                break;
            case MarkdownNodeKind.HtmlBlock:
                RenderHtmlBlock(context, node);
                break;
            case MarkdownNodeKind.LinkReferenceDefinition:
                break;
            case MarkdownNodeKind.FrontMatter:
                break;
            case MarkdownNodeKind.AbbreviationDefinition:
                break;
            case MarkdownNodeKind.FootnoteDefinition:
                break;
            case MarkdownNodeKind.Attribute:
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
                RenderWrappedInline(context, node, "sup");
                break;
            case MarkdownNodeKind.Subscript:
                RenderWrappedInline(context, node, "sub");
                break;
            case MarkdownNodeKind.InsertedText:
                RenderWrappedInline(context, node, "ins");
                break;
            case MarkdownNodeKind.MarkedText:
                RenderWrappedInline(context, node, "mark");
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
                RenderTableHead(context, node);
                break;
            case MarkdownNodeKind.TableBody:
                RenderTableBody(context, node);
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
    }

    internal static void RenderDocument(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        CollectFootnoteReferences(context, node);
        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if (child.Kind != MarkdownNodeKind.FootnoteDefinition)
            {
                RenderNode(context, child);
            }

            index = index + 1;
        }

        RenderFootnoteSection(context, node);
    }

    /// <summary>
    /// Renders each direct child of a node with the active HTML context.
    /// </summary>
    /// <remarks>
    /// Custom link and container hooks can use this method to preserve normal rendering for their nested inline content.
    /// </remarks>
    /// <param name="context">The active HTML rendering context.</param>
    /// <param name="node">The node whose children are rendered.</param>
    internal static void RenderChildren(HtmlRenderContext context, MarkdownNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            RenderNode(context, node.GetChild(index));
            index = index + 1;
        }
    }

    internal static void RenderParagraph(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (IsParagraphInTightList(node))
        {
            RenderChildren(context, node);
            if (node.GetNextSibling() != null)
            {
                WriteLineEnding(context);
            }

            return;
        }

        context.Writer.Write("<p");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</p>");
        WriteLineEnding(context);
    }

    internal static void RenderHeading(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        int level = GetHeadingLevel(node);
        context.Writer.Write("<h" + level.ToString());
        string id = node.GetAttributeValue("id");
        if (id.Length > 0)
        {
            context.Writer.Write(" id=\"");
            context.Writer.WriteEscapedAttribute(id);
            context.Writer.Write("\"");
        }

        WriteHeadingAttributes(context, node);
        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</h" + level.ToString() + ">");
        WriteLineEnding(context);
    }

    internal static void WriteHeadingAttributes(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        int index = 0;
        while (index < node.GetAttributeCount())
        {
            MarkdownAttribute attribute = node.Attributes[index];
            if (ShouldRenderHeadingAttribute(attribute.Name))
            {
                context.Writer.Write(" ");
                context.Writer.Write(attribute.Name);
                context.Writer.Write("=\"");
                context.Writer.WriteEscapedAttribute(attribute.Value);
                context.Writer.Write("\"");
            }

            index = index + 1;
        }
    }

    internal static bool ShouldRenderHeadingAttribute(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        return name != "level"
            && name != "id"
            && name != "explicitAttributes";
    }

    internal static void WriteAttachedAttributes(HtmlRenderContext context, MarkdownNode node)
    {
        WriteAttachedAttributesExcluding(context, node, "");
    }

    internal static void WriteAttachedAttributesExcluding(
        HtmlRenderContext context,
        MarkdownNode node,
        string excludedName)
    {
        ValidateContextAndNode(context, node);
        if (excludedName == null)
        {
            throw new ArgumentNullException("excludedName");
        }

        MarkdownAttachedAttributeData attached = node.Data.AttachedAttributes;
        int index = 0;
        while (index < attached.Names.Count)
        {
            string name = attached.Names[index];
            if (name != excludedName
                && HeadingAttributeParser.IsAttributeName(name)
                && ShouldRenderAttachedAttribute(node, name))
            {
                context.Writer.Write(" ");
                context.Writer.Write(name);
                context.Writer.Write("=\"");
                context.Writer.WriteEscapedAttribute(node.GetAttributeValue(name));
                context.Writer.Write("\"");
            }

            index = index + 1;
        }
    }

    internal static bool ShouldRenderAttachedAttribute(MarkdownNode node, string name)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (node.Kind == MarkdownNodeKind.DetailsBlock && name == "open")
        {
            return false;
        }

        if ((node.Kind == MarkdownNodeKind.Link
                || node.Kind == MarkdownNodeKind.Image
                || node.Kind == MarkdownNodeKind.Abbreviation)
            && name == "title")
        {
            return false;
        }

        if (node.Kind == MarkdownNodeKind.List && name == "start")
        {
            return false;
        }

        if (node.Kind == MarkdownNodeKind.TableCell && name == "align")
        {
            return false;
        }

        if (node.Kind == MarkdownNodeKind.FootnoteReference && name == "id")
        {
            return false;
        }

        return true;
    }

    internal static void WriteAttachedClassSuffix(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.Data.AttachedAttributes.Names.Contains("class"))
        {
            string classes = node.GetAttributeValue("class");
            if (classes.Length > 0)
            {
                context.Writer.Write(" ");
                context.Writer.WriteEscapedAttribute(classes);
            }
        }
    }

    internal static void RenderBlockQuote(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<blockquote");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteLineEnding(context);
        RenderChildren(context, node);
        context.Writer.Write("</blockquote>");
        WriteLineEnding(context);
    }

    internal static void RenderList(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string tagName = GetListTagName(node);
        context.Writer.Write("<" + tagName);
        if (tagName == "ol")
        {
            string start = node.GetAttributeValue("start");
            if (start.Length > 0 && start != "1")
            {
                context.Writer.Write(" start=\"");
                context.Writer.WriteEscapedAttribute(start);
                context.Writer.Write("\"");
            }
        }

        WriteAttachedAttributes(context, node);

        context.Writer.Write(">");
        WriteLineEnding(context);
        RenderChildren(context, node);
        context.Writer.Write("</" + tagName + ">");
        WriteLineEnding(context);
    }

    internal static void RenderListItem(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<li");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        if (ShouldWriteLineAfterListItemOpening(node))
        {
            WriteLineEnding(context);
        }

        RenderChildren(context, node);
        context.Writer.Write("</li>");
        WriteLineEnding(context);
    }

    internal static void RenderTaskListItem(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<li");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteTaskListCheckbox(context, node);
        context.Writer.Write(" ");
        RenderChildren(context, node);
        context.Writer.Write("</li>");
        WriteLineEnding(context);
    }

    internal static bool IsParagraphInTightList(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownNode? item = node.Parent;
        if (item == null || (item.Kind != MarkdownNodeKind.ListItem && item.Kind != MarkdownNodeKind.TaskListItem))
        {
            return false;
        }

        MarkdownNode? list = item.Parent;
        return list != null
            && list.Kind == MarkdownNodeKind.List
            && list.GetAttributeValue("tight") == "true";
    }

    internal static bool ShouldWriteLineAfterListItemOpening(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.GetChildCount() == 0)
        {
            return false;
        }

        MarkdownNode first = node.GetChild(0);
        return first.Kind != MarkdownNodeKind.Paragraph || !IsParagraphInTightList(first);
    }

    internal static void WriteTaskListCheckbox(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (IsTaskListItemChecked(node))
        {
            context.Writer.Write("<input checked=\"\" disabled=\"\" type=\"checkbox\">");
        }
        else
        {
            context.Writer.Write("<input disabled=\"\" type=\"checkbox\">");
        }
    }

    internal static void RenderThematicBreak(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<hr");
        WriteAttachedAttributes(context, node);
        if (context.Options.Xhtml)
        {
            context.Writer.Write(" />");
        }
        else
        {
            context.Writer.Write(">");
        }

        WriteLineEnding(context);
    }

    internal static void RenderCodeBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<pre");
        WriteAttachedAttributes(context, node);
        context.Writer.Write("><code");
        if (node.Kind == MarkdownNodeKind.FencedCodeBlock)
        {
            string language = GetCodeInfoLanguage(node.GetAttributeValue("info"));
            if (language.Length > 0)
            {
                context.Writer.Write(" class=\"language-");
                context.Writer.WriteEscapedAttribute(language);
                context.Writer.Write("\"");
            }
        }

        context.Writer.Write(">");
        context.Writer.WriteEscapedText(node.Literal);
        if (node.Literal.Length > 0)
        {
            WriteLineEnding(context);
        }

        context.Writer.Write("</code></pre>");
        WriteLineEnding(context);
    }

    internal static void RenderMathBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<div class=\"math");
        WriteAttachedClassSuffix(context, node);
        context.Writer.Write("\"");
        WriteAttachedAttributesExcluding(context, node, "class");
        context.Writer.Write(">");
        WriteLineEnding(context);
        context.Writer.WriteEscapedText(node.Literal);
        WriteLineEnding(context);
        context.Writer.Write("</div>");
        WriteLineEnding(context);
    }

    internal static void RenderDiagramBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<pre");
        WriteAttachedAttributes(context, node);
        context.Writer.Write("><code");
        string diagramType = node.GetAttributeValue("diagramType");
        if (diagramType.Length > 0)
        {
            context.Writer.Write(" class=\"language-");
            context.Writer.WriteEscapedAttribute(diagramType);
            context.Writer.Write("\" data-diagram=\"");
            context.Writer.WriteEscapedAttribute(diagramType);
            context.Writer.Write("\"");
        }

        context.Writer.Write(">");
        context.Writer.WriteEscapedText(node.Literal);
        WriteLineEnding(context);
        context.Writer.Write("</code></pre>");
        WriteLineEnding(context);
    }

    internal static void RenderAdmonitionBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string type = node.GetAttributeValue("type");
        if (type.Length == 0)
        {
            type = "note";
        }

        context.Writer.Write("<div class=\"admonition ");
        context.Writer.WriteEscapedAttribute(type);
        WriteAttachedClassSuffix(context, node);
        context.Writer.Write("\"");
        WriteAttachedAttributesExcluding(context, node, "class");
        context.Writer.Write(">");
        WriteLineEnding(context);
        string title = node.GetAttributeValue("title");
        if (title.Length > 0)
        {
            context.Writer.Write("<p class=\"admonition-title\">");
            context.Writer.WriteEscapedText(title);
            context.Writer.Write("</p>");
            WriteLineEnding(context);
        }

        RenderChildren(context, node);
        context.Writer.Write("</div>");
        WriteLineEnding(context);
    }

    internal static void RenderCustomContainerBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string name = node.GetAttributeValue("name");
        context.Writer.Write("<div class=\"custom-container");
        if (name.Length > 0)
        {
            context.Writer.Write(" ");
            context.Writer.WriteEscapedAttribute(name);
        }

        WriteAttachedClassSuffix(context, node);
        context.Writer.Write("\"");
        WriteAttachedAttributesExcluding(context, node, "class");
        context.Writer.Write(">");
        WriteLineEnding(context);
        string title = node.GetAttributeValue("title");
        if (title.Length > 0)
        {
            context.Writer.Write("<p class=\"custom-container-title\">");
            context.Writer.WriteEscapedText(title);
            context.Writer.Write("</p>");
            WriteLineEnding(context);
        }

        RenderChildren(context, node);
        context.Writer.Write("</div>");
        WriteLineEnding(context);
    }

    internal static void RenderCustomFencedBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<pre");
        WriteAttachedAttributes(context, node);
        context.Writer.Write("><code");
        string infoKey = node.GetAttributeValue("infoKey");
        if (infoKey.Length > 0)
        {
            context.Writer.Write(" class=\"language-");
            context.Writer.WriteEscapedAttribute(infoKey);
            context.Writer.Write("\" data-custom-fenced=\"");
            context.Writer.WriteEscapedAttribute(infoKey);
            context.Writer.Write("\"");
        }

        context.Writer.Write(">");
        context.Writer.WriteEscapedText(node.Literal);
        WriteLineEnding(context);
        context.Writer.Write("</code></pre>");
        WriteLineEnding(context);
    }

    internal static void RenderDetailsBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<details");
        if (node.GetAttributeValue("open") == "true")
        {
            context.Writer.Write(" open");
        }
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteLineEnding(context);
        RenderChildren(context, node);
        context.Writer.Write("</details>");
        WriteLineEnding(context);
    }

    internal static void RenderDetailsSummary(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<summary");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</summary>");
        WriteLineEnding(context);
    }

    internal static void RenderExtensionBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<div data-markdown-extension=\"");
        context.Writer.WriteEscapedAttribute(GetExtensionName(node));
        context.Writer.Write("\"");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteLineEnding(context);
        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else if (node.Literal.Length > 0)
        {
            context.Writer.WriteEscapedText(node.Literal);
            WriteLineEnding(context);
        }

        context.Writer.Write("</div>");
        WriteLineEnding(context);
    }

    internal static void RenderExtensionInline(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<span data-markdown-extension=\"");
        context.Writer.WriteEscapedAttribute(GetExtensionName(node));
        context.Writer.Write("\"");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else
        {
            context.Writer.WriteEscapedText(node.Literal);
        }

        context.Writer.Write("</span>");
    }

    internal static string GetExtensionName(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        string name = node.GetAttributeValue("name");
        if (name.Length == 0 && node.Data.Extension != null)
        {
            name = node.Data.Extension.Name;
        }

        return name;
    }

    internal static void RenderHtmlBlock(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteHtmlNodeLiteral(context, node, node.Literal);
        WriteLineEnding(context);
    }

    internal static void RenderText(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.Data.AttachedAttributes.Names.Count > 0)
        {
            context.Writer.Write("<span");
            WriteAttachedAttributes(context, node);
            context.Writer.Write(">");
            context.Writer.WriteEscapedText(node.Literal);
            context.Writer.Write("</span>");
        }
        else
        {
            context.Writer.WriteEscapedText(node.Literal);
        }
    }

    internal static void RenderSoftLineBreak(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (context.Options.HardWrap || context.Options.SoftBreakMode == MarkdownSoftBreakMode.HardBreak)
        {
            WriteBreakTag(context);
            WriteLineEnding(context);
        }
        else if (context.Options.SoftBreakMode == MarkdownSoftBreakMode.Space)
        {
            context.Writer.Write(" ");
        }
        else if (CjkSoftBreaks.ShouldCollapse(context.Pipeline, context.Options, node))
        {
            return;
        }
        else
        {
            WriteLineEnding(context);
        }
    }

    internal static void RenderHardLineBreak(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteBreakTag(context);
        WriteLineEnding(context);
    }

    internal static void RenderCodeSpan(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<code");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        context.Writer.WriteEscapedText(node.Literal);
        context.Writer.Write("</code>");
    }

    internal static void RenderMathSpan(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<span class=\"math");
        WriteAttachedClassSuffix(context, node);
        context.Writer.Write("\"");
        WriteAttachedAttributesExcluding(context, node, "class");
        context.Writer.Write(">");
        context.Writer.WriteEscapedText(node.Literal);
        context.Writer.Write("</span>");
    }

    internal static void RenderEmoji(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        bool wrapped = node.Data.AttachedAttributes.Names.Count > 0;
        if (wrapped)
        {
            context.Writer.Write("<span");
            WriteAttachedAttributes(context, node);
            context.Writer.Write(">");
        }

        string replacement = node.GetAttributeValue("replacement");
        if (replacement.Length > 0)
        {
            context.Writer.WriteEscapedText(replacement);
        }
        else
        {
            context.Writer.WriteEscapedText(node.Literal);
        }

        if (wrapped)
        {
            context.Writer.Write("</span>");
        }
    }

    internal static void RenderCustomInline(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<span class=\"custom-inline");
        string name = node.GetAttributeValue("name");
        if (name.Length > 0)
        {
            context.Writer.Write(" ");
            context.Writer.WriteEscapedAttribute(name);
        }

        WriteAttachedClassSuffix(context, node);
        context.Writer.Write("\"");
        WriteAttachedAttributesExcluding(context, node, "class");
        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</span>");
    }

    internal static void RenderEmphasis(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<em");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</em>");
    }

    internal static void RenderStrongEmphasis(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<strong");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</strong>");
    }

    internal static void RenderStrikethrough(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<del");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</del>");
    }

    internal static void RenderLink(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        MarkdownUriEvaluation destinationEvaluation = MarkdownSafetyPolicyEvaluator.EvaluateUri(
            node,
            node.GetAttributeValue("destination"),
            false,
            context.Options);
        string title = node.GetAttributeValue("title");
        context.Writer.Write("<a");
        if (destinationEvaluation.Allowed)
        {
            context.Writer.Write(" href=\"");
            context.Writer.WriteEscapedAttribute(HtmlEscaper.EscapeUrl(destinationEvaluation.Destination));
            context.Writer.Write("\"");
        }

        if (title.Length > 0)
        {
            context.Writer.Write(" title=\"");
            context.Writer.WriteEscapedAttribute(title);
            context.Writer.Write("\"");
        }

        WriteAttachedAttributes(context, node);

        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</a>");
    }

    internal static void RenderImage(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        MarkdownUriEvaluation destinationEvaluation = MarkdownSafetyPolicyEvaluator.EvaluateUri(
            node,
            node.GetAttributeValue("destination"),
            true,
            context.Options);
        string title = node.GetAttributeValue("title");
        context.Writer.Write("<img");
        if (destinationEvaluation.Allowed)
        {
            context.Writer.Write(" src=\"");
            context.Writer.WriteEscapedAttribute(HtmlEscaper.EscapeUrl(destinationEvaluation.Destination));
            context.Writer.Write("\"");
        }

        context.Writer.Write(" alt=\"");
        context.Writer.WriteEscapedAttribute(GetPlainText(node));
        context.Writer.Write("\"");
        if (title.Length > 0)
        {
            context.Writer.Write(" title=\"");
            context.Writer.WriteEscapedAttribute(title);
            context.Writer.Write("\"");
        }

        WriteAttachedAttributes(context, node);

        if (context.Options.Xhtml)
        {
            context.Writer.Write(" />");
        }
        else
        {
            context.Writer.Write(">");
        }
    }

    internal static void RenderHtmlInline(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteHtmlNodeLiteral(context, node, node.Literal);
    }

    internal static bool TryRenderWithRules(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        int index = 0;
        while (index < context.RenderRules.GetCount())
        {
            MarkdownRuleRegistration registration = context.RenderRules.GetAt(index);
            IHtmlRenderRule? rule = registration.Rule as IHtmlRenderRule;
            if (rule != null && rule.TryRender(context, node))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void RenderFootnoteReference(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        int number = GetFootnoteReferenceNumber(context, node);
        string idLabel = GetFootnoteIdLabel(node.GetAttributeValue("normalizedLabel"));
        context.Writer.Write("<sup id=\"fnref-");
        context.Writer.WriteEscapedAttribute(idLabel);
        context.Writer.Write("\"");
        WriteAttachedAttributes(context, node);
        context.Writer.Write("><a href=\"#fn-");
        context.Writer.WriteEscapedAttribute(idLabel);
        context.Writer.Write("\" class=\"footnote-ref\">");
        context.Writer.Write(number.ToString());
        context.Writer.Write("</a></sup>");
    }

    internal static void RenderAbbreviation(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<abbr");
        string title = node.GetAttributeValue("title");
        if (title.Length > 0)
        {
            context.Writer.Write(" title=\"");
            context.Writer.WriteEscapedAttribute(title);
            context.Writer.Write("\"");
        }

        WriteAttachedAttributes(context, node);

        context.Writer.Write(">");
        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else
        {
            context.Writer.WriteEscapedText(node.Literal);
        }

        context.Writer.Write("</abbr>");
    }

    internal static void RenderWrappedInline(HtmlRenderContext context, MarkdownNode node, string tagName)
    {
        ValidateContextAndNode(context, node);
        if (tagName == null)
        {
            throw new ArgumentNullException("tagName");
        }

        context.Writer.Write("<");
        context.Writer.Write(tagName);
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else
        {
            context.Writer.WriteEscapedText(node.Literal);
        }

        context.Writer.Write("</");
        context.Writer.Write(tagName);
        context.Writer.Write(">");
    }

    internal static void RenderFootnoteSection(HtmlRenderContext context, MarkdownNode root)
    {
        ValidateContextAndNode(context, root);

        if (!HasRenderableFootnotes(context, root))
        {
            return;
        }

        context.Writer.Write("<section class=\"footnotes\">");
        WriteLineEnding(context);
        context.Writer.Write("<ol>");
        WriteLineEnding(context);

        int index = 0;
        while (index < context.FootnoteOrder.Count)
        {
            string normalizedLabel = context.FootnoteOrder[index];
            MarkdownNode? definition = FindFootnoteDefinition(root, normalizedLabel);
            if (definition != null)
            {
                RenderFootnoteListItem(context, definition, normalizedLabel);
            }

            index = index + 1;
        }

        context.Writer.Write("</ol>");
        WriteLineEnding(context);
        context.Writer.Write("</section>");
        WriteLineEnding(context);
    }

    internal static void RenderFootnoteListItem(HtmlRenderContext context, MarkdownNode definition, string normalizedLabel)
    {
        ValidateContextAndNode(context, definition);
        if (normalizedLabel == null)
        {
            throw new ArgumentNullException("normalizedLabel");
        }

        string idLabel = GetFootnoteIdLabel(normalizedLabel);
        context.Writer.Write("<li id=\"fn-");
        context.Writer.WriteEscapedAttribute(idLabel);
        context.Writer.Write("\">");

        if (definition.GetChildCount() == 1 && definition.GetChild(0).Kind == MarkdownNodeKind.Paragraph)
        {
            context.Writer.Write("<p>");
            RenderChildren(context, definition.GetChild(0));
            context.Writer.Write(" ");
            WriteFootnoteBackreference(context, idLabel);
            context.Writer.Write("</p>");
            WriteLineEnding(context);
        }
        else
        {
            RenderChildren(context, definition);
            WriteFootnoteBackreference(context, idLabel);
        }

        context.Writer.Write("</li>");
        WriteLineEnding(context);
    }

    internal static void WriteFootnoteBackreference(HtmlRenderContext context, string idLabel)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (idLabel == null)
        {
            throw new ArgumentNullException("idLabel");
        }

        context.Writer.Write("<a href=\"#fnref-");
        context.Writer.WriteEscapedAttribute(idLabel);
        context.Writer.Write("\" class=\"footnote-backref\">&#8617;</a>");
    }

    internal static void RenderDefinitionList(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<dl");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteLineEnding(context);
        RenderChildren(context, node);
        context.Writer.Write("</dl>");
        WriteLineEnding(context);
    }

    internal static void RenderDefinitionTerm(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<dt");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else
        {
            context.Writer.WriteEscapedText(node.Literal);
        }

        context.Writer.Write("</dt>");
        WriteLineEnding(context);
    }

    internal static void RenderDefinitionDescription(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<dd");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        RenderChildren(context, node);
        context.Writer.Write("</dd>");
        WriteLineEnding(context);
    }

    internal static void RenderTable(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<table");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteLineEnding(context);
        RenderChildren(context, node);
        context.Writer.Write("</table>");
        WriteLineEnding(context);
    }

    internal static void RenderTableHead(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<thead");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteLineEnding(context);
        RenderChildren(context, node);
        context.Writer.Write("</thead>");
        WriteLineEnding(context);
    }

    internal static void RenderTableBody(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<tbody");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteLineEnding(context);
        RenderChildren(context, node);
        context.Writer.Write("</tbody>");
        WriteLineEnding(context);
    }

    internal static void RenderTableRow(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write("<tr");
        WriteAttachedAttributes(context, node);
        context.Writer.Write(">");
        WriteLineEnding(context);
        RenderChildren(context, node);
        context.Writer.Write("</tr>");
        WriteLineEnding(context);
    }

    internal static void RenderTableCell(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string tagName = GetTableCellTagName(node);
        context.Writer.Write("<" + tagName);
        string alignment = node.GetAttributeValue("align");
        if (alignment.Length > 0)
        {
            context.Writer.Write(" align=\"");
            context.Writer.WriteEscapedAttribute(alignment);
            context.Writer.Write("\"");
        }

        WriteAttachedAttributes(context, node);

        context.Writer.Write(">");
        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else
        {
            context.Writer.WriteEscapedText(node.Literal);
        }

        context.Writer.Write("</" + tagName + ">");
        WriteLineEnding(context);
    }

    internal static string GetTableCellTagName(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownNode? parent = node.Parent;
        if (parent != null && parent.Kind == MarkdownNodeKind.TableRow)
        {
            MarkdownNode? grandparent = parent.Parent;
            if (grandparent != null && grandparent.Kind == MarkdownNodeKind.TableHead)
            {
                return "th";
            }
        }

        return "td";
    }

    internal static void CollectFootnoteReferences(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.Kind == MarkdownNodeKind.FootnoteDefinition)
        {
            return;
        }

        if (node.Kind == MarkdownNodeKind.FootnoteReference)
        {
            AddFootnoteReferenceLabel(context, node.GetAttributeValue("normalizedLabel"));
            return;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            CollectFootnoteReferences(context, node.GetChild(index));
            index = index + 1;
        }
    }

    internal static void AddFootnoteReferenceLabel(HtmlRenderContext context, string normalizedLabel)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (normalizedLabel == null)
        {
            throw new ArgumentNullException("normalizedLabel");
        }

        if (normalizedLabel.Length == 0 || FindFootnoteOrderIndex(context, normalizedLabel) >= 0)
        {
            return;
        }

        context.FootnoteOrder.Add(normalizedLabel);
    }

    internal static int FindFootnoteOrderIndex(HtmlRenderContext context, string normalizedLabel)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (normalizedLabel == null)
        {
            throw new ArgumentNullException("normalizedLabel");
        }

        int index = 0;
        while (index < context.FootnoteOrder.Count)
        {
            if (context.FootnoteOrder[index] == normalizedLabel)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int GetFootnoteReferenceNumber(HtmlRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string normalizedLabel = node.GetAttributeValue("normalizedLabel");
        AddFootnoteReferenceLabel(context, normalizedLabel);
        int index = FindFootnoteOrderIndex(context, normalizedLabel);
        if (index < 0)
        {
            return 0;
        }

        return index + 1;
    }

    internal static bool HasRenderableFootnotes(HtmlRenderContext context, MarkdownNode root)
    {
        ValidateContextAndNode(context, root);

        int index = 0;
        while (index < context.FootnoteOrder.Count)
        {
            if (FindFootnoteDefinition(root, context.FootnoteOrder[index]) != null)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static MarkdownNode? FindFootnoteDefinition(MarkdownNode node, string normalizedLabel)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (normalizedLabel == null)
        {
            throw new ArgumentNullException("normalizedLabel");
        }

        if (node.Kind == MarkdownNodeKind.FootnoteDefinition
            && node.GetAttributeValue("normalizedLabel") == normalizedLabel)
        {
            return node;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode? found = FindFootnoteDefinition(node.GetChild(index), normalizedLabel);
            if (found != null)
            {
                return found;
            }

            index = index + 1;
        }

        return null;
    }

    internal static string GetFootnoteIdLabel(string normalizedLabel)
    {
        if (normalizedLabel == null)
        {
            throw new ArgumentNullException("normalizedLabel");
        }

        if (normalizedLabel.Length == 0)
        {
            return "footnote";
        }

        StringWriter writer = new StringWriter();
        int index = 0;
        bool lastWasDash = false;
        while (index < normalizedLabel.Length)
        {
            char character = normalizedLabel[index];
            if (IsFootnoteIdCharacter(character))
            {
                writer.Write(char.ToLowerInvariant(character));
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                writer.Write("-");
                lastWasDash = true;
            }

            index = index + 1;
        }

        string result = writer.ToString().Trim('-');
        if (result.Length == 0)
        {
            return "footnote";
        }

        return result;
    }

    internal static bool IsFootnoteIdCharacter(char character)
    {
        return char.IsLetterOrDigit(character)
            || character == '-'
            || character == '_'
            || character == ':'
            || character == '.';
    }

    internal static void WriteHtmlLiteral(HtmlRenderContext context, string literal)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.HtmlInline);
        WriteHtmlNodeLiteral(context, node, literal);
    }

    internal static void WriteHtmlNodeLiteral(
        HtmlRenderContext context,
        MarkdownNode node,
        string literal)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        MarkdownRawHtmlEvaluation evaluation = MarkdownSafetyPolicyEvaluator.EvaluateRawHtml(
            node,
            literal,
            context.Options);
        WriteRawHtmlWithDecision(context.Writer, literal, evaluation.Decision);
    }

    internal static void WriteRawHtmlWithDecision(
        HtmlWriter writer,
        string literal,
        MarkdownRawHtmlDecision decision)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        MarkdownRawHtmlDecision normalized = MarkdownSafetyPolicyEvaluator.NormalizeRawHtmlDecision(decision);
        if (normalized == MarkdownRawHtmlDecision.Render)
        {
            writer.Write(literal);
        }
        else if (normalized == MarkdownRawHtmlDecision.Filter)
        {
            WriteGfmFilteredRawHtmlToWriter(writer, literal);
        }
        else if (normalized == MarkdownRawHtmlDecision.Escape)
        {
            writer.WriteEscapedText(literal);
        }
    }

    internal static bool ShouldRenderRawHtml(HtmlRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return context.Pipeline.AllowsRawHtml() || context.Pipeline.AllowsUnsafeHtml();
    }

    internal static bool ShouldFilterGfmDisallowedRawHtml(HtmlRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return context.Options.Features.GfmDisallowedRawHtml && !context.Pipeline.AllowsUnsafeHtml();
    }

    internal static void WriteGfmFilteredRawHtml(HtmlRenderContext context, string literal)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        WriteGfmFilteredRawHtmlToWriter(context.Writer, literal);
    }

    internal static void WriteGfmFilteredRawHtmlToWriter(HtmlWriter writer, string literal)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        int chunkStart = 0;
        int index = 0;
        while (index < literal.Length)
        {
            if (IsGfmDisallowedRawHtmlTag(literal, index))
            {
                if (index > chunkStart)
                {
                    writer.Write(literal.Substring(chunkStart, index - chunkStart));
                }

                writer.Write("&lt;");
                index = index + 1;
                chunkStart = index;
            }
            else
            {
                index = index + 1;
            }
        }

        if (chunkStart < literal.Length)
        {
            writer.Write(literal.Substring(chunkStart));
        }
    }

    internal static bool IsGfmDisallowedRawHtmlTag(string literal, int startIndex)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        if (startIndex < 0 || startIndex > literal.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (startIndex >= literal.Length || literal[startIndex] != '<')
        {
            return false;
        }

        int tagStart = startIndex + 1;
        if (tagStart < literal.Length && literal[tagStart] == '/')
        {
            tagStart = tagStart + 1;
        }

        return IsGfmDisallowedRawHtmlTagName(literal, tagStart, "title")
            || IsGfmDisallowedRawHtmlTagName(literal, tagStart, "textarea")
            || IsGfmDisallowedRawHtmlTagName(literal, tagStart, "style")
            || IsGfmDisallowedRawHtmlTagName(literal, tagStart, "xmp")
            || IsGfmDisallowedRawHtmlTagName(literal, tagStart, "iframe")
            || IsGfmDisallowedRawHtmlTagName(literal, tagStart, "noembed")
            || IsGfmDisallowedRawHtmlTagName(literal, tagStart, "noframes")
            || IsGfmDisallowedRawHtmlTagName(literal, tagStart, "script")
            || IsGfmDisallowedRawHtmlTagName(literal, tagStart, "plaintext");
    }

    internal static bool IsGfmDisallowedRawHtmlTagName(string literal, int startIndex, string tagName)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        if (tagName == null)
        {
            throw new ArgumentNullException("tagName");
        }

        if (startIndex < 0 || startIndex > literal.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (!StartsWithIgnoreCaseAt(literal, startIndex, tagName))
        {
            return false;
        }

        int boundaryIndex = startIndex + tagName.Length;
        if (boundaryIndex >= literal.Length)
        {
            return true;
        }

        return IsGfmRawHtmlTagNameBoundary(literal[boundaryIndex]);
    }

    internal static bool IsGfmRawHtmlTagNameBoundary(char character)
    {
        return MarkdownChar.IsWhitespace(character)
            || character == '>'
            || character == '/';
    }

    internal static bool StartsWithIgnoreCaseAt(string text, int startIndex, string value)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (startIndex < 0 || startIndex > text.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (value.Length > text.Length - startIndex)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char left = char.ToLowerInvariant(text[startIndex + index]);
            char right = char.ToLowerInvariant(value[index]);
            if (left != right)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void WriteBreakTag(HtmlRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (context.Options.Xhtml)
        {
            context.Writer.Write("<br />");
        }
        else
        {
            context.Writer.Write("<br>");
        }
    }

    internal static int GetHeadingLevel(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        int level = ParsePositiveInteger(node.GetAttributeValue("level"));
        if (level < 1)
        {
            return 1;
        }

        if (level > 6)
        {
            return 6;
        }

        return level;
    }

    internal static string GetListTagName(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.GetAttributeValue("type") == "ordered")
        {
            return "ol";
        }

        return "ul";
    }

    internal static bool IsTaskListItemChecked(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return node.GetAttributeValue("checked") == "true";
    }

    internal static string GetCodeInfoLanguage(string info)
    {
        if (info == null)
        {
            throw new ArgumentNullException("info");
        }

        int start = 0;
        while (start < info.Length && char.IsWhiteSpace(info[start]))
        {
            start = start + 1;
        }

        int end = start;
        while (end < info.Length && !char.IsWhiteSpace(info[end]))
        {
            end = end + 1;
        }

        if (end <= start)
        {
            return "";
        }

        return info.Substring(start, end - start);
    }

    internal static bool ShouldRenderLinkDestination(HtmlRenderContext context, string destination)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Link);
        return MarkdownSafetyPolicyEvaluator.EvaluateUri(
            node,
            destination,
            false,
            context.Options).Allowed;
    }

    internal static bool ShouldRenderImageDestination(HtmlRenderContext context, string destination)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Image);
        return MarkdownSafetyPolicyEvaluator.EvaluateUri(
            node,
            destination,
            true,
            context.Options).Allowed;
    }

    /// <summary>
    /// Collects the literal text beneath a node without Markdown or HTML delimiters.
    /// </summary>
    /// <remarks>
    /// Image hooks can use the result as alternative text. Nested child literals are concatenated in document order.
    /// </remarks>
    /// <param name="node">The node whose descendant text is collected.</param>
    /// <returns>The concatenated plain text.</returns>
    internal static string GetPlainText(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        StringWriter writer = new StringWriter();
        WritePlainText(writer, node);
        return writer.ToString();
    }

    internal static void WritePlainText(TextWriter writer, MarkdownNode node)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        switch (node.Kind)
        {
            case MarkdownNodeKind.Text:
                writer.Write(node.Literal);
                break;
            case MarkdownNodeKind.CodeSpan:
                writer.Write(node.Literal);
                break;
            case MarkdownNodeKind.SoftLineBreak:
                writer.Write("\n");
                break;
            case MarkdownNodeKind.HardLineBreak:
                writer.Write("\n");
                break;
            case MarkdownNodeKind.HtmlInline:
                writer.Write(node.Literal);
                break;
            default:
                int index = 0;
                while (index < node.GetChildCount())
                {
                    WritePlainText(writer, node.GetChild(index));
                    index = index + 1;
                }

                break;
        }
    }

    internal static int ParsePositiveInteger(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return 0;
            }

            int digit = character - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                return 0;
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        return value;
    }

    /// <summary>
    /// Writes the line-ending sequence configured on the active pipeline.
    /// </summary>
    /// <param name="context">The active HTML rendering context.</param>
    internal static void WriteLineEnding(HtmlRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        context.Writer.Write(context.Options.LineEnding);
    }

    internal static void ValidateContextAndNode(HtmlRenderContext context, MarkdownNode node)
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
