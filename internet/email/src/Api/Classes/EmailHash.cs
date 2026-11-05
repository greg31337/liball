using System;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Provides operations for working with hash.
/// </summary>
public class EmailHash : IEmailHashContract
{
    /// <summary>
    /// The default buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultBufferBytes = 81920;
    internal static EmailHashResult ComputeExactImplementationWithContentAndAlgorithmAndMaximumBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        return ComputeExactImplementationWithContentAndAlgorithmAndMaximumBytesAndBufferBytes(content, algorithm, maximumBytes, DefaultBufferBytes);
    }

    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeExactWithContentAndAlgorithmAndMaximumBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        return EmailHash.ComputeExactImplementationWithContentAndAlgorithmAndMaximumBytes(content, algorithm, maximumBytes);
    }

    internal static EmailHashResult ComputeExactImplementationWithContentAndAlgorithmAndMaximumBytesAndBufferBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes, int bufferBytes)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        ValidateArguments(algorithm, maximumBytes, bufferBytes);
        if (content.HasKnownLength() && content.GetLength() > maximumBytes)
        {
            throw new EmailLimitException("MaxHashBytes", maximumBytes, content.GetLength());
        }

        if (content.GetKind() == EmailContentKind.Stream)
        {
            Stream? borrowed = content.OpenRead();
            return ComputeStream(borrowed, algorithm, maximumBytes, bufferBytes);
        }

        using (Stream? input = content.OpenRead())
        {
            return ComputeStream(input, algorithm, maximumBytes, bufferBytes);
        }
    }

    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "bufferBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailHashResult ComputeExactWithContentAndAlgorithmAndMaximumBytesAndBufferBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes, int bufferBytes)
    {
        return EmailHash.ComputeExactImplementationWithContentAndAlgorithmAndMaximumBytesAndBufferBytes(content, algorithm, maximumBytes, bufferBytes);
    }

    internal static EmailHashResult ComputeExactImplementationWithContentAndAlgorithm(ReadOnlySpan<byte> content, EmailHashAlgorithm algorithm)
    {
        return ComputeSpan(content, algorithm);
    }

    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeExactWithContentAndAlgorithm(ReadOnlySpan<byte> content, EmailHashAlgorithm algorithm)
    {
        return EmailHash.ComputeExactImplementationWithContentAndAlgorithm(content, algorithm);
    }

    internal static EmailHashResult ComputeDecodedImplementationWithDecodedContentAndAlgorithmAndMaximumBytes(Stream decodedContent, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        return ComputeDecodedImplementationWithDecodedContentAndAlgorithmAndMaximumBytesAndBufferBytes(decodedContent, algorithm, maximumBytes, DefaultBufferBytes);
    }

    /// <summary>
    /// Computes the decoded.
    /// </summary>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytes(Stream decodedContent, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        return EmailHash.ComputeDecodedImplementationWithDecodedContentAndAlgorithmAndMaximumBytes(decodedContent, algorithm, maximumBytes);
    }

    internal static EmailHashResult ComputeDecodedImplementationWithDecodedContentAndAlgorithmAndMaximumBytesAndBufferBytes(Stream decodedContent, EmailHashAlgorithm algorithm, long maximumBytes, int bufferBytes)
    {
        if (decodedContent == null)
        {
            throw new ArgumentNullException("decodedContent");
        }

        if (!decodedContent.CanRead)
        {
            throw new ArgumentException("The decoded-content stream must be readable.", "decodedContent");
        }

        ValidateArguments(algorithm, maximumBytes, bufferBytes);
        if (decodedContent.CanSeek)
        {
            long position = decodedContent.Position;
            long length = decodedContent.Length;
            if (position > length)
            {
                throw new ArgumentException("The decoded-content stream position exceeds its length.", "decodedContent");
            }

            long remaining = length - position;
            if (remaining > maximumBytes)
            {
                throw new EmailLimitException("MaxHashBytes", maximumBytes, remaining);
            }
        }

        return ComputeStream(decodedContent, algorithm, maximumBytes, bufferBytes);
    }

    /// <summary>
    /// Computes the decoded.
    /// </summary>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "bufferBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailHashResult ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytesAndBufferBytes(Stream decodedContent, EmailHashAlgorithm algorithm, long maximumBytes, int bufferBytes)
    {
        return EmailHash.ComputeDecodedImplementationWithDecodedContentAndAlgorithmAndMaximumBytesAndBufferBytes(decodedContent, algorithm, maximumBytes, bufferBytes);
    }

    internal static EmailHashResult ComputeDecodedImplementationWithDecodedContentAndAlgorithm(ReadOnlySpan<byte> decodedContent, EmailHashAlgorithm algorithm)
    {
        return ComputeSpan(decodedContent, algorithm);
    }

    /// <summary>
    /// Computes the decoded.
    /// </summary>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeDecodedWithDecodedContentAndAlgorithm(ReadOnlySpan<byte> decodedContent, EmailHashAlgorithm algorithm)
    {
        return EmailHash.ComputeDecodedImplementationWithDecodedContentAndAlgorithm(decodedContent, algorithm);
    }

    static EmailHashResult ComputeSpan(ReadOnlySpan<byte> content, EmailHashAlgorithm algorithm)
    {
        HashAlgorithmName algorithmName = GetAlgorithmName(algorithm);
        using (IncrementalHash hash = IncrementalHash.CreateHash(algorithmName))
        {
            hash.AppendData(content);
            return new EmailHashResult(algorithm, hash.GetHashAndReset(), content.Length);
        }
    }

    static EmailHashResult ComputeStream(Stream? input, EmailHashAlgorithm algorithm, long maximumBytes, int bufferBytes)
    {
        HashAlgorithmName algorithmName = GetAlgorithmName(algorithm);
        using (IncrementalHash hash = IncrementalHash.CreateHash(algorithmName))
        {
            byte[] transfer = new byte[bufferBytes];
            long total = 0;
            while (true)
            {
                int read = (input ?? throw new InvalidOperationException()).Read(transfer, 0, transfer.Length);
                if (read == 0)
                {
                    break;
                }

                if (read > maximumBytes - total)
                {
                    CryptographicOperations.ZeroMemory(transfer);
                    throw new EmailLimitException("MaxHashBytes", maximumBytes, total + read);
                }

                hash.AppendData(transfer, 0, read);
                total += read;
            }

            byte[] digest = hash.GetHashAndReset();
            CryptographicOperations.ZeroMemory(transfer);
            return new EmailHashResult(algorithm, digest, total);
        }
    }

    static void ValidateArguments(EmailHashAlgorithm algorithm, long maximumBytes, int bufferBytes)
    {
        GetAlgorithmName(algorithm);
        if (maximumBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumBytes");
        }

        if (bufferBytes < 1)
        {
            throw new ArgumentOutOfRangeException("bufferBytes");
        }
    }

    static HashAlgorithmName GetAlgorithmName(EmailHashAlgorithm algorithm)
    {
        switch (algorithm)
        {
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
