using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a delivery status.
/// </summary>
public class EmailDeliveryStatus : IEmailDeliveryStatusContract
{
    bool internationalized;
    EmailDeliveryStatusMessage? messageFields;
    List<EmailDeliveryStatusRecipient>? recipients;
    EmailMultipartReport? multipartReport;
    MimeEntity? originalContentPart;
    EmailHeaders? originalHeaders;
    EmailMessage? originalMessage;
    /// <summary>
    /// Creates a delivery status with its enclosing multipart context.
    /// </summary>
    /// <param name = "internationalized">Whether to enable the internationalized behavior.</param>
    /// <param name = "messageFields">The message fields used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    public EmailDeliveryStatus(bool internationalized, EmailDeliveryStatusMessage messageFields, List<EmailDeliveryStatusRecipient> recipients)
    {
        Initialize(internationalized, messageFields, recipients, null, null, null, null);
    }

    internal static EmailDeliveryStatus CreateImplementationWithInternationalizedAndMessageFieldsAndRecipients(bool internationalized, EmailDeliveryStatusMessage messageFields, List<EmailDeliveryStatusRecipient> recipients)
    {
        return new EmailDeliveryStatus(internationalized, messageFields, recipients);
    }

    internal static EmailDeliveryStatus CreateImplementationWithInternationalizedAndMessageFieldsAndRecipientsAndMultipartReportAndOriginalContentPartAndOriginalHeadersAndOriginalMessage(bool internationalized, EmailDeliveryStatusMessage messageFields, List<EmailDeliveryStatusRecipient> recipients, EmailMultipartReport multipartReport, MimeEntity? originalContentPart, EmailHeaders? originalHeaders, EmailMessage? originalMessage)
    {
        EmailDeliveryStatus value = new EmailDeliveryStatus(internationalized, messageFields, recipients);
        value.Initialize(internationalized, messageFields, recipients, multipartReport, originalContentPart, originalHeaders, originalMessage);
        return value;
    }

    void Initialize(bool internationalized, EmailDeliveryStatusMessage suppliedMessageFields, List<EmailDeliveryStatusRecipient> suppliedRecipients, EmailMultipartReport? suppliedMultipartReport, MimeEntity? suppliedOriginalContentPart, EmailHeaders? suppliedOriginalHeaders, EmailMessage? suppliedOriginalMessage)
    {
        if (suppliedMessageFields == null)
            throw new ArgumentNullException("suppliedMessageFields");
        if (suppliedRecipients == null)
            throw new ArgumentNullException("suppliedRecipients");
        int index = 0;
        while (index < suppliedRecipients.Count)
        {
            if (suppliedRecipients[index] == null)
                throw new ArgumentException("Recipient blocks cannot contain null entries.", "suppliedRecipients");
            index++;
        }

        this.internationalized = internationalized;
        messageFields = suppliedMessageFields;
        recipients = new List<EmailDeliveryStatusRecipient>(suppliedRecipients);
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
    /// Gets the message fields.
    /// </summary>
    /// <returns>The current message fields.</returns>
    public EmailDeliveryStatusMessage? GetMessageFields()
    {
        return messageFields;
    }

    /// <summary>
    /// Gets the recipient count.
    /// </summary>
    /// <returns>The current recipient count.</returns>
    public int GetRecipientCount()
    {
        return (recipients ?? throw new InvalidOperationException()).Count;
    }

    /// <summary>
    /// Gets the recipient at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested recipient.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDeliveryStatusRecipient GetRecipientAt(int index)
    {
        if (index < 0 || index >= (recipients ?? throw new InvalidOperationException()).Count)
            throw new ArgumentOutOfRangeException("index");
        return recipients[index];
    }

    /// <summary>
    /// Gets the recipients.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipients.</returns>
    public List<EmailDeliveryStatusRecipient> GetRecipients()
    {
        return new List<EmailDeliveryStatusRecipient>((recipients ?? throw new InvalidOperationException()));
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
