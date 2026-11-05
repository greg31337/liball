using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF recipient.
/// </summary>
public class EmailTnefRecipient : IEmailTnefRecipientContract
{
    int index;
    EmailTnefRecipientKind kind;
    string? displayName;
    string? addressType;
    string? emailAddress;
    string? smtpAddress;
    List<EmailTnefProperty> properties;
    /// <summary>
    /// Creates a new TNEF recipient.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "displayName">The display name used by the operation.</param>
    /// <param name = "addressType">The address type used by the operation.</param>
    /// <param name = "emailAddress">The email address used by the operation.</param>
    /// <param name = "smtpAddress">The smtp address used by the operation.</param>
    /// <param name = "properties">The properties used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTnefRecipient(int index, EmailTnefRecipientKind kind, string? displayName, string? addressType, string? emailAddress, string? smtpAddress, List<EmailTnefProperty> properties)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException("index");
        if (kind < EmailTnefRecipientKind.Unknown || kind > EmailTnefRecipientKind.Bcc)
            throw new ArgumentOutOfRangeException("kind");
        if (properties == null)
            throw new ArgumentNullException("properties");
        this.index = index;
        this.kind = kind;
        this.displayName = displayName;
        this.addressType = addressType;
        this.emailAddress = emailAddress;
        this.smtpAddress = smtpAddress;
        this.properties = new List<EmailTnefProperty>(properties);
    }

    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailTnefRecipientKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <returns>The current display name.</returns>
    public string? GetDisplayName()
    {
        return displayName;
    }

    /// <summary>
    /// Gets the address type.
    /// </summary>
    /// <returns>The current address type.</returns>
    public string? GetAddressType()
    {
        return addressType;
    }

    /// <summary>
    /// Gets the address.
    /// </summary>
    /// <returns>The current address.</returns>
    public string? GetEmailAddress()
    {
        return emailAddress;
    }

    /// <summary>
    /// Gets the SMTP address.
    /// </summary>
    /// <returns>The current SMTP address.</returns>
    public string? GetSmtpAddress()
    {
        return smtpAddress;
    }

    /// <summary>
    /// Gets the preferred address.
    /// </summary>
    /// <returns>The current preferred address.</returns>
    public string? GetPreferredAddress()
    {
        return smtpAddress == null ? emailAddress : smtpAddress;
    }

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    public List<EmailTnefProperty> GetProperties()
    {
        return new List<EmailTnefProperty>(properties);
    }
}
