using System;

namespace argon2;

/// <summary>
/// Provides allocating one-shot Argon2 key derivation.
/// </summary>
public class Argon2KeyDeriver : IArgon2KeyDeriver
{
    /// <summary>
    /// Initializes a stateless Argon2 key deriver.
    /// </summary>
    public Argon2KeyDeriver()
    {
    }

    /// <summary>
    /// Derives a newly allocated key with the requested output length.
    /// </summary>
    /// <param name="password">The password or message bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="secret">The optional secret bytes, or an empty span.</param>
    /// <param name="associatedData">The optional associated-data bytes, or an empty span.</param>
    /// <param name="parameters">The Argon2 algorithm parameters.</param>
    /// <param name="executionOptions">The execution controls.</param>
    /// <param name="outputLength">The required key length in bytes.</param>
    /// <returns>A new array containing exactly one complete Argon2 output.</returns>
    /// <exception cref="Argon2ParameterException">A parameter, input, or output length is invalid.</exception>
    public byte[] DeriveKey(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        int outputLength)
    {
        Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters);
        Argon2ParameterValidator.ValidateInputLengths(
            password,
            salt,
            secret,
            associatedData,
            outputLength);
        int managedOutputLength = Argon2ParameterValidator.GetManagedArrayLength(outputLength);
        byte[] output = new byte[managedOutputLength];
        Argon2Core.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            output);
        return output;
    }
}
