using System;

namespace argon2;

/// <summary>
/// Defines allocating one-shot Argon2 key derivation.
/// </summary>
public interface IArgon2KeyDeriver
{
    /// <summary>
    /// Derives a key with the requested output length.
    /// </summary>
    /// <param name="password">The password or message bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <param name="parameters">The Argon2 algorithm parameters.</param>
    /// <param name="executionOptions">The execution controls.</param>
    /// <param name="outputLength">The required key length in bytes.</param>
    /// <returns>A newly allocated array containing the derived key.</returns>
    byte[] DeriveKey(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        int outputLength);
}
