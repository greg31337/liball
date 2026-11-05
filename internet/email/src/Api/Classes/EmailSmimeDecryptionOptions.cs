using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Controls how S/MIME decryption operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailSmimeDecryptionOptions : IEmailSmimeDecryptionOptionsContract
{
    EmailCryptographicLimits limits;
    EmailSmimeEncryptionPolicy policy;
    /// <summary>
    /// Creates a new S/MIME decryption options object.
    /// </summary>
    public EmailSmimeDecryptionOptions()
    {
        limits = new EmailCryptographicLimits();
        policy = new EmailSmimeEncryptionPolicy();
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailCryptographicLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailCryptographicLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the policy.
    /// </summary>
    /// <returns>The current policy.</returns>
    public EmailSmimeEncryptionPolicy GetPolicy()
    {
        return policy.Clone();
    }

    /// <summary>
    /// Sets the policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetPolicy(EmailSmimeEncryptionPolicy value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        policy = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailSmimeDecryptionOptions Clone()
    {
        EmailSmimeDecryptionOptions copy = new EmailSmimeDecryptionOptions();
        copy.SetLimits(limits);
        copy.SetPolicy(policy);
        return copy;
    }
}
