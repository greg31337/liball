using System;

namespace email;
/// <summary>
/// Controls how body ingest operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailBodyIngestOptions : IEmailBodyIngestOptionsContract
{
    EmailBodyStorageMode storageMode;
    bool decodeOnIngest;
    bool computeRawHash;
    bool computeDecodedHash;
    EmailHashAlgorithm hashAlgorithm;
    /// <summary>
    /// Creates a new body ingest options object.
    /// </summary>
    public EmailBodyIngestOptions()
    {
        storageMode = EmailBodyStorageMode.Lazy;
        decodeOnIngest = false;
        computeRawHash = false;
        computeDecodedHash = false;
        hashAlgorithm = EmailHashAlgorithm.Sha256;
    }

    /// <summary>
    /// Gets the storage mode.
    /// </summary>
    /// <returns>The current storage mode.</returns>
    public EmailBodyStorageMode GetStorageMode()
    {
        return storageMode;
    }

    /// <summary>
    /// Sets the storage mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetStorageMode(EmailBodyStorageMode value)
    {
        if (value < EmailBodyStorageMode.Lazy || value > EmailBodyStorageMode.Discard)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        storageMode = value;
    }

    /// <summary>
    /// Gets the decode on ingest.
    /// </summary>
    /// <returns><c>true</c> when the decode on ingest setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetDecodeOnIngest()
    {
        return decodeOnIngest;
    }

    /// <summary>
    /// Sets the decode on ingest used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the decode on ingest behavior.</param>
    public void SetDecodeOnIngest(bool value)
    {
        decodeOnIngest = value;
    }

    /// <summary>
    /// Gets the compute raw hash.
    /// </summary>
    /// <returns><c>true</c> when the compute raw hash setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetComputeRawHash()
    {
        return computeRawHash;
    }

    /// <summary>
    /// Sets the compute raw hash used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the compute raw hash behavior.</param>
    public void SetComputeRawHash(bool value)
    {
        computeRawHash = value;
    }

    /// <summary>
    /// Gets the compute decoded hash.
    /// </summary>
    /// <returns><c>true</c> when the compute decoded hash setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetComputeDecodedHash()
    {
        return computeDecodedHash;
    }

    /// <summary>
    /// Sets the compute decoded hash used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the compute decoded hash behavior.</param>
    public void SetComputeDecodedHash(bool value)
    {
        computeDecodedHash = value;
    }

    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    public EmailHashAlgorithm GetHashAlgorithm()
    {
        return hashAlgorithm;
    }

    /// <summary>
    /// Sets the hash algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetHashAlgorithm(EmailHashAlgorithm value)
    {
        if (value < EmailHashAlgorithm.Sha256 || value > EmailHashAlgorithm.Sha512)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        hashAlgorithm = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailBodyIngestOptions Clone()
    {
        EmailBodyIngestOptions copy = new EmailBodyIngestOptions();
        copy.SetStorageMode(storageMode);
        copy.SetDecodeOnIngest(decodeOnIngest);
        copy.SetComputeRawHash(computeRawHash);
        copy.SetComputeDecodedHash(computeDecodedHash);
        copy.SetHashAlgorithm(hashAlgorithm);
        return copy;
    }
}
