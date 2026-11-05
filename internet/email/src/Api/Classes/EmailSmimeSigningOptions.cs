using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Controls how S/MIME signing operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailSmimeSigningOptions : IEmailSmimeSigningOptionsContract
{
    EmailCryptographicLimits limits;
    EmailSmimeAlgorithmPolicy algorithmPolicy;
    bool includeCertificates;
    /// <summary>
    /// Creates a new S/MIME signing options object.
    /// </summary>
    public EmailSmimeSigningOptions()
    {
        limits = new EmailCryptographicLimits();
        algorithmPolicy = new EmailSmimeAlgorithmPolicy();
        includeCertificates = true;
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
    /// Gets the algorithm policy.
    /// </summary>
    /// <returns>The current algorithm policy.</returns>
    public EmailSmimeAlgorithmPolicy GetAlgorithmPolicy()
    {
        return algorithmPolicy.Clone();
    }

    /// <summary>
    /// Sets the algorithm policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetAlgorithmPolicy(EmailSmimeAlgorithmPolicy value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        algorithmPolicy = value.Clone();
    }

    /// <summary>
    /// Gets the include certificates.
    /// </summary>
    /// <returns><c>true</c> when the include certificates setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeCertificates()
    {
        return includeCertificates;
    }

    /// <summary>
    /// Sets the include certificates used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include certificates behavior.</param>
    public void SetIncludeCertificates(bool value)
    {
        includeCertificates = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailSmimeSigningOptions Clone()
    {
        EmailSmimeSigningOptions copy = new EmailSmimeSigningOptions();
        copy.SetLimits(limits);
        copy.SetAlgorithmPolicy(algorithmPolicy);
        copy.SetIncludeCertificates(includeCertificates);
        return copy;
    }
}
