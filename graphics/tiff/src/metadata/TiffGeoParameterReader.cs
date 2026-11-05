using System;

namespace tiff;

internal class TiffGeoParameterReader
{
    internal static TiffGeoParameterReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        if (header == null)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF header is required to read GeoTIFF parameter metadata.");
            return TiffGeoParameterReadResult.Create(new TiffGeoParameterMetadata(), diagnostics);
        }

        return Read(directory, fileBytes, header.ByteOrder);
    }

    internal static TiffGeoParameterReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return Read(directory, fileBytes, byteOrder, new TiffLimits());
    }

    internal static TiffGeoParameterReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read GeoTIFF parameter metadata.");
            return TiffGeoParameterReadResult.Create(new TiffGeoParameterMetadata(), diagnostics);
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "GeoTIFF parameter parsing requires a known byte order.");
            return TiffGeoParameterReadResult.Create(new TiffGeoParameterMetadata(), diagnostics);
        }

        double[] doubleParams = ReadDoubleParams(directory, fileBytes, byteOrder, limits, diagnostics);
        TiffGeoAsciiParams asciiParams = ReadAsciiParams(directory, fileBytes, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGeoParameterReadResult.Create(new TiffGeoParameterMetadata(), diagnostics);
        }

        TiffGeoParameterMetadata metadata = TiffGeoParameterMetadata.Create(doubleParams.Length > 0, doubleParams, asciiParams.HasAsciiParams, asciiParams.Text, asciiParams.Values, asciiParams.Bytes);
        return TiffGeoParameterReadResult.Create(metadata, diagnostics);
    }

    internal static double[] ReadDoubleParams(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.GeoDoubleParams);
        if (entry == null)
        {
            return new double[0];
        }

        if (entry.DataType != TiffDataType.Double)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "GeoDoubleParamsTag must use DOUBLE data.", directory.ImageIndex, TiffTagId.GeoDoubleParams);
            return new double[0];
        }

        if (entry.Count <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoDoubleParamsTag must contain at least one value.", directory.ImageIndex, TiffTagId.GeoDoubleParams);
            return new double[0];
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return new double[0];
        }

        return DecodeDoubles(loadResult.Bytes, entry.Count, byteOrder, diagnostics);
    }

    internal static TiffGeoAsciiParams ReadAsciiParams(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.GeoAsciiParams);
        if (entry == null)
        {
            return new TiffGeoAsciiParams();
        }

        if (entry.DataType != TiffDataType.Ascii)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "GeoAsciiParamsTag must use ASCII data.", directory.ImageIndex, TiffTagId.GeoAsciiParams);
            return new TiffGeoAsciiParams();
        }

        if (entry.Count <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoAsciiParamsTag must contain at least one byte.", directory.ImageIndex, TiffTagId.GeoAsciiParams);
            return new TiffGeoAsciiParams();
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return new TiffGeoAsciiParams();
        }

        return DecodeAsciiParams(loadResult.Bytes, diagnostics);
    }

    internal static double[] DecodeDoubles(byte[] bytes, long count, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (count > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "GeoDoubleParamsTag count is too large to allocate.");
            return new double[0];
        }

        long expectedByteCount = count * 8;
        if (bytes == null || bytes.Length != expectedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "GeoDoubleParamsTag byte count does not match its value count.");
            return new double[0];
        }

        double[] values = new double[(int)count];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * 8;
            values[index] = TiffBinaryReader.ReadDouble(bytes.AsSpan(offset, 8), byteOrder);
            index = index + 1;
        }

        return values;
    }

    internal static TiffGeoAsciiParams DecodeAsciiParams(byte[] bytes, TiffDiagnostics diagnostics)
    {
        if (bytes == null || bytes.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "GeoAsciiParamsTag must contain at least one ASCII byte.");
            return new TiffGeoAsciiParams();
        }

        string text = DecodeAsciiText(bytes, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new TiffGeoAsciiParams();
        }

        return TiffGeoAsciiParams.Create(true, text, SplitAsciiValues(text), bytes);
    }

    internal static string DecodeAsciiText(byte[] bytes, TiffDiagnostics diagnostics)
    {
        int length = bytes.Length;
        if (length > 0 && bytes[length - 1] == 0)
        {
            length = length - 1;
        }

        char[] chars = new char[length];
        int index = 0;
        while (index < length)
        {
            byte value = bytes[index];
            if (value > 127)
            {
                diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "GeoAsciiParamsTag contains a byte outside the 7-bit ASCII range.");
                chars[index] = '?';
            }
            else
            {
                chars[index] = (char)value;
            }

            index = index + 1;
        }

        return new string(chars);
    }

    internal static string[] SplitAsciiValues(string text)
    {
        string normalized = TiffGeoParameterMetadata.NormalizeString(text);
        if (normalized.Length == 0)
        {
            return new string[0];
        }

        int count = CountPipeDelimitedValues(normalized);
        string[] values = new string[count];
        int valueIndex = 0;
        int start = 0;
        int index = 0;
        while (index < normalized.Length)
        {
            if (normalized[index] == '|')
            {
                if (index > start)
                {
                    values[valueIndex] = normalized.Substring(start, index - start);
                    valueIndex = valueIndex + 1;
                }

                start = index + 1;
            }

            index = index + 1;
        }

        if (start < normalized.Length)
        {
            values[valueIndex] = normalized.Substring(start, normalized.Length - start);
        }

        return values;
    }

    internal static int CountPipeDelimitedValues(string text)
    {
        if (text == null || text.Length == 0)
        {
            return 0;
        }

        int count = 0;
        int start = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '|')
            {
                if (index > start)
                {
                    count = count + 1;
                }

                start = index + 1;
            }

            index = index + 1;
        }

        if (start < text.Length)
        {
            count = count + 1;
        }

        return count;
    }
}
