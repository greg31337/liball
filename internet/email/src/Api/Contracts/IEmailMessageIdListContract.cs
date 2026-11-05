using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageIdList"/>.
/// </summary>
public interface IEmailMessageIdListContract
{
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    int GetCount();
    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailMessageId GetAt(int index);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    List<EmailMessageId> GetAll();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    bool UsesObsoleteSyntax();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The message ID format result produced by the operation.</returns>
    EmailMessageIdFormatResult Format();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    EmailMessageIdFormatResult FormatWithOptions(EmailMessageIdFormatOptions options);
}
