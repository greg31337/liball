using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFeedbackReportWriter"/>.
/// </summary>
public interface IEmailFeedbackReportWriterContract
{
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] WriteWithReport(EmailFeedbackReport report);
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    byte[] WriteWithReportAndMaximumBytes(EmailFeedbackReport report, int maximumBytes);
    /// <summary>
    /// Creates a report configured for the requested behavior.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadable(EmailFeedbackReport report, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable);
    /// <summary>
    /// Creates a report configured for the requested behavior.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailComposer CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(EmailFeedbackReport report, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, EmailReportOptions options);
}
