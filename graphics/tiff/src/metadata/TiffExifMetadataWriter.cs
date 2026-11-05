using System;

namespace tiff;

internal class TiffExifMetadataWriter
{
    internal static TiffExifWriteResult WriteClassic(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] exifValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, parentEntries, exifValues, byteOrder, parentOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffExifWriteResult WriteClassic(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] exifValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, parentEntries, exifValues, byteOrder, parentOffset, nextIfdOffset, diagnostics))
        {
            return TiffExifWriteResult.Create(parentOffset, 0, 0, diagnostics);
        }

        TiffIfdEntry[] exifEntries = CreateEntries(exifValues);
        byte[][] exifValueBytes = EncodeValues(exifValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffExifWriteResult.Create(parentOffset, 0, 0, diagnostics);
        }

        ulong exifOffset = CalculateClassicExifOffset(parentOffset, parentEntries);
        TiffIfdEntry[] actualParentEntries = BuildParentEntries(parentEntries, CreateClassicExifPointerEntry(exifOffset));
        int parentByteCount = TiffDirectoryWriter.CalculateClassicDirectoryByteCount(actualParentEntries.Length);
        TiffDiagnostics parentDiagnostics = TiffDirectoryWriter.WriteClassic(destination.Slice((int)parentOffset, parentByteCount), actualParentEntries, byteOrder, nextIfdOffset, actualOptions);
        diagnostics.Merge(parentDiagnostics);

        int exifLayoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(exifEntries, exifValueBytes);
        TiffDiagnostics exifDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(destination.Slice((int)exifOffset, exifLayoutByteCount), exifEntries, exifValueBytes, byteOrder, exifOffset, 0, actualOptions);
        diagnostics.Merge(exifDiagnostics);

        ulong endOffset = exifOffset + (ulong)exifLayoutByteCount;
        return TiffExifWriteResult.Create(parentOffset, exifOffset, endOffset - parentOffset, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffIfdEntry[] parentEntries, TiffTagValue[] exifValues)
    {
        TiffIfdEntry[] exifEntries = CreateEntries(exifValues);
        byte[][] exifValueBytes = EncodeValues(exifValues, TiffByteOrder.LittleEndian, new TiffDiagnostics());
        ulong byteCount = CalculateClassicWriteByteCountAsUInt64(parentEntries, exifEntries, exifValueBytes);
        if (byteCount > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)byteCount;
    }

    internal static ulong CalculateClassicWriteByteCountAsUInt64(TiffIfdEntry[] parentEntries, TiffIfdEntry[] exifEntries, byte[][] exifValueBytes)
    {
        TiffIfdEntry[] actualParentEntries = BuildParentEntries(parentEntries, CreateClassicExifPointerEntry(0));
        ulong parentByteCount = (ulong)TiffDirectoryWriter.CalculateClassicDirectoryByteCount(actualParentEntries.Length);
        ulong exifStart = TiffDirectoryWriter.AlignExternalValueOffset(parentByteCount, 2);
        ulong exifByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCountAsUInt64(exifEntries, exifValueBytes);
        if (exifStart > ulong.MaxValue - exifByteCount)
        {
            return ulong.MaxValue;
        }

        return exifStart + exifByteCount;
    }

    internal static ulong CalculateClassicExifOffset(ulong parentOffset, TiffIfdEntry[] parentEntries)
    {
        TiffIfdEntry[] actualParentEntries = BuildParentEntries(parentEntries, CreateClassicExifPointerEntry(0));
        ulong parentByteCount = (ulong)TiffDirectoryWriter.CalculateClassicDirectoryByteCount(actualParentEntries.Length);
        return parentOffset + TiffDirectoryWriter.AlignExternalValueOffset(parentByteCount, 2);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffIfdEntry[] parentEntries, TiffTagValue[] exifValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry[] actualParentEntries = parentEntries;
        TiffTagValue[] actualExifValues = exifValues;
        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "EXIF writer requires a known byte order.");
        }

        if (parentEntries == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "EXIF writer requires parent entries.");
            actualParentEntries = new TiffIfdEntry[0];
        }

        if (exifValues == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "EXIF writer requires EXIF tag values.");
            actualExifValues = new TiffTagValue[0];
        }

        if (parentOffset == 0 || parentOffset > int.MaxValue || parentOffset > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "EXIF parent IFD offset is outside the destination buffer.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic parent IFD next offset must fit in 32 bits.");
        }

        if (diagnostics.HasErrors())
        {
            return false;
        }

        TiffIfdEntry[] exifEntries = CreateEntries(actualExifValues);
        byte[][] exifValueBytes = EncodeValues(actualExifValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        ulong writeByteCount = CalculateClassicWriteByteCountAsUInt64(actualParentEntries, exifEntries, exifValueBytes);
        if (writeByteCount == ulong.MaxValue || parentOffset > ulong.MaxValue - writeByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "EXIF write byte range overflows the supported range.");
            return false;
        }

        if (parentOffset + writeByteCount > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "EXIF destination buffer is too short for parent and EXIF IFDs.");
        }

        ulong exifOffset = CalculateClassicExifOffset(parentOffset, actualParentEntries);
        if (!TiffMath.IsClassicOffset(exifOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic EXIF IFD offset must fit in 32 bits.");
        }

        return !diagnostics.HasErrors();
    }

    internal static TiffIfdEntry[] BuildParentEntries(TiffIfdEntry[] parentEntries, TiffIfdEntry exifPointerEntry)
    {
        int nonExifCount = CountNonExifEntries(parentEntries);
        TiffIfdEntry[] entries = new TiffIfdEntry[nonExifCount + 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (parentEntries != null && sourceIndex < parentEntries.Length)
        {
            if (parentEntries[sourceIndex] != null && parentEntries[sourceIndex].TagId != TiffTagId.ExifIfd)
            {
                entries[destinationIndex] = parentEntries[sourceIndex].Copy();
                destinationIndex = destinationIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        entries[destinationIndex] = exifPointerEntry.Copy();
        return entries;
    }

    internal static int CountNonExifEntries(TiffIfdEntry[] parentEntries)
    {
        if (parentEntries == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < parentEntries.Length)
        {
            if (parentEntries[index] != null && parentEntries[index].TagId != TiffTagId.ExifIfd)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static TiffIfdEntry CreateClassicExifPointerEntry(ulong exifOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.ExifIfd;
        entry.DataType = TiffDataType.Long;
        entry.Count = 1;
        entry.RawCount = 1;
        entry.ValueOffset = exifOffset;
        entry.ValueByteCount = 4;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];
        return entry;
    }

    internal static TiffIfdEntry[] CreateEntries(TiffTagValue[] values)
    {
        if (values == null)
        {
            return new TiffIfdEntry[0];
        }

        TiffIfdEntry[] entries = new TiffIfdEntry[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            entries[index] = CreateEntry(values[index]);
            index = index + 1;
        }

        return entries;
    }

    internal static TiffIfdEntry CreateEntry(TiffTagValue value)
    {
        TiffTagValue actualValue = value;
        if (actualValue == null)
        {
            actualValue = new TiffTagValue();
        }

        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = actualValue.TagId;
        entry.DataType = actualValue.DataType;
        entry.Count = actualValue.Count;
        entry.RawCount = (ulong)actualValue.Count;
        entry.ValueOffset = 0;
        entry.ValueOffsetBytes = new byte[0];
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(actualValue.DataType, actualValue.Count);
        entry.ValueSlotByteCount = 0;
        return entry;
    }

    internal static byte[][] EncodeValues(TiffTagValue[] values, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (values == null)
        {
            return new byte[0][];
        }

        byte[][] bytes = new byte[values.Length][];
        int index = 0;
        while (index < values.Length)
        {
            bytes[index] = EncodeValue(values[index], byteOrder, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0][];
            }

            index = index + 1;
        }

        return bytes;
    }

    internal static byte[] EncodeValue(TiffTagValue value, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (value == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "EXIF tag value is required.");
            return new byte[0];
        }

        if (!TiffDataTypeInfo.IsKnown(value.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "EXIF tag value has an unknown data type.", TiffDiagnostic.NoImageIndex, value.TagId);
            return new byte[0];
        }

        if (value.IsRaw)
        {
            return TiffTagValue.CopyByteArray(value.RawBytes);
        }

        if (value.DataType == TiffDataType.Ascii)
        {
            return TiffAscii.EncodeStrings(value.TextValues);
        }

        if (value.IsUnsigned())
        {
            return EncodeUnsignedValues(value.UnsignedValues, value.DataType, byteOrder);
        }

        if (value.IsSigned())
        {
            return EncodeSignedValues(value.SignedValues, value.DataType, byteOrder);
        }

        if (value.IsRational())
        {
            TiffRationalCodecResult result = TiffRationalCodec.Encode(value.RationalValues, value.DataType, byteOrder);
            diagnostics.Merge(result.Diagnostics);
            return result.Bytes;
        }

        if (value.IsFloating())
        {
            return EncodeFloatingValues(value.FloatingValues, value.DataType, byteOrder);
        }

        return TiffTagValue.CopyByteArray(value.RawBytes);
    }

    internal static byte[] EncodeUnsignedValues(ulong[] values, TiffDataType dataType, TiffByteOrder byteOrder)
    {
        ulong[] actualValues = TiffTagValue.CopyUnsignedArray(values);
        int elementSize = TiffDataTypeInfo.GetElementSize(dataType);
        byte[] bytes = new byte[actualValues.Length * elementSize];
        int index = 0;
        while (index < actualValues.Length)
        {
            int offset = index * elementSize;
            if (dataType == TiffDataType.Byte)
            {
                bytes[offset] = (byte)actualValues[index];
            }
            else if (dataType == TiffDataType.Short)
            {
                TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)actualValues[index], byteOrder);
            }
            else if (dataType == TiffDataType.Long || dataType == TiffDataType.Ifd)
            {
                TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset, 4), (uint)actualValues[index], byteOrder);
            }
            else
            {
                TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset, 8), actualValues[index], byteOrder);
            }

            index = index + 1;
        }

        return bytes;
    }

    internal static byte[] EncodeSignedValues(long[] values, TiffDataType dataType, TiffByteOrder byteOrder)
    {
        long[] actualValues = TiffTagValue.CopySignedArray(values);
        int elementSize = TiffDataTypeInfo.GetElementSize(dataType);
        byte[] bytes = new byte[actualValues.Length * elementSize];
        int index = 0;
        while (index < actualValues.Length)
        {
            int offset = index * elementSize;
            if (dataType == TiffDataType.SByte)
            {
                TiffBinaryWriter.WriteSByte(bytes.AsSpan(offset, 1), (sbyte)actualValues[index]);
            }
            else if (dataType == TiffDataType.SShort)
            {
                TiffBinaryWriter.WriteInt16(bytes.AsSpan(offset, 2), (short)actualValues[index], byteOrder);
            }
            else if (dataType == TiffDataType.SLong)
            {
                TiffBinaryWriter.WriteInt32(bytes.AsSpan(offset, 4), (int)actualValues[index], byteOrder);
            }
            else
            {
                TiffBinaryWriter.WriteInt64(bytes.AsSpan(offset, 8), actualValues[index], byteOrder);
            }

            index = index + 1;
        }

        return bytes;
    }

    internal static byte[] EncodeFloatingValues(double[] values, TiffDataType dataType, TiffByteOrder byteOrder)
    {
        double[] actualValues = TiffTagValue.CopyFloatingArray(values);
        int elementSize = TiffDataTypeInfo.GetElementSize(dataType);
        byte[] bytes = new byte[actualValues.Length * elementSize];
        int index = 0;
        while (index < actualValues.Length)
        {
            int offset = index * elementSize;
            if (dataType == TiffDataType.Float)
            {
                TiffBinaryWriter.WriteSingle(bytes.AsSpan(offset, 4), (float)actualValues[index], byteOrder);
            }
            else
            {
                TiffBinaryWriter.WriteDouble(bytes.AsSpan(offset, 8), actualValues[index], byteOrder);
            }

            index = index + 1;
        }

        return bytes;
    }
}
