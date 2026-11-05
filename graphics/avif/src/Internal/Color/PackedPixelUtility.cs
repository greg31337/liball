using System;

namespace avif;

internal class PackedColorSample
{
    internal double Red;
    internal double Green;
    internal double Blue;
    internal double Alpha;

    internal PackedColorSample(
        double red,
        double green,
        double blue,
        double alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }
}

internal class PackedDitherState
{
    static readonly int[] orderedMatrix = new int[]
    {
        0, 8, 2, 10,
        12, 4, 14, 6,
        3, 11, 1, 9,
        15, 7, 13, 5
    };

    AvifDitherMode mode;
    double[]? currentErrors;
    double[]? nextErrors;

    internal PackedDitherState(
        AvifDitherMode mode,
        int width)
    {
        this.mode = mode;
        if (mode == AvifDitherMode.ErrorDiffusion)
        {
            currentErrors = new double[(width + 2) * 3];
            nextErrors = new double[(width + 2) * 3];
        }
    }

    internal int Quantize(
        double value,
        int maximum,
        int x,
        int y,
        int channel,
        bool dither)
    {
        value = System.Math.Max(0, System.Math.Min(1, value));
        double scaled = value * maximum;
        if (!dither || mode == AvifDitherMode.None)
        {
            return (int)System.Math.Floor(scaled + 0.5);
        }
        if (mode == AvifDitherMode.Ordered)
        {
            double threshold = (orderedMatrix[
                (y & 3) * 4 + (x & 3)] + 0.5) / 16;
            int lower = (int)System.Math.Floor(scaled);
            return scaled - lower >= threshold
                ? System.Math.Min(maximum, lower + 1)
                : lower;
        }

        int index = ((x + 1) * 3) + channel;
        double adjusted = scaled + (currentErrors ?? throw new InvalidOperationException("Invalid internal state."))[index];
        int result = (int)System.Math.Floor(adjusted + 0.5);
        result = System.Math.Max(0, System.Math.Min(maximum, result));
        double error = adjusted - result;
        currentErrors[index + 3] += error * 7 / 16;
        (nextErrors ?? throw new InvalidOperationException("Invalid internal state."))[index - 3] += error * 3 / 16;
        nextErrors[index] += error * 5 / 16;
        nextErrors[index + 3] += error / 16;
        return result;
    }

    internal void FinishRow()
    {
        if (mode != AvifDitherMode.ErrorDiffusion)
        {
            return;
        }
        double[] exchanged = currentErrors
            ?? throw new InvalidOperationException("Invalid internal state.");
        currentErrors = nextErrors;
        nextErrors = exchanged;
        System.Array.Clear(nextErrors
            ?? throw new InvalidOperationException("Invalid internal state."));
    }
}

internal static class PackedPixelUtility
{
    internal static PackedColorSample Read(
        AvifPixelBuffer source,
        Span<byte> row,
        int x)
    {
        AvifPixelFormat format = source.GetPixelFormat();
        int offset = x * source.GetBytesPerPixel();
        if (source.IsFloatingPoint())
        {
            double red = ReadHalf(row, offset);
            double green = ReadHalf(row, offset + 2);
            double blue = ReadHalf(row, offset + 4);
            double alpha = format == AvifPixelFormat.Rgba
                ? ReadHalf(row, offset + 6) : 1;
            return new PackedColorSample(red, green, blue, alpha);
        }
        if (format == AvifPixelFormat.Rgb565)
        {
            int packed = row[offset] | row[offset + 1] << 8;
            return new PackedColorSample(
                ((packed >> 11) & 31) / 31.0,
                ((packed >> 5) & 63) / 63.0,
                (packed & 31) / 31.0,
                1);
        }

        int depth = source.GetBitDepth();
        int bytes = depth <= 8 ? 1 : 2;
        double maximum = (1 << depth) - 1;
        double first = ReadInteger(row, offset, bytes) / maximum;
        double second = source.GetChannelCount() > 1
            ? ReadInteger(row, offset + bytes, bytes) / maximum : 1;
        double third = source.GetChannelCount() > 2
            ? ReadInteger(row, offset + 2 * bytes, bytes) / maximum : 0;
        double fourth = source.GetChannelCount() > 3
            ? ReadInteger(row, offset + 3 * bytes, bytes) / maximum : 1;

        if (format == AvifPixelFormat.Gray)
        {
            return new PackedColorSample(first, first, first, 1);
        }
        if (format == AvifPixelFormat.GrayAlpha)
        {
            return new PackedColorSample(first, first, first, second);
        }
        if (format == AvifPixelFormat.Rgb)
        {
            return new PackedColorSample(first, second, third, 1);
        }
        if (format == AvifPixelFormat.Rgba)
        {
            return new PackedColorSample(first, second, third, fourth);
        }
        if (format == AvifPixelFormat.Bgr)
        {
            return new PackedColorSample(third, second, first, 1);
        }
        if (format == AvifPixelFormat.Bgra)
        {
            return new PackedColorSample(third, second, first, fourth);
        }
        if (format == AvifPixelFormat.Argb)
        {
            return new PackedColorSample(second, third, fourth, first);
        }
        return new PackedColorSample(fourth, third, second, first);
    }

    internal static void Write(
        AvifPixelBuffer destination,
        Span<byte> row,
        int x,
        int y,
        PackedColorSample sample,
        PackedDitherState dither,
        bool reduceDepth)
    {
        int offset = x * destination.GetBytesPerPixel();
        AvifPixelFormat format = destination.GetPixelFormat();
        if (destination.IsFloatingPoint())
        {
            WriteHalf(row, offset, sample.Red);
            WriteHalf(row, offset + 2, sample.Green);
            WriteHalf(row, offset + 4, sample.Blue);
            if (format == AvifPixelFormat.Rgba)
            {
                WriteHalf(row, offset + 6, sample.Alpha);
            }
            return;
        }

        if (format == AvifPixelFormat.Rgb565)
        {
            int red = dither.Quantize(
                sample.Red, 31, x, y, 0, reduceDepth);
            int green = dither.Quantize(
                sample.Green, 63, x, y, 1, reduceDepth);
            int blue = dither.Quantize(
                sample.Blue, 31, x, y, 2, reduceDepth);
            int packed = red << 11 | green << 5 | blue;
            row[offset] = IntegerConversion.ToByte(packed);
            row[offset + 1] = IntegerConversion.ToByte(packed >> 8);
            return;
        }

        int depth = destination.GetBitDepth();
        int bytes = depth <= 8 ? 1 : 2;
        int maximum = (1 << depth) - 1;
        if (format == AvifPixelFormat.Gray
            || format == AvifPixelFormat.GrayAlpha)
        {
            int grayValue = dither.Quantize(
                0.2126 * sample.Red + 0.7152 * sample.Green
                    + 0.0722 * sample.Blue,
                maximum, x, y, 0, reduceDepth);
            int grayAlphaValue = (int)System.Math.Floor(
                System.Math.Max(0, System.Math.Min(1, sample.Alpha))
                    * maximum + 0.5);
            WriteInteger(row, offset, bytes, grayValue);
            if (format == AvifPixelFormat.GrayAlpha)
            {
                WriteInteger(row, offset + bytes, bytes, grayAlphaValue);
            }
            return;
        }

        int redValue = dither.Quantize(
            sample.Red, maximum, x, y, 0, reduceDepth);
        int greenValue = dither.Quantize(
            sample.Green, maximum, x, y, 1, reduceDepth);
        int blueValue = dither.Quantize(
            sample.Blue, maximum, x, y, 2, reduceDepth);
        int alphaValue = (int)System.Math.Floor(
            System.Math.Max(0, System.Math.Min(1, sample.Alpha))
                * maximum + 0.5);

        if (format == AvifPixelFormat.Rgb
            || format == AvifPixelFormat.Rgba)
        {
            WriteInteger(row, offset, bytes, redValue);
            WriteInteger(row, offset + bytes, bytes, greenValue);
            WriteInteger(row, offset + 2 * bytes, bytes, blueValue);
            if (format == AvifPixelFormat.Rgba)
            {
                WriteInteger(row, offset + 3 * bytes, bytes, alphaValue);
            }
        }
        else if (format == AvifPixelFormat.Bgr
            || format == AvifPixelFormat.Bgra)
        {
            WriteInteger(row, offset, bytes, blueValue);
            WriteInteger(row, offset + bytes, bytes, greenValue);
            WriteInteger(row, offset + 2 * bytes, bytes, redValue);
            if (format == AvifPixelFormat.Bgra)
            {
                WriteInteger(row, offset + 3 * bytes, bytes, alphaValue);
            }
        }
        else if (format == AvifPixelFormat.Argb)
        {
            WriteInteger(row, offset, bytes, alphaValue);
            WriteInteger(row, offset + bytes, bytes, redValue);
            WriteInteger(row, offset + 2 * bytes, bytes, greenValue);
            WriteInteger(row, offset + 3 * bytes, bytes, blueValue);
        }
        else
        {
            WriteInteger(row, offset, bytes, alphaValue);
            WriteInteger(row, offset + bytes, bytes, blueValue);
            WriteInteger(row, offset + 2 * bytes, bytes, greenValue);
            WriteInteger(row, offset + 3 * bytes, bytes, redValue);
        }
    }

    static int ReadInteger(Span<byte> row, int offset, int bytes)
    {
        return bytes == 1
            ? row[offset]
            : row[offset] | row[offset + 1] << 8;
    }

    static void WriteInteger(
        Span<byte> row,
        int offset,
        int bytes,
        int value)
    {
        row[offset] = IntegerConversion.ToByte(value);
        if (bytes == 2)
        {
            row[offset + 1] = IntegerConversion.ToByte(value >> 8);
        }
    }

    static double ReadHalf(Span<byte> row, int offset)
    {
        ushort bits = (ushort)(row[offset] | row[offset + 1] << 8);
        return (double)System.BitConverter.UInt16BitsToHalf(bits);
    }

    static void WriteHalf(Span<byte> row, int offset, double value)
    {
        System.Half half = (System.Half)value;
        ushort bits = System.BitConverter.HalfToUInt16Bits(half);
        row[offset] = IntegerConversion.ToByte(bits);
        row[offset + 1] = IntegerConversion.ToByte(bits >> 8);
    }
}
