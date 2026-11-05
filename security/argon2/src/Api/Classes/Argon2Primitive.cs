using System;
using System.Security.Cryptography;

namespace argon2;

/// <summary>
/// Provides destination-based Argon2 hashing and fixed-time raw-tag verification.
/// </summary>
public class Argon2Primitive : IArgon2Primitive
{
    /// <summary>
    /// Initializes a stateless Argon2 primitive.
    /// </summary>
    public Argon2Primitive()
    {
    }

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
    /// <exception cref="Argon2ParameterException">A parameter, input, or destination is invalid.</exception>
    public void ComputeHash(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        Span<byte> destination)
    {
        Argon2Core.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            destination);
    }

    /// <summary>
    /// Verifies an expected raw Argon2 tag using a fixed-time comparison.
    /// </summary>
    /// <param name="password">The password or message bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <param name="parameters">The Argon2 algorithm parameters.</param>
    /// <param name="executionOptions">The execution controls.</param>
    /// <param name="expectedTag">The expected raw tag.</param>
    /// <returns><c>true</c> when the computed tag matches; otherwise, <c>false</c>.</returns>
    /// <exception cref="Argon2ParameterException">A parameter, input, or expected tag is invalid.</exception>
    public bool VerifyHash(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        ReadOnlySpan<byte> expectedTag)
    {
        Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters);
        Argon2ParameterValidator.ValidateInputLengths(
            password,
            salt,
            secret,
            associatedData,
            expectedTag.Length);

        using (SecureBuffer computedTagBuffer = new SecureBuffer(expectedTag.Length))
        {
            Span<byte> computedTag = computedTagBuffer.GetSpan();
            Argon2Core.ComputeHash(
                password,
                salt,
                secret,
                associatedData,
                parameters,
                executionOptions,
                computedTag);
            return CryptographicOperations.FixedTimeEquals(computedTag, expectedTag);
        }
    }
}
