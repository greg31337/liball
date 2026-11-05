using System;

namespace uuid;

/// <summary>Represents the 16 network-order bytes of a ULID.</summary>
/// <remarks>The first six bytes store Unix milliseconds and the remaining ten bytes store randomness. Byte-returning methods make copies.</remarks>
public class UlidValue : IIdentifierValue, IUlidValue, ITimestampedIdentifierValue
{
    internal byte[] Bytes;

    /// <summary>Creates a ULID from exactly 16 network-order bytes.</summary>
    /// <param name="bytes">The bytes to copy.</param>
    /// <exception cref="ArgumentNullException">The byte array is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The byte array does not contain exactly 16 bytes.</exception>
    public UlidValue(byte[]? bytes)
    {
        if (bytes != null && bytes.Length != 16)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidLength, "ULID byte array must contain exactly 16 bytes");
        }

        Bytes = ByteArrayTools.CopyLength(bytes, 16);
    }

    /// <summary>Returns the ULID identifier kind.</summary>
    /// <returns><see cref="IdentifierKind.Ulid"/>.</returns>
    public IdentifierKind Kind()
    {
        return IdentifierKind.Ulid;
    }

    /// <summary>Returns a copy of the 16 ULID bytes.</summary>
    /// <returns>A new array that the caller may modify.</returns>
    public byte[] ToBytes()
    {
        return ByteArrayTools.Copy(Bytes);
    }

    /// <summary>Returns the raw 128 bits for UUID-compatible storage.</summary>
    /// <returns>A new 16-byte array; no UUID version or variant bits are changed.</returns>
    public byte[] ToUuidBytes()
    {
        return ToBytes();
    }

    /// <summary>Interprets 16 UUID-compatible bytes as a ULID without changing their bits.</summary>
    /// <param name="bytes">Exactly 16 bytes to copy.</param>
    /// <returns>A ULID with the same raw bits.</returns>
    /// <exception cref="ArgumentNullException">The array is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The array does not contain exactly 16 bytes.</exception>
    internal static UlidValue FromUuidBytes(byte[]? bytes)
    {
        return new UlidValue(bytes);
    }

    /// <summary>Copies the 16 ULID bytes to the start of an existing array.</summary>
    /// <param name="destination">An array with room for at least 16 bytes.</param>
    /// <exception cref="ArgumentNullException">The destination is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The destination is shorter than 16 bytes.</exception>
    public void CopyTo(byte[]? destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (destination.Length < 16)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidLength, "destination must have room for 16 bytes");
        }

        int index = 0;
        while (index < 16)
        {
            destination[index] = Bytes[index];
            index = index + 1;
        }
    }

    /// <summary>Returns the embedded 48-bit Unix-millisecond timestamp.</summary>
    /// <returns>Milliseconds since the Unix epoch.</returns>
    public long UnixMilliseconds()
    {
        return BigEndianTools.ReadUInt48(Bytes, 0);
    }

    /// <summary>Returns the embedded timestamp as a UTC date and time.</summary>
    /// <returns>The ULID creation millisecond.</returns>
    /// <exception cref="IdentifierException">The 48-bit timestamp is later than <see cref="DateTimeOffset"/> can represent.</exception>
    public DateTimeOffset Time()
    {
        IdentifierTimestampResult result = Timestamp();
        if (!result.Ok)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Time;
    }

    /// <summary>Returns the timestamp carried by the ULID.</summary>
    /// <returns>A successful millisecond-precision timestamp result.</returns>
    public IdentifierTimestampResult Timestamp()
    {
        return Identifier.TimestampFromUnixMilliseconds(IdentifierKind.Ulid, UnixMilliseconds());
    }

    /// <summary>Returns a copy of the 10-byte random field.</summary>
    /// <returns>A new array containing bytes 6 through 15.</returns>
    public byte[] RandomnessBytes()
    {
        byte[] randomness = new byte[10];
        int index = 0;
        while (index < randomness.Length)
        {
            randomness[index] = Bytes[index + 6];
            index = index + 1;
        }

        return randomness;
    }

    /// <summary>Formats the value as canonical 26-character uppercase Crockford Base32 text.</summary>
    /// <returns>The canonical ULID text.</returns>
    public string ToCanonicalString()
    {
        return CrockfordBase32.EncodeUlidBytes(Bytes);
    }

    /// <summary>Tests whether another ULID contains the same 16 bytes.</summary>
    /// <param name="other">The value to compare, which may be <c>null</c>.</param>
    /// <returns><c>true</c> only when both byte sequences are equal.</returns>
    public bool EqualsValue(UlidValue? other)
    {
        if (other == null)
        {
            return false;
        }

        return ByteArrayTools.AreEqual(Bytes, other.Bytes);
    }

    /// <summary>Tests whether a byte array exactly matches this ULID.</summary>
    /// <param name="bytes">The bytes to compare.</param>
    /// <returns><c>true</c> only for an equal 16-byte sequence.</returns>
    public bool EqualsBytes(byte[]? bytes)
    {
        return ByteArrayTools.AreEqual(Bytes, bytes);
    }

    /// <summary>Compares two ULIDs in unsigned byte order.</summary>
    /// <param name="other">The value to compare; <c>null</c> sorts before this value.</param>
    /// <returns>A negative, zero, or positive value according to ULID sort order.</returns>
    public int CompareTo(UlidValue? other)
    {
        if (other == null)
        {
            return 1;
        }

        return ByteArrayTools.Compare(Bytes, other.Bytes);
    }

    /// <summary>Compares two ULIDs in unsigned byte order, including <c>null</c> values.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>A negative, zero, or positive value; <c>null</c> sorts before non-null.</returns>
    internal static int Compare(UlidValue? left, UlidValue? right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return -1;
        }

        return left.CompareTo(right);
    }
}
