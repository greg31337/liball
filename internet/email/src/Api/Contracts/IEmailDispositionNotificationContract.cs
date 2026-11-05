using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotification"/>.
/// </summary>
public interface IEmailDispositionNotificationContract
{
    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    bool IsInternationalized();
    /// <summary>
    /// Gets the field block.
    /// </summary>
    /// <returns>The current field block.</returns>
    EmailReportFieldBlock? GetFieldBlock();
    /// <summary>
    /// Determines whether the reporting user agent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reporting user agent is available; otherwise, <c>false</c>.</returns>
    bool HasReportingUserAgent();
    /// <summary>
    /// Gets the reporting user agent.
    /// </summary>
    /// <returns>The current reporting user agent.</returns>
    EmailDispositionNotificationUserAgent? GetReportingUserAgent();
    /// <summary>
    /// Determines whether the gateway is present or available.
    /// </summary>
    /// <returns><c>true</c> when the gateway is available; otherwise, <c>false</c>.</returns>
    bool HasGateway();
    /// <summary>
    /// Gets the gateway.
    /// </summary>
    /// <returns>The current gateway.</returns>
    EmailDispositionNotificationTypedValue? GetGateway();
    /// <summary>
    /// Determines whether the original recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original recipient is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalRecipient();
    /// <summary>
    /// Gets the original recipient.
    /// </summary>
    /// <returns>The current original recipient.</returns>
    EmailDispositionNotificationTypedValue? GetOriginalRecipient();
    /// <summary>
    /// Determines whether the final recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the final recipient is available; otherwise, <c>false</c>.</returns>
    bool HasFinalRecipient();
    /// <summary>
    /// Gets the final recipient.
    /// </summary>
    /// <returns>The current final recipient.</returns>
    EmailDispositionNotificationTypedValue? GetFinalRecipient();
    /// <summary>
    /// Determines whether the original message ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original message ID is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalMessageId();
    /// <summary>
    /// Gets the original message ID.
    /// </summary>
    /// <returns>The current original message ID.</returns>
    EmailMessageId? GetOriginalMessageId();
    /// <summary>
    /// Determines whether the disposition is present or available.
    /// </summary>
    /// <returns><c>true</c> when the disposition is available; otherwise, <c>false</c>.</returns>
    bool HasDisposition();
    /// <summary>
    /// Gets the disposition.
    /// </summary>
    /// <returns>The current disposition.</returns>
    EmailDispositionNotificationDisposition? GetDisposition();
    /// <summary>
    /// Gets the failure values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current failure values.</returns>
    List<string> GetFailureValues();
    /// <summary>
    /// Gets the error values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current error values.</returns>
    List<string> GetErrorValues();
    /// <summary>
    /// Gets the warning values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current warning values.</returns>
    List<string> GetWarningValues();
    /// <summary>
    /// Gets the extension fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension fields.</returns>
    List<EmailHeader> GetExtensionFields();
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
