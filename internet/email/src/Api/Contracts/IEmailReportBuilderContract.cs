using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReportBuilder"/>.
/// </summary>
public interface IEmailReportBuilderContract
{
    /// <summary>
    /// Creates a bounce configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "deliveryStatus">The delivery status used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateBounceWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatus(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] deliveryStatus);
    /// <summary>
    /// Creates a bounce configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "deliveryStatus">The delivery status used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateBounceWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatusAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] deliveryStatus, EmailReportOptions options);
    /// <summary>
    /// Creates a delivery status configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report);
    /// <summary>
    /// Creates a delivery status configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options);
    /// <summary>
    /// Creates a disposition notification configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateDispositionNotificationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report);
    /// <summary>
    /// Creates a disposition notification configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateDispositionNotificationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options);
    /// <summary>
    /// Creates a feedback report configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateFeedbackReportWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report);
    /// <summary>
    /// Creates a feedback report configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateFeedbackReportWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options);
    /// <summary>
    /// Creates a report configured for the requested behavior.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateReport(EmailReportKind kind, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options);
    /// <summary>
    /// Creates an international delivery status configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateInternationalDeliveryStatus(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options);
    /// <summary>
    /// Creates an international disposition notification configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateInternationalDispositionNotification(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options);
}
