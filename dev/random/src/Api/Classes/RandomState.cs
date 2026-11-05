using System;

namespace random;

/// <summary>
/// Contains a validated, versioned snapshot of a deterministic random generator.
/// </summary>
/// <remarks>
/// State provides integrity checking, not secrecy or authentication. Anyone who
/// obtains a state snapshot can reproduce output from the represented generator.
/// Returned byte arrays are independent copies owned by the caller. A snapshot records
/// the named engine, algorithm version, transform version, and exact engine state, but not
/// <see cref="RandomMathMode"/>; callers must persist and restore that mode separately when
/// replaying distribution calls.
/// </remarks>
public class RandomState : IRandomState
{
    RandomAlgorithm algorithm;
    int algorithmVersion;
    int transformVersion;
    byte[] payload;

    internal RandomState(RandomAlgorithm algorithm, int algorithmVersion, int transformVersion, byte[] payload)
    {
        this.algorithm = algorithm;
        this.algorithmVersion = algorithmVersion;
        this.transformVersion = transformVersion;
        this.payload = (byte[])payload.Clone();
    }

    /// <summary>
    /// Decodes and validates a canonical binary state envelope.
    /// </summary>
    /// <param name="encoded">The complete state envelope.</param>
    /// <returns>A validated deterministic state snapshot.</returns>
    /// <exception cref="RandomArgumentException">The input is null.</exception>
    /// <exception cref="RandomStateException">The envelope is malformed, corrupt, incompatible, or non-deterministic.</exception>
    internal static RandomState FromBytes(byte[] encoded)
    {
        return StateCodec.Decode(encoded);
    }

    /// <summary>
    /// Decodes and validates a canonical Base64 state envelope.
    /// </summary>
    /// <param name="encoded">The Base64 representation of a complete state envelope.</param>
    /// <returns>A validated deterministic state snapshot.</returns>
    /// <exception cref="RandomArgumentException">The input is null.</exception>
    /// <exception cref="RandomStateException">The text or decoded envelope is malformed, corrupt, incompatible, or non-deterministic.</exception>
    internal static RandomState FromBase64(string encoded)
    {
        return StateCodec.DecodeBase64(encoded);
    }

    /// <summary>
    /// Returns the deterministic algorithm represented by the snapshot.
    /// </summary>
    /// <returns>The stable algorithm identifier.</returns>
    public RandomAlgorithm Algorithm()
    {
        return algorithm;
    }

    /// <summary>
    /// Returns the semantic version of the represented algorithm.
    /// </summary>
    /// <returns>The positive algorithm version.</returns>
    public int AlgorithmVersion()
    {
        return algorithmVersion;
    }

    /// <summary>
    /// Returns the version of the random-to-value transforms represented by the snapshot.
    /// </summary>
    /// <returns>The positive transform version.</returns>
    public int TransformVersion()
    {
        return transformVersion;
    }

    /// <summary>
    /// Encodes the snapshot as a canonical little-endian binary envelope with a SHA-256 digest.
    /// </summary>
    /// <returns>A new byte array containing the complete state envelope.</returns>
    public byte[] ToBytes()
    {
        return StateCodec.Encode(algorithm, algorithmVersion, transformVersion, payload);
    }

    /// <summary>
    /// Encodes the canonical binary envelope as Base64.
    /// </summary>
    /// <returns>The stable Base64 representation of the complete state envelope.</returns>
    public string ToBase64()
    {
        return Convert.ToBase64String(ToBytes());
    }

    internal byte[] Payload()
    {
        return (byte[])payload.Clone();
    }
}
