using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeDecryptionOptions"/>.
/// </summary>
public interface IEmailSmimeDecryptionOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailCryptographicLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailCryptographicLimits value);
    /// <summary>
    /// Gets the policy.
    /// </summary>
    /// <returns>The current policy.</returns>
    EmailSmimeEncryptionPolicy GetPolicy();
    /// <summary>
    /// Sets the policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetPolicy(EmailSmimeEncryptionPolicy value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailSmimeDecryptionOptions Clone();
}
