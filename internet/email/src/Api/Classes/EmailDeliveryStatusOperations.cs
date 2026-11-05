using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailDeliveryStatus"/>.
/// </summary>
public class EmailDeliveryStatusOperations : IEmailDeliveryStatusOperationsContract
{
    /// <summary>Creates a standalone delivery status report.</summary>
    /// <param name = "internationalized">Whether the report uses internationalized fields.</param>
    /// <param name = "messageFields">The message fields.</param>
    /// <param name = "recipients">The recipient field blocks.</param>
    /// <returns>The configured delivery status.</returns>
    public EmailDeliveryStatus CreateWithInternationalizedAndMessageFieldsAndRecipients(bool internationalized, EmailDeliveryStatusMessage messageFields, List<EmailDeliveryStatusRecipient> recipients)
    {
        return EmailDeliveryStatus.CreateImplementationWithInternationalizedAndMessageFieldsAndRecipients(internationalized, messageFields, recipients);
    }

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
    public EmailDeliveryStatus CreateWithInternationalizedAndMessageFieldsAndRecipientsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(bool internationalized, EmailDeliveryStatusMessage messageFields, List<EmailDeliveryStatusRecipient> recipients, EmailMultipartReport multipartReport, MimeEntity? originalContentPart, EmailHeaders? originalHeaders, EmailMessage? originalMessage)
    {
        return EmailDeliveryStatus.CreateImplementationWithInternationalizedAndMessageFieldsAndRecipientsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(internationalized, messageFields, recipients, multipartReport, originalContentPart, originalHeaders, originalMessage);
    }
}
