using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefRecipient"/>.
/// </summary>
public interface IEmailTnefRecipientContract
{
    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    int GetIndex();
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailTnefRecipientKind GetKind();
    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <returns>The current display name.</returns>
    string? GetDisplayName();
    /// <summary>
    /// Gets the address type.
    /// </summary>
    /// <returns>The current address type.</returns>
    string? GetAddressType();
    /// <summary>
    /// Gets the address.
    /// </summary>
    /// <returns>The current address.</returns>
    string? GetEmailAddress();
    /// <summary>
    /// Gets the SMTP address.
    /// </summary>
    /// <returns>The current SMTP address.</returns>
    string? GetSmtpAddress();
    /// <summary>
    /// Gets the preferred address.
    /// </summary>
    /// <returns>The current preferred address.</returns>
    string? GetPreferredAddress();
    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    List<EmailTnefProperty> GetProperties();
}
