using System;

namespace uuid;

/// <summary>Represents the 20 network-order bytes of a KSUID.</summary>
/// <remarks>The first four bytes store unsigned seconds since <see cref="KsuidLimits.EpochUnixSeconds"/> and the remaining 16 bytes are payload. Byte-returning methods make copies so callers cannot mutate the stored value.</remarks>
public class KsuidValue : IIdentifierValue, IKsuidValue, ITimestampedIdentifierValue
{
    internal byte[] Bytes;

    /// <summary>Creates a KSUID value from 20 network-order bytes.</summary>
    /// <param name="bytes">The timestamp and payload bytes to copy.</param>
    /// <exception cref="ArgumentNullException">The byte array is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The byte array does not contain exactly 20 bytes.</exception>
    public KsuidValue(byte[]? bytes)
    {
        if (bytes != null && bytes.Length != 20)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidLength, "KSUID byte array must contain exactly 20 bytes");
        }

        Bytes = ByteArrayTools.CopyLength(bytes, 20);
    }

    /// <summary>Returns the KSUID identifier kind.</summary>
    /// <returns><see cref="IdentifierKind.Ksuid"/>.</returns>
    public IdentifierKind Kind()
    {
        return IdentifierKind.Ksuid;
    }

    /// <summary>Returns a copy of the 20 network-order bytes.</summary>
    /// <returns>A new byte array that the caller may modify.</returns>
    public byte[] ToBytes()
    {
        return ByteArrayTools.Copy(Bytes);
    }

    /// <summary>Copies the 20 network-order bytes to the start of an existing array.</summary>
    /// <param name="destination">An array with room for at least 20 bytes.</param>
    /// <exception cref="ArgumentNullException">The destination is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The destination is shorter than 20 bytes.</exception>
    public void CopyTo(byte[]? destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (destination.Length < 20)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidLength, "destination must have room for 20 bytes");
        }

        int index = 0;
        while (index < 20)
        {
            destination[index] = Bytes[index];
            index = index + 1;
        }
    }

    /// <summary>Reads the unsigned seconds offset stored in the first four bytes.</summary>
    /// <returns>Seconds since the KSUID epoch.</returns>
    public long AdjustedSeconds()
    {
        return BigEndianTools.ReadUInt32(Bytes, 0);
    }

    /// <summary>Returns the embedded timestamp as seconds since the Unix epoch.</summary>
    /// <returns>The KSUID epoch plus the stored unsigned offset.</returns>
    public long UnixSeconds()
    {
        return KsuidTimestamp.ToUnixSeconds(AdjustedSeconds());
    }

    /// <summary>Returns the embedded timestamp as a UTC date and time.</summary>
    /// <returns>The KSUID creation second.</returns>
    public DateTimeOffset Time()
    {
        IdentifierTimestampResult result = Timestamp();
        if (!result.Ok)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Time;
    }

    /// <summary>Returns the timestamp carried by the KSUID.</summary>
    /// <returns>A successful second-precision timestamp result.</returns>
    public IdentifierTimestampResult Timestamp()
    {
        return Identifier.TimestampFromUnixSeconds(IdentifierKind.Ksuid, UnixSeconds());
    }

    /// <summary>Returns a copy of the 16-byte payload.</summary>
    /// <returns>A new array containing bytes 4 through 19.</returns>
    public byte[] PayloadBytes()
    {
        byte[] payload = new byte[16];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = Bytes[index + 4];
            index = index + 1;
        }

        return payload;
    }

    /// <summary>Formats the value as canonical 27-character Base62 text.</summary>
    /// <returns>The canonical KSUID text.</returns>
    public string ToCanonicalString()
    {
        return Base62.EncodeKsuidBytes(Bytes);
    }

    /// <summary>Tests whether another KSUID contains the same 20 bytes.</summary>
    /// <param name="other">The value to compare, which may be <c>null</c>.</param>
    /// <returns><c>true</c> only when both byte sequences are equal.</returns>
    public bool EqualsValue(KsuidValue? other)
    {
        if (other == null)
        {
            return false;
        }

        return ByteArrayTools.AreEqual(Bytes, other.Bytes);
    }

    /// <summary>Tests whether a byte array exactly matches this KSUID.</summary>
    /// <param name="bytes">The bytes to compare.</param>
    /// <returns><c>true</c> only for an equal 20-byte sequence.</returns>
    public bool EqualsBytes(byte[]? bytes)
    {
        return ByteArrayTools.AreEqual(Bytes, bytes);
    }

    /// <summary>Compares two KSUIDs in unsigned byte order.</summary>
    /// <param name="other">The value to compare; <c>null</c> sorts before this value.</param>
    /// <returns>A negative, zero, or positive value according to KSUID sort order.</returns>
    public int CompareTo(KsuidValue? other)
    {
        if (other == null)
        {
            return 1;
        }

        return ByteArrayTools.Compare(Bytes, other.Bytes);
    }

    /// <summary>Compares two KSUIDs in unsigned byte order, including <c>null</c> values.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>A negative, zero, or positive value; <c>null</c> sorts before non-null.</returns>
    internal static int Compare(KsuidValue? left, KsuidValue? right)
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
