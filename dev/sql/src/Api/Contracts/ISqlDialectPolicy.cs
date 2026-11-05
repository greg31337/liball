namespace sql;

/// <summary>Exposes an immutable snapshot of SQL dialect syntax policies.</summary>
public interface ISqlDialectPolicy
{
    /// <summary>Returns the represented dialect kind.</summary>
    /// <returns>The dialect kind.</returns>
    SqlDialectKind GetKind();

    /// <summary>Returns canonical keywords in ordinal order.</summary>
    /// <returns>An independent keyword array.</returns>
    string[] GetKeywords();

    /// <summary>Returns whether text is a keyword under the snapshot's casing policy.</summary>
    /// <param name="text">The text to inspect.</param>
    /// <returns><c>true</c> when text is a keyword; otherwise, <c>false</c>.</returns>
    bool IsKeyword(string text);

    /// <summary>Returns operator and precedence policies.</summary>
    /// <returns>Independent operator policy objects in policy order.</returns>
    SqlOperatorPolicy[] GetOperators();

    /// <summary>Returns the precedence of a unary operator kind.</summary>
    /// <param name="operatorKind">The unary operator kind.</param>
    /// <returns>The precedence, or zero when unsupported.</returns>
    int GetUnaryPrecedence(SqlUnaryOperatorKind operatorKind);

    /// <summary>Returns the precedence of a binary operator kind.</summary>
    /// <param name="operatorKind">The binary operator kind.</param>
    /// <returns>The precedence, or zero when unsupported.</returns>
    int GetBinaryPrecedence(SqlBinaryOperatorKind operatorKind);

    /// <summary>Returns the precedence of a caller-defined operator.</summary>
    /// <param name="text">The custom operator text.</param>
    /// <returns>The precedence, or zero when unsupported.</returns>
    int GetCustomOperatorPrecedence(string text);

    /// <summary>Returns supported identifier quote styles.</summary>
    /// <returns>An independent quote-style array.</returns>
    SqlNameQuoteStyle[] GetIdentifierQuoteStyles();

    /// <summary>Returns supported string quote styles.</summary>
    /// <returns>An independent quote-style array.</returns>
    SqlStringQuoteStyle[] GetStringQuoteStyles();

    /// <summary>Returns supported parameter placeholder styles.</summary>
    /// <returns>An independent parameter-style array.</returns>
    SqlParameterStyle[] GetParameterStyles();

    /// <summary>Returns supported literal syntax policies.</summary>
    /// <returns>An independent literal-policy array.</returns>
    SqlDialectLiteralPolicyKind[] GetLiteralPolicies();

    /// <summary>Returns whether a statement kind is enabled.</summary>
    /// <param name="kind">The statement kind.</param>
    /// <returns><c>true</c> when enabled; otherwise, <c>false</c>.</returns>
    bool SupportsStatement(SqlStatementKind kind);

    /// <summary>Returns whether a dialect-dependent clause is enabled.</summary>
    /// <param name="kind">The clause kind.</param>
    /// <returns><c>true</c> when enabled; otherwise, <c>false</c>.</returns>
    bool SupportsClause(SqlDialectClauseKind kind);
}
