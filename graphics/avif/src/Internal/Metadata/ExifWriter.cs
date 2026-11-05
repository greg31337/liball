using System;

namespace avif;

internal class ExifWriteResult
{
    internal AvifResultCode Code;
    internal byte[] Data;

    internal ExifWriteResult(AvifResultCode code)
    {
        Code = code;
        Data = System.Array.Empty<byte>();
    }
}

internal static class ExifWriter
{
    internal static ExifWriteResult SetOrientation(
        ExifParsedData? parsed,
        int orientation,
        AvifSecurityLimits? limits)
    {
        ExifWriteResult result = new ExifWriteResult(
            AvifResultCode.InvalidArgument);
        if (parsed == null || limits == null
            || orientation < 1 || orientation > 8)
        {
            return result;
        }
        int orientationIndex = ExifParser.FindOrientationTag(parsed);
        if (orientationIndex >= 0)
        {
            byte[] updated = (byte[])parsed.Data.Clone();
            ExifTag tag = parsed.Tags[orientationIndex];
            WriteUInt16(
                updated,
                tag.ValueOffset,
                (ushort)orientation,
                parsed.LittleEndian);
            result.Code = AvifResultCode.Success;
            result.Data = updated;
            return result;
        }

        int oldEntryCount = 0;
        int oldEntriesOffset = 0;
        uint oldNext = 0;
        if (parsed.Ifds.Count != 0)
        {
            ExifIfd root = parsed.Ifds[0];
            oldEntryCount = root.TagCount;
            oldEntriesOffset = root.AbsoluteOffset + 2;
            oldNext = root.NextIfdOffset;
        }
        if (oldEntryCount == ushort.MaxValue)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }
        int newIfdOffset = AlignFour(parsed.Data.Length);
        long newLengthValue = (long)newIfdOffset
            + 2 + (long)(oldEntryCount + 1) * 12 + 4;
        if (newLengthValue > int.MaxValue
            || newLengthValue > limits.MaxExifBytes
            || newLengthValue > limits.MaxMetadataBytes)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }
        int relativeOffset = newIfdOffset - parsed.TiffOffset;
        if (relativeOffset < 8)
        {
            result.Code = AvifResultCode.InvalidMetadata;
            return result;
        }

        byte[] output = new byte[(int)newLengthValue];
        parsed.Data.AsSpan().CopyTo(output);
        WriteUInt16(
            output,
            newIfdOffset,
            (ushort)(oldEntryCount + 1),
            parsed.LittleEndian);
        int sourceEntry = 0;
        int destinationEntry = 0;
        bool written = false;
        while (sourceEntry < oldEntryCount)
        {
            int sourceOffset = oldEntriesOffset + sourceEntry * 12;
            ushort identifier = ExifParser.ReadUInt16(
                parsed.Data, sourceOffset, parsed.LittleEndian);
            if (!written && identifier > 274)
            {
                WriteOrientationEntry(
                    output,
                    newIfdOffset + 2 + destinationEntry * 12,
                    orientation,
                    parsed.LittleEndian);
                destinationEntry++;
                written = true;
            }
            parsed.Data.AsSpan(sourceOffset, 12).CopyTo(
                output.AsSpan(
                    newIfdOffset + 2 + destinationEntry * 12, 12));
            sourceEntry++;
            destinationEntry++;
        }
        if (!written)
        {
            WriteOrientationEntry(
                output,
                newIfdOffset + 2 + destinationEntry * 12,
                orientation,
                parsed.LittleEndian);
        }
        int nextOffset = newIfdOffset + 2 + (oldEntryCount + 1) * 12;
        WriteUInt32(output, nextOffset, oldNext, parsed.LittleEndian);
        WriteUInt32(
            output,
            parsed.TiffOffset + 4,
            (uint)relativeOffset,
            parsed.LittleEndian);
        result.Code = AvifResultCode.Success;
        result.Data = output;
        return result;
    }

    static void WriteOrientationEntry(
        byte[] data,
        int offset,
        int orientation,
        bool littleEndian)
    {
        WriteUInt16(data, offset, 274, littleEndian);
        WriteUInt16(data, offset + 2, 3, littleEndian);
        WriteUInt32(data, offset + 4, 1, littleEndian);
        WriteUInt16(data, offset + 8, (ushort)orientation, littleEndian);
        data[offset + 10] = 0;
        data[offset + 11] = 0;
    }

    static void WriteUInt16(
        byte[] data,
        int offset,
        ushort value,
        bool littleEndian)
    {
        if (littleEndian)
        {
            data[offset] = IntegerConversion.ToByte(value);
            data[offset + 1] = IntegerConversion.ToByte(value >> 8);
        }
        else
        {
            data[offset] = IntegerConversion.ToByte(value >> 8);
            data[offset + 1] = IntegerConversion.ToByte(value);
        }
    }

    static void WriteUInt32(
        byte[] data,
        int offset,
        uint value,
        bool littleEndian)
    {
        if (littleEndian)
        {
            data[offset] = IntegerConversion.ToByte(value);
            data[offset + 1] = IntegerConversion.ToByte(value >> 8);
            data[offset + 2] = IntegerConversion.ToByte(value >> 16);
            data[offset + 3] = IntegerConversion.ToByte(value >> 24);
        }
        else
        {
            data[offset] = IntegerConversion.ToByte(value >> 24);
            data[offset + 1] = IntegerConversion.ToByte(value >> 16);
            data[offset + 2] = IntegerConversion.ToByte(value >> 8);
            data[offset + 3] = IntegerConversion.ToByte(value);
        }
    }

    static int AlignFour(int value)
    {
        return (value + 3) & -4;
    }
}
