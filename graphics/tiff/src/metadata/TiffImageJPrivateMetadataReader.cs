using System;

namespace tiff;

internal class TiffImageJPrivateMetadataReader
{
    internal const uint Magic = 0x494a494a;
    internal const uint InfoType = 0x696e666f;
    internal const uint LabelsType = 0x6c61626c;
    internal const uint RangesType = 0x72616e67;
    internal const uint LutsType = 0x6c757473;
    internal const uint PlotType = 0x706c6f74;
    internal const uint RoiType = 0x726f6920;
    internal const uint OverlayType = 0x6f766572;
    internal const uint PropertiesType = 0x70726f70;

    internal static void Apply(TiffImageJMetadata metadata, TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (metadata == null || directory == null)
        {
            return;
        }

        TiffIfdEntry? countsEntry = directory.FindEntry(TiffTagId.ImageJMetadataByteCounts);
        TiffIfdEntry? metadataEntry = directory.FindEntry(TiffTagId.ImageJMetadata);
        if (countsEntry == null && metadataEntry == null)
        {
            return;
        }

        if (countsEntry == null || metadataEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ private metadata requires both metadata and byte-count tags.", directory.ImageIndex, TiffTagId.ImageJMetadata);
            return;
        }

        if (countsEntry.DataType != TiffDataType.Long || metadataEntry.DataType != TiffDataType.Byte)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "ImageJ private metadata tags have invalid TIFF data types.", directory.ImageIndex, TiffTagId.ImageJMetadata);
            return;
        }

        TiffTagValueLoadResult metadataLoad = TiffTagValueLoader.LoadBytes(metadataEntry, fileBytes, limits);
        diagnostics.Merge(metadataLoad.Diagnostics);
        TiffByteOrder byteOrder = DetectByteOrder(metadataLoad.Bytes);
        if (byteOrder == TiffByteOrder.Unknown)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ private metadata does not begin with the IJIJ signature.", directory.ImageIndex, TiffTagId.ImageJMetadata);
            return;
        }

        TiffTagValueLoadResult countsLoad = TiffTagValueLoader.LoadBytes(countsEntry, fileBytes, limits);
        diagnostics.Merge(countsLoad.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        int[] byteCounts = DecodeByteCounts(countsLoad.Bytes, byteOrder, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors() || byteCounts.Length == 0)
        {
            return;
        }

        ParseBlocks(metadata, metadataLoad.Bytes, byteCounts, byteOrder, directory.ImageIndex, diagnostics);
    }

    internal static TiffByteOrder DetectByteOrder(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
        {
            return TiffByteOrder.Unknown;
        }

        if (bytes[0] == 0x49 && bytes[1] == 0x4a && bytes[2] == 0x49 && bytes[3] == 0x4a)
        {
            return TiffByteOrder.BigEndian;
        }

        if (bytes[0] == 0x4a && bytes[1] == 0x49 && bytes[2] == 0x4a && bytes[3] == 0x49)
        {
            return TiffByteOrder.LittleEndian;
        }

        return TiffByteOrder.Unknown;
    }

    internal static int[] DecodeByteCounts(byte[] bytes, TiffByteOrder byteOrder, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (bytes == null || bytes.Length == 0 || bytes.Length % 4 != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ metadata byte counts must contain 32-bit values.", imageIndex, TiffTagId.ImageJMetadataByteCounts);
            return new int[0];
        }

        int[] counts = new int[bytes.Length / 4];
        int index = 0;
        while (index < counts.Length)
        {
            uint value = TiffBinaryReader.ReadUInt32(bytes.AsSpan(index * 4, 4), byteOrder);
            if (value > int.MaxValue)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ metadata byte count exceeds the supported range.", imageIndex, TiffTagId.ImageJMetadataByteCounts);
                return new int[0];
            }

            counts[index] = (int)value;
            index = index + 1;
        }

        return counts;
    }

    internal static void ParseBlocks(TiffImageJMetadata metadata, byte[] bytes, int[] byteCounts, TiffByteOrder byteOrder, int imageIndex, TiffDiagnostics diagnostics)
    {
        int headerSize = byteCounts[0];
        if (headerSize < 12 || headerSize > bytes.Length || (headerSize - 4) % 8 != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ private metadata header size is invalid.", imageIndex, TiffTagId.ImageJMetadata);
            return;
        }

        uint signature = TiffBinaryReader.ReadUInt32(bytes.AsSpan(0, 4), byteOrder);
        if (signature != Magic)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ private metadata signature is invalid.", imageIndex, TiffTagId.ImageJMetadata);
            return;
        }

        int typeCount = (headerSize - 4) / 8;
        uint[] types = new uint[typeCount];
        int[] counts = new int[typeCount];
        int totalItems = 0;
        int typeIndex = 0;
        while (typeIndex < typeCount)
        {
            int headerOffset = 4 + typeIndex * 8;
            types[typeIndex] = TiffBinaryReader.ReadUInt32(bytes.AsSpan(headerOffset, 4), byteOrder);
            uint rawCount = TiffBinaryReader.ReadUInt32(bytes.AsSpan(headerOffset + 4, 4), byteOrder);
            if (rawCount > int.MaxValue || totalItems > int.MaxValue - (int)rawCount)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ private metadata item count exceeds the supported range.", imageIndex, TiffTagId.ImageJMetadata);
                return;
            }

            counts[typeIndex] = (int)rawCount;
            totalItems = totalItems + counts[typeIndex];
            typeIndex = typeIndex + 1;
        }

        if (totalItems != byteCounts.Length - 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ private metadata type counts do not match the byte-count table.", imageIndex, TiffTagId.ImageJMetadataByteCounts);
            return;
        }

        int byteCountIndex = 1;
        int dataOffset = headerSize;
        typeIndex = 0;
        while (typeIndex < types.Length)
        {
            int itemIndex = 0;
            while (itemIndex < counts[typeIndex])
            {
                int length = byteCounts[byteCountIndex];
                if (length < 0 || dataOffset > bytes.Length - length)
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.InvalidByteRange, "ImageJ private metadata block exceeds the payload.", imageIndex, TiffTagId.ImageJMetadata);
                    return;
                }

                byte[] data = CopyRange(bytes, dataOffset, length);
                ApplyBlock(metadata, types[typeIndex], itemIndex, data, byteOrder, imageIndex, diagnostics);
                dataOffset = dataOffset + length;
                byteCountIndex = byteCountIndex + 1;
                itemIndex = itemIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }

        if (dataOffset != bytes.Length)
        {
            diagnostics.AddWarningForTag(TiffErrorCode.InvalidTagValue, "ImageJ private metadata contains trailing bytes; the original payload was preserved.", imageIndex, TiffTagId.ImageJMetadata);
        }

        metadata.HasImageJ = true;
        metadata.HasPrivateMetadata = true;
        metadata.PrivateMetadataByteCounts = TiffImageJMetadata.CopyInts(byteCounts);
        metadata.RawPrivateMetadataBytes = TiffTagValue.CopyByteArray(bytes);
    }

    internal static void ApplyBlock(TiffImageJMetadata metadata, uint type, int itemIndex, byte[] data, TiffByteOrder byteOrder, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (type == InfoType && itemIndex == 0)
        {
            metadata.Info = DecodeUtf16(data, byteOrder, imageIndex, diagnostics);
        }
        else if (type == LabelsType)
        {
            metadata.SliceLabels = AppendString(metadata.SliceLabels, DecodeUtf16(data, byteOrder, imageIndex, diagnostics));
        }
        else if (type == RangesType)
        {
            metadata.DisplayRanges = AppendRanges(metadata.DisplayRanges, data, byteOrder, imageIndex, diagnostics);
        }
        else if (type == LutsType)
        {
            metadata.ChannelLuts = AppendBytes(metadata.ChannelLuts, data);
        }
        else if (type == PlotType && itemIndex == 0)
        {
            metadata.Plot = TiffTagValue.CopyByteArray(data);
        }
        else if (type == RoiType && itemIndex == 0)
        {
            metadata.Roi = TiffTagValue.CopyByteArray(data);
        }
        else if (type == OverlayType)
        {
            metadata.Overlays = AppendBytes(metadata.Overlays, data);
        }
        else if (type == PropertiesType)
        {
            metadata.Properties = AppendString(metadata.Properties, DecodeUtf16(data, byteOrder, imageIndex, diagnostics));
        }
        else
        {
            metadata.UnrecognizedBlocks = AppendPrivateBlock(metadata.UnrecognizedBlocks, TiffImageJPrivateMetadataBlock.Create(type, itemIndex, data));
        }
    }

    internal static string DecodeUtf16(byte[] data, TiffByteOrder byteOrder, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (data.Length % 2 != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ UTF-16 metadata block has an odd byte count.", imageIndex, TiffTagId.ImageJMetadata);
            return "";
        }

        char[] characters = new char[data.Length / 2];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = (char)TiffBinaryReader.ReadUInt16(data.AsSpan(index * 2, 2), byteOrder);
            index = index + 1;
        }

        return new string(characters);
    }

    internal static TiffImageJDisplayRange[] AppendRanges(TiffImageJDisplayRange[] ranges, byte[] data, TiffByteOrder byteOrder, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (data.Length % 8 != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ display-range metadata must contain 64-bit values.", imageIndex, TiffTagId.ImageJMetadata);
            return ranges;
        }

        int valueCount = data.Length / 8;
        if (valueCount % 2 != 0)
        {
            diagnostics.AddWarningForTag(TiffErrorCode.InvalidTagValue, "ImageJ display-range metadata contains an unmatched value.", imageIndex, TiffTagId.ImageJMetadata);
        }

        int index = 0;
        while (index + 1 < valueCount)
        {
            double minimum = TiffBinaryReader.ReadDouble(data.AsSpan(index * 8, 8), byteOrder);
            double maximum = TiffBinaryReader.ReadDouble(data.AsSpan((index + 1) * 8, 8), byteOrder);
            ranges = AppendRange(ranges, TiffImageJDisplayRange.Create(ranges.Length, minimum, maximum));
            index = index + 2;
        }

        return ranges;
    }

    internal static byte[] CopyRange(byte[] bytes, int offset, int length)
    {
        byte[] copy = new byte[length];
        int index = 0;
        while (index < length)
        {
            copy[index] = bytes[offset + index];
            index = index + 1;
        }

        return copy;
    }

    internal static string[] AppendString(string[] values, string value)
    {
        string[] result = new string[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }

        result[index] = value;
        return result;
    }

    internal static byte[][] AppendBytes(byte[][] values, byte[] value)
    {
        byte[][] result = new byte[values.Length + 1][];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = TiffTagValue.CopyByteArray(values[index]);
            index = index + 1;
        }

        result[index] = TiffTagValue.CopyByteArray(value);
        return result;
    }

    internal static TiffImageJDisplayRange[] AppendRange(TiffImageJDisplayRange[] values, TiffImageJDisplayRange value)
    {
        TiffImageJDisplayRange[] result = new TiffImageJDisplayRange[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }

    internal static TiffImageJPrivateMetadataBlock[] AppendPrivateBlock(TiffImageJPrivateMetadataBlock[] values, TiffImageJPrivateMetadataBlock value)
    {
        TiffImageJPrivateMetadataBlock[] result = new TiffImageJPrivateMetadataBlock[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }
}
