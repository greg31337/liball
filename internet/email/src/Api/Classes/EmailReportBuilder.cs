using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Builds report from caller-supplied values.
/// </summary>
public class EmailReportBuilder : IEmailReportBuilderContract
{
    internal static EmailComposer CreateBounceImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatus(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] deliveryStatus)
    {
        EmailReportOptions options = new EmailReportOptions();
        options.SetOriginalContent(EmailReportOriginalContent.FullMessage);
        return CreateReportImplementation(EmailReportKind.DeliveryStatus, original, reporter, recipient, humanReadable, deliveryStatus, options);
    }

    /// <summary>
    /// Creates a bounce configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "deliveryStatus">The delivery status used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateBounceWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatus(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] deliveryStatus)
    {
        return EmailReportBuilder.CreateBounceImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatus(original, reporter, recipient, humanReadable, deliveryStatus);
    }

    internal static EmailComposer CreateBounceImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatusAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] deliveryStatus, EmailReportOptions options)
    {
        return CreateReportImplementation(EmailReportKind.DeliveryStatus, original, reporter, recipient, humanReadable, deliveryStatus, options);
    }

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
    public EmailComposer CreateBounceWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatusAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] deliveryStatus, EmailReportOptions options)
    {
        return EmailReportBuilder.CreateBounceImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatusAndOptions(original, reporter, recipient, humanReadable, deliveryStatus, options);
    }

    internal static EmailComposer CreateDeliveryStatusImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report)
    {
        return CreateReportImplementation(EmailReportKind.DeliveryStatus, original, reporter, recipient, humanReadable, report, new EmailReportOptions());
    }

    /// <summary>
    /// Creates a delivery status configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report)
    {
        return EmailReportBuilder.CreateDeliveryStatusImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(original, reporter, recipient, humanReadable, report);
    }

    internal static EmailComposer CreateDeliveryStatusImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return CreateReportImplementation(EmailReportKind.DeliveryStatus, original, reporter, recipient, humanReadable, report, options);
    }

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
    public EmailComposer CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return EmailReportBuilder.CreateDeliveryStatusImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(original, reporter, recipient, humanReadable, report, options);
    }

    internal static EmailComposer CreateDispositionNotificationImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report)
    {
        return CreateReportImplementation(EmailReportKind.DispositionNotification, original, reporter, recipient, humanReadable, report, new EmailReportOptions());
    }

    /// <summary>
    /// Creates a disposition notification configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateDispositionNotificationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report)
    {
        return EmailReportBuilder.CreateDispositionNotificationImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(original, reporter, recipient, humanReadable, report);
    }

    internal static EmailComposer CreateDispositionNotificationImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return CreateReportImplementation(EmailReportKind.DispositionNotification, original, reporter, recipient, humanReadable, report, options);
    }

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
    public EmailComposer CreateDispositionNotificationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return EmailReportBuilder.CreateDispositionNotificationImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(original, reporter, recipient, humanReadable, report, options);
    }

    internal static EmailComposer CreateFeedbackReportImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report)
    {
        return CreateReportImplementation(EmailReportKind.FeedbackReport, original, reporter, recipient, humanReadable, report, new EmailReportOptions());
    }

    /// <summary>
    /// Creates a feedback report configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateFeedbackReportWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report)
    {
        return EmailReportBuilder.CreateFeedbackReportImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(original, reporter, recipient, humanReadable, report);
    }

    internal static EmailComposer CreateFeedbackReportImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return CreateReportImplementation(EmailReportKind.FeedbackReport, original, reporter, recipient, humanReadable, report, options);
    }

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
    public EmailComposer CreateFeedbackReportWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return EmailReportBuilder.CreateFeedbackReportImplementationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(original, reporter, recipient, humanReadable, report, options);
    }

    internal static EmailComposer CreateReportImplementation(EmailReportKind kind, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return CreateReportWithKindAndOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptionsAndInternationalizedReport(kind, original, reporter, recipient, humanReadable, report, options, false);
    }

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
    public EmailComposer CreateReport(EmailReportKind kind, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return EmailReportBuilder.CreateReportImplementation(kind, original, reporter, recipient, humanReadable, report, options);
    }

    internal static EmailComposer CreateInternationalDeliveryStatusImplementation(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return CreateReportWithKindAndOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptionsAndInternationalizedReport(EmailReportKind.DeliveryStatus, original, reporter, recipient, humanReadable, report, options, true);
    }

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
    public EmailComposer CreateInternationalDeliveryStatus(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return EmailReportBuilder.CreateInternationalDeliveryStatusImplementation(original, reporter, recipient, humanReadable, report, options);
    }

    internal static EmailComposer CreateInternationalDispositionNotificationImplementation(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return CreateReportWithKindAndOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptionsAndInternationalizedReport(EmailReportKind.DispositionNotification, original, reporter, recipient, humanReadable, report, options, true);
    }

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
    public EmailComposer CreateInternationalDispositionNotification(EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        return EmailReportBuilder.CreateInternationalDispositionNotificationImplementation(original, reporter, recipient, humanReadable, report, options);
    }

    static EmailComposer CreateReportWithKindAndOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptionsAndInternationalizedReport(EmailReportKind kind, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options, bool internationalizedReport)
    {
        ValidateInputs(kind, original, reporter, recipient, humanReadable, report, options);
        EmailReportOptions snapshot = options.Clone();
        if (internationalizedReport && !snapshot.GetComposerOptions().GetAllowSmtpUtf8())
        {
            throw new InvalidOperationException("Internationalized reports require SMTPUTF8 composition.");
        }

        EnsureLength(humanReadable.Length, snapshot.GetMaximumHumanReadableCharacters(), "MaxReportHumanReadableCharacters");
        byte[] normalizedReport = NormalizeReport(report, snapshot.GetMaximumReportBytes(), internationalizedReport);
        List<MimeEntity> children = new List<MimeEntity>();
        children.Add(new EmailEntityFactory().CreateTextWithValue(humanReadable));
        children.Add(CreateMachinePart(kind, normalizedReport, internationalizedReport));
        AddOriginalPart(children, original, snapshot, internationalizedReport);
        MimeEntity root = CreateReportRoot(kind, children);
        EmailComposer composer = new EmailComposerOperations().CreateWithOptions(snapshot.GetComposerOptions());
        composer.SetFrom(reporter);
        composer.AddTo(recipient);
        composer.SetNullEnvelopeSender();
        composer.SetSubject(GetSubject(kind));
        composer.SetAutoSubmitted("auto-generated");
        composer.SetRawEntity(GenerateBoundaries(root, kind));
        return composer;
    }

    static void ValidateInputs(EmailReportKind kind, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, byte[] report, EmailReportOptions options)
    {
        if (kind < EmailReportKind.DeliveryStatus || kind > EmailReportKind.FeedbackReport)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (original == null)
        {
            throw new ArgumentNullException("original");
        }

        if (reporter == null)
        {
            throw new ArgumentNullException("reporter");
        }

        if (recipient == null)
        {
            throw new ArgumentNullException("recipient");
        }

        if (humanReadable == null)
        {
            throw new ArgumentNullException("humanReadable");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }

    static void AddOriginalPart(List<MimeEntity> children, EmailMessage original, EmailReportOptions options, bool internationalizedReport)
    {
        if (options.GetOriginalContent() == EmailReportOriginalContent.None)
        {
            return;
        }

        EmailMessage safe = CreateSafeOriginal(original, options.GetIncludeBlindRecipientHeaders());
        if (options.GetOriginalContent() == EmailReportOriginalContent.FullMessage)
        {
            if (internationalizedReport)
            {
                children.Add(new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), ParseMediaType("message/global"), null, "8bit", null, null, null, null, null, null, new List<MimeEntity>(), safe, null, null, null, new EmailIssueList()));
            }
            else
            {
                children.Add(new EmailEntityFactory().CreateNestedMessage(safe));
            }

            return;
        }

        EmailHeaderWriteOptions writeOptions = new EmailHeaderWriteOptions();
        writeOptions.SetAllowInternationalized(options.GetComposerOptions().GetAllowSmtpUtf8());
        writeOptions.SetMaximumOutputBytes(options.GetMaximumOriginalHeaderBytes());
        EmailHeaderWriteResult written = new EmailHeaderWriter().WritePreservingWithHeadersAndOptions(safe.GetHeaders(), writeOptions);
        if (!written.IsSuccess())
        {
            throw new InvalidOperationException("The original headers cannot be represented safely in a report.");
        }

        EmailMediaType mediaType = ParseMediaType(internationalizedReport ? "message/global-headers" : "text/rfc822-headers");
        string transferEncoding = ContainsNonAscii(written.GetBytes()) ? "8bit" : "7bit";
        children.Add(CreateContentPart(MimeEntityKind.Leaf, mediaType, transferEncoding, written.GetBytes()));
    }

    static EmailMessage CreateSafeOriginal(EmailMessage original, bool includeBlindRecipientHeaders)
    {
        EmailMessage safe = original.Clone();
        (safe.GetEnvelope() ?? throw new InvalidOperationException()).ClearSender();
        (safe.GetEnvelope() ?? throw new InvalidOperationException()).ClearRecipients();
        (safe.GetEnvelope() ?? throw new InvalidOperationException()).ClearMboxFromLine();
        if (!includeBlindRecipientHeaders)
        {
            (safe.GetHeaders() ?? throw new InvalidOperationException()).RemoveAll("Bcc");
            (safe.GetHeaders() ?? throw new InvalidOperationException()).RemoveAll("Resent-Bcc");
        }

        return safe;
    }

    static MimeEntity CreateMachinePart(EmailReportKind kind, byte[] report, bool internationalizedReport)
    {
        MimeEntityKind entityKind = MimeEntityKind.DeliveryStatus;
        string mediaType = "message/delivery-status";
        string transferEncoding = "7bit";
        if (kind == EmailReportKind.DeliveryStatus && internationalizedReport)
        {
            mediaType = "message/global-delivery-status";
            transferEncoding = "8bit";
        }

        if (kind == EmailReportKind.DispositionNotification)
        {
            entityKind = MimeEntityKind.DispositionNotification;
            mediaType = internationalizedReport ? "message/global-disposition-notification" : "message/disposition-notification";
            transferEncoding = internationalizedReport ? "8bit" : "7bit";
        }
        else if (kind == EmailReportKind.FeedbackReport)
        {
            entityKind = MimeEntityKind.FeedbackReport;
            mediaType = "message/feedback-report";
        }

        return CreateContentPart(entityKind, ParseMediaType(mediaType), transferEncoding, report);
    }

    static MimeEntity CreateContentPart(MimeEntityKind kind, EmailMediaType mediaType, string transferEncoding, byte[] content)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(kind, new EmailHeadersOperations().CreateWith(), mediaType, null, transferEncoding, null, null, null, null, null, new EmailContentOperations().CreateMemoryCopy(content, false), new List<MimeEntity>(), null, null, null, null, new EmailIssueList());
    }

    static MimeEntity CreateReportRoot(EmailReportKind kind, List<MimeEntity> children)
    {
        string reportType = GetReportType(kind);
        EmailMediaType mediaType = ParseMediaType("multipart/report; report-type=" + reportType);
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Multipart, new EmailHeadersOperations().CreateWith(), mediaType, null, children, null);
    }

    static MimeEntity? GenerateBoundaries(MimeEntity value, EmailReportKind kind)
    {
        EmailGenerationOptions generation = new EmailGenerationOptions();
        generation.SetGenerateDate(false);
        generation.SetGenerateMessageId(false);
        generation.SetGenerateContentIds(false);
        generation.SetGenerateMimeVersion(false);
        generation.SetGenerateBoundaries(true);
        generation.SetDeterministicOutput(true);
        generation.SetIdentifierDomain("local.invalid");
        generation.SetDeterministicSeed(Encoding.ASCII.GetBytes("email-report-boundary-v1-" + GetReportType(kind)));
        generation.ClearGenerationDate();
        generation.SetMaximumDepth(EmailTraversalConstants.DefaultMaximumDepth);
        generation.SetMaximumEntityCount(EmailTraversalConstants.DefaultMaximumEntityCount);
        return new EmailGenerator().GenerateWithEntityAndOptions(value, generation).GetEntity();
    }

    static EmailMediaType ParseMediaType(string value)
    {
        EmailMediaTypeParseResult parsed = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailStructuredValueParseMode.Strict);
        if (!parsed.IsSuccess() || !parsed.IsComplete())
        {
            throw new InvalidOperationException("An internal report media type is invalid.");
        }

        return parsed.GetValue();
    }

    static byte[] NormalizeReport(byte[] value, int maximumBytes, bool allowUtf8)
    {
        if (allowUtf8)
        {
            if (!new EmailUtf8().IsValid(value))
                throw new ArgumentException("An internationalized report field block must contain valid UTF-8.", "report");
        }

        List<byte> output = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current == 0 || (!allowUtf8 && current > 127))
            {
                throw new ArgumentException(allowUtf8 ? "A report field block must not contain NUL bytes." : "A report field block must contain only NUL-free ASCII bytes.", "report");
            }

            if (current == 13 || current == 10)
            {
                if (current == 13 && index + 1 < value.Length && value[index + 1] == 10)
                {
                    index++;
                }

                AppendReportByte(output, 13, maximumBytes);
                AppendReportByte(output, 10, maximumBytes);
            }
            else
            {
                AppendReportByte(output, current, maximumBytes);
            }

            index++;
        }

        if (output.Count == 0 || output[output.Count - 1] != 10)
        {
            AppendReportByte(output, 13, maximumBytes);
            AppendReportByte(output, 10, maximumBytes);
        }

        return output.ToArray();
    }

    static void AppendReportByte(List<byte> output, byte value, int maximumBytes)
    {
        if (output.Count >= maximumBytes)
        {
            throw new EmailLimitException("MaxReportBytes", maximumBytes, output.Count + 1);
        }

        output.Add(value);
    }

    static void EnsureLength(int actual, int maximum, string limitName)
    {
        if (actual > maximum)
        {
            throw new EmailLimitException(limitName, maximum, actual);
        }
    }

    static string GetReportType(EmailReportKind kind)
    {
        if (kind == EmailReportKind.DeliveryStatus)
        {
            return "delivery-status";
        }

        return kind == EmailReportKind.DispositionNotification ? "disposition-notification" : "feedback-report";
    }

    static string GetSubject(EmailReportKind kind)
    {
        if (kind == EmailReportKind.DeliveryStatus)
        {
            return "Delivery Status Notification";
        }

        return kind == EmailReportKind.DispositionNotification ? "Message Disposition Notification" : "Abuse Feedback Report";
    }

    static bool ContainsNonAscii(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
