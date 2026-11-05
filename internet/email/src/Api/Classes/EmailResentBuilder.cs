using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Builds resent from caller-supplied values.
/// </summary>
public class EmailResentBuilder : IEmailResentBuilderContract
{
    EmailMessage? original;
    EmailResentOptions? options;
    EmailDateTime? date;
    List<EmailAddress>? from;
    EmailAddress? sender;
    List<EmailAddress>? to;
    List<EmailAddress>? cc;
    List<EmailAddress>? bcc;
    EmailMessageId? messageId;
    EmailAddress? envelopeSender;
    List<EmailAddress>? envelopeRecipients;
    /// <summary>
    /// Creates a resent builder using the supplied policy.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailResentBuilder(EmailMessage original, EmailResentOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Initialize(original, options.Clone());
    }

    internal static EmailResentBuilder CreateImplementationWithOriginal(EmailMessage original)
    {
        return new EmailResentBuilder(original, new EmailResentOptions());
    }

    internal static EmailResentBuilder CreateImplementationWithOriginalAndOptions(EmailMessage original, EmailResentOptions options)
    {
        return new EmailResentBuilder(original, options);
    }

    void Initialize(EmailMessage suppliedOriginal, EmailResentOptions suppliedOptions)
    {
        if (suppliedOriginal == null)
        {
            throw new ArgumentNullException("original");
        }

        original = suppliedOriginal.Clone();
        options = suppliedOptions;
        date = null;
        from = new List<EmailAddress>();
        sender = null;
        to = new List<EmailAddress>();
        cc = new List<EmailAddress>();
        bcc = new List<EmailAddress>();
        messageId = null;
        envelopeSender = null;
        envelopeRecipients = new List<EmailAddress>();
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    public EmailResentOptions GetOptions()
    {
        return (options ?? throw new InvalidOperationException()).Clone();
    }

    /// <summary>
    /// Sets the date used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetDate(EmailDateTime value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        FormatDate(value);
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
    /// Sets the from used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetFrom(EmailAddress value)
    {
        ValidateAddress(value, "value");
        EnsureAddressCapacity((from ?? throw new InvalidOperationException()).Count, 1);
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
    /// Sets the sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetSender(EmailAddress value)
    {
        ValidateAddress(value, "value");
        EnsureAddressCapacity(sender == null ? 0 : 1, 1);
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
    /// Sets the message ID used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetMessageId(EmailMessageId value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        FormatMessageId(value);
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
    /// Sets the envelope sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetEnvelopeSender(EmailAddress value)
    {
        ValidateAddress(value, "value");
        EnsureAddressCapacity(envelopeSender == null ? 0 : 1, 1);
        envelopeSender = value;
    }

    /// <summary>
    /// Clears the stored envelope sender value or values.
    /// </summary>
    public void ClearEnvelopeSender()
    {
        envelopeSender = null;
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
    /// Builds the configured value.
    /// </summary>
    /// <returns>The message produced by the operation.</returns>
    public EmailMessage Build()
    {
        ValidateRequiredFields();
        EmailMessage message = (original ?? throw new InvalidOperationException()).Clone();
        List<EmailHeader> block = CreateHeaderBlock();
        int index = block.Count - 1;
        while (index >= 0)
        {
            (message.GetHeaders() ?? throw new InvalidOperationException()).Insert(0, block[index]);
            index--;
        }

        EmailEnvelope? envelope = message.GetEnvelope();
        (envelope ?? throw new InvalidOperationException()).ClearSender();
        envelope.ClearRecipients();
        envelope.ClearMboxFromLine();
        EmailAddress selectedSender = envelopeSender != null ? envelopeSender : sender != null ? sender : (from ?? throw new InvalidOperationException())[0];
        envelope.SetSender(selectedSender);
        AddEnvelopeAddresses(envelope, to);
        AddEnvelopeAddresses(envelope, cc);
        AddEnvelopeAddresses(envelope, bcc);
        AddEnvelopeAddresses(envelope, envelopeRecipients);
        return message;
    }

    void ValidateRequiredFields()
    {
        if (date == null)
        {
            throw new InvalidOperationException("A resent message requires Resent-Date.");
        }

        if ((from ?? throw new InvalidOperationException()).Count == 0)
        {
            throw new InvalidOperationException("A resent message requires at least one Resent-From mailbox.");
        }

        if (from.Count == 1 && sender != null)
        {
            throw new InvalidOperationException("Resent-Sender is prohibited when Resent-From contains exactly one mailbox.");
        }

        if (from.Count > 1 && sender == null)
        {
            throw new InvalidOperationException("Resent-Sender is required when Resent-From contains multiple mailboxes.");
        }

        if ((to ?? throw new InvalidOperationException()).Count + (cc ?? throw new InvalidOperationException()).Count + (bcc ?? throw new InvalidOperationException()).Count + (envelopeRecipients ?? throw new InvalidOperationException()).Count == 0)
        {
            throw new InvalidOperationException("A resent message requires at least one recipient.");
        }
    }

    List<EmailHeader> CreateHeaderBlock()
    {
        List<EmailHeader> block = new List<EmailHeader>();
        block.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Resent-Date", FormatDate(date), EmailHeaderValueKind.ResentField));
        block.Add(CreateAddressHeader("Resent-From", from));
        if (sender != null)
        {
            List<EmailAddress> senders = new List<EmailAddress>();
            senders.Add(sender);
            block.Add(CreateAddressHeader("Resent-Sender", senders));
        }

        AddAddressHeader(block, "Resent-To", to);
        AddAddressHeader(block, "Resent-Cc", cc);
        AddAddressHeader(block, "Resent-Bcc", bcc);
        if (messageId != null)
        {
            block.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Resent-Message-ID", FormatMessageId(messageId), EmailHeaderValueKind.ResentField));
        }

        return block;
    }

    void AddAddressHeader(List<EmailHeader> headers, string name, List<EmailAddress>? values)
    {
        if ((values ?? throw new InvalidOperationException()).Count > 0)
        {
            headers.Add(CreateAddressHeader(name, values));
        }
    }

    EmailHeader CreateAddressHeader(string name, List<EmailAddress>? values)
    {
        List<EmailAddressListEntry> entries = new List<EmailAddressListEntry>();
        int index = 0;
        while (index < (values ?? throw new InvalidOperationException()).Count)
        {
            entries.Add(new EmailAddressListEntryOperations().CreateMailbox(values[index]));
            index++;
        }

        EmailAddressList list = new EmailAddressList(entries, new byte[0]);
        EmailAddressFormatOptions formatOptions = new EmailAddressFormatOptions();
        formatOptions.SetAllowInternationalized((options ?? throw new InvalidOperationException()).GetAllowSmtpUtf8());
        formatOptions.SetFirstLineColumn(name.Length + 2);
        formatOptions.SetMaximumOutputBytes(options.GetMaximumHeaderValueBytes());
        EmailAddressFormatResult formatted = new EmailAddressFormatter().FormatListWithListAndOptions(list, formatOptions);
        if (!formatted.IsSuccess())
        {
            throw new ArgumentException("A resent address cannot be formatted safely.", "values");
        }

        return new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, formatted.GetBytes(), EmailHeaderValueKind.ResentField);
    }

    byte[] FormatDate(EmailDateTime? value)
    {
        EmailDateFormatOptions formatOptions = new EmailDateFormatOptions();
        formatOptions.SetMaximumOutputBytes((options ?? throw new InvalidOperationException()).GetMaximumHeaderValueBytes());
        EmailDateFormatResult formatted = new EmailDateFormatter().FormatWithValueAndOptions(value, formatOptions);
        if (!formatted.IsSuccess())
        {
            throw new ArgumentException("The resent date cannot be formatted safely.", "value");
        }

        return formatted.GetBytes();
    }

    byte[] FormatMessageId(EmailMessageId value)
    {
        EmailMessageIdFormatOptions formatOptions = new EmailMessageIdFormatOptions();
        formatOptions.SetFirstLineColumn("Resent-Message-ID".Length + 2);
        formatOptions.SetMaximumOutputBytes((options ?? throw new InvalidOperationException()).GetMaximumHeaderValueBytes());
        EmailMessageIdFormatResult formatted = new EmailMessageIdFormatter().FormatWithIdentifierAndOptions(value, formatOptions);
        if (!formatted.IsSuccess())
        {
            throw new ArgumentException("The resent message identifier cannot be formatted safely.", "value");
        }

        return formatted.GetBytes();
    }

    void AddAddress(List<EmailAddress>? values, EmailAddress value)
    {
        ValidateAddress(value, "value");
        EnsureAddressCapacity(0, 1);
        (values ?? throw new InvalidOperationException()).Add(value);
    }

    void ValidateAddress(EmailAddress value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        EmailAddressFormatOptions formatOptions = new EmailAddressFormatOptions();
        formatOptions.SetAllowInternationalized((options ?? throw new InvalidOperationException()).GetAllowSmtpUtf8());
        formatOptions.SetMaximumOutputBytes(options.GetMaximumHeaderValueBytes());
        if (!new EmailAddressFormatter().FormatWithAddressAndOptions(value, formatOptions).IsSuccess())
        {
            throw new ArgumentException("The address cannot be formatted safely.", parameterName);
        }
    }

    void EnsureAddressCapacity(int removed, int added)
    {
        int count = (from ?? throw new InvalidOperationException()).Count + (to ?? throw new InvalidOperationException()).Count + (cc ?? throw new InvalidOperationException()).Count + (bcc ?? throw new InvalidOperationException()).Count + (envelopeRecipients ?? throw new InvalidOperationException()).Count;
        if (sender != null)
        {
            count++;
        }

        if (envelopeSender != null)
        {
            count++;
        }

        long prospective = (long)count - removed + added;
        if (prospective > (options ?? throw new InvalidOperationException()).GetMaximumAddressCount())
        {
            throw new EmailLimitException("MaxResentAddresses", options.GetMaximumAddressCount(), prospective);
        }
    }

    static void AddEnvelopeAddresses(EmailEnvelope envelope, List<EmailAddress>? values)
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
}
