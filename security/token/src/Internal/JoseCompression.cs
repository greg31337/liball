using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Applies policy-gated JWE compression using the <c>DEF</c> algorithm.
/// </summary>
internal static class JoseCompression
{
    const int BufferSize = 8192;

    /// <summary>Compresses plaintext using the JWE compression algorithm.</summary>
    /// <param name="plaintext">The plaintext bytes to compress.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>zip</c> algorithm name.</param>
    /// <param name="policy">The compression opt-in and expansion policy.</param>
    /// <returns>A result containing raw DEFLATE bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Compress(
        byte[] plaintext,
        string algorithm,
        TokenPolicy policy)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        return Compress(plaintext.AsSpan(), algorithm, policy);
    }

    /// <summary>Compresses a plaintext span using the JWE compression algorithm.</summary>
    /// <param name="plaintext">The plaintext bytes to compress.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>zip</c> algorithm name.</param>
    /// <param name="policy">The compression opt-in and expansion policy.</param>
    /// <returns>A result containing raw DEFLATE bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Compress(
        ReadOnlySpan<byte> plaintext,
        string algorithm,
        TokenPolicy policy)
    {
        ValidateReferences(algorithm, policy);
        TokenResult validation = Validate(algorithm, policy);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        if (plaintext.Length > policy.MaximumDecompressedSizeInBytes)
        {
            return CompressionLimitFailure();
        }

        byte[] input = plaintext.ToArray();
        Task<JoseCompressionOperationResult> operation = Task.Run(
            () => CompressCore(input, policy.MaximumTokenSizeInBytes));
        Wait(operation);
        Array.Clear(input, 0, input.Length);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            return Failure(
                "compression_failed",
                "The JWE plaintext could not be compressed.");
        }

        JoseCompressionOperationResult completed = operation.Result;
        if (completed.LimitExceeded)
        {
            return Failure(
                "compression_limit_exceeded",
                "The compressed JWE plaintext exceeds the configured token size limit.");
        }

        if (!completed.Success)
        {
            return Failure(
                "compression_failed",
                "The JWE plaintext could not be compressed.");
        }

        return Success(completed.Bytes);
    }

    /// <summary>Decompresses JWE compressed plaintext while enforcing expansion limits.</summary>
    /// <param name="compressed">The raw DEFLATE bytes to decompress.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>zip</c> algorithm name.</param>
    /// <param name="policy">The compression opt-in and expansion policy.</param>
    /// <returns>A result containing decompressed plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Decompress(
        byte[] compressed,
        string algorithm,
        TokenPolicy policy)
    {
        if (compressed == null)
        {
            throw new ArgumentNullException("compressed");
        }

        return Decompress(compressed.AsSpan(), algorithm, policy);
    }

    /// <summary>Decompresses a JWE compressed-plaintext span with expansion limits.</summary>
    /// <param name="compressed">The raw DEFLATE bytes to decompress.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>zip</c> algorithm name.</param>
    /// <param name="policy">The compression opt-in and expansion policy.</param>
    /// <returns>A result containing decompressed plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Decompress(
        ReadOnlySpan<byte> compressed,
        string algorithm,
        TokenPolicy policy)
    {
        ValidateReferences(algorithm, policy);
        TokenResult validation = Validate(algorithm, policy);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        if (compressed.Length > policy.MaximumTokenSizeInBytes)
        {
            return LimitFailure();
        }

        long ratioLimit = (long)compressed.Length
            * policy.MaximumCompressionRatio;
        int outputLimit = policy.MaximumDecompressedSizeInBytes;
        if (ratioLimit < outputLimit)
        {
            outputLimit = (int)ratioLimit;
        }

        byte[] input = compressed.ToArray();
        Task<JoseCompressionOperationResult> operation = Task.Run(
            () => DecompressCore(input, outputLimit));
        Wait(operation);
        Array.Clear(input, 0, input.Length);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            return Failure(
                "decompression_failed",
                "The JWE compressed plaintext is invalid.");
        }

        JoseCompressionOperationResult completed = operation.Result;
        if (completed.LimitExceeded)
        {
            return LimitFailure();
        }

        if (!completed.Success)
        {
            return Failure(
                "decompression_failed",
                "The JWE compressed plaintext is invalid.");
        }

        return Success(completed.Bytes);
    }

    static JoseCompressionOperationResult CompressCore(
        byte[] plaintext,
        int outputLimit)
    {
        JoseCompressionOperationResult result =
            new JoseCompressionOperationResult();
        using (MemoryStream output = new MemoryStream())
        {
            using (DeflateStream compressor = new DeflateStream(
                output,
                CompressionLevel.Optimal,
                true))
            {
                compressor.Write(plaintext, 0, plaintext.Length);
            }

            if (output.Length > outputLimit)
            {
                Array.Clear(
                    output.GetBuffer(),
                    0,
                    (int)output.Length);
                result.LimitExceeded = true;
                return result;
            }

            result.Success = true;
            result.Bytes = output.ToArray();
            return result;
        }
    }

    static JoseCompressionOperationResult DecompressCore(
        byte[] compressed,
        int outputLimit)
    {
        JoseCompressionOperationResult result =
            new JoseCompressionOperationResult();
        using (MemoryStream input = new MemoryStream(compressed, false))
        using (DeflateStream decompressor = new DeflateStream(
            input,
            CompressionMode.Decompress,
            true))
        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[BufferSize];
            int count = decompressor.Read(buffer, 0, buffer.Length);
            while (count != 0)
            {
                if (output.Length + count > outputLimit)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    Array.Clear(
                        output.GetBuffer(),
                        0,
                        (int)output.Length);
                    result.LimitExceeded = true;
                    return result;
                }

                output.Write(buffer, 0, count);
                count = decompressor.Read(buffer, 0, buffer.Length);
            }

            Array.Clear(buffer, 0, buffer.Length);
            result.Success = true;
            result.Bytes = output.ToArray();
            return result;
        }
    }

    static TokenResult Validate(string algorithm, TokenPolicy policy)
    {
        if (!string.Equals(
            algorithm,
            JoseCompressionAlgorithms.Deflate,
            StringComparison.Ordinal))
        {
            if (!JoseCompressionAlgorithmsOperations.IsRegistered(algorithm))
            {
                return ValidationFailure(
                    "unknown_compression_algorithm",
                    "The JWE compression algorithm is not registered.");
            }

            return ValidationFailure(
                "unsupported_compression_algorithm",
                "The JWE compression algorithm is not supported.");
        }

        if (!policy.AllowCompression)
        {
            return ValidationFailure(
                "compression_disabled",
                "JWE compression is disabled by policy.");
        }

        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumDecompressedSizeInBytes <= 0
            || policy.MaximumCompressionRatio <= 0)
        {
            return ValidationFailure(
                "invalid_compression_policy",
                "The JWE compression policy limits are invalid.");
        }

        return ValidationSuccess();
    }

    static void ValidateReferences(string algorithm, TokenPolicy policy)
    {
        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }

    static void Wait(Task<JoseCompressionOperationResult> operation)
    {
        while (!operation.IsCompleted)
        {
            Thread.Yield();
        }
    }

    static void ObserveFailure(
        Task<JoseCompressionOperationResult> operation)
    {
        AggregateException? failure = operation.Exception;
        if (failure != null)
        {
            failure.Flatten();
        }
    }

    static TokenBytesResult Success(byte[]? bytes)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = bytes;
        return result;
    }

    static TokenBytesResult LimitFailure()
    {
        return Failure(
            "decompression_limit_exceeded",
            "The JWE plaintext exceeds the configured decompression expansion limit.");
    }

    static TokenBytesResult CompressionLimitFailure()
    {
        return Failure(
            "compression_limit_exceeded",
            "The JWE plaintext exceeds the configured compression size limit.");
    }

    static TokenBytesResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static TokenBytesResult Failure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
