using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Parses and rewrites native template source with stable block indentation and configured newline handling.
/// </summary>
public class TemplateFormatter : ITemplateFormatter
{
    /// <summary>
    /// Specifies the options for this formatter.
    /// </summary>
    internal TemplateFormatterOptions Options;
    internal TemplateWhitespaceNormalizer WhitespaceNormalizer;

    /// <summary>
    /// Initializes a new template formatter and validates any supplied values.
    /// </summary>
    public TemplateFormatter()
    {
        Options = new TemplateFormatterOptions();
        WhitespaceNormalizer = new TemplateWhitespaceNormalizer();
    }

    /// <summary>
    /// Configures the formatter options.
    /// </summary>
    /// <param name="options">The options that control formatting.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>options</c> is <c>null</c>.</exception>
    public void Configure(TemplateFormatterOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Options = options;
    }

    /// <summary>
    /// Creates a formatter that uses the supplied options.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The configured formatter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateFormatter Create(TemplateFormatterOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        TemplateFormatter formatter = new TemplateFormatter();
        formatter.Options = options;
        return formatter;
    }

    /// <summary>
    /// Formats supplied template or diagnostic text into a stable, human-readable form.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string Format(string templateText)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        TemplateParseResult parseResult = TemplateParser.ParseNative(templateText);
        return FormatDocument(parseResult.Document);
    }

    /// <summary>
    /// Formats this value into a stable, human-readable form.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string FormatDocument(TemplateDocumentSyntax document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        Options.Validate();
        StringBuilder builder = new StringBuilder();
        WriteNodes(builder, document.Children, 0);
        return WhitespaceNormalizer.Normalize(builder.ToString(), Options.WhitespaceMode);
    }

    internal void WriteNodes(StringBuilder builder, List<TemplateSyntaxNode> nodes, int depth)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            WriteNode(builder, nodes[index], depth);
            index = index + 1;
        }
    }

    internal void WriteNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Kind == TemplateSyntaxKind.Text && node.Text != null)
        {
            WriteText(builder, node.Text.Text, depth);
        }
        else if (node.Kind == TemplateSyntaxKind.Output && node.Output != null)
        {
            WriteOutput(builder, node.Output, depth);
        }
        else if (node.Kind == TemplateSyntaxKind.Comment && node.Comment != null)
        {
            WriteComment(builder, node.Comment, depth);
        }
        else if (node.Kind == TemplateSyntaxKind.Raw && node.Raw != null)
        {
            WriteRaw(builder, node.Raw, depth);
        }
        else if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
        {
            WriteStatementLine(builder, node.Statement, depth);
        }
        else if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            WriteBlock(builder, node.Block, depth);
        }
    }

    internal void WriteText(StringBuilder builder, string text, int depth)
    {
        string normalized = WhitespaceNormalizer.ConvertToLineFeeds(text);
        string line = "";
        int index = 0;
        while (index < normalized.Length)
        {
            char value = normalized[index];
            if (value == '\n')
            {
                WriteTextLine(builder, line, depth);
                line = "";
            }
            else
            {
                line = line + value.ToString();
            }

            index = index + 1;
        }

        WriteTextLine(builder, line, depth);
    }

    internal void WriteTextLine(StringBuilder builder, string line, int depth)
    {
        string trimmed = line.Trim();
        if (trimmed == "")
        {
            return;
        }

        WriteIndent(builder, depth);
        builder.Append(trimmed);
        builder.Append(Options.NewLine);
    }

    internal void WriteOutput(StringBuilder builder, TemplateOutputSyntax output, int depth)
    {
        WriteIndent(builder, depth);
        string sourceText = output.Span.GetText();
        WriteTagStart(builder, "{{", sourceText);
        builder.Append(' ');
        builder.Append(output.Expression.Text);
        if (output.Raw || !output.Escape)
        {
            builder.Append(" | safe");
        }

        builder.Append(' ');
        WriteTagEnd(builder, "}}", sourceText);
        builder.Append(Options.NewLine);
    }

    internal void WriteComment(StringBuilder builder, TemplateCommentSyntax comment, int depth)
    {
        WriteIndent(builder, depth);
        string sourceText = comment.Span.GetText();
        WriteTagStart(builder, "{#", sourceText);
        builder.Append(' ');
        builder.Append(comment.Text.Trim());
        builder.Append(' ');
        WriteTagEnd(builder, "#}", sourceText);
        builder.Append(Options.NewLine);
    }

    internal void WriteRaw(StringBuilder builder, TemplateRawSyntax raw, int depth)
    {
        string sourceText = raw.Span.GetText();
        int openEnd = sourceText.IndexOf("%}", StringComparison.Ordinal);
        int closeStart = sourceText.LastIndexOf("{%", StringComparison.Ordinal);
        string openingText = sourceText;
        string closingText = "";
        if (openEnd >= 0)
        {
            openingText = sourceText.Substring(0, openEnd + 2);
        }

        if (closeStart >= 0)
        {
            closingText = sourceText.Substring(closeStart);
        }

        WriteIndent(builder, depth);
        WriteTagStart(builder, "{%", openingText);
        builder.Append(" raw ");
        WriteTagEnd(builder, "%}", openingText);
        builder.Append(Options.NewLine);
        WriteText(builder, raw.Text, depth + 1);
        WriteIndent(builder, depth);
        WriteTagStart(builder, "{%", closingText);
        builder.Append(" endraw ");
        WriteTagEnd(builder, "%}", closingText);
        builder.Append(Options.NewLine);
    }

    internal void WriteBlock(StringBuilder builder, TemplateBlockSyntax block, int depth)
    {
        WriteStatementLine(builder, block.StartStatement, depth);
        WriteNodes(builder, block.Children, depth + 1);
        if (block.ElseChildren.Count > 0)
        {
            WriteElseChildren(builder, block, depth);
        }

        WriteEndStatement(builder, block, depth);
    }

    internal void WriteElseChildren(StringBuilder builder, TemplateBlockSyntax block, int depth)
    {
        int index = 0;
        while (index < block.ElseChildren.Count)
        {
            TemplateSyntaxNode node = block.ElseChildren[index];
            if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null && IsBranchStatement(node.Statement.Name))
            {
                WriteStatementLine(builder, node.Statement, depth);
            }
            else
            {
                WriteNode(builder, node, depth + 1);
            }

            index = index + 1;
        }
    }

    internal void WriteEndStatement(StringBuilder builder, TemplateBlockSyntax block, int depth)
    {
        if (block.EndStatement != null)
        {
            WriteStatementLine(builder, block.EndStatement, depth);
            return;
        }

        WriteIndent(builder, depth);
        builder.Append("{% end");
        builder.Append(block.Name);
        builder.Append(" %}");
        builder.Append(Options.NewLine);
    }

    internal void WriteStatementLine(StringBuilder builder, TemplateStatementSyntax statement, int depth)
    {
        WriteIndent(builder, depth);
        string sourceText = statement.Span.GetText();
        WriteTagStart(builder, "{%", sourceText);
        builder.Append(' ');
        builder.Append(statement.Name);
        WriteArguments(builder, statement.Arguments);
        builder.Append(' ');
        WriteTagEnd(builder, "%}", sourceText);
        builder.Append(Options.NewLine);
    }

    internal void WriteArguments(StringBuilder builder, List<TemplateExpressionSyntax> arguments)
    {
        int index = 0;
        while (index < arguments.Count)
        {
            builder.Append(' ');
            builder.Append(arguments[index].Text);
            index = index + 1;
        }
    }

    internal bool IsBranchStatement(string name)
    {
        return name == "else" || name == "elseif" || name == "when" || name == "empty";
    }

    internal void WriteTagStart(StringBuilder builder, string delimiter, string sourceText)
    {
        builder.Append(delimiter);
        if (sourceText.Length >= 3 && (sourceText[2] == '-' || sourceText[2] == '~'))
        {
            builder.Append(sourceText[2]);
        }
    }

    internal void WriteTagEnd(StringBuilder builder, string delimiter, string sourceText)
    {
        if (sourceText.Length >= 3 && (sourceText[sourceText.Length - 3] == '-' || sourceText[sourceText.Length - 3] == '~'))
        {
            builder.Append(sourceText[sourceText.Length - 3]);
        }

        builder.Append(delimiter);
    }

    internal void WriteIndent(StringBuilder builder, int depth)
    {
        int index = 0;
        while (index < depth)
        {
            builder.Append(Options.IndentText);
            index = index + 1;
        }
    }
}
