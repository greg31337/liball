using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEnvelope"/>.
/// </summary>
public interface IEmailEnvelopeContract
{
    /// <summary>
    /// Determines whether the sender is present or available.
    /// </summary>
    /// <returns><c>true</c> when the sender is available; otherwise, <c>false</c>.</returns>
    bool HasSender();
    /// <summary>
    /// Gets the sender.
    /// </summary>
    /// <returns>The current sender.</returns>
    EmailAddress? GetSender();
    /// <summary>
    /// Sets the sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetSender(EmailAddress? value);
    /// <summary>
    /// Clears the stored sender value or values.
    /// </summary>
    void ClearSender();
    /// <summary>
    /// Gets the recipient count.
    /// </summary>
    /// <returns>The current recipient count.</returns>
    int GetRecipientCount();
    /// <summary>
    /// Gets the recipient at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested recipient.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAddress GetRecipientAt(int index);
    /// <summary>
    /// Gets the recipients.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipients.</returns>
    List<EmailAddress> GetRecipients();
    /// <summary>
    /// Adds a recipient to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddRecipient(EmailAddress? value);
    /// <summary>
    /// Inserts a recipient at the requested position.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void InsertRecipient(int index, EmailAddress value);
    /// <summary>
    /// Removes the matching recipient at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The address produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAddress RemoveRecipientAt(int index);
    /// <summary>
    /// Clears the stored recipients value or values.
    /// </summary>
    void ClearRecipients();
    /// <summary>
    /// Determines whether the mbox from line is present or available.
    /// </summary>
    /// <returns><c>true</c> when the mbox from line is available; otherwise, <c>false</c>.</returns>
    bool HasMboxFromLine();
    /// <summary>
    /// Gets the mbox from line.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current mbox from line.</returns>
    byte[]? GetMboxFromLine();
    /// <summary>
    /// Sets the mbox from line used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMboxFromLine(ReadOnlySpan<byte> value);
    /// <summary>
    /// Clears the stored mbox from line value or values.
    /// </summary>
    void ClearMboxFromLine();
    /// <summary>
    /// Determines whether the mbox from line range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the mbox from line range is available; otherwise, <c>false</c>.</returns>
    bool HasMboxFromLineRange();
    /// <summary>
    /// Gets the mbox from line range.
    /// </summary>
    /// <returns>The current mbox from line range.</returns>
    EmailRange? GetMboxFromLineRange();
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailEnvelope Clone();
    /// <summary>
    /// Detaches the source.
    /// </summary>
    void DetachSource();
}
