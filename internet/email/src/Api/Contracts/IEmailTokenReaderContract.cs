using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTokenReader"/>.
/// </summary>
public interface IEmailTokenReaderContract
{
    /// <summary>
    /// Starts the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    void Start(byte[] input);
    /// <summary>
    /// Starts the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    void StartMemory(ReadOnlyMemory<byte> input);
    /// <summary>
    /// Starts the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    void StartStream(Stream input);
    /// <summary>
    /// Reads the next value from the input.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool Read();
    /// <summary>
    /// Gets the token kind.
    /// </summary>
    /// <returns>The current token kind.</returns>
    EmailTokenKind GetTokenKind();
    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <returns>The current headers.</returns>
    EmailHeaders? GetHeaders();
    /// <summary>
    /// Opens a body chunk for caller-controlled reading or access.
    /// </summary>
    /// <returns>The stream produced by the operation.</returns>
    /// <exception cref = "EndOfStreamException">Thrown when the input ends before the required data is available.</exception>
    Stream OpenBodyChunk();
    /// <summary>
    /// Gets the body chunk byte count.
    /// </summary>
    /// <returns>The current body chunk byte count.</returns>
    int GetBodyChunkByteCount();
    /// <summary>
    /// Gets the body chunk kind.
    /// </summary>
    /// <returns>The current body chunk kind.</returns>
    EmailBodyChunkKind GetBodyChunkKind();
    /// <summary>
    /// Gets the issue.
    /// </summary>
    /// <returns>The current issue.</returns>
    EmailIssue? GetIssue();
    /// <summary>
    /// Skips the body.
    /// </summary>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    void SkipBody();
    /// <summary>
    /// Gets the depth.
    /// </summary>
    /// <returns>The current depth.</returns>
    int GetDepth();
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    EmailPath GetPath();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    long GetConsumedByteCount();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
}
