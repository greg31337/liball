using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMboxEntry"/>.
/// </summary>
public interface IEmailMboxEntryContract
{
    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    int GetIndex();
    /// <summary>
    /// Gets the format.
    /// </summary>
    /// <returns>The current format.</returns>
    EmailMboxFormat GetFormat();
    /// <summary>
    /// Gets the archive range.
    /// </summary>
    /// <returns>The current archive range.</returns>
    EmailRange GetArchiveRange();
    /// <summary>
    /// Determines whether the envelope line is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope line is available; otherwise, <c>false</c>.</returns>
    bool HasEnvelopeLine();
    /// <summary>
    /// Gets the envelope line.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current envelope line.</returns>
    byte[]? GetEnvelopeLine();
    /// <summary>
    /// Determines whether the envelope line range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope line range is available; otherwise, <c>false</c>.</returns>
    bool HasEnvelopeLineRange();
    /// <summary>
    /// Gets the envelope line range.
    /// </summary>
    /// <returns>The current envelope line range.</returns>
    EmailRange? GetEnvelopeLineRange();
    /// <summary>
    /// Gets the message range.
    /// </summary>
    /// <returns>The current message range.</returns>
    EmailRange GetMessageRange();
    /// <summary>
    /// Determines whether the envelope sender text is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope sender text is available; otherwise, <c>false</c>.</returns>
    bool HasEnvelopeSenderText();
    /// <summary>
    /// Gets the envelope sender text.
    /// </summary>
    /// <returns>The current envelope sender text.</returns>
    string? GetEnvelopeSenderText();
    /// <summary>
    /// Determines whether the envelope date text is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope date text is available; otherwise, <c>false</c>.</returns>
    bool HasEnvelopeDateText();
    /// <summary>
    /// Gets the envelope date text.
    /// </summary>
    /// <returns>The current envelope date text.</returns>
    string? GetEnvelopeDateText();
    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    bool HasMessage();
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    EmailMessage? GetMessage();
    /// <summary>
    /// Gets the unescaped message byte count.
    /// </summary>
    /// <returns>The current unescaped message byte count.</returns>
    long GetUnescapedMessageByteCount();
    /// <summary>
    /// Gets the unescaped from line count.
    /// </summary>
    /// <returns>The current unescaped from line count.</returns>
    int GetUnescapedFromLineCount();
    /// <summary>
    /// Determines whether the operation or source value was recovered.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasRecovered();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
