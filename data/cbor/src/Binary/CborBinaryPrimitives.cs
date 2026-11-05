using System;

namespace cbor;

internal static class CborBinaryPrimitives
{
    internal static byte ReadUInt8(ReadOnlySpan<byte> source)
    {
        EnsureReadLength(source.Length, 1);
        return source[0];
    }

    internal static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source)
    {
        EnsureReadLength(source.Length, 2);
        return (ushort)(((uint)source[0] << 8) | (uint)source[1]);
    }

    internal static uint ReadUInt32BigEndian(ReadOnlySpan<byte> source)
    {
        EnsureReadLength(source.Length, 4);
        return ((uint)source[0] << 24)
            | ((uint)source[1] << 16)
            | ((uint)source[2] << 8)
            | (uint)source[3];
    }

    internal static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> source)
    {
        EnsureReadLength(source.Length, 8);
        return ((ulong)source[0] << 56)
            | ((ulong)source[1] << 48)
            | ((ulong)source[2] << 40)
            | ((ulong)source[3] << 32)
            | ((ulong)source[4] << 24)
            | ((ulong)source[5] << 16)
            | ((ulong)source[6] << 8)
            | (ulong)source[7];
    }

    internal static void WriteUInt8(Span<byte> destination, byte value)
    {
        EnsureWriteLength(destination.Length, 1);
        destination[0] = value;
    }

    internal static void WriteUInt16BigEndian(Span<byte> destination, ushort value)
    {
        EnsureWriteLength(destination.Length, 2);
        destination[0] = (byte)(((uint)value >> 8) & 255U);
        destination[1] = (byte)((uint)value & 255U);
    }

    internal static void WriteUInt32BigEndian(Span<byte> destination, uint value)
    {
        EnsureWriteLength(destination.Length, 4);
        destination[0] = (byte)((value >> 24) & 255U);
        destination[1] = (byte)((value >> 16) & 255U);
        destination[2] = (byte)((value >> 8) & 255U);
        destination[3] = (byte)(value & 255U);
    }

    internal static void WriteUInt64BigEndian(Span<byte> destination, ulong value)
    {
        EnsureWriteLength(destination.Length, 8);
        destination[0] = (byte)((value >> 56) & 255UL);
        destination[1] = (byte)((value >> 48) & 255UL);
        destination[2] = (byte)((value >> 40) & 255UL);
        destination[3] = (byte)((value >> 32) & 255UL);
        destination[4] = (byte)((value >> 24) & 255UL);
        destination[5] = (byte)((value >> 16) & 255UL);
        destination[6] = (byte)((value >> 8) & 255UL);
        destination[7] = (byte)(value & 255UL);
    }

    internal static int GetArgumentByteCount(int additionalInformation)
    {
        if (additionalInformation >= 0 && additionalInformation < 24)
        {
            return 0;
        }
        if (additionalInformation == 24)
        {
            return 1;
        }
        if (additionalInformation == 25)
        {
            return 2;
        }
        if (additionalInformation == 26)
        {
            return 4;
        }
        if (additionalInformation == 27)
        {
            return 8;
        }
        if (additionalInformation == 31)
        {
            return -1;
        }
        if (additionalInformation >= 28 && additionalInformation <= 30)
        {
            throw new CborDecodeException(
                "CBOR_RESERVED_ADDITIONAL_INFORMATION",
                "CBOR additional information values 28 through 30 are reserved.",
                0,
                "$"
            );
        }
        throw new CborDecodeException(
            "CBOR_INVALID_ADDITIONAL_INFORMATION",
            "The CBOR additional information value is outside the five-bit range.",
            0,
            "$"
        );
    }

    internal static ulong ReadArgument(ReadOnlySpan<byte> source, int additionalInformation)
    {
        if (additionalInformation >= 0 && additionalInformation < 24)
        {
            return (ulong)additionalInformation;
        }
        if (additionalInformation == 24)
        {
            return ReadUInt8(source);
        }
        if (additionalInformation == 25)
        {
            return ReadUInt16BigEndian(source);
        }
        if (additionalInformation == 26)
        {
            return ReadUInt32BigEndian(source);
        }
        if (additionalInformation == 27)
        {
            return ReadUInt64BigEndian(source);
        }
        GetArgumentByteCount(additionalInformation);
        throw new CborDecodeException(
            "CBOR_INDEFINITE_ARGUMENT",
            "An indefinite-length head does not have an integer argument.",
            0,
            "$"
        );
    }

    internal static CborHead ReadHead(ReadOnlySpan<byte> source)
    {
        if (source.Length < 1)
        {
            throw new CborDecodeException(
                "CBOR_TRUNCATED_HEAD",
                "The source does not contain a CBOR initial byte.",
                0,
                "$"
            );
        }
        byte initialByte = source[0];
        int majorType = initialByte >> 5;
        int additionalInformation = initialByte & 31;
        CborHead head = new CborHead();
        head.MajorType = majorType;
        head.AdditionalInformation = additionalInformation;
        head.HeadLength = 1;

        if (additionalInformation == 31)
        {
            if (majorType >= 2 && majorType <= 5)
            {
                head.IsIndefinite = true;
                return head;
            }
            if (majorType == 7)
            {
                head.IsBreak = true;
                return head;
            }
            throw new CborDecodeException(
                "CBOR_ILLEGAL_INDEFINITE_HEAD",
                "This CBOR major type cannot use an indefinite-length head.",
                0,
                "$"
            );
        }

        int argumentWidth = GetArgumentByteCount(additionalInformation);
        if (source.Length < argumentWidth + 1)
        {
            throw new CborDecodeException(
                "CBOR_TRUNCATED_HEAD",
                "The CBOR head ends before its argument is complete.",
                source.Length,
                "$"
            );
        }
        head.ArgumentWidth = argumentWidth;
        head.HeadLength = argumentWidth + 1;
        head.Argument = ReadArgument(source.Slice(1, argumentWidth), additionalInformation);
        return head;
    }

    internal static int GetHeadSize(ulong argument)
    {
        if (argument < 24)
        {
            return 1;
        }
        if (argument <= byte.MaxValue)
        {
            return 2;
        }
        if (argument <= ushort.MaxValue)
        {
            return 3;
        }
        if (argument <= uint.MaxValue)
        {
            return 5;
        }
        return 9;
    }

    internal static int WriteHead(Span<byte> destination, int majorType, ulong argument)
    {
        if (majorType < 0 || majorType > 7)
        {
            throw new CborEncodeException(
                "CBOR_INVALID_MAJOR_TYPE",
                "The CBOR major type must be between zero and seven.",
                -1,
                "$"
            );
        }

        int headSize = GetHeadSize(argument);
        EnsureWriteLength(destination.Length, headSize);
        int majorBits = majorType << 5;
        if (argument < 24)
        {
            destination[0] = (byte)(majorBits | (int)argument);
            return 1;
        }
        if (argument <= byte.MaxValue)
        {
            destination[0] = (byte)(majorBits | 24);
            WriteUInt8(destination.Slice(1, 1), (byte)argument);
            return 2;
        }
        if (argument <= ushort.MaxValue)
        {
            destination[0] = (byte)(majorBits | 25);
            WriteUInt16BigEndian(destination.Slice(1, 2), (ushort)argument);
            return 3;
        }
        if (argument <= uint.MaxValue)
        {
            destination[0] = (byte)(majorBits | 26);
            WriteUInt32BigEndian(destination.Slice(1, 4), (uint)argument);
            return 5;
        }
        destination[0] = (byte)(majorBits | 27);
        WriteUInt64BigEndian(destination.Slice(1, 8), argument);
        return 9;
    }

    internal static int WriteHeadWithWidth(
        Span<byte> destination,
        int majorType,
        ulong argument,
        int argumentWidth)
    {
        if (majorType < 0 || majorType > 7)
        {
            throw new CborEncodeException(
                "CBOR_INVALID_MAJOR_TYPE",
                "The CBOR major type must be between zero and seven.",
                -1,
                "$"
            );
        }
        int required = argumentWidth + 1;
        EnsureWriteLength(destination.Length, required);
        int majorBits = majorType << 5;
        if (argumentWidth == 0)
        {
            if (argument >= 24)
            {
                throw new CborEncodeException(
                    "CBOR_REPRESENTATION_HEAD_WIDTH_INVALID",
                    "An inline CBOR head can represent only arguments below 24.",
                    -1,
                    "$"
                );
            }
            destination[0] = (byte)(majorBits | (int)argument);
            return 1;
        }
        if (argumentWidth == 1 && argument <= byte.MaxValue)
        {
            destination[0] = (byte)(majorBits | 24);
            WriteUInt8(destination.Slice(1, 1), (byte)argument);
            return 2;
        }
        if (argumentWidth == 2 && argument <= ushort.MaxValue)
        {
            destination[0] = (byte)(majorBits | 25);
            WriteUInt16BigEndian(destination.Slice(1, 2), (ushort)argument);
            return 3;
        }
        if (argumentWidth == 4 && argument <= uint.MaxValue)
        {
            destination[0] = (byte)(majorBits | 26);
            WriteUInt32BigEndian(destination.Slice(1, 4), (uint)argument);
            return 5;
        }
        if (argumentWidth == 8)
        {
            destination[0] = (byte)(majorBits | 27);
            WriteUInt64BigEndian(destination.Slice(1, 8), argument);
            return 9;
        }
        throw new CborEncodeException(
            "CBOR_REPRESENTATION_HEAD_WIDTH_INVALID",
            "The requested CBOR argument width cannot represent the value.",
            -1,
            "$"
        );
    }

    internal static int WriteIndefiniteHead(Span<byte> destination, int majorType)
    {
        if (majorType < 2 || majorType > 5)
        {
            throw new CborEncodeException(
                "CBOR_ILLEGAL_INDEFINITE_HEAD",
                "Only CBOR strings, arrays, and maps can use an indefinite-length head.",
                -1,
                "$"
            );
        }
        EnsureWriteLength(destination.Length, 1);
        destination[0] = (byte)((majorType << 5) | 31);
        return 1;
    }

    internal static int WriteBreak(Span<byte> destination)
    {
        EnsureWriteLength(destination.Length, 1);
        destination[0] = 255;
        return 1;
    }

    internal static void EnsureReadLength(int actualLength, int requiredLength)
    {
        if (actualLength < requiredLength)
        {
            throw new CborDecodeException(
                "CBOR_TRUNCATED_ARGUMENT",
                "The source does not contain the complete binary argument.",
                actualLength,
                "$"
            );
        }
    }

    internal static void EnsureWriteLength(int actualLength, int requiredLength)
    {
        if (actualLength < requiredLength)
        {
            throw new CborEncodeException(
                "CBOR_DESTINATION_TOO_SMALL",
                "The destination is too small for the binary argument.",
                actualLength,
                "$"
            );
        }
    }
}
