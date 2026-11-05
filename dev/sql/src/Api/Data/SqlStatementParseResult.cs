using System;

namespace sql;

/// <summary>Contains the statement parse output and any diagnostics produced by the operation.</summary>
/// <remarks>The statement is a best-effort tree when diagnostics contain errors. Tokens remain in source order and include an end-of-file token.</remarks>
public class SqlStatementParseResult
{
    /// <summary>The statement associated with this value.</summary>
    public SqlStatement Statement;
    /// <summary>The tokens in source or insertion order.</summary>
    public SqlToken[] Tokens;
    /// <summary>The diagnostics in reporting order.</summary>
    public SqlDiagnosticBag Diagnostics;
    /// <summary>The original source text used to produce this value.</summary>
    public SqlSourceText SourceText;
    /// <summary>The options used to produce this value.</summary>
    public SqlParseOptions Options;

    /// <summary>Initializes a new instance of <see cref="SqlStatementParseResult"/> from the supplied values.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <param name="diagnostics">The diagnostic collection produced during processing.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <exception cref="ArgumentNullException">statement is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">sourceText is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    public SqlStatementParseResult(SqlStatement statement, SqlToken[] tokens, SqlDiagnosticBag diagnostics, SqlSourceText sourceText, SqlParseOptions options)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        ValidateTokens(tokens);
        ValidateDiagnostics(diagnostics);
        if (sourceText == null)
        {
            throw new ArgumentNullException("sourceText");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();

        Statement = statement;
        Tokens = tokens;
        Diagnostics = diagnostics;
        SourceText = sourceText;
        Options = options;
    }

    /// <summary>Returns whether processing completed without error diagnostics.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSuccess()
    {
        return !Diagnostics.HasErrors();
    }

    /// <summary>Returns whether diagnostics is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDiagnostics()
    {
        return !Diagnostics.IsEmpty();
    }

    /// <summary>Returns whether errors is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasErrors()
    {
        return Diagnostics.HasErrors();
    }

    /// <summary>Returns the number of diagnostic items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetDiagnosticCount()
    {
        return Diagnostics.GetCount();
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting diagnostic.</returns>
    internal SqlDiagnostic GetDiagnosticAt(int index)
    {
        return Diagnostics.GetAt(index);
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

    /// <summary>Creates a cursor over the result tokens that reports expectation failures into the same diagnostic bag.</summary>
    /// <returns>The resulting token stream.</returns>
    internal SqlTokenStream CreateTokenStream()
    {
        if (Tokens.Length == 0)
        {
            throw new ArgumentException("tokens must not be empty");
        }

        return SqlTokenStream.Create(Tokens, Diagnostics);
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

    internal static void ValidateDiagnostics(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }
    }
}
