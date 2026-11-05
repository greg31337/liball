using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Parses report field block into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailReportFieldBlockParser : IEmailReportFieldBlockParserContract
{
    internal static EmailReportFieldBlockParseResult ParseImplementationWithInput(ReadOnlySpan<byte> input)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, new EmailReportParseOptions(), new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReportFieldBlockParseResult ParseWithInput(ReadOnlySpan<byte> input)
    {
        return EmailReportFieldBlockParser.ParseImplementationWithInput(input);
    }

    internal static EmailReportFieldBlockParseResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailReportParseOptions options)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReportFieldBlockParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailReportParseOptions options)
    {
        return EmailReportFieldBlockParser.ParseImplementationWithInputAndOptions(input, options);
    }

    internal static EmailReportFieldBlockParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailReportParseOptions options, EmailSourcePosition initialPosition)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        if (initialPosition == null)
            throw new ArgumentNullException("initialPosition");
        EmailReportParseOptions snapshot = options.Clone();
        if (input.Length > snapshot.GetMaximumReportBytes())
        {
            throw new EmailLimitException("MaxReportBytes", snapshot.GetMaximumReportBytes(), input.Length);
        }

        EmailReportFieldBlockParserWorker worker = new EmailReportFieldBlockParserWorker(input.ToArray(), snapshot, initialPosition);
        return worker.Parse();
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailReportFieldBlockParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailReportParseOptions options, EmailSourcePosition initialPosition)
    {
        return EmailReportFieldBlockParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailReportFieldBlockParseResult ParseImplementationWithEntity(MimeEntity entity)
    {
        return ParseImplementationWithEntityAndOptions(entity, new EmailReportParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReportFieldBlockParseResult ParseWithEntity(MimeEntity entity)
    {
        return EmailReportFieldBlockParser.ParseImplementationWithEntity(entity);
    }

    internal static EmailReportFieldBlockParseResult ParseImplementationWithEntityAndOptions(MimeEntity entity, EmailReportParseOptions options)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailReportParseOptions snapshot = options.Clone();
        if (!entity.HasContent())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.ReportFieldBlockMalformed, "The report part has no direct content to parse as field blocks.", null, null, null, "No field blocks were produced."));
            return new EmailReportFieldBlockParseResult(new List<EmailReportFieldBlock>(), issues, 0);
        }

        using (MemoryStream output = new MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(entity, output, snapshot.GetMaximumReportBytes(), "MaxReportBytes", snapshot.GetMaximumReportBytes(), (long)snapshot.GetMaximumReportBytes() + 1);
            return ParseImplementationWithInputAndOptionsAndInitialPosition(output.ToArray(), snapshot, new EmailSourcePosition(0, 1, 1));
        }
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailReportFieldBlockParseResult ParseWithEntityAndOptions(MimeEntity entity, EmailReportParseOptions options)
    {
        return EmailReportFieldBlockParser.ParseImplementationWithEntityAndOptions(entity, options);
    }
}
