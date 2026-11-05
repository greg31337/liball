using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailComposer"/>.
/// </summary>
public interface IEmailComposerContract
{
    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    EmailComposerOptions GetOptions();
    /// <summary>
    /// Sets the sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetSender(EmailAddress value);
    /// <summary>
    /// Clears the stored sender value or values.
    /// </summary>
    void ClearSender();
    /// <summary>
    /// Sets the from used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetFrom(EmailAddress value);
    /// <summary>
    /// Adds a from to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddFrom(EmailAddress value);
    /// <summary>
    /// Clears the stored from value or values.
    /// </summary>
    void ClearFrom();
    /// <summary>
    /// Adds a reply to to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddReplyTo(EmailAddress value);
    /// <summary>
    /// Clears the stored reply to value or values.
    /// </summary>
    void ClearReplyTo();
    /// <summary>
    /// Adds a to to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddTo(EmailAddress value);
    /// <summary>
    /// Clears the stored to value or values.
    /// </summary>
    void ClearTo();
    /// <summary>
    /// Adds a cc to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddCc(EmailAddress value);
    /// <summary>
    /// Clears the stored cc value or values.
    /// </summary>
    void ClearCc();
    /// <summary>
    /// Adds a bcc to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddBcc(EmailAddress value);
    /// <summary>
    /// Clears the stored bcc value or values.
    /// </summary>
    void ClearBcc();
    /// <summary>
    /// Sets the envelope sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetEnvelopeSender(EmailAddress value);
    /// <summary>
    /// Clears the stored envelope sender value or values.
    /// </summary>
    void ClearEnvelopeSender();
    /// <summary>
    /// Sets the null envelope sender used by this instance.
    /// </summary>
    void SetNullEnvelopeSender();
    /// <summary>
    /// Adds an envelope recipient to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddEnvelopeRecipient(EmailAddress value);
    /// <summary>
    /// Clears the stored envelope recipients value or values.
    /// </summary>
    void ClearEnvelopeRecipients();
    /// <summary>
    /// Sets the subject used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetSubject(string value);
    /// <summary>
    /// Clears the stored subject value or values.
    /// </summary>
    void ClearSubject();
    /// <summary>
    /// Sets the date used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetDate(EmailDateTime value);
    /// <summary>
    /// Clears the stored date value or values.
    /// </summary>
    void ClearDate();
    /// <summary>
    /// Sets the message ID used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMessageId(EmailMessageId value);
    /// <summary>
    /// Clears the stored message ID value or values.
    /// </summary>
    void ClearMessageId();
    /// <summary>
    /// Sets the in reply to used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetInReplyTo(EmailMessageId value);
    /// <summary>
    /// Clears the stored in reply to value or values.
    /// </summary>
    void ClearInReplyTo();
    /// <summary>
    /// Adds a reference to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void AddReference(EmailMessageId value);
    /// <summary>
    /// Clears the stored references value or values.
    /// </summary>
    void ClearReferences();
    /// <summary>
    /// Sets the priority used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetPriority(EmailPriorityKind value);
    /// <summary>
    /// Clears the stored priority value or values.
    /// </summary>
    void ClearPriority();
    /// <summary>
    /// Sets the auto submitted used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void SetAutoSubmitted(string value);
    /// <summary>
    /// Clears the stored auto submitted value or values.
    /// </summary>
    void ClearAutoSubmitted();
    /// <summary>
    /// Adds a custom field to the current collection or model.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void AddCustomField(string name, string value);
    /// <summary>
    /// Removes the matching custom fields value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    int RemoveCustomFields(string name);
    /// <summary>
    /// Clears the stored custom fields value or values.
    /// </summary>
    void ClearCustomFields();
    /// <summary>
    /// Sets the text used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetText(string value);
    /// <summary>
    /// Sets the text used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    void SetTextWithCharset(string value, string charset);
    /// <summary>
    /// Clears the stored text value or values.
    /// </summary>
    void ClearText();
    /// <summary>
    /// Sets the HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetHtml(string? value);
    /// <summary>
    /// Sets the HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    void SetHtmlWithCharset(string? value, string charset);
    /// <summary>
    /// Clears the stored HTML value or values.
    /// </summary>
    void ClearHtml();
    /// <summary>
    /// Sets the amp HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetAmpHtml(string value);
    /// <summary>
    /// Sets the amp HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    void SetAmpHtmlWithCharset(string value, string charset);
    /// <summary>
    /// Clears the stored amp HTML value or values.
    /// </summary>
    void ClearAmpHtml();
    /// <summary>
    /// Sets the watch HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetWatchHtml(string value);
    /// <summary>
    /// Sets the watch HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    void SetWatchHtmlWithCharset(string value, string charset);
    /// <summary>
    /// Clears the stored watch HTML value or values.
    /// </summary>
    void ClearWatchHtml();
    /// <summary>
    /// Sets the calendar used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "method">The method used by the operation.</param>
    void SetCalendar(string value, string method);
    /// <summary>
    /// Sets the calendar used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "method">The method used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    void SetCalendarWithCharset(string value, string method, string charset);
    /// <summary>
    /// Clears the stored calendar value or values.
    /// </summary>
    void ClearCalendar();
    /// <summary>
    /// Sets the content used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    void SetContent(string value, string type, string subtype, string charset);
    /// <summary>
    /// Adds an alternative to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    void AddAlternative(string value, string type, string subtype, string charset);
    /// <summary>
    /// Adds an alternative to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddAlternativeEntity(MimeEntity value);
    /// <summary>
    /// Clears the stored alternatives value or values.
    /// </summary>
    void ClearAlternatives();
    /// <summary>
    /// Sets the raw entity used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetRawEntity(MimeEntity? value);
    /// <summary>
    /// Clears the stored raw entity value or values.
    /// </summary>
    void ClearRawEntity();
    /// <summary>
    /// Clears the stored body value or values.
    /// </summary>
    void ClearBody();
    /// <summary>
    /// Adds an attachment to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddAttachment(EmailAttachmentSource value);
    /// <summary>
    /// Clears the stored attachments value or values.
    /// </summary>
    void ClearAttachments();
    /// <summary>
    /// Adds a related resource to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddRelatedResource(EmailAttachmentSource value);
    /// <summary>
    /// Clears the stored related resources value or values.
    /// </summary>
    void ClearRelatedResources();
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <returns>The message produced by the operation.</returns>
    EmailMessage Build();
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailValidationResult Validate();
}
