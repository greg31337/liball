using System;

namespace tiff;

internal class TiffIccProfileWriter
{
    internal static TiffIccProfileWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, byte[] profileBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, values, profileBytes, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffIccProfileWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, byte[] profileBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, values, profileBytes, byteOrder, directoryOffset, nextIfdOffset, diagnostics))
        {
            return TiffIccProfileWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        TiffTagValue[] actualValues = BuildValues(values, CreateValue(profileBytes));
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffIccProfileWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        int byteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        TiffDiagnostics writeDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(destination.Slice((int)directoryOffset, byteCount), entries, valueBytes, byteOrder, directoryOffset, nextIfdOffset, actualOptions);
        diagnostics.Merge(writeDiagnostics);
        return TiffIccProfileWriteResult.Create(directoryOffset, (ulong)byteCount, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffTagValue[] values, byte[] profileBytes)
    {
        TiffTagValue[] actualValues = BuildValues(values, CreateValue(profileBytes));
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, TiffByteOrder.LittleEndian, new TiffDiagnostics());
        return TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffTagValue[] values, byte[] profileBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] actualValues = values;
        if (values == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "ICC writer requires directory tag values.");
            actualValues = new TiffTagValue[0];
        }

        if (profileBytes == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "ICC writer requires profile bytes.");
            profileBytes = new byte[0];
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "ICC writer requires a known byte order.");
        }

        if (directoryOffset == 0 || directoryOffset > int.MaxValue || directoryOffset > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "ICC directory offset is outside the destination buffer.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic ICC IFD next offset must fit in 32 bits.");
        }

        if (diagnostics.HasErrors())
        {
            return false;
        }

        int byteCount = CalculateClassicWriteByteCount(actualValues, profileBytes);
        if (directoryOffset > ulong.MaxValue - (ulong)byteCount || directoryOffset + (ulong)byteCount > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "ICC destination buffer is too short for the directory and profile.");
        }

        return !diagnostics.HasErrors();
    }

    internal static TiffTagValue CreateValue(byte[] profileBytes)
    {
        byte[] bytes = TiffTagValue.CopyByteArray(profileBytes);
        return TiffTagValue.CreateRaw(TiffTagId.IccProfile, TiffDataType.Undefined, bytes.Length, bytes);
    }

    internal static TiffTagValue[] BuildValues(TiffTagValue[] values, TiffTagValue profileValue)
    {
        int nonProfileCount = CountNonProfileValues(values);
        TiffTagValue[] result = new TiffTagValue[nonProfileCount + 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (values != null && sourceIndex < values.Length)
        {
            if (values[sourceIndex] != null && values[sourceIndex].TagId != TiffTagId.IccProfile)
            {
                result[destinationIndex] = values[sourceIndex].Copy();
                destinationIndex = destinationIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        result[destinationIndex] = profileValue.Copy();
        return result;
    }

    internal static int CountNonProfileValues(TiffTagValue[] values)
    {
        if (values == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && values[index].TagId != TiffTagId.IccProfile)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
