using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFeedbackReport"/>.
/// </summary>
public interface IEmailFeedbackReportOperationsContract
{
    /// <summary>Creates a standalone feedback report.</summary>
    /// <param name = "fields">The feedback fields.</param>
    /// <returns>The configured feedback report.</returns>
    EmailFeedbackReport CreateWithFields(EmailReportFieldBlock fields);
    /// <summary>
    /// Creates a new feedback report.
    /// </summary>
    /// <param name = "fields">The fields used by the operation.</param>
    /// <param name = "multipartReport">The multipart report used by the operation.</param>
    /// <param name = "originalContentPart">The original content part used by the operation.</param>
    /// <param name = "originalHeaders">The original headers used by the operation.</param>
    /// <param name = "originalMessage">The original message used by the operation.</param>
    /// <returns>The configured feedback report.</returns>
    EmailFeedbackReport CreateWithFieldsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(EmailReportFieldBlock fields, EmailMultipartReport multipartReport, MimeEntity? originalContentPart, EmailHeaders? originalHeaders, EmailMessage? originalMessage);
}
