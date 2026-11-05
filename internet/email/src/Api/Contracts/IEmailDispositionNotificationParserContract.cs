using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotificationParser"/>.
/// </summary>
public interface IEmailDispositionNotificationParserContract
{
    /// <summary>
    /// Parses the supplied request data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDispositionNotificationRequestParseResult ParseRequestWithMessage(EmailMessage message);
    /// <summary>
    /// Parses the supplied request data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailDispositionNotificationRequestParseResult ParseRequestWithMessageAndMode(EmailMessage message, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDispositionNotificationParseResult ParseWithMessage(EmailMessage message);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDispositionNotificationParseResult ParseWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDispositionNotificationParseResult ParseWithRoot(MimeEntity root);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDispositionNotificationParseResult ParseWithRootAndOptions(MimeEntity root, EmailReportParseOptions options);
}
