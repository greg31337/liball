using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Writes feedback report in its wire or storage format.
/// </summary>
public class EmailFeedbackReportWriter : IEmailFeedbackReportWriterContract
{
    internal static byte[] WriteImplementationWithReport(EmailFeedbackReport report)
    {
        return WriteImplementationWithReportAndMaximumBytes(report, EmailReportOptionsConstants.DefaultMaximumReportBytes);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] WriteWithReport(EmailFeedbackReport report)
    {
        return EmailFeedbackReportWriter.WriteImplementationWithReport(report);
    }

    internal static byte[] WriteImplementationWithReportAndMaximumBytes(EmailFeedbackReport report, int maximumBytes)
    {
        if (report == null)
            throw new ArgumentNullException("report");
        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException("maximumBytes");
        Validate(report);
        byte[] raw = (report.GetFieldBlock() ?? throw new InvalidOperationException()).GetRawBytes();
        ValidateAscii(raw);
        return Normalize(raw, maximumBytes);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public byte[] WriteWithReportAndMaximumBytes(EmailFeedbackReport report, int maximumBytes)
    {
        return EmailFeedbackReportWriter.WriteImplementationWithReportAndMaximumBytes(report, maximumBytes);
    }

    internal static EmailComposer CreateReportImplementationWithReportAndOriginalAndReporterAndRecipientAndHumanReadable(EmailFeedbackReport report, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable)
    {
        return CreateReportImplementationWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(report, original, reporter, recipient, humanReadable, new EmailReportOptions());
    }

    /// <summary>
    /// Creates a report configured for the requested behavior.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadable(EmailFeedbackReport report, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable)
    {
        return EmailFeedbackReportWriter.CreateReportImplementationWithReportAndOriginalAndReporterAndRecipientAndHumanReadable(report, original, reporter, recipient, humanReadable);
    }

    internal static EmailComposer CreateReportImplementationWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(EmailFeedbackReport report, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, EmailReportOptions options)
    {
        if (report == null)
            throw new ArgumentNullException("report");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailReportOptions snapshot = options.Clone();
        if (snapshot.GetOriginalContent() == EmailReportOriginalContent.None)
            throw new InvalidOperationException("A feedback report must include the reported original message or its headers.");
        byte[] fields = WriteImplementationWithReportAndMaximumBytes(report, snapshot.GetMaximumReportBytes());
        return new EmailReportBuilder().CreateFeedbackReportWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(original, reporter, recipient, humanReadable, fields, snapshot);
    }

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
    public EmailComposer CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(EmailFeedbackReport report, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, EmailReportOptions options)
    {
        return EmailFeedbackReportWriter.CreateReportImplementationWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(report, original, reporter, recipient, humanReadable, options);
    }

    static void Validate(EmailFeedbackReport value)
    {
        EmailReportFieldBlock? fields = value.GetFieldBlock();
        if (!(fields ?? throw new InvalidOperationException()).IsSuccess())
            throw new InvalidOperationException("The feedback-report field block contains malformed field syntax.");
        ValidateRequired(fields, "Feedback-Type", value.HasFeedbackType());
        ValidateRequired(fields, "User-Agent", value.HasUserAgent());
        ValidateRequired(fields, "Version", value.HasVersion() && value.GetVersion() == 1);
        ValidateOptional(fields, "Original-Envelope-Id", value.GetOriginalEnvelopeId() != null);
        ValidateOptional(fields, "Original-Mail-From", value.GetOriginalMailFrom() != null);
        ValidateOptional(fields, "Arrival-Date", EmailFeedbackReportValueParser.ParseDate(fields.GetFirstField("Arrival-Date")) != null);
        ValidateOptional(fields, "Received-Date", EmailFeedbackReportValueParser.ParseDate(fields.GetFirstField("Received-Date")) != null);
        if (fields.GetFirstField("Arrival-Date") != null && fields.GetFirstField("Received-Date") != null)
            throw new InvalidOperationException("Arrival-Date and Received-Date cannot both be emitted.");
        ValidateOptional(fields, "Reporting-MTA", value.GetReportingMta() != null);
        ValidateOptional(fields, "Source-IP", value.GetSourceIp() != null);
        ValidateOptional(fields, "Source-Port", EmailFeedbackReportValueParser.ParseSourcePort(fields.GetFirstField("Source-Port")) != 0);
        ValidateOptional(fields, "Incidents", EmailFeedbackReportValueParser.ParsePositiveUInt32(fields.GetFirstField("Incidents")) != 0);
        ValidateRepeated(fields, "Authentication-Results");
        ValidateRepeated(fields, "Original-Rcpt-To");
        ValidateRepeated(fields, "Reported-Domain");
        ValidateRepeated(fields, "Reported-URI");
        ValidateOptional(fields, "Auth-Failure", EmailFeedbackReportValueParser.GetToken(fields.GetFirstField("Auth-Failure")) != null);
        ValidateOptional(fields, "Delivery-Result", EmailFeedbackReportValueParser.GetToken(fields.GetFirstField("Delivery-Result")) != null);
        ValidateOptional(fields, "DKIM-Domain", EmailFeedbackReportValueParser.ParseDomain(fields.GetFirstField("DKIM-Domain")) != null);
        ValidateOptional(fields, "DKIM-Identity", EmailFeedbackReportValueParser.GetNonemptyText(fields.GetFirstField("DKIM-Identity")) != null);
        ValidateOptional(fields, "DKIM-Selector", EmailFeedbackReportValueParser.GetToken(fields.GetFirstField("DKIM-Selector")) != null);
        ValidateRepeated(fields, "SPF-DNS");
        if (value.GetFeedbackType() == EmailFeedbackType.AuthenticationFailure)
        {
            ValidateRequired(fields, "Auth-Failure", value.GetAuthenticationFailure() != EmailFeedbackAuthenticationFailure.Unknown);
            if (fields.GetFieldsNamed("Authentication-Results").Count != 1)
                throw new InvalidOperationException("An auth-failure report requires exactly one Authentication-Results field.");
            List<EmailAuthenticationResults> authentication = value.GetAuthenticationResults();
            if (authentication.Count != 1 || authentication[0].GetResultCount() != 1)
                throw new InvalidOperationException("An auth-failure report must describe exactly one authentication method result.");
        }
    }

    static void ValidateRepeated(EmailReportFieldBlock fields, string name)
    {
        List<EmailHeader> values = fields.GetFieldsNamed(name);
        int index = 0;
        while (index < values.Count)
        {
            if (!IsRepeatedValueValid(name, values[index]))
                throw new InvalidOperationException("A repeatable feedback-report field is malformed.");
            index++;
        }
    }

    static bool IsRepeatedValueValid(string name, EmailHeader field)
    {
        if (name == "Authentication-Results")
        {
            EmailAuthenticationResultsParseResult parsed = new EmailAuthenticationResultsParser().ParseWithInputAndMode(field.GetUnfoldedValue(), EmailStructuredValueParseMode.Strict);
            return parsed.IsSuccess() && parsed.IsComplete() && parsed.HasValue();
        }

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

    static void ValidateRequired(EmailReportFieldBlock fields, string name, bool valid)
    {
        if (fields.GetFieldsNamed(name).Count != 1 || !valid)
            throw new InvalidOperationException("The feedback report requires exactly one valid " + name + " field.");
    }

    static void ValidateOptional(EmailReportFieldBlock fields, string name, bool valid)
    {
        int count = fields.GetFieldsNamed(name).Count;
        if (count > 1 || count == 1 && !valid)
            throw new InvalidOperationException("An optional feedback-report field is duplicated or malformed.");
    }

    static void ValidateAscii(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 0 || value[index] > 127)
                throw new InvalidOperationException("Feedback-report fields must contain NUL-free ASCII bytes.");
            index++;
        }
    }

    static byte[] Normalize(byte[] value, int maximumBytes)
    {
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current == 13 || current == 10)
            {
                if (current == 13 && index + 1 < value.Length && value[index + 1] == 10)
                    index++;
                Append(output, 13, maximumBytes);
                Append(output, 10, maximumBytes);
            }
            else
                Append(output, current, maximumBytes);
            index++;
        }

        if (output.Count == 0 || output[output.Count - 1] != 10)
        {
            Append(output, 13, maximumBytes);
            Append(output, 10, maximumBytes);
        }

        return output.ToArray();
    }

    static void Append(List<byte> output, byte value, int maximumBytes)
    {
        if (output.Count >= maximumBytes)
            throw new EmailLimitException("MaxReportBytes", maximumBytes, output.Count + 1);
        output.Add(value);
    }
}
