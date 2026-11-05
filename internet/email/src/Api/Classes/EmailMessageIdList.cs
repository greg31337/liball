using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a message ID list.
/// </summary>
public class EmailMessageIdList : IEmailMessageIdListContract
{
    List<EmailMessageId> identifiers;
    byte[] rawBytes;
    /// <summary>
    /// Creates a new message ID list.
    /// </summary>
    /// <param name = "identifiers">The identifiers used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMessageIdList(List<EmailMessageId> identifiers, byte[] rawBytes)
    {
        if (identifiers == null)
        {
            throw new ArgumentNullException("identifiers");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        this.identifiers = new List<EmailMessageId>(identifiers);
        this.rawBytes = new byte[rawBytes.Length];
        rawBytes.CopyTo(this.rawBytes, 0);
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return identifiers.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMessageId GetAt(int index)
    {
        if (index < 0 || index >= identifiers.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return identifiers[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<EmailMessageId> GetAll()
    {
        return new List<EmailMessageId>(identifiers);
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
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    public bool UsesObsoleteSyntax()
    {
        int index = 0;
        while (index < identifiers.Count)
        {
            if (identifiers[index].UsesObsoleteSyntax())
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The message ID format result produced by the operation.</returns>
    public EmailMessageIdFormatResult Format()
    {
        return new EmailMessageIdFormatter().FormatListWithIdentifiers(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    public EmailMessageIdFormatResult FormatWithOptions(EmailMessageIdFormatOptions options)
    {
        return new EmailMessageIdFormatter().FormatListWithIdentifiersAndOptions(this, options);
    }
}
