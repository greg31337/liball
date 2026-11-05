using System.Collections.Generic;

namespace avif;

internal static class ExifParser
{
    internal static ExifParseResult Parse(
        byte[] data,
        AvifSecurityLimits? limits)
    {
        ExifParseResult result = new ExifParseResult(
            AvifResultCode.InvalidMetadata);
        if (data == null || limits == null)
        {
            result.Code = AvifResultCode.InvalidArgument;
            return result;
        }
        if (data.LongLength > limits.MaxExifBytes
            || data.LongLength > limits.MaxMetadataBytes)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }
        if (data.Length < 12)
        {
            return result;
        }
        uint headerDisplacement = ReadUInt32BigEndian(data, 0);
        if (headerDisplacement > int.MaxValue - 4)
        {
            return result;
        }
        int tiffOffset = 4 + (int)headerDisplacement;
        if (tiffOffset < 4 || tiffOffset > data.Length - 8)
        {
            return result;
        }
        bool littleEndian;
        if (data[tiffOffset] == 73 && data[tiffOffset + 1] == 73)
        {
            littleEndian = true;
        }
        else if (data[tiffOffset] == 77 && data[tiffOffset + 1] == 77)
        {
            littleEndian = false;
        }
        else
        {
            return result;
        }
        if (ReadUInt16(data, tiffOffset + 2, littleEndian) != 42)
        {
            return result;
        }
        uint firstIfd = ReadUInt32(data, tiffOffset + 4, littleEndian);
        if (firstIfd != 0 && firstIfd < 8)
        {
            return result;
        }

        ExifParsedData parsed = new ExifParsedData();
        parsed.Data = (byte[])data.Clone();
        parsed.TiffOffset = tiffOffset;
        parsed.LittleEndian = littleEndian;
        parsed.FirstIfdOffset = firstIfd;
        List<uint> visited = new List<uint>();
        List<uint> active = new List<uint>();
        AvifResultCode parseCode = AvifResultCode.Success;
        if (firstIfd != 0)
        {
            parseCode = ParseIfd(
                parsed, firstIfd, 0, 0, visited, active, limits);
        }
        if (parseCode != AvifResultCode.Success)
        {
            result.Code = parseCode;
            return result;
        }
        result.Code = AvifResultCode.Success;
        result.Parsed = parsed;
        return result;
    }

    static AvifResultCode ParseIfd(
        ExifParsedData parsed,
        uint relativeOffset,
        int depth,
        int recursionDepth,
        List<uint> visited,
        List<uint> active,
        AvifSecurityLimits limits)
    {
        if (recursionDepth >= limits.MaxRecursionDepth)
        {
            return AvifResultCode.LimitExceeded;
        }
        if (Contains(active, relativeOffset))
        {
            return AvifResultCode.InvalidMetadata;
        }
        if (Contains(visited, relativeOffset))
        {
            return AvifResultCode.Success;
        }
        if (relativeOffset > int.MaxValue)
        {
            return AvifResultCode.InvalidMetadata;
        }
        int relative = (int)relativeOffset;
        if (relative < 8 || relative > parsed.Data.Length
                - parsed.TiffOffset - 2)
        {
            return AvifResultCode.InvalidMetadata;
        }
        int absolute = parsed.TiffOffset + relative;
        ushort entryCount = ReadUInt16(
            parsed.Data, absolute, parsed.LittleEndian);
        long ifdLength = 2L + (long)entryCount * 12 + 4;
        if (ifdLength > parsed.Data.Length - absolute)
        {
            return AvifResultCode.InvalidMetadata;
        }
        long objectEstimate = ((long)parsed.Tags.Count + entryCount) * 64
            + ((long)parsed.Ifds.Count + 1) * 48;
        if (objectEstimate > limits.MaxTemporaryBytes)
        {
            return AvifResultCode.LimitExceeded;
        }

        active.Add(relativeOffset);
        ExifIfd ifd = new ExifIfd(
            relativeOffset,
            absolute,
            depth,
            parsed.Tags.Count);
        int ifdIndex = parsed.Ifds.Count;
        parsed.Ifds.Add(ifd);
        int entryIndex = 0;
        while (entryIndex < entryCount)
        {
            int entryOffset = absolute + 2 + entryIndex * 12;
            ushort identifier = ReadUInt16(
                parsed.Data, entryOffset, parsed.LittleEndian);
            ushort type = ReadUInt16(
                parsed.Data, entryOffset + 2, parsed.LittleEndian);
            uint count = ReadUInt32(
                parsed.Data, entryOffset + 4, parsed.LittleEndian);
            int elementSize = GetTypeSize(type);
            if (elementSize == 0 || count == 0
                || HasTag(parsed, ifdIndex, identifier))
            {
                return AvifResultCode.InvalidMetadata;
            }
            long valueLengthValue = (long)elementSize * count;
            if (valueLengthValue > int.MaxValue)
            {
                return AvifResultCode.InvalidMetadata;
            }
            int valueLength = (int)valueLengthValue;
            bool inline = valueLength <= 4;
            int valueOffset;
            if (inline)
            {
                valueOffset = entryOffset + 8;
            }
            else
            {
                uint valueRelative = ReadUInt32(
                    parsed.Data, entryOffset + 8, parsed.LittleEndian);
                if (valueRelative > int.MaxValue)
                {
                    return AvifResultCode.InvalidMetadata;
                }
                int valueRelativeInt = (int)valueRelative;
                if (valueRelativeInt < 8
                    || valueRelativeInt > parsed.Data.Length
                        - parsed.TiffOffset
                    || valueLength > parsed.Data.Length
                        - parsed.TiffOffset - valueRelativeInt)
                {
                    return AvifResultCode.InvalidMetadata;
                }
                valueOffset = parsed.TiffOffset + valueRelativeInt;
            }
            ExifTag tag = new ExifTag(
                identifier,
                type,
                count,
                ifdIndex,
                entryOffset,
                valueOffset,
                valueLength,
                inline);
            AvifResultCode validation = ValidateTag(parsed, tag);
            if (validation != AvifResultCode.Success)
            {
                return validation;
            }
            parsed.Tags.Add(tag);
            entryIndex++;
        }
        ifd.TagCount = entryCount;
        int nextOffsetPosition = absolute + 2 + entryCount * 12;
        ifd.NextIfdOffset = ReadUInt32(
            parsed.Data, nextOffsetPosition, parsed.LittleEndian);

        int tagIndex = ifd.TagStartIndex;
        while (tagIndex < ifd.TagStartIndex + ifd.TagCount)
        {
            ExifTag tag = parsed.Tags[tagIndex];
            if (IsSingleIfdPointer(tag.Identifier))
            {
                if (tag.Type != 4 && tag.Type != 13)
                {
                    return AvifResultCode.InvalidMetadata;
                }
                if (tag.Count != 1)
                {
                    return AvifResultCode.InvalidMetadata;
                }
                uint child = ReadTagUInt32(parsed, tag, 0);
                if (child != 0)
                {
                    AvifResultCode childCode = ParseIfd(
                        parsed,
                        child,
                        depth + 1,
                        recursionDepth + 1,
                        visited,
                        active,
                        limits);
                    if (childCode != AvifResultCode.Success)
                    {
                        return childCode;
                    }
                }
            }
            else if (tag.Identifier == 330)
            {
                if (tag.Type != 4 && tag.Type != 13)
                {
                    return AvifResultCode.InvalidMetadata;
                }
                uint childIndex = 0;
                while (childIndex < tag.Count)
                {
                    uint child = ReadTagUInt32(
                        parsed, tag, childIndex);
                    if (child != 0)
                    {
                        AvifResultCode childCode = ParseIfd(
                            parsed,
                            child,
                            depth + 1,
                            recursionDepth + 1,
                            visited,
                            active,
                            limits);
                        if (childCode != AvifResultCode.Success)
                        {
                            return childCode;
                        }
                    }
                    childIndex++;
                }
            }
            tagIndex++;
        }
        if (ifd.NextIfdOffset != 0)
        {
            AvifResultCode nextCode = ParseIfd(
                parsed,
                ifd.NextIfdOffset,
                depth,
                recursionDepth + 1,
                visited,
                active,
                limits);
            if (nextCode != AvifResultCode.Success)
            {
                return nextCode;
            }
        }
        active.RemoveAt(active.Count - 1);
        visited.Add(relativeOffset);
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidateTag(
        ExifParsedData parsed,
        ExifTag tag)
    {
        if (tag.Type == 2
            && parsed.Data[tag.ValueOffset + tag.ValueLength - 1] != 0)
        {
            return AvifResultCode.InvalidMetadata;
        }
        if (tag.Type == 5 || tag.Type == 10)
        {
            uint index = 0;
            while (index < tag.Count)
            {
                int denominatorOffset = tag.ValueOffset
                    + (int)index * 8 + 4;
                uint denominator = ReadUInt32(
                    parsed.Data,
                    denominatorOffset,
                    parsed.LittleEndian);
                if (denominator == 0)
                {
                    return AvifResultCode.InvalidMetadata;
                }
                index++;
            }
        }
        if (tag.Identifier == 274)
        {
            if (tag.Type != 3 || tag.Count != 1)
            {
                return AvifResultCode.InvalidMetadata;
            }
            ushort orientation = ReadUInt16(
                parsed.Data, tag.ValueOffset, parsed.LittleEndian);
            if (orientation < 1 || orientation > 8)
            {
                return AvifResultCode.InvalidMetadata;
            }
        }
        return AvifResultCode.Success;
    }

    internal static uint ReadTagUInt32(
        ExifParsedData parsed,
        ExifTag tag,
        uint index)
    {
        if (tag == null || index >= tag.Count
            || (tag.Type != 4 && tag.Type != 13))
        {
            return 0;
        }
        return ReadUInt32(
            parsed.Data,
            tag.ValueOffset + (int)index * 4,
            parsed.LittleEndian);
    }

    internal static int FindOrientationTag(ExifParsedData parsed)
    {
        int index = 0;
        while (index < parsed.Tags.Count)
        {
            ExifTag tag = parsed.Tags[index];
            if (tag.Identifier == 274 && tag.IfdIndex == 0)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    static bool HasTag(
        ExifParsedData parsed,
        int ifdIndex,
        ushort identifier)
    {
        int index = 0;
        while (index < parsed.Tags.Count)
        {
            ExifTag tag = parsed.Tags[index];
            if (tag.IfdIndex == ifdIndex && tag.Identifier == identifier)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static bool Contains(List<uint> values, uint value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static bool IsSingleIfdPointer(ushort identifier)
    {
        return identifier == 34665
            || identifier == 34853
            || identifier == 40965;
    }

    internal static int GetTypeSize(ushort type)
    {
        if (type == 1 || type == 2 || type == 6 || type == 7)
        {
            return 1;
        }
        if (type == 3 || type == 8)
        {
            return 2;
        }
        if (type == 4 || type == 9 || type == 11 || type == 13)
        {
            return 4;
        }
        if (type == 5 || type == 10 || type == 12)
        {
            return 8;
        }
        return 0;
    }

    internal static ushort ReadUInt16(
        byte[] data,
        int offset,
        bool littleEndian)
    {
        return littleEndian
            ? (ushort)(data[offset] | data[offset + 1] << 8)
            : (ushort)(data[offset] << 8 | data[offset + 1]);
    }

    internal static uint ReadUInt32(
        byte[] data,
        int offset,
        bool littleEndian)
    {
        if (littleEndian)
        {
            return data[offset]
                | (uint)data[offset + 1] << 8
                | (uint)data[offset + 2] << 16
                | (uint)data[offset + 3] << 24;
        }
        return ReadUInt32BigEndian(data, offset);
    }

    static uint ReadUInt32BigEndian(byte[] data, int offset)
    {
        return (uint)data[offset] << 24
            | (uint)data[offset + 1] << 16
            | (uint)data[offset + 2] << 8
            | data[offset + 3];
    }
}
