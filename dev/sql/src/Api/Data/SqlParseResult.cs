using System;

namespace sql;

/// <summary>Contains the parse output and any diagnostics produced by the operation.</summary>
/// <remarks>The token array is in source order and includes an end-of-file token. A syntax tree may still be available when errors are present because recovery creates best-effort nodes.</remarks>
public class SqlParseResult
{
    /// <summary>The script associated with this value.</summary>
    public SqlScript Script;
    /// <summary>The tokens in source or insertion order.</summary>
    public SqlToken[] Tokens;
    /// <summary>The diagnostics in reporting order.</summary>
    public SqlDiagnosticBag Diagnostics;
    /// <summary>The original source text used to produce this value.</summary>
    public SqlSourceText? SourceText;
    /// <summary>The options used to produce this value.</summary>
    public SqlParseOptions Options;

    /// <summary>Initializes a parse result without source or custom option metadata.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <param name="diagnostics">The diagnostic collection produced during processing.</param>
    public SqlParseResult(SqlScript script, SqlToken[] tokens, SqlDiagnosticBag diagnostics)
    {
        ValidateScript(script);
        ValidateTokens(tokens);
        ValidateDiagnostics(diagnostics);

        Script = script;
        Tokens = tokens;
        Diagnostics = diagnostics;
        SourceText = null;
        Options = new SqlParseOptions();
    }

    /// <summary>Creates a parse result without source or custom option metadata.</summary>
    /// <param name="script">The script syntax tree to use.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <param name="diagnostics">The diagnostics produced during parsing.</param>
    /// <returns>The resulting parse result.</returns>
    internal static SqlParseResult Create(SqlScript script, SqlToken[] tokens, SqlDiagnosticBag diagnostics)
    {
        return new SqlParseResult(script, tokens, diagnostics);
    }

    /// <summary>Creates a parse result with source and option metadata.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <param name="diagnostics">The diagnostic collection produced during processing.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    /// <returns>The resulting parse result.</returns>
    internal static SqlParseResult Create(SqlScript script, SqlToken[] tokens, SqlDiagnosticBag diagnostics, SqlSourceText? sourceText, SqlParseOptions options)
    {
        SqlParseResult result = new SqlParseResult(script, tokens, diagnostics);
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();

        result.SourceText = sourceText;
        result.Options = options;
        return result;
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

    /// <summary>Returns the number of statement items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetStatementCount()
    {
        return Script.GetStatementCount();
    }

    /// <summary>Returns whether source text is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSourceText()
    {
        return SourceText != null;
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

    internal static void ValidateScript(SqlScript script)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }
    }

    internal static void ValidateDiagnostics(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }
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
}
