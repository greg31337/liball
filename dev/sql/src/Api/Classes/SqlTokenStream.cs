using System;

namespace sql;

/// <summary>Provides lookahead and sequential consumption over a fixed token sequence.</summary>
public class SqlTokenStream : ISqlTokenStream
{
    internal SqlToken[] Tokens;
    internal SqlDiagnosticBag Diagnostics;
    internal int Position;

    /// <summary>Initializes a token stream with a snapshot of the supplied tokens and a new diagnostic bag.</summary>
    /// <param name="tokens">The tokens in source order, ending with an end-of-file token.</param>
    /// <exception cref="ArgumentNullException">tokens is <c>null</c> or contains a <c>null</c> token.</exception>
    /// <exception cref="ArgumentException">tokens is empty or does not end with an end-of-file token.</exception>
    public SqlTokenStream(SqlToken[] tokens)
    {
        ValidateTokens(tokens);

        Tokens = CopyTokens(tokens);
        Diagnostics = new SqlDiagnosticBag();
        Position = 0;
    }

    /// <summary>Returns the zero-based index of the current token.</summary>
    /// <returns>The current token index.</returns>
    public int GetPosition()
    {
        return Position;
    }

    /// <summary>Returns whether the current token is the end-of-file token.</summary>
    /// <returns><c>true</c> at the end-of-file token; otherwise, <c>false</c>.</returns>
    public bool IsAtEnd()
    {
        return Peek().Kind == SqlTokenKind.EndOfFile;
    }

    /// <summary>Returns the current token without advancing.</summary>
    /// <returns>The current token.</returns>
    public SqlToken Peek()
    {
        return PeekAtOffset(0);
    }

    /// <summary>Returns a token at a nonnegative lookahead offset without advancing.</summary>
    /// <param name="offset">The nonnegative offset from the current token.</param>
    /// <returns>The token at the offset, clamped to the end-of-file token.</returns>
    /// <exception cref="ArgumentOutOfRangeException">offset is negative.</exception>
    public SqlToken LookAhead(int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        return PeekAtOffset(offset);
    }

    /// <summary>Returns the current token and advances when it is not the end-of-file token.</summary>
    /// <returns>The consumed token.</returns>
    public SqlToken Consume()
    {
        SqlToken token = Peek();

        if (Position < Tokens.Length - 1)
        {
            Position = Position + 1;
        }

        return token;
    }

    /// <summary>Consumes the current token only when it has the requested kind.</summary>
    /// <param name="kind">The token kind to match.</param>
    /// <returns><c>true</c> when a token was consumed; otherwise, <c>false</c>.</returns>
    public bool Match(SqlTokenKind kind)
    {
        if (Peek().Kind == kind)
        {
            Consume();
            return true;
        }

        return false;
    }

    /// <summary>Consumes and returns the expected token, or reports an error and consumes the unexpected token when possible.</summary>
    /// <param name="kind">The expected token kind.</param>
    /// <returns>The current token, whether expected or unexpected.</returns>
    public SqlToken Expect(SqlTokenKind kind)
    {
        SqlToken token = Peek();

        if (token.Kind == kind)
        {
            return Consume();
        }

        AddExpectedTokenDiagnostic(kind, token);
        if (token.Kind != SqlTokenKind.EndOfFile)
        {
            return Consume();
        }

        return token;
    }

    /// <summary>Returns the diagnostics produced by expectation failures.</summary>
    /// <returns>The diagnostic collection associated with the stream.</returns>
    public SqlDiagnosticBag GetDiagnostics()
    {
        return Diagnostics;
    }

    internal static SqlTokenStream Create(SqlToken[] tokens)
    {
        SqlTokenStream stream = new SqlTokenStream(tokens);
        stream.Tokens = tokens;
        return stream;
    }

    internal static SqlTokenStream Create(SqlToken[] tokens, SqlDiagnosticBag diagnostics)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        SqlTokenStream stream = SqlTokenStream.Create(tokens);
        stream.Diagnostics = diagnostics;
        return stream;
    }

    internal SqlToken PeekAtOffset(int offset)
    {
        long index = (long)Position + offset;

        if (index < 0)
        {
            return Tokens[0];
        }

        if (index >= Tokens.Length)
        {
            return Tokens[Tokens.Length - 1];
        }

        return Tokens[(int)index];
    }

    internal void AddExpectedTokenDiagnostic(SqlTokenKind kind, SqlToken token)
    {
        Diagnostics.AddDiagnosticWithDetails(
            SqlDiagnosticSeverity.Error,
            "SQLSTREAM001",
            "Expected token " + kind.ToString() + ".",
            token.Span,
            null);
    }

    internal static SqlToken[] CopyTokens(SqlToken[] tokens)
    {
        SqlToken[] copy = new SqlToken[tokens.Length];
        int index = 0;
        while (index < tokens.Length)
        {
            copy[index] = tokens[index];
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateTokens(SqlToken[] tokens)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (tokens.Length == 0)
        {
            throw new ArgumentException("tokens must not be empty");
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

        if (tokens[tokens.Length - 1].Kind != SqlTokenKind.EndOfFile)
        {
            throw new ArgumentException("tokens must end with an end-of-file token");
        }
    }
}
