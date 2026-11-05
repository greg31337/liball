using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBodyIngestOptions"/>.
/// </summary>
public interface IEmailBodyIngestOptionsContract
{
    /// <summary>
    /// Gets the storage mode.
    /// </summary>
    /// <returns>The current storage mode.</returns>
    EmailBodyStorageMode GetStorageMode();
    /// <summary>
    /// Sets the storage mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetStorageMode(EmailBodyStorageMode value);
    /// <summary>
    /// Gets the decode on ingest.
    /// </summary>
    /// <returns><c>true</c> when the decode on ingest setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetDecodeOnIngest();
    /// <summary>
    /// Sets the decode on ingest used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the decode on ingest behavior.</param>
    void SetDecodeOnIngest(bool value);
    /// <summary>
    /// Gets the compute raw hash.
    /// </summary>
    /// <returns><c>true</c> when the compute raw hash setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetComputeRawHash();
    /// <summary>
    /// Sets the compute raw hash used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the compute raw hash behavior.</param>
    void SetComputeRawHash(bool value);
    /// <summary>
    /// Gets the compute decoded hash.
    /// </summary>
    /// <returns><c>true</c> when the compute decoded hash setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetComputeDecodedHash();
    /// <summary>
    /// Sets the compute decoded hash used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the compute decoded hash behavior.</param>
    void SetComputeDecodedHash(bool value);
    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    EmailHashAlgorithm GetHashAlgorithm();
    /// <summary>
    /// Sets the hash algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetHashAlgorithm(EmailHashAlgorithm value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailBodyIngestOptions Clone();
}
