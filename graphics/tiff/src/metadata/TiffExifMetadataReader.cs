using System;

namespace tiff;

internal class TiffExifMetadataReader
{
    internal static TiffTagValue ReadSourceValue(TiffIfdEntry entry, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadSourceBytes(entry, source, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return new TiffTagValue();
        }

        return DecodeValue(entry, loadResult.Bytes, byteOrder, diagnostics);
    }

    internal static TiffExifReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        if (header == null)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF header is required to read EXIF metadata.");
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        return Read(directory, fileBytes, header.ByteOrder, header.FileKind);
    }

    internal static TiffExifReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        return Read(directory, fileBytes, byteOrder, fileKind, new TiffReadOptions());
    }

    internal static TiffExifReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A parent directory is required to read EXIF metadata.");
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        TiffIfdPointerReadResult pointerResult = TiffIfdPointerReader.Read(directory, fileBytes, byteOrder, fileKind, options);
        diagnostics.Merge(pointerResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        TiffIfdPointer exifPointer = FindPointer(pointerResult.Pointers, TiffTagId.ExifIfd);
        if (exifPointer.TagId == 0)
        {
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        if (exifPointer.Directories.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidOffset, "EXIF IFD pointer did not resolve to a directory.", directory.ImageIndex, TiffTagId.ExifIfd);
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        TiffDirectory exifDirectory = exifPointer.Directories[0];
        TiffLimits limits = TiffDirectoryReader.GetLimits(TiffDirectoryReader.NormalizeOptions(options));
        TiffTagValue[] values = ReadValues(exifDirectory, fileBytes, byteOrder, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        TiffIfdPointer[] interoperabilityPointers = FilterPointers(pointerResult.Pointers, TiffTagId.InteroperabilityIfd);
        TiffExifMetadata metadata = TiffExifMetadata.Create(true, directory.ImageIndex, directory.Offset, exifPointer.Offset, exifDirectory, values, interoperabilityPointers);
        return TiffExifReadResult.Create(metadata, diagnostics);
    }

    internal static TiffIfdPointer FindPointer(TiffIfdPointer[] pointers, int tagId)
    {
        if (pointers == null)
        {
            return new TiffIfdPointer();
        }

        int index = 0;
        while (index < pointers.Length)
        {
            if (pointers[index] != null && pointers[index].TagId == tagId)
            {
                return pointers[index].Copy();
            }

            index = index + 1;
        }

        return new TiffIfdPointer();
    }

    internal static TiffIfdPointer[] FilterPointers(TiffIfdPointer[] pointers, int tagId)
    {
        TiffIfdPointer[] filtered = new TiffIfdPointer[0];
        if (pointers == null)
        {
            return filtered;
        }

        int index = 0;
        while (index < pointers.Length)
        {
            if (pointers[index] != null && pointers[index].TagId == tagId)
            {
                filtered = TiffIfdPointerReader.AppendPointer(filtered, pointers[index]);
            }

            index = index + 1;
        }

        return filtered;
    }

    internal static TiffTagValue[] ReadValues(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] values = new TiffTagValue[0];
        int index = 0;
        while (index < directory.Entries.Length)
        {
            TiffTagValue value = ReadValue(directory.Entries[index], fileBytes, byteOrder, limits, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffTagValue[0];
            }

            values = AppendValue(values, value);
            index = index + 1;
        }

        return values;
    }

    internal static TiffTagValue ReadValue(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return new TiffTagValue();
        }

        if (entry.DataType == TiffDataType.Ascii)
        {
            TiffAsciiResult asciiResult = TiffAscii.Decode(loadResult.Bytes);
            diagnostics.Merge(asciiResult.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffTagValue();
            }

            return TiffTagValue.CreateAsciiArray(entry.TagId, asciiResult.Values);
        }

        if (TiffTagValue.IsUnsignedDataType(entry.DataType))
        {
            return TiffTagValue.CreateUnsignedArray(entry.TagId, entry.DataType, DecodeUnsignedValues(loadResult.Bytes, entry.DataType, entry.Count, byteOrder, diagnostics));
        }

        if (TiffTagValue.IsSignedDataType(entry.DataType))
        {
            return TiffTagValue.CreateSignedArray(entry.TagId, entry.DataType, DecodeSignedValues(loadResult.Bytes, entry.DataType, entry.Count, byteOrder, diagnostics));
        }

        if (TiffTagValue.IsRationalDataType(entry.DataType))
        {
            TiffRationalCodecResult rationalResult = TiffRationalCodec.Decode(loadResult.Bytes, entry.DataType, byteOrder);
            diagnostics.Merge(rationalResult.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffTagValue();
            }

            return TiffTagValue.CreateRationalArray(entry.TagId, entry.DataType, rationalResult.Values);
        }

        if (TiffTagValue.IsFloatingDataType(entry.DataType))
        {
            return TiffTagValue.CreateFloatingArray(entry.TagId, entry.DataType, DecodeFloatingValues(loadResult.Bytes, entry.DataType, entry.Count, byteOrder, diagnostics));
        }

        return TiffTagValue.CreateRaw(entry.TagId, entry.DataType, entry.Count, loadResult.Bytes);
    }

    internal static TiffTagValue DecodeValue(TiffIfdEntry entry, byte[] bytes, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (entry.DataType == TiffDataType.Ascii)
        {
            TiffAsciiResult asciiResult = TiffAscii.Decode(bytes);
            diagnostics.Merge(asciiResult.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffTagValue();
            }

            return TiffTagValue.CreateAsciiArray(entry.TagId, asciiResult.Values);
        }

        if (TiffTagValue.IsUnsignedDataType(entry.DataType))
        {
            return TiffTagValue.CreateUnsignedArray(entry.TagId, entry.DataType, DecodeUnsignedValues(bytes, entry.DataType, entry.Count, byteOrder, diagnostics));
        }

        if (TiffTagValue.IsSignedDataType(entry.DataType))
        {
            return TiffTagValue.CreateSignedArray(entry.TagId, entry.DataType, DecodeSignedValues(bytes, entry.DataType, entry.Count, byteOrder, diagnostics));
        }

        if (TiffTagValue.IsRationalDataType(entry.DataType))
        {
            TiffRationalCodecResult rationalResult = TiffRationalCodec.Decode(bytes, entry.DataType, byteOrder);
            diagnostics.Merge(rationalResult.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffTagValue();
            }

            return TiffTagValue.CreateRationalArray(entry.TagId, entry.DataType, rationalResult.Values);
        }

        if (TiffTagValue.IsFloatingDataType(entry.DataType))
        {
            return TiffTagValue.CreateFloatingArray(entry.TagId, entry.DataType, DecodeFloatingValues(bytes, entry.DataType, entry.Count, byteOrder, diagnostics));
        }

        return TiffTagValue.CreateRaw(entry.TagId, entry.DataType, entry.Count, bytes);
    }

    internal static ulong[] DecodeUnsignedValues(byte[] bytes, TiffDataType dataType, long count, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateDecodedValueBytes(bytes, dataType, count, diagnostics))
        {
            return new ulong[0];
        }

        int elementSize = TiffDataTypeInfo.GetElementSize(dataType);
        ulong[] values = new ulong[(int)count];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * elementSize;
            values[index] = TiffImageInfoExtractor.ReadUnsignedValue(bytes, offset, dataType, byteOrder);
            index = index + 1;
        }

        return values;
    }

    internal static long[] DecodeSignedValues(byte[] bytes, TiffDataType dataType, long count, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateDecodedValueBytes(bytes, dataType, count, diagnostics))
        {
            return new long[0];
        }

        int elementSize = TiffDataTypeInfo.GetElementSize(dataType);
        long[] values = new long[(int)count];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * elementSize;
            values[index] = ReadSignedValue(bytes, offset, dataType, byteOrder);
            index = index + 1;
        }

        return values;
    }

    internal static double[] DecodeFloatingValues(byte[] bytes, TiffDataType dataType, long count, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateDecodedValueBytes(bytes, dataType, count, diagnostics))
        {
            return new double[0];
        }

        int elementSize = TiffDataTypeInfo.GetElementSize(dataType);
        double[] values = new double[(int)count];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * elementSize;
            if (dataType == TiffDataType.Float)
            {
                values[index] = TiffBinaryReader.ReadSingle(bytes.AsSpan(offset, 4), byteOrder);
            }
            else
            {
                values[index] = TiffBinaryReader.ReadDouble(bytes.AsSpan(offset, 8), byteOrder);
            }

            index = index + 1;
        }

        return values;
    }

    internal static long ReadSignedValue(byte[] bytes, int offset, TiffDataType dataType, TiffByteOrder byteOrder)
    {
        if (dataType == TiffDataType.SByte)
        {
            return TiffBinaryReader.ReadSByte(bytes.AsSpan(offset, 1));
        }

        if (dataType == TiffDataType.SShort)
        {
            return TiffBinaryReader.ReadInt16(bytes.AsSpan(offset, 2), byteOrder);
        }

        if (dataType == TiffDataType.SLong)
        {
            return TiffBinaryReader.ReadInt32(bytes.AsSpan(offset, 4), byteOrder);
        }

        return TiffBinaryReader.ReadInt64(bytes.AsSpan(offset, 8), byteOrder);
    }

    internal static bool ValidateDecodedValueBytes(byte[] bytes, TiffDataType dataType, long count, TiffDiagnostics diagnostics)
    {
        if (count < 0 || count > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "EXIF tag count is outside the supported range.");
            return false;
        }

        long expectedByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        if (expectedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "EXIF tag value byte count is too large to decode.");
            return false;
        }

        if (bytes == null || bytes.Length < expectedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "EXIF tag value bytes are shorter than the declared count.");
            return false;
        }

        return true;
    }

    internal static TiffTagValue[] AppendValue(TiffTagValue[] values, TiffTagValue value)
    {
        if (values == null)
        {
            TiffTagValue[] single = new TiffTagValue[1];
            single[0] = value.Copy();
            return single;
        }

        TiffTagValue[] copy = new TiffTagValue[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index].Copy();
            index = index + 1;
        }

        copy[copy.Length - 1] = value.Copy();
        return copy;
    }
}
