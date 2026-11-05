using System;

namespace sql;

/// <summary>Represents a SQL expression with a kind tag and fields for the corresponding expression form.</summary>
/// <remarks>Inspect <see cref="Kind"/> before reading form-specific fields. References that do not apply to the selected kind are normally <c>null</c>, and collection fields are empty arrays.</remarks>
public class SqlExpression
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlExpressionKind Kind;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The tokens in source or insertion order.</summary>
    public SqlToken[] Tokens;
    /// <summary>The arguments in source or insertion order.</summary>
    public SqlExpression[] Arguments;
    /// <summary>The left associated with this value.</summary>
    public SqlExpression? Left;
    /// <summary>The right associated with this value.</summary>
    public SqlExpression? Right;
    /// <summary>The operand associated with this value.</summary>
    public SqlExpression? Operand;
    /// <summary>The alternate associated with this value.</summary>
    public SqlExpression? Alternate;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName? Name;
    /// <summary>The literal associated with this value.</summary>
    public SqlLiteral? Literal;
    /// <summary>The nested query, or <c>null</c> when this syntax form has no query.</summary>
    public SqlQuery? Query;
    /// <summary>The source type name for a cast, or <c>null</c> when this is not a cast expression.</summary>
    public string? TypeName;
    /// <summary>The original placeholder text for a parameter, or <c>null</c> when this is not a parameter expression.</summary>
    public string? Value;
    /// <summary>The operator text associated with this value.</summary>
    public string? OperatorText;
    /// <summary>The operator token associated with this value.</summary>
    public SqlToken? OperatorToken;
    /// <summary>The unary operator associated with this value.</summary>
    public SqlUnaryOperatorKind UnaryOperator;
    /// <summary>The binary operator associated with this value.</summary>
    public SqlBinaryOperatorKind BinaryOperator;
    /// <summary>Indicates whether distinct.</summary>
    public bool IsDistinct;
    /// <summary>The order by in source or insertion order.</summary>
    public SqlOrderingTerm[] OrderBy;
    /// <summary>The filter associated with this value.</summary>
    public SqlExpression? Filter;
    /// <summary>The window definition associated with this value.</summary>
    public SqlWindowDefinition? WindowDefinition;
    /// <summary>The typed collation payload, or <c>null</c> for other expression kinds.</summary>
    public SqlCollateExpressionData? CollateData;
    /// <summary>The typed pattern and escape payload, or <c>null</c> for other expression kinds.</summary>
    public SqlPatternExpressionData? PatternData;
    /// <summary>The typed distinctness payload, or <c>null</c> for other expression kinds.</summary>
    public SqlDistinctnessExpressionData? DistinctnessData;
    /// <summary>The typed <c>IN</c> payload, or <c>null</c> for other expression kinds.</summary>
    public SqlInExpressionData? InData;
    /// <summary>The typed extract payload, or <c>null</c> for other expression kinds.</summary>
    public SqlExtractExpressionData? ExtractData;
    /// <summary>The typed cast payload, or <c>null</c> for other expression kinds.</summary>
    public SqlCastExpressionData? CastData;
    /// <summary>The typed array-constructor payload, or <c>null</c> for other expression kinds.</summary>
    public SqlArrayExpressionData? ArrayData;
    /// <summary>The typed subscript payload, or <c>null</c> for other expression kinds.</summary>
    public SqlSubscriptExpressionData? SubscriptData;
    /// <summary>The typed JSON or custom-operator payload, or <c>null</c> for other expression kinds.</summary>
    public SqlOperatorExpressionData? OperatorData;
    /// <summary>The typed wildcard payload, or <c>null</c> for other expression kinds.</summary>
    public SqlWildcardExpressionData? WildcardData;
    /// <summary>The typed function, aggregate, filter, and window payload, or <c>null</c> for other expression kinds.</summary>
    public SqlFunctionInvocationData? FunctionData;

    /// <summary>Initializes a new instance of <see cref="SqlExpression"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlExpression Create(SqlExpressionKind kind, SqlTextSpan span)
    {
        return SqlExpression.Create(kind, span, new SqlToken[0], new SqlExpression[0]);
    }

    /// <summary>Initializes a new instance of <see cref="SqlExpression"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlExpression Create(SqlExpressionKind kind, SqlTextSpan span, SqlToken[] tokens)
    {
        return SqlExpression.Create(kind, span, tokens, new SqlExpression[0]);
    }

    /// <summary>Creates an expression from explicit tokens and arguments.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <param name="arguments">The arguments to use.</param>
    /// <returns>The resulting expression.</returns>
    internal static SqlExpression Create(SqlExpressionKind kind, SqlTextSpan span, SqlToken[] tokens, SqlExpression[] arguments)
    {
        return new SqlExpression(kind, span, tokens, arguments);
    }

    /// <summary>Initializes a new instance of <see cref="SqlExpression"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <param name="arguments">The arguments to use.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlExpression(SqlExpressionKind kind, SqlTextSpan span, SqlToken[] tokens, SqlExpression[] arguments)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ValidateTokens(tokens);
        ValidateArguments(arguments);

        Kind = kind;
        Span = span;
        Tokens = tokens;
        Arguments = arguments;
        Left = null;
        Right = null;
        Operand = null;
        Alternate = null;
        Name = null;
        Literal = null;
        Query = null;
        TypeName = null;
        Value = null;
        OperatorText = null;
        OperatorToken = null;
        UnaryOperator = SqlUnaryOperatorKind.None;
        BinaryOperator = SqlBinaryOperatorKind.None;
        IsDistinct = false;
        OrderBy = new SqlOrderingTerm[0];
        Filter = null;
        WindowDefinition = null;
        CollateData = null;
        PatternData = null;
        DistinctnessData = null;
        InData = null;
        ExtractData = null;
        CastData = null;
        ArrayData = null;
        SubscriptData = null;
        OperatorData = null;
        WildcardData = null;
        FunctionData = null;
    }

    /// <summary>Returns whether the requested kind matches this value's discriminator.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsKind(SqlExpressionKind kind)
    {
        return Kind == kind;
    }

    /// <summary>Returns whether tokens is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTokens()
    {
        return Tokens.Length > 0;
    }

    /// <summary>Returns the number of token items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetTokenCount()
    {
        return Tokens.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting token.</returns>
    internal SqlToken GetTokenAt(int index)
    {
        return Tokens[index];
    }

    /// <summary>Returns whether arguments is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasArguments()
    {
        return Arguments.Length > 0;
    }

    /// <summary>Returns the number of argument items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetArgumentCount()
    {
        return Arguments.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting expression.</returns>
    internal SqlExpression GetArgumentAt(int index)
    {
        return Arguments[index];
    }

    internal static void ValidateTokens(SqlToken[] tokens)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == null)
            {
                throw new ArgumentNullException("tokens");
            }

            index = index + 1;
        }
    }

    internal static void ValidateArguments(SqlExpression[] arguments)
    {
        if (arguments == null)
        {
            throw new ArgumentNullException("arguments");
        }

        int index = 0;
        while (index < arguments.Length)
        {
            if (arguments[index] == null)
            {
                throw new ArgumentNullException("arguments");
            }

            index = index + 1;
        }
    }
}
