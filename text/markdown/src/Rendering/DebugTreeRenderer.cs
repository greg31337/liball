using System;
using System.IO;

namespace markdown;

/// <summary>
/// Renders a stable, human-readable view of the Markdown syntax tree.
/// </summary>
internal class DebugTreeRenderer
{
    /// <summary>
    /// Renders a stable, indented tree description using the CommonMark preset.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string Render(MarkdownDocument document)
    {
        return Render(document, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>
    /// Renders a stable, indented tree description using the supplied line-ending settings.
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
    /// Writes a stable, indented tree description without disposing the destination writer.
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

        DebugTreeRenderContext context = new DebugTreeRenderContext(writer, pipeline);
        RenderNode(context, document.Root);
    }

    internal static void RenderNode(DebugTreeRenderContext context, MarkdownNode node)
    {
        RenderNodeAtDepth(context, node, 0);
    }

    internal static void RenderNodeAtDepth(DebugTreeRenderContext context, MarkdownNode node, int depth)
    {
        ValidateContextAndNode(context, node);
        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException("depth");
        }

        WriteIndent(context, depth);
        context.Write(node.Kind.ToString());
        context.Write(" span=");
        context.Write(FormatSpan(node.Span));
        context.Write(" length=");
        context.Write(FormatInteger(node.Span.GetLength()));
        WriteAttributes(context, node);
        WriteLiteral(context, node);
        context.Write(" children=");
        context.Write(FormatInteger(node.GetChildCount()));
        WriteLineEnding(context);

        int index = 0;
        while (index < node.GetChildCount())
        {
            RenderNodeAtDepth(context, node.GetChild(index), depth + 1);
            index = index + 1;
        }
    }

    internal static void WriteIndent(DebugTreeRenderContext context, int depth)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException("depth");
        }

        int index = 0;
        while (index < depth)
        {
            context.Write("  ");
            index = index + 1;
        }
    }

    internal static void WriteAttributes(DebugTreeRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.GetAttributeCount() == 0)
        {
            return;
        }

        context.Write(" attrs=[");
        int index = 0;
        while (index < node.GetAttributeCount())
        {
            MarkdownAttribute attribute = node.Attributes[index];
            if (index > 0)
            {
                context.Write(", ");
            }

            context.Write(attribute.Name);
            context.Write("=\"");
            context.Write(EscapeDebugText(attribute.Value));
            context.Write("\"");
            index = index + 1;
        }

        context.Write("]");
    }

    internal static void WriteLiteral(DebugTreeRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        if (node.Literal.Length == 0)
        {
            return;
        }

        context.Write(" literal=\"");
        context.Write(EscapeDebugText(node.Literal));
        context.Write("\"");
    }

    internal static string FormatSpan(MarkdownSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        return FormatPosition(span.Start) + "-" + FormatPosition(span.End);
    }

    internal static string FormatPosition(MarkdownSourcePosition position)
    {
        if (position == null)
        {
            throw new ArgumentNullException("position");
        }

        return FormatInteger(position.Line) + ":" + FormatInteger(position.Column);
    }

    internal static string FormatInteger(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string EscapeDebugText(string text)
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
            if (character == '\\')
            {
                writer.Write("\\\\");
            }
            else if (character == '"')
            {
                writer.Write("\\\"");
            }
            else if (character == '\n')
            {
                writer.Write("\\n");
            }
            else if (character == '\r')
            {
                writer.Write("\\r");
            }
            else if (character == '\t')
            {
                writer.Write("\\t");
            }
            else
            {
                writer.Write(character);
            }

            index = index + 1;
        }

        return writer.ToString();
    }

    internal static void WriteLineEnding(DebugTreeRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        context.Write(context.Options.LineEnding);
    }

    internal static void ValidateContextAndNode(DebugTreeRenderContext context, MarkdownNode node)
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
