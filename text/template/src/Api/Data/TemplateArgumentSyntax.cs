using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Represents a named argument and records whether it uses helper-style hash semantics.
/// </summary>
public class TemplateNamedArgumentSyntax
{
    /// <summary>
    /// Identifies the complete source range represented by this argument.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Identifies the source range of the argument name.
    /// </summary>
    public TemplateSourceSpan NameSpan;
    /// <summary>
    /// Identifies the source range of the equals delimiter.
    /// </summary>
    public TemplateSourceSpan EqualsSpan;
    /// <summary>
    /// Stores the argument name.
    /// </summary>
    public string Name;
    /// <summary>
    /// Specifies the argument value expression.
    /// </summary>
    public TemplateExpressionSyntax Value;
    /// <summary>
    /// Indicates whether the argument uses Handlebars-style hash semantics.
    /// </summary>
    public bool Hash;

    /// <summary>
    /// Initializes named argument syntax and validates the supplied values.
    /// </summary>
    /// <param name="span">The complete source range associated with the argument.</param>
    /// <param name="nameSpan">The source range associated with the argument name.</param>
    /// <param name="equalsSpan">The source range associated with the equals delimiter.</param>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The argument value expression.</param>
    /// <param name="hash">Whether the argument uses helper-style hash semantics.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateNamedArgumentSyntax(TemplateSourceSpan span, TemplateSourceSpan nameSpan, TemplateSourceSpan equalsSpan, string name, TemplateExpressionSyntax value, bool hash)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (nameSpan == null)
        {
            throw new ArgumentNullException("nameSpan");
        }

        if (equalsSpan == null)
        {
            throw new ArgumentNullException("equalsSpan");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        Span = span;
        NameSpan = nameSpan;
        EqualsSpan = equalsSpan;
        Name = name;
        Value = value;
        Hash = hash;
    }
}

/// <summary>
/// Represents one alias declared by a block parameter list.
/// </summary>
public class TemplateBlockParameterSyntax
{
    /// <summary>
    /// Identifies the exact source range represented by this parameter.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Stores the parameter name.
    /// </summary>
    public string Name;

    /// <summary>
    /// Initializes block parameter syntax and validates the supplied values.
    /// </summary>
    /// <param name="span">The exact source range associated with the parameter.</param>
    /// <param name="name">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateBlockParameterSyntax(TemplateSourceSpan span, string name)
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
        Name = name;
    }
}

/// <summary>
/// Represents an expression used to select a template, partial, or parent name at runtime.
/// </summary>
public class TemplateDynamicNameSyntax
{
    /// <summary>
    /// Identifies the exact source range represented by this dynamic name.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Specifies the expression that produces the name.
    /// </summary>
    public TemplateExpressionSyntax Expression;

    /// <summary>
    /// Initializes dynamic name syntax and validates the supplied values.
    /// </summary>
    /// <param name="span">The exact source range associated with the dynamic name.</param>
    /// <param name="expression">The expression that produces the name.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateDynamicNameSyntax(TemplateSourceSpan span, TemplateExpressionSyntax expression)
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
    }
}

/// <summary>
/// Represents one ordered alternate branch in a block.
/// </summary>
public class TemplateBranchSyntax
{
    /// <summary>
    /// Identifies the source range from the branch tag through its body.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Specifies the statement that starts the branch.
    /// </summary>
    public TemplateStatementSyntax Statement;
    /// <summary>
    /// Stores the ordered children belonging only to this branch.
    /// </summary>
    public List<TemplateSyntaxNode> Children;

    /// <summary>
    /// Initializes branch syntax and validates the supplied values.
    /// </summary>
    /// <param name="span">The source range associated with the branch.</param>
    /// <param name="statement">The statement that starts the branch.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateBranchSyntax(TemplateSourceSpan span, TemplateStatementSyntax statement)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        Span = span;
        Statement = statement;
        Children = new List<TemplateSyntaxNode>();
    }

    internal void AddChild(TemplateSyntaxNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        Children.Add(node);
    }
}
