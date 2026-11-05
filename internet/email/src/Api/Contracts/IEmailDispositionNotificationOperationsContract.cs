using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotification"/>.
/// </summary>
public interface IEmailDispositionNotificationOperationsContract
{
    /// <summary>Creates a standalone disposition notification.</summary>
    /// <param name = "internationalized">Whether the report uses internationalized fields.</param>
    /// <param name = "fields">The notification fields.</param>
    /// <returns>The configured notification.</returns>
    EmailDispositionNotification CreateWithInternationalizedAndFields(bool internationalized, EmailReportFieldBlock fields);
    /// <summary>
    /// Creates a new disposition notification.
    /// </summary>
    /// <param name = "internationalized">Whether to enable the internationalized behavior.</param>
    /// <param name = "fields">The fields used by the operation.</param>
    /// <param name = "multipartReport">The multipart report used by the operation.</param>
    /// <param name = "originalContentPart">The original content part used by the operation.</param>
    /// <param name = "originalHeaders">The original headers used by the operation.</param>
    /// <param name = "originalMessage">The original message used by the operation.</param>
    /// <returns>The configured disposition notification.</returns>
    EmailDispositionNotification CreateWithInternationalizedAndFieldsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(bool internationalized, EmailReportFieldBlock fields, EmailMultipartReport multipartReport, MimeEntity? originalContentPart, EmailHeaders? originalHeaders, EmailMessage? originalMessage);
}
