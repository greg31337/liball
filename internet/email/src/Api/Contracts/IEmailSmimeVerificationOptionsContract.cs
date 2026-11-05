using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeVerificationOptions"/>.
/// </summary>
public interface IEmailSmimeVerificationOptionsContract
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
    /// Gets the verify certificate chain.
    /// </summary>
    /// <returns><c>true</c> when the verify certificate chain setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetVerifyCertificateChain();
    /// <summary>
    /// Sets the verify certificate chain used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the verify certificate chain behavior.</param>
    void SetVerifyCertificateChain(bool value);
    /// <summary>
    /// Gets the revocation mode.
    /// </summary>
    /// <returns>The current revocation mode.</returns>
    X509RevocationMode GetRevocationMode();
    /// <summary>
    /// Sets the revocation mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetRevocationMode(X509RevocationMode value);
    /// <summary>
    /// Determines whether the verification time is present or available.
    /// </summary>
    /// <returns><c>true</c> when the verification time is available; otherwise, <c>false</c>.</returns>
    bool HasVerificationTime();
    /// <summary>
    /// Gets the verification time.
    /// </summary>
    /// <returns>The current verification time.</returns>
    DateTime GetVerificationTime();
    /// <summary>
    /// Sets the verification time used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetVerificationTime(DateTime value);
    /// <summary>
    /// Clears the stored verification time value or values.
    /// </summary>
    void ClearVerificationTime();
    /// <summary>
    /// Adds a custom trust root to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddCustomTrustRoot(X509Certificate2 value);
    /// <summary>
    /// Adds an extra certificate to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddExtraCertificate(X509Certificate2 value);
    /// <summary>
    /// Gets the custom trust root count.
    /// </summary>
    /// <returns>The current custom trust root count.</returns>
    int GetCustomTrustRootCount();
    /// <summary>
    /// Gets the extra certificate count.
    /// </summary>
    /// <returns>The current extra certificate count.</returns>
    int GetExtraCertificateCount();
    /// <summary>
    /// Gets the custom trust root at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested custom trust root.</returns>
    X509Certificate2 GetCustomTrustRootAt(int index);
    /// <summary>
    /// Gets the extra certificate at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested extra certificate.</returns>
    X509Certificate2 GetExtraCertificateAt(int index);
    /// <summary>
    /// Copies the custom trust roots to a separate destination.
    /// </summary>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<X509Certificate2> CopyCustomTrustRoots();
    /// <summary>
    /// Copies the extra certificates to a separate destination.
    /// </summary>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<X509Certificate2> CopyExtraCertificates();
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailSmimeVerificationOptions Clone();
}
