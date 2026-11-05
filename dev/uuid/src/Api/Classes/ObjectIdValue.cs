using System;

namespace uuid;

/// <summary>Represents the 12 network-order bytes of a MongoDB-style ObjectId.</summary>
/// <remarks>The first four bytes are an unsigned Unix-second timestamp, the next five are process-unique data, and the final three are a counter. Returned byte arrays are copies.</remarks>
public class ObjectIdValue : IIdentifierValue, IObjectIdValue, ITimestampedIdentifierValue
{
    internal byte[] Bytes;

    /// <summary>Creates an ObjectId from exactly 12 network-order bytes.</summary>
    /// <param name="bytes">The bytes to copy.</param>
    /// <exception cref="ArgumentNullException">The byte array is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The byte array does not contain exactly 12 bytes.</exception>
    public ObjectIdValue(byte[]? bytes)
    {
        if (bytes != null && bytes.Length != ObjectIdLimits.ByteLength)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidLength, "ObjectId byte array must contain exactly 12 bytes");
        }

        Bytes = ByteArrayTools.CopyLength(bytes, ObjectIdLimits.ByteLength);
    }

    /// <summary>Returns the ObjectId identifier kind.</summary>
    /// <returns><see cref="IdentifierKind.ObjectId"/>.</returns>
    public IdentifierKind Kind()
    {
        return IdentifierKind.ObjectId;
    }

    /// <summary>Returns a copy of the 12 ObjectId bytes.</summary>
    /// <returns>A new byte array that the caller may modify.</returns>
    public byte[] ToBytes()
    {
        return ByteArrayTools.Copy(Bytes);
    }

    /// <summary>Copies the 12 ObjectId bytes to the start of an existing array.</summary>
    /// <param name="destination">An array with room for at least 12 bytes.</param>
    /// <exception cref="ArgumentNullException">The destination is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The destination is shorter than 12 bytes.</exception>
    public void CopyTo(byte[]? destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (destination.Length < ObjectIdLimits.ByteLength)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidLength, "destination must have room for 12 bytes");
        }

        int index = 0;
        while (index < ObjectIdLimits.ByteLength)
        {
            destination[index] = Bytes[index];
            index = index + 1;
        }
    }

    /// <summary>Returns the embedded unsigned timestamp as Unix seconds.</summary>
    /// <returns>A value from zero through <see cref="ObjectIdLimits.MaxUnixSeconds"/>.</returns>
    public long UnixSeconds()
    {
        return BigEndianTools.ReadUInt32(Bytes, 0);
    }

    /// <summary>Returns the embedded timestamp as a UTC date and time.</summary>
    /// <returns>The ObjectId creation second.</returns>
    public DateTimeOffset Time()
    {
        IdentifierTimestampResult result = Timestamp();
        if (!result.Ok)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Time;
    }

    /// <summary>Returns the timestamp carried by the ObjectId.</summary>
    /// <returns>A successful second-precision timestamp result.</returns>
    public IdentifierTimestampResult Timestamp()
    {
        return Identifier.TimestampFromUnixSeconds(IdentifierKind.ObjectId, UnixSeconds());
    }

    /// <summary>Formats the value as canonical lowercase hexadecimal text.</summary>
    /// <returns>A 24-character string.</returns>
    public string ToCanonicalString()
    {
        return ToHexString();
    }

    /// <summary>Formats the value as lowercase hexadecimal text.</summary>
    /// <returns>A 24-character string.</returns>
    public string ToHexString()
    {
        return HexTools.FormatObjectIdLower(Bytes);
    }

    /// <summary>Formats the value as uppercase hexadecimal text.</summary>
    /// <returns>A 24-character string.</returns>
    public string ToUpperHexString()
    {
        return HexTools.FormatObjectIdUpper(Bytes);
    }

    /// <summary>Tests whether another ObjectId contains the same 12 bytes.</summary>
    /// <param name="other">The value to compare, which may be <c>null</c>.</param>
    /// <returns><c>true</c> only when both byte sequences are equal.</returns>
    public bool EqualsValue(ObjectIdValue? other)
    {
        if (other == null)
        {
            return false;
        }

        return ByteArrayTools.AreEqual(Bytes, other.Bytes);
    }

    /// <summary>Tests whether a byte array exactly matches this ObjectId.</summary>
    /// <param name="bytes">The bytes to compare.</param>
    /// <returns><c>true</c> only for an equal 12-byte sequence.</returns>
    public bool EqualsBytes(byte[]? bytes)
    {
        return ByteArrayTools.AreEqual(Bytes, bytes);
    }

    /// <summary>Compares two ObjectIds in unsigned byte order.</summary>
    /// <param name="other">The value to compare; <c>null</c> sorts before this value.</param>
    /// <returns>A negative, zero, or positive value according to ObjectId sort order.</returns>
    public int CompareTo(ObjectIdValue? other)
    {
        if (other == null)
        {
            return 1;
        }

        return ByteArrayTools.Compare(Bytes, other.Bytes);
    }

    /// <summary>Compares two ObjectIds in unsigned byte order, including <c>null</c> values.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>A negative, zero, or positive value; <c>null</c> sorts before non-null.</returns>
    internal static int Compare(ObjectIdValue? left, ObjectIdValue? right)
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
