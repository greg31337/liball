using System;

namespace email;
/// <summary>
/// Describes the outcome of a mbox write operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailMboxWriteResult : IEmailMboxWriteResultContract
{
    int index;
    EmailMboxFormat format;
    EmailRange? archiveRange;
    EmailRange? envelopeLineRange;
    EmailRange? messageRange;
    byte[]? envelopeLine;
    EmailWriteResult messageWriteResult;
    int paddingByteCount;
    int escapedFromLineCount;
    bool written;
    /// <summary>
    /// Creates a new mbox write result.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "format">The format used by the operation.</param>
    /// <param name = "archiveRange">The archive range used by the operation.</param>
    /// <param name = "envelopeLineRange">The envelope line range used by the operation.</param>
    /// <param name = "messageRange">The message range used by the operation.</param>
    /// <param name = "envelopeLine">The envelope line used by the operation.</param>
    /// <param name = "messageWriteResult">The message write result used by the operation.</param>
    /// <param name = "paddingByteCount">The number of items or bytes to process.</param>
    /// <param name = "escapedFromLineCount">The number of items or bytes to process.</param>
    /// <param name = "written">Whether to enable the written behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMboxWriteResult(int index, EmailMboxFormat format, EmailRange? archiveRange, EmailRange? envelopeLineRange, EmailRange? messageRange, byte[]? envelopeLine, EmailWriteResult messageWriteResult, int paddingByteCount, int escapedFromLineCount, bool written)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException("index");
        if (format < EmailMboxFormat.Mboxo || format > EmailMboxFormat.Mboxrd)
            throw new ArgumentOutOfRangeException("format");
        if (messageWriteResult == null)
            throw new ArgumentNullException("messageWriteResult");
        if (paddingByteCount < 0)
            throw new ArgumentOutOfRangeException("paddingByteCount");
        if (escapedFromLineCount < 0)
            throw new ArgumentOutOfRangeException("escapedFromLineCount");
        if (written && (archiveRange == null || envelopeLineRange == null || messageRange == null || envelopeLine == null))
            throw new ArgumentException("Written mbox results require output ranges and an envelope line.", "archiveRange");
        this.index = index;
        this.format = format;
        this.archiveRange = archiveRange;
        this.envelopeLineRange = envelopeLineRange;
        this.messageRange = messageRange;
        this.envelopeLine = CopyNullable(envelopeLine);
        this.messageWriteResult = messageWriteResult;
        this.paddingByteCount = paddingByteCount;
        this.escapedFromLineCount = escapedFromLineCount;
        this.written = written;
    }

    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>
    /// Gets the format.
    /// </summary>
    /// <returns>The current format.</returns>
    public EmailMboxFormat GetFormat()
    {
        return format;
    }

    /// <summary>
    /// Determines whether the operation or source value was written.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasWritten()
    {
        return written;
    }

    /// <summary>
    /// Determines whether the archive range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the archive range is available; otherwise, <c>false</c>.</returns>
    public bool HasArchiveRange()
    {
        return archiveRange != null;
    }

    /// <summary>
    /// Gets the archive range.
    /// </summary>
    /// <returns>The current archive range.</returns>
    public EmailRange? GetArchiveRange()
    {
        return archiveRange;
    }

    /// <summary>
    /// Determines whether the envelope line range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope line range is available; otherwise, <c>false</c>.</returns>
    public bool HasEnvelopeLineRange()
    {
        return envelopeLineRange != null;
    }

    /// <summary>
    /// Gets the envelope line range.
    /// </summary>
    /// <returns>The current envelope line range.</returns>
    public EmailRange? GetEnvelopeLineRange()
    {
        return envelopeLineRange;
    }

    /// <summary>
    /// Determines whether the message range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message range is available; otherwise, <c>false</c>.</returns>
    public bool HasMessageRange()
    {
        return messageRange != null;
    }

    /// <summary>
    /// Gets the message range.
    /// </summary>
    /// <returns>The current message range.</returns>
    public EmailRange? GetMessageRange()
    {
        return messageRange;
    }

    /// <summary>
    /// Gets the envelope line.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current envelope line.</returns>
    public byte[]? GetEnvelopeLine()
    {
        return CopyNullable(envelopeLine);
    }

    /// <summary>
    /// Gets the message write result.
    /// </summary>
    /// <returns>The current message write result.</returns>
    public EmailWriteResult GetMessageWriteResult()
    {
        return messageWriteResult;
    }

    /// <summary>
    /// Gets the padding byte count.
    /// </summary>
    /// <returns>The current padding byte count.</returns>
    public int GetPaddingByteCount()
    {
        return paddingByteCount;
    }

    /// <summary>
    /// Gets the escaped from line count.
    /// </summary>
    /// <returns>The current escaped from line count.</returns>
    public int GetEscapedFromLineCount()
    {
        return escapedFromLineCount;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return written && messageWriteResult.IsSuccess();
    }

    static byte[]? CopyNullable(byte[]? value)
    {
        if (value == null)
            return null;
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
