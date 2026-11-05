using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Generates boundary using explicit policy settings.
/// </summary>
public class EmailBoundaryGenerator : IEmailBoundaryGeneratorContract
{
    /// <summary>
    /// The default random byte count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultRandomByteCount = 24;
    /// <summary>
    /// The default buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultBufferBytes = 81920;
    /// <summary>
    /// The maximum attempts value exposed by this API.
    /// </summary>
    internal const int MaximumAttempts = 64;
    /// <summary>
    /// The boundary prefix value exposed by this API.
    /// </summary>
    internal const string BoundaryPrefix = "=_email_";
    internal static EmailBoundaryGenerationResult GenerateImplementationWith()
    {
        return GenerateImplementationWithContentsAndOptions(new List<EmailContent>(), new EmailWriteOptions());
    }

    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <returns>The boundary generation result produced by the operation.</returns>
    public EmailBoundaryGenerationResult GenerateWith()
    {
        return EmailBoundaryGenerator.GenerateImplementationWith();
    }

    internal static EmailBoundaryGenerationResult GenerateImplementationWithOptions(EmailWriteOptions options)
    {
        return GenerateImplementationWithContentsAndOptions(new List<EmailContent>(), options);
    }

    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The boundary generation result produced by the operation.</returns>
    public EmailBoundaryGenerationResult GenerateWithOptions(EmailWriteOptions options)
    {
        return EmailBoundaryGenerator.GenerateImplementationWithOptions(options);
    }

    internal static EmailBoundaryGenerationResult GenerateImplementationWithContentsAndOptions(List<EmailContent> contents, EmailWriteOptions options)
    {
        Validate(contents, options);
        EmailWriteOptions snapshot = options.Clone();
        int attempt = 0;
        int collisions = 0;
        long scannedBytes = 0;
        while (attempt < MaximumAttempts)
        {
            string candidate = CreateCandidateImplementation(snapshot, attempt);
            EmailBoundaryCollisionScan scan = HasCollision(candidate, contents);
            scannedBytes = new EmailSafeArithmetic().AddNonNegative(scannedBytes, scan.GetScannedByteCount());
            attempt++;
            if (!scan.HasCollision())
            {
                return new EmailBoundaryGenerationResult(candidate, attempt, collisions, scannedBytes, snapshot.GetDeterministicOutput());
            }

            collisions++;
        }

        throw new EmailLimitException("MaxBoundaryGenerationAttempts", MaximumAttempts, MaximumAttempts + 1);
    }

    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "contents">The contents used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The boundary generation result produced by the operation.</returns>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailBoundaryGenerationResult GenerateWithContentsAndOptions(List<EmailContent> contents, EmailWriteOptions options)
    {
        return EmailBoundaryGenerator.GenerateImplementationWithContentsAndOptions(contents, options);
    }

    internal static string CreateCandidateImplementation(EmailWriteOptions options, int attempt)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException("attempt");
        }

        byte[] random = options.GetDeterministicOutput() ? CreateDeterministicBytes(options.GetDeterministicSeed(), attempt) : CreateRandomBytes();
        return BoundaryPrefix + ToHex(random);
    }

    /// <summary>
    /// Creates a candidate configured for the requested behavior.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "attempt">The attempt used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string CreateCandidate(EmailWriteOptions options, int attempt)
    {
        return EmailBoundaryGenerator.CreateCandidateImplementation(options, attempt);
    }

    static EmailBoundaryCollisionScan HasCollision(string boundary, List<EmailContent> contents)
    {
        long scannedBytes = 0;
        int index = 0;
        while (index < contents.Count)
        {
            EmailContent content = contents[index];
            EmailBoundaryCollisionScanner scanner = new EmailBoundaryCollisionScanner(boundary);
            Stream? input = content.OpenRead();
            byte[] buffer = new byte[DefaultBufferBytes];
            bool collision = false;
            while (!collision)
            {
                int read = (input ?? throw new InvalidOperationException()).Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                scannedBytes = new EmailSafeArithmetic().AddNonNegative(scannedBytes, read);
                collision = scanner.Feed(new ReadOnlySpan<byte>(buffer, 0, read));
            }

            if (!collision)
            {
                collision = scanner.Complete();
            }

            if (content.GetKind() != EmailContentKind.Stream)
            {
                (input ?? throw new InvalidOperationException()).Close();
            }

            if (collision)
            {
                return new EmailBoundaryCollisionScan(true, scannedBytes);
            }

            index++;
        }

        return new EmailBoundaryCollisionScan(false, scannedBytes);
    }

    static byte[] CreateRandomBytes()
    {
        byte[] value = new byte[DefaultRandomByteCount];
        RandomNumberGenerator.Fill(value);
        return value;
    }

    static byte[] CreateDeterministicBytes(byte[] seed, int attempt)
    {
        byte[] input = new byte[seed.Length + 12];
        input[0] = 69;
        input[1] = 77;
        input[2] = 65;
        input[3] = 73;
        Array.Copy(seed, 0, input, 4, seed.Length);
        int offset = seed.Length + 4;
        input[offset] = (byte)(attempt >> 24 & 255);
        input[offset + 1] = (byte)(attempt >> 16 & 255);
        input[offset + 2] = (byte)(attempt >> 8 & 255);
        input[offset + 3] = (byte)(attempt & 255);
        input[offset + 4] = 66;
        input[offset + 5] = 79;
        input[offset + 6] = 85;
        input[offset + 7] = 78;
        byte[] hash = SHA256.HashData(input);
        byte[] value = new byte[DefaultRandomByteCount];
        Array.Copy(hash, value, value.Length);
        return value;
    }

    static string ToHex(byte[] value)
    {
        char[] characters = new char[value.Length * 2];
        int index = 0;
        while (index < value.Length)
        {
            int current = value[index];
            characters[index * 2] = Hex(current >> 4);
            characters[index * 2 + 1] = Hex(current & 15);
            index++;
        }

        return new string (characters);
    }

    static char Hex(int value)
    {
        return (char)(value < 10 ? 48 + value : 87 + value);
    }

    static void Validate(List<EmailContent> contents, EmailWriteOptions options)
    {
        if (contents == null)
        {
            throw new ArgumentNullException("contents");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        int index = 0;
        while (index < contents.Count)
        {
            EmailContent content = contents[index];
            if (content == null)
            {
                throw new ArgumentException("Boundary collision content cannot contain null entries.", "contents");
            }

            if (content.IsClosed())
            {
                throw new ObjectDisposedException("contents");
            }

            if (!content.IsRepeatable())
            {
                throw new ArgumentException("Boundary collision retry requires repeatable content.", "contents");
            }

            index++;
        }
    }
}
