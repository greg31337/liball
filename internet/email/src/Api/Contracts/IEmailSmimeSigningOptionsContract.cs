using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeSigningOptions"/>.
/// </summary>
public interface IEmailSmimeSigningOptionsContract
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
    /// Gets the algorithm policy.
    /// </summary>
    /// <returns>The current algorithm policy.</returns>
    EmailSmimeAlgorithmPolicy GetAlgorithmPolicy();
    /// <summary>
    /// Sets the algorithm policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetAlgorithmPolicy(EmailSmimeAlgorithmPolicy value);
    /// <summary>
    /// Gets the include certificates.
    /// </summary>
    /// <returns><c>true</c> when the include certificates setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeCertificates();
    /// <summary>
    /// Sets the include certificates used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include certificates behavior.</param>
    void SetIncludeCertificates(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailSmimeSigningOptions Clone();
}
