using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDeliveryStatus"/>.
/// </summary>
public interface IEmailDeliveryStatusContract
{
    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    bool IsInternationalized();
    /// <summary>
    /// Gets the message fields.
    /// </summary>
    /// <returns>The current message fields.</returns>
    EmailDeliveryStatusMessage? GetMessageFields();
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
    EmailDeliveryStatusRecipient GetRecipientAt(int index);
    /// <summary>
    /// Gets the recipients.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipients.</returns>
    List<EmailDeliveryStatusRecipient> GetRecipients();
    /// <summary>
    /// Determines whether the multipart report is present or available.
    /// </summary>
    /// <returns><c>true</c> when the multipart report is available; otherwise, <c>false</c>.</returns>
    bool HasMultipartReport();
    /// <summary>
    /// Gets the multipart report.
    /// </summary>
    /// <returns>The current multipart report.</returns>
    EmailMultipartReport? GetMultipartReport();
    /// <summary>
    /// Determines whether the original content part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original content part is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalContentPart();
    /// <summary>
    /// Gets the original content part.
    /// </summary>
    /// <returns>The current original content part.</returns>
    MimeEntity? GetOriginalContentPart();
    /// <summary>
    /// Determines whether the original headers is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original headers is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalHeaders();
    /// <summary>
    /// Gets the original headers.
    /// </summary>
    /// <returns>The current original headers.</returns>
    EmailHeaders? GetOriginalHeaders();
    /// <summary>
    /// Determines whether the original message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original message is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalMessage();
    /// <summary>
    /// Gets the original message.
    /// </summary>
    /// <returns>The current original message.</returns>
    EmailMessage? GetOriginalMessage();
}
