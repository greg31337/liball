using System;

namespace tiff;

internal class TiffGdalMetadataWriter
{
    internal static TiffGdalMetadataWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, TiffGdalMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, values, metadata, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffGdalMetadataWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, TiffGdalMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, values, metadata, byteOrder, directoryOffset, nextIfdOffset, diagnostics))
        {
            return TiffGdalMetadataWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        TiffTagValue[] actualValues = BuildValues(values, metadata, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGdalMetadataWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGdalMetadataWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        int byteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        TiffDiagnostics writeDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(destination.Slice((int)directoryOffset, byteCount), entries, valueBytes, byteOrder, directoryOffset, nextIfdOffset, actualOptions);
        diagnostics.Merge(writeDiagnostics);
        return TiffGdalMetadataWriteResult.Create(directoryOffset, (ulong)byteCount, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffTagValue[] values, TiffGdalMetadata metadata)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffTagValue[] actualValues = BuildValues(values, metadata, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, TiffByteOrder.LittleEndian, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        return TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffTagValue[] values, TiffGdalMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] actualValues = values;
        if (values == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "GDAL metadata writer requires directory tag values.");
            actualValues = new TiffTagValue[0];
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "GDAL metadata writer requires a known byte order.");
        }

        if (directoryOffset == 0 || directoryOffset > int.MaxValue || directoryOffset > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "GDAL metadata directory offset is outside the destination buffer.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic GDAL metadata IFD next offset must fit in 32 bits.");
        }

        if (diagnostics.HasErrors())
        {
            return false;
        }

        TiffTagValue[] encodedValues = BuildValues(actualValues, metadata, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(encodedValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(encodedValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        int byteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        if (directoryOffset > ulong.MaxValue - (ulong)byteCount || directoryOffset + (ulong)byteCount > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "GDAL metadata destination buffer is too short for the directory.");
        }

        return !diagnostics.HasErrors();
    }

    internal static TiffTagValue[] BuildValues(TiffTagValue[] values, TiffGdalMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffGdalMetadata actualMetadata = NormalizeMetadata(metadata);
        TiffTagValue[] gdalValues = CreateGdalValues(actualMetadata, diagnostics);
        int nonGdalCount = CountNonGdalValues(values);
        TiffTagValue[] result = new TiffTagValue[nonGdalCount + gdalValues.Length];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (values != null && sourceIndex < values.Length)
        {
            if (values[sourceIndex] != null && !IsGdalTag(values[sourceIndex].TagId))
            {
                result[destinationIndex] = values[sourceIndex].Copy();
                destinationIndex = destinationIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        int gdalIndex = 0;
        while (gdalIndex < gdalValues.Length)
        {
            result[destinationIndex] = gdalValues[gdalIndex].Copy();
            destinationIndex = destinationIndex + 1;
            gdalIndex = gdalIndex + 1;
        }

        return result;
    }

    internal static TiffTagValue[] CreateGdalValues(TiffGdalMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] values = new TiffTagValue[0];
        if (metadata.HasMetadata)
        {
            values = AppendValue(values, CreateMetadataValue(metadata.MetadataXml));
        }

        if (metadata.HasNoData)
        {
            values = AppendValue(values, CreateNoDataValue(metadata.NoDataText));
        }

        return values;
    }

    internal static TiffTagValue CreateMetadataValue(string metadataXml)
    {
        return TiffTagValue.CreateAscii(TiffTagId.GdalMetadata, TiffGdalAsciiValue.NormalizeString(metadataXml));
    }

    internal static TiffTagValue CreateNoDataValue(string noDataText)
    {
        return TiffTagValue.CreateAscii(TiffTagId.GdalNoData, TiffGdalAsciiValue.NormalizeString(noDataText));
    }

    internal static TiffTagValue[] AppendValue(TiffTagValue[] values, TiffTagValue value)
    {
        TiffTagValue[] result = new TiffTagValue[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }

    internal static int CountNonGdalValues(TiffTagValue[] values)
    {
        if (values == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && !IsGdalTag(values[index].TagId))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static bool IsGdalTag(int tagId)
    {
        return tagId == TiffTagId.GdalMetadata || tagId == TiffTagId.GdalNoData;
    }

    internal static TiffGdalMetadata NormalizeMetadata(TiffGdalMetadata metadata)
    {
        if (metadata == null)
        {
            return new TiffGdalMetadata();
        }

        return metadata.Copy();
    }
}
