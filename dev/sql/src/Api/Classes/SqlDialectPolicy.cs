using System;

namespace sql;

/// <summary>Exposes an immutable snapshot of SQL dialect syntax policies.</summary>
public class SqlDialectPolicy : ISqlDialectPolicy
{
    internal SqlDialectKind Kind;
    internal bool CaseInsensitiveKeywords;
    internal string[] Keywords;
    internal SqlOperatorPolicy[] Operators;
    internal SqlNameQuoteStyle[] IdentifierQuoteStyles;
    internal SqlStringQuoteStyle[] StringQuoteStyles;
    internal SqlParameterStyle[] ParameterStyles;
    internal SqlDialectLiteralPolicyKind[] LiteralPolicies;
    internal SqlStatementKind[] SupportedStatements;
    internal bool AllowCommonTableExpressions;
    internal bool AllowWindowFunctions;
    internal bool AllowReturningClause;
    internal bool AllowUpsertClause;

    /// <summary>Initializes an immutable policy snapshot from dialect options.</summary>
    /// <param name="options">The dialect options to snapshot.</param>
    /// <exception cref="ArgumentNullException">options is <c>null</c> or contains a <c>null</c> customization array.</exception>
    public SqlDialectPolicy(SqlDialectOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        Kind = options.Kind;
        CaseInsensitiveKeywords = options.CaseInsensitiveKeywords;
        Keywords = options.GetAllKeywords();
        Operators = CreateOperators(options.CustomOperators);
        IdentifierQuoteStyles = CreateIdentifierQuoteStyles(options);
        StringQuoteStyles = CreateStringQuoteStyles(options);
        ParameterStyles = CreateParameterStyles(options);
        LiteralPolicies = CreateLiteralPolicies(options);
        SupportedStatements = CopyStatementKinds(options.SupportedStatements);
        AllowCommonTableExpressions = options.AllowCommonTableExpressions;
        AllowWindowFunctions = options.AllowWindowFunctions;
        AllowReturningClause = options.AllowReturningClause;
        AllowUpsertClause = options.AllowUpsertClause;
    }

    /// <summary>Returns the represented dialect kind.</summary>
    /// <returns>The dialect kind.</returns>
    public SqlDialectKind GetKind()
    {
        return Kind;
    }

    /// <summary>Returns canonical keywords in ordinal order.</summary>
    /// <returns>An independent keyword array.</returns>
    public string[] GetKeywords()
    {
        return CopyStrings(Keywords);
    }

    /// <summary>Returns whether text is a keyword under the snapshot's casing policy.</summary>
    /// <param name="text">The text to inspect.</param>
    /// <returns><c>true</c> when text is a keyword; otherwise, <c>false</c>.</returns>
    public bool IsKeyword(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < Keywords.Length)
        {
            if (CaseInsensitiveKeywords)
            {
                if (string.Equals(Keywords[index], text, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else
            {
                if (Keywords[index] == text)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Returns operator and precedence policies.</summary>
    /// <returns>Independent operator policy objects in policy order.</returns>
    public SqlOperatorPolicy[] GetOperators()
    {
        return CopyOperators(Operators);
    }

    /// <summary>Returns the precedence of a unary operator kind.</summary>
    /// <param name="operatorKind">The unary operator kind.</param>
    /// <returns>The precedence, or zero when unsupported.</returns>
    public int GetUnaryPrecedence(SqlUnaryOperatorKind operatorKind)
    {
        int index = 0;
        while (index < Operators.Length)
        {
            if (Operators[index].UnaryOperator == operatorKind && operatorKind != SqlUnaryOperatorKind.None)
            {
                return Operators[index].Precedence;
            }

            index = index + 1;
        }

        return 0;
    }

    /// <summary>Returns the precedence of a binary operator kind.</summary>
    /// <param name="operatorKind">The binary operator kind.</param>
    /// <returns>The precedence, or zero when unsupported.</returns>
    public int GetBinaryPrecedence(SqlBinaryOperatorKind operatorKind)
    {
        int index = 0;
        while (index < Operators.Length)
        {
            if (Operators[index].BinaryOperator == operatorKind && operatorKind != SqlBinaryOperatorKind.None)
            {
                return Operators[index].Precedence;
            }

            index = index + 1;
        }

        return 0;
    }

    /// <summary>Returns the precedence of a caller-defined operator.</summary>
    /// <param name="text">The custom operator text.</param>
    /// <returns>The precedence, or zero when unsupported.</returns>
    public int GetCustomOperatorPrecedence(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < Operators.Length)
        {
            if (Operators[index].TokenKind == SqlTokenKind.CustomOperator && Operators[index].Text == text)
            {
                return Operators[index].Precedence;
            }

            index = index + 1;
        }

        return 0;
    }

    /// <summary>Returns supported identifier quote styles.</summary>
    /// <returns>An independent quote-style array.</returns>
    public SqlNameQuoteStyle[] GetIdentifierQuoteStyles()
    {
        SqlNameQuoteStyle[] copy = new SqlNameQuoteStyle[IdentifierQuoteStyles.Length];
        Array.Copy(IdentifierQuoteStyles, copy, IdentifierQuoteStyles.Length);
        return copy;
    }

    /// <summary>Returns supported string quote styles.</summary>
    /// <returns>An independent quote-style array.</returns>
    public SqlStringQuoteStyle[] GetStringQuoteStyles()
    {
        SqlStringQuoteStyle[] copy = new SqlStringQuoteStyle[StringQuoteStyles.Length];
        Array.Copy(StringQuoteStyles, copy, StringQuoteStyles.Length);
        return copy;
    }

    /// <summary>Returns supported parameter placeholder styles.</summary>
    /// <returns>An independent parameter-style array.</returns>
    public SqlParameterStyle[] GetParameterStyles()
    {
        SqlParameterStyle[] copy = new SqlParameterStyle[ParameterStyles.Length];
        Array.Copy(ParameterStyles, copy, ParameterStyles.Length);
        return copy;
    }

    /// <summary>Returns supported literal syntax policies.</summary>
    /// <returns>An independent literal-policy array.</returns>
    public SqlDialectLiteralPolicyKind[] GetLiteralPolicies()
    {
        SqlDialectLiteralPolicyKind[] copy = new SqlDialectLiteralPolicyKind[LiteralPolicies.Length];
        Array.Copy(LiteralPolicies, copy, LiteralPolicies.Length);
        return copy;
    }

    /// <summary>Returns whether a statement kind is enabled.</summary>
    /// <param name="kind">The statement kind.</param>
    /// <returns><c>true</c> when enabled; otherwise, <c>false</c>.</returns>
    public bool SupportsStatement(SqlStatementKind kind)
    {
        int index = 0;
        while (index < SupportedStatements.Length)
        {
            if (SupportedStatements[index] == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Returns whether a dialect-dependent clause is enabled.</summary>
    /// <param name="kind">The clause kind.</param>
    /// <returns><c>true</c> when enabled; otherwise, <c>false</c>.</returns>
    public bool SupportsClause(SqlDialectClauseKind kind)
    {
        if (kind == SqlDialectClauseKind.CommonTableExpression)
        {
            return AllowCommonTableExpressions;
        }

        if (kind == SqlDialectClauseKind.Window)
        {
            return AllowWindowFunctions;
        }

        if (kind == SqlDialectClauseKind.Returning)
        {
            return AllowReturningClause;
        }

        if (kind == SqlDialectClauseKind.Upsert)
        {
            return AllowUpsertClause;
        }

        return false;
    }

    internal static SqlOperatorPolicy[] CreateOperators(SqlOperatorPolicy[] customOperators)
    {
        SqlOperatorPolicy[] builtIn = CreateBuiltInOperators();
        SqlOperatorPolicy[] result = new SqlOperatorPolicy[builtIn.Length + customOperators.Length];
        int index = 0;
        while (index < builtIn.Length)
        {
            result[index] = builtIn[index];
            index = index + 1;
        }

        int customIndex = 0;
        while (customIndex < customOperators.Length)
        {
            result[index] = CopyOperator(customOperators[customIndex]);
            index = index + 1;
            customIndex = customIndex + 1;
        }

        return result;
    }

    internal static SqlOperatorPolicy[] CreateBuiltInOperators()
    {
        return new SqlOperatorPolicy[]
        {
            new SqlOperatorPolicy("+", SqlTokenKind.Plus, SqlUnaryOperatorKind.Positive, SqlBinaryOperatorKind.None, 90, false),
            new SqlOperatorPolicy("-", SqlTokenKind.Minus, SqlUnaryOperatorKind.Negative, SqlBinaryOperatorKind.None, 90, false),
            new SqlOperatorPolicy("~", SqlTokenKind.Tilde, SqlUnaryOperatorKind.BitwiseNot, SqlBinaryOperatorKind.None, 90, false),
            new SqlOperatorPolicy("NOT", SqlTokenKind.Keyword, SqlUnaryOperatorKind.Not, SqlBinaryOperatorKind.None, 30, false),
            new SqlOperatorPolicy("||", SqlTokenKind.DoublePipe, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Concat, 80, false),
            new SqlOperatorPolicy("*", SqlTokenKind.Star, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Multiply, 70, false),
            new SqlOperatorPolicy("/", SqlTokenKind.Slash, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Divide, 70, false),
            new SqlOperatorPolicy("%", SqlTokenKind.Percent, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Modulo, 70, false),
            new SqlOperatorPolicy("+", SqlTokenKind.Plus, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Add, 60, false),
            new SqlOperatorPolicy("-", SqlTokenKind.Minus, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Subtract, 60, false),
            new SqlOperatorPolicy("&", SqlTokenKind.Ampersand, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.BitwiseAnd, 50, false),
            new SqlOperatorPolicy("|", SqlTokenKind.Pipe, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.BitwiseOr, 50, false),
            new SqlOperatorPolicy("^", SqlTokenKind.Caret, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.BitwiseXor, 50, false),
            new SqlOperatorPolicy("<<", SqlTokenKind.ShiftLeft, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.ShiftLeft, 50, false),
            new SqlOperatorPolicy(">>", SqlTokenKind.ShiftRight, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.ShiftRight, 50, false),
            new SqlOperatorPolicy("=", SqlTokenKind.Equal, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Equal, 40, false),
            new SqlOperatorPolicy("==", SqlTokenKind.DoubleEqual, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Equal, 40, false),
            new SqlOperatorPolicy("!=", SqlTokenKind.BangEqual, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.NotEqual, 40, false),
            new SqlOperatorPolicy("<>", SqlTokenKind.LessGreater, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.NotEqual, 40, false),
            new SqlOperatorPolicy("<", SqlTokenKind.Less, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Less, 40, false),
            new SqlOperatorPolicy("<=", SqlTokenKind.LessEqual, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.LessOrEqual, 40, false),
            new SqlOperatorPolicy(">", SqlTokenKind.Greater, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Greater, 40, false),
            new SqlOperatorPolicy(">=", SqlTokenKind.GreaterEqual, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.GreaterOrEqual, 40, false),
            new SqlOperatorPolicy("COLLATE", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 85, false),
            new SqlOperatorPolicy("IS", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("LIKE", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("GLOB", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("REGEXP", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("MATCH", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("BETWEEN", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("IN", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("AND", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.And, 20, false),
            new SqlOperatorPolicy("OR", SqlTokenKind.Keyword, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Or, 10, false)
        };
    }

    internal static SqlNameQuoteStyle[] CreateIdentifierQuoteStyles(SqlDialectOptions options)
    {
        int count = 0;
        if (options.AllowDoubleQuotedIdentifiers)
        {
            count = count + 1;
        }

        if (options.AllowBracketQuotedIdentifiers)
        {
            count = count + 1;
        }

        if (options.AllowBacktickQuotedIdentifiers)
        {
            count = count + 1;
        }

        SqlNameQuoteStyle[] result = new SqlNameQuoteStyle[count];
        int index = 0;
        if (options.AllowDoubleQuotedIdentifiers)
        {
            result[index] = SqlNameQuoteStyle.DoubleQuote;
            index = index + 1;
        }

        if (options.AllowBracketQuotedIdentifiers)
        {
            result[index] = SqlNameQuoteStyle.Bracket;
            index = index + 1;
        }

        if (options.AllowBacktickQuotedIdentifiers)
        {
            result[index] = SqlNameQuoteStyle.Backtick;
        }

        return result;
    }

    internal static SqlStringQuoteStyle[] CreateStringQuoteStyles(SqlDialectOptions options)
    {
        int count = 0;
        if (options.AllowSingleQuotedStrings)
        {
            count = count + 1;
        }

        if (options.AllowDoubleQuotedStrings)
        {
            count = count + 1;
        }

        if (options.AllowEscapedStrings)
        {
            count = count + 1;
        }

        if (options.AllowDollarQuotedStrings)
        {
            count = count + 1;
        }

        if (options.AllowUnicodeStrings)
        {
            count = count + 1;
        }

        SqlStringQuoteStyle[] result = new SqlStringQuoteStyle[count];
        int index = 0;
        if (options.AllowSingleQuotedStrings)
        {
            result[index] = SqlStringQuoteStyle.SingleQuote;
            index = index + 1;
        }

        if (options.AllowDoubleQuotedStrings)
        {
            result[index] = SqlStringQuoteStyle.DoubleQuote;
            index = index + 1;
        }

        if (options.AllowEscapedStrings)
        {
            result[index] = SqlStringQuoteStyle.EscapedSingleQuote;
            index = index + 1;
        }

        if (options.AllowDollarQuotedStrings)
        {
            result[index] = SqlStringQuoteStyle.DollarQuote;
            index = index + 1;
        }

        if (options.AllowUnicodeStrings)
        {
            result[index] = SqlStringQuoteStyle.UnicodeSingleQuote;
        }

        return result;
    }

    internal static SqlParameterStyle[] CreateParameterStyles(SqlDialectOptions options)
    {
        int count = 0;
        if (options.AllowQuestionParameters)
        {
            count = count + 2;
        }

        if (options.AllowColonParameters)
        {
            count = count + 1;
        }

        if (options.AllowAtParameters)
        {
            count = count + 1;
        }

        if (options.AllowDollarParameters)
        {
            count = count + 1;
        }

        if (options.AllowDollarNumberParameters)
        {
            count = count + 1;
        }

        SqlParameterStyle[] result = new SqlParameterStyle[count];
        int index = 0;
        if (options.AllowQuestionParameters)
        {
            result[index] = SqlParameterStyle.Question;
            result[index + 1] = SqlParameterStyle.NumberedQuestion;
            index = index + 2;
        }

        if (options.AllowColonParameters)
        {
            result[index] = SqlParameterStyle.ColonName;
            index = index + 1;
        }

        if (options.AllowAtParameters)
        {
            result[index] = SqlParameterStyle.AtName;
            index = index + 1;
        }

        if (options.AllowDollarParameters)
        {
            result[index] = SqlParameterStyle.DollarName;
            index = index + 1;
        }

        if (options.AllowDollarNumberParameters)
        {
            result[index] = SqlParameterStyle.DollarNumber;
        }

        return result;
    }

    internal static SqlDialectLiteralPolicyKind[] CreateLiteralPolicies(SqlDialectOptions options)
    {
        int count = 7;
        if (options.AllowSingleQuotedStrings || options.AllowDoubleQuotedStrings || options.AllowEscapedStrings || options.AllowUnicodeStrings || options.AllowDollarQuotedStrings)
        {
            count = count + 1;
        }

        if (options.AllowBlobLiterals)
        {
            count = count + 1;
        }

        if (options.AllowHexIntegerLiterals)
        {
            count = count + 1;
        }

        if (options.AllowBooleanLiterals)
        {
            count = count + 1;
        }

        SqlDialectLiteralPolicyKind[] result = new SqlDialectLiteralPolicyKind[count];
        result[0] = SqlDialectLiteralPolicyKind.Null;
        result[1] = SqlDialectLiteralPolicyKind.Integer;
        result[2] = SqlDialectLiteralPolicyKind.Decimal;
        result[3] = SqlDialectLiteralPolicyKind.Exponent;
        result[4] = SqlDialectLiteralPolicyKind.CurrentDate;
        result[5] = SqlDialectLiteralPolicyKind.CurrentTime;
        result[6] = SqlDialectLiteralPolicyKind.CurrentTimestamp;
        int index = 7;
        if (options.AllowSingleQuotedStrings || options.AllowDoubleQuotedStrings || options.AllowEscapedStrings || options.AllowUnicodeStrings || options.AllowDollarQuotedStrings)
        {
            result[index] = SqlDialectLiteralPolicyKind.String;
            index = index + 1;
        }

        if (options.AllowBlobLiterals)
        {
            result[index] = SqlDialectLiteralPolicyKind.Blob;
            index = index + 1;
        }

        if (options.AllowHexIntegerLiterals)
        {
            result[index] = SqlDialectLiteralPolicyKind.HexInteger;
            index = index + 1;
        }

        if (options.AllowBooleanLiterals)
        {
            result[index] = SqlDialectLiteralPolicyKind.Boolean;
        }

        return result;
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static SqlStatementKind[] CopyStatementKinds(SqlStatementKind[] values)
    {
        SqlStatementKind[] copy = new SqlStatementKind[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static SqlOperatorPolicy CopyOperator(SqlOperatorPolicy value)
    {
        return new SqlOperatorPolicy(value.Text, value.TokenKind, value.UnaryOperator, value.BinaryOperator, value.Precedence, value.IsRightAssociative);
    }

    internal static SqlOperatorPolicy[] CopyOperators(SqlOperatorPolicy[] values)
    {
        SqlOperatorPolicy[] copy = new SqlOperatorPolicy[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = CopyOperator(values[index]);
            index = index + 1;
        }

        return copy;
    }
}
