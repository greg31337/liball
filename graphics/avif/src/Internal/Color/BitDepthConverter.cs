using System;

namespace avif;

internal static class BitDepthConverter
{
    internal static AvifResultCode ConvertImage(
        AvifImage source,
        AvifImage destination,
        AvifDitherMode ditherMode)
    {
        AvifResultCode validation = ValidateImages(
            source, destination, ditherMode);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int index = (int)AvifPlaneKind.Y;
        while (index <= (int)AvifPlaneKind.Alpha)
        {
            AvifPlane? sourcePlane = source.GetPlane((AvifPlaneKind)index);
            if (sourcePlane != null)
            {
                AvifRange sourceRange = source.GetRange();
                AvifRange destinationRange = destination.GetRange();
                if (index == (int)AvifPlaneKind.Alpha)
                {
                    sourceRange = AvifRange.Full;
                    destinationRange = AvifRange.Full;
                }
                AvifResultCode result = ConvertPlane(
                    sourcePlane,
                    sourceRange,
                    destination.GetPlane((AvifPlaneKind)index),
                    destinationRange,
                    ditherMode);
                if (result != AvifResultCode.Success)
                {
                    return result;
                }
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    internal static AvifResultCode ConvertPlane(
        AvifPlane source,
        AvifRange sourceRange,
        AvifPlane? destination,
        AvifRange destinationRange,
        AvifDitherMode ditherMode)
    {
        AvifResultCode validation = ValidatePlanes(
            source,
            sourceRange,
            destination,
            destinationRange,
            ditherMode);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        AvifPlane input = source;
        AvifPlane? scratch = null;
        PlaneBuffer? sourceBuffer = source.GetInternalBuffer();
        PlaneBuffer? destinationBuffer = (destination ?? throw new InvalidOperationException("Invalid internal state.")).GetInternalBuffer();
        if ((sourceBuffer ?? throw new InvalidOperationException("Invalid internal state.")).Buffer == (destinationBuffer ?? throw new InvalidOperationException("Invalid internal state.")).Buffer
            && (sourceBuffer.Offset != destinationBuffer.Offset
                || sourceBuffer.Stride != destinationBuffer.Stride))
        {
            scratch = source.CreateCopy();
            if (scratch.GetCode() != AvifResultCode.Success)
            {
                return scratch.GetCode();
            }
            input = scratch;
        }

        AvifResultCode result;
        if (ditherMode == AvifDitherMode.ErrorDiffusion
            && destination.GetBitDepth() < input.GetBitDepth())
        {
            result = ConvertWithErrorDiffusion(
                input, sourceRange, destination, destinationRange);
        }
        else
        {
            result = ConvertRows(
                input,
                sourceRange,
                destination,
                destinationRange,
                ditherMode);
        }
        if (scratch != null)
        {
            scratch.Close();
        }
        return result;
    }

    static AvifResultCode ConvertRows(
        AvifPlane source,
        AvifRange sourceRange,
        AvifPlane destination,
        AvifRange destinationRange,
        AvifDitherMode ditherMode)
    {
        bool chroma = IsChroma(source.GetKind());
        int sourceMinimum = RangeConverter.GetMinimum(
            source.GetBitDepth(), sourceRange);
        int sourceMaximum = RangeConverter.GetMaximum(
            source.GetBitDepth(), sourceRange, chroma);
        int destinationMinimum = RangeConverter.GetMinimum(
            destination.GetBitDepth(), destinationRange);
        int destinationMaximum = RangeConverter.GetMaximum(
            destination.GetBitDepth(), destinationRange, chroma);
        int sourceSpan = sourceMaximum - sourceMinimum;
        int destinationSpan = destinationMaximum - destinationMinimum;
        bool ordered = ditherMode == AvifDitherMode.Ordered
            && destination.GetBitDepth() < source.GetBitDepth();

        int y = 0;
        while (y < source.GetHeight())
        {
            Span<byte> sourceRow = source.GetRowSpan(y);
            Span<byte> destinationRow = destination.GetRowSpan(y);
            int x = 0;
            while (x < source.GetWidth())
            {
                int sample = ReadSample(sourceRow, x, source.GetBitDepth());
                int converted;
                if (sample <= sourceMinimum)
                {
                    converted = destinationMinimum;
                }
                else if (sample >= sourceMaximum)
                {
                    converted = destinationMaximum;
                }
                else
                {
                    long numerator = (long)(sample - sourceMinimum)
                        * destinationSpan;
                    int relative = ordered
                        ? Dither.QuantizeOrdered(
                            numerator, sourceSpan, destinationSpan, x, y)
                        : Dither.QuantizeNearest(
                            numerator, sourceSpan, destinationSpan);
                    converted = destinationMinimum + relative;
                }
                WriteSample(
                    destinationRow, x, destination.GetBitDepth(), converted);
                x++;
            }
            y++;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ConvertWithErrorDiffusion(
        AvifPlane source,
        AvifRange sourceRange,
        AvifPlane destination,
        AvifRange destinationRange)
    {
        int width = source.GetWidth();
        long errorBytes = ((long)width + 2) * 16;
        if (width > System.Array.MaxLength - 2
            || errorBytes > AvifSecurityLimits.DefaultMaxTemporaryBytes)
        {
            return AvifResultCode.LimitExceeded;
        }
        long[] currentErrors = new long[width + 2];
        long[] nextErrors = new long[width + 2];
        bool chroma = IsChroma(source.GetKind());
        int sourceMinimum = RangeConverter.GetMinimum(
            source.GetBitDepth(), sourceRange);
        int sourceMaximum = RangeConverter.GetMaximum(
            source.GetBitDepth(), sourceRange, chroma);
        int destinationMinimum = RangeConverter.GetMinimum(
            destination.GetBitDepth(), destinationRange);
        int destinationMaximum = RangeConverter.GetMaximum(
            destination.GetBitDepth(), destinationRange, chroma);
        int sourceSpan = sourceMaximum - sourceMinimum;
        int destinationSpan = destinationMaximum - destinationMinimum;

        int y = 0;
        while (y < source.GetHeight())
        {
            Span<byte> sourceRow = source.GetRowSpan(y);
            Span<byte> destinationRow = destination.GetRowSpan(y);
            int x = 0;
            while (x < width)
            {
                int sample = ReadSample(sourceRow, x, source.GetBitDepth());
                int relative;
                long quantizationError = 0;
                if (sample <= sourceMinimum)
                {
                    relative = 0;
                }
                else if (sample >= sourceMaximum)
                {
                    relative = destinationSpan;
                }
                else
                {
                    long numerator = (long)(sample - sourceMinimum)
                        * destinationSpan;
                    long adjusted = numerator + currentErrors[x + 1];
                    relative = Dither.QuantizeErrorDiffusion(
                        numerator,
                        currentErrors[x + 1],
                        sourceSpan,
                        destinationSpan);
                    quantizationError = adjusted
                        - (long)relative * sourceSpan;
                }
                WriteSample(
                    destinationRow,
                    x,
                    destination.GetBitDepth(),
                    destinationMinimum + relative);

                currentErrors[x + 2] += Dither.ScaleError(
                    quantizationError, 7);
                nextErrors[x] += Dither.ScaleError(
                    quantizationError, 3);
                nextErrors[x + 1] += Dither.ScaleError(
                    quantizationError, 5);
                nextErrors[x + 2] += Dither.ScaleError(
                    quantizationError, 1);
                x++;
            }

            long[] exchanged = currentErrors;
            currentErrors = nextErrors;
            nextErrors = exchanged;
            System.Array.Clear(nextErrors, 0, nextErrors.Length);
            y++;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidateImages(
        AvifImage source,
        AvifImage destination,
        AvifDitherMode ditherMode)
    {
        if (source == null || destination == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode destinationState = destination.GetCode();
        if (destinationState != AvifResultCode.Success)
        {
            return destinationState;
        }
        AvifResultCode sourceState = source.GetCode();
        if (sourceState != AvifResultCode.Success)
        {
            return sourceState;
        }
        if (source.GetWidth() != destination.GetWidth()
            || source.GetHeight() != destination.GetHeight()
            || source.GetChromaFormat() != destination.GetChromaFormat()
            || source.GetAlphaMode() != destination.GetAlphaMode()
            || !IsValidDitherMode(ditherMode))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidatePlanes(
        AvifPlane source,
        AvifRange sourceRange,
        AvifPlane? destination,
        AvifRange destinationRange,
        AvifDitherMode ditherMode)
    {
        if (source == null || destination == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode destinationState = destination.GetCode();
        if (destinationState != AvifResultCode.Success)
        {
            return destinationState;
        }
        AvifResultCode sourceState = source.GetCode();
        if (sourceState != AvifResultCode.Success)
        {
            return sourceState;
        }
        if (source.GetWidth() != destination.GetWidth()
            || source.GetHeight() != destination.GetHeight()
            || source.GetKind() != destination.GetKind()
            || !RangeConverter.IsValidBitDepth(source.GetBitDepth())
            || !RangeConverter.IsValidBitDepth(destination.GetBitDepth())
            || !RangeConverter.IsValidRange(sourceRange)
            || !RangeConverter.IsValidRange(destinationRange)
            || !IsValidDitherMode(ditherMode))
        {
            return AvifResultCode.InvalidArgument;
        }
        if (source.GetKind() == AvifPlaneKind.Alpha
            && (sourceRange != AvifRange.Full
                || destinationRange != AvifRange.Full))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static bool IsChroma(AvifPlaneKind kind)
    {
        return kind == AvifPlaneKind.U || kind == AvifPlaneKind.V;
    }

    static bool IsValidDitherMode(AvifDitherMode value)
    {
        return value == AvifDitherMode.None
            || value == AvifDitherMode.Ordered
            || value == AvifDitherMode.ErrorDiffusion;
    }

    static int ReadSample(Span<byte> row, int x, int bitDepth)
    {
        if (bitDepth == 8)
        {
            return row[x];
        }
        int offset = x * 2;
        return row[offset] | row[offset + 1] << 8;
    }

    static void WriteSample(
        Span<byte> row,
        int x,
        int bitDepth,
        int value)
    {
        if (bitDepth == 8)
        {
            row[x] = IntegerConversion.ToByte(value);
            return;
        }
        int offset = x * 2;
        row[offset] = IntegerConversion.ToByte(value);
        row[offset + 1] = IntegerConversion.ToByte(value >> 8);
    }
}
