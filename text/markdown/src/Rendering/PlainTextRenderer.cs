using System;
using System.IO;

namespace markdown;

/// <summary>
/// Renders the readable textual content of a Markdown document.
/// </summary>
internal class PlainTextRenderer
{
    /// <summary>
    /// Renders readable text using the CommonMark preset while omitting Markdown delimiters.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string Render(MarkdownDocument document)
    {
        return Render(document, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>
    /// Renders readable text using the supplied pipeline's line-break behavior.
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
    /// Writes readable text without closing or disposing the destination writer.
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

        PlainTextWriter plainTextWriter = PlainTextWriter.Create(writer, pipeline.GetOptions().MaximumOutputSize);
        PlainTextRenderContext context = new PlainTextRenderContext(plainTextWriter, pipeline);
        RenderNode(context, document.Root);
    }

    internal static void RenderNode(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        switch (node.Kind)
        {
            case MarkdownNodeKind.Document:
                RenderDocument(context, node);
                break;
            case MarkdownNodeKind.Paragraph:
                RenderParagraph(context, node);
                break;
            case MarkdownNodeKind.Heading:
                RenderHeading(context, node);
                break;
            case MarkdownNodeKind.BlockQuote:
                RenderChildren(context, node);
                break;
            case MarkdownNodeKind.List:
                RenderChildren(context, node);
                break;
            case MarkdownNodeKind.ListItem:
                RenderChildren(context, node);
                break;
            case MarkdownNodeKind.DefinitionList:
                RenderChildren(context, node);
                break;
            case MarkdownNodeKind.DefinitionTerm:
                RenderInlineContainer(context, node);
                WriteLineEnding(context);
                break;
            case MarkdownNodeKind.DefinitionDescription:
                RenderChildren(context, node);
                break;
            case MarkdownNodeKind.IndentedCodeBlock:
                RenderLiteralBlock(context, node);
                break;
            case MarkdownNodeKind.FencedCodeBlock:
                RenderLiteralBlock(context, node);
                break;
            case MarkdownNodeKind.MathBlock:
                RenderLiteralBlock(context, node);
                break;
            case MarkdownNodeKind.DiagramBlock:
                RenderLiteralBlock(context, node);
                break;
            case MarkdownNodeKind.CustomFencedBlock:
                RenderLiteralBlock(context, node);
                break;
            case MarkdownNodeKind.DetailsBlock:
                RenderChildren(context, node);
                break;
            case MarkdownNodeKind.DetailsSummary:
                RenderInlineContainer(context, node);
                WriteLineEnding(context);
                break;
            case MarkdownNodeKind.HtmlBlock:
                RenderLiteralBlock(context, node);
                break;
            case MarkdownNodeKind.ThematicBreak:
                break;
            case MarkdownNodeKind.LinkReferenceDefinition:
                break;
            case MarkdownNodeKind.AbbreviationDefinition:
                break;
            case MarkdownNodeKind.Text:
                RenderLiteralInline(context, node);
                break;
            case MarkdownNodeKind.CodeSpan:
                RenderLiteralInline(context, node);
                break;
            case MarkdownNodeKind.MathSpan:
                RenderLiteralInline(context, node);
                break;
            case MarkdownNodeKind.Emoji:
                RenderEmoji(context, node);
                break;
            case MarkdownNodeKind.HtmlInline:
                RenderLiteralInline(context, node);
                break;
            case MarkdownNodeKind.SoftLineBreak:
                RenderSoftLineBreak(context, node);
                break;
            case MarkdownNodeKind.HardLineBreak:
                RenderLineBreak(context, node);
                break;
            default:
                RenderChildren(context, node);
                break;
        }
    }

    internal static void RenderChildren(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        int index = 0;
        while (index < node.GetChildCount())
        {
            RenderNode(context, node.GetChild(index));
            index = index + 1;
        }
    }

    internal static void RenderDocument(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        bool wrote = false;
        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if (ShouldRenderBlock(child))
            {
                if (wrote)
                {
                    WriteLineEnding(context);
                }

                RenderNode(context, child);
                wrote = true;
            }

            index = index + 1;
        }
    }

    internal static void RenderParagraph(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        RenderInlineContainer(context, node);
        WriteLineEnding(context);
    }

    internal static void RenderHeading(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        RenderInlineContainer(context, node);
        WriteLineEnding(context);
    }

    internal static void RenderInlineContainer(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.GetChildCount() > 0)
        {
            RenderChildren(context, node);
        }
        else if (node.Literal.Length > 0)
        {
            context.Writer.Write(node.Literal);
        }
    }

    internal static void RenderLiteralBlock(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteLiteralWithNormalizedLineEndings(context, node.Literal);
        if (node.Literal.Length == 0 || !MarkdownRenderer.EndsWithLineEnding(node.Literal))
        {
            WriteLineEnding(context);
        }
    }

    internal static void RenderLiteralInline(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        context.Writer.Write(node.Literal);
    }

    internal static void RenderEmoji(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        string replacement = node.GetAttributeValue("replacement");
        if (replacement.Length > 0)
        {
            context.Writer.Write(replacement);
        }
        else
        {
            context.Writer.Write(node.Literal);
        }
    }

    internal static void RenderLineBreak(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteLineEnding(context);
    }

    internal static void RenderSoftLineBreak(PlainTextRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (CjkSoftBreaks.ShouldCollapse(context.Pipeline, context.Options, node))
        {
            return;
        }

        WriteLineEnding(context);
    }

    internal static bool ShouldRenderBlock(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return node.Kind != MarkdownNodeKind.LinkReferenceDefinition
            && node.Kind != MarkdownNodeKind.AbbreviationDefinition
            && node.Kind != MarkdownNodeKind.ThematicBreak;
    }

    internal static void WriteLiteralWithNormalizedLineEndings(PlainTextRenderContext context, string literal)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        string normalized = MarkdownRenderer.NormalizeLineEndings(literal);
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

    internal static void WriteLineEnding(PlainTextRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        context.Writer.WriteLineEnding(context.Options.LineEnding);
    }

    internal static void ValidateContextAndNode(PlainTextRenderContext context, MarkdownNode node)
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
