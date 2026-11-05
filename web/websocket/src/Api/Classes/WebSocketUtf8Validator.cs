using System;

namespace websocket;

/// <summary>
/// Validates UTF-8 incrementally using the strict RFC 3629 scalar-value range.
/// </summary>
public class WebSocketUtf8Validator : IWebSocketUtf8Validator
{
    internal Utf8State State;

    /// <summary>
    /// Initializes a validator for an empty UTF-8 stream.
    /// </summary>
    public WebSocketUtf8Validator()
    {
        State = new Utf8State();
    }

    /// <summary>
    /// Resets validation state for a new byte stream.
    /// </summary>
    public void ResetValidation()
    {
        State.Reset();
    }

    /// <summary>
    /// Validates the next chunk of a UTF-8 byte stream.
    /// </summary>
    /// <param name="source">The next bytes in stream order.</param>
    /// <returns>True when the stream remains valid so far; otherwise, false.</returns>
    public bool ValidateChunk(ReadOnlySpan<byte> source)
    {
        return State.Validate(source);
    }

    /// <summary>
    /// Completes the current stream and checks for an unfinished sequence.
    /// </summary>
    /// <returns>True when the complete stream is valid UTF-8; otherwise, false.</returns>
    public bool CompleteValidation()
    {
        return State.Complete();
    }
}
