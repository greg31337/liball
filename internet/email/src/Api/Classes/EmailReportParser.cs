using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses report into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailReportParser : IEmailReportParserContract
{
    internal static EmailReportParseResult ParseImplementationWithMessage(EmailMessage message)
    {
        return ParseImplementationWithMessageAndOptions(message, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReportParseResult ParseWithMessage(EmailMessage message)
    {
        return EmailReportParser.ParseImplementationWithMessage(message);
    }

    internal static EmailReportParseResult ParseImplementationWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (options == null)
            throw new ArgumentNullException("options");
        if (!message.HasBody())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(CreateIssue(EmailIssueSeverity.Error, EmailIssueCode.ReportStructureMalformed, "The message has no MIME body to inspect as a report.", "No report was produced."));
            return new EmailReportParseResult(null, issues);
        }

        return ParseImplementationWithRootAndOptions(message.GetBody() ?? throw new InvalidOperationException(), options);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailReportParseResult ParseWithMessageAndOptions(EmailMessage message, EmailReportParseOptions options)
    {
        return EmailReportParser.ParseImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailReportParseResult ParseImplementationWithRoot(MimeEntity root)
    {
        return ParseImplementationWithRootAndOptions(root, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReportParseResult ParseWithRoot(MimeEntity root)
    {
        return EmailReportParser.ParseImplementationWithRoot(root);
    }

    internal static EmailReportParseResult ParseImplementationWithRootAndOptions(MimeEntity root, EmailReportParseOptions options)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailReportParseOptions snapshot = options.Clone();
        EmailIssueList issues = new EmailIssueList();
        if (!(root.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "report"))
        {
            issues.Add(CreateIssue(EmailIssueSeverity.Error, EmailIssueCode.ReportStructureMalformed, "The MIME entity is not multipart/report.", "No report was produced."));
            return new EmailReportParseResult(null, issues);
        }

        if (root.GetChildCount() > snapshot.GetMaximumPartCount())
        {
            throw new EmailLimitException("MaxReportParts", snapshot.GetMaximumPartCount(), root.GetChildCount());
        }

        string? reportType = GetReportType(root, issues);
        MimeEntity? human = root.GetChildCount() > 0 ? root.GetChildAt(0) : null;
        MimeEntity? machine = root.GetChildCount() > 1 ? root.GetChildAt(1) : null;
        if (human == null || machine == null)
        {
            issues.Add(CreateIssue(EmailIssueSeverity.Error, EmailIssueCode.ReportStructureMalformed, "A multipart/report requires human-readable and machine-readable parts.", "The available ordered parts were retained."));
        }

        if (human != null && !string.Equals((human.GetMediaType() ?? throw new InvalidOperationException()).GetTopLevelType(), "text", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(CreateIssue(EmailIssueSeverity.Warning, EmailIssueCode.ReportStructureMalformed, "The first multipart/report part is not human-readable text.", "The part was retained in its declared position."));
        }

        if (reportType == null && machine != null)
        {
            reportType = InferReportType(machine);
        }

        bool consistent = machine != null && reportType != null && MachineTypeMatches(machine, reportType);
        if (machine != null && reportType != null && !consistent)
        {
            issues.Add(CreateIssue(EmailIssueSeverity.Error, EmailIssueCode.ReportTypeMismatch, "The machine-readable part does not match the multipart report-type parameter.", "Both declared types were preserved."));
        }

        MimeEntity? original = null;
        List<MimeEntity> additional = new List<MimeEntity>();
        int index = 2;
        while (index < root.GetChildCount())
        {
            MimeEntity child = root.GetChildAt(index);
            if (original == null && IsOriginalContent(child))
                original = child;
            else
                additional.Add(child);
            index++;
        }

        return new EmailReportParseResult(new EmailMultipartReport(root, reportType, human, machine, original, additional, consistent), issues);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailReportParseResult ParseWithRootAndOptions(MimeEntity root, EmailReportParseOptions options)
    {
        return EmailReportParser.ParseImplementationWithRootAndOptions(root, options);
    }

    internal static EmailReportFieldBlockParseResult ParseFieldBlocksImplementationWithReport(EmailMultipartReport report)
    {
        return ParseFieldBlocksImplementationWithReportAndOptions(report, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied field blocks data.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReportFieldBlockParseResult ParseFieldBlocksWithReport(EmailMultipartReport report)
    {
        return EmailReportParser.ParseFieldBlocksImplementationWithReport(report);
    }

    internal static EmailReportFieldBlockParseResult ParseFieldBlocksImplementationWithReportAndOptions(EmailMultipartReport report, EmailReportParseOptions options)
    {
        if (report == null)
            throw new ArgumentNullException("report");
        if (options == null)
            throw new ArgumentNullException("options");
        if (!report.HasMachineReadablePart())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(CreateIssue(EmailIssueSeverity.Error, EmailIssueCode.ReportStructureMalformed, "The report has no machine-readable part to parse.", "No field blocks were produced."));
            return new EmailReportFieldBlockParseResult(new List<EmailReportFieldBlock>(), issues, 0);
        }

        return new EmailReportFieldBlockParser().ParseWithEntityAndOptions(report.GetMachineReadablePart() ?? throw new InvalidOperationException(), options);
    }

    /// <summary>
    /// Parses the supplied field blocks data.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailReportFieldBlockParseResult ParseFieldBlocksWithReportAndOptions(EmailMultipartReport report, EmailReportParseOptions options)
    {
        return EmailReportParser.ParseFieldBlocksImplementationWithReportAndOptions(report, options);
    }

    static string? GetReportType(MimeEntity root, EmailIssueList issues)
    {
        EmailRfc2231DecodedParameter? parameter = (root.GetMediaType() ?? throw new InvalidOperationException()).GetParameters().GetDecoded("report-type");
        if (parameter == null)
        {
            issues.Add(CreateIssue(EmailIssueSeverity.Error, EmailIssueCode.ReportTypeMissing, "The multipart/report Content-Type has no report-type parameter.", "The machine-readable media subtype was used when possible."));
            return null;
        }

        byte[] bytes = parameter.GetDecodedBytes();
        if (!IsToken(bytes))
        {
            issues.Add(CreateIssue(EmailIssueSeverity.Error, EmailIssueCode.ReportStructureMalformed, "The multipart report-type parameter is not a valid ASCII token.", "The invalid value was preserved but not used."));
            return null;
        }

        return Encoding.ASCII.GetString(bytes).ToLowerInvariant();
    }

    static string? InferReportType(MimeEntity machine)
    {
        if (!string.Equals((machine.GetMediaType() ?? throw new InvalidOperationException()).GetTopLevelType(), "message", StringComparison.OrdinalIgnoreCase))
            return null;
        string subtype = (machine.GetMediaType() ?? throw new InvalidOperationException()).GetNormalizedSubtype();
        if (string.Equals(subtype, "global-delivery-status", StringComparison.Ordinal))
            return "delivery-status";
        if (string.Equals(subtype, "global-disposition-notification", StringComparison.Ordinal))
            return "disposition-notification";
        return subtype;
    }

    static bool MachineTypeMatches(MimeEntity machine, string reportType)
    {
        EmailMediaType? type = machine.GetMediaType();
        if (!string.Equals((type ?? throw new InvalidOperationException()).GetTopLevelType(), "message", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.Equals(type.GetSubtype(), reportType, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(reportType, "delivery-status", StringComparison.OrdinalIgnoreCase) && type.IsType("message", "global-delivery-status"))
            return true;
        return string.Equals(reportType, "disposition-notification", StringComparison.OrdinalIgnoreCase) && type.IsType("message", "global-disposition-notification");
    }

    static bool IsOriginalContent(MimeEntity entity)
    {
        EmailMediaType? type = entity.GetMediaType();
        return (type ?? throw new InvalidOperationException()).IsType("message", "rfc822") || type.IsType("message", "global") || type.IsType("text", "rfc822-headers") || type.IsType("message", "global-headers");
    }

    static bool IsToken(byte[] value)
    {
        if (value.Length == 0)
            return false;
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current < 33 || current > 126 || current == 40 || current == 41 || current == 60 || current == 62 || current == 64 || current == 44 || current == 59 || current == 58 || current == 92 || current == 34 || current == 47 || current == 91 || current == 93 || current == 63 || current == 61)
                return false;
            index++;
        }

        return true;
    }

    static EmailIssue CreateIssue(EmailIssueSeverity severity, EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, null, null, "Content-Type", recovery);
    }
}
