using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Parses delivery status into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailDeliveryStatusParser : IEmailDeliveryStatusParserContract
{
    internal static EmailDeliveryStatusParseResult ParseImplementationWithMessage(EmailMessage message)
    {
        return ParseImplementationWithMessageAndOptions(message, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDeliveryStatusParseResult ParseWithMessage(EmailMessage message)
    {
        return EmailDeliveryStatusParser.ParseImplementationWithMessage(message);
    }

    internal static EmailDeliveryStatusParseResult ParseImplementationWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailReportParseResult report = new EmailReportParser().ParseWithMessageAndOptions(message, options);
        return ParseReport(report, options.Clone());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDeliveryStatusParseResult ParseWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options)
    {
        return EmailDeliveryStatusParser.ParseImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailDeliveryStatusParseResult ParseImplementationWithRoot(MimeEntity root)
    {
        return ParseImplementationWithRootAndOptions(root, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDeliveryStatusParseResult ParseWithRoot(MimeEntity root)
    {
        return EmailDeliveryStatusParser.ParseImplementationWithRoot(root);
    }

    internal static EmailDeliveryStatusParseResult ParseImplementationWithRootAndOptions(MimeEntity root, EmailReportParseOptions options)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailReportParseResult report = new EmailReportParser().ParseWithRootAndOptions(root, options);
        return ParseReport(report, options.Clone());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDeliveryStatusParseResult ParseWithRootAndOptions(MimeEntity root, EmailReportParseOptions options)
    {
        return EmailDeliveryStatusParser.ParseImplementationWithRootAndOptions(root, options);
    }

    static EmailDeliveryStatusParseResult ParseReport(EmailReportParseResult reportResult, EmailReportParseOptions options)
    {
        EmailIssueList issues = reportResult.GetIssues();
        if (!reportResult.HasReport())
            return new EmailDeliveryStatusParseResult(null, issues);
        EmailMultipartReport? report = reportResult.GetReport();
        if (!(report ?? throw new InvalidOperationException()).IsDeliveryStatus())
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "The multipart report is not a delivery-status report.", "No delivery-status value was produced.", null));
            return new EmailDeliveryStatusParseResult(null, issues);
        }

        if (!report.HasMachineReadablePart())
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "The delivery-status report has no machine-readable part.", "No delivery-status value was produced.", null));
            return new EmailDeliveryStatusParseResult(null, issues);
        }

        MimeEntity? machine = report.GetMachineReadablePart();
        bool internationalized = ((machine ?? throw new InvalidOperationException()).GetMediaType() ?? throw new InvalidOperationException()).IsType("message", "global-delivery-status");
        if (!internationalized && !(machine.GetMediaType() ?? throw new InvalidOperationException()).IsType("message", "delivery-status"))
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "The machine-readable part is not a delivery-status media type.", "No delivery-status value was produced.", "Content-Type"));
            return new EmailDeliveryStatusParseResult(null, issues);
        }

        EmailReportFieldBlockParseResult fields = new EmailReportFieldBlockParser().ParseWithEntityAndOptions(machine, options);
        issues.AddRange(fields.GetIssues());
        if (fields.GetBlockCount() == 0)
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusRequiredFieldMissing, "The delivery-status body has no per-message field block.", "No delivery-status value was produced.", null));
            return new EmailDeliveryStatusParseResult(null, issues);
        }

        EmailDeliveryStatusMessage messageFields = new EmailDeliveryStatusMessage(fields.GetBlockAt(0));
        ValidateMessageFields(messageFields, issues);
        List<EmailDeliveryStatusRecipient> recipients = new List<EmailDeliveryStatusRecipient>();
        int index = 1;
        while (index < fields.GetBlockCount())
        {
            EmailDeliveryStatusRecipient recipient = new EmailDeliveryStatusRecipient(fields.GetBlockAt(index));
            ValidateRecipient(recipient, issues);
            recipients.Add(recipient);
            index++;
        }

        if (recipients.Count == 0)
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusRequiredFieldMissing, "The delivery-status body has no per-recipient field block.", "The per-message block was retained.", null));
        }

        ValidateInternationalization(fields.GetBlocks(), internationalized, issues);
        MimeEntity? originalPart = report.HasOriginalContentPart() ? report.GetOriginalContentPart() : null;
        EmailHeaders? originalHeaders = null;
        EmailMessage? originalMessage = null;
        if (originalPart != null)
        {
            if (originalPart.GetKind() == MimeEntityKind.Message && originalPart.HasNestedMessage())
                originalMessage = originalPart.GetNestedMessage();
            else if ((originalPart.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "rfc822-headers") || (originalPart.GetMediaType() ?? throw new InvalidOperationException()).IsType("message", "global-headers"))
            {
                originalHeaders = ParseOriginalHeaders(originalPart, options, issues);
            }
        }

        EmailDeliveryStatus value = new EmailDeliveryStatusOperations().CreateWithInternationalizedAndMessageFieldsAndRecipientsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(internationalized, messageFields, recipients, report, originalPart, originalHeaders, originalMessage);
        return new EmailDeliveryStatusParseResult(value, issues);
    }

    static void ValidateMessageFields(EmailDeliveryStatusMessage value, EmailIssueList issues)
    {
        EmailReportFieldBlock block = value.GetFieldBlock();
        if (block.GetFieldsNamed("Reporting-MTA").Count == 0)
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusRequiredFieldMissing, "The per-message block has no Reporting-MTA field.", "The remaining fields were retained.", "Reporting-MTA"));
        }
        else if (!value.HasReportingMta())
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "The Reporting-MTA field is not a valid typed value.", "The original field was retained.", "Reporting-MTA"));
        }

        ValidateDuplicate(block, "Reporting-MTA", issues);
        ValidateOptionalTyped(block, "DSN-Gateway", value.GetDsnGateway(), issues);
        ValidateOptionalTyped(block, "Received-From-MTA", value.GetReceivedFromMta(), issues);
        ValidateOptionalText(block, "Original-Envelope-Id", value.GetOriginalEnvelopeId(), issues);
        ValidateOptionalDate(block, "Arrival-Date", value.GetArrivalDate(), issues);
    }

    static void ValidateRecipient(EmailDeliveryStatusRecipient value, EmailIssueList issues)
    {
        EmailReportFieldBlock block = value.GetFieldBlock();
        ValidateRequiredTyped(block, "Final-Recipient", value.GetFinalRecipient(), issues);
        ValidateDuplicate(block, "Final-Recipient", issues);
        ValidateOptionalTyped(block, "Original-Recipient", value.GetOriginalRecipient(), issues);
        ValidateOptionalTyped(block, "Remote-MTA", value.GetRemoteMta(), issues);
        ValidateOptionalTyped(block, "Diagnostic-Code", value.GetDiagnosticCode(), issues);
        EmailHeader? actionField = block.GetFirstField("Action");
        if (actionField == null)
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusRequiredFieldMissing, "A per-recipient block has no Action field.", "The remaining fields were retained.", "Action"));
        }
        else if (value.GetAction() == EmailDeliveryStatusAction.Unknown)
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusActionInvalid, "The delivery Action value is not recognized.", "The original value was retained.", "Action"));
        }

        ValidateDuplicate(block, "Action", issues);
        EmailHeader? statusField = block.GetFirstField("Status");
        EmailDeliveryStatusCode? status = value.GetStatus();
        if (statusField == null)
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusRequiredFieldMissing, "A per-recipient block has no Status field.", "The remaining fields were retained.", "Status"));
        }
        else if (status == null)
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusCodeInvalid, "The enhanced Status value is malformed.", "The original value was retained.", "Status"));
        }
        else if (!StatusMatchesAction(value.GetAction(), status.GetStatusClass()))
        {
            issues.Add(Issue(EmailIssueCode.DeliveryStatusStatusMismatch, "The enhanced Status class conflicts with the delivery Action.", "Both original values were retained.", "Status"));
        }

        ValidateDuplicate(block, "Status", issues);
        ValidateOptionalDate(block, "Last-Attempt-Date", value.GetLastAttemptDate(), issues);
        ValidateOptionalDate(block, "Will-Retry-Until", value.GetWillRetryUntil(), issues);
        ValidateOptionalText(block, "Final-Log-ID", value.GetFinalLogId(), issues);
    }

    static void ValidateRequiredTyped(EmailReportFieldBlock block, string name, EmailDeliveryStatusTypedValue? value, EmailIssueList issues)
    {
        if (block.GetFirstField(name) == null)
            issues.Add(Issue(EmailIssueCode.DeliveryStatusRequiredFieldMissing, "A required delivery-status typed field is missing.", "The remaining fields were retained.", name));
        else if (value == null)
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "A delivery-status typed field is malformed.", "The original field was retained.", name));
    }

    static void ValidateOptionalTyped(EmailReportFieldBlock block, string name, EmailDeliveryStatusTypedValue? value, EmailIssueList issues)
    {
        ValidateDuplicate(block, name, issues);
        if (block.GetFirstField(name) != null && value == null)
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "An optional delivery-status typed field is malformed.", "The original field was retained.", name));
    }

    static void ValidateOptionalDate(EmailReportFieldBlock block, string name, EmailDateTime? value, EmailIssueList issues)
    {
        ValidateDuplicate(block, name, issues);
        if (block.GetFirstField(name) != null && value == null)
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "A delivery-status date field is malformed.", "The original field was retained.", name));
    }

    static void ValidateOptionalText(EmailReportFieldBlock block, string name, string? value, EmailIssueList issues)
    {
        ValidateDuplicate(block, name, issues);
        if (block.GetFirstField(name) != null && value == null)
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "A delivery-status text field is empty.", "The original field was retained.", name));
    }

    static void ValidateDuplicate(EmailReportFieldBlock block, string name, EmailIssueList issues)
    {
        if (block.GetFieldsNamed(name).Count > 1)
            issues.Add(Issue(EmailIssueCode.DeliveryStatusMalformed, "A singleton delivery-status field is duplicated.", "The first value was used and every field was retained.", name));
    }

    static bool StatusMatchesAction(EmailDeliveryStatusAction action, int statusClass)
    {
        if (action == EmailDeliveryStatusAction.Unknown)
            return true;
        if (action == EmailDeliveryStatusAction.Failed)
            return statusClass == 5;
        if (action == EmailDeliveryStatusAction.Delayed)
            return statusClass == 4;
        return statusClass == 2;
    }

    static void ValidateInternationalization(List<EmailReportFieldBlock> blocks, bool internationalized, EmailIssueList issues)
    {
        int index = 0;
        while (index < blocks.Count)
        {
            byte[] raw = blocks[index].GetRawBytes();
            if (!internationalized && ContainsNonAscii(raw))
            {
                issues.Add(Issue(EmailIssueCode.DeliveryStatusInternationalizationMismatch, "A message/delivery-status part contains non-ASCII field bytes.", "The bytes were retained; message/global-delivery-status is required for internationalized fields.", "Content-Type"));
                return;
            }

            if (internationalized && !IsValidUtf8(raw))
            {
                issues.Add(Issue(EmailIssueCode.DeliveryStatusInternationalizationMismatch, "A message/global-delivery-status part contains invalid UTF-8.", "The original bytes were retained.", "Content-Type"));
                return;
            }

            index++;
        }
    }

    static EmailHeaders ParseOriginalHeaders(MimeEntity entity, EmailReportParseOptions options, EmailIssueList issues)
    {
        byte[] bytes;
        using (MemoryStream output = new MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes(entity, output, options.GetMaximumReportBytes());
            bytes = output.ToArray();
        }

        byte[] parserInput = AddHeaderSeparator(bytes);
        EmailParsingLimits limits = new EmailParsingLimits();
        limits.SetMaxHeaderBytes(Math.Max(1, parserInput.Length));
        limits.SetMaxHeaderFieldCount(options.GetMaximumFieldCount());
        limits.SetMaxLineBytes(options.GetMaximumLineBytes());
        EmailHeaderBlockParserOptions parserOptions = new EmailHeaderBlockParserOptions();
        parserOptions.SetLimits(limits);
        parserOptions.SetMode(options.GetMode());
        EmailHeaderBlockParseResult parsed = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(parserInput, parserOptions, new EmailSourcePosition(0, 1, 1), true);
        issues.AddRange(parsed.GetIssues());
        if (!parsed.IsComplete())
            issues.Add(Issue(EmailIssueCode.DeliveryStatusOriginalContentMalformed, "The returned original-header part is malformed.", "All complete original fields were retained.", null));
        return parsed.GetHeaders();
    }

    static byte[] AddHeaderSeparator(byte[] value)
    {
        byte[] suffix;
        if (value.Length >= 2 && value[value.Length - 2] == 13 && value[value.Length - 1] == 10)
            suffix = new byte[]
            {
                13,
                10
            };
        else if (value.Length >= 1 && value[value.Length - 1] == 10)
            suffix = new byte[]
            {
                10
            };
        else if (value.Length >= 1 && value[value.Length - 1] == 13)
            suffix = new byte[]
            {
                13
            };
        else
            suffix = new byte[]
            {
                13,
                10,
                13,
                10
            };
        byte[] result = new byte[value.Length + suffix.Length];
        value.CopyTo(result, 0);
        suffix.CopyTo(result, value.Length);
        return result;
    }

    static bool ContainsNonAscii(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
                return true;
            index++;
        }

        return false;
    }

    static bool IsValidUtf8(byte[] value)
    {
        return new EmailUtf8().IsValid(value);
    }

    static EmailIssue Issue(EmailIssueCode code, string description, string recovery, string? fieldName)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, fieldName, recovery);
    }
}
