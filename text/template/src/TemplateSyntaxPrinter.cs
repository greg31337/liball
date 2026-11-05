using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Reconstructs template text from syntax nodes using native delimiters.
/// </summary>
internal static class TemplateSyntaxPrinter
{
    /// <summary>
    /// Produces a readable native or structural representation of supplied syntax.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string Print(TemplateDocumentSyntax document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        StringBuilder builder = new StringBuilder();
        WriteDocument(builder, document, 0);
        return builder.ToString();
    }

    /// <summary>
    /// Produces a readable native or structural representation of supplied syntax.
    /// </summary>
    /// <param name="node">The syntax node to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string Print(TemplateSyntaxNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        StringBuilder builder = new StringBuilder();
        WriteNode(builder, node, 0);
        return builder.ToString();
    }

    /// <summary>
    /// Produces a readable native or structural representation of supplied syntax.
    /// </summary>
    /// <param name="expression">The expression syntax to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string PrintExpression(TemplateExpressionSyntax expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        StringBuilder builder = new StringBuilder();
        WriteExpression(builder, expression, 0);
        return builder.ToString();
    }

    internal static void WriteDocument(StringBuilder builder, TemplateDocumentSyntax document, int depth)
    {
        WriteIndent(builder, depth);
        builder.Append("Document ");
        WriteSpan(builder, document.Span);
        builder.Append(" source=");
        WriteQuoted(builder, document.Source.GetDisplayName());
        builder.Append('\n');

        WriteDiagnostics(builder, document.Diagnostics, depth + 1);
        WriteChildren(builder, document.Children, depth + 1);
    }

    internal static void WriteNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Kind == TemplateSyntaxKind.Document)
        {
            if (node.Document == null)
            {
                WriteMissing(builder, depth, "Document");
            }
            else
            {
                WriteDocument(builder, node.Document, depth);
            }

            return;
        }

        if (node.Kind == TemplateSyntaxKind.Text)
        {
            WriteTextNode(builder, node, depth);
            return;
        }

        if (node.Kind == TemplateSyntaxKind.Output)
        {
            WriteOutputNode(builder, node, depth);
            return;
        }

        if (node.Kind == TemplateSyntaxKind.Comment)
        {
            WriteCommentNode(builder, node, depth);
            return;
        }

        if (node.Kind == TemplateSyntaxKind.Raw)
        {
            WriteRawNode(builder, node, depth);
            return;
        }

        if (node.Kind == TemplateSyntaxKind.Block)
        {
            WriteBlockNode(builder, node, depth);
            return;
        }

        if (node.Kind == TemplateSyntaxKind.Statement)
        {
            WriteStatementNode(builder, node, depth);
            return;
        }

        if (node.Kind == TemplateSyntaxKind.Expression)
        {
            WriteExpressionNode(builder, node, depth);
            return;
        }

        if (node.Kind == TemplateSyntaxKind.Error)
        {
            WriteErrorNode(builder, node, depth);
            return;
        }

        WriteIndent(builder, depth);
        builder.Append("UnknownNode kind=");
        builder.Append(node.Kind.ToString());
        builder.Append('\n');
    }

    internal static void WriteTextNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Text == null)
        {
            WriteMissing(builder, depth, "Text");
            return;
        }

        WriteIndent(builder, depth);
        builder.Append("Text ");
        WriteSpan(builder, node.Text.Span);
        builder.Append(" text=");
        WriteQuoted(builder, node.Text.Text);
        builder.Append('\n');
    }

    internal static void WriteOutputNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Output == null)
        {
            WriteMissing(builder, depth, "Output");
            return;
        }

        WriteIndent(builder, depth);
        builder.Append("Output ");
        WriteSpan(builder, node.Output.Span);
        builder.Append(" raw=");
        WriteBoolean(builder, node.Output.Raw);
        builder.Append(" escape=");
        WriteBoolean(builder, node.Output.Escape);
        builder.Append('\n');
        WriteExpression(builder, node.Output.Expression, depth + 1);
    }

    internal static void WriteCommentNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Comment == null)
        {
            WriteMissing(builder, depth, "Comment");
            return;
        }

        WriteIndent(builder, depth);
        builder.Append("Comment ");
        WriteSpan(builder, node.Comment.Span);
        builder.Append(" text=");
        WriteQuoted(builder, node.Comment.Text);
        builder.Append('\n');
    }

    internal static void WriteRawNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Raw == null)
        {
            WriteMissing(builder, depth, "Raw");
            return;
        }

        WriteIndent(builder, depth);
        builder.Append("Raw ");
        WriteSpan(builder, node.Raw.Span);
        builder.Append(" text=");
        WriteQuoted(builder, node.Raw.Text);
        builder.Append('\n');
    }

    internal static void WriteBlockNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Block == null)
        {
            WriteMissing(builder, depth, "Block");
            return;
        }

        TemplateBlockSyntax block = node.Block;
        WriteIndent(builder, depth);
        builder.Append("Block ");
        WriteSpan(builder, block.Span);
        builder.Append(" name=");
        WriteQuoted(builder, block.Name);
        builder.Append('\n');

        WriteIndent(builder, depth + 1);
        builder.Append("Start\n");
        WriteStatement(builder, block.StartStatement, depth + 2);

        WriteIndent(builder, depth + 1);
        builder.Append("Children\n");
        WriteChildren(builder, block.Children, depth + 2);

        if (block.ElseChildren.Count > 0)
        {
            WriteIndent(builder, depth + 1);
            builder.Append("ElseChildren\n");
            WriteChildren(builder, block.ElseChildren, depth + 2);
        }

        if (block.EndStatement != null)
        {
            WriteIndent(builder, depth + 1);
            builder.Append("End\n");
            WriteStatement(builder, block.EndStatement, depth + 2);
        }
    }

    internal static void WriteStatementNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Statement == null)
        {
            WriteMissing(builder, depth, "Statement");
            return;
        }

        WriteStatement(builder, node.Statement, depth);
    }

    internal static void WriteExpressionNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Expression == null)
        {
            WriteMissing(builder, depth, "Expression");
            return;
        }

        WriteExpression(builder, node.Expression, depth);
    }

    internal static void WriteErrorNode(StringBuilder builder, TemplateSyntaxNode node, int depth)
    {
        if (node.Error == null)
        {
            WriteMissing(builder, depth, "Error");
            return;
        }

        WriteIndent(builder, depth);
        builder.Append("Error ");
        WriteSpan(builder, node.Error.Span);
        builder.Append(" code=");
        builder.Append(node.Error.Diagnostic.Code);
        builder.Append(" message=");
        WriteQuoted(builder, node.Error.Message);
        builder.Append('\n');
    }

    internal static void WriteStatement(StringBuilder builder, TemplateStatementSyntax statement, int depth)
    {
        WriteIndent(builder, depth);
        builder.Append("Statement ");
        WriteSpan(builder, statement.Span);
        builder.Append(" name=");
        WriteQuoted(builder, statement.Name);
        builder.Append('\n');

        int index = 0;
        while (index < statement.Arguments.Count)
        {
            WriteIndent(builder, depth + 1);
            builder.Append("Argument ");
            builder.Append(index.ToString());
            builder.Append('\n');
            WriteExpression(builder, statement.Arguments[index], depth + 2);
            index = index + 1;
        }
    }

    internal static void WriteExpression(StringBuilder builder, TemplateExpressionSyntax expression, int depth)
    {
        WriteIndent(builder, depth);
        builder.Append("Expression ");
        WriteSpan(builder, expression.Span);
        builder.Append(" kind=");
        builder.Append(expression.ExpressionKind.ToString());
        if (expression.LiteralKind != TemplateLiteralSyntaxKind.None)
        {
            builder.Append(" literal=");
            builder.Append(expression.LiteralKind.ToString());
        }

        if (expression.PathKind != TemplatePathSyntaxKind.None)
        {
            builder.Append(" path=");
            builder.Append(expression.PathKind.ToString());
        }

        if (expression.OperatorKind != TemplateExpressionOperatorKind.None)
        {
            builder.Append(" operator=");
            builder.Append(expression.OperatorKind.ToString());
        }

        if (expression.CallKind != TemplateExpressionCallKind.None)
        {
            builder.Append(" call=");
            builder.Append(expression.CallKind.ToString());
            builder.Append(" name=");
            WriteQuoted(builder, expression.Name);
        }

        builder.Append(" text=");
        WriteQuoted(builder, expression.Text);
        builder.Append('\n');

        int segmentIndex = 0;
        while (segmentIndex < expression.PathSegments.Count)
        {
            WritePathSegment(builder, expression.PathSegments[segmentIndex], depth + 1);
            segmentIndex = segmentIndex + 1;
        }

        int index = 0;
        while (index < expression.Children.Count)
        {
            WriteIndent(builder, depth + 1);
            builder.Append("ChildExpression ");
            builder.Append(index.ToString());
            builder.Append('\n');
            WriteExpression(builder, expression.Children[index], depth + 2);
            index = index + 1;
        }

        int argumentIndex = 0;
        while (argumentIndex < expression.Arguments.Count)
        {
            WriteIndent(builder, depth + 1);
            builder.Append("CallArgument ");
            builder.Append(argumentIndex.ToString());
            builder.Append('\n');
            WriteExpression(builder, expression.Arguments[argumentIndex], depth + 2);
            argumentIndex = argumentIndex + 1;
        }
    }

    internal static void WritePathSegment(StringBuilder builder, TemplatePathSegmentSyntax segment, int depth)
    {
        WriteIndent(builder, depth);
        builder.Append("PathSegment ");
        builder.Append(segment.SegmentKind.ToString());
        builder.Append(" text=");
        WriteQuoted(builder, segment.Text);
        builder.Append('\n');

        if (segment.Expression != null)
        {
            WriteIndent(builder, depth + 1);
            builder.Append("IndexerExpression\n");
            WriteExpression(builder, segment.Expression, depth + 2);
        }
    }

    internal static void WriteDiagnostics(StringBuilder builder, List<TemplateDiagnostic> diagnostics, int depth)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            TemplateDiagnostic diagnostic = diagnostics[index];
            WriteIndent(builder, depth);
            builder.Append("Diagnostic ");
            builder.Append(diagnostic.Code);
            builder.Append(" ");
            builder.Append(diagnostic.Severity.ToString());
            if (diagnostic.Span != null)
            {
                builder.Append(" ");
                WriteSpan(builder, diagnostic.Span);
            }

            builder.Append(" message=");
            WriteQuoted(builder, diagnostic.Message);
            if (diagnostic.Hint != null)
            {
                builder.Append(" hint=");
                WriteQuoted(builder, diagnostic.Hint);
            }

            builder.Append('\n');
            index = index + 1;
        }
    }

    internal static void WriteChildren(StringBuilder builder, List<TemplateSyntaxNode> children, int depth)
    {
        int index = 0;
        while (index < children.Count)
        {
            WriteNode(builder, children[index], depth);
            index = index + 1;
        }
    }

    internal static void WriteMissing(StringBuilder builder, int depth, string kind)
    {
        WriteIndent(builder, depth);
        builder.Append("Missing ");
        builder.Append(kind);
        builder.Append('\n');
    }

    internal static void WriteSpan(StringBuilder builder, TemplateSourceSpan span)
    {
        builder.Append("[");
        builder.Append(span.Start.ToString());
        builder.Append("..");
        builder.Append(span.End.ToString());
        builder.Append(")");
    }

    internal static void WriteBoolean(StringBuilder builder, bool value)
    {
        if (value)
        {
            builder.Append("true");
        }
        else
        {
            builder.Append("false");
        }
    }

    internal static void WriteQuoted(StringBuilder builder, string value)
    {
        builder.Append('"');
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\\')
            {
                builder.Append("\\\\");
            }
            else if (current == '"')
            {
                builder.Append("\\\"");
            }
            else if (current == '\r')
            {
                builder.Append("\\r");
            }
            else if (current == '\n')
            {
                builder.Append("\\n");
            }
            else if (current == '\t')
            {
                builder.Append("\\t");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        builder.Append('"');
    }

    internal static void WriteIndent(StringBuilder builder, int depth)
    {
        int index = 0;
        while (index < depth)
        {
            builder.Append("  ");
            index = index + 1;
        }
    }
}
