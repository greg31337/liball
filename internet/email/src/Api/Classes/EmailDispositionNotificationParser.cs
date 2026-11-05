using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Parses disposition notification into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailDispositionNotificationParser : IEmailDispositionNotificationParserContract
{
    internal static EmailDispositionNotificationRequestParseResult ParseRequestImplementationWithMessage(EmailMessage message)
    {
        return ParseRequestImplementationWithMessageAndMode(message, EmailStructuredValueParseMode.Strict);
    }

    /// <summary>
    /// Parses the supplied request data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDispositionNotificationRequestParseResult ParseRequestWithMessage(EmailMessage message)
    {
        return EmailDispositionNotificationParser.ParseRequestImplementationWithMessage(message);
    }

    internal static EmailDispositionNotificationRequestParseResult ParseRequestImplementationWithMessageAndMode(EmailMessage message, EmailStructuredValueParseMode mode)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (mode < EmailStructuredValueParseMode.Strict || mode > EmailStructuredValueParseMode.Tolerant)
            throw new ArgumentOutOfRangeException("mode");
        EmailIssueList issues = new EmailIssueList();
        EmailHeaders? headers = message.GetHeaders();
        EmailHeader? notificationField = (headers ?? throw new InvalidOperationException()).GetFirst("Disposition-Notification-To");
        if (notificationField == null)
            return new EmailDispositionNotificationRequestParseResult(null, issues);
        if (headers.GetCountNamed("Disposition-Notification-To") > 1)
            issues.Add(Issue(EmailIssueCode.DispositionNotificationRequestMalformed, "Disposition-Notification-To is duplicated.", "The first request field was used.", "Disposition-Notification-To"));
        byte[] addressBytes = notificationField.GetUnfoldedValue();
        EmailAddressParserOptions addressOptions = new EmailAddressParserOptions();
        addressOptions.SetMode(mode == EmailStructuredValueParseMode.Strict ? EmailAddressParseMode.Strict : EmailAddressParseMode.Tolerant);
        addressOptions.SetAllowInternationalized(ContainsNonAscii(addressBytes));
        EmailAddressParseResult addressResult = new EmailAddressParser().ParseListWithInputAndOptions(addressBytes, addressOptions);
        issues.AddRange(addressResult.GetIssues());
        EmailAddressList notificationTo = addressResult.GetAddressList();
        bool addressesValid = addressResult.IsSuccess() && addressResult.IsComplete() && notificationTo.GetMailboxCount() > 0 && notificationTo.GetGroupCount() == 0;
        if (!addressesValid)
            issues.Add(Issue(EmailIssueCode.DispositionNotificationRequestMalformed, "Disposition-Notification-To must contain a nonempty mailbox list.", "Every successfully parsed address was retained.", "Disposition-Notification-To"));
        EmailDispositionNotificationTypedValue? originalRecipient = null;
        EmailHeader? originalField = headers.GetFirst("Original-Recipient");
        if (originalField != null)
        {
            originalRecipient = EmailDispositionNotificationValueParser.ParseTyped(originalField);
            if (originalRecipient == null)
                issues.Add(Issue(EmailIssueCode.DispositionNotificationRequestMalformed, "The request Original-Recipient field is malformed.", "The original field was retained.", "Original-Recipient"));
            if (headers.GetCountNamed("Original-Recipient") > 1)
                issues.Add(Issue(EmailIssueCode.DispositionNotificationRequestMalformed, "The request Original-Recipient field is duplicated.", "The first field was used.", "Original-Recipient"));
        }

        List<EmailDispositionNotificationOption> requestOptions = new List<EmailDispositionNotificationOption>();
        EmailHeader? optionsField = headers.GetFirst("Disposition-Notification-Options");
        if (optionsField != null)
        {
            List<EmailDispositionNotificationOption>? parsedOptions = EmailDispositionNotificationRequestValueParser.ParseOptions(optionsField);
            if (parsedOptions == null)
                issues.Add(Issue(EmailIssueCode.DispositionNotificationRequestMalformed, "Disposition-Notification-Options is malformed.", "The original field was retained.", "Disposition-Notification-Options"));
            else
                requestOptions = parsedOptions;
            if (headers.GetCountNamed("Disposition-Notification-Options") > 1)
                issues.Add(Issue(EmailIssueCode.DispositionNotificationRequestMalformed, "Disposition-Notification-Options is duplicated.", "The first field was used.", "Disposition-Notification-Options"));
        }

        bool internationalized = ContainsNonAscii(addressBytes) || originalField != null && ContainsNonAscii(originalField.GetUnfoldedValue());
        EmailDispositionNotificationRequest? request = addressesValid ? new EmailDispositionNotificationRequest(internationalized, notificationTo, originalRecipient, requestOptions) : null;
        return new EmailDispositionNotificationRequestParseResult(request, issues);
    }

    /// <summary>
    /// Parses the supplied request data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDispositionNotificationRequestParseResult ParseRequestWithMessageAndMode(EmailMessage message, EmailStructuredValueParseMode mode)
    {
        return EmailDispositionNotificationParser.ParseRequestImplementationWithMessageAndMode(message, mode);
    }

    internal static EmailDispositionNotificationParseResult ParseImplementationWithMessage(EmailMessage message)
    {
        return ParseImplementationWithMessageAndOptions(message, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDispositionNotificationParseResult ParseWithMessage(EmailMessage message)
    {
        return EmailDispositionNotificationParser.ParseImplementationWithMessage(message);
    }

    internal static EmailDispositionNotificationParseResult ParseImplementationWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (options == null)
            throw new ArgumentNullException("options");
        return ParseReport(new EmailReportParser().ParseWithMessageAndOptions(message, options), options.Clone());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDispositionNotificationParseResult ParseWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options)
    {
        return EmailDispositionNotificationParser.ParseImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailDispositionNotificationParseResult ParseImplementationWithRoot(MimeEntity root)
    {
        return ParseImplementationWithRootAndOptions(root, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDispositionNotificationParseResult ParseWithRoot(MimeEntity root)
    {
        return EmailDispositionNotificationParser.ParseImplementationWithRoot(root);
    }

    internal static EmailDispositionNotificationParseResult ParseImplementationWithRootAndOptions(MimeEntity root, EmailReportParseOptions options)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        if (options == null)
            throw new ArgumentNullException("options");
        return ParseReport(new EmailReportParser().ParseWithRootAndOptions(root, options), options.Clone());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDispositionNotificationParseResult ParseWithRootAndOptions(MimeEntity root, EmailReportParseOptions options)
    {
        return EmailDispositionNotificationParser.ParseImplementationWithRootAndOptions(root, options);
    }

    static EmailDispositionNotificationParseResult ParseReport(EmailReportParseResult reportResult, EmailReportParseOptions options)
    {
        EmailIssueList issues = reportResult.GetIssues();
        if (!reportResult.HasReport())
            return new EmailDispositionNotificationParseResult(null, issues);
        EmailMultipartReport? report = reportResult.GetReport();
        if (!(report ?? throw new InvalidOperationException()).IsDispositionNotification())
        {
            issues.Add(Issue(EmailIssueCode.DispositionNotificationMalformed, "The multipart report is not a disposition-notification report.", "No MDN value was produced.", null));
            return new EmailDispositionNotificationParseResult(null, issues);
        }

        if (!report.HasMachineReadablePart())
        {
            issues.Add(Issue(EmailIssueCode.DispositionNotificationMalformed, "The MDN report has no machine-readable part.", "No MDN value was produced.", null));
            return new EmailDispositionNotificationParseResult(null, issues);
        }

        MimeEntity? machine = report.GetMachineReadablePart();
        bool internationalized = ((machine ?? throw new InvalidOperationException()).GetMediaType() ?? throw new InvalidOperationException()).IsType("message", "global-disposition-notification");
        if (!internationalized && !(machine.GetMediaType() ?? throw new InvalidOperationException()).IsType("message", "disposition-notification"))
        {
            issues.Add(Issue(EmailIssueCode.DispositionNotificationMalformed, "The machine-readable part is not an MDN media type.", "No MDN value was produced.", "Content-Type"));
            return new EmailDispositionNotificationParseResult(null, issues);
        }

        EmailReportFieldBlockParseResult parsedFields = new EmailReportFieldBlockParser().ParseWithEntityAndOptions(machine, options);
        issues.AddRange(parsedFields.GetIssues());
        if (parsedFields.GetBlockCount() == 0)
        {
            issues.Add(Issue(EmailIssueCode.DispositionNotificationRequiredFieldMissing, "The MDN body has no field block.", "No MDN value was produced.", null));
            return new EmailDispositionNotificationParseResult(null, issues);
        }

        if (parsedFields.GetBlockCount() > 1)
        {
            issues.Add(Issue(EmailIssueCode.DispositionNotificationMalformed, "An MDN body must contain one continuous field block.", "The first block was interpreted and all machine-part bytes remain available.", null));
        }

        EmailReportFieldBlock fields = parsedFields.GetBlockAt(0);
        ValidateFields(fields, issues);
        ValidateInternationalization(parsedFields.GetBlocks(), internationalized, issues);
        MimeEntity? originalPart = report.HasOriginalContentPart() ? report.GetOriginalContentPart() : null;
        EmailHeaders? originalHeaders = null;
        EmailMessage? originalMessage = null;
        if (originalPart != null)
        {
            if (originalPart.GetKind() == MimeEntityKind.Message && originalPart.HasNestedMessage())
                originalMessage = originalPart.GetNestedMessage();
            else if ((originalPart.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "rfc822-headers") || (originalPart.GetMediaType() ?? throw new InvalidOperationException()).IsType("message", "global-headers"))
                originalHeaders = ParseOriginalHeaders(originalPart, options, issues);
        }

        EmailDispositionNotification value = new EmailDispositionNotificationOperations().CreateWithInternationalizedAndFieldsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(internationalized, fields, report, originalPart, originalHeaders, originalMessage);
        ValidateOriginalMessageId(value, issues);
        return new EmailDispositionNotificationParseResult(value, issues);
    }

    static void ValidateFields(EmailReportFieldBlock fields, EmailIssueList issues)
    {
        ValidateOptional(fields, "Reporting-UA", EmailDispositionNotificationValueParser.ParseUserAgent(fields.GetFirstField("Reporting-UA")) != null, issues);
        ValidateOptional(fields, "MDN-Gateway", EmailDispositionNotificationValueParser.ParseTyped(fields.GetFirstField("MDN-Gateway")) != null, issues);
        ValidateOptional(fields, "Original-Recipient", EmailDispositionNotificationValueParser.ParseTyped(fields.GetFirstField("Original-Recipient")) != null, issues);
        ValidateRequired(fields, "Final-Recipient", EmailDispositionNotificationValueParser.ParseTyped(fields.GetFirstField("Final-Recipient")) != null, issues);
        ValidateOptional(fields, "Original-Message-ID", EmailDispositionNotificationValueParser.ParseMessageId(fields.GetFirstField("Original-Message-ID")) != null, issues);
        ValidateRequired(fields, "Disposition", EmailDispositionNotificationValueParser.ParseDisposition(fields.GetFirstField("Disposition")) != null, issues);
    }

    static void ValidateRequired(EmailReportFieldBlock fields, string name, bool valid, EmailIssueList issues)
    {
        int count = fields.GetFieldsNamed(name).Count;
        if (count == 0)
            issues.Add(Issue(EmailIssueCode.DispositionNotificationRequiredFieldMissing, "A required MDN field is missing.", "The remaining fields were retained.", name));
        else if (!valid)
            issues.Add(Issue(EmailIssueCode.DispositionNotificationValueInvalid, "An MDN field value is malformed.", "The original field was retained.", name));
        if (count > 1)
            issues.Add(Issue(EmailIssueCode.DispositionNotificationMalformed, "A singleton MDN field is duplicated.", "The first value was used and every field was retained.", name));
    }

    static void ValidateOptional(EmailReportFieldBlock fields, string name, bool valid, EmailIssueList issues)
    {
        int count = fields.GetFieldsNamed(name).Count;
        if (count > 0 && !valid)
            issues.Add(Issue(EmailIssueCode.DispositionNotificationValueInvalid, "An optional MDN field value is malformed.", "The original field was retained.", name));
        if (count > 1)
            issues.Add(Issue(EmailIssueCode.DispositionNotificationMalformed, "A singleton MDN field is duplicated.", "The first value was used and every field was retained.", name));
    }

    static void ValidateOriginalMessageId(EmailDispositionNotification value, EmailIssueList issues)
    {
        EmailHeaders? original = value.HasOriginalHeaders() ? value.GetOriginalHeaders() : value.HasOriginalMessage() ? (value.GetOriginalMessage() ?? throw new InvalidOperationException()).GetHeaders() : null;
        if (original == null)
            return;
        EmailHeader? field = original.GetFirst("Message-ID");
        if (field == null)
        {
            if (value.HasOriginalMessageId())
                issues.Add(Issue(EmailIssueCode.DispositionNotificationValueInvalid, "Original-Message-ID is present although the returned original has no Message-ID.", "Both values were retained.", "Original-Message-ID"));
            return;
        }

        EmailMessageIdParseResult parsed = new EmailMessageIdParser().ParseMessageIdWithInputAndMode(field.GetUnfoldedValue(), EmailMessageIdParseMode.Strict);
        if (!value.HasOriginalMessageId())
        {
            issues.Add(Issue(EmailIssueCode.DispositionNotificationRequiredFieldMissing, "Original-Message-ID is required when the original message has a Message-ID.", "The returned original content was retained.", "Original-Message-ID"));
        }
        else if (parsed.IsSuccess() && parsed.HasSingleIdentifier() && !IdentifiersEqual(value.GetOriginalMessageId(), parsed.GetIdentifier()))
        {
            issues.Add(Issue(EmailIssueCode.DispositionNotificationValueInvalid, "Original-Message-ID does not match the returned original message.", "Both identifiers were retained.", "Original-Message-ID"));
        }
    }

    static bool IdentifiersEqual(EmailMessageId? first, EmailMessageId? second)
    {
        return string.Equals((first ?? throw new InvalidOperationException()).GetIdLeft(), (second ?? throw new InvalidOperationException()).GetIdLeft(), StringComparison.Ordinal) && string.Equals(first.GetIdRight(), second.GetIdRight(), StringComparison.OrdinalIgnoreCase) && first.IsRightDomainLiteral() == second.IsRightDomainLiteral();
    }

    static void ValidateInternationalization(List<EmailReportFieldBlock> blocks, bool internationalized, EmailIssueList issues)
    {
        int index = 0;
        while (index < blocks.Count)
        {
            byte[] raw = blocks[index].GetRawBytes();
            if (!internationalized && ContainsNonAscii(raw))
            {
                issues.Add(Issue(EmailIssueCode.DispositionNotificationInternationalizationMismatch, "A message/disposition-notification part contains non-ASCII bytes.", "The bytes were retained; the global media type is required for UTF-8 fields.", "Content-Type"));
                return;
            }

            if (internationalized && !IsValidUtf8(raw))
            {
                issues.Add(Issue(EmailIssueCode.DispositionNotificationInternationalizationMismatch, "A message/global-disposition-notification part contains invalid UTF-8.", "The original bytes were retained.", "Content-Type"));
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
            issues.Add(Issue(EmailIssueCode.DispositionNotificationOriginalContentMalformed, "The returned original-header part is malformed.", "All complete original fields were retained.", null));
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
