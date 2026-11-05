using System;
using System.IO;

namespace markdown;

/// <summary>
/// Renders a Markdown syntax tree as an XML-like diagnostic representation.
/// </summary>
internal class XmlAstRenderer
{
    /// <summary>
    /// Renders an XML-like AST description using the CommonMark preset.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string Render(MarkdownDocument document)
    {
        return Render(document, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>
    /// Renders an XML-like AST description using the supplied line-ending settings.
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
    /// Writes an XML-like AST description without disposing the destination writer.
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

        XmlAstRenderContext context = new XmlAstRenderContext(writer, pipeline);
        RenderNode(context, document.Root);
    }

    internal static void RenderNode(XmlAstRenderContext context, MarkdownNode node)
    {
        RenderNodeAtDepth(context, node, 0);
    }

    internal static void RenderNodeAtDepth(XmlAstRenderContext context, MarkdownNode node, int depth)
    {
        ValidateContextAndNode(context, node);
        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException("depth");
        }

        WriteIndent(context, depth);
        context.Write("<");
        context.Write(GetElementName(node));
        WriteNodeAttributes(context, node);
        if (node.GetChildCount() == 0)
        {
            context.Write(" />");
            WriteLineEnding(context);
            return;
        }

        context.Write(">");
        WriteLineEnding(context);

        int index = 0;
        while (index < node.GetChildCount())
        {
            RenderNodeAtDepth(context, node.GetChild(index), depth + 1);
            index = index + 1;
        }

        WriteIndent(context, depth);
        context.Write("</");
        context.Write(GetElementName(node));
        context.Write(">");
        WriteLineEnding(context);
    }

    internal static string GetElementName(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return node.Kind.ToString();
    }

    internal static void WriteNodeAttributes(XmlAstRenderContext context, MarkdownNode node)
    {
        ValidateContextAndNode(context, node);

        WriteAttribute(context, "span", DebugTreeRenderer.FormatSpan(node.Span));
        WriteAttribute(context, "length", DebugTreeRenderer.FormatInteger(node.Span.GetLength()));
        if (node.Literal.Length > 0)
        {
            WriteAttribute(context, "literal", node.Literal);
        }

        int index = 0;
        while (index < node.GetAttributeCount())
        {
            MarkdownAttribute attribute = node.Attributes[index];
            WriteAttribute(context, "attr-" + attribute.Name, attribute.Value);
            index = index + 1;
        }

        WriteAttribute(context, "children", DebugTreeRenderer.FormatInteger(node.GetChildCount()));
    }

    internal static void WriteAttribute(XmlAstRenderContext context, string name, string value)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        context.Write(" ");
        context.Write(name);
        context.Write("=\"");
        context.Write(EscapeXmlAttribute(value));
        context.Write("\"");
    }

    internal static void WriteIndent(XmlAstRenderContext context, int depth)
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

    internal static string EscapeXmlAttribute(string text)
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
            if (character == '&')
            {
                writer.Write("&amp;");
            }
            else if (character == '<')
            {
                writer.Write("&lt;");
            }
            else if (character == '>')
            {
                writer.Write("&gt;");
            }
            else if (character == '"')
            {
                writer.Write("&quot;");
            }
            else if (character == '\'')
            {
                writer.Write("&#39;");
            }
            else if (character == '\n')
            {
                writer.Write("&#10;");
            }
            else if (character == '\r')
            {
                writer.Write("&#13;");
            }
            else if (character == '\t')
            {
                writer.Write("&#9;");
            }
            else
            {
                writer.Write(character);
            }

            index = index + 1;
        }

        return writer.ToString();
    }

    internal static void WriteLineEnding(XmlAstRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        context.Write(context.Options.LineEnding);
    }

    internal static void ValidateContextAndNode(XmlAstRenderContext context, MarkdownNode node)
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
