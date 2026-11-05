using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an address list.
/// </summary>
public class EmailAddressList : IEmailAddressListContract
{
    List<EmailAddressListEntry> entries;
    byte[] rawBytes;
    /// <summary>
    /// Creates a new address list.
    /// </summary>
    /// <param name = "entries">The entries used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAddressList(List<EmailAddressListEntry> entries, byte[] rawBytes)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        this.entries = new List<EmailAddressListEntry>(entries);
        this.rawBytes = new byte[rawBytes.Length];
        rawBytes.CopyTo(this.rawBytes, 0);
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return entries.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAddressListEntry GetAt(int index)
    {
        if (index < 0 || index >= entries.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return entries[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<EmailAddressListEntry> GetAll()
    {
        return new List<EmailAddressListEntry>(entries);
    }

    /// <summary>
    /// Gets the mailbox count.
    /// </summary>
    /// <returns>The current mailbox count.</returns>
    public int GetMailboxCount()
    {
        int count = 0;
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].IsMailbox())
            {
                count++;
            }
            else
            {
                count += (entries[index].GetGroup() ?? throw new InvalidOperationException()).GetAddressCount();
            }

            index++;
        }

        return count;
    }

    /// <summary>
    /// Gets the group count.
    /// </summary>
    /// <returns>The current group count.</returns>
    public int GetGroupCount()
    {
        int count = 0;
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].IsGroup())
            {
                count++;
            }

            index++;
        }

        return count;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        byte[] copy = new byte[rawBytes.Length];
        rawBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    public EmailAddressFormatResult Format(EmailAddressFormatOptions options)
    {
        return new EmailAddressFormatter().FormatListWithListAndOptions(this, options);
    }
}
