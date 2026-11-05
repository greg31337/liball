using System;

namespace argon2;

/// <summary>
/// Defines non-allocating Argon2 hashing and raw-tag verification operations.
/// </summary>
public interface IArgon2Primitive
{
    /// <summary>
    /// Computes an Argon2 tag into caller-owned storage.
    /// </summary>
    /// <param name="password">The password or message bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <param name="parameters">The Argon2 algorithm parameters.</param>
    /// <param name="executionOptions">The execution controls.</param>
    /// <param name="destination">The storage that receives the derived tag.</param>
    void ComputeHash(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        Span<byte> destination);

    /// <summary>
    /// Verifies an expected raw tag using the supplied Argon2 inputs.
    /// </summary>
    /// <param name="password">The password or message bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <param name="parameters">The Argon2 algorithm parameters.</param>
    /// <param name="executionOptions">The execution controls.</param>
    /// <param name="expectedTag">The expected raw tag.</param>
    /// <returns><c>true</c> when the computed tag matches; otherwise, <c>false</c>.</returns>
    bool VerifyHash(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        ReadOnlySpan<byte> expectedTag);
}
