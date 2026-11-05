using System;

namespace uuid;

internal class UuidConverter
{
    internal static UuidValue V1ToV6(UuidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Version() != UuidVersion.Version1)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidUuidVersion, "value must be a UUIDv1 value");
        }

        if (!value.HasRfc9562Variant())
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidUuidVariant, "value must use RFC 9562 variant bits");
        }

        byte[] source = value.ToBytes();
        long timestamp = V1Timestamp(source);
        byte[] converted = new byte[16];
        BigEndianTools.WriteUInt48(converted, 0, timestamp >> 12);
        converted[6] = (byte)(96 | ((timestamp >> 8) & 15));
        converted[7] = (byte)(timestamp & 255);
        CopyClockSequenceAndNode(source, converted);
        return new UuidValue(converted);
    }

    internal static UuidValue V6ToV1(UuidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Version() != UuidVersion.Version6)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidUuidVersion, "value must be a UUIDv6 value");
        }

        if (!value.HasRfc9562Variant())
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidUuidVariant, "value must use RFC 9562 variant bits");
        }

        byte[] source = value.ToBytes();
        long timestamp = V6Timestamp(source);
        byte[] converted = new byte[16];
        BigEndianTools.WriteUInt32(converted, 0, (uint)(timestamp & 4294967295));
        BigEndianTools.WriteUInt16(converted, 4, (int)((timestamp >> 32) & 65535));
        converted[6] = (byte)(16 | ((timestamp >> 56) & 15));
        converted[7] = (byte)((timestamp >> 48) & 255);
        CopyClockSequenceAndNode(source, converted);
        return new UuidValue(converted);
    }

    internal static long V1Timestamp(byte[] bytes)
    {
        ByteArrayTools.CopyLength(bytes, 16);
        long timeHigh = ((long)(bytes[6] & 15) << 8) | bytes[7];
        long timeMid = BigEndianTools.ReadUInt16(bytes, 4);
        long timeLow = BigEndianTools.ReadUInt32(bytes, 0);
        return (timeHigh << 48) | (timeMid << 32) | timeLow;
    }

    internal static long V6Timestamp(byte[] bytes)
    {
        ByteArrayTools.CopyLength(bytes, 16);
        long timeHigh = BigEndianTools.ReadUInt48(bytes, 0);
        long timeLow = ((long)(bytes[6] & 15) << 8) | bytes[7];
        return (timeHigh << 12) | timeLow;
    }

    internal static void CopyClockSequenceAndNode(byte[] source, byte[] destination)
    {
        ByteArrayTools.CopyLength(source, 16);
        ByteArrayTools.CopyLength(destination, 16);
        int index = 8;
        while (index < 16)
        {
            destination[index] = source[index];
            index = index + 1;
        }
    }
}
