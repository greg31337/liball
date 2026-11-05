using System;

namespace sql;

/// <summary>Represents a parsed SQL script as an ordered collection of statements and its combined source span.</summary>
public class SqlScript
{
    /// <summary>The statements in source or insertion order.</summary>
    public SqlStatement[] Statements;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The complete token stream in source order, including the end-of-file token when parsed.</summary>
    public SqlToken[] Tokens;
    /// <summary>The token range covered by this script.</summary>
    public SqlTokenRange TokenRange;
    /// <summary>The leading trivia before the first statement.</summary>
    public SqlTrivia[] LeadingTrivia;
    /// <summary>The trailing trivia after the last statement.</summary>
    public SqlTrivia[] TrailingTrivia;
    /// <summary>The exact parsed source text, or an empty string for a constructed script.</summary>
    public string SourceText;
    /// <summary>The unsupported syntax markers in source order.</summary>
    public SqlUnsupportedSyntax[] UnsupportedSyntax;

    /// <summary>Initializes a new instance of <see cref="SqlScript"/> from the supplied values.</summary>
    /// <returns>The resulting value.</returns>
    internal static SqlScript CreateEmpty()
    {
        return new SqlScript(new SqlStatement[0], new SqlTextSpan(0, 0));
    }

    /// <summary>Initializes a new instance of <see cref="SqlScript"/> from the supplied values.</summary>
    /// <param name="statements">The statements to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlScript(SqlStatement[] statements, SqlTextSpan span)
    {
        ValidateStatements(statements);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Statements = statements;
        Span = span;
        Tokens = new SqlToken[0];
        TokenRange = new SqlTokenRange(0, 0);
        LeadingTrivia = new SqlTrivia[0];
        TrailingTrivia = new SqlTrivia[0];
        SourceText = "";
        UnsupportedSyntax = new SqlUnsupportedSyntax[0];
    }

    /// <summary>Creates a script from statements and their combined source span.</summary>
    /// <param name="statements">The statements to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting script.</returns>
    internal static SqlScript Create(SqlStatement[] statements, SqlTextSpan span)
    {
        return new SqlScript(statements, span);
    }

    /// <summary>Returns the number of statement items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetStatementCount()
    {
        return Statements.Length;
    }

    /// <summary>Returns whether this value contains no elements or source characters.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return Statements.Length == 0;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting statement.</returns>
    internal SqlStatement GetStatementAt(int index)
    {
        return Statements[index];
    }

    internal static void ValidateStatements(SqlStatement[] statements)
    {
        if (statements == null)
        {
            throw new ArgumentNullException("statements");
        }

        int index = 0;
        while (index < statements.Length)
        {
            if (statements[index] == null)
            {
                throw new ArgumentNullException("statements");
            }

            index = index + 1;
        }
    }
}
