using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDeliveryStatus"/>.
/// </summary>
public interface IEmailDeliveryStatusOperationsContract
{
    /// <summary>Creates a standalone delivery status report.</summary>
    /// <param name = "internationalized">Whether the report uses internationalized fields.</param>
    /// <param name = "messageFields">The message fields.</param>
    /// <param name = "recipients">The recipient field blocks.</param>
    /// <returns>The configured delivery status.</returns>
    EmailDeliveryStatus CreateWithInternationalizedAndMessageFieldsAndRecipients(bool internationalized, EmailDeliveryStatusMessage messageFields, List<EmailDeliveryStatusRecipient> recipients);
    /// <summary>
    /// Creates a new delivery status.
    /// </summary>
    /// <param name = "internationalized">Whether to enable the internationalized behavior.</param>
    /// <param name = "messageFields">The message fields used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "multipartReport">The multipart report used by the operation.</param>
    /// <param name = "originalContentPart">The original content part used by the operation.</param>
    /// <param name = "originalHeaders">The original headers used by the operation.</param>
    /// <param name = "originalMessage">The original message used by the operation.</param>
    /// <returns>The configured delivery status.</returns>
    EmailDeliveryStatus CreateWithInternationalizedAndMessageFieldsAndRecipientsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(bool internationalized, EmailDeliveryStatusMessage messageFields, List<EmailDeliveryStatusRecipient> recipients, EmailMultipartReport multipartReport, MimeEntity? originalContentPart, EmailHeaders? originalHeaders, EmailMessage? originalMessage);
}
