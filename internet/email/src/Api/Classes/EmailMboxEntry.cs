using System;

namespace email;
/// <summary>
/// Represents a mbox entry.
/// </summary>
public class EmailMboxEntry : IEmailMboxEntryContract
{
    int index;
    EmailMboxFormat format;
    EmailRange archiveRange;
    EmailRange? envelopeLineRange;
    EmailRange messageRange;
    byte[]? envelopeLine;
    string? envelopeSenderText;
    string? envelopeDateText;
    EmailMessage? message;
    long unescapedMessageByteCount;
    int unescapedFromLineCount;
    bool recovered;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new mbox entry.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "format">The format used by the operation.</param>
    /// <param name = "archiveRange">The archive range used by the operation.</param>
    /// <param name = "envelopeLineRange">The envelope line range used by the operation.</param>
    /// <param name = "messageRange">The message range used by the operation.</param>
    /// <param name = "envelopeLine">The envelope line used by the operation.</param>
    /// <param name = "envelopeSenderText">The envelope sender text used by the operation.</param>
    /// <param name = "envelopeDateText">The envelope date text used by the operation.</param>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "unescapedMessageByteCount">The number of items or bytes to process.</param>
    /// <param name = "unescapedFromLineCount">The number of items or bytes to process.</param>
    /// <param name = "recovered">Whether to enable the recovered behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMboxEntry(int index, EmailMboxFormat format, EmailRange archiveRange, EmailRange? envelopeLineRange, EmailRange messageRange, byte[]? envelopeLine, string? envelopeSenderText, string? envelopeDateText, EmailMessage? message, long unescapedMessageByteCount, int unescapedFromLineCount, bool recovered, EmailIssueList issues)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException("index");
        if (format < EmailMboxFormat.Mboxo || format > EmailMboxFormat.Mboxrd)
            throw new ArgumentOutOfRangeException("format");
        if (archiveRange == null || messageRange == null || issues == null)
            throw new ArgumentNullException("archiveRange");
        if (!archiveRange.ContainsRange(messageRange))
            throw new ArgumentOutOfRangeException("messageRange");
        if (envelopeLineRange != null && !archiveRange.ContainsRange(envelopeLineRange))
            throw new ArgumentOutOfRangeException("envelopeLineRange");
        if ((envelopeLine == null) != (envelopeLineRange == null))
            throw new ArgumentException("Envelope bytes and their range must either both be supplied or both be absent.", "envelopeLine");
        if (unescapedMessageByteCount < 0)
            throw new ArgumentOutOfRangeException("unescapedMessageByteCount");
        if (unescapedFromLineCount < 0)
            throw new ArgumentOutOfRangeException("unescapedFromLineCount");
        this.index = index;
        this.format = format;
        this.archiveRange = archiveRange;
        this.envelopeLineRange = envelopeLineRange;
        this.messageRange = messageRange;
        this.envelopeLine = CopyNullable(envelopeLine);
        this.envelopeSenderText = envelopeSenderText;
        this.envelopeDateText = envelopeDateText;
        this.message = message;
        this.unescapedMessageByteCount = unescapedMessageByteCount;
        this.unescapedFromLineCount = unescapedFromLineCount;
        this.recovered = recovered;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
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
    /// Gets the archive range.
    /// </summary>
    /// <returns>The current archive range.</returns>
    public EmailRange GetArchiveRange()
    {
        return archiveRange;
    }

    /// <summary>
    /// Determines whether the envelope line is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope line is available; otherwise, <c>false</c>.</returns>
    public bool HasEnvelopeLine()
    {
        return envelopeLine != null;
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
    /// Gets the message range.
    /// </summary>
    /// <returns>The current message range.</returns>
    public EmailRange GetMessageRange()
    {
        return messageRange;
    }

    /// <summary>
    /// Determines whether the envelope sender text is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope sender text is available; otherwise, <c>false</c>.</returns>
    public bool HasEnvelopeSenderText()
    {
        return envelopeSenderText != null;
    }

    /// <summary>
    /// Gets the envelope sender text.
    /// </summary>
    /// <returns>The current envelope sender text.</returns>
    public string? GetEnvelopeSenderText()
    {
        return envelopeSenderText;
    }

    /// <summary>
    /// Determines whether the envelope date text is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope date text is available; otherwise, <c>false</c>.</returns>
    public bool HasEnvelopeDateText()
    {
        return envelopeDateText != null;
    }

    /// <summary>
    /// Gets the envelope date text.
    /// </summary>
    /// <returns>The current envelope date text.</returns>
    public string? GetEnvelopeDateText()
    {
        return envelopeDateText;
    }

    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    public bool HasMessage()
    {
        return message != null;
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    public EmailMessage? GetMessage()
    {
        return message;
    }

    /// <summary>
    /// Gets the unescaped message byte count.
    /// </summary>
    /// <returns>The current unescaped message byte count.</returns>
    public long GetUnescapedMessageByteCount()
    {
        return unescapedMessageByteCount;
    }

    /// <summary>
    /// Gets the unescaped from line count.
    /// </summary>
    /// <returns>The current unescaped from line count.</returns>
    public int GetUnescapedFromLineCount()
    {
        return unescapedFromLineCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was recovered.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasRecovered()
    {
        return recovered;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return message != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
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
