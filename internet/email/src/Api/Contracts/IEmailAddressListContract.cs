using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressList"/>.
/// </summary>
public interface IEmailAddressListContract
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
    EmailAddressListEntry GetAt(int index);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    List<EmailAddressListEntry> GetAll();
    /// <summary>
    /// Gets the mailbox count.
    /// </summary>
    /// <returns>The current mailbox count.</returns>
    int GetMailboxCount();
    /// <summary>
    /// Gets the group count.
    /// </summary>
    /// <returns>The current group count.</returns>
    int GetGroupCount();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    EmailAddressFormatResult Format(EmailAddressFormatOptions options);
}
