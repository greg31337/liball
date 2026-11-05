using System;

namespace uuid;

/// <summary>Represents the 16 network-order bytes of a UUID.</summary>
/// <remarks>Construction and byte-returning methods copy data, preventing callers from mutating the stored UUID. The byte order follows UUID text fields and differs from the legacy mixed-endian layout returned by a <see cref="Guid"/> byte-array conversion.</remarks>
public class UuidValue : IIdentifierValue, IUuidValue, ITimestampedIdentifierValue
{
    internal byte[] Bytes;

    /// <summary>Creates a UUID from exactly 16 network-order bytes.</summary>
    /// <param name="bytes">The UUID bytes to copy.</param>
    /// <exception cref="ArgumentNullException">The byte array is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The byte array does not contain exactly 16 bytes.</exception>
    public UuidValue(byte[]? bytes)
    {
        if (bytes != null && bytes.Length != 16)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidLength, "UUID byte array must contain exactly 16 bytes");
        }

        Bytes = ByteArrayTools.CopyLength(bytes, 16);
    }

    /// <summary>Returns the UUID identifier kind.</summary>
    /// <returns><see cref="IdentifierKind.Uuid"/>.</returns>
    public IdentifierKind Kind()
    {
        return IdentifierKind.Uuid;
    }

    /// <summary>Converts a <see cref="Guid"/> to UUID network-order bytes.</summary>
    /// <param name="guid">The GUID to convert.</param>
    /// <returns>A UUID with the same canonical string representation.</returns>
    internal static UuidValue FromGuid(Guid guid)
    {
        return new UuidValue(HexTools.ParseUuidCompact(guid.ToString("N")));
    }

    /// <summary>Returns a copy of the 16 network-order UUID bytes.</summary>
    /// <returns>A new array that the caller may modify.</returns>
    public byte[] ToBytes()
    {
        return ByteArrayTools.Copy(Bytes);
    }

    /// <summary>Copies the 16 UUID bytes to the start of an existing array.</summary>
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

    /// <summary>Formats the UUID as lowercase canonical text with hyphens.</summary>
    /// <returns>A 36-character UUID string.</returns>
    public string ToCanonicalString()
    {
        return FormatCanonical(false);
    }

    /// <summary>Formats the UUID as uppercase canonical text with hyphens.</summary>
    /// <returns>A 36-character UUID string.</returns>
    public string ToUpperString()
    {
        return FormatCanonical(true);
    }

    /// <summary>Formats the UUID as 32 lowercase hexadecimal characters without hyphens.</summary>
    /// <returns>The compact UUID text.</returns>
    public string ToCompactString()
    {
        return HexTools.FormatUuidCompactLower(Bytes);
    }

    /// <summary>Formats the UUID as 32 uppercase hexadecimal characters without hyphens.</summary>
    /// <returns>The compact UUID text.</returns>
    public string ToCompactUpperString()
    {
        return HexTools.FormatUuidCompactUpper(Bytes);
    }

    /// <summary>Converts the UUID to a <see cref="Guid"/> with the same canonical text.</summary>
    /// <returns>The equivalent .NET GUID.</returns>
    public Guid ToGuid()
    {
        return new Guid(ToCanonicalString());
    }

    /// <summary>Reads the raw four-bit version number.</summary>
    /// <returns>An integer from zero through 15, even when the version is not standardized.</returns>
    public int VersionNumber()
    {
        return (Bytes[6] >> 4) & 15;
    }

    /// <summary>Maps the encoded version nibble to a supported UUID version.</summary>
    /// <returns>The recognized version, or <see cref="UuidVersion.Unknown"/>.</returns>
    public UuidVersion Version()
    {
        int version = VersionNumber();
        if (version == 1)
        {
            return UuidVersion.Version1;
        }

        if (version == 3)
        {
            return UuidVersion.Version3;
        }

        if (version == 4)
        {
            return UuidVersion.Version4;
        }

        if (version == 5)
        {
            return UuidVersion.Version5;
        }

        if (version == 6)
        {
            return UuidVersion.Version6;
        }

        if (version == 7)
        {
            return UuidVersion.Version7;
        }

        if (version == 8)
        {
            return UuidVersion.Version8;
        }

        return UuidVersion.Unknown;
    }

    /// <summary>Classifies the UUID variant from the leading bits of byte 8.</summary>
    /// <returns>The NCS, RFC 9562, Microsoft, or future variant category.</returns>
    public UuidVariant Variant()
    {
        int value = Bytes[8];
        if ((value & 128) == 0)
        {
            return UuidVariant.Ncs;
        }

        if ((value & 192) == 128)
        {
            return UuidVariant.Rfc9562;
        }

        if ((value & 224) == 192)
        {
            return UuidVariant.Microsoft;
        }

        return UuidVariant.Future;
    }

    /// <summary>Checks whether the UUID uses RFC 9562 variant bits.</summary>
    /// <returns><c>true</c> when byte 8 begins with binary <c>10</c>.</returns>
    public bool HasRfc9562Variant()
    {
        return Variant() == UuidVariant.Rfc9562;
    }

    /// <summary>Checks whether all 128 bits are zero.</summary>
    /// <returns><c>true</c> for the Nil UUID.</returns>
    public bool IsNil()
    {
        int index = 0;
        while (index < Bytes.Length)
        {
            if (Bytes[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>Checks whether all 128 bits are one.</summary>
    /// <returns><c>true</c> for the Max UUID.</returns>
    public bool IsMax()
    {
        int index = 0;
        while (index < Bytes.Length)
        {
            if (Bytes[index] != 255)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>Extracts the timestamp carried by a version 1, 6, or 7 UUID.</summary>
    /// <returns>A timestamp result, or a failure reason for non-RFC and non-time-based values.</returns>
    public IdentifierTimestampResult Timestamp()
    {
        return UuidTimestamp.Extract(this);
    }

    /// <summary>Extracts the timestamp as Unix milliseconds from a version 1, 6, or 7 UUID.</summary>
    /// <returns>The embedded timestamp, with 100-nanosecond version 1 and 6 precision truncated to milliseconds.</returns>
    /// <exception cref="IdentifierException">The UUID has non-RFC variant bits or does not carry a supported timestamp.</exception>
    public long UnixMilliseconds()
    {
        return UuidTimestamp.ExtractUnixMilliseconds(this);
    }

    /// <summary>Extracts the UUID timestamp as a UTC date and time.</summary>
    /// <returns>The embedded version 1, 6, or 7 timestamp.</returns>
    /// <exception cref="IdentifierException">The UUID does not carry a supported RFC timestamp or its timestamp is outside the range supported by <see cref="DateTimeOffset"/>.</exception>
    public DateTimeOffset Time()
    {
        IdentifierTimestampResult result = Timestamp();
        if (!result.Ok)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Time;
    }

    /// <summary>Tests whether another UUID contains the same 16 bytes.</summary>
    /// <param name="other">The value to compare, which may be <c>null</c>.</param>
    /// <returns><c>true</c> only when both byte sequences are equal.</returns>
    public bool EqualsValue(UuidValue? other)
    {
        if (other == null)
        {
            return false;
        }

        return ByteArrayTools.AreEqual(Bytes, other.Bytes);
    }

    /// <summary>Tests whether a byte array exactly matches this UUID.</summary>
    /// <param name="bytes">The network-order bytes to compare.</param>
    /// <returns><c>true</c> only for an equal 16-byte sequence.</returns>
    public bool EqualsBytes(byte[]? bytes)
    {
        return ByteArrayTools.AreEqual(Bytes, bytes);
    }

    /// <summary>Compares two UUIDs in unsigned network-byte order.</summary>
    /// <param name="other">The value to compare; <c>null</c> sorts before this value.</param>
    /// <returns>A negative, zero, or positive value according to byte order.</returns>
    public int CompareTo(UuidValue? other)
    {
        if (other == null)
        {
            return 1;
        }

        return ByteArrayTools.Compare(Bytes, other.Bytes);
    }

    /// <summary>Compares two UUIDs in unsigned network-byte order, including <c>null</c> values.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>A negative, zero, or positive value; <c>null</c> sorts before non-null.</returns>
    internal static int Compare(UuidValue? left, UuidValue? right)
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

    internal string FormatCanonical(bool upper)
    {
        string compact = HexTools.Format(Bytes, upper);
        char[] characters = new char[36];
        int compactIndex = 0;
        int textIndex = 0;
        while (textIndex < characters.Length)
        {
            if (textIndex == 8 || textIndex == 13 || textIndex == 18 || textIndex == 23)
            {
                characters[textIndex] = '-';
            }
            else
            {
                characters[textIndex] = compact[compactIndex];
                compactIndex = compactIndex + 1;
            }

            textIndex = textIndex + 1;
        }

        return new string(characters);
    }
}
