using System;

namespace websocket;

/// <summary>
/// Defines strict incremental UTF-8 validation operations.
/// </summary>
public interface IWebSocketUtf8Validator
{
    /// <summary>
    /// Resets validation state for a new byte stream.
    /// </summary>
    void ResetValidation();

    /// <summary>
    /// Validates the next chunk of a UTF-8 byte stream.
    /// </summary>
    /// <param name="source">The next bytes in stream order.</param>
    /// <returns>True when the stream remains valid so far; otherwise, false.</returns>
    bool ValidateChunk(ReadOnlySpan<byte> source);

    /// <summary>
    /// Completes the current stream and checks for an unfinished sequence.
    /// </summary>
    /// <returns>True when the complete stream is valid UTF-8; otherwise, false.</returns>
    bool CompleteValidation();
}
