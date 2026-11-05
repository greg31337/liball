using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Controls how S/MIME encryption operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailSmimeEncryptionOptions : IEmailSmimeEncryptionOptionsContract
{
    EmailCryptographicLimits limits;
    EmailSmimeEncryptionPolicy policy;
    EmailSmimeContentEncryptionAlgorithm contentAlgorithm;
    EmailSmimeKeyTransportAlgorithm keyTransportAlgorithm;
    /// <summary>
    /// Creates a new S/MIME encryption options object.
    /// </summary>
    public EmailSmimeEncryptionOptions()
    {
        limits = new EmailCryptographicLimits();
        policy = new EmailSmimeEncryptionPolicy();
        contentAlgorithm = EmailSmimeContentEncryptionAlgorithm.Aes256Gcm;
        keyTransportAlgorithm = EmailSmimeKeyTransportAlgorithm.RsaOaepSha256;
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
    /// Gets the content algorithm.
    /// </summary>
    /// <returns>The current content algorithm.</returns>
    public EmailSmimeContentEncryptionAlgorithm GetContentAlgorithm()
    {
        return contentAlgorithm;
    }

    /// <summary>
    /// Sets the content algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetContentAlgorithm(EmailSmimeContentEncryptionAlgorithm value)
    {
        if (value < EmailSmimeContentEncryptionAlgorithm.Aes128Cbc || value > EmailSmimeContentEncryptionAlgorithm.Aes256Gcm)
            throw new ArgumentOutOfRangeException("value");
        contentAlgorithm = value;
    }

    /// <summary>
    /// Gets the key transport algorithm.
    /// </summary>
    /// <returns>The current key transport algorithm.</returns>
    public EmailSmimeKeyTransportAlgorithm GetKeyTransportAlgorithm()
    {
        return keyTransportAlgorithm;
    }

    /// <summary>
    /// Sets the key transport algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetKeyTransportAlgorithm(EmailSmimeKeyTransportAlgorithm value)
    {
        if (value < EmailSmimeKeyTransportAlgorithm.RsaPkcs1 || value > EmailSmimeKeyTransportAlgorithm.RsaOaepSha256)
            throw new ArgumentOutOfRangeException("value");
        keyTransportAlgorithm = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailSmimeEncryptionOptions Clone()
    {
        EmailSmimeEncryptionOptions copy = new EmailSmimeEncryptionOptions();
        copy.SetLimits(limits);
        copy.SetPolicy(policy);
        copy.SetContentAlgorithm(contentAlgorithm);
        copy.SetKeyTransportAlgorithm(keyTransportAlgorithm);
        return copy;
    }
}
