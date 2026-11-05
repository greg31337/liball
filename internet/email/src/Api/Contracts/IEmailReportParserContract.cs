using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReportParser"/>.
/// </summary>
public interface IEmailReportParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReportParseResult ParseWithMessage(EmailMessage message);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailReportParseResult ParseWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReportParseResult ParseWithRoot(MimeEntity root);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailReportParseResult ParseWithRootAndOptions(MimeEntity root, EmailReportParseOptions options);
    /// <summary>
    /// Parses the supplied field blocks data.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailReportFieldBlockParseResult ParseFieldBlocksWithReport(EmailMultipartReport report);
    /// <summary>
    /// Parses the supplied field blocks data.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailReportFieldBlockParseResult ParseFieldBlocksWithReportAndOptions(EmailMultipartReport report, EmailReportParseOptions options);
}
