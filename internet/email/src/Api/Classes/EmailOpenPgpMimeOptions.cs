using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Controls how openPGP MIME operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailOpenPgpMimeOptions : IEmailOpenPgpMimeOptionsContract
{
    EmailCryptographicLimits limits;
    EmailOpenPgpAlgorithmOptions algorithmOptions;
    bool includeArmorChecksum;
    bool hasSignatureCreationTime;
    uint signatureCreationTime;
    int aeadChunkSizeOctet;
    /// <summary>
    /// Creates a new openPGP MIME options object.
    /// </summary>
    public EmailOpenPgpMimeOptions()
    {
        limits = new EmailCryptographicLimits();
        algorithmOptions = new EmailOpenPgpAlgorithmOptions();
        includeArmorChecksum = true;
        hasSignatureCreationTime = false;
        signatureCreationTime = 0;
        aeadChunkSizeOctet = 14;
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
        algorithmOptions.SetLimits(limits);
    }

    /// <summary>
    /// Gets the algorithm options.
    /// </summary>
    /// <returns>The current algorithm options.</returns>
    public EmailOpenPgpAlgorithmOptions GetAlgorithmOptions()
    {
        return algorithmOptions.Clone();
    }

    /// <summary>
    /// Sets the algorithm options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetAlgorithmOptions(EmailOpenPgpAlgorithmOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        algorithmOptions = value.Clone();
        limits = algorithmOptions.GetLimits();
    }

    /// <summary>
    /// Gets the include armor checksum.
    /// </summary>
    /// <returns><c>true</c> when the include armor checksum setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeArmorChecksum()
    {
        return includeArmorChecksum;
    }

    /// <summary>
    /// Sets the include armor checksum used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include armor checksum behavior.</param>
    public void SetIncludeArmorChecksum(bool value)
    {
        includeArmorChecksum = value;
    }

    /// <summary>
    /// Determines whether the signature creation time is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signature creation time is available; otherwise, <c>false</c>.</returns>
    public bool HasSignatureCreationTime()
    {
        return hasSignatureCreationTime;
    }

    /// <summary>
    /// Gets the signature creation time.
    /// </summary>
    /// <returns>The current signature creation time.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public uint GetSignatureCreationTime()
    {
        if (!hasSignatureCreationTime)
            throw new InvalidOperationException("No fixed signature creation time is configured.");
        return signatureCreationTime;
    }

    /// <summary>
    /// Sets the signature creation time used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetSignatureCreationTime(uint value)
    {
        signatureCreationTime = value;
        hasSignatureCreationTime = true;
    }

    /// <summary>
    /// Clears the stored signature creation time value or values.
    /// </summary>
    public void ClearSignatureCreationTime()
    {
        signatureCreationTime = 0;
        hasSignatureCreationTime = false;
    }

    /// <summary>
    /// Gets the aead chunk size octet.
    /// </summary>
    /// <returns>The current aead chunk size octet.</returns>
    public int GetAeadChunkSizeOctet()
    {
        return aeadChunkSizeOctet;
    }

    /// <summary>
    /// Sets the aead chunk size octet used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetAeadChunkSizeOctet(int value)
    {
        if (value < 0 || value > 16)
            throw new ArgumentOutOfRangeException("value");
        aeadChunkSizeOctet = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailOpenPgpMimeOptions Clone()
    {
        EmailOpenPgpMimeOptions copy = new EmailOpenPgpMimeOptions();
        copy.SetAlgorithmOptions(algorithmOptions);
        copy.SetIncludeArmorChecksum(includeArmorChecksum);
        if (hasSignatureCreationTime)
            copy.SetSignatureCreationTime(signatureCreationTime);
        copy.SetAeadChunkSizeOctet(aeadChunkSizeOctet);
        return copy;
    }
}
