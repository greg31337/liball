using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an envelope.
/// </summary>
public class EmailEnvelope : IEmailEnvelopeContract
{
    EmailAddress? sender;
    List<EmailAddress> recipients;
    byte[]? mboxFromLine;
    EmailRange? mboxFromLineRange;
    /// <summary>
    /// Creates an envelope from explicit delivery metadata.
    /// </summary>
    public EmailEnvelope()
    {
        sender = null;
        recipients = new List<EmailAddress>();
        mboxFromLine = null;
        mboxFromLineRange = null;
    }

    internal static EmailEnvelope CreateImplementationWith()
    {
        return new EmailEnvelope();
    }

    internal static EmailEnvelope CreateImplementationWithSenderAndRecipientsAndMboxFromLineAndMboxFromLineRange(EmailAddress? sender, List<EmailAddress> recipients, byte[]? mboxFromLine, EmailRange? mboxFromLineRange)
    {
        if (recipients == null)
        {
            throw new ArgumentNullException("recipients");
        }

        ValidateRecipients(recipients);
        EmailEnvelope value = new EmailEnvelope();
        value.sender = sender;
        value.recipients = new List<EmailAddress>(recipients);
        value.mboxFromLine = CopyNullable(mboxFromLine);
        value.mboxFromLineRange = mboxFromLineRange;
        return value;
    }

    internal static EmailEnvelope CreateRecipientProjectionImplementation(EmailHeaders headers)
    {
        if (headers == null)
        {
            throw new ArgumentNullException("headers");
        }

        EmailEnvelope projection = new EmailEnvelope();
        int index = 0;
        while (index < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(index);
            if (IsRecipientHeader(header.GetName()))
            {
                EmailAddressParseResult parsed = new EmailAddressParser().ParseListWithInputAndMode(header.GetUnfoldedValue(), EmailAddressParseMode.Tolerant);
                AddAddressList(projection, parsed.GetAddressList());
            }

            index++;
        }

        return projection;
    }

    /// <summary>
    /// Determines whether the sender is present or available.
    /// </summary>
    /// <returns><c>true</c> when the sender is available; otherwise, <c>false</c>.</returns>
    public bool HasSender()
    {
        return sender != null;
    }

    /// <summary>
    /// Gets the sender.
    /// </summary>
    /// <returns>The current sender.</returns>
    public EmailAddress? GetSender()
    {
        return sender;
    }

    /// <summary>
    /// Sets the sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetSender(EmailAddress? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

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
    /// Gets the recipient count.
    /// </summary>
    /// <returns>The current recipient count.</returns>
    public int GetRecipientCount()
    {
        return recipients.Count;
    }

    /// <summary>
    /// Gets the recipient at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested recipient.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAddress GetRecipientAt(int index)
    {
        if (index < 0 || index >= recipients.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return recipients[index];
    }

    /// <summary>
    /// Gets the recipients.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipients.</returns>
    public List<EmailAddress> GetRecipients()
    {
        return new List<EmailAddress>(recipients);
    }

    /// <summary>
    /// Adds a recipient to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddRecipient(EmailAddress? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        recipients.Add(value);
    }

    /// <summary>
    /// Inserts a recipient at the requested position.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void InsertRecipient(int index, EmailAddress value)
    {
        if (index < 0 || index > recipients.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        recipients.Insert(index, value);
    }

    /// <summary>
    /// Removes the matching recipient at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The address produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAddress RemoveRecipientAt(int index)
    {
        if (index < 0 || index >= recipients.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        EmailAddress removed = recipients[index];
        recipients.RemoveAt(index);
        return removed;
    }

    /// <summary>
    /// Clears the stored recipients value or values.
    /// </summary>
    public void ClearRecipients()
    {
        recipients.Clear();
    }

    /// <summary>
    /// Determines whether the mbox from line is present or available.
    /// </summary>
    /// <returns><c>true</c> when the mbox from line is available; otherwise, <c>false</c>.</returns>
    public bool HasMboxFromLine()
    {
        return mboxFromLine != null;
    }

    /// <summary>
    /// Gets the mbox from line.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current mbox from line.</returns>
    public byte[]? GetMboxFromLine()
    {
        return CopyNullable(mboxFromLine);
    }

    /// <summary>
    /// Sets the mbox from line used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMboxFromLine(ReadOnlySpan<byte> value)
    {
        byte[] bytes = value.ToArray();
        ValidateMutableMboxLine(bytes);
        mboxFromLine = bytes;
    }

    /// <summary>
    /// Clears the stored mbox from line value or values.
    /// </summary>
    public void ClearMboxFromLine()
    {
        mboxFromLine = null;
    }

    /// <summary>
    /// Determines whether the mbox from line range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the mbox from line range is available; otherwise, <c>false</c>.</returns>
    public bool HasMboxFromLineRange()
    {
        return mboxFromLineRange != null;
    }

    /// <summary>
    /// Gets the mbox from line range.
    /// </summary>
    /// <returns>The current mbox from line range.</returns>
    public EmailRange? GetMboxFromLineRange()
    {
        return mboxFromLineRange;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailEnvelope Clone()
    {
        return CreateImplementationWithSenderAndRecipientsAndMboxFromLineAndMboxFromLineRange(sender, recipients, mboxFromLine, mboxFromLineRange);
    }

    /// <summary>
    /// Detaches the source.
    /// </summary>
    public void DetachSource()
    {
        mboxFromLineRange = null;
    }

    static void ValidateRecipients(List<EmailAddress> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Envelope recipients cannot contain null entries.", "values");
            }

            index++;
        }
    }

    static bool IsRecipientHeader(string name)
    {
        return string.Equals(name, "To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Cc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Bcc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Cc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Bcc", StringComparison.OrdinalIgnoreCase);
    }

    static void AddAddressList(EmailEnvelope envelope, EmailAddressList addresses)
    {
        int index = 0;
        while (index < addresses.GetCount())
        {
            EmailAddressListEntry entry = addresses.GetAt(index);
            if (entry.IsMailbox())
            {
                envelope.AddRecipient(entry.GetMailbox());
            }
            else
            {
                EmailAddressGroup? group = entry.GetGroup();
                int groupIndex = 0;
                while (groupIndex < (group ?? throw new InvalidOperationException()).GetAddressCount())
                {
                    envelope.AddRecipient(group.GetAddress(groupIndex));
                    groupIndex++;
                }
            }

            index++;
        }
    }

    static void ValidateMutableMboxLine(byte[] value)
    {
        if (value.Length < 5 || value[0] != 70 || value[1] != 114 || value[2] != 111 || value[3] != 109 || value[4] != 32)
        {
            throw new ArgumentException("An mbox envelope line must begin with 'From '.", "value");
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 0)
            {
                throw new ArgumentException("An mbox envelope line cannot contain a NUL byte.", "value");
            }

            if (value[index] == 13 || value[index] == 10)
            {
                bool terminalCrLf = value[index] == 13 && index + 2 == value.Length && value[index + 1] == 10;
                bool terminalSingle = index + 1 == value.Length;
                if (!terminalCrLf && !terminalSingle)
                {
                    throw new ArgumentException("An mbox envelope line cannot contain an embedded line break.", "value");
                }

                return;
            }

            index++;
        }
    }

    static byte[]? CopyNullable(byte[]? value)
    {
        if (value == null)
        {
            return null;
        }

        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
