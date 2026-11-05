using System;
using System.Security.Cryptography;

namespace random;

/// <summary>
/// Represents immutable canonical seed material and a deterministic unsigned spawn path.
/// </summary>
/// <remarks>
/// Indexed children depend on the root seed and complete path, not on creation
/// order. Canonical seed encoding, expansion, and indexed spawning replay bit-for-bit
/// across supported .NET 10 platforms. Seed sequences are deterministic derivation
/// objects and are not cryptographic generators. A fingerprint can reveal whether
/// roots match and must not be treated as a secret.
/// </remarks>
public class SeedSequence : ISeedSequence
{
    internal const int SequenceVersion = 1;
    internal const int HeaderSize = 20;
    internal const int SystemEntropyByteCount = 32;
    internal const ulong Magic = 0x3130514553444E52UL;
    byte[] rootSeedMaterial;
    ulong[] spawnPath;

    internal SeedSequence(byte[] rootSeedMaterial, ulong[] spawnPath)
    {
        this.rootSeedMaterial = (byte[])rootSeedMaterial.Clone();
        this.spawnPath = (ulong[])spawnPath.Clone();
    }

    /// <summary>
    /// Creates a root sequence from an unsigned 64-bit integer.
    /// </summary>
    /// <param name="seed">The integer seed.</param>
    /// <returns>A root sequence with an empty spawn path.</returns>
    internal static SeedSequence FromUInt64(ulong seed)
    {
        return new SeedSequence(SeedCodec.EncodeUInt64(seed), new ulong[0]);
    }

    /// <summary>
    /// Creates a root sequence from a signed 64-bit integer.
    /// </summary>
    /// <param name="seed">The integer seed.</param>
    /// <returns>A root sequence with an empty spawn path.</returns>
    internal static SeedSequence FromInt64(long seed)
    {
        return new SeedSequence(SeedCodec.EncodeInt64(seed), new ulong[0]);
    }

    /// <summary>
    /// Creates a root sequence from an exact byte seed.
    /// </summary>
    /// <param name="seed">The byte seed, which is copied.</param>
    /// <returns>A root sequence with an empty spawn path.</returns>
    /// <exception cref="RandomArgumentException">The seed is null.</exception>
    internal static SeedSequence FromBytes(byte[] seed)
    {
        return new SeedSequence(SeedCodec.EncodeBytes(seed), new ulong[0]);
    }

    /// <summary>
    /// Creates a root sequence from text encoded as canonical UTF-8.
    /// </summary>
    /// <param name="seed">The text seed.</param>
    /// <returns>A root sequence with an empty spawn path.</returns>
    /// <exception cref="RandomArgumentException">The seed is null or contains invalid UTF-16.</exception>
    internal static SeedSequence FromText(string seed)
    {
        return new SeedSequence(SeedCodec.EncodeText(seed), new ulong[0]);
    }

    /// <summary>
    /// Creates a root sequence from unsigned 32-bit integers in canonical order.
    /// </summary>
    /// <param name="seed">The integer seed array, which is copied.</param>
    /// <returns>A root sequence with an empty spawn path.</returns>
    /// <exception cref="RandomArgumentException">The seed is null or too large.</exception>
    internal static SeedSequence FromUInt32Array(uint[] seed)
    {
        return new SeedSequence(SeedCodec.EncodeUInt32Array(seed), new ulong[0]);
    }

    /// <summary>
    /// Creates a root sequence from 256 bits supplied directly by the operating-system CSPRNG.
    /// </summary>
    /// <returns>A root sequence with an empty spawn path.</returns>
    /// <remarks>
    /// Operating-system failures propagate to the caller. This method never
    /// substitutes a clock, process identifier, deterministic generator, or
    /// another fallback entropy source.
    /// </remarks>
    internal static SeedSequence FromSystemEntropy()
    {
        byte[] entropy = new byte[SystemEntropyByteCount];
        RandomNumberGenerator.Fill(entropy);
        byte[] encoded = SeedCodec.EncodeBytes(entropy);
        SeedSequence sequence = new SeedSequence(encoded, new ulong[0]);
        CryptographicOperations.ZeroMemory(entropy);
        CryptographicOperations.ZeroMemory(encoded);
        return sequence;
    }

    /// <summary>
    /// Creates the child at an explicit unsigned index.
    /// </summary>
    /// <param name="index">The child index appended to the complete current path.</param>
    /// <returns>A deterministic child sequence.</returns>
    /// <exception cref="RandomArgumentException">The resulting path is too long.</exception>
    public SeedSequence Spawn(ulong index)
    {
        if (spawnPath.Length == int.MaxValue)
        {
            throw RandomArgumentException.Create("index", "The spawn path is too long.");
        }

        ulong[] childPath = new ulong[spawnPath.Length + 1];
        Array.Copy(spawnPath, childPath, spawnPath.Length);
        childPath[spawnPath.Length] = index;
        return new SeedSequence(rootSeedMaterial, childPath);
    }

    /// <summary>
    /// Appends an explicit sequence of unsigned indexes to the current path.
    /// </summary>
    /// <param name="path">The path suffix, which is copied.</param>
    /// <returns>A deterministic descendant sequence.</returns>
    /// <exception cref="RandomArgumentException">The path is null or the resulting path is too long.</exception>
    public SeedSequence SpawnPath(ulong[] path)
    {
        Guard.NotNull(path, "path");
        if (path.Length > int.MaxValue - spawnPath.Length)
        {
            throw RandomArgumentException.Create("path", "The spawn path is too long.");
        }

        ulong[] childPath = new ulong[spawnPath.Length + path.Length];
        Array.Copy(spawnPath, childPath, spawnPath.Length);
        Array.Copy(path, 0, childPath, spawnPath.Length, path.Length);
        return new SeedSequence(rootSeedMaterial, childPath);
    }

    /// <summary>
    /// Creates children whose appended indexes are zero through count minus one.
    /// </summary>
    /// <param name="count">The nonnegative number of children.</param>
    /// <returns>Children in ascending index order.</returns>
    /// <exception cref="RandomArgumentException">The count is negative.</exception>
    public SeedSequence[] SpawnMany(int count)
    {
        Guard.Count(count, "count");
        SeedSequence[] children = new SeedSequence[count];
        int index = 0;
        while (index < count)
        {
            children[index] = Spawn((ulong)index);
            index++;
        }

        return children;
    }

    /// <summary>
    /// Deterministically derives bytes from the root seed and complete spawn path.
    /// </summary>
    /// <param name="length">The nonnegative number of bytes.</param>
    /// <returns>A new byte array of the requested length.</returns>
    /// <exception cref="RandomArgumentException">The length is negative or the identity is too large to encode.</exception>
    public byte[] GenerateBytes(int length)
    {
        Guard.Count(length, "length");
        return SeedExpander.ExpandSeedSequence(Material(), "seed-sequence-output", length);
    }

    /// <summary>
    /// Returns a stable SHA-256-derived hexadecimal fingerprint of the root seed material.
    /// </summary>
    /// <returns>A 64-character uppercase hexadecimal root fingerprint.</returns>
    public string Fingerprint()
    {
        byte[] fingerprint = SeedExpander.ExpandSeedSequence(rootSeedMaterial, "root-fingerprint", 32);
        return Convert.ToHexString(fingerprint);
    }

    /// <summary>
    /// Returns a copy of the complete unsigned spawn path.
    /// </summary>
    /// <returns>A new array owned by the caller.</returns>
    public ulong[] Path()
    {
        return (ulong[])spawnPath.Clone();
    }

    internal byte[] RootMaterial()
    {
        return (byte[])rootSeedMaterial.Clone();
    }

    internal byte[] Material()
    {
        if (spawnPath.Length > (int.MaxValue - HeaderSize - rootSeedMaterial.Length) / 8)
        {
            throw RandomArgumentException.Create("path", "The spawn path is too large to encode.");
        }

        int pathByteLength = spawnPath.Length * 8;
        byte[] material = new byte[HeaderSize + rootSeedMaterial.Length + pathByteLength];
        int offset = 0;
        offset = BinaryCodec.WriteUInt64(material, offset, Magic);
        offset = BinaryCodec.WriteUInt32(material, offset, (uint)SequenceVersion);
        offset = BinaryCodec.WriteUInt32(material, offset, (uint)rootSeedMaterial.Length);
        offset = BinaryCodec.WriteUInt32(material, offset, (uint)spawnPath.Length);
        offset = BinaryCodec.WriteBytes(material, offset, rootSeedMaterial);

        int index = 0;
        while (index < spawnPath.Length)
        {
            offset = BinaryCodec.WriteUInt64(material, offset, spawnPath[index]);
            index++;
        }

        return material;
    }
}
