using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a disposition notification.
/// </summary>
public class EmailDispositionNotification : IEmailDispositionNotificationContract
{
    bool internationalized;
    EmailReportFieldBlock? fields;
    EmailMultipartReport? multipartReport;
    MimeEntity? originalContentPart;
    EmailHeaders? originalHeaders;
    EmailMessage? originalMessage;
    /// <summary>
    /// Creates a disposition notification with its enclosing multipart context.
    /// </summary>
    /// <param name = "internationalized">Whether to enable the internationalized behavior.</param>
    /// <param name = "fields">The fields used by the operation.</param>
    public EmailDispositionNotification(bool internationalized, EmailReportFieldBlock fields)
    {
        Initialize(internationalized, fields, null, null, null, null);
    }

    internal static EmailDispositionNotification CreateImplementationWithInternationalizedAndFields(bool internationalized, EmailReportFieldBlock fields)
    {
        return new EmailDispositionNotification(internationalized, fields);
    }

    internal static EmailDispositionNotification CreateImplementationWithInternationalizedAndFieldsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(bool internationalized, EmailReportFieldBlock fields, EmailMultipartReport multipartReport, MimeEntity? originalContentPart, EmailHeaders? originalHeaders, EmailMessage? originalMessage)
    {
        EmailDispositionNotification value = new EmailDispositionNotification(internationalized, fields);
        value.Initialize(internationalized, fields, multipartReport, originalContentPart, originalHeaders, originalMessage);
        return value;
    }

    void Initialize(bool suppliedInternationalized, EmailReportFieldBlock suppliedFields, EmailMultipartReport? suppliedMultipartReport, MimeEntity? suppliedOriginalContentPart, EmailHeaders? suppliedOriginalHeaders, EmailMessage? suppliedOriginalMessage)
    {
        if (suppliedFields == null)
            throw new ArgumentNullException("suppliedFields");
        internationalized = suppliedInternationalized;
        fields = suppliedFields;
        multipartReport = suppliedMultipartReport;
        originalContentPart = suppliedOriginalContentPart;
        originalHeaders = suppliedOriginalHeaders == null ? null : suppliedOriginalHeaders.Clone();
        originalMessage = suppliedOriginalMessage;
    }

    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    public bool IsInternationalized()
    {
        return internationalized;
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
    /// Determines whether the reporting user agent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reporting user agent is available; otherwise, <c>false</c>.</returns>
    public bool HasReportingUserAgent()
    {
        return GetReportingUserAgent() != null;
    }

    /// <summary>
    /// Gets the reporting user agent.
    /// </summary>
    /// <returns>The current reporting user agent.</returns>
    public EmailDispositionNotificationUserAgent? GetReportingUserAgent()
    {
        return EmailDispositionNotificationValueParser.ParseUserAgent((fields ?? throw new InvalidOperationException()).GetFirstField("Reporting-UA"));
    }

    /// <summary>
    /// Determines whether the gateway is present or available.
    /// </summary>
    /// <returns><c>true</c> when the gateway is available; otherwise, <c>false</c>.</returns>
    public bool HasGateway()
    {
        return GetGateway() != null;
    }

    /// <summary>
    /// Gets the gateway.
    /// </summary>
    /// <returns>The current gateway.</returns>
    public EmailDispositionNotificationTypedValue? GetGateway()
    {
        return EmailDispositionNotificationValueParser.ParseTyped((fields ?? throw new InvalidOperationException()).GetFirstField("MDN-Gateway"));
    }

    /// <summary>
    /// Determines whether the original recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original recipient is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalRecipient()
    {
        return GetOriginalRecipient() != null;
    }

    /// <summary>
    /// Gets the original recipient.
    /// </summary>
    /// <returns>The current original recipient.</returns>
    public EmailDispositionNotificationTypedValue? GetOriginalRecipient()
    {
        return EmailDispositionNotificationValueParser.ParseTyped((fields ?? throw new InvalidOperationException()).GetFirstField("Original-Recipient"));
    }

    /// <summary>
    /// Determines whether the final recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the final recipient is available; otherwise, <c>false</c>.</returns>
    public bool HasFinalRecipient()
    {
        return GetFinalRecipient() != null;
    }

    /// <summary>
    /// Gets the final recipient.
    /// </summary>
    /// <returns>The current final recipient.</returns>
    public EmailDispositionNotificationTypedValue? GetFinalRecipient()
    {
        return EmailDispositionNotificationValueParser.ParseTyped((fields ?? throw new InvalidOperationException()).GetFirstField("Final-Recipient"));
    }

    /// <summary>
    /// Determines whether the original message ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original message ID is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalMessageId()
    {
        return GetOriginalMessageId() != null;
    }

    /// <summary>
    /// Gets the original message ID.
    /// </summary>
    /// <returns>The current original message ID.</returns>
    public EmailMessageId? GetOriginalMessageId()
    {
        return EmailDispositionNotificationValueParser.ParseMessageId((fields ?? throw new InvalidOperationException()).GetFirstField("Original-Message-ID"));
    }

    /// <summary>
    /// Determines whether the disposition is present or available.
    /// </summary>
    /// <returns><c>true</c> when the disposition is available; otherwise, <c>false</c>.</returns>
    public bool HasDisposition()
    {
        return GetDisposition() != null;
    }

    /// <summary>
    /// Gets the disposition.
    /// </summary>
    /// <returns>The current disposition.</returns>
    public EmailDispositionNotificationDisposition? GetDisposition()
    {
        return EmailDispositionNotificationValueParser.ParseDisposition((fields ?? throw new InvalidOperationException()).GetFirstField("Disposition"));
    }

    /// <summary>
    /// Gets the failure values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current failure values.</returns>
    public List<string> GetFailureValues()
    {
        return EmailDispositionNotificationValueParser.GetTextValues(fields, "Failure");
    }

    /// <summary>
    /// Gets the error values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current error values.</returns>
    public List<string> GetErrorValues()
    {
        return EmailDispositionNotificationValueParser.GetTextValues(fields, "Error");
    }

    /// <summary>
    /// Gets the warning values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current warning values.</returns>
    public List<string> GetWarningValues()
    {
        return EmailDispositionNotificationValueParser.GetTextValues(fields, "Warning");
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
        return EmailDispositionNotificationValueParser.GetExtensions(fields);
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
