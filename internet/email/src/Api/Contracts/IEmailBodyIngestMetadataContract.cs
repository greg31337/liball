using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBodyIngestMetadata"/>.
/// </summary>
public interface IEmailBodyIngestMetadataContract
{
    /// <summary>
    /// Gets the storage mode.
    /// </summary>
    /// <returns>The current storage mode.</returns>
    EmailBodyStorageMode GetStorageMode();
    /// <summary>
    /// Gets the raw byte count.
    /// </summary>
    /// <returns>The current raw byte count.</returns>
    long GetRawByteCount();
    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    long GetDecodedByteCount();
    /// <summary>
    /// Determines whether the operation or source value was decoding attempted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasDecodingAttempted();
    /// <summary>
    /// Determines whether the current value or state is decoding complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is decoding complete; otherwise, <c>false</c>.</returns>
    bool IsDecodingComplete();
    /// <summary>
    /// Determines whether the raw hash is present or available.
    /// </summary>
    /// <returns><c>true</c> when the raw hash is available; otherwise, <c>false</c>.</returns>
    bool HasRawHash();
    /// <summary>
    /// Gets the raw hash.
    /// </summary>
    /// <returns>The current raw hash.</returns>
    EmailHashResult? GetRawHash();
    /// <summary>
    /// Determines whether the decoded hash is present or available.
    /// </summary>
    /// <returns><c>true</c> when the decoded hash is available; otherwise, <c>false</c>.</returns>
    bool HasDecodedHash();
    /// <summary>
    /// Gets the decoded hash.
    /// </summary>
    /// <returns>The current decoded hash.</returns>
    EmailHashResult? GetDecodedHash();
}
