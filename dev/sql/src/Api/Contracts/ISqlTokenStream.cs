namespace sql;

/// <summary>Provides sequential token inspection and consumption without exposing parser state.</summary>
public interface ISqlTokenStream
{
    /// <summary>Returns the zero-based index of the current token.</summary>
    /// <returns>The current token index.</returns>
    int GetPosition();

    /// <summary>Returns whether the current token is the end-of-file token.</summary>
    /// <returns><c>true</c> at the end-of-file token; otherwise, <c>false</c>.</returns>
    bool IsAtEnd();

    /// <summary>Returns the current token without advancing.</summary>
    /// <returns>The current token.</returns>
    SqlToken Peek();

    /// <summary>Returns a token at a nonnegative lookahead offset without advancing.</summary>
    /// <param name="offset">The nonnegative offset from the current token.</param>
    /// <returns>The token at the offset, clamped to the end-of-file token.</returns>
    SqlToken LookAhead(int offset);

    /// <summary>Returns the current token and advances when it is not the end-of-file token.</summary>
    /// <returns>The consumed token.</returns>
    SqlToken Consume();

    /// <summary>Consumes the current token only when it has the requested kind.</summary>
    /// <param name="kind">The token kind to match.</param>
    /// <returns><c>true</c> when a token was consumed; otherwise, <c>false</c>.</returns>
    bool Match(SqlTokenKind kind);

    /// <summary>Consumes and returns the expected token, or reports an error and consumes the unexpected token when possible.</summary>
    /// <param name="kind">The expected token kind.</param>
    /// <returns>The current token, whether expected or unexpected.</returns>
    SqlToken Expect(SqlTokenKind kind);

    /// <summary>Returns the diagnostics produced by expectation failures.</summary>
    /// <returns>The diagnostic collection associated with the stream.</returns>
    SqlDiagnosticBag GetDiagnostics();
}
