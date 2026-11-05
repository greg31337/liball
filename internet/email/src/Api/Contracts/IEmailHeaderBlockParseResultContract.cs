using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderBlockParseResult"/>.
/// </summary>
public interface IEmailHeaderBlockParseResultContract
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailHeaderBlockParseStatus GetStatus();
    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <returns>The current headers.</returns>
    EmailHeaders GetHeaders();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    int GetConsumedByteCount();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    int GetSourceByteCount();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the current state needs more data.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool NeedsMoreData();
    /// <summary>
    /// Determines whether processing reached end of input.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ReachedEndOfInput();
    /// <summary>
    /// Determines whether the operation or source value was recovered.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasRecovered();
    /// <summary>
    /// Determines whether the operation or source value was limit reached.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasLimitReached();
    /// <summary>
    /// Determines whether the separator is present or available.
    /// </summary>
    /// <returns><c>true</c> when the separator is available; otherwise, <c>false</c>.</returns>
    bool HasSeparator();
    /// <summary>
    /// Gets the separator line ending.
    /// </summary>
    /// <returns>The current separator line ending.</returns>
    EmailLineEnding GetSeparatorLineEnding();
    /// <summary>
    /// Determines whether the body start offset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body start offset is available; otherwise, <c>false</c>.</returns>
    bool HasBodyStartOffset();
    /// <summary>
    /// Gets the body start offset.
    /// </summary>
    /// <returns>The current body start offset.</returns>
    int GetBodyStartOffset();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    bool HasFatalError();
}
