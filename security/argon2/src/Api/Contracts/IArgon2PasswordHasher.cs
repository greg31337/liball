using System;

namespace argon2;

/// <summary>
/// Defines high-level Argon2 password hashing and verification operations.
/// </summary>
public interface IArgon2PasswordHasher
{
    /// <summary>
    /// Hashes password bytes using a fresh random salt.
    /// </summary>
    /// <param name="password">The password bytes to hash.</param>
    /// <returns>The encoded Argon2 hash.</returns>
    string HashPasswordBytes(ReadOnlySpan<byte> password);

    /// <summary>
    /// Hashes text encoded as UTF-8 using a fresh random salt.
    /// </summary>
    /// <param name="password">The password text to hash without Unicode normalization.</param>
    /// <returns>The encoded Argon2 hash.</returns>
    string HashPasswordText(string password);

    /// <summary>
    /// Hashes password bytes with external secret and associated-data context.
    /// </summary>
    /// <param name="password">The password bytes to hash.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <returns>The encoded Argon2 hash.</returns>
    string HashPasswordBytesWithContext(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData);

    /// <summary>
    /// Hashes UTF-8 password text with external secret and associated-data context.
    /// </summary>
    /// <param name="password">The password text to hash without Unicode normalization.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <returns>The encoded Argon2 hash.</returns>
    string HashPasswordTextWithContext(
        string password,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData);

    /// <summary>
    /// Returns an independent snapshot of the hashing options.
    /// </summary>
    /// <returns>The hashing options snapshot.</returns>
    Argon2PasswordHasherOptions GetHashingOptions();

    /// <summary>
    /// Verifies a password against an encoded Argon2 hash.
    /// </summary>
    /// <param name="password">The password bytes to verify.</param>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <returns>The verification outcome.</returns>
    Argon2VerificationResult VerifyPasswordBytes(ReadOnlySpan<byte> password, string encodedHash);

    /// <summary>
    /// Verifies UTF-8 password text against an encoded Argon2 hash.
    /// </summary>
    /// <param name="password">The password text to verify without Unicode normalization.</param>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <returns>The verification outcome.</returns>
    Argon2VerificationResult VerifyPasswordText(string password, string encodedHash);

    /// <summary>
    /// Verifies password bytes with external secret and associated-data context.
    /// </summary>
    /// <param name="password">The password bytes to verify.</param>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <returns>The verification outcome.</returns>
    Argon2VerificationResult VerifyPasswordBytesWithContext(
        ReadOnlySpan<byte> password,
        string encodedHash,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData);

    /// <summary>
    /// Verifies UTF-8 password text with external secret and associated-data context.
    /// </summary>
    /// <param name="password">The password text to verify without Unicode normalization.</param>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <returns>The verification outcome.</returns>
    Argon2VerificationResult VerifyPasswordTextWithContext(
        string password,
        string encodedHash,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData);

    /// <summary>
    /// Determines whether an encoded hash differs from the generation configuration.
    /// </summary>
    /// <param name="encodedHash">The encoded Argon2 hash.</param>
    /// <returns><c>true</c> when the hash should be replaced; otherwise, <c>false</c>.</returns>
    bool NeedsRehash(string encodedHash);
}
