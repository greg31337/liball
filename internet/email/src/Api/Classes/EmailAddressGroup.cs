using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an address group.
/// </summary>
public class EmailAddressGroup : IEmailAddressGroupContract
{
    string name;
    List<EmailAddress> addresses;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    bool internationalized;
    bool obsoleteSyntax;
    /// <summary>
    /// Creates a new address group.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "addresses">The addresses used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "internationalized">Whether to enable the internationalized behavior.</param>
    /// <param name = "obsoleteSyntax">Whether to enable the obsolete syntax behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAddressGroup(string? name, List<EmailAddress> addresses, byte[] rawBytes, EmailSourcePosition sourcePosition, bool internationalized, bool obsoleteSyntax)
    {
        if (name == null || name.Length == 0)
        {
            throw new ArgumentException("An address group name cannot be empty.", "name");
        }

        if (addresses == null)
        {
            throw new ArgumentNullException("addresses");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.name = name;
        this.addresses = new List<EmailAddress>(addresses);
        this.rawBytes = new byte[rawBytes.Length];
        rawBytes.CopyTo(this.rawBytes, 0);
        this.sourcePosition = sourcePosition;
        this.internationalized = internationalized;
        this.obsoleteSyntax = obsoleteSyntax;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the address count.
    /// </summary>
    /// <returns>The current address count.</returns>
    public int GetAddressCount()
    {
        return addresses.Count;
    }

    /// <summary>
    /// Gets the address.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current address.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAddress GetAddress(int index)
    {
        if (index < 0 || index >= addresses.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return addresses[index];
    }

    /// <summary>
    /// Gets the addresses.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current addresses.</returns>
    public List<EmailAddress> GetAddresses()
    {
        return new List<EmailAddress>(addresses);
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
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
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
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    public bool UsesObsoleteSyntax()
    {
        return obsoleteSyntax;
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    public EmailAddressFormatResult Format(EmailAddressFormatOptions options)
    {
        return new EmailAddressFormatter().FormatGroupWithGroupAndOptions(this, options);
    }
}
