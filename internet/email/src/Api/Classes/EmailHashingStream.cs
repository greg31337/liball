using System;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Represents a hashing stream.
/// </summary>
public class EmailHashingStream : IDisposable, IEmailHashingStreamContract
{
    EmailHashAlgorithm algorithm;
    IncrementalHash? hash;
    long byteCount;
    bool completed;
    /// <summary>
    /// Creates a new hashing stream.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    public EmailHashingStream(EmailHashAlgorithm algorithm)
    {
        this.algorithm = algorithm;
        hash = IncrementalHash.CreateHash(GetAlgorithmName(algorithm));
        byteCount = 0;
        completed = false;
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    public void Write(ReadOnlySpan<byte> value)
    {
        EnsureActive();
        if (value.Length > long.MaxValue - byteCount)
        {
            throw new OverflowException("The hashed output exceeds Int64.MaxValue.");
        }

        (hash ?? throw new InvalidOperationException()).AppendData(value);
        byteCount += value.Length;
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <param name = "count">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void WriteSegment(byte[] value, int offset, int count)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (offset < 0 || count < 0 || offset > value.Length || count > value.Length - offset)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        Write(new ReadOnlySpan<byte>(value, offset, count));
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public long GetByteCount()
    {
        return byteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return completed;
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult Complete()
    {
        EnsureActive();
        byte[] digest = (hash ?? throw new InvalidOperationException()).GetHashAndReset();
        hash.Dispose();
        hash = null;
        completed = true;
        return new EmailHashResult(algorithm, digest, byteCount);
    }

    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    public void Close()
    {
        if (completed)
        {
            return;
        }

        (hash ?? throw new InvalidOperationException()).Dispose();
        hash = null;
        completed = true;
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    void EnsureActive()
    {
        if (completed)
        {
            throw new InvalidOperationException("The hashing output is already complete.");
        }
    }

    static HashAlgorithmName GetAlgorithmName(EmailHashAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case EmailHashAlgorithm.Sha1:
                return HashAlgorithmName.SHA1;
            case EmailHashAlgorithm.Sha256:
                return HashAlgorithmName.SHA256;
            case EmailHashAlgorithm.Sha384:
                return HashAlgorithmName.SHA384;
            case EmailHashAlgorithm.Sha512:
                return HashAlgorithmName.SHA512;
            default:
                throw new ArgumentOutOfRangeException("algorithm");
        }
    }
}
