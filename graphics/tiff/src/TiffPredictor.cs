using System;

namespace tiff;

internal class TiffPredictor
{
    internal static byte[] ApplyHorizontalInteger(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to apply horizontal predictor data.");
            return new byte[0];
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to apply horizontal predictor data.");
            return new byte[0];
        }

        byte[] bytes = TiffIfdEntry.CopyBytes(source);
        if (context.Info.Predictor != 2)
        {
            return bytes;
        }

        int rowCount = CalculateRowCount(context, diagnostics);
        int pixelCount = CalculatePredictorPixelCount(context, diagnostics);
        int samplesPerPixel = CalculatePredictorSamplesPerPixel(context, diagnostics);
        int sampleByteCount = CalculatePredictorSampleByteCount(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ApplyHorizontalIntegerRows(bytes, rowCount, pixelCount, samplesPerPixel, sampleByteCount, context.ByteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return bytes;
    }

    internal static byte[] ApplyFloatingPoint(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to apply floating-point predictor data.");
            return new byte[0];
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to apply floating-point predictor data.");
            return new byte[0];
        }

        byte[] bytes = TiffIfdEntry.CopyBytes(source);
        if (context.Info.Predictor != 3)
        {
            return bytes;
        }

        int rowCount = CalculateRowCount(context, diagnostics);
        int pixelCount = CalculatePredictorPixelCount(context, diagnostics);
        int samplesPerPixel = CalculatePredictorSamplesPerPixel(context, diagnostics);
        int sampleByteCount = CalculateFloatingPointSampleByteCount(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ApplyFloatingPointRows(bytes, rowCount, pixelCount, samplesPerPixel, sampleByteCount, context.ByteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return bytes;
    }

    internal static byte[] ReverseHorizontalInteger(TiffCodecContext context, byte[] source, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to reverse horizontal predictor data.");
            return new byte[0];
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to reverse horizontal predictor data.");
            return new byte[0];
        }

        byte[] bytes = TiffIfdEntry.CopyBytes(source);
        if (context.Info.Predictor != 2)
        {
            return bytes;
        }

        int rowCount = CalculateRowCount(context, diagnostics);
        int pixelCount = CalculatePredictorPixelCount(context, diagnostics);
        int samplesPerPixel = CalculatePredictorSamplesPerPixel(context, diagnostics);
        int sampleByteCount = CalculatePredictorSampleByteCount(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ReverseHorizontalIntegerRows(bytes, rowCount, pixelCount, samplesPerPixel, sampleByteCount, context.ByteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return bytes;
    }

    internal static byte[] ReverseFloatingPoint(TiffCodecContext context, byte[] source, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to reverse floating-point predictor data.");
            return new byte[0];
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to reverse floating-point predictor data.");
            return new byte[0];
        }

        byte[] bytes = TiffIfdEntry.CopyBytes(source);
        if (context.Info.Predictor != 3)
        {
            return bytes;
        }

        int rowCount = CalculateRowCount(context, diagnostics);
        int pixelCount = CalculatePredictorPixelCount(context, diagnostics);
        int samplesPerPixel = CalculatePredictorSamplesPerPixel(context, diagnostics);
        int sampleByteCount = CalculateFloatingPointSampleByteCount(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ReverseFloatingPointRows(bytes, rowCount, pixelCount, samplesPerPixel, sampleByteCount, context.ByteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return bytes;
    }

    internal static int CalculateRowCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        ulong rowCount = 0;
        if (context.IsTile)
        {
            rowCount = context.Info.TileLength;
        }
        else
        {
            rowCount = TiffStrileLayout.CalculateRowsInStrip(context.Info, context.SegmentIndex, diagnostics);
        }

        if (rowCount == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Predictor row count must be positive.");
            return 0;
        }

        if (rowCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Predictor row count exceeds the supported range.");
            return 0;
        }

        return (int)rowCount;
    }

    internal static int CalculatePredictorPixelCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        ulong pixelCount = context.Info.Width;
        if (context.IsTile)
        {
            pixelCount = context.Info.TileWidth;
        }

        if (pixelCount == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Predictor row width must be positive.");
            return 0;
        }

        if (pixelCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Predictor row width exceeds the supported range.");
            return 0;
        }

        return (int)pixelCount;
    }

    internal static int CalculatePredictorSamplesPerPixel(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context.Info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            return 1;
        }

        if (context.Info.SamplesPerPixel <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be positive for horizontal predictor data.", context.Info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        return context.Info.SamplesPerPixel;
    }

    internal static int CalculatePredictorSampleByteCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        int sampleIndex = CalculatePredictorSampleIndex(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        int bitsPerSample = GetBitsPerSample(context.Info, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (context.Info.PlanarConfiguration != TiffPlanarConfiguration.Separate)
        {
            ValidateContiguousBitsPerSample(context.Info, bitsPerSample, diagnostics);
        }

        ValidateIntegerSampleFormat(context.Info, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (bitsPerSample <= 0 || bitsPerSample % 8 != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Horizontal integer predictor requires byte-aligned positive BitsPerSample values.", context.Info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        return bitsPerSample / 8;
    }

    internal static int CalculateFloatingPointSampleByteCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        int sampleIndex = CalculatePredictorSampleIndex(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        int bitsPerSample = GetBitsPerSample(context.Info, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (context.Info.PlanarConfiguration != TiffPlanarConfiguration.Separate)
        {
            ValidateContiguousBitsPerSample(context.Info, bitsPerSample, diagnostics);
        }

        ValidateFloatingPointSampleFormat(context.Info, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (bitsPerSample != 16 && bitsPerSample != 24 && bitsPerSample != 32 && bitsPerSample != 64)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Floating-point predictor requires 16, 24, 32, or 64 BitsPerSample.", context.Info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        return bitsPerSample / 8;
    }

    internal static int CalculatePredictorSampleIndex(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context.Info.PlanarConfiguration != TiffPlanarConfiguration.Separate)
        {
            return 0;
        }

        ulong segmentsPerPlane = 0;
        if (context.IsTile)
        {
            segmentsPerPlane = TiffStrileLayout.CalculateTilesPerPlane(context.Info, diagnostics);
        }
        else
        {
            segmentsPerPlane = TiffStrileLayout.CalculateStripsPerPlane(context.Info, diagnostics);
        }

        if (segmentsPerPlane == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Predictor segment count per plane must be positive.");
            return 0;
        }

        ulong sampleIndex = context.SegmentIndex / segmentsPerPlane;
        if (sampleIndex > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Predictor sample index exceeds the supported range.");
            return 0;
        }

        return (int)sampleIndex;
    }

    internal static int GetBitsPerSample(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample is required for horizontal predictor data.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        if (info.BitsPerSample.Length == 1)
        {
            return info.BitsPerSample[0];
        }

        if (sampleIndex < 0 || sampleIndex >= info.BitsPerSample.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample does not contain the predictor sample.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        return info.BitsPerSample[sampleIndex];
    }

    internal static void ValidateContiguousBitsPerSample(TiffImageInfo info, int bitsPerSample, TiffDiagnostics diagnostics)
    {
        int sampleIndex = 0;
        while (sampleIndex < info.SamplesPerPixel)
        {
            int sampleBits = GetBitsPerSample(info, sampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            if (sampleBits != bitsPerSample)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Horizontal predictor requires equal BitsPerSample values in contiguous planar data.", info.ImageIndex, TiffTagId.BitsPerSample);
                return;
            }

            sampleIndex = sampleIndex + 1;
        }
    }

    internal static void ValidateIntegerSampleFormat(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        TiffSampleFormat sampleFormat = GetSampleFormat(info, sampleIndex);
        if (sampleFormat == TiffSampleFormat.IeeeFloatingPoint || sampleFormat == TiffSampleFormat.ComplexIeeeFloatingPoint)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Horizontal integer predictor cannot be used with floating-point sample formats.", info.ImageIndex, TiffTagId.SampleFormat);
        }
    }

    internal static void ValidateFloatingPointSampleFormat(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            if (GetSampleFormat(info, sampleIndex) != TiffSampleFormat.IeeeFloatingPoint)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Floating-point predictor requires IEEE floating-point sample formats.", info.ImageIndex, TiffTagId.SampleFormat);
            }

            return;
        }

        int index = 0;
        while (index < info.SamplesPerPixel)
        {
            if (GetSampleFormat(info, index) != TiffSampleFormat.IeeeFloatingPoint)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Floating-point predictor requires IEEE floating-point sample formats.", info.ImageIndex, TiffTagId.SampleFormat);
                return;
            }

            index = index + 1;
        }
    }

    internal static TiffSampleFormat GetSampleFormat(TiffImageInfo info, int sampleIndex)
    {
        if (info.SampleFormats == null || info.SampleFormats.Length == 0)
        {
            return TiffSampleFormat.UnsignedInteger;
        }

        if (info.SampleFormats.Length == 1)
        {
            return info.SampleFormats[0];
        }

        if (sampleIndex < 0 || sampleIndex >= info.SampleFormats.Length)
        {
            return TiffSampleFormat.Unknown;
        }

        return info.SampleFormats[sampleIndex];
    }

    internal static void ReverseHorizontalIntegerRows(byte[] bytes, int rowCount, int pixelCount, int samplesPerPixel, int sampleByteCount, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateHorizontalIntegerRowLayout(bytes, rowCount, pixelCount, samplesPerPixel, sampleByteCount, byteOrder, diagnostics))
        {
            return;
        }

        int pixelByteCount = samplesPerPixel * sampleByteCount;
        int rowByteCount = pixelCount * pixelByteCount;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            int rowStart = rowIndex * rowByteCount;
            ReverseHorizontalIntegerRow(bytes, rowStart, pixelCount, samplesPerPixel, sampleByteCount, byteOrder);
            rowIndex = rowIndex + 1;
        }
    }

    internal static void ApplyHorizontalIntegerRows(byte[] bytes, int rowCount, int pixelCount, int samplesPerPixel, int sampleByteCount, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateHorizontalIntegerRowLayout(bytes, rowCount, pixelCount, samplesPerPixel, sampleByteCount, byteOrder, diagnostics))
        {
            return;
        }

        int pixelByteCount = samplesPerPixel * sampleByteCount;
        int rowByteCount = pixelCount * pixelByteCount;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            int rowStart = rowIndex * rowByteCount;
            ApplyHorizontalIntegerRow(bytes, rowStart, pixelCount, samplesPerPixel, sampleByteCount, byteOrder);
            rowIndex = rowIndex + 1;
        }
    }

    internal static bool ValidateHorizontalIntegerRowLayout(byte[] bytes, int rowCount, int pixelCount, int samplesPerPixel, int sampleByteCount, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (rowCount <= 0 || pixelCount <= 0 || samplesPerPixel <= 0 || sampleByteCount <= 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Predictor row layout values must be positive.");
            return false;
        }

        long pixelByteCountValue = (long)samplesPerPixel * (long)sampleByteCount;
        long rowByteCountValue = (long)pixelCount * pixelByteCountValue;
        long expectedByteCount = rowByteCountValue * (long)rowCount;
        if (pixelByteCountValue > int.MaxValue || rowByteCountValue > int.MaxValue || expectedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Predictor row layout exceeds the supported range.");
            return false;
        }

        int rowByteCount = (int)rowByteCountValue;
        if (rowByteCount <= 0 || bytes.Length != expectedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Predictor decoded byte count does not match the expected row layout.");
            return false;
        }

        if (sampleByteCount > 1 && byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "A known byte order is required for multi-byte horizontal predictor samples.");
            return false;
        }

        return true;
    }

    internal static void ApplyHorizontalIntegerRow(byte[] bytes, int rowStart, int pixelCount, int samplesPerPixel, int sampleByteCount, TiffByteOrder byteOrder)
    {
        int pixelByteCount = samplesPerPixel * sampleByteCount;
        int column = pixelCount - 1;
        while (column >= 1)
        {
            int sampleIndex = 0;
            while (sampleIndex < samplesPerPixel)
            {
                int currentOffset = rowStart + column * pixelByteCount + sampleIndex * sampleByteCount;
                int previousOffset = currentOffset - pixelByteCount;
                SubtractPreviousSample(bytes, currentOffset, previousOffset, sampleByteCount, byteOrder);
                sampleIndex = sampleIndex + 1;
            }

            column = column - 1;
        }
    }

    internal static void ApplyFloatingPointRows(byte[] bytes, int rowCount, int pixelCount, int samplesPerPixel, int sampleByteCount, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateHorizontalIntegerRowLayout(bytes, rowCount, pixelCount, samplesPerPixel, sampleByteCount, byteOrder, diagnostics))
        {
            return;
        }

        int rowByteCount = pixelCount * samplesPerPixel * sampleByteCount;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            int rowStart = rowIndex * rowByteCount;
            ShuffleFloatingPointRow(bytes, rowStart, rowByteCount, sampleByteCount, byteOrder);
            DifferenceFloatingPointRow(bytes, rowStart, rowByteCount, samplesPerPixel);
            rowIndex = rowIndex + 1;
        }
    }

    internal static void ReverseFloatingPointRows(byte[] bytes, int rowCount, int pixelCount, int samplesPerPixel, int sampleByteCount, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateHorizontalIntegerRowLayout(bytes, rowCount, pixelCount, samplesPerPixel, sampleByteCount, byteOrder, diagnostics))
        {
            return;
        }

        int rowByteCount = pixelCount * samplesPerPixel * sampleByteCount;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            int rowStart = rowIndex * rowByteCount;
            AccumulateFloatingPointRow(bytes, rowStart, rowByteCount, samplesPerPixel);
            UnshuffleFloatingPointRow(bytes, rowStart, rowByteCount, sampleByteCount, byteOrder);
            rowIndex = rowIndex + 1;
        }
    }

    internal static void ShuffleFloatingPointRow(byte[] bytes, int rowStart, int rowByteCount, int sampleByteCount, TiffByteOrder byteOrder)
    {
        byte[] shuffled = new byte[rowByteCount];
        int sampleCount = rowByteCount / sampleByteCount;
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            int byteIndex = 0;
            while (byteIndex < sampleByteCount)
            {
                int planeIndex = CalculateFloatingPointPlaneIndex(byteIndex, sampleByteCount, byteOrder);
                shuffled[planeIndex * sampleCount + sampleIndex] = bytes[rowStart + sampleIndex * sampleByteCount + byteIndex];
                byteIndex = byteIndex + 1;
            }

            sampleIndex = sampleIndex + 1;
        }

        CopyRowBytes(shuffled, bytes, rowStart);
    }

    internal static void UnshuffleFloatingPointRow(byte[] bytes, int rowStart, int rowByteCount, int sampleByteCount, TiffByteOrder byteOrder)
    {
        byte[] shuffled = new byte[rowByteCount];
        int index = 0;
        while (index < rowByteCount)
        {
            shuffled[index] = bytes[rowStart + index];
            index = index + 1;
        }

        int sampleCount = rowByteCount / sampleByteCount;
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            int byteIndex = 0;
            while (byteIndex < sampleByteCount)
            {
                int planeIndex = CalculateFloatingPointPlaneIndex(byteIndex, sampleByteCount, byteOrder);
                bytes[rowStart + sampleIndex * sampleByteCount + byteIndex] = shuffled[planeIndex * sampleCount + sampleIndex];
                byteIndex = byteIndex + 1;
            }

            sampleIndex = sampleIndex + 1;
        }
    }

    internal static int CalculateFloatingPointPlaneIndex(int byteIndex, int sampleByteCount, TiffByteOrder byteOrder)
    {
        if (byteOrder == TiffByteOrder.BigEndian)
        {
            return byteIndex;
        }

        return sampleByteCount - byteIndex - 1;
    }

    internal static void CopyRowBytes(byte[] source, byte[] destination, int destinationIndex)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[destinationIndex + index] = source[index];
            index = index + 1;
        }
    }

    internal static void AccumulateFloatingPointRow(byte[] bytes, int rowStart, int rowByteCount, int stride)
    {
        int index = stride;
        while (index < rowByteCount)
        {
            bytes[rowStart + index] = (byte)((bytes[rowStart + index] + bytes[rowStart + index - stride]) % 256);
            index = index + 1;
        }
    }

    internal static void DifferenceFloatingPointRow(byte[] bytes, int rowStart, int rowByteCount, int stride)
    {
        int index = rowByteCount - 1;
        while (index >= stride)
        {
            bytes[rowStart + index] = (byte)((bytes[rowStart + index] - bytes[rowStart + index - stride] + 256) % 256);
            index = index - 1;
        }
    }

    internal static void ReverseHorizontalIntegerRow(byte[] bytes, int rowStart, int pixelCount, int samplesPerPixel, int sampleByteCount, TiffByteOrder byteOrder)
    {
        int pixelByteCount = samplesPerPixel * sampleByteCount;
        int column = 1;
        while (column < pixelCount)
        {
            int sampleIndex = 0;
            while (sampleIndex < samplesPerPixel)
            {
                int currentOffset = rowStart + column * pixelByteCount + sampleIndex * sampleByteCount;
                int previousOffset = currentOffset - pixelByteCount;
                AddPreviousSample(bytes, currentOffset, previousOffset, sampleByteCount, byteOrder);
                sampleIndex = sampleIndex + 1;
            }

            column = column + 1;
        }
    }

    internal static void AddPreviousSample(byte[] bytes, int currentOffset, int previousOffset, int sampleByteCount, TiffByteOrder byteOrder)
    {
        if (sampleByteCount == 1 || byteOrder == TiffByteOrder.LittleEndian)
        {
            AddPreviousLittleEndianSample(bytes, currentOffset, previousOffset, sampleByteCount);
            return;
        }

        AddPreviousBigEndianSample(bytes, currentOffset, previousOffset, sampleByteCount);
    }

    internal static void SubtractPreviousSample(byte[] bytes, int currentOffset, int previousOffset, int sampleByteCount, TiffByteOrder byteOrder)
    {
        if (sampleByteCount == 1 || byteOrder == TiffByteOrder.LittleEndian)
        {
            SubtractPreviousLittleEndianSample(bytes, currentOffset, previousOffset, sampleByteCount);
            return;
        }

        SubtractPreviousBigEndianSample(bytes, currentOffset, previousOffset, sampleByteCount);
    }

    internal static void AddPreviousLittleEndianSample(byte[] bytes, int currentOffset, int previousOffset, int sampleByteCount)
    {
        int carry = 0;
        int index = 0;
        while (index < sampleByteCount)
        {
            int sum = bytes[currentOffset + index] + bytes[previousOffset + index] + carry;
            bytes[currentOffset + index] = (byte)(sum % 256);
            carry = sum / 256;
            index = index + 1;
        }
    }

    internal static void AddPreviousBigEndianSample(byte[] bytes, int currentOffset, int previousOffset, int sampleByteCount)
    {
        int carry = 0;
        int index = sampleByteCount - 1;
        while (index >= 0)
        {
            int sum = bytes[currentOffset + index] + bytes[previousOffset + index] + carry;
            bytes[currentOffset + index] = (byte)(sum % 256);
            carry = sum / 256;
            index = index - 1;
        }
    }

    internal static void SubtractPreviousLittleEndianSample(byte[] bytes, int currentOffset, int previousOffset, int sampleByteCount)
    {
        int borrow = 0;
        int index = 0;
        while (index < sampleByteCount)
        {
            int difference = bytes[currentOffset + index] - bytes[previousOffset + index] - borrow;
            if (difference < 0)
            {
                difference = difference + 256;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            bytes[currentOffset + index] = (byte)difference;
            index = index + 1;
        }
    }

    internal static void SubtractPreviousBigEndianSample(byte[] bytes, int currentOffset, int previousOffset, int sampleByteCount)
    {
        int borrow = 0;
        int index = sampleByteCount - 1;
        while (index >= 0)
        {
            int difference = bytes[currentOffset + index] - bytes[previousOffset + index] - borrow;
            if (difference < 0)
            {
                difference = difference + 256;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            bytes[currentOffset + index] = (byte)difference;
            index = index - 1;
        }
    }
}
