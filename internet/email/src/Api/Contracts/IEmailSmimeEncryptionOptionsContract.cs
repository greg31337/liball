using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeEncryptionOptions"/>.
/// </summary>
public interface IEmailSmimeEncryptionOptionsContract
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
    /// Gets the content algorithm.
    /// </summary>
    /// <returns>The current content algorithm.</returns>
    EmailSmimeContentEncryptionAlgorithm GetContentAlgorithm();
    /// <summary>
    /// Sets the content algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetContentAlgorithm(EmailSmimeContentEncryptionAlgorithm value);
    /// <summary>
    /// Gets the key transport algorithm.
    /// </summary>
    /// <returns>The current key transport algorithm.</returns>
    EmailSmimeKeyTransportAlgorithm GetKeyTransportAlgorithm();
    /// <summary>
    /// Sets the key transport algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetKeyTransportAlgorithm(EmailSmimeKeyTransportAlgorithm value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailSmimeEncryptionOptions Clone();
}
