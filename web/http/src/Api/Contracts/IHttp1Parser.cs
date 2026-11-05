namespace http;

/// <summary>
/// Parses HTTP/1 input incrementally.
/// </summary>
public interface IHttp1Parser
{
    /// <summary>
    /// Processes an input byte sequence and returns its parse status and emitted events.
    /// </summary>
    /// <param name="input">The bytes available to the parser.</param>
    /// <returns>The result of processing the available bytes.</returns>
    Http1ParseResult ParseHttp1Bytes(byte[] input);

    /// <summary>Signals end of input and completes close-delimited content or reports truncation.</summary>
    /// <returns>The final parse result and emitted events.</returns>
    Http1ParseResult CompleteHttp1Input();

    /// <summary>Resets parser state for a new HTTP/1 message sequence.</summary>
    /// <param name="options">The parser mode, context, compatibility policies, and limits.</param>
    void ResetHttp1Parser(Http1ParserOptions options);
}
