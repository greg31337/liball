using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpMimeOptions"/>.
/// </summary>
public interface IEmailOpenPgpMimeOptionsContract
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
    /// Gets the algorithm options.
    /// </summary>
    /// <returns>The current algorithm options.</returns>
    EmailOpenPgpAlgorithmOptions GetAlgorithmOptions();
    /// <summary>
    /// Sets the algorithm options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetAlgorithmOptions(EmailOpenPgpAlgorithmOptions value);
    /// <summary>
    /// Gets the include armor checksum.
    /// </summary>
    /// <returns><c>true</c> when the include armor checksum setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeArmorChecksum();
    /// <summary>
    /// Sets the include armor checksum used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include armor checksum behavior.</param>
    void SetIncludeArmorChecksum(bool value);
    /// <summary>
    /// Determines whether the signature creation time is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signature creation time is available; otherwise, <c>false</c>.</returns>
    bool HasSignatureCreationTime();
    /// <summary>
    /// Gets the signature creation time.
    /// </summary>
    /// <returns>The current signature creation time.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    uint GetSignatureCreationTime();
    /// <summary>
    /// Sets the signature creation time used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetSignatureCreationTime(uint value);
    /// <summary>
    /// Clears the stored signature creation time value or values.
    /// </summary>
    void ClearSignatureCreationTime();
    /// <summary>
    /// Gets the aead chunk size octet.
    /// </summary>
    /// <returns>The current aead chunk size octet.</returns>
    int GetAeadChunkSizeOctet();
    /// <summary>
    /// Sets the aead chunk size octet used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetAeadChunkSizeOctet(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailOpenPgpMimeOptions Clone();
}
