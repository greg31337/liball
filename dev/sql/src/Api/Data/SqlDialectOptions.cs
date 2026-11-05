using System;

namespace sql;

/// <summary>Controls dialect behavior.</summary>
public class SqlDialectOptions
{
    /// <summary>Initializes a generic, permissive set of dialect options that callers can customize before parsing.</summary>
    public SqlDialectOptions()
    {
        CustomKeywords = new string[0];
        CustomOperators = new SqlOperatorPolicy[0];
        SupportedStatements = CreateDefaultSupportedStatements();
    }

    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlDialectKind Kind = SqlDialectKind.Generic;
    internal SqlKeywordTable Keywords = SqlKeywordTable.CreateBase();
    /// <summary>The case insensitive keywords associated with this value.</summary>
    public bool CaseInsensitiveKeywords = true;

    /// <summary>Controls whether bare identifiers is accepted.</summary>
    public bool AllowBareIdentifiers = true;
    /// <summary>Controls whether double quoted identifiers is accepted.</summary>
    public bool AllowDoubleQuotedIdentifiers = true;
    /// <summary>Controls whether bracket quoted identifiers is accepted.</summary>
    public bool AllowBracketQuotedIdentifiers = false;
    /// <summary>Controls whether backtick quoted identifiers is accepted.</summary>
    public bool AllowBacktickQuotedIdentifiers = false;
    /// <summary>Controls whether dollar in bare identifiers is accepted.</summary>
    public bool AllowDollarInBareIdentifiers = false;

    /// <summary>Controls whether single quoted strings is accepted.</summary>
    public bool AllowSingleQuotedStrings = true;
    /// <summary>Controls whether double quoted strings is accepted.</summary>
    public bool AllowDoubleQuotedStrings = false;
    /// <summary>Controls whether escaped strings is accepted.</summary>
    public bool AllowEscapedStrings = false;
    /// <summary>Controls whether SQL Unicode escape strings are accepted.</summary>
    public bool AllowUnicodeStrings = false;
    /// <summary>Controls whether PostgreSQL dollar-quoted strings are accepted.</summary>
    public bool AllowDollarQuotedStrings = false;
    /// <summary>Controls whether blob literals is accepted.</summary>
    public bool AllowBlobLiterals = false;
    /// <summary>Controls whether hex integer literals is accepted.</summary>
    public bool AllowHexIntegerLiterals = false;
    /// <summary>Controls whether boolean literals is accepted.</summary>
    public bool AllowBooleanLiterals = true;

    /// <summary>Controls whether question parameters is accepted.</summary>
    public bool AllowQuestionParameters = true;
    /// <summary>Controls whether colon parameters is accepted.</summary>
    public bool AllowColonParameters = true;
    /// <summary>Controls whether at parameters is accepted.</summary>
    public bool AllowAtParameters = true;
    /// <summary>Controls whether dollar parameters is accepted.</summary>
    public bool AllowDollarParameters = true;
    /// <summary>Controls whether dollar-prefixed numbered parameters are accepted.</summary>
    public bool AllowDollarNumberParameters = false;

    /// <summary>Controls whether common table expressions is accepted.</summary>
    public bool AllowCommonTableExpressions = true;
    /// <summary>Controls whether window functions is accepted.</summary>
    public bool AllowWindowFunctions = true;
    /// <summary>Controls whether returning clause is accepted.</summary>
    public bool AllowReturningClause = false;
    /// <summary>Controls whether upsert clause is accepted.</summary>
    public bool AllowUpsertClause = false;

    /// <summary>Additional caller-defined keywords recognized by this dialect.</summary>
    public string[] CustomKeywords;
    /// <summary>Additional caller-defined symbolic binary operators recognized by this dialect.</summary>
    public SqlOperatorPolicy[] CustomOperators;
    /// <summary>The statement kinds enabled for this dialect.</summary>
    public SqlStatementKind[] SupportedStatements;

    /// <summary>Adds a keyword recognized by this dialect.</summary>
    /// <param name="keyword">The keyword to add.</param>
    /// <exception cref="ArgumentNullException">keyword is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">keyword is empty or whitespace.</exception>
    internal void AddKeyword(string keyword)
    {
        Keywords.Add(keyword);
    }

    /// <summary>Adds keywords recognized by this dialect.</summary>
    /// <param name="keywords">The keywords to add.</param>
    /// <exception cref="ArgumentNullException">keywords is <c>null</c> or contains a <c>null</c> value.</exception>
    /// <exception cref="ArgumentException">keywords contains an empty or whitespace value.</exception>
    internal void AddKeywords(string[] keywords)
    {
        Keywords.AddMany(keywords);
    }

    /// <summary>Returns whether keyword.</summary>
    /// <param name="text">The source text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsKeyword(string text)
    {
        string? canonical = GetCanonicalKeyword(text);
        return canonical != null;
    }

    /// <summary>Returns the canonical keyword associated with this value.</summary>
    /// <param name="text">The source text to process.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    internal string? GetCanonicalKeyword(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string? canonical = Keywords.GetCanonicalKeyword(text);
        if (canonical == null)
        {
            int index = 0;
            while (index < CustomKeywords.Length)
            {
                string keyword = CustomKeywords[index];
                if (string.Equals(keyword, text, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = keyword.ToUpperInvariant();
                    break;
                }

                index = index + 1;
            }
        }

        if (canonical == null)
        {
            return null;
        }

        if (CaseInsensitiveKeywords)
        {
            return canonical;
        }

        if (canonical == text)
        {
            return canonical;
        }

        return null;
    }

    /// <summary>Returns whether identifier quote allowed.</summary>
    /// <param name="quote">The quote to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsIdentifierQuoteAllowed(char quote)
    {
        if (quote == '"')
        {
            return AllowDoubleQuotedIdentifiers;
        }

        if (quote == '[' || quote == ']')
        {
            return AllowBracketQuotedIdentifiers;
        }

        if (quote == '`')
        {
            return AllowBacktickQuotedIdentifiers;
        }

        return false;
    }

    /// <summary>Returns whether parameter prefix allowed.</summary>
    /// <param name="prefix">The prefix to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsParameterPrefixAllowed(char prefix)
    {
        if (prefix == '?')
        {
            return AllowQuestionParameters;
        }

        if (prefix == ':')
        {
            return AllowColonParameters;
        }

        if (prefix == '@')
        {
            return AllowAtParameters;
        }

        if (prefix == '$')
        {
            return AllowDollarParameters || AllowDollarNumberParameters;
        }

        return false;
    }

    internal string[] GetAllKeywords()
    {
        SqlKeywordTable combined = SqlKeywordTable.Create(Keywords.GetKeywords());
        combined.AddMany(CustomKeywords);
        return combined.GetKeywords();
    }

    internal int GetCustomOperatorPrecedence(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < CustomOperators.Length)
        {
            if (CustomOperators[index].Text == text)
            {
                return CustomOperators[index].Precedence;
            }

            index = index + 1;
        }

        return 0;
    }

    internal bool IsCustomOperatorRightAssociative(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < CustomOperators.Length)
        {
            if (CustomOperators[index].Text == text)
            {
                return CustomOperators[index].IsRightAssociative;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool SupportsStatement(SqlStatementKind kind)
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

    internal void Validate()
    {
        if (CustomKeywords == null)
        {
            throw new ArgumentNullException("CustomKeywords");
        }

        if (CustomOperators == null)
        {
            throw new ArgumentNullException("CustomOperators");
        }

        if (SupportedStatements == null)
        {
            throw new ArgumentNullException("SupportedStatements");
        }

        int index = 0;
        while (index < CustomKeywords.Length)
        {
            SqlKeywordTable.ValidateKeyword(CustomKeywords[index]);
            index = index + 1;
        }

        index = 0;
        while (index < CustomOperators.Length)
        {
            if (CustomOperators[index] == null)
            {
                throw new ArgumentNullException("CustomOperators");
            }

            ValidateCustomOperator(CustomOperators[index]);
            index = index + 1;
        }
    }

    internal static void ValidateCustomOperator(SqlOperatorPolicy policy)
    {
        if (policy.Text == null)
        {
            throw new ArgumentNullException("CustomOperators");
        }

        if (policy.Text.Length == 0 || string.IsNullOrWhiteSpace(policy.Text))
        {
            throw new ArgumentException("custom operator text must not be empty or whitespace");
        }

        if (policy.Precedence < 1)
        {
            throw new ArgumentOutOfRangeException("CustomOperators");
        }

        if (policy.TokenKind != SqlTokenKind.CustomOperator)
        {
            throw new ArgumentException("custom operators must use the custom operator token kind");
        }

        if (policy.UnaryOperator != SqlUnaryOperatorKind.None || policy.BinaryOperator != SqlBinaryOperatorKind.None)
        {
            throw new ArgumentException("custom operators must use none for built-in operator kinds");
        }

        int index = 0;
        while (index < policy.Text.Length)
        {
            char value = policy.Text[index];
            if (char.IsLetterOrDigit(value) || value == '_' || char.IsWhiteSpace(value))
            {
                throw new ArgumentException("custom operator text must contain only symbols");
            }

            index = index + 1;
        }
    }

    internal static SqlStatementKind[] CreateDefaultSupportedStatements()
    {
        return new SqlStatementKind[]
        {
            SqlStatementKind.Empty,
            SqlStatementKind.Select,
            SqlStatementKind.Insert,
            SqlStatementKind.Update,
            SqlStatementKind.Delete,
            SqlStatementKind.CreateTable,
            SqlStatementKind.AlterTable,
            SqlStatementKind.DropTable,
            SqlStatementKind.CreateIndex,
            SqlStatementKind.DropIndex,
            SqlStatementKind.CreateView,
            SqlStatementKind.DropView,
            SqlStatementKind.CreateTrigger,
            SqlStatementKind.DropTrigger,
            SqlStatementKind.CreateVirtualTable,
            SqlStatementKind.Pragma,
            SqlStatementKind.Analyze,
            SqlStatementKind.Vacuum,
            SqlStatementKind.Attach,
            SqlStatementKind.Detach,
            SqlStatementKind.Reindex,
            SqlStatementKind.Begin,
            SqlStatementKind.Commit,
            SqlStatementKind.End,
            SqlStatementKind.Rollback,
            SqlStatementKind.Savepoint,
            SqlStatementKind.Release,
            SqlStatementKind.Explain
        };
    }

    internal void AddSupportedStatement(SqlStatementKind kind)
    {
        if (SupportsStatement(kind))
        {
            return;
        }

        SqlStatementKind[] values = new SqlStatementKind[SupportedStatements.Length + 1];
        int index = 0;
        while (index < SupportedStatements.Length)
        {
            values[index] = SupportedStatements[index];
            index = index + 1;
        }

        values[SupportedStatements.Length] = kind;
        SupportedStatements = values;
    }
}
