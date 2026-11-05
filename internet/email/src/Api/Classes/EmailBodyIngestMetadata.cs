using System;

namespace email;
/// <summary>
/// Represents a body ingest metadata.
/// </summary>
public class EmailBodyIngestMetadata : IEmailBodyIngestMetadataContract
{
    EmailBodyStorageMode storageMode;
    long rawByteCount;
    long decodedByteCount;
    bool decodingAttempted;
    bool decodingComplete;
    EmailHashResult? rawHash;
    EmailHashResult? decodedHash;
    /// <summary>
    /// Creates a new body ingest metadata.
    /// </summary>
    /// <param name = "storageMode">The storage mode used by the operation.</param>
    /// <param name = "rawByteCount">The number of items or bytes to process.</param>
    /// <param name = "decodedByteCount">The number of items or bytes to process.</param>
    /// <param name = "decodingAttempted">Whether to enable the decoding attempted behavior.</param>
    /// <param name = "decodingComplete">Whether to enable the decoding complete behavior.</param>
    /// <param name = "rawHash">The raw hash used by the operation.</param>
    /// <param name = "decodedHash">The decoded hash used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailBodyIngestMetadata(EmailBodyStorageMode storageMode, long rawByteCount, long decodedByteCount, bool decodingAttempted, bool decodingComplete, EmailHashResult? rawHash, EmailHashResult? decodedHash)
    {
        if (storageMode < EmailBodyStorageMode.Lazy || storageMode > EmailBodyStorageMode.Discard)
        {
            throw new ArgumentOutOfRangeException("storageMode");
        }

        if (rawByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("rawByteCount");
        }

        if (decodedByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("decodedByteCount");
        }

        if (!decodingAttempted && (decodingComplete || decodedByteCount != 0 || decodedHash != null))
        {
            throw new ArgumentException("Decoded ingest results require a decoding attempt.", "decodingAttempted");
        }

        if (rawHash != null && rawHash.GetHashedByteCount() != rawByteCount)
        {
            throw new ArgumentException("The raw hash byte count must match the ingested raw byte count.", "rawHash");
        }

        if (decodedHash != null && decodedHash.GetHashedByteCount() != decodedByteCount)
        {
            throw new ArgumentException("The decoded hash byte count must match the ingested decoded byte count.", "decodedHash");
        }

        this.storageMode = storageMode;
        this.rawByteCount = rawByteCount;
        this.decodedByteCount = decodedByteCount;
        this.decodingAttempted = decodingAttempted;
        this.decodingComplete = decodingComplete;
        this.rawHash = rawHash;
        this.decodedHash = decodedHash;
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
    /// Gets the raw byte count.
    /// </summary>
    /// <returns>The current raw byte count.</returns>
    public long GetRawByteCount()
    {
        return rawByteCount;
    }

    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    public long GetDecodedByteCount()
    {
        return decodedByteCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was decoding attempted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasDecodingAttempted()
    {
        return decodingAttempted;
    }

    /// <summary>
    /// Determines whether the current value or state is decoding complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is decoding complete; otherwise, <c>false</c>.</returns>
    public bool IsDecodingComplete()
    {
        return decodingComplete;
    }

    /// <summary>
    /// Determines whether the raw hash is present or available.
    /// </summary>
    /// <returns><c>true</c> when the raw hash is available; otherwise, <c>false</c>.</returns>
    public bool HasRawHash()
    {
        return rawHash != null;
    }

    /// <summary>
    /// Gets the raw hash.
    /// </summary>
    /// <returns>The current raw hash.</returns>
    public EmailHashResult? GetRawHash()
    {
        return rawHash;
    }

    /// <summary>
    /// Determines whether the decoded hash is present or available.
    /// </summary>
    /// <returns><c>true</c> when the decoded hash is available; otherwise, <c>false</c>.</returns>
    public bool HasDecodedHash()
    {
        return decodedHash != null;
    }

    /// <summary>
    /// Gets the decoded hash.
    /// </summary>
    /// <returns>The current decoded hash.</returns>
    public EmailHashResult? GetDecodedHash()
    {
        return decodedHash;
    }
}
