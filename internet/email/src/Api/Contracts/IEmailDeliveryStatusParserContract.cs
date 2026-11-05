using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDeliveryStatusParser"/>.
/// </summary>
public interface IEmailDeliveryStatusParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDeliveryStatusParseResult ParseWithMessage(EmailMessage message);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDeliveryStatusParseResult ParseWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDeliveryStatusParseResult ParseWithRoot(MimeEntity root);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDeliveryStatusParseResult ParseWithRootAndOptions(MimeEntity root, EmailReportParseOptions options);
}
