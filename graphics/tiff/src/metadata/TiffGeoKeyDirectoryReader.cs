using System;

namespace tiff;

internal class TiffGeoKeyDirectoryReader
{
    internal static TiffGeoKeyDirectoryReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        if (header == null)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF header is required to read GeoKeyDirectory metadata.");
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        return Read(directory, fileBytes, header.ByteOrder);
    }

    internal static TiffGeoKeyDirectoryReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return Read(directory, fileBytes, byteOrder, new TiffLimits());
    }

    internal static TiffGeoKeyDirectoryReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read GeoKeyDirectory metadata.");
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "GeoKeyDirectory parsing requires a known byte order.");
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.GeoKeyDirectory);
        if (entry == null)
        {
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        if (entry.DataType != TiffDataType.Short)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "GeoKeyDirectoryTag must use SHORT data.", directory.ImageIndex, TiffTagId.GeoKeyDirectory);
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        if (!IsValidDirectoryCount(entry.Count))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoKeyDirectoryTag must contain a four-short header followed by four-short key entries.", directory.ImageIndex, TiffTagId.GeoKeyDirectory);
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        ushort[] values = DecodeShorts(loadResult.Bytes, entry.Count, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        TiffGeoKeyDirectory geoDirectory = CreateDirectory(values, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        TiffGeoParameterReadResult parameterResult = TiffGeoParameterReader.Read(directory, fileBytes, byteOrder, limits);
        diagnostics.Merge(parameterResult.Diagnostics);
        TiffGeoModelReadResult modelResult = TiffGeoModelMetadataReader.Read(directory, fileBytes, byteOrder, limits);
        diagnostics.Merge(modelResult.Diagnostics);
        TiffGeoKeyDirectory resolved = TiffGeoKeyResolver.Resolve(geoDirectory, parameterResult.Metadata, directory.ImageIndex, diagnostics);
        return TiffGeoKeyDirectoryReadResult.Create(resolved, diagnostics);
    }

    internal static bool IsValidDirectoryCount(long count)
    {
        if (count < 4)
        {
            return false;
        }

        return (count - 4) % 4 == 0;
    }

    internal static ushort[] DecodeShorts(byte[] bytes, long count, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (count > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "GeoKeyDirectoryTag count is too large to allocate.");
            return new ushort[0];
        }

        long expectedByteCount = count * 2;
        if (bytes == null || bytes.Length != expectedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "GeoKeyDirectoryTag byte count does not match its value count.");
            return new ushort[0];
        }

        ushort[] values = new ushort[(int)count];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * 2;
            values[index] = TiffBinaryReader.ReadUInt16(bytes.AsSpan(offset, 2), byteOrder);
            index = index + 1;
        }

        return values;
    }

    internal static TiffGeoKeyDirectory CreateDirectory(ushort[] values, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (values == null || values.Length < 4)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoKeyDirectoryTag is missing its four-short header.", imageIndex, TiffTagId.GeoKeyDirectory);
            return new TiffGeoKeyDirectory();
        }

        int numberOfKeys = values[3];
        int expectedCount = 4 + (numberOfKeys * 4);
        if (values.Length != expectedCount)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoKeyDirectoryTag key count does not match its declared header value.", imageIndex, TiffTagId.GeoKeyDirectory);
            return new TiffGeoKeyDirectory();
        }

        TiffGeoKeyEntry[] keys = new TiffGeoKeyEntry[numberOfKeys];
        int index = 0;
        while (index < keys.Length)
        {
            int offset = 4 + (index * 4);
            keys[index] = TiffGeoKeyEntry.Create(values[offset], values[offset + 1], values[offset + 2], values[offset + 3]);
            index = index + 1;
        }

        return TiffGeoKeyDirectory.Create(true, values[0], values[1], values[2], numberOfKeys, keys);
    }
}
