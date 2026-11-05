using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodingSelectionResult"/>.
/// </summary>
public interface IEmailEncodingSelectionResultContract
{
    /// <summary>
    /// Gets the encoding.
    /// </summary>
    /// <returns>The current encoding.</returns>
    EmailTransferEncodingKind GetEncoding();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    EmailEncodingSelectionReason GetReason();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Gets the text byte count.
    /// </summary>
    /// <returns>The current text byte count.</returns>
    long GetTextByteCount();
    /// <summary>
    /// Gets the high bit byte count.
    /// </summary>
    /// <returns>The current high bit byte count.</returns>
    long GetHighBitByteCount();
    /// <summary>
    /// Gets the null byte count.
    /// </summary>
    /// <returns>The current null byte count.</returns>
    long GetNullByteCount();
    /// <summary>
    /// Gets the control byte count.
    /// </summary>
    /// <returns>The current control byte count.</returns>
    long GetControlByteCount();
    /// <summary>
    /// Gets the maximum line byte count.
    /// </summary>
    /// <returns>The current maximum line byte count.</returns>
    long GetMaximumLineByteCount();
    /// <summary>
    /// Determines whether the canonical line breaks is present or available.
    /// </summary>
    /// <returns><c>true</c> when the canonical line breaks is available; otherwise, <c>false</c>.</returns>
    bool HasCanonicalLineBreaks();
    /// <summary>
    /// Determines whether the current value or state is textual media type.
    /// </summary>
    /// <returns><c>true</c> when the value or state is textual media type; otherwise, <c>false</c>.</returns>
    bool IsTextualMediaType();
    /// <summary>
    /// Determines whether the current value or state is mostly text.
    /// </summary>
    /// <returns><c>true</c> when the value or state is mostly text; otherwise, <c>false</c>.</returns>
    bool IsMostlyText();
}
