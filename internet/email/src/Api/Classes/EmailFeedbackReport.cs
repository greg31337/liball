using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Represents a feedback report.
/// </summary>
public class EmailFeedbackReport : IEmailFeedbackReportContract
{
    EmailReportFieldBlock? fields;
    EmailMultipartReport? multipartReport;
    MimeEntity? originalContentPart;
    EmailHeaders? originalHeaders;
    EmailMessage? originalMessage;
    /// <summary>
    /// Creates a feedback report with its enclosing multipart context.
    /// </summary>
    /// <param name = "fields">The fields used by the operation.</param>
    public EmailFeedbackReport(EmailReportFieldBlock fields)
    {
        Initialize(fields, null, null, null, null);
    }

    internal static EmailFeedbackReport CreateImplementationWithFields(EmailReportFieldBlock fields)
    {
        return new EmailFeedbackReport(fields);
    }

    internal static EmailFeedbackReport CreateImplementationWithFieldsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(EmailReportFieldBlock fields, EmailMultipartReport multipartReport, MimeEntity? originalContentPart, EmailHeaders? originalHeaders, EmailMessage? originalMessage)
    {
        EmailFeedbackReport value = new EmailFeedbackReport(fields);
        value.Initialize(fields, multipartReport, originalContentPart, originalHeaders, originalMessage);
        return value;
    }

    void Initialize(EmailReportFieldBlock suppliedFields, EmailMultipartReport? suppliedMultipartReport, MimeEntity? suppliedOriginalContentPart, EmailHeaders? suppliedOriginalHeaders, EmailMessage? suppliedOriginalMessage)
    {
        if (suppliedFields == null)
            throw new ArgumentNullException("suppliedFields");
        fields = suppliedFields;
        multipartReport = suppliedMultipartReport;
        originalContentPart = suppliedOriginalContentPart;
        originalHeaders = suppliedOriginalHeaders == null ? null : suppliedOriginalHeaders.Clone();
        originalMessage = suppliedOriginalMessage;
    }

    /// <summary>
    /// Gets the field block.
    /// </summary>
    /// <returns>The current field block.</returns>
    public EmailReportFieldBlock? GetFieldBlock()
    {
        return fields;
    }

    /// <summary>
    /// Determines whether the feedback type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the feedback type is available; otherwise, <c>false</c>.</returns>
    public bool HasFeedbackType()
    {
        return GetFeedbackTypeName() != null;
    }

    /// <summary>
    /// Gets the feedback type name.
    /// </summary>
    /// <returns>The current feedback type name.</returns>
    public string? GetFeedbackTypeName()
    {
        return EmailFeedbackReportValueParser.GetToken((fields ?? throw new InvalidOperationException()).GetFirstField("Feedback-Type"));
    }

    /// <summary>
    /// Gets the feedback type.
    /// </summary>
    /// <returns>The current feedback type.</returns>
    public EmailFeedbackType GetFeedbackType()
    {
        return EmailFeedbackReportValueParser.ParseFeedbackType((fields ?? throw new InvalidOperationException()).GetFirstField("Feedback-Type"));
    }

    /// <summary>
    /// Determines whether the user agent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the user agent is available; otherwise, <c>false</c>.</returns>
    public bool HasUserAgent()
    {
        return GetUserAgent() != null;
    }

    /// <summary>
    /// Gets the user agent.
    /// </summary>
    /// <returns>The current user agent.</returns>
    public string? GetUserAgent()
    {
        return EmailFeedbackReportValueParser.GetPrintableAscii((fields ?? throw new InvalidOperationException()).GetFirstField("User-Agent"));
    }

    /// <summary>
    /// Determines whether the version is present or available.
    /// </summary>
    /// <returns><c>true</c> when the version is available; otherwise, <c>false</c>.</returns>
    public bool HasVersion()
    {
        return EmailFeedbackReportValueParser.ParsePositiveUInt32((fields ?? throw new InvalidOperationException()).GetFirstField("Version")) != 0;
    }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    public uint GetVersion()
    {
        return EmailFeedbackReportValueParser.ParsePositiveUInt32((fields ?? throw new InvalidOperationException()).GetFirstField("Version"));
    }

    /// <summary>
    /// Determines whether the original envelope ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original envelope ID is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalEnvelopeId()
    {
        return GetOriginalEnvelopeId() != null;
    }

    /// <summary>
    /// Gets the original envelope ID.
    /// </summary>
    /// <returns>The current original envelope ID.</returns>
    public string? GetOriginalEnvelopeId()
    {
        return EmailFeedbackReportValueParser.GetNonemptyText((fields ?? throw new InvalidOperationException()).GetFirstField("Original-Envelope-Id"));
    }

    /// <summary>
    /// Determines whether the original mail from is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original mail from is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalMailFrom()
    {
        return GetOriginalMailFrom() != null;
    }

    /// <summary>
    /// Gets the original mail from.
    /// </summary>
    /// <returns>The current original mail from.</returns>
    public EmailFeedbackPath? GetOriginalMailFrom()
    {
        return EmailFeedbackReportValueParser.ParsePath((fields ?? throw new InvalidOperationException()).GetFirstField("Original-Mail-From"), true);
    }

    /// <summary>
    /// Determines whether the arrival date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the arrival date is available; otherwise, <c>false</c>.</returns>
    public bool HasArrivalDate()
    {
        return GetArrivalDate() != null;
    }

    /// <summary>
    /// Gets the arrival date.
    /// </summary>
    /// <returns>The current arrival date.</returns>
    public EmailDateTime? GetArrivalDate()
    {
        EmailHeader? arrival = (fields ?? throw new InvalidOperationException()).GetFirstField("Arrival-Date");
        return EmailFeedbackReportValueParser.ParseDate(arrival ?? fields.GetFirstField("Received-Date"));
    }

    /// <summary>
    /// Determines whether the current value or state uses historic received date.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses historic received date; otherwise, <c>false</c>.</returns>
    public bool UsesHistoricReceivedDate()
    {
        return (fields ?? throw new InvalidOperationException()).GetFirstField("Arrival-Date") == null && fields.GetFirstField("Received-Date") != null;
    }

    /// <summary>
    /// Determines whether the reporting mta is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reporting mta is available; otherwise, <c>false</c>.</returns>
    public bool HasReportingMta()
    {
        return GetReportingMta() != null;
    }

    /// <summary>
    /// Gets the reporting mta.
    /// </summary>
    /// <returns>The current reporting mta.</returns>
    public EmailFeedbackTypedValue? GetReportingMta()
    {
        return EmailFeedbackReportValueParser.ParseTyped((fields ?? throw new InvalidOperationException()).GetFirstField("Reporting-MTA"));
    }

    /// <summary>
    /// Determines whether the source ip is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source ip is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceIp()
    {
        return GetSourceIp() != null;
    }

    /// <summary>
    /// Gets the source ip.
    /// </summary>
    /// <returns>The current source ip.</returns>
    public IPAddress? GetSourceIp()
    {
        return EmailFeedbackReportValueParser.ParseIp((fields ?? throw new InvalidOperationException()).GetFirstField("Source-IP"));
    }

    /// <summary>
    /// Determines whether the source port is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source port is available; otherwise, <c>false</c>.</returns>
    public bool HasSourcePort()
    {
        return EmailFeedbackReportValueParser.ParseSourcePort((fields ?? throw new InvalidOperationException()).GetFirstField("Source-Port")) != 0;
    }

    /// <summary>
    /// Gets the source port.
    /// </summary>
    /// <returns>The current source port.</returns>
    public int GetSourcePort()
    {
        return EmailFeedbackReportValueParser.ParseSourcePort((fields ?? throw new InvalidOperationException()).GetFirstField("Source-Port"));
    }

    /// <summary>
    /// Determines whether the incidents is present or available.
    /// </summary>
    /// <returns><c>true</c> when the incidents is available; otherwise, <c>false</c>.</returns>
    public bool HasIncidents()
    {
        return EmailFeedbackReportValueParser.ParsePositiveUInt32((fields ?? throw new InvalidOperationException()).GetFirstField("Incidents")) != 0;
    }

    /// <summary>
    /// Gets the incidents.
    /// </summary>
    /// <returns>The current incidents.</returns>
    public uint GetIncidents()
    {
        uint value = EmailFeedbackReportValueParser.ParsePositiveUInt32((fields ?? throw new InvalidOperationException()).GetFirstField("Incidents"));
        return value == 0 ? 1 : value;
    }

    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current authentication results.</returns>
    public List<EmailAuthenticationResults> GetAuthenticationResults()
    {
        return EmailFeedbackReportValueParser.ParseAuthenticationResults(fields);
    }

    /// <summary>
    /// Gets the authentication failure name.
    /// </summary>
    /// <returns>The current authentication failure name.</returns>
    public string? GetAuthenticationFailureName()
    {
        return EmailFeedbackReportValueParser.GetToken((fields ?? throw new InvalidOperationException()).GetFirstField("Auth-Failure"));
    }

    /// <summary>
    /// Gets the authentication failure.
    /// </summary>
    /// <returns>The current authentication failure.</returns>
    public EmailFeedbackAuthenticationFailure GetAuthenticationFailure()
    {
        return EmailFeedbackReportValueParser.ParseAuthenticationFailure((fields ?? throw new InvalidOperationException()).GetFirstField("Auth-Failure"));
    }

    /// <summary>
    /// Gets the delivery result name.
    /// </summary>
    /// <returns>The current delivery result name.</returns>
    public string? GetDeliveryResultName()
    {
        return EmailFeedbackReportValueParser.GetToken((fields ?? throw new InvalidOperationException()).GetFirstField("Delivery-Result"));
    }

    /// <summary>
    /// Gets the delivery result.
    /// </summary>
    /// <returns>The current delivery result.</returns>
    public EmailFeedbackDeliveryResult GetDeliveryResult()
    {
        return EmailFeedbackReportValueParser.ParseDeliveryResult((fields ?? throw new InvalidOperationException()).GetFirstField("Delivery-Result"));
    }

    /// <summary>
    /// Gets the DKIM domain.
    /// </summary>
    /// <returns>The current DKIM domain.</returns>
    public string? GetDkimDomain()
    {
        return EmailFeedbackReportValueParser.ParseDomain((fields ?? throw new InvalidOperationException()).GetFirstField("DKIM-Domain"));
    }

    /// <summary>
    /// Gets the DKIM identity.
    /// </summary>
    /// <returns>The current DKIM identity.</returns>
    public string? GetDkimIdentity()
    {
        return EmailFeedbackReportValueParser.GetNonemptyText((fields ?? throw new InvalidOperationException()).GetFirstField("DKIM-Identity"));
    }

    /// <summary>
    /// Gets the DKIM selector.
    /// </summary>
    /// <returns>The current DKIM selector.</returns>
    public string? GetDkimSelector()
    {
        return EmailFeedbackReportValueParser.GetToken((fields ?? throw new InvalidOperationException()).GetFirstField("DKIM-Selector"));
    }

    /// <summary>
    /// Gets the spf dns values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current spf dns values.</returns>
    public List<string> GetSpfDnsValues()
    {
        return EmailFeedbackReportValueParser.GetTextValues(fields, "SPF-DNS");
    }

    /// <summary>
    /// Gets the original recipients.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original recipients.</returns>
    public List<EmailFeedbackPath> GetOriginalRecipients()
    {
        return EmailFeedbackReportValueParser.ParsePaths(fields, "Original-Rcpt-To", false);
    }

    /// <summary>
    /// Gets the reported domains.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current reported domains.</returns>
    public List<string> GetReportedDomains()
    {
        return EmailFeedbackReportValueParser.ParseDomains(fields);
    }

    /// <summary>
    /// Gets the reported uris.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current reported uris.</returns>
    public List<Uri> GetReportedUris()
    {
        return EmailFeedbackReportValueParser.ParseUris(fields);
    }

    /// <summary>
    /// Gets the extension fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension fields.</returns>
    public List<EmailHeader> GetExtensionFields()
    {
        return EmailFeedbackReportValueParser.GetExtensions(fields);
    }

    /// <summary>
    /// Determines whether the multipart report is present or available.
    /// </summary>
    /// <returns><c>true</c> when the multipart report is available; otherwise, <c>false</c>.</returns>
    public bool HasMultipartReport()
    {
        return multipartReport != null;
    }

    /// <summary>
    /// Gets the multipart report.
    /// </summary>
    /// <returns>The current multipart report.</returns>
    public EmailMultipartReport? GetMultipartReport()
    {
        return multipartReport;
    }

    /// <summary>
    /// Determines whether the original content part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original content part is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalContentPart()
    {
        return originalContentPart != null;
    }

    /// <summary>
    /// Gets the original content part.
    /// </summary>
    /// <returns>The current original content part.</returns>
    public MimeEntity? GetOriginalContentPart()
    {
        return originalContentPart;
    }

    /// <summary>
    /// Determines whether the original headers is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original headers is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalHeaders()
    {
        return originalHeaders != null;
    }

    /// <summary>
    /// Gets the original headers.
    /// </summary>
    /// <returns>The current original headers.</returns>
    public EmailHeaders? GetOriginalHeaders()
    {
        return originalHeaders == null ? null : originalHeaders.Clone();
    }

    /// <summary>
    /// Determines whether the original message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original message is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalMessage()
    {
        return originalMessage != null;
    }

    /// <summary>
    /// Gets the original message.
    /// </summary>
    /// <returns>The current original message.</returns>
    public EmailMessage? GetOriginalMessage()
    {
        return originalMessage;
    }
}
