using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Builds a message and MIME tree from addresses, headers, bodies, and attachments.
/// </summary>
public class EmailComposer : IEmailComposerContract
{
    EmailComposerOptions? options;
    EmailAddress? sender;
    List<EmailAddress>? from;
    List<EmailAddress>? replyTo;
    List<EmailAddress>? to;
    List<EmailAddress>? cc;
    List<EmailAddress>? bcc;
    EmailAddress? envelopeSender;
    bool nullEnvelopeSender;
    List<EmailAddress>? envelopeRecipients;
    string? subject;
    EmailDateTime? date;
    EmailMessageId? messageId;
    EmailMessageId? inReplyTo;
    List<EmailMessageId>? references;
    bool priorityPresent;
    EmailPriorityKind priority;
    string? autoSubmitted;
    List<EmailHeader>? customFields;
    MimeEntity? textBody;
    MimeEntity? watchHtmlBody;
    MimeEntity? ampHtmlBody;
    MimeEntity? htmlBody;
    MimeEntity? calendarBody;
    MimeEntity? explicitBody;
    List<MimeEntity>? alternativeBodies;
    MimeEntity? rawBody;
    List<EmailAttachmentSource>? attachments;
    List<EmailAttachmentSource>? relatedResources;
    /// <summary>
    /// Creates a composer using the supplied policy.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailComposer(EmailComposerOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Initialize(options.Clone());
    }

    internal static EmailComposer CreateImplementationWith()
    {
        return new EmailComposer(new EmailComposerOptions());
    }

    internal static EmailComposer CreateImplementationWithOptions(EmailComposerOptions options)
    {
        return new EmailComposer(options);
    }

    void Initialize(EmailComposerOptions suppliedOptions)
    {
        options = suppliedOptions;
        sender = null;
        from = new List<EmailAddress>();
        replyTo = new List<EmailAddress>();
        to = new List<EmailAddress>();
        cc = new List<EmailAddress>();
        bcc = new List<EmailAddress>();
        envelopeSender = null;
        nullEnvelopeSender = false;
        envelopeRecipients = new List<EmailAddress>();
        subject = null;
        date = null;
        messageId = null;
        inReplyTo = null;
        references = new List<EmailMessageId>();
        priorityPresent = false;
        priority = EmailPriorityKind.Normal;
        autoSubmitted = null;
        customFields = new List<EmailHeader>();
        textBody = new EmailEntityFactory().CreateTextWithValue("");
        watchHtmlBody = null;
        ampHtmlBody = null;
        htmlBody = null;
        calendarBody = null;
        explicitBody = null;
        alternativeBodies = new List<MimeEntity>();
        rawBody = null;
        attachments = new List<EmailAttachmentSource>();
        relatedResources = new List<EmailAttachmentSource>();
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    public EmailComposerOptions GetOptions()
    {
        return (options ?? throw new InvalidOperationException()).Clone();
    }

    /// <summary>
    /// Sets the sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetSender(EmailAddress value)
    {
        ValidateAddress(value, "value");
        EnsureAddressCapacity(sender == null ? 0 : 1);
        sender = value;
    }

    /// <summary>
    /// Clears the stored sender value or values.
    /// </summary>
    public void ClearSender()
    {
        sender = null;
    }

    /// <summary>
    /// Sets the from used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetFrom(EmailAddress value)
    {
        ValidateAddress(value, "value");
        EnsureAddressCapacity((from ?? throw new InvalidOperationException()).Count);
        from.Clear();
        from.Add(value);
    }

    /// <summary>
    /// Adds a from to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddFrom(EmailAddress value)
    {
        AddAddress(from, value);
    }

    /// <summary>
    /// Clears the stored from value or values.
    /// </summary>
    public void ClearFrom()
    {
        (from ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Adds a reply to to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddReplyTo(EmailAddress value)
    {
        AddAddress(replyTo, value);
    }

    /// <summary>
    /// Clears the stored reply to value or values.
    /// </summary>
    public void ClearReplyTo()
    {
        (replyTo ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Adds a to to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddTo(EmailAddress value)
    {
        AddAddress(to, value);
    }

    /// <summary>
    /// Clears the stored to value or values.
    /// </summary>
    public void ClearTo()
    {
        (to ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Adds a cc to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddCc(EmailAddress value)
    {
        AddAddress(cc, value);
    }

    /// <summary>
    /// Clears the stored cc value or values.
    /// </summary>
    public void ClearCc()
    {
        (cc ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Adds a bcc to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddBcc(EmailAddress value)
    {
        AddAddress(bcc, value);
    }

    /// <summary>
    /// Clears the stored bcc value or values.
    /// </summary>
    public void ClearBcc()
    {
        (bcc ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Sets the envelope sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetEnvelopeSender(EmailAddress value)
    {
        ValidateAddress(value, "value");
        EnsureAddressCapacity(envelopeSender == null ? 0 : 1);
        envelopeSender = value;
        nullEnvelopeSender = false;
    }

    /// <summary>
    /// Clears the stored envelope sender value or values.
    /// </summary>
    public void ClearEnvelopeSender()
    {
        envelopeSender = null;
        nullEnvelopeSender = false;
    }

    /// <summary>
    /// Sets the null envelope sender used by this instance.
    /// </summary>
    public void SetNullEnvelopeSender()
    {
        envelopeSender = null;
        nullEnvelopeSender = true;
    }

    /// <summary>
    /// Adds an envelope recipient to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddEnvelopeRecipient(EmailAddress value)
    {
        AddAddress(envelopeRecipients, value);
    }

    /// <summary>
    /// Clears the stored envelope recipients value or values.
    /// </summary>
    public void ClearEnvelopeRecipients()
    {
        (envelopeRecipients ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Sets the subject used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetSubject(string value)
    {
        ValidateHeaderText(value, "value");
        FormatUnstructured("Subject", value);
        subject = value;
    }

    /// <summary>
    /// Clears the stored subject value or values.
    /// </summary>
    public void ClearSubject()
    {
        subject = null;
    }

    /// <summary>
    /// Sets the date used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetDate(EmailDateTime value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        EmailDateFormatResult formatted = new EmailDateFormatter().FormatWithValue(value);
        if (!formatted.IsSuccess())
        {
            throw new ArgumentException("The date cannot be formatted as a safe Internet message date.", "value");
        }

        date = value;
    }

    /// <summary>
    /// Clears the stored date value or values.
    /// </summary>
    public void ClearDate()
    {
        date = null;
    }

    /// <summary>
    /// Sets the message ID used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMessageId(EmailMessageId value)
    {
        ValidateIdentifier(value, "value");
        messageId = value;
    }

    /// <summary>
    /// Clears the stored message ID value or values.
    /// </summary>
    public void ClearMessageId()
    {
        messageId = null;
    }

    /// <summary>
    /// Sets the in reply to used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetInReplyTo(EmailMessageId value)
    {
        ValidateIdentifier(value, "value");
        inReplyTo = value;
    }

    /// <summary>
    /// Clears the stored in reply to value or values.
    /// </summary>
    public void ClearInReplyTo()
    {
        inReplyTo = null;
    }

    /// <summary>
    /// Adds a reference to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void AddReference(EmailMessageId value)
    {
        ValidateIdentifier(value, "value");
        List<EmailMessageId> prospective = new List<EmailMessageId>((references ?? throw new InvalidOperationException()));
        prospective.Add(value);
        EmailMessageIdList identifiers = new EmailMessageIdList(prospective, new byte[0]);
        EmailMessageIdFormatResult formatted = new EmailMessageIdFormatter().FormatListWithIdentifiersAndOptions(identifiers, CreateIdentifierFormatOptions(12));
        if (!formatted.IsSuccess())
        {
            throw new EmailLimitException("MaxComposerHeaderValueBytes", (options ?? throw new InvalidOperationException()).GetMaximumHeaderValueBytes(), (long)options.GetMaximumHeaderValueBytes() + 1);
        }

        references.Add(value);
    }

    /// <summary>
    /// Clears the stored references value or values.
    /// </summary>
    public void ClearReferences()
    {
        (references ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Sets the priority used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetPriority(EmailPriorityKind value)
    {
        if (value < EmailPriorityKind.NonUrgent || value > EmailPriorityKind.Urgent)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        priority = value;
        priorityPresent = true;
    }

    /// <summary>
    /// Clears the stored priority value or values.
    /// </summary>
    public void ClearPriority()
    {
        priorityPresent = false;
        priority = EmailPriorityKind.Normal;
    }

    /// <summary>
    /// Sets the auto submitted used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void SetAutoSubmitted(string value)
    {
        ValidateHeaderText(value, "value");
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                throw new ArgumentException("Auto-Submitted must contain only ASCII characters.", "value");
            }

            index++;
        }

        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > (options ?? throw new InvalidOperationException()).GetMaximumHeaderValueBytes())
        {
            throw new EmailLimitException("MaxComposerHeaderValueBytes", options.GetMaximumHeaderValueBytes(), bytes.Length);
        }

        EmailMetadataParseResult parsed = new EmailMetadataParser().ParseAutoSubmittedWithInputAndMode(bytes, EmailStructuredValueParseMode.Strict);
        if (!parsed.IsSuccess() || !parsed.IsComplete())
        {
            throw new ArgumentException("Auto-Submitted must be a valid ASCII disposition and parameter sequence.", "value");
        }

        autoSubmitted = value;
    }

    /// <summary>
    /// Clears the stored auto submitted value or values.
    /// </summary>
    public void ClearAutoSubmitted()
    {
        autoSubmitted = null;
    }

    /// <summary>
    /// Adds a custom field to the current collection or model.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void AddCustomField(string name, string value)
    {
        ValidateCustomName(name);
        ValidateHeaderText(value, "value");
        if ((customFields ?? throw new InvalidOperationException()).Count >= (options ?? throw new InvalidOperationException()).GetMaximumCustomFieldCount())
        {
            throw new EmailLimitException("MaxComposerCustomFields", options.GetMaximumCustomFieldCount(), customFields.Count + 1);
        }

        byte[] bytes = FormatUnstructured(name, value);
        customFields.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, bytes, EmailHeaderValueKind.Unstructured));
    }

    /// <summary>
    /// Removes the matching custom fields value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public int RemoveCustomFields(string name)
    {
        ValidateHeaderName(name, "name");
        int removed = 0;
        int index = (customFields ?? throw new InvalidOperationException()).Count - 1;
        while (index >= 0)
        {
            if (string.Equals(customFields[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                customFields.RemoveAt(index);
                removed++;
            }

            index--;
        }

        return removed;
    }

    /// <summary>
    /// Clears the stored custom fields value or values.
    /// </summary>
    public void ClearCustomFields()
    {
        (customFields ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Sets the text used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetText(string value)
    {
        SetTextWithCharset(value, ContainsNonAscii(Encoding.UTF8.GetBytes(RequireBodyText(value))) ? "utf-8" : "us-ascii");
    }

    /// <summary>
    /// Sets the text used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    public void SetTextWithCharset(string value, string charset)
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(RequireBodyText(value), "plain", charset);
        SetStandardBody(entity, 0);
    }

    /// <summary>
    /// Clears the stored text value or values.
    /// </summary>
    public void ClearText()
    {
        textBody = null;
    }

    /// <summary>
    /// Sets the HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetHtml(string? value)
    {
        SetHtmlWithCharset(value, "utf-8");
    }

    /// <summary>
    /// Sets the HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    public void SetHtmlWithCharset(string? value, string charset)
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(RequireBodyText(value), "html", charset);
        SetStandardBody(entity, 3);
    }

    /// <summary>
    /// Clears the stored HTML value or values.
    /// </summary>
    public void ClearHtml()
    {
        htmlBody = null;
    }

    /// <summary>
    /// Sets the amp HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetAmpHtml(string value)
    {
        SetAmpHtmlWithCharset(value, "utf-8");
    }

    /// <summary>
    /// Sets the amp HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    public void SetAmpHtmlWithCharset(string value, string charset)
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(RequireBodyText(value), "x-amp-html", charset);
        SetStandardBody(entity, 2);
    }

    /// <summary>
    /// Clears the stored amp HTML value or values.
    /// </summary>
    public void ClearAmpHtml()
    {
        ampHtmlBody = null;
    }

    /// <summary>
    /// Sets the watch HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetWatchHtml(string value)
    {
        SetWatchHtmlWithCharset(value, "utf-8");
    }

    /// <summary>
    /// Sets the watch HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    public void SetWatchHtmlWithCharset(string value, string charset)
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(RequireBodyText(value), "watch-html", charset);
        SetStandardBody(entity, 1);
    }

    /// <summary>
    /// Clears the stored watch HTML value or values.
    /// </summary>
    public void ClearWatchHtml()
    {
        watchHtmlBody = null;
    }

    /// <summary>
    /// Sets the calendar used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "method">The method used by the operation.</param>
    public void SetCalendar(string value, string method)
    {
        SetCalendarWithCharset(value, method, "utf-8");
    }

    /// <summary>
    /// Sets the calendar used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "method">The method used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    public void SetCalendarWithCharset(string value, string method, string charset)
    {
        MimeEntity entity = new EmailEntityFactory().CreateCalendar(RequireBodyText(value), method, charset);
        SetStandardBody(entity, 4);
    }

    /// <summary>
    /// Clears the stored calendar value or values.
    /// </summary>
    public void ClearCalendar()
    {
        calendarBody = null;
    }

    /// <summary>
    /// Sets the content used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    public void SetContent(string value, string type, string subtype, string charset)
    {
        MimeEntity entity = new EmailEntityFactory().CreateContent(RequireBodyText(value), type, subtype, charset);
        ValidateBodySize(entity);
        ValidateReplacementBodySize(entity);
        textBody = null;
        watchHtmlBody = null;
        ampHtmlBody = null;
        htmlBody = null;
        calendarBody = null;
        (alternativeBodies ?? throw new InvalidOperationException()).Clear();
        explicitBody = entity;
        rawBody = null;
    }

    /// <summary>
    /// Adds an alternative to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    public void AddAlternative(string value, string type, string subtype, string charset)
    {
        AddAlternativeEntity(new EmailEntityFactory().CreateContent(RequireBodyText(value), type, subtype, charset));
    }

    /// <summary>
    /// Adds an alternative to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddAlternativeEntity(MimeEntity value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        EnsureAlternativeCapacity(1);
        ValidateBodySize(value);
        ValidateManagedBodySize(null, null, value);
        (alternativeBodies ?? throw new InvalidOperationException()).Add(value.Clone());
        rawBody = null;
    }

    /// <summary>
    /// Clears the stored alternatives value or values.
    /// </summary>
    public void ClearAlternatives()
    {
        (alternativeBodies ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Sets the raw entity used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetRawEntity(MimeEntity? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateBodySize(value);
        rawBody = value.Clone();
    }

    /// <summary>
    /// Clears the stored raw entity value or values.
    /// </summary>
    public void ClearRawEntity()
    {
        rawBody = null;
    }

    /// <summary>
    /// Clears the stored body value or values.
    /// </summary>
    public void ClearBody()
    {
        textBody = new EmailEntityFactory().CreateTextWithValue("");
        watchHtmlBody = null;
        ampHtmlBody = null;
        htmlBody = null;
        calendarBody = null;
        explicitBody = null;
        (alternativeBodies ?? throw new InvalidOperationException()).Clear();
        rawBody = null;
    }

    /// <summary>
    /// Adds an attachment to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddAttachment(EmailAttachmentSource value)
    {
        AddContentSource(attachments, value, false);
    }

    /// <summary>
    /// Clears the stored attachments value or values.
    /// </summary>
    public void ClearAttachments()
    {
        (attachments ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Adds a related resource to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddRelatedResource(EmailAttachmentSource value)
    {
        AddContentSource(relatedResources, value, true);
    }

    /// <summary>
    /// Clears the stored related resources value or values.
    /// </summary>
    public void ClearRelatedResources()
    {
        (relatedResources ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <returns>The message produced by the operation.</returns>
    public EmailMessage Build()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        AddDateHeader(headers);
        AddAddressHeader(headers, "From", from, EmailHeaderValueKind.AddressList);
        AddMailboxHeader(headers, "Sender", sender);
        AddAddressHeader(headers, "Reply-To", replyTo, EmailHeaderValueKind.AddressList);
        AddAddressHeader(headers, "To", to, EmailHeaderValueKind.AddressList);
        AddAddressHeader(headers, "Cc", cc, EmailHeaderValueKind.AddressList);
        AddIdentifierHeader(headers, "Message-ID", messageId, EmailHeaderValueKind.MessageId);
        AddIdentifierHeader(headers, "In-Reply-To", inReplyTo, EmailHeaderValueKind.InReplyTo);
        AddReferencesHeader(headers);
        if (subject != null)
        {
            headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", FormatUnstructured("Subject", subject), EmailHeaderValueKind.Unstructured));
        }

        AddPriorityHeaders(headers);
        if (autoSubmitted != null)
        {
            headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Auto-Submitted", Encoding.ASCII.GetBytes(autoSubmitted), EmailHeaderValueKind.AutoSubmitted));
        }

        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("MIME-Version", Encoding.ASCII.GetBytes("1.0")));
        int customIndex = 0;
        while (customIndex < (customFields ?? throw new InvalidOperationException()).Count)
        {
            headers.Add(customFields[customIndex].Clone());
            customIndex++;
        }

        EmailEnvelope envelope = BuildEnvelope();
        return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, BuildContentTree(), envelope, null, new EmailIssueList());
    }

    MimeEntity? BuildContentTree()
    {
        if (rawBody != null)
        {
            MimeEntity exact = rawBody.Clone();
            ValidateTotalBodySize(exact);
            return exact;
        }

        MimeEntity? body = BuildBody();
        if ((relatedResources ?? throw new InvalidOperationException()).Count > 0)
        {
            List<MimeEntity> related = new List<MimeEntity>();
            related.Add((body ?? throw new InvalidOperationException()));
            int resourceIndex = 0;
            while (resourceIndex < relatedResources.Count)
            {
                related.Add(relatedResources[resourceIndex].CreateResourceEntity());
                resourceIndex++;
            }

            body = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", related);
        }

        if ((attachments ?? throw new InvalidOperationException()).Count > 0)
        {
            List<MimeEntity> mixed = new List<MimeEntity>();
            mixed.Add((body ?? throw new InvalidOperationException()));
            int attachmentIndex = 0;
            while (attachmentIndex < attachments.Count)
            {
                mixed.Add(attachments[attachmentIndex].CreateAttachmentEntity());
                attachmentIndex++;
            }

            body = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", mixed);
        }

        ValidateTotalBodySize(body);
        if ((body ?? throw new InvalidOperationException()).IsContainer())
        {
            body = GenerateBoundaries(body, "email-composer-content-boundary-v1");
        }

        return body;
    }

    MimeEntity? BuildBody()
    {
        List<MimeEntity> bodies = new List<MimeEntity>();
        AddBodyClone(bodies, explicitBody);
        AddBodyClone(bodies, textBody);
        AddBodyClone(bodies, watchHtmlBody);
        AddBodyClone(bodies, ampHtmlBody);
        AddBodyClone(bodies, htmlBody);
        AddBodyClone(bodies, calendarBody);
        int index = 0;
        while (index < (alternativeBodies ?? throw new InvalidOperationException()).Count)
        {
            bodies.Add(alternativeBodies[index].Clone());
            index++;
        }

        if (bodies.Count == 0)
        {
            return new EmailEntityFactory().CreateTextWithValue("");
        }

        if (bodies.Count == 1)
        {
            ValidateBodySize(bodies[0]);
            return bodies[0];
        }

        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", bodies);
        ValidateBodySize(multipart);
        return GenerateBoundaries(multipart, "email-composer-alternative-boundary-v1");
    }

    static MimeEntity? GenerateBoundaries(MimeEntity value, string seed)
    {
        EmailGenerationOptions generation = new EmailGenerationOptions();
        generation.SetGenerateDate(false);
        generation.SetGenerateMessageId(false);
        generation.SetGenerateContentIds(false);
        generation.SetGenerateMimeVersion(false);
        generation.SetGenerateBoundaries(true);
        generation.SetDeterministicOutput(true);
        generation.SetIdentifierDomain("local.invalid");
        generation.SetDeterministicSeed(Encoding.ASCII.GetBytes(seed));
        generation.ClearGenerationDate();
        generation.SetMaximumDepth(EmailTraversalConstants.DefaultMaximumDepth);
        generation.SetMaximumEntityCount(EmailTraversalConstants.DefaultMaximumEntityCount);
        return new EmailGenerator().GenerateWithEntityAndOptions(value, generation).GetEntity();
    }

    static void AddBodyClone(List<MimeEntity> values, MimeEntity? value)
    {
        if (value != null)
        {
            values.Add(value.Clone());
        }
    }

    void SetStandardBody(MimeEntity entity, int slot)
    {
        ValidateBodySize(entity);
        MimeEntity? current = GetStandardBody(slot);
        ValidateManagedBodySize(current, explicitBody, entity);
        int removed = current == null ? 0 : 1;
        if (explicitBody != null)
        {
            removed++;
        }

        EnsureManagedBodyCapacity(removed, 1);
        explicitBody = null;
        rawBody = null;
        if (slot == 0)
        {
            textBody = entity;
        }
        else if (slot == 1)
        {
            watchHtmlBody = entity;
        }
        else if (slot == 2)
        {
            ampHtmlBody = entity;
        }
        else if (slot == 3)
        {
            htmlBody = entity;
        }
        else
        {
            calendarBody = entity;
        }
    }

    MimeEntity? GetStandardBody(int slot)
    {
        if (slot == 0)
        {
            return textBody;
        }

        if (slot == 1)
        {
            return watchHtmlBody;
        }

        if (slot == 2)
        {
            return ampHtmlBody;
        }

        return slot == 3 ? htmlBody : calendarBody;
    }

    void EnsureAlternativeCapacity(int added)
    {
        EnsureManagedBodyCapacity(0, added);
    }

    void EnsureManagedBodyCapacity(int removed, int added)
    {
        int count = GetManagedBodyCount() - removed + added;
        if (count > (options ?? throw new InvalidOperationException()).GetMaximumAlternativeCount())
        {
            throw new EmailLimitException("MaxComposerAlternatives", options.GetMaximumAlternativeCount(), count);
        }
    }

    int GetManagedBodyCount()
    {
        int count = (alternativeBodies ?? throw new InvalidOperationException()).Count;
        if (explicitBody != null)
            count++;
        if (textBody != null)
            count++;
        if (watchHtmlBody != null)
            count++;
        if (ampHtmlBody != null)
            count++;
        if (htmlBody != null)
            count++;
        if (calendarBody != null)
            count++;
        return count;
    }

    void ValidateBodySize(MimeEntity value)
    {
        long total = GetKnownBodyBytes(value);
        if (total > (options ?? throw new InvalidOperationException()).GetMaximumBodyBytes())
        {
            throw new EmailLimitException("MaxComposerBodyBytes", options.GetMaximumBodyBytes(), total);
        }
    }

    void ValidateManagedBodySize(MimeEntity? firstRemoved, MimeEntity? secondRemoved, MimeEntity added)
    {
        long total = AddKnownSourceBytes(GetManagedKnownBodyBytes());
        if (firstRemoved != null)
        {
            total -= GetKnownBodyBytes(firstRemoved);
        }

        if (secondRemoved != null)
        {
            total -= GetKnownBodyBytes(secondRemoved);
        }

        long addedBytes = GetKnownBodyBytes(added);
        if (addedBytes > (options ?? throw new InvalidOperationException()).GetMaximumBodyBytes() - total)
        {
            throw new EmailLimitException("MaxComposerBodyBytes", options.GetMaximumBodyBytes(), total + addedBytes);
        }
    }

    void ValidateReplacementBodySize(MimeEntity value)
    {
        long total = AddKnownSourceBytes(GetKnownBodyBytes(value));
        if (total > (options ?? throw new InvalidOperationException()).GetMaximumBodyBytes())
        {
            throw new EmailLimitException("MaxComposerBodyBytes", options.GetMaximumBodyBytes(), total);
        }
    }

    void AddContentSource(List<EmailAttachmentSource>? destination, EmailAttachmentSource value, bool related)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (related && !value.HasContentId() && !value.HasContentLocation())
        {
            throw new ArgumentException("A related resource requires a content ID or content location.", "value");
        }

        if ((attachments ?? throw new InvalidOperationException()).Count + (relatedResources ?? throw new InvalidOperationException()).Count >= (options ?? throw new InvalidOperationException()).GetMaximumAttachmentCount())
        {
            throw new EmailLimitException("MaxComposerAttachments", options.GetMaximumAttachmentCount(), attachments.Count + relatedResources.Count + 1);
        }

        EmailContent content = value.GetContent();
        if (content.IsClosed())
        {
            throw new ObjectDisposedException("value");
        }

        long total = GetManagedKnownBodyBytes();
        total = AddKnownSourceBytes(total);
        if (content.HasKnownLength())
        {
            long length = content.GetLength();
            if (length > options.GetMaximumBodyBytes() - total)
            {
                throw new EmailLimitException("MaxComposerBodyBytes", options.GetMaximumBodyBytes(), AddSaturated(total, length));
            }
        }

        (destination ?? throw new InvalidOperationException()).Add(value.Clone());
        rawBody = null;
    }

    long AddKnownSourceBytes(long total)
    {
        int index = 0;
        while (index < (attachments ?? throw new InvalidOperationException()).Count)
        {
            total = AddKnownContentBytes(total, attachments[index].GetContent());
            index++;
        }

        index = 0;
        while (index < (relatedResources ?? throw new InvalidOperationException()).Count)
        {
            total = AddKnownContentBytes(total, relatedResources[index].GetContent());
            index++;
        }

        return total;
    }

    static long AddKnownContentBytes(long total, EmailContent content)
    {
        if (content.IsClosed())
        {
            throw new ObjectDisposedException("content");
        }

        return content.HasKnownLength() ? AddSaturated(total, content.GetLength()) : total;
    }

    static long AddSaturated(long first, long second)
    {
        return second > long.MaxValue - first ? long.MaxValue : first + second;
    }

    void ValidateTotalBodySize(MimeEntity? body)
    {
        long total = GetKnownBodyBytes(body);
        if (total > (options ?? throw new InvalidOperationException()).GetMaximumBodyBytes())
        {
            throw new EmailLimitException("MaxComposerBodyBytes", options.GetMaximumBodyBytes(), total);
        }
    }

    long GetManagedKnownBodyBytes()
    {
        long total = 0;
        total = AddKnownBodyBytes(total, explicitBody);
        total = AddKnownBodyBytes(total, textBody);
        total = AddKnownBodyBytes(total, watchHtmlBody);
        total = AddKnownBodyBytes(total, ampHtmlBody);
        total = AddKnownBodyBytes(total, htmlBody);
        total = AddKnownBodyBytes(total, calendarBody);
        int index = 0;
        while (index < (alternativeBodies ?? throw new InvalidOperationException()).Count)
        {
            total = AddKnownBodyBytes(total, alternativeBodies[index]);
            index++;
        }

        return total;
    }

    static long AddKnownBodyBytes(long total, MimeEntity? value)
    {
        long bytes = GetKnownBodyBytes(value);
        return bytes > long.MaxValue - total ? long.MaxValue : total + bytes;
    }

    static long GetKnownBodyBytes(MimeEntity? value)
    {
        if (value == null)
        {
            return 0;
        }

        List<MimeEntity> entities = new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(value, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
        long total = 0;
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.HasContent())
            {
                EmailContent? content = entity.GetContent();
                if ((content ?? throw new InvalidOperationException()).IsClosed())
                {
                    throw new ObjectDisposedException("value");
                }

                if (content.HasKnownLength())
                {
                    long length = content.GetLength();
                    if (length > long.MaxValue - total)
                    {
                        return long.MaxValue;
                    }

                    total += length;
                }
            }

            index++;
        }

        return total;
    }

    static string RequireBodyText(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value;
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailValidationResult Validate()
    {
        EmailMessage message = Build();
        EmailValidationOptions validationOptions = CreateValidationOptions();
        EmailValidationResult validation = new EmailValidator().ValidateWithMessageAndOptions(message, validationOptions);
        EmailIssueList semantic = validation.GetSemanticIssues();
        if ((options ?? throw new InvalidOperationException()).GetRequireRecipient() && (message.GetEnvelope() ?? throw new InvalidOperationException()).GetRecipientCount() == 0)
        {
            semantic.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.CompositionInvalid, "The composed message has no envelope recipient.", null, null, null, "Add a To, Cc, Bcc, or explicit envelope recipient."));
        }

        return new EmailValidationResult(validation.GetSyntaxIssues(), semantic, validation.GetValidatedEntityCount(), validation.GetValidatedHeaderCount());
    }

    void AddAddress(List<EmailAddress>? values, EmailAddress value)
    {
        ValidateAddress(value, "value");
        if (GetAddressCount() >= (options ?? throw new InvalidOperationException()).GetMaximumAddressCount())
        {
            throw new EmailLimitException("MaxComposerAddresses", options.GetMaximumAddressCount(), GetAddressCount() + 1);
        }

        (values ?? throw new InvalidOperationException()).Add(value);
    }

    void EnsureAddressCapacity(int replacedCount)
    {
        int nextCount = GetAddressCount() - replacedCount + 1;
        if (nextCount > (options ?? throw new InvalidOperationException()).GetMaximumAddressCount())
        {
            throw new EmailLimitException("MaxComposerAddresses", options.GetMaximumAddressCount(), nextCount);
        }
    }

    int GetAddressCount()
    {
        return (from ?? throw new InvalidOperationException()).Count + (replyTo ?? throw new InvalidOperationException()).Count + (to ?? throw new InvalidOperationException()).Count + (cc ?? throw new InvalidOperationException()).Count + (bcc ?? throw new InvalidOperationException()).Count + (envelopeRecipients ?? throw new InvalidOperationException()).Count + (sender == null ? 0 : 1) + (envelopeSender == null ? 0 : 1);
    }

    void ValidateAddress(EmailAddress value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        EmailAddressFormatOptions formatOptions = CreateAddressFormatOptions(0);
        EmailAddressFormatResult formatted = new EmailAddressFormatter().FormatWithAddressAndOptions(value, formatOptions);
        if (!formatted.IsSuccess())
        {
            throw new ArgumentException("The address cannot be formatted under the composer's SMTPUTF8 policy.", parameterName);
        }
    }

    void AddDateHeader(EmailHeaders headers)
    {
        if (date == null)
        {
            return;
        }

        EmailDateFormatResult formatted = new EmailDateFormatter().FormatWithValue(date);
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Date", formatted.GetBytes(), EmailHeaderValueKind.Date));
    }

    void AddMailboxHeader(EmailHeaders headers, string name, EmailAddress? value)
    {
        if (value == null)
        {
            return;
        }

        EmailAddressFormatOptions formatOptions = CreateAddressFormatOptions(name.Length + 2);
        EmailAddressFormatResult formatted = new EmailAddressFormatter().FormatWithAddressAndOptions(value, formatOptions);
        EnsureFormatted(formatted.IsSuccess(), "The mailbox field could not be formatted safely.");
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, formatted.GetBytes(), EmailHeaderValueKind.Mailbox));
    }

    void AddAddressHeader(EmailHeaders headers, string name, List<EmailAddress>? values, EmailHeaderValueKind kind)
    {
        if ((values ?? throw new InvalidOperationException()).Count == 0)
        {
            return;
        }

        List<EmailAddressListEntry> entries = new List<EmailAddressListEntry>();
        int index = 0;
        while (index < values.Count)
        {
            entries.Add(new EmailAddressListEntryOperations().CreateMailbox(values[index]));
            index++;
        }

        EmailAddressList list = new EmailAddressList(entries, new byte[0]);
        EmailAddressFormatOptions formatOptions = CreateAddressFormatOptions(name.Length + 2);
        EmailAddressFormatResult formatted = new EmailAddressFormatter().FormatListWithListAndOptions(list, formatOptions);
        EnsureFormatted(formatted.IsSuccess(), "The address-list field could not be formatted safely.");
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, formatted.GetBytes(), kind));
    }

    EmailAddressFormatOptions CreateAddressFormatOptions(int firstLineColumn)
    {
        EmailAddressFormatOptions formatOptions = new EmailAddressFormatOptions();
        formatOptions.SetDisplayNameFormat((options ?? throw new InvalidOperationException()).GetAllowSmtpUtf8() ? EmailAddressDisplayNameFormat.Unicode : EmailAddressDisplayNameFormat.EncodedWord);
        formatOptions.SetDomainFormat(options.GetAllowSmtpUtf8() ? EmailAddressDomainFormat.Unicode : EmailAddressDomainFormat.Ascii);
        formatOptions.SetEncodedWordEncoding(EmailEncodedWordEncodingChoice.Automatic);
        formatOptions.SetAllowInternationalized(options.GetAllowSmtpUtf8());
        formatOptions.SetFirstLineColumn(firstLineColumn);
        formatOptions.SetMaximumLineLength(EmailAddressFormatOptionsConstants.DefaultMaximumLineLength);
        formatOptions.SetMaximumEncodedWords(EmailAddressFormatOptionsConstants.DefaultMaximumEncodedWords);
        formatOptions.SetMaximumOutputBytes(options.GetMaximumHeaderValueBytes());
        return formatOptions;
    }

    void AddIdentifierHeader(EmailHeaders headers, string name, EmailMessageId? value, EmailHeaderValueKind kind)
    {
        if (value == null)
        {
            return;
        }

        EmailMessageIdFormatOptions formatOptions = CreateIdentifierFormatOptions(name.Length + 2);
        EmailMessageIdFormatResult formatted = new EmailMessageIdFormatter().FormatWithIdentifierAndOptions(value, formatOptions);
        EnsureFormatted(formatted.IsSuccess(), "The identifier field could not be formatted safely.");
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, formatted.GetBytes(), kind));
    }

    void AddReferencesHeader(EmailHeaders headers)
    {
        if ((references ?? throw new InvalidOperationException()).Count == 0)
        {
            return;
        }

        EmailMessageIdList identifiers = new EmailMessageIdList(new List<EmailMessageId>(references), new byte[0]);
        EmailMessageIdFormatResult formatted = new EmailMessageIdFormatter().FormatListWithIdentifiersAndOptions(identifiers, CreateIdentifierFormatOptions(12));
        EnsureFormatted(formatted.IsSuccess(), "The References field could not be formatted safely.");
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("References", formatted.GetBytes(), EmailHeaderValueKind.References));
    }

    EmailMessageIdFormatOptions CreateIdentifierFormatOptions(int firstLineColumn)
    {
        EmailMessageIdFormatOptions formatOptions = new EmailMessageIdFormatOptions();
        formatOptions.SetFirstLineColumn(firstLineColumn);
        formatOptions.SetMaximumLineLength(78);
        formatOptions.SetMaximumOutputBytes((options ?? throw new InvalidOperationException()).GetMaximumHeaderValueBytes());
        return formatOptions;
    }

    void ValidateIdentifier(EmailMessageId value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        EmailMessageIdFormatResult formatted = new EmailMessageIdFormatter().FormatWithIdentifierAndOptions(value, CreateIdentifierFormatOptions(0));
        if (!formatted.IsSuccess())
        {
            throw new ArgumentException("The message identifier cannot be formatted safely.", parameterName);
        }
    }

    void AddPriorityHeaders(EmailHeaders headers)
    {
        if (!priorityPresent)
        {
            return;
        }

        string priorityValue = priority == EmailPriorityKind.Urgent ? "urgent" : priority == EmailPriorityKind.NonUrgent ? "non-urgent" : "normal";
        string numericValue = priority == EmailPriorityKind.Urgent ? "1" : priority == EmailPriorityKind.NonUrgent ? "5" : "3";
        string importanceValue = priority == EmailPriorityKind.Urgent ? "high" : priority == EmailPriorityKind.NonUrgent ? "low" : "normal";
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Priority", Encoding.ASCII.GetBytes(priorityValue), EmailHeaderValueKind.Priority));
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("X-Priority", Encoding.ASCII.GetBytes(numericValue), EmailHeaderValueKind.XPriority));
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Importance", Encoding.ASCII.GetBytes(importanceValue), EmailHeaderValueKind.Importance));
    }

    EmailEnvelope BuildEnvelope()
    {
        EmailEnvelope envelope = new EmailEnvelopeOperations().CreateWith();
        if (!nullEnvelopeSender)
        {
            if (envelopeSender != null)
            {
                envelope.SetSender(envelopeSender);
            }
            else if (sender != null)
            {
                envelope.SetSender(sender);
            }
            else if ((from ?? throw new InvalidOperationException()).Count == 1)
            {
                envelope.SetSender(from[0]);
            }
        }

        AddEnvelopeAddresses(envelope, to);
        AddEnvelopeAddresses(envelope, cc);
        AddEnvelopeAddresses(envelope, bcc);
        AddEnvelopeAddresses(envelope, envelopeRecipients);
        return envelope;
    }

    void AddEnvelopeAddresses(EmailEnvelope envelope, List<EmailAddress>? values)
    {
        int index = 0;
        while (index < (values ?? throw new InvalidOperationException()).Count)
        {
            if (!ContainsEnvelopeAddress(envelope, values[index]))
            {
                envelope.AddRecipient(values[index]);
            }

            index++;
        }
    }

    static bool ContainsEnvelopeAddress(EmailEnvelope envelope, EmailAddress value)
    {
        int index = 0;
        while (index < envelope.GetRecipientCount())
        {
            EmailAddress current = envelope.GetRecipientAt(index);
            if (string.Equals(current.GetLocalPart(), value.GetLocalPart(), StringComparison.Ordinal) && string.Equals(current.GetDomain(), value.GetDomain(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    byte[] FormatUnstructured(string name, string value)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(value);
        if (utf8.Length > (options ?? throw new InvalidOperationException()).GetMaximumHeaderValueBytes())
        {
            throw new EmailLimitException("MaxComposerHeaderValueBytes", options.GetMaximumHeaderValueBytes(), utf8.Length);
        }

        if (options.GetAllowSmtpUtf8() || !ContainsNonAscii(utf8))
        {
            return utf8;
        }

        EmailEncodedWordGenerationOptions encodedOptions = new EmailEncodedWordGenerationOptions("utf-8");
        encodedOptions.SetContext(EmailEncodedWordContext.UnstructuredText);
        encodedOptions.SetEncodingChoice(EmailEncodedWordEncodingChoice.Automatic);
        encodedOptions.SetFirstLineColumn(name.Length + 2);
        encodedOptions.SetMaximumLineLength(EmailEncodedWordGenerationOptionsConstants.DefaultMaximumLineLength);
        encodedOptions.SetMaximumEncodedWords(EmailEncodedWordConstants.DefaultMaximumEncodedWords);
        EmailEncodedWordEncodeResult encoded = new EmailEncodedWord().EncodeText(value, Encoding.UTF8, encodedOptions);
        if (encoded.GetByteCount() > options.GetMaximumHeaderValueBytes())
        {
            throw new EmailLimitException("MaxComposerHeaderValueBytes", options.GetMaximumHeaderValueBytes(), encoded.GetByteCount());
        }

        return encoded.GetBytes();
    }

    static bool ContainsNonAscii(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void ValidateHeaderText(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n' || current == 0 || current < 32 && current != '\t' || current == 127)
            {
                throw new ArgumentException("Header text cannot contain line breaks, NUL, or unsafe control characters.", parameterName);
            }

            index++;
        }
    }

    static void ValidateCustomName(string name)
    {
        ValidateHeaderName(name, "name");
        if (new EmailHeaderRegistry().Contains(name) || string.Equals(name, "MIME-Version", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Return-Path", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Content-Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A composer-managed or standardized field cannot be added as a custom field.", "name");
        }
    }

    static void ValidateHeaderName(string name, string parameterName)
    {
        if (name == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("A header field name is required.", parameterName);
        }

        int index = 0;
        while (index < name.Length)
        {
            char current = name[index];
            if (current < 33 || current > 126 || current == ':')
            {
                throw new ArgumentException("A header field name contains an unsafe or invalid character.", parameterName);
            }

            index++;
        }
    }

    EmailValidationOptions CreateValidationOptions()
    {
        EmailValidationOptions validation = new EmailValidationOptions();
        validation.SetValidateSyntax(true);
        validation.SetValidateSemantics(true);
        validation.SetRequireMimeVersion(true);
        validation.SetMaximumDepth(EmailTraversalConstants.DefaultMaximumDepth);
        validation.SetMaximumEntityCount(EmailTraversalConstants.DefaultMaximumEntityCount);
        validation.SetMaximumContentBytes(EmailDecodingLimitsConstants.DefaultMaxDecodedBytes);
        EmailHeaderValidationOptions message = new EmailHeaderValidationOptions();
        message.SetContext(EmailHeaderValidationContext.Message);
        message.SetAllowSmtpUtf8((options ?? throw new InvalidOperationException()).GetAllowSmtpUtf8());
        message.SetAllowObsoleteSyntax(false);
        message.SetRequireDate(options.GetRequireDate());
        message.SetRequireFrom(options.GetRequireFrom());
        message.SetRequireMessageId(options.GetRequireMessageId());
        message.SetRequireBccStripped(true);
        message.SetValidateStructuredValues(true);
        message.SetMaximumLineLength(998);
        validation.SetMessageHeaderOptions(message);
        EmailHeaderValidationOptions entity = new EmailHeaderValidationOptions();
        entity.SetContext(EmailHeaderValidationContext.MimeEntity);
        entity.SetAllowSmtpUtf8(options.GetAllowSmtpUtf8());
        entity.SetAllowObsoleteSyntax(false);
        entity.SetRequireDate(false);
        entity.SetRequireFrom(false);
        entity.SetRequireMessageId(false);
        entity.SetRequireBccStripped(false);
        entity.SetValidateStructuredValues(true);
        entity.SetMaximumLineLength(998);
        validation.SetEntityHeaderOptions(entity);
        return validation;
    }

    static void EnsureFormatted(bool success, string message)
    {
        if (!success)
        {
            throw new InvalidOperationException(message);
        }
    }
}
