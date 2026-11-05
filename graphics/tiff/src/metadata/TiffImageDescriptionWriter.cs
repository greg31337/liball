using System;

namespace tiff;

internal class TiffImageDescriptionWriter
{
    internal static TiffImageDescriptionWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, string description, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, values, description, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffImageDescriptionWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, string description, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, values, description, byteOrder, directoryOffset, nextIfdOffset, diagnostics))
        {
            return TiffImageDescriptionWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        TiffTagValue[] actualValues = BuildValues(values, CreateValue(description));
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffImageDescriptionWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        int byteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        TiffDiagnostics writeDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(destination.Slice((int)directoryOffset, byteCount), entries, valueBytes, byteOrder, directoryOffset, nextIfdOffset, actualOptions);
        diagnostics.Merge(writeDiagnostics);
        return TiffImageDescriptionWriteResult.Create(directoryOffset, (ulong)byteCount, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffTagValue[] values, string description)
    {
        TiffTagValue[] actualValues = BuildValues(values, CreateValue(description));
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, TiffByteOrder.LittleEndian, new TiffDiagnostics());
        return TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffTagValue[] values, string description, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] actualValues = values;
        if (values == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "ImageDescription writer requires directory tag values.");
            actualValues = new TiffTagValue[0];
        }

        if (description == null)
        {
            description = "";
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "ImageDescription writer requires a known byte order.");
        }

        if (directoryOffset == 0 || directoryOffset > int.MaxValue || directoryOffset > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "ImageDescription directory offset is outside the destination buffer.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic ImageDescription IFD next offset must fit in 32 bits.");
        }

        if (diagnostics.HasErrors())
        {
            return false;
        }

        int byteCount = CalculateClassicWriteByteCount(actualValues, description);
        if (directoryOffset > ulong.MaxValue - (ulong)byteCount || directoryOffset + (ulong)byteCount > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "ImageDescription destination buffer is too short for the directory.");
        }

        return !diagnostics.HasErrors();
    }

    internal static TiffTagValue CreateValue(string description)
    {
        return TiffTagValue.CreateAscii(TiffTagId.ImageDescription, description);
    }

    internal static TiffTagValue[] BuildValues(TiffTagValue[] values, TiffTagValue descriptionValue)
    {
        int nonDescriptionCount = CountNonDescriptionValues(values);
        TiffTagValue[] result = new TiffTagValue[nonDescriptionCount + 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (values != null && sourceIndex < values.Length)
        {
            if (values[sourceIndex] != null && values[sourceIndex].TagId != TiffTagId.ImageDescription)
            {
                result[destinationIndex] = values[sourceIndex].Copy();
                destinationIndex = destinationIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        result[destinationIndex] = descriptionValue.Copy();
        return result;
    }

    internal static int CountNonDescriptionValues(TiffTagValue[] values)
    {
        if (values == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && values[index].TagId != TiffTagId.ImageDescription)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
