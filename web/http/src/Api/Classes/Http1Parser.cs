namespace http;

/// <summary>
/// Parses one HTTP/1 request or response incrementally from caller-provided bytes.
/// </summary>
public class Http1Parser : IHttp1Parser
{
    internal Http1ParserState state = new Http1ParserState();

    /// <summary>Creates a request-mode HTTP/1 parser with strict defaults.</summary>
    public Http1Parser()
    {
        state.Reset(new Http1ParserOptions());
    }

    /// <summary>
    /// Processes an input byte sequence and returns its parse status and emitted events.
    /// </summary>
    /// <param name="input">The bytes available to the parser.</param>
    /// <returns>The result of processing the available bytes.</returns>
    public Http1ParseResult ParseHttp1Bytes(byte[] input)
    {
        return state.Parse(input);
    }

    /// <summary>Signals end of input and reports whether the current message is complete.</summary>
    /// <returns>The final parse result and emitted events.</returns>
    public Http1ParseResult CompleteHttp1Input()
    {
        return state.CompleteInput();
    }

    /// <summary>Resets parser state for a new HTTP/1 message.</summary>
    /// <param name="options">The parser mode, compatibility policies, and limits.</param>
    public void ResetHttp1Parser(Http1ParserOptions options)
    {
        state.Reset(options);
    }
}
