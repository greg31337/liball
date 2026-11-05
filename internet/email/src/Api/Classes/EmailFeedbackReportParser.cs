using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Parses feedback report into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailFeedbackReportParser : IEmailFeedbackReportParserContract
{
    internal static EmailFeedbackReportParseResult ParseImplementationWithMessage(EmailMessage message)
    {
        return ParseImplementationWithMessageAndOptions(message, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailFeedbackReportParseResult ParseWithMessage(EmailMessage message)
    {
        return EmailFeedbackReportParser.ParseImplementationWithMessage(message);
    }

    internal static EmailFeedbackReportParseResult ParseImplementationWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options)
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
    public EmailFeedbackReportParseResult ParseWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options)
    {
        return EmailFeedbackReportParser.ParseImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailFeedbackReportParseResult ParseImplementationWithRoot(MimeEntity root)
    {
        return ParseImplementationWithRootAndOptions(root, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailFeedbackReportParseResult ParseWithRoot(MimeEntity root)
    {
        return EmailFeedbackReportParser.ParseImplementationWithRoot(root);
    }

    internal static EmailFeedbackReportParseResult ParseImplementationWithRootAndOptions(MimeEntity root, EmailReportParseOptions options)
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
    public EmailFeedbackReportParseResult ParseWithRootAndOptions(MimeEntity root, EmailReportParseOptions options)
    {
        return EmailFeedbackReportParser.ParseImplementationWithRootAndOptions(root, options);
    }

    static EmailFeedbackReportParseResult ParseReport(EmailReportParseResult reportResult, EmailReportParseOptions options)
    {
        EmailIssueList issues = reportResult.GetIssues();
        if (!reportResult.HasReport())
            return new EmailFeedbackReportParseResult(null, issues);
        EmailMultipartReport? multipart = reportResult.GetReport();
        if (!(multipart ?? throw new InvalidOperationException()).IsFeedbackReport())
        {
            issues.Add(Issue(EmailIssueCode.FeedbackReportMalformed, "The multipart report is not a feedback report.", "No feedback-report value was produced.", null));
            return new EmailFeedbackReportParseResult(null, issues);
        }

        if (!multipart.HasMachineReadablePart() || !((multipart.GetMachineReadablePart() ?? throw new InvalidOperationException()).GetMediaType() ?? throw new InvalidOperationException()).IsType("message", "feedback-report"))
        {
            issues.Add(Issue(EmailIssueCode.FeedbackReportMalformed, "The report has no message/feedback-report machine part.", "No feedback-report value was produced.", "Content-Type"));
            return new EmailFeedbackReportParseResult(null, issues);
        }

        MimeEntity? machine = multipart.GetMachineReadablePart();
        EmailReportFieldBlockParseResult parsedFields = new EmailReportFieldBlockParser().ParseWithEntityAndOptions(machine ?? throw new InvalidOperationException(), options);
        issues.AddRange(parsedFields.GetIssues());
        if (parsedFields.GetBlockCount() == 0)
        {
            issues.Add(Issue(EmailIssueCode.FeedbackReportRequiredFieldMissing, "The feedback report has no field block.", "No feedback-report value was produced.", null));
            return new EmailFeedbackReportParseResult(null, issues);
        }

        if (parsedFields.GetBlockCount() > 1)
            issues.Add(Issue(EmailIssueCode.FeedbackReportMalformed, "A feedback report must contain one continuous field block.", "The first block was interpreted and all machine-part bytes remain available.", null));
        EmailReportFieldBlock fields = parsedFields.GetBlockAt(0);
        ValidateFields(fields, issues);
        ValidateAscii(parsedFields.GetBlocks(), issues);
        MimeEntity? originalPart = multipart.HasOriginalContentPart() ? multipart.GetOriginalContentPart() : null;
        EmailHeaders? originalHeaders = null;
        EmailMessage? originalMessage = null;
        if (originalPart == null)
        {
            issues.Add(Issue(EmailIssueCode.FeedbackReportOriginalContentMissing, "A feedback report must include the reported original message or its headers.", "The human and machine-readable report parts were retained.", null));
        }
        else if (originalPart.GetKind() == MimeEntityKind.Message && originalPart.HasNestedMessage())
            originalMessage = originalPart.GetNestedMessage();
        else if ((originalPart.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "rfc822-headers") || (originalPart.GetMediaType() ?? throw new InvalidOperationException()).IsType("message", "global-headers"))
            originalHeaders = ParseOriginalHeaders(originalPart, options, issues);
        else
            issues.Add(Issue(EmailIssueCode.FeedbackReportOriginalContentMalformed, "The reported original-content part cannot be projected.", "The MIME part remains available.", "Content-Type"));
        EmailFeedbackReport value = new EmailFeedbackReportOperations().CreateWithFieldsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(fields, multipart, originalPart, originalHeaders, originalMessage);
        return new EmailFeedbackReportParseResult(value, issues);
    }

    static void ValidateFields(EmailReportFieldBlock fields, EmailIssueList issues)
    {
        ValidateRequired(fields, "Feedback-Type", EmailFeedbackReportValueParser.GetToken(fields.GetFirstField("Feedback-Type")) != null, issues);
        ValidateRequired(fields, "User-Agent", EmailFeedbackReportValueParser.GetPrintableAscii(fields.GetFirstField("User-Agent")) != null, issues);
        uint version = EmailFeedbackReportValueParser.ParsePositiveUInt32(fields.GetFirstField("Version"));
        ValidateRequired(fields, "Version", version != 0, issues);
        if (version != 0 && version != 1)
            issues.Add(Issue(EmailIssueCode.FeedbackReportVersionUnsupported, "The feedback-report version is not supported.", "The original version and every field were retained.", "Version"));
        ValidateOptional(fields, "Original-Envelope-Id", EmailFeedbackReportValueParser.GetNonemptyText(fields.GetFirstField("Original-Envelope-Id")) != null, issues);
        ValidateOptional(fields, "Original-Mail-From", EmailFeedbackReportValueParser.ParsePath(fields.GetFirstField("Original-Mail-From"), true) != null, issues);
        ValidateOptional(fields, "Arrival-Date", EmailFeedbackReportValueParser.ParseDate(fields.GetFirstField("Arrival-Date")) != null, issues);
        ValidateOptional(fields, "Received-Date", EmailFeedbackReportValueParser.ParseDate(fields.GetFirstField("Received-Date")) != null, issues);
        if (fields.GetFirstField("Arrival-Date") != null && fields.GetFirstField("Received-Date") != null)
            issues.Add(Issue(EmailIssueCode.FeedbackReportMalformed, "Arrival-Date and historic Received-Date cannot both appear.", "Arrival-Date was used and both fields were retained.", "Arrival-Date"));
        ValidateOptional(fields, "Reporting-MTA", EmailFeedbackReportValueParser.ParseTyped(fields.GetFirstField("Reporting-MTA")) != null, issues);
        ValidateOptional(fields, "Source-IP", EmailFeedbackReportValueParser.ParseIp(fields.GetFirstField("Source-IP")) != null, issues);
        ValidateOptional(fields, "Source-Port", EmailFeedbackReportValueParser.ParseSourcePort(fields.GetFirstField("Source-Port")) != 0, issues);
        ValidateOptional(fields, "Incidents", EmailFeedbackReportValueParser.ParsePositiveUInt32(fields.GetFirstField("Incidents")) != 0, issues);
        ValidateRepeated(fields, "Original-Rcpt-To", issues);
        ValidateRepeated(fields, "Reported-Domain", issues);
        ValidateRepeated(fields, "Reported-URI", issues);
        ValidateAuthenticationResults(fields, issues);
        ValidateOptional(fields, "Auth-Failure", EmailFeedbackReportValueParser.GetToken(fields.GetFirstField("Auth-Failure")) != null, issues);
        ValidateOptional(fields, "Delivery-Result", EmailFeedbackReportValueParser.GetToken(fields.GetFirstField("Delivery-Result")) != null, issues);
        ValidateOptional(fields, "DKIM-Domain", EmailFeedbackReportValueParser.ParseDomain(fields.GetFirstField("DKIM-Domain")) != null, issues);
        ValidateOptional(fields, "DKIM-Identity", EmailFeedbackReportValueParser.GetNonemptyText(fields.GetFirstField("DKIM-Identity")) != null, issues);
        ValidateOptional(fields, "DKIM-Selector", EmailFeedbackReportValueParser.GetToken(fields.GetFirstField("DKIM-Selector")) != null, issues);
        ValidateRepeated(fields, "SPF-DNS", issues);
        if (EmailFeedbackReportValueParser.ParseFeedbackType(fields.GetFirstField("Feedback-Type")) == EmailFeedbackType.AuthenticationFailure)
        {
            if (fields.GetFieldsNamed("Auth-Failure").Count != 1)
                issues.Add(Issue(EmailIssueCode.FeedbackReportRequiredFieldMissing, "An auth-failure report requires exactly one Auth-Failure field.", "All available authentication evidence was retained.", "Auth-Failure"));
            if (fields.GetFieldsNamed("Authentication-Results").Count != 1)
                issues.Add(Issue(EmailIssueCode.FeedbackReportRequiredFieldMissing, "An auth-failure report requires exactly one Authentication-Results field.", "All available authentication evidence was retained.", "Authentication-Results"));
            else
            {
                List<EmailAuthenticationResults> authentication = EmailFeedbackReportValueParser.ParseAuthenticationResults(fields);
                if (authentication.Count == 1 && authentication[0].GetResultCount() != 1)
                    issues.Add(Issue(EmailIssueCode.FeedbackReportValueInvalid, "An auth-failure report must describe one authentication method result.", "The complete Authentication-Results field was retained.", "Authentication-Results"));
            }
        }
    }

    static void ValidateRepeated(EmailReportFieldBlock fields, string name, EmailIssueList issues)
    {
        List<EmailHeader> values = fields.GetFieldsNamed(name);
        int index = 0;
        while (index < values.Count)
        {
            if (!IsRepeatedValueValid(name, values[index]))
                issues.Add(Issue(EmailIssueCode.FeedbackReportValueInvalid, "A repeatable feedback-report field is malformed.", "The original field was retained.", name));
            index++;
        }
    }

    static bool IsRepeatedValueValid(string name, EmailHeader field)
    {
        if (name == "Original-Rcpt-To")
            return EmailFeedbackReportValueParser.ParsePath(field, false) != null;
        if (name == "Reported-Domain")
            return EmailFeedbackReportValueParser.ParseDomain(field) != null;
        if (name == "SPF-DNS")
            return EmailFeedbackReportValueParser.GetNonemptyText(field) != null;
        if (name == "Reported-URI")
        {
            string? text = EmailFeedbackReportValueParser.GetNonemptyText(field);
            Uri? uri;
            return text != null && Uri.TryCreate(text, UriKind.Absolute, out uri);
        }

        return false;
    }

    static void ValidateAuthenticationResults(EmailReportFieldBlock fields, EmailIssueList issues)
    {
        List<EmailHeader> values = fields.GetFieldsNamed("Authentication-Results");
        int index = 0;
        while (index < values.Count)
        {
            EmailAuthenticationResultsParseResult parsed = new EmailAuthenticationResultsParser().ParseWithInputAndMode(values[index].GetUnfoldedValue(), EmailStructuredValueParseMode.Strict);
            if (!parsed.IsSuccess() || !parsed.IsComplete() || !parsed.HasValue())
                issues.Add(Issue(EmailIssueCode.FeedbackReportValueInvalid, "An Authentication-Results feedback field is malformed.", "The original field was retained.", "Authentication-Results"));
            index++;
        }
    }

    static void ValidateRequired(EmailReportFieldBlock fields, string name, bool valid, EmailIssueList issues)
    {
        int count = fields.GetFieldsNamed(name).Count;
        if (count == 0)
            issues.Add(Issue(EmailIssueCode.FeedbackReportRequiredFieldMissing, "A required feedback-report field is missing.", "The remaining fields were retained.", name));
        else if (!valid)
            issues.Add(Issue(EmailIssueCode.FeedbackReportValueInvalid, "A required feedback-report field is malformed.", "The original field was retained.", name));
        if (count > 1)
            issues.Add(Issue(EmailIssueCode.FeedbackReportMalformed, "A singleton feedback-report field is duplicated.", "The first value was used and every field was retained.", name));
    }

    static void ValidateOptional(EmailReportFieldBlock fields, string name, bool valid, EmailIssueList issues)
    {
        int count = fields.GetFieldsNamed(name).Count;
        if (count > 0 && !valid)
            issues.Add(Issue(EmailIssueCode.FeedbackReportValueInvalid, "An optional feedback-report field is malformed.", "The original field was retained.", name));
        if (count > 1)
            issues.Add(Issue(EmailIssueCode.FeedbackReportMalformed, "A singleton feedback-report field is duplicated.", "The first value was used and every field was retained.", name));
    }

    static void ValidateAscii(List<EmailReportFieldBlock> blocks, EmailIssueList issues)
    {
        int blockIndex = 0;
        while (blockIndex < blocks.Count)
        {
            byte[] bytes = blocks[blockIndex].GetRawBytes();
            int byteIndex = 0;
            while (byteIndex < bytes.Length)
            {
                if (bytes[byteIndex] > 127)
                {
                    issues.Add(Issue(EmailIssueCode.FeedbackReportValueInvalid, "A message/feedback-report field block contains non-ASCII bytes.", "The original bytes were retained.", "Content-Type"));
                    return;
                }

                byteIndex++;
            }

            blockIndex++;
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
            issues.Add(Issue(EmailIssueCode.FeedbackReportOriginalContentMalformed, "The returned original-header part is malformed.", "All complete original fields were retained.", null));
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

    static EmailIssue Issue(EmailIssueCode code, string description, string recovery, string? fieldName)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, fieldName, recovery);
    }
}
