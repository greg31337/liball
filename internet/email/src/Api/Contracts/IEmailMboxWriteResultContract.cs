using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMboxWriteResult"/>.
/// </summary>
public interface IEmailMboxWriteResultContract
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
    /// Determines whether the operation or source value was written.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasWritten();
    /// <summary>
    /// Determines whether the archive range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the archive range is available; otherwise, <c>false</c>.</returns>
    bool HasArchiveRange();
    /// <summary>
    /// Gets the archive range.
    /// </summary>
    /// <returns>The current archive range.</returns>
    EmailRange? GetArchiveRange();
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
    /// Determines whether the message range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message range is available; otherwise, <c>false</c>.</returns>
    bool HasMessageRange();
    /// <summary>
    /// Gets the message range.
    /// </summary>
    /// <returns>The current message range.</returns>
    EmailRange? GetMessageRange();
    /// <summary>
    /// Gets the envelope line.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current envelope line.</returns>
    byte[]? GetEnvelopeLine();
    /// <summary>
    /// Gets the message write result.
    /// </summary>
    /// <returns>The current message write result.</returns>
    EmailWriteResult GetMessageWriteResult();
    /// <summary>
    /// Gets the padding byte count.
    /// </summary>
    /// <returns>The current padding byte count.</returns>
    int GetPaddingByteCount();
    /// <summary>
    /// Gets the escaped from line count.
    /// </summary>
    /// <returns>The current escaped from line count.</returns>
    int GetEscapedFromLineCount();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
