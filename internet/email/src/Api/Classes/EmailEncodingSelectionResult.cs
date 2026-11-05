using System;

namespace email;
/// <summary>
/// Describes the outcome of a encoding selection operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailEncodingSelectionResult : IEmailEncodingSelectionResultContract
{
    EmailTransferEncodingKind encoding;
    EmailEncodingSelectionReason reason;
    long sourceByteCount;
    long textByteCount;
    long highBitByteCount;
    long nullByteCount;
    long controlByteCount;
    long maximumLineByteCount;
    bool canonicalLineBreaks;
    bool textualMediaType;
    bool mostlyText;
    /// <summary>
    /// Creates a new encoding selection result.
    /// </summary>
    /// <param name = "encoding">The encoding used by the operation.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "textByteCount">The number of items or bytes to process.</param>
    /// <param name = "highBitByteCount">The number of items or bytes to process.</param>
    /// <param name = "nullByteCount">The number of items or bytes to process.</param>
    /// <param name = "controlByteCount">The number of items or bytes to process.</param>
    /// <param name = "maximumLineByteCount">The number of items or bytes to process.</param>
    /// <param name = "canonicalLineBreaks">Whether to enable the canonical line breaks behavior.</param>
    /// <param name = "textualMediaType">Whether to enable the textual media type behavior.</param>
    /// <param name = "mostlyText">Whether to enable the mostly text behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailEncodingSelectionResult(EmailTransferEncodingKind encoding, EmailEncodingSelectionReason reason, long sourceByteCount, long textByteCount, long highBitByteCount, long nullByteCount, long controlByteCount, long maximumLineByteCount, bool canonicalLineBreaks, bool textualMediaType, bool mostlyText)
    {
        new EmailTransferEncoding().ValidateKind(encoding);
        if (reason < EmailEncodingSelectionReason.SevenBitSafe || reason > EmailEncodingSelectionReason.BinaryContent)
        {
            throw new ArgumentOutOfRangeException("reason");
        }

        if (sourceByteCount < 0 || textByteCount < 0 || highBitByteCount < 0 || nullByteCount < 0 || controlByteCount < 0 || maximumLineByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("sourceByteCount");
        }

        if (textByteCount > sourceByteCount || highBitByteCount > sourceByteCount || nullByteCount > sourceByteCount || controlByteCount > sourceByteCount)
        {
            throw new ArgumentException("Selection statistics cannot exceed the source byte count.", "sourceByteCount");
        }

        this.encoding = encoding;
        this.reason = reason;
        this.sourceByteCount = sourceByteCount;
        this.textByteCount = textByteCount;
        this.highBitByteCount = highBitByteCount;
        this.nullByteCount = nullByteCount;
        this.controlByteCount = controlByteCount;
        this.maximumLineByteCount = maximumLineByteCount;
        this.canonicalLineBreaks = canonicalLineBreaks;
        this.textualMediaType = textualMediaType;
        this.mostlyText = mostlyText;
    }

    /// <summary>
    /// Gets the encoding.
    /// </summary>
    /// <returns>The current encoding.</returns>
    public EmailTransferEncodingKind GetEncoding()
    {
        return encoding;
    }

    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    public EmailEncodingSelectionReason GetReason()
    {
        return reason;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Gets the text byte count.
    /// </summary>
    /// <returns>The current text byte count.</returns>
    public long GetTextByteCount()
    {
        return textByteCount;
    }

    /// <summary>
    /// Gets the high bit byte count.
    /// </summary>
    /// <returns>The current high bit byte count.</returns>
    public long GetHighBitByteCount()
    {
        return highBitByteCount;
    }

    /// <summary>
    /// Gets the null byte count.
    /// </summary>
    /// <returns>The current null byte count.</returns>
    public long GetNullByteCount()
    {
        return nullByteCount;
    }

    /// <summary>
    /// Gets the control byte count.
    /// </summary>
    /// <returns>The current control byte count.</returns>
    public long GetControlByteCount()
    {
        return controlByteCount;
    }

    /// <summary>
    /// Gets the maximum line byte count.
    /// </summary>
    /// <returns>The current maximum line byte count.</returns>
    public long GetMaximumLineByteCount()
    {
        return maximumLineByteCount;
    }

    /// <summary>
    /// Determines whether the canonical line breaks is present or available.
    /// </summary>
    /// <returns><c>true</c> when the canonical line breaks is available; otherwise, <c>false</c>.</returns>
    public bool HasCanonicalLineBreaks()
    {
        return canonicalLineBreaks;
    }

    /// <summary>
    /// Determines whether the current value or state is textual media type.
    /// </summary>
    /// <returns><c>true</c> when the value or state is textual media type; otherwise, <c>false</c>.</returns>
    public bool IsTextualMediaType()
    {
        return textualMediaType;
    }

    /// <summary>
    /// Determines whether the current value or state is mostly text.
    /// </summary>
    /// <returns><c>true</c> when the value or state is mostly text; otherwise, <c>false</c>.</returns>
    public bool IsMostlyText()
    {
        return mostlyText;
    }
}
