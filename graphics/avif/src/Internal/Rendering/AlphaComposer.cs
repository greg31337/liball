using System;

namespace avif;

internal static class AlphaComposer
{
    internal static AvifResultCode AttachAlpha(
        AvifImage target,
        AvifImage auxiliary,
        bool premultiplied,
        AvifResamplingFilter filter)
    {
        if (target == null || auxiliary == null || target == auxiliary)
        {
            return AvifResultCode.InvalidArgument;
        }

        AvifResultCode targetCode = target.GetCode();
        if (targetCode != AvifResultCode.Success)
        {
            return targetCode;
        }
        AvifResultCode auxiliaryCode = auxiliary.GetCode();
        if (auxiliaryCode != AvifResultCode.Success)
        {
            return auxiliaryCode;
        }
        if (auxiliary.GetChromaFormat() != AvifChromaFormat.Monochrome
            || auxiliary.GetBitDepth() != target.GetBitDepth()
            || !IsValidFilter(filter))
        {
            return AvifResultCode.InvalidArgument;
        }

        AvifPlane alpha = AvifPlane.Create(
            target.GetWidth(),
            target.GetHeight(),
            target.GetBitDepth(),
            AvifPlaneKind.Alpha,
            target.GetAlignmentValue(),
            (target.GetLimitsValue() ?? throw new InvalidOperationException("Invalid internal state.")).MaxDecodedBytes);
        if (alpha.GetCode() != AvifResultCode.Success)
        {
            return alpha.GetCode();
        }

        AvifResultCode resize = ResizePlane(
            auxiliary.GetPlane(AvifPlaneKind.Y),
            alpha,
            filter,
            (target.GetLimitsValue() ?? throw new InvalidOperationException("Invalid internal state.")).MaxTemporaryBytes);
        if (resize != AvifResultCode.Success)
        {
            alpha.Close();
            return resize;
        }

        target.ReplaceAlphaPlane(
            alpha,
            premultiplied
                ? AvifAlphaMode.Premultiplied
                : AvifAlphaMode.Straight);
        return AvifResultCode.Success;
    }

    internal static AvifPixelBuffer ConvertAlphaMode(
        AvifPixelBuffer source,
        bool sourcePremultiplied,
        bool outputPremultiplied)
    {
        if (source == null)
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }
        AvifResultCode sourceCode = source.GetCode();
        if (sourceCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(sourceCode);
        }
        if (!source.HasAlpha())
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }

        AvifPixelBuffer output = CreateCompatibleOutput(source);
        if (output.GetCode() != AvifResultCode.Success)
        {
            return output;
        }

        PackedDitherState dither = new PackedDitherState(
            AvifDitherMode.None,
            source.GetWidth());
        int y = 0;
        while (y < source.GetHeight())
        {
            Span<byte> sourceRow = source.GetRowSpan(y);
            Span<byte> outputRow = output.GetRowSpan(y);
            int x = 0;
            while (x < source.GetWidth())
            {
                PackedColorSample sample = PackedPixelUtility.Read(
                    source,
                    sourceRow,
                    x);
                ConvertSampleAlphaMode(
                    sample,
                    sourcePremultiplied,
                    outputPremultiplied);
                PackedPixelUtility.Write(
                    output,
                    outputRow,
                    x,
                    y,
                    sample,
                    dither,
                    false);
                x++;
            }
            dither.FinishRow();
            y++;
        }
        return output;
    }

    internal static AvifPixelBuffer CompositeOver(
        AvifPixelBuffer foreground,
        AvifPixelBuffer background,
        bool foregroundPremultiplied,
        bool backgroundPremultiplied,
        bool outputPremultiplied)
    {
        if (foreground == null || background == null)
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }
        AvifResultCode foregroundCode = foreground.GetCode();
        if (foregroundCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(foregroundCode);
        }
        AvifResultCode backgroundCode = background.GetCode();
        if (backgroundCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(backgroundCode);
        }
        if (!foreground.HasAlpha()
            || foreground.GetWidth() != background.GetWidth()
            || foreground.GetHeight() != background.GetHeight())
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }

        AvifPixelBuffer output = CreateCompatibleOutput(foreground);
        if (output.GetCode() != AvifResultCode.Success)
        {
            return output;
        }

        PackedDitherState dither = new PackedDitherState(
            AvifDitherMode.None,
            foreground.GetWidth());
        int y = 0;
        while (y < foreground.GetHeight())
        {
            Span<byte> foregroundRow = foreground.GetRowSpan(y);
            Span<byte> backgroundRow = background.GetRowSpan(y);
            Span<byte> outputRow = output.GetRowSpan(y);
            int x = 0;
            while (x < foreground.GetWidth())
            {
                PackedColorSample front = PackedPixelUtility.Read(
                    foreground,
                    foregroundRow,
                    x);
                PackedColorSample back = PackedPixelUtility.Read(
                    background,
                    backgroundRow,
                    x);
                PackedColorSample composed = CompositeSamples(
                    front,
                    back,
                    foregroundPremultiplied,
                    backgroundPremultiplied,
                    outputPremultiplied);
                PackedPixelUtility.Write(
                    output,
                    outputRow,
                    x,
                    y,
                    composed,
                    dither,
                    false);
                x++;
            }
            dither.FinishRow();
            y++;
        }
        return output;
    }

    static AvifPixelBuffer CreateCompatibleOutput(AvifPixelBuffer source)
    {
        return AvifPixelBuffer.Create(
            source.GetWidth(),
            source.GetHeight(),
            source.GetPixelFormat(),
            source.GetBitDepth(),
            source.IsFloatingPoint());
    }

    static void ConvertSampleAlphaMode(
        PackedColorSample sample,
        bool sourcePremultiplied,
        bool outputPremultiplied)
    {
        if (sourcePremultiplied == outputPremultiplied)
        {
            return;
        }
        if (outputPremultiplied)
        {
            sample.Red *= sample.Alpha;
            sample.Green *= sample.Alpha;
            sample.Blue *= sample.Alpha;
            return;
        }
        if (sample.Alpha <= 0)
        {
            sample.Red = 0;
            sample.Green = 0;
            sample.Blue = 0;
            return;
        }
        sample.Red = Clamp01(sample.Red / sample.Alpha);
        sample.Green = Clamp01(sample.Green / sample.Alpha);
        sample.Blue = Clamp01(sample.Blue / sample.Alpha);
    }

    static PackedColorSample CompositeSamples(
        PackedColorSample front,
        PackedColorSample back,
        bool foregroundPremultiplied,
        bool backgroundPremultiplied,
        bool outputPremultiplied)
    {
        double frontRed = foregroundPremultiplied
            ? front.Red
            : front.Red * front.Alpha;
        double frontGreen = foregroundPremultiplied
            ? front.Green
            : front.Green * front.Alpha;
        double frontBlue = foregroundPremultiplied
            ? front.Blue
            : front.Blue * front.Alpha;
        double backRed = backgroundPremultiplied
            ? back.Red
            : back.Red * back.Alpha;
        double backGreen = backgroundPremultiplied
            ? back.Green
            : back.Green * back.Alpha;
        double backBlue = backgroundPremultiplied
            ? back.Blue
            : back.Blue * back.Alpha;
        double inverseFrontAlpha = 1 - front.Alpha;
        double outputAlpha = front.Alpha + back.Alpha * inverseFrontAlpha;
        double outputRed = frontRed + backRed * inverseFrontAlpha;
        double outputGreen = frontGreen + backGreen * inverseFrontAlpha;
        double outputBlue = frontBlue + backBlue * inverseFrontAlpha;
        if (!outputPremultiplied && outputAlpha > 0)
        {
            outputRed /= outputAlpha;
            outputGreen /= outputAlpha;
            outputBlue /= outputAlpha;
        }
        return new PackedColorSample(
            Clamp01(outputRed),
            Clamp01(outputGreen),
            Clamp01(outputBlue),
            Clamp01(outputAlpha));
    }

    static AvifResultCode ResizePlane(
        AvifPlane? source,
        AvifPlane destination,
        AvifResamplingFilter filter,
        long maxTemporaryBytes)
    {
        if (source == null
            || destination == null
            || source.GetCode() != AvifResultCode.Success
            || destination.GetCode() != AvifResultCode.Success
            || source.GetBitDepth() != destination.GetBitDepth())
        {
            return AvifResultCode.InvalidArgument;
        }
        if (filter == AvifResamplingFilter.Nearest)
        {
            return ResizeNearest(source, destination);
        }

        long count = (long)destination.GetWidth() * source.GetHeight();
        if (count > System.Array.MaxLength
            || count * 8 > maxTemporaryBytes)
        {
            return AvifResultCode.LimitExceeded;
        }
        double[] horizontal = new double[(int)count];
        double scaleX = (double)source.GetWidth() / destination.GetWidth();
        double filterScaleX = System.Math.Max(1, scaleX);
        int sourceY = 0;
        while (sourceY < source.GetHeight())
        {
            Span<byte> sourceRow = source.GetRowSpan(sourceY);
            int destinationX = 0;
            while (destinationX < destination.GetWidth())
            {
                double center = (destinationX + 0.5) * scaleX - 0.5;
                horizontal[sourceY * destination.GetWidth() + destinationX] =
                    FilterRow(
                        sourceRow,
                        source.GetWidth(),
                        source.GetBitDepth(),
                        center,
                        filterScaleX,
                        filter);
                destinationX++;
            }
            sourceY++;
        }

        double scaleY = (double)source.GetHeight() / destination.GetHeight();
        double filterScaleY = System.Math.Max(1, scaleY);
        int maximum = (1 << destination.GetBitDepth()) - 1;
        int destinationY = 0;
        while (destinationY < destination.GetHeight())
        {
            Span<byte> destinationRow = destination.GetRowSpan(destinationY);
            double center = (destinationY + 0.5) * scaleY - 0.5;
            int destinationX = 0;
            while (destinationX < destination.GetWidth())
            {
                double value = FilterColumn(
                    horizontal,
                    destination.GetWidth(),
                    source.GetHeight(),
                    destinationX,
                    center,
                    filterScaleY,
                    filter);
                int rounded = (int)System.Math.Floor(value + 0.5);
                rounded = System.Math.Max(0, System.Math.Min(maximum, rounded));
                WriteSample(
                    destinationRow,
                    destinationX,
                    destination.GetBitDepth(),
                    rounded);
                destinationX++;
            }
            destinationY++;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ResizeNearest(
        AvifPlane source,
        AvifPlane destination)
    {
        double scaleX = (double)source.GetWidth() / destination.GetWidth();
        double scaleY = (double)source.GetHeight() / destination.GetHeight();
        int y = 0;
        while (y < destination.GetHeight())
        {
            int sourceY = ClampIndex(
                (int)System.Math.Floor((y + 0.5) * scaleY),
                source.GetHeight());
            Span<byte> sourceRow = source.GetRowSpan(sourceY);
            Span<byte> destinationRow = destination.GetRowSpan(y);
            int x = 0;
            while (x < destination.GetWidth())
            {
                int sourceX = ClampIndex(
                    (int)System.Math.Floor((x + 0.5) * scaleX),
                    source.GetWidth());
                WriteSample(
                    destinationRow,
                    x,
                    destination.GetBitDepth(),
                    ReadSample(sourceRow, sourceX, source.GetBitDepth()));
                x++;
            }
            y++;
        }
        return AvifResultCode.Success;
    }

    static double FilterRow(
        Span<byte> row,
        int width,
        int bitDepth,
        double center,
        double scale,
        AvifResamplingFilter filter)
    {
        double radius = GetRadius(filter) * scale;
        int start = (int)System.Math.Floor(center - radius);
        int end = (int)System.Math.Ceiling(center + radius);
        double sum = 0;
        double weightSum = 0;
        int index = start;
        while (index <= end)
        {
            double weight = GetWeight((center - index) / scale, filter);
            if (weight != 0)
            {
                sum += weight * ReadSample(
                    row,
                    ClampIndex(index, width),
                    bitDepth);
                weightSum += weight;
            }
            index++;
        }
        return weightSum == 0 ? 0 : sum / weightSum;
    }

    static double FilterColumn(
        double[] samples,
        int width,
        int height,
        int x,
        double center,
        double scale,
        AvifResamplingFilter filter)
    {
        double radius = GetRadius(filter) * scale;
        int start = (int)System.Math.Floor(center - radius);
        int end = (int)System.Math.Ceiling(center + radius);
        double sum = 0;
        double weightSum = 0;
        int index = start;
        while (index <= end)
        {
            double weight = GetWeight((center - index) / scale, filter);
            if (weight != 0)
            {
                sum += weight * samples[ClampIndex(index, height) * width + x];
                weightSum += weight;
            }
            index++;
        }
        return weightSum == 0 ? 0 : sum / weightSum;
    }

    static double GetRadius(AvifResamplingFilter filter)
    {
        return filter == AvifResamplingFilter.Bilinear
            ? 1
            : filter == AvifResamplingFilter.Bicubic ? 2 : 3;
    }

    static double GetWeight(
        double distance,
        AvifResamplingFilter filter)
    {
        double absolute = System.Math.Abs(distance);
        if (filter == AvifResamplingFilter.Bilinear)
        {
            return absolute < 1 ? 1 - absolute : 0;
        }
        if (filter == AvifResamplingFilter.Bicubic)
        {
            if (absolute < 1)
            {
                return 1.5 * absolute * absolute * absolute
                    - 2.5 * absolute * absolute + 1;
            }
            if (absolute < 2)
            {
                return -0.5 * absolute * absolute * absolute
                    + 2.5 * absolute * absolute
                    - 4 * absolute + 2;
            }
            return 0;
        }
        if (absolute == 0)
        {
            return 1;
        }
        if (absolute >= 3)
        {
            return 0;
        }
        double piDistance = System.Math.PI * distance;
        return System.Math.Sin(piDistance) / piDistance
            * System.Math.Sin(piDistance / 3) / (piDistance / 3);
    }

    static int ClampIndex(int value, int length)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value >= length)
        {
            return length - 1;
        }
        return value;
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

    static bool IsValidFilter(AvifResamplingFilter filter)
    {
        int value = (int)filter;
        return value >= (int)AvifResamplingFilter.Nearest
            && value <= (int)AvifResamplingFilter.Lanczos3;
    }

    static double Clamp01(double value)
    {
        return System.Math.Max(0, System.Math.Min(1, value));
    }
}
