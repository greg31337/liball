using System;
using System.Text;

namespace tiff;

internal class TiffXmpMetadataWriter
{
    internal static TiffXmpWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, byte[] xmpBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, values, xmpBytes, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffXmpWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, byte[] xmpBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, values, xmpBytes, byteOrder, directoryOffset, nextIfdOffset, diagnostics))
        {
            return TiffXmpWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        TiffTagValue[] actualValues = BuildValues(values, CreateValue(xmpBytes));
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffXmpWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        int byteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        TiffDiagnostics writeDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(destination.Slice((int)directoryOffset, byteCount), entries, valueBytes, byteOrder, directoryOffset, nextIfdOffset, actualOptions);
        diagnostics.Merge(writeDiagnostics);
        return TiffXmpWriteResult.Create(directoryOffset, (ulong)byteCount, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffTagValue[] values, byte[] xmpBytes)
    {
        TiffTagValue[] actualValues = BuildValues(values, CreateValue(xmpBytes));
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, TiffByteOrder.LittleEndian, new TiffDiagnostics());
        return TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffTagValue[] values, byte[] xmpBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] actualValues = values;
        if (values == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "XMP writer requires directory tag values.");
            actualValues = new TiffTagValue[0];
        }

        if (xmpBytes == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "XMP writer requires packet bytes.");
            xmpBytes = new byte[0];
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "XMP writer requires a known byte order.");
        }

        if (directoryOffset == 0 || directoryOffset > int.MaxValue || directoryOffset > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "XMP directory offset is outside the destination buffer.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic XMP IFD next offset must fit in 32 bits.");
        }

        if (diagnostics.HasErrors())
        {
            return false;
        }

        int byteCount = CalculateClassicWriteByteCount(actualValues, xmpBytes);
        if (directoryOffset > ulong.MaxValue - (ulong)byteCount || directoryOffset + (ulong)byteCount > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "XMP destination buffer is too short for the directory and packet.");
        }

        return !diagnostics.HasErrors();
    }

    internal static TiffTagValue CreateValue(string text)
    {
        if (text == null)
        {
            text = "";
        }

        return CreateValue(Encoding.UTF8.GetBytes(text));
    }

    internal static TiffTagValue CreateValue(byte[] xmpBytes)
    {
        byte[] bytes = TiffTagValue.CopyByteArray(xmpBytes);
        return TiffTagValue.CreateRaw(TiffTagId.Xmp, TiffDataType.Byte, bytes.Length, bytes);
    }

    internal static TiffTagValue[] BuildValues(TiffTagValue[] values, TiffTagValue xmpValue)
    {
        int nonXmpCount = CountNonXmpValues(values);
        TiffTagValue[] result = new TiffTagValue[nonXmpCount + 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (values != null && sourceIndex < values.Length)
        {
            if (values[sourceIndex] != null && values[sourceIndex].TagId != TiffTagId.Xmp)
            {
                result[destinationIndex] = values[sourceIndex].Copy();
                destinationIndex = destinationIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        result[destinationIndex] = xmpValue.Copy();
        return result;
    }

    internal static int CountNonXmpValues(TiffTagValue[] values)
    {
        if (values == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && values[index].TagId != TiffTagId.Xmp)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
