using System;

namespace tiff;

internal class TiffGpsMetadataWriter
{
    internal static TiffGpsWriteResult WriteClassic(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] gpsValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, parentEntries, gpsValues, byteOrder, parentOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffGpsWriteResult WriteClassic(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] gpsValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, parentEntries, gpsValues, byteOrder, parentOffset, nextIfdOffset, diagnostics))
        {
            return TiffGpsWriteResult.Create(parentOffset, 0, 0, diagnostics);
        }

        TiffIfdEntry[] gpsEntries = TiffExifMetadataWriter.CreateEntries(gpsValues);
        byte[][] gpsValueBytes = TiffExifMetadataWriter.EncodeValues(gpsValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGpsWriteResult.Create(parentOffset, 0, 0, diagnostics);
        }

        ulong gpsOffset = CalculateClassicGpsOffset(parentOffset, parentEntries);
        TiffIfdEntry[] actualParentEntries = BuildParentEntries(parentEntries, CreateClassicGpsPointerEntry(gpsOffset));
        int parentByteCount = TiffDirectoryWriter.CalculateClassicDirectoryByteCount(actualParentEntries.Length);
        TiffDiagnostics parentDiagnostics = TiffDirectoryWriter.WriteClassic(destination.Slice((int)parentOffset, parentByteCount), actualParentEntries, byteOrder, nextIfdOffset, actualOptions);
        diagnostics.Merge(parentDiagnostics);

        int gpsLayoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(gpsEntries, gpsValueBytes);
        TiffDiagnostics gpsDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(destination.Slice((int)gpsOffset, gpsLayoutByteCount), gpsEntries, gpsValueBytes, byteOrder, gpsOffset, 0, actualOptions);
        diagnostics.Merge(gpsDiagnostics);

        ulong endOffset = gpsOffset + (ulong)gpsLayoutByteCount;
        return TiffGpsWriteResult.Create(parentOffset, gpsOffset, endOffset - parentOffset, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffIfdEntry[] parentEntries, TiffTagValue[] gpsValues)
    {
        TiffIfdEntry[] gpsEntries = TiffExifMetadataWriter.CreateEntries(gpsValues);
        byte[][] gpsValueBytes = TiffExifMetadataWriter.EncodeValues(gpsValues, TiffByteOrder.LittleEndian, new TiffDiagnostics());
        TiffIfdEntry[] actualParentEntries = BuildParentEntries(parentEntries, CreateClassicGpsPointerEntry(0));
        ulong parentByteCount = (ulong)TiffDirectoryWriter.CalculateClassicDirectoryByteCount(actualParentEntries.Length);
        ulong gpsStart = TiffDirectoryWriter.AlignExternalValueOffset(parentByteCount, 2);
        ulong gpsByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCountAsUInt64(gpsEntries, gpsValueBytes);
        ulong total = gpsStart + gpsByteCount;
        if (total > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)total;
    }

    internal static ulong CalculateClassicGpsOffset(ulong parentOffset, TiffIfdEntry[] parentEntries)
    {
        TiffIfdEntry[] actualParentEntries = BuildParentEntries(parentEntries, CreateClassicGpsPointerEntry(0));
        ulong parentByteCount = (ulong)TiffDirectoryWriter.CalculateClassicDirectoryByteCount(actualParentEntries.Length);
        return parentOffset + TiffDirectoryWriter.AlignExternalValueOffset(parentByteCount, 2);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffIfdEntry[] parentEntries, TiffTagValue[] gpsValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry[] actualParentEntries = parentEntries;
        TiffTagValue[] actualGpsValues = gpsValues;
        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "GPS writer requires a known byte order.");
        }

        if (parentEntries == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "GPS writer requires parent entries.");
            actualParentEntries = new TiffIfdEntry[0];
        }

        if (gpsValues == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "GPS writer requires GPS tag values.");
            actualGpsValues = new TiffTagValue[0];
        }

        if (parentOffset == 0 || parentOffset > int.MaxValue || parentOffset > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "GPS parent IFD offset is outside the destination buffer.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic parent IFD next offset must fit in 32 bits.");
        }

        if (diagnostics.HasErrors())
        {
            return false;
        }

        int byteCount = CalculateClassicWriteByteCount(actualParentEntries, actualGpsValues);
        if (parentOffset > ulong.MaxValue - (ulong)byteCount || parentOffset + (ulong)byteCount > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "GPS destination buffer is too short for parent and GPS IFDs.");
        }

        ulong gpsOffset = CalculateClassicGpsOffset(parentOffset, actualParentEntries);
        if (!TiffMath.IsClassicOffset(gpsOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic GPS IFD offset must fit in 32 bits.");
        }

        return !diagnostics.HasErrors();
    }

    internal static TiffIfdEntry[] BuildParentEntries(TiffIfdEntry[] parentEntries, TiffIfdEntry gpsPointerEntry)
    {
        int nonGpsCount = CountNonGpsEntries(parentEntries);
        TiffIfdEntry[] entries = new TiffIfdEntry[nonGpsCount + 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (parentEntries != null && sourceIndex < parentEntries.Length)
        {
            if (parentEntries[sourceIndex] != null && parentEntries[sourceIndex].TagId != TiffTagId.GpsIfd)
            {
                entries[destinationIndex] = parentEntries[sourceIndex].Copy();
                destinationIndex = destinationIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        entries[destinationIndex] = gpsPointerEntry.Copy();
        return entries;
    }

    internal static int CountNonGpsEntries(TiffIfdEntry[] parentEntries)
    {
        if (parentEntries == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < parentEntries.Length)
        {
            if (parentEntries[index] != null && parentEntries[index].TagId != TiffTagId.GpsIfd)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static TiffIfdEntry CreateClassicGpsPointerEntry(ulong gpsOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.GpsIfd;
        entry.DataType = TiffDataType.Long;
        entry.Count = 1;
        entry.RawCount = 1;
        entry.ValueOffset = gpsOffset;
        entry.ValueByteCount = 4;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];
        return entry;
    }
}
