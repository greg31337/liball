using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Wraps one concrete syntax-node kind so different document children can be stored in one list.
/// </summary>
public class TemplateSyntaxNode
{
    /// <summary>
    /// Identifies the kind represented by this syntax node.
    /// </summary>
    public TemplateSyntaxKind Kind;
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Specifies the document for this syntax node.
    /// </summary>
    public TemplateDocumentSyntax? Document;
    /// <summary>
    /// Stores the source or rendered text represented by this value.
    /// </summary>
    public TemplateTextSyntax? Text;
    /// <summary>
    /// Specifies the output for this syntax node.
    /// </summary>
    public TemplateOutputSyntax? Output;
    /// <summary>
    /// Specifies the comment for this syntax node.
    /// </summary>
    public TemplateCommentSyntax? Comment;
    /// <summary>
    /// Specifies the raw for this syntax node.
    /// </summary>
    public TemplateRawSyntax? Raw;
    /// <summary>
    /// Specifies the block for this syntax node.
    /// </summary>
    public TemplateBlockSyntax? Block;
    /// <summary>
    /// Specifies the statement for this syntax node.
    /// </summary>
    public TemplateStatementSyntax? Statement;
    /// <summary>
    /// Specifies the expression for this syntax node.
    /// </summary>
    public TemplateExpressionSyntax? Expression;
    /// <summary>
    /// Specifies the error for this syntax node.
    /// </summary>
    public TemplateErrorSyntax? Error;

    /// <summary>
    /// Initializes a new template syntax node and validates any supplied values.
    /// </summary>
    /// <param name="kind">The kind represented by the new value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateSyntaxNode(TemplateSyntaxKind kind, TemplateSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Kind = kind;
        Span = span;
        Document = null;
        Text = null;
        Output = null;
        Comment = null;
        Raw = null;
        Block = null;
        Statement = null;
        Expression = null;
        Error = null;
    }

    /// <summary>
    /// Creates a template representation from the supplied document.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromDocument(TemplateDocumentSyntax document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Document, document.Span);
        node.Document = document;
        return node;
    }

    /// <summary>
    /// Creates a template representation from the supplied text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromText(TemplateTextSyntax text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Text, text.Span);
        node.Text = text;
        return node;
    }

    /// <summary>
    /// Creates a template representation from the supplied output.
    /// </summary>
    /// <param name="output">The output used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromOutput(TemplateOutputSyntax output)
    {
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Output, output.Span);
        node.Output = output;
        return node;
    }

    /// <summary>
    /// Creates a template representation from the supplied comment.
    /// </summary>
    /// <param name="comment">The comment used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromComment(TemplateCommentSyntax comment)
    {
        if (comment == null)
        {
            throw new ArgumentNullException("comment");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Comment, comment.Span);
        node.Comment = comment;
        return node;
    }

    /// <summary>
    /// Creates a template representation from the supplied raw.
    /// </summary>
    /// <param name="raw">Whether to bypass automatic escaping.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromRaw(TemplateRawSyntax raw)
    {
        if (raw == null)
        {
            throw new ArgumentNullException("raw");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Raw, raw.Span);
        node.Raw = raw;
        return node;
    }

    /// <summary>
    /// Creates a template representation from the supplied block.
    /// </summary>
    /// <param name="block">The block used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromBlock(TemplateBlockSyntax block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Block, block.Span);
        node.Block = block;
        return node;
    }

    /// <summary>
    /// Creates a template representation from the supplied statement.
    /// </summary>
    /// <param name="statement">The statement used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromStatement(TemplateStatementSyntax statement)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Statement, statement.Span);
        node.Statement = statement;
        return node;
    }

    /// <summary>
    /// Creates a template representation from the supplied expression.
    /// </summary>
    /// <param name="expression">The expression syntax to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromExpression(TemplateExpressionSyntax expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Expression, expression.Span);
        node.Expression = expression;
        return node;
    }

    /// <summary>
    /// Creates a template representation from the supplied error.
    /// </summary>
    /// <param name="error">The error used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSyntaxNode FromError(TemplateErrorSyntax error)
    {
        if (error == null)
        {
            throw new ArgumentNullException("error");
        }

        TemplateSyntaxNode node = new TemplateSyntaxNode(TemplateSyntaxKind.Error, error.Span);
        node.Error = error;
        return node;
    }

    /// <summary>
    /// Determines whether the error condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsError()
    {
        return Kind == TemplateSyntaxKind.Error;
    }

    /// <summary>
    /// Returns the source text.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal string GetSourceText()
    {
        return Span.GetText();
    }
}

/// <summary>
/// Represents a parsed template document and its ordered child nodes and diagnostics.
/// </summary>
public class TemplateDocumentSyntax
{
    /// <summary>
    /// Stores the template source associated with this value.
    /// </summary>
    public TemplateSource Source;
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Stores the ordered children.
    /// </summary>
    public List<TemplateSyntaxNode> Children;
    /// <summary>
    /// Stores the ordered diagnostics.
    /// </summary>
    public List<TemplateDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new template document syntax and validates any supplied values.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateDocumentSyntax(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        Source = source;
        Span = source.CreateSpan(0, source.Length);
        Children = new List<TemplateSyntaxNode>();
        Diagnostics = new List<TemplateDiagnostic>();
    }

    /// <summary>
    /// Adds the supplied child after validating it.
    /// </summary>
    /// <param name="node">The syntax node to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddChild(TemplateSyntaxNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        Children.Add(node);
    }

    /// <summary>
    /// Adds the supplied diagnostic after validating it.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add or format.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddDiagnostic(TemplateDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        Diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Indicates whether this value contains children.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasChildren()
    {
        return Children.Count > 0;
    }

    /// <summary>
    /// Indicates whether this value contains diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasDiagnostics()
    {
        return Diagnostics.Count > 0;
    }
}

/// <summary>
/// Represents literal text copied directly to rendered output.
/// </summary>
public class TemplateTextSyntax
{
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Stores the source or rendered text represented by this value.
    /// </summary>
    public string Text;

    /// <summary>
    /// Initializes text syntax by copying text from the source span.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateTextSyntax(TemplateSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Span = span;
        Text = span.GetText();
    }

    /// <summary>
    /// Creates text syntax with text that can differ from the source span contents.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The new text syntax.</returns>
    internal static TemplateTextSyntax Create(TemplateSourceSpan span, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        TemplateTextSyntax syntax = new TemplateTextSyntax(span);
        syntax.Text = text;
        return syntax;
    }
}

/// <summary>
/// Represents an expression output tag and whether its result is escaped or treated as raw text.
/// </summary>
public class TemplateOutputSyntax
{
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Specifies the expression for this output syntax.
    /// </summary>
    public TemplateExpressionSyntax Expression;
    /// <summary>
    /// Indicates whether the output bypasses automatic escaping.
    /// </summary>
    public bool Raw;
    /// <summary>
    /// Indicates whether the output explicitly requests encoding.
    /// </summary>
    public bool Escape;

    /// <summary>
    /// Initializes a new template output syntax and validates any supplied values.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="expression">The expression syntax to process.</param>
    /// <param name="raw">Whether to bypass automatic escaping.</param>
    /// <param name="escape">Whether the output node requests escaping.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateOutputSyntax(TemplateSourceSpan span, TemplateExpressionSyntax expression, bool raw, bool escape)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        Span = span;
        Expression = expression;
        Raw = raw;
        Escape = escape;
    }
}

/// <summary>
/// Represents a template comment that does not produce rendered output.
/// </summary>
public class TemplateCommentSyntax
{
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Stores the source or rendered text represented by this value.
    /// </summary>
    public string Text;

    /// <summary>
    /// Initializes a new template comment syntax and validates any supplied values.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateCommentSyntax(TemplateSourceSpan span, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Span = span;
        Text = text;
    }
}

/// <summary>
/// Represents raw block text emitted without parsing nested tags.
/// </summary>
public class TemplateRawSyntax
{
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Stores the source or rendered text represented by this value.
    /// </summary>
    public string Text;

    /// <summary>
    /// Initializes a new template raw syntax and validates any supplied values.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateRawSyntax(TemplateSourceSpan span, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Span = span;
        Text = text;
    }
}

/// <summary>
/// Represents a named statement and its ordered expression arguments.
/// </summary>
public class TemplateStatementSyntax
{
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Identifies the exact source range of the statement name.
    /// </summary>
    public TemplateSourceSpan NameSpan;
    /// <summary>
    /// Identifies the source range inside the statement delimiters, excluding trim markers.
    /// </summary>
    public TemplateSourceSpan ContentSpan;
    /// <summary>
    /// Stores the logical name used to identify this value.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the ordered arguments.
    /// </summary>
    public List<TemplateExpressionSyntax> Arguments;
    /// <summary>
    /// Stores named arguments in source order.
    /// </summary>
    public List<TemplateNamedArgumentSyntax> NamedArguments;
    /// <summary>
    /// Stores helper-style hash arguments in source order.
    /// </summary>
    public List<TemplateNamedArgumentSyntax> HashArguments;
    /// <summary>
    /// Stores aliases declared by an <c>as |...|</c> block parameter list.
    /// </summary>
    public List<TemplateBlockParameterSyntax> BlockParameters;
    /// <summary>
    /// Specifies a runtime-selected composition name when the first name argument is not a string literal.
    /// </summary>
    public TemplateDynamicNameSyntax? DynamicName;
    /// <summary>
    /// Stores malformed argument fragments retained for recovery.
    /// </summary>
    public List<TemplateErrorSyntax> RecoveryNodes;

    /// <summary>
    /// Initializes a new template statement syntax and validates any supplied values.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="name">The logical name used for lookup.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateStatementSyntax(TemplateSourceSpan span, string name)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        Span = span;
        NameSpan = span;
        ContentSpan = span;
        Name = name;
        Arguments = new List<TemplateExpressionSyntax>();
        NamedArguments = new List<TemplateNamedArgumentSyntax>();
        HashArguments = new List<TemplateNamedArgumentSyntax>();
        BlockParameters = new List<TemplateBlockParameterSyntax>();
        DynamicName = null;
        RecoveryNodes = new List<TemplateErrorSyntax>();
    }

    /// <summary>
    /// Adds the supplied argument after validating it.
    /// </summary>
    /// <param name="expression">The expression syntax to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddArgument(TemplateExpressionSyntax expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        Arguments.Add(expression);
    }

    internal void AddNamedArgument(TemplateNamedArgumentSyntax argument)
    {
        if (argument == null)
        {
            throw new ArgumentNullException("argument");
        }

        NamedArguments.Add(argument);
    }

    internal void AddHashArgument(TemplateNamedArgumentSyntax argument)
    {
        if (argument == null)
        {
            throw new ArgumentNullException("argument");
        }

        HashArguments.Add(argument);
    }

    internal void AddBlockParameter(TemplateBlockParameterSyntax parameter)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException("parameter");
        }

        BlockParameters.Add(parameter);
    }

    internal void SetDynamicName(TemplateDynamicNameSyntax dynamicName)
    {
        if (dynamicName == null)
        {
            throw new ArgumentNullException("dynamicName");
        }

        DynamicName = dynamicName;
    }

    internal void AddRecoveryNode(TemplateErrorSyntax error)
    {
        if (error == null)
        {
            throw new ArgumentNullException("error");
        }

        RecoveryNodes.Add(error);
    }

    internal void SetLexicalSpans(TemplateSourceSpan nameSpan, TemplateSourceSpan contentSpan)
    {
        if (nameSpan == null)
        {
            throw new ArgumentNullException("nameSpan");
        }

        if (contentSpan == null)
        {
            throw new ArgumentNullException("contentSpan");
        }

        NameSpan = nameSpan;
        ContentSpan = contentSpan;
    }
}

/// <summary>
/// Represents a block statement, its normal and alternate children, and its optional closing statement.
/// </summary>
public class TemplateBlockSyntax
{
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Stores the logical name used to identify this value.
    /// </summary>
    public string Name;
    /// <summary>
    /// Specifies the start statement for this block syntax.
    /// </summary>
    public TemplateStatementSyntax StartStatement;
    /// <summary>
    /// Specifies the end statement for this block syntax.
    /// </summary>
    public TemplateStatementSyntax? EndStatement;
    /// <summary>
    /// Stores the ordered children.
    /// </summary>
    public List<TemplateSyntaxNode> Children;
    /// <summary>
    /// Stores the ordered else children.
    /// </summary>
    public List<TemplateSyntaxNode> ElseChildren;
    /// <summary>
    /// Stores alternate branches as separate ordered nodes while <see cref="ElseChildren"/> remains available as a flattened compatibility view.
    /// </summary>
    public List<TemplateBranchSyntax> Branches;
    /// <summary>
    /// Stores malformed block fragments retained for recovery.
    /// </summary>
    public List<TemplateErrorSyntax> RecoveryNodes;

    /// <summary>
    /// Initializes a new template block syntax and validates any supplied values.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="startStatement">The start statement used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateBlockSyntax(TemplateSourceSpan span, string name, TemplateStatementSyntax startStatement)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (startStatement == null)
        {
            throw new ArgumentNullException("startStatement");
        }

        Span = span;
        Name = name;
        StartStatement = startStatement;
        EndStatement = null;
        Children = new List<TemplateSyntaxNode>();
        ElseChildren = new List<TemplateSyntaxNode>();
        Branches = new List<TemplateBranchSyntax>();
        RecoveryNodes = new List<TemplateErrorSyntax>();
    }

    /// <summary>
    /// Adds the supplied child after validating it.
    /// </summary>
    /// <param name="node">The syntax node to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddChild(TemplateSyntaxNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        Children.Add(node);
    }

    /// <summary>
    /// Adds the supplied else child after validating it.
    /// </summary>
    /// <param name="node">The syntax node to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddElseChild(TemplateSyntaxNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        ElseChildren.Add(node);
    }

    internal void AddBranch(TemplateBranchSyntax branch)
    {
        if (branch == null)
        {
            throw new ArgumentNullException("branch");
        }

        Branches.Add(branch);
    }

    internal void AddRecoveryNode(TemplateErrorSyntax error)
    {
        if (error == null)
        {
            throw new ArgumentNullException("error");
        }

        RecoveryNodes.Add(error);
    }

    /// <summary>
    /// Replaces the configured end statement after validating the supplied value.
    /// </summary>
    /// <param name="statement">The statement used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void SetEndStatement(TemplateStatementSyntax statement)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        EndStatement = statement;
    }

    /// <summary>
    /// Indicates whether this value contains end statement.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasEndStatement()
    {
        return EndStatement != null;
    }
}

/// <summary>
/// Represents a literal, path, call, collection, range, unary, binary, test, or conditional expression.
/// </summary>
public class TemplateExpressionSyntax
{
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Identifies the expression kind represented by this expression syntax.
    /// </summary>
    public TemplateExpressionSyntaxKind ExpressionKind;
    /// <summary>
    /// Identifies the literal kind represented by this expression syntax.
    /// </summary>
    public TemplateLiteralSyntaxKind LiteralKind;
    /// <summary>
    /// Identifies the path kind represented by this expression syntax.
    /// </summary>
    public TemplatePathSyntaxKind PathKind;
    /// <summary>
    /// Identifies the operator kind represented by this expression syntax.
    /// </summary>
    public TemplateExpressionOperatorKind OperatorKind;
    /// <summary>
    /// Identifies the call kind represented by this expression syntax.
    /// </summary>
    public TemplateExpressionCallKind CallKind;
    /// <summary>
    /// Stores the logical name used to identify this value.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the source or rendered text represented by this value.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the ordered children.
    /// </summary>
    public List<TemplateExpressionSyntax> Children;
    /// <summary>
    /// Stores the ordered path segments.
    /// </summary>
    public List<TemplatePathSegmentSyntax> PathSegments;
    /// <summary>
    /// Stores the ordered arguments.
    /// </summary>
    public List<TemplateExpressionSyntax> Arguments;
    /// <summary>
    /// Stores named call arguments in source order.
    /// </summary>
    public List<TemplateNamedArgumentSyntax> NamedArguments;
    /// <summary>
    /// Stores helper-style hash arguments in source order.
    /// </summary>
    public List<TemplateNamedArgumentSyntax> HashArguments;
    /// <summary>
    /// Stores malformed expression fragments retained for recovery.
    /// </summary>
    public List<TemplateErrorSyntax> RecoveryNodes;

    /// <summary>
    /// Initializes expression syntax without an additional literal, path, operator, or call classification.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="expressionKind">The expression kind used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateExpressionSyntax(TemplateSourceSpan span, TemplateExpressionSyntaxKind expressionKind, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Span = span;
        ExpressionKind = expressionKind;
        LiteralKind = TemplateLiteralSyntaxKind.None;
        PathKind = TemplatePathSyntaxKind.None;
        OperatorKind = TemplateExpressionOperatorKind.None;
        CallKind = TemplateExpressionCallKind.None;
        Name = "";
        Text = text;
        Children = new List<TemplateExpressionSyntax>();
        PathSegments = new List<TemplatePathSegmentSyntax>();
        Arguments = new List<TemplateExpressionSyntax>();
        NamedArguments = new List<TemplateNamedArgumentSyntax>();
        HashArguments = new List<TemplateNamedArgumentSyntax>();
        RecoveryNodes = new List<TemplateErrorSyntax>();
    }

    /// <summary>
    /// Creates expression syntax with a literal classification.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="expressionKind">The expression kind used by the operation.</param>
    /// <param name="literalKind">The literal kind used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The new expression syntax.</returns>
    internal static TemplateExpressionSyntax Create(TemplateSourceSpan span, TemplateExpressionSyntaxKind expressionKind, TemplateLiteralSyntaxKind literalKind, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        TemplateExpressionSyntax syntax = new TemplateExpressionSyntax(span, expressionKind, text);
        syntax.LiteralKind = literalKind;
        return syntax;
    }

    /// <summary>
    /// Creates expression syntax with a path classification.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="expressionKind">The expression kind used by the operation.</param>
    /// <param name="pathKind">The path kind used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The new expression syntax.</returns>
    internal static TemplateExpressionSyntax Create(TemplateSourceSpan span, TemplateExpressionSyntaxKind expressionKind, TemplatePathSyntaxKind pathKind, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        TemplateExpressionSyntax syntax = new TemplateExpressionSyntax(span, expressionKind, text);
        syntax.PathKind = pathKind;
        return syntax;
    }

    /// <summary>
    /// Creates expression syntax with an operator classification.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="expressionKind">The expression kind used by the operation.</param>
    /// <param name="operatorKind">The operator kind used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The new expression syntax.</returns>
    internal static TemplateExpressionSyntax Create(TemplateSourceSpan span, TemplateExpressionSyntaxKind expressionKind, TemplateExpressionOperatorKind operatorKind, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        TemplateExpressionSyntax syntax = new TemplateExpressionSyntax(span, expressionKind, text);
        syntax.OperatorKind = operatorKind;
        return syntax;
    }

    /// <summary>
    /// Creates expression syntax with a call classification and callable name.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="expressionKind">The expression kind used by the operation.</param>
    /// <param name="callKind">The call kind used by the operation.</param>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The new expression syntax.</returns>
    internal static TemplateExpressionSyntax Create(TemplateSourceSpan span, TemplateExpressionSyntaxKind expressionKind, TemplateExpressionCallKind callKind, string name, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        TemplateExpressionSyntax syntax = new TemplateExpressionSyntax(span, expressionKind, text);
        syntax.CallKind = callKind;
        syntax.Name = name;
        return syntax;
    }

    /// <summary>
    /// Adds the supplied child after validating it.
    /// </summary>
    /// <param name="expression">The expression syntax to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddChild(TemplateExpressionSyntax expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        Children.Add(expression);
    }

    /// <summary>
    /// Indicates whether this value contains children.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasChildren()
    {
        return Children.Count > 0;
    }

    /// <summary>
    /// Adds the supplied path segment after validating it.
    /// </summary>
    /// <param name="segment">The segment used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddPathSegment(TemplatePathSegmentSyntax segment)
    {
        if (segment == null)
        {
            throw new ArgumentNullException("segment");
        }

        PathSegments.Add(segment);
    }

    /// <summary>
    /// Indicates whether this value contains path segments.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasPathSegments()
    {
        return PathSegments.Count > 0;
    }

    /// <summary>
    /// Adds the supplied argument after validating it.
    /// </summary>
    /// <param name="expression">The expression syntax to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddArgument(TemplateExpressionSyntax expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        Arguments.Add(expression);
    }

    /// <summary>
    /// Indicates whether this value contains arguments.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasArguments()
    {
        return Arguments.Count > 0;
    }

    internal void AddNamedArgument(TemplateNamedArgumentSyntax argument)
    {
        if (argument == null)
        {
            throw new ArgumentNullException("argument");
        }

        NamedArguments.Add(argument);
    }

    internal void AddHashArgument(TemplateNamedArgumentSyntax argument)
    {
        if (argument == null)
        {
            throw new ArgumentNullException("argument");
        }

        HashArguments.Add(argument);
    }

    internal void AddRecoveryNode(TemplateErrorSyntax error)
    {
        if (error == null)
        {
            throw new ArgumentNullException("error");
        }

        RecoveryNodes.Add(error);
    }
}

/// <summary>
/// Represents a named or indexed segment in a template path expression.
/// </summary>
public class TemplatePathSegmentSyntax
{
    /// <summary>
    /// Identifies the exact source range represented by this segment when it originated in parsed source.
    /// </summary>
    public TemplateSourceSpan? Span;
    /// <summary>
    /// Identifies the segment kind represented by this path segment syntax.
    /// </summary>
    public TemplatePathSegmentSyntaxKind SegmentKind;
    /// <summary>
    /// Stores the source or rendered text represented by this value.
    /// </summary>
    public string Text;
    /// <summary>
    /// Specifies the expression for this path segment syntax.
    /// </summary>
    public TemplateExpressionSyntax? Expression;
    /// <summary>
    /// Indicates whether a missing intermediate value stops path traversal without a strict-mode failure.
    /// </summary>
    public bool NullSafe;

    /// <summary>
    /// Initializes a path segment without an index expression or null-safe behavior.
    /// </summary>
    /// <param name="segmentKind">The segment kind used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplatePathSegmentSyntax(TemplatePathSegmentSyntaxKind segmentKind, string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Span = null;
        SegmentKind = segmentKind;
        Text = text;
        Expression = null;
        NullSafe = false;
    }

    /// <summary>
    /// Creates a path segment with explicit null-safe behavior.
    /// </summary>
    /// <param name="segmentKind">The segment kind used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <param name="nullSafe">Whether a missing intermediate value stops lookup without a strict-mode failure.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The new path segment syntax.</returns>
    internal static TemplatePathSegmentSyntax Create(TemplatePathSegmentSyntaxKind segmentKind, string text, bool nullSafe)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        TemplatePathSegmentSyntax syntax = new TemplatePathSegmentSyntax(segmentKind, text);
        syntax.NullSafe = nullSafe;
        return syntax;
    }

    /// <summary>
    /// Creates an indexed path segment.
    /// </summary>
    /// <param name="segmentKind">The segment kind used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <param name="expression">The expression syntax to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The new path segment syntax.</returns>
    internal static TemplatePathSegmentSyntax Create(TemplatePathSegmentSyntaxKind segmentKind, string text, TemplateExpressionSyntax expression)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        TemplatePathSegmentSyntax syntax = new TemplatePathSegmentSyntax(segmentKind, text);
        syntax.Expression = expression;
        return syntax;
    }

    /// <summary>
    /// Creates an indexed path segment with explicit null-safe behavior.
    /// </summary>
    /// <param name="segmentKind">The segment kind used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <param name="expression">The expression syntax to process.</param>
    /// <param name="nullSafe">Whether a missing intermediate value stops lookup without a strict-mode failure.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The new path segment syntax.</returns>
    internal static TemplatePathSegmentSyntax Create(TemplatePathSegmentSyntaxKind segmentKind, string text, TemplateExpressionSyntax expression, bool nullSafe)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        TemplatePathSegmentSyntax syntax = new TemplatePathSegmentSyntax(segmentKind, text);
        syntax.Expression = expression;
        syntax.NullSafe = nullSafe;
        return syntax;
    }

    internal void SetSpan(TemplateSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Span = span;
    }
}

/// <summary>
/// Represents invalid source retained in the syntax tree for diagnostics and tooling.
/// </summary>
public class TemplateErrorSyntax
{
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Specifies the diagnostic for this error syntax.
    /// </summary>
    public TemplateDiagnostic Diagnostic;
    /// <summary>
    /// Specifies the message for this error syntax.
    /// </summary>
    public string Message;
    /// <summary>
    /// Stores the exact invalid source retained by this recovery node.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the lexer tokens retained by this recovery node in source order.
    /// </summary>
    public List<TemplateToken> RecoveredTokens;

    /// <summary>
    /// Initializes a new template error syntax and validates any supplied values.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="diagnostic">The diagnostic to add or format.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateErrorSyntax(TemplateSourceSpan span, TemplateDiagnostic diagnostic)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        Span = span;
        Diagnostic = diagnostic;
        Message = diagnostic.Message;
        Text = span.GetText();
        RecoveredTokens = new List<TemplateToken>();
    }

    internal void AddRecoveredToken(TemplateToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        RecoveredTokens.Add(token);
    }
}
