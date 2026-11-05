using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Defines the supported syntax kind values used by the template API.
/// </summary>
public enum TemplateSyntaxKind
{
    /// <summary>
    /// Identifies a document syntax node.
    /// </summary>
    Document = 0,
    /// <summary>
    /// Identifies a text syntax node.
    /// </summary>
    Text = 1,
    /// <summary>
    /// Identifies an output syntax node.
    /// </summary>
    Output = 2,
    /// <summary>
    /// Identifies a comment syntax node.
    /// </summary>
    Comment = 3,
    /// <summary>
    /// Identifies a raw syntax node.
    /// </summary>
    Raw = 4,
    /// <summary>
    /// Identifies a block syntax node.
    /// </summary>
    Block = 5,
    /// <summary>
    /// Identifies a statement syntax node.
    /// </summary>
    Statement = 6,
    /// <summary>
    /// Identifies an expression syntax node.
    /// </summary>
    Expression = 7,
    /// <summary>
    /// Identifies an error syntax node.
    /// </summary>
    Error = 8
}

/// <summary>
/// Defines the supported expression syntax kind values used by the template API.
/// </summary>
public enum TemplateExpressionSyntaxKind
{
    /// <summary>
    /// Identifies the unknown form.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Identifies the literal form.
    /// </summary>
    Literal = 1,
    /// <summary>
    /// Identifies the variable form.
    /// </summary>
    Variable = 2,
    /// <summary>
    /// Identifies the path form.
    /// </summary>
    Path = 3,
    /// <summary>
    /// Identifies the call form.
    /// </summary>
    Call = 4,
    /// <summary>
    /// Identifies the operator form.
    /// </summary>
    Operator = 5,
    /// <summary>
    /// Identifies the group form.
    /// </summary>
    Group = 6
}

/// <summary>
/// Defines the supported literal syntax kind values used by the template API.
/// </summary>
public enum TemplateLiteralSyntaxKind
{
    /// <summary>
    /// Identifies the none form.
    /// </summary>
    None = 0,
    /// <summary>
    /// Identifies the string form.
    /// </summary>
    String = 1,
    /// <summary>
    /// Identifies the number form.
    /// </summary>
    Number = 2,
    /// <summary>
    /// Identifies the boolean form.
    /// </summary>
    Boolean = 3,
    /// <summary>
    /// Identifies the null form.
    /// </summary>
    Null = 4,
    /// <summary>
    /// Identifies the empty form.
    /// </summary>
    Empty = 5,
    /// <summary>
    /// Identifies the array form.
    /// </summary>
    Array = 6,
    /// <summary>
    /// Identifies the object form.
    /// </summary>
    Object = 7,
    /// <summary>
    /// Identifies the range form.
    /// </summary>
    Range = 8
}

/// <summary>
/// Defines the supported path syntax kind values used by the template API.
/// </summary>
public enum TemplatePathSyntaxKind
{
    /// <summary>
    /// Identifies the none form.
    /// </summary>
    None = 0,
    /// <summary>
    /// Identifies the variable form.
    /// </summary>
    Variable = 1,
    /// <summary>
    /// Identifies the root form.
    /// </summary>
    Root = 2,
    /// <summary>
    /// Identifies the current form.
    /// </summary>
    Current = 3,
    /// <summary>
    /// Identifies the parent form.
    /// </summary>
    Parent = 4,
    /// <summary>
    /// Identifies the local form.
    /// </summary>
    Local = 5
}

/// <summary>
/// Defines the supported path segment syntax kind values used by the template API.
/// </summary>
public enum TemplatePathSegmentSyntaxKind
{
    /// <summary>
    /// Identifies the name form.
    /// </summary>
    Name = 0,
    /// <summary>
    /// Identifies the indexer form.
    /// </summary>
    Indexer = 1
}

/// <summary>
/// Defines the supported expression operator kind values used by the template API.
/// </summary>
public enum TemplateExpressionOperatorKind
{
    /// <summary>
    /// Identifies the none form.
    /// </summary>
    None = 0,
    /// <summary>
    /// Identifies the unary not form.
    /// </summary>
    UnaryNot = 1,
    /// <summary>
    /// Identifies the unary negate form.
    /// </summary>
    UnaryNegate = 2,
    /// <summary>
    /// Identifies the unary plus form.
    /// </summary>
    UnaryPlus = 3,
    /// <summary>
    /// Identifies the multiply form.
    /// </summary>
    Multiply = 4,
    /// <summary>
    /// Identifies the divide form.
    /// </summary>
    Divide = 5,
    /// <summary>
    /// Identifies the modulo form.
    /// </summary>
    Modulo = 6,
    /// <summary>
    /// Identifies the add form.
    /// </summary>
    Add = 7,
    /// <summary>
    /// Identifies the subtract form.
    /// </summary>
    Subtract = 8,
    /// <summary>
    /// Identifies the equal form.
    /// </summary>
    Equal = 9,
    /// <summary>
    /// Identifies the not equal form.
    /// </summary>
    NotEqual = 10,
    /// <summary>
    /// Identifies the less than form.
    /// </summary>
    LessThan = 11,
    /// <summary>
    /// Identifies the less than or equal form.
    /// </summary>
    LessThanOrEqual = 12,
    /// <summary>
    /// Identifies the greater than form.
    /// </summary>
    GreaterThan = 13,
    /// <summary>
    /// Identifies the greater than or equal form.
    /// </summary>
    GreaterThanOrEqual = 14,
    /// <summary>
    /// Identifies the and form.
    /// </summary>
    And = 15,
    /// <summary>
    /// Identifies the or form.
    /// </summary>
    Or = 16,
    /// <summary>
    /// Identifies the null coalescing form.
    /// </summary>
    NullCoalescing = 17,
    /// <summary>
    /// Identifies the conditional form.
    /// </summary>
    Conditional = 18,
    /// <summary>
    /// Identifies a containment comparison.
    /// </summary>
    Contains = 19,
    /// <summary>
    /// Identifies a prefix comparison.
    /// </summary>
    StartsWith = 20,
    /// <summary>
    /// Identifies a suffix comparison.
    /// </summary>
    EndsWith = 21
}

/// <summary>
/// Defines the supported expression call kind values used by the template API.
/// </summary>
public enum TemplateExpressionCallKind
{
    /// <summary>
    /// Identifies the none form.
    /// </summary>
    None = 0,
    /// <summary>
    /// Identifies the function form.
    /// </summary>
    Function = 1,
    /// <summary>
    /// Identifies the filter form.
    /// </summary>
    Filter = 2,
    /// <summary>
    /// Identifies the test form.
    /// </summary>
    Test = 3,
    /// <summary>
    /// Identifies the subexpression form.
    /// </summary>
    Subexpression = 4
}
