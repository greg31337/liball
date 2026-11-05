using System;
using System.Security.Cryptography;

namespace argon2;

/// <summary>
/// Provides configurable high-level Argon2 password hashing and verification.
/// </summary>
public class Argon2PasswordHasher : IArgon2PasswordHasher
{
    Argon2PasswordHasherOptions optionsSnapshot;

    /// <summary>
    /// Initializes a password hasher with a validated independent copy of its options.
    /// </summary>
    /// <param name="options">The hashing, execution, and verification configuration.</param>
    /// <exception cref="Argon2ParameterException">The options or their nested values are invalid.</exception>
    public Argon2PasswordHasher(Argon2PasswordHasherOptions options)
    {
        optionsSnapshot = Argon2PasswordHasherOptionsSnapshot.Create(options);
    }

    /// <summary>
    /// Hashes password bytes using a fresh random salt.
    /// </summary>
    /// <param name="password">The password bytes to hash.</param>
    /// <returns>The encoded Argon2 hash.</returns>
    public string HashPasswordBytes(ReadOnlySpan<byte> password)
    {
        return HashPasswordBytesWithContext(
            password,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty);
    }

    /// <summary>
    /// Hashes text encoded as UTF-8 using a fresh random salt.
    /// </summary>
    /// <param name="password">The password text to hash without Unicode normalization.</param>
    /// <returns>The encoded Argon2 hash.</returns>
    /// <exception cref="Argon2ParameterException">The password text is <c>null</c>.</exception>
    public string HashPasswordText(string password)
    {
        using (SecureBuffer passwordBuffer = Utf8PasswordEncoding.Encode(password))
        {
            return HashPasswordBytes(passwordBuffer.GetSpan());
        }
    }

    /// <summary>
    /// Hashes password bytes with external secret and associated-data context.
    /// </summary>
    /// <param name="password">The password bytes to hash.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <returns>The encoded Argon2 hash.</returns>
    public string HashPasswordBytesWithContext(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData)
    {
        byte[] salt = RandomSaltGenerator.CreateSalt(optionsSnapshot.SaltLength);
        using (SecureBuffer tagBuffer = new SecureBuffer(optionsSnapshot.TagLength))
        {
            Argon2Core.ComputeHash(
                password,
                salt,
                secret,
                associatedData,
                optionsSnapshot.Parameters,
                optionsSnapshot.ExecutionOptions,
                tagBuffer.GetSpan());

            Argon2EncodedHash hash = new Argon2EncodedHash();
            hash.Parameters = optionsSnapshot.Parameters;
            hash.Salt = salt;
            hash.Tag = tagBuffer.GetArray();
            return Argon2PhcFormatter.Format(hash);
        }
    }

    /// <summary>
    /// Hashes UTF-8 password text with external secret and associated-data context.
    /// </summary>
    /// <param name="password">The password text to hash without Unicode normalization.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <returns>The encoded Argon2 hash.</returns>
    /// <exception cref="Argon2ParameterException">The password text is <c>null</c>.</exception>
    public string HashPasswordTextWithContext(
        string password,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData)
    {
        using (SecureBuffer passwordBuffer = Utf8PasswordEncoding.Encode(password))
        {
            return HashPasswordBytesWithContext(
                passwordBuffer.GetSpan(),
                secret,
                associatedData);
        }
    }

    /// <summary>
    /// Returns an independent snapshot of the hashing options.
    /// </summary>
    /// <returns>The hashing options snapshot.</returns>
    public Argon2PasswordHasherOptions GetHashingOptions()
    {
        return Argon2PasswordHasherOptionsSnapshot.Create(optionsSnapshot);
    }

    /// <summary>
    /// Verifies a password against an encoded Argon2 hash.
    /// </summary>
    /// <param name="password">The password bytes to verify.</param>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <returns>The verification outcome.</returns>
    public Argon2VerificationResult VerifyPasswordBytes(
        ReadOnlySpan<byte> password,
        string encodedHash)
    {
        return VerifyPasswordBytesWithContext(
            password,
            encodedHash,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty);
    }

    /// <summary>
    /// Verifies UTF-8 password text against an encoded Argon2 hash.
    /// </summary>
    /// <param name="password">The password text to verify without Unicode normalization.</param>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <returns>The verification outcome.</returns>
    /// <exception cref="Argon2ParameterException">The password text is <c>null</c>.</exception>
    public Argon2VerificationResult VerifyPasswordText(string password, string encodedHash)
    {
        using (SecureBuffer passwordBuffer = Utf8PasswordEncoding.Encode(password))
        {
            return VerifyPasswordBytes(passwordBuffer.GetSpan(), encodedHash);
        }
    }

    /// <summary>
    /// Verifies password bytes with external secret and associated-data context.
    /// </summary>
    /// <param name="password">The password bytes to verify.</param>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <returns>The verification outcome.</returns>
    public Argon2VerificationResult VerifyPasswordBytesWithContext(
        ReadOnlySpan<byte> password,
        string encodedHash,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData)
    {
        Argon2VerificationResult verificationResult = new Argon2VerificationResult();
        if (encodedHash == null)
        {
            return verificationResult;
        }

        Argon2VerificationPolicy policy = optionsSnapshot.VerificationPolicy;
        if (!Argon2VerificationPolicyEvaluator.IsEncodedLengthAllowed(encodedHash.Length, policy))
        {
            verificationResult.Status = Argon2VerificationStatus.RejectedByPolicy;
            return verificationResult;
        }

        Argon2DecodeResult decodeResult = Argon2PhcParser.Parse(encodedHash.AsSpan());
        Argon2EncodedHash? hash = decodeResult.EncodedHash;
        if (decodeResult.Status != Argon2DecodeStatus.Success || hash == null)
        {
            return verificationResult;
        }

        if (!Argon2VerificationPolicyEvaluator.IsDecodedHashAllowed(hash, policy))
        {
            verificationResult.Status = Argon2VerificationStatus.RejectedByPolicy;
            return verificationResult;
        }

        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = optionsSnapshot.ExecutionOptions.MaximumWorkerCount;
        if (executionOptions.MaximumWorkerCount > hash.Parameters.DegreeOfParallelism)
        {
            executionOptions.MaximumWorkerCount = hash.Parameters.DegreeOfParallelism;
        }

        executionOptions.ClearMemory = optionsSnapshot.ExecutionOptions.ClearMemory;
        using (SecureBuffer computedTagBuffer = new SecureBuffer(hash.Tag.Length))
        {
            Span<byte> computedTag = computedTagBuffer.GetSpan();
            Argon2Core.ComputeHash(
                password,
                hash.Salt,
                secret,
                associatedData,
                hash.Parameters,
                executionOptions,
                computedTag);
            bool matches = CryptographicOperations.FixedTimeEquals(computedTag, hash.Tag);
            if (matches)
            {
                verificationResult.Status = Argon2VerificationStatus.Match;
                verificationResult.NeedsRehash = Argon2RehashEvaluator.NeedsRehash(
                    hash,
                    optionsSnapshot);
            }
            else
            {
                verificationResult.Status = Argon2VerificationStatus.Mismatch;
            }

            return verificationResult;
        }
    }

    /// <summary>
    /// Verifies UTF-8 password text with external secret and associated-data context.
    /// </summary>
    /// <param name="password">The password text to verify without Unicode normalization.</param>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <returns>The verification outcome.</returns>
    /// <exception cref="Argon2ParameterException">The password text is <c>null</c>.</exception>
    public Argon2VerificationResult VerifyPasswordTextWithContext(
        string password,
        string encodedHash,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData)
    {
        using (SecureBuffer passwordBuffer = Utf8PasswordEncoding.Encode(password))
        {
            return VerifyPasswordBytesWithContext(
                passwordBuffer.GetSpan(),
                encodedHash,
                secret,
                associatedData);
        }
    }

    /// <summary>
    /// Determines whether an encoded hash differs from the generation configuration.
    /// </summary>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <returns><c>true</c> when the hash should be replaced; otherwise, <c>false</c>.</returns>
    /// <exception cref="Argon2FormatException">The encoded hash is malformed or unsupported.</exception>
    public bool NeedsRehash(string encodedHash)
    {
        if (encodedHash == null)
        {
            throw new Argon2FormatException("The Argon2 encoded hash is malformed or unsupported.");
        }

        Argon2DecodeResult decodeResult = Argon2PhcParser.Parse(encodedHash.AsSpan());
        Argon2EncodedHash? hash = decodeResult.EncodedHash;
        if (decodeResult.Status != Argon2DecodeStatus.Success || hash == null)
        {
            throw new Argon2FormatException("The Argon2 encoded hash is malformed or unsupported.");
        }

        return Argon2RehashEvaluator.NeedsRehash(hash, optionsSnapshot);
    }
}
