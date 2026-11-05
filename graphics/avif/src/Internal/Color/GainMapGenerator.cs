using System;

namespace avif;

internal class GainMapScan
{
    internal double[] BaseMinimum;
    internal double[] AlternateMinimum;
    internal double[] GainMinimum;
    internal double[] GainMaximum;
    internal double[] BaseOffset;
    internal double[] AlternateOffset;
    internal double BasePeak;
    internal double AlternatePeak;
    internal double BaseSum;
    internal double AlternateSum;
    internal bool Descending;
    internal double BaseHeadroom;
    internal double AlternateHeadroom;

    internal GainMapScan()
    {
        BaseMinimum = new double[] { 0, 0, 0 };
        AlternateMinimum = new double[] { 0, 0, 0 };
        GainMinimum = new double[]
        {
            double.PositiveInfinity,
            double.PositiveInfinity,
            double.PositiveInfinity
        };
        GainMaximum = new double[]
        {
            double.NegativeInfinity,
            double.NegativeInfinity,
            double.NegativeInfinity
        };
        BaseOffset = new double[] { 1.0 / 64, 1.0 / 64, 1.0 / 64 };
        AlternateOffset = new double[] { 1.0 / 64, 1.0 / 64, 1.0 / 64 };
    }
}

internal static class GainMapGenerator
{
    const double Epsilon = 1e-10;

    internal static AvifGainMap Generate(
        AvifImage? baseImage,
        AvifImage alternateImage,
        int gainMapWidth,
        int gainMapHeight,
        int gainMapBitDepth,
        AvifChromaFormat gainMapChromaFormat,
        AvifColorConversionOptions? options,
        AvifSecurityLimits limits)
    {
        AvifResultCode validation = Validate(
            baseImage,
            alternateImage,
            gainMapWidth,
            gainMapHeight,
            gainMapBitDepth,
            gainMapChromaFormat,
            options,
            limits);
        if (validation != AvifResultCode.Success)
        {
            return AvifGainMap.FromCode(validation);
        }

        AvifColorConversionOptions conversion = CopyOptions(options);
        conversion.UseEmbeddedIccProfile = false;
        conversion.ClampOutOfGamut = false;
        conversion.PreserveAlpha = false;
        conversion.OutputPremultipliedAlpha = false;
        AvifPixelBuffer baseLinear = ConvertToLinear(
            baseImage, conversion, limits);
        if (baseLinear.GetCode() != AvifResultCode.Success)
        {
            return AvifGainMap.Create(baseLinear.GetCode());
        }
        AvifPixelBuffer alternateLinear = ConvertToLinear(
            alternateImage, conversion, limits);
        if (alternateLinear.GetCode() != AvifResultCode.Success)
        {
            AvifResultCode code = alternateLinear.GetCode();
            baseLinear.Close();
            return AvifGainMap.FromCode(code);
        }

        ColorConversionContext baseContext = new ColorConversionContext(
            (baseImage ?? throw new InvalidOperationException("Invalid internal state.")).GetColorInfo());
        ColorConversionContext alternateContext = new ColorConversionContext(
            alternateImage.GetColorInfo());
        AvifColorPrimaries mathPrimaries = ChooseMathPrimaries(
            baseContext.Primaries, alternateContext.Primaries);
        if (!ColorTables.IsSupported(mathPrimaries))
        {
            baseLinear.Close();
            alternateLinear.Close();
            return AvifGainMap.FromCode(AvifResultCode.UnsupportedFeature);
        }
        ColorMatrix? toMath = ColorTables.GetPrimariesTransform(
            AvifColorPrimaries.Bt709, mathPrimaries);
        if (toMath == null)
        {
            baseLinear.Close();
            alternateLinear.Close();
            return AvifGainMap.FromCode(AvifResultCode.UnsupportedFeature);
        }

        bool multichannel = gainMapChromaFormat != AvifChromaFormat.Monochrome;
        GainMapScan? scan = Scan(
            baseLinear,
            alternateLinear,
            toMath,
            mathPrimaries,
            multichannel);
        if (scan == null)
        {
            baseLinear.Close();
            alternateLinear.Close();
            return AvifGainMap.FromCode(AvifResultCode.EncodeFailed);
        }

        AvifPixelBuffer encoded = EncodePixels(
            baseLinear,
            alternateLinear,
            gainMapWidth,
            gainMapHeight,
            toMath,
            mathPrimaries,
            multichannel,
            scan,
            limits);
        baseLinear.Close();
        alternateLinear.Close();
        if (encoded.GetCode() != AvifResultCode.Success)
        {
            return AvifGainMap.Create(encoded.GetCode());
        }

        double[] gamma = new double[] { 1, 1, 1 };
        byte[] metadataBytes = GainMapMetadataWriter.Write(
            multichannel,
            mathPrimaries == baseContext.Primaries,
            scan.BaseHeadroom,
            scan.AlternateHeadroom,
            scan.GainMinimum,
            scan.GainMaximum,
            gamma,
            scan.BaseOffset,
            scan.AlternateOffset);
        AvifGainMapMetadata metadata = new AvifGainMapMetadata(metadataBytes);
        if (metadata.GetCode() != AvifResultCode.Success)
        {
            encoded.Close();
            return AvifGainMap.Create(metadata.GetCode());
        }

        AvifImage gainImage = CreateGainImage(
            encoded,
            gainMapWidth,
            gainMapHeight,
            gainMapBitDepth,
            gainMapChromaFormat,
            conversion,
            limits);
        encoded.Close();
        if (gainImage.GetCode() != AvifResultCode.Success)
        {
            return AvifGainMap.Create(gainImage.GetCode());
        }
        return AvifGainMap.Create(
            gainImage,
            metadata,
            alternateImage.GetColorInfo());
    }

    static AvifResultCode Validate(
        AvifImage? baseImage,
        AvifImage alternateImage,
        int gainMapWidth,
        int gainMapHeight,
        int gainMapBitDepth,
        AvifChromaFormat gainMapChromaFormat,
        AvifColorConversionOptions? options,
        AvifSecurityLimits limits)
    {
        if (baseImage == null || alternateImage == null || options == null
            || limits == null || gainMapWidth <= 0 || gainMapHeight <= 0
            || (gainMapBitDepth != 8 && gainMapBitDepth != 10
                && gainMapBitDepth != 12 && gainMapBitDepth != 16)
            || (gainMapChromaFormat != AvifChromaFormat.Monochrome
                && gainMapChromaFormat != AvifChromaFormat.Yuv420
                && gainMapChromaFormat != AvifChromaFormat.Yuv422
                && gainMapChromaFormat != AvifChromaFormat.Yuv444))
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode baseCode = baseImage.GetCode();
        if (baseCode != AvifResultCode.Success)
        {
            return baseCode;
        }
        AvifResultCode alternateCode = alternateImage.GetCode();
        if (alternateCode != AvifResultCode.Success)
        {
            return alternateCode;
        }
        if (baseImage.GetWidth() != alternateImage.GetWidth()
            || baseImage.GetHeight() != alternateImage.GetHeight())
        {
            return AvifResultCode.InvalidArgument;
        }
        if (baseImage.GetIccProfile() != null
            || alternateImage.GetIccProfile() != null)
        {
            return AvifResultCode.UnsupportedFeature;
        }
        if (ResourceGuard.ValidateLimits(limits) != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (baseImage.GetWidth() > limits.MaxWidth
            || baseImage.GetHeight() > limits.MaxHeight
            || gainMapWidth > limits.MaxWidth
            || gainMapHeight > limits.MaxHeight
            || (long)baseImage.GetWidth() * baseImage.GetHeight()
                > limits.MaxPixels
            || (long)gainMapWidth * gainMapHeight > limits.MaxPixels)
        {
            return AvifResultCode.LimitExceeded;
        }
        long temporaryBytes = (long)baseImage.GetWidth()
            * baseImage.GetHeight() * 16
            + (long)gainMapWidth * gainMapHeight * 6;
        if (temporaryBytes > limits.MaxTemporaryBytes)
        {
            return AvifResultCode.LimitExceeded;
        }
        AvifColorConverter converter = AvifColorConverter.Create();
        return converter.SetOptions(CopyOptions(options));
    }

    static AvifPixelBuffer ConvertToLinear(
        AvifImage? image,
        AvifColorConversionOptions options,
        AvifSecurityLimits limits)
    {
        AvifPixelBuffer output = AvifPixelBuffer.Create(
            (image ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(),
            image.GetHeight(),
            AvifPixelFormat.Rgba,
            16,
            true,
            1,
            limits);
        if (output.GetCode() != AvifResultCode.Success)
        {
            return output;
        }
        AvifResultCode code = AvifColorConverter.Create(options).ConvertToPixelBuffer(
            image, output);
        if (code != AvifResultCode.Success)
        {
            output.Close();
            return AvifPixelBuffer.FromCode(code);
        }
        return output;
    }

    static AvifColorPrimaries ChooseMathPrimaries(
        AvifColorPrimaries basePrimaries,
        AvifColorPrimaries alternatePrimaries)
    {
        if (basePrimaries == alternatePrimaries)
        {
            return basePrimaries;
        }
        ColorMatrix? baseToAlternate = ColorTables.GetPrimariesTransform(
            basePrimaries, alternatePrimaries);
        ColorMatrix? alternateToBase = ColorTables.GetPrimariesTransform(
            alternatePrimaries, basePrimaries);
        if (baseToAlternate == null || alternateToBase == null)
        {
            return AvifColorPrimaries.Unspecified;
        }
        double baseMinimum = GetBasisMinimum(alternateToBase);
        double alternateMinimum = GetBasisMinimum(baseToAlternate);
        return alternateMinimum <= baseMinimum
            ? basePrimaries : alternatePrimaries;
    }

    static double GetBasisMinimum(ColorMatrix transform)
    {
        double minimum = 0;
        int channel = 0;
        while (channel < 3)
        {
            ColorVector basis = new ColorVector(
                channel == 0 ? 1 : 0,
                channel == 1 ? 1 : 0,
                channel == 2 ? 1 : 0);
            ColorVector converted = transform.Transform(basis);
            minimum = System.Math.Min(minimum, converted.First);
            minimum = System.Math.Min(minimum, converted.Second);
            minimum = System.Math.Min(minimum, converted.Third);
            channel++;
        }
        return minimum;
    }

    static GainMapScan? Scan(
        AvifPixelBuffer baseLinear,
        AvifPixelBuffer alternateLinear,
        ColorMatrix toMath,
        AvifColorPrimaries mathPrimaries,
        bool multichannel)
    {
        GainMapScan scan = new GainMapScan();
        int y = 0;
        while (y < baseLinear.GetHeight())
        {
            int x = 0;
            while (x < baseLinear.GetWidth())
            {
                ColorVector baseValue = ReadMathPixel(
                    baseLinear, x, y, toMath);
                ColorVector alternateValue = ReadMathPixel(
                    alternateLinear, x, y, toMath);
                Accumulate(scan, baseValue, alternateValue);
                x++;
            }
            y++;
        }
        SetOffsets(scan);
        FindGainRange(
            scan,
            baseLinear,
            alternateLinear,
            toMath,
            mathPrimaries,
            multichannel);
        SetHeadrooms(scan);
        if (scan.Descending)
        {
            int channel = 0;
            while (channel < 3)
            {
                double oldMinimum = scan.GainMinimum[channel];
                scan.GainMinimum[channel] = -scan.GainMaximum[channel];
                scan.GainMaximum[channel] = -oldMinimum;
                channel++;
            }
        }
        return HasFiniteRange(scan) ? scan : null;
    }

    static void Accumulate(
        GainMapScan scan,
        ColorVector baseValue,
        ColorVector alternateValue)
    {
        double[] baseChannels = new double[]
        {
            baseValue.First, baseValue.Second, baseValue.Third
        };
        double[] alternateChannels = new double[]
        {
            alternateValue.First,
            alternateValue.Second,
            alternateValue.Third
        };
        double baseMaximum = baseChannels[0];
        double alternateMaximum = alternateChannels[0];
        int channel = 0;
        while (channel < 3)
        {
            scan.BaseMinimum[channel] = System.Math.Min(
                scan.BaseMinimum[channel], baseChannels[channel]);
            scan.AlternateMinimum[channel] = System.Math.Min(
                scan.AlternateMinimum[channel], alternateChannels[channel]);
            baseMaximum = System.Math.Max(baseMaximum, baseChannels[channel]);
            alternateMaximum = System.Math.Max(
                alternateMaximum, alternateChannels[channel]);
            channel++;
        }
        scan.BasePeak = System.Math.Max(scan.BasePeak, baseMaximum);
        scan.AlternatePeak = System.Math.Max(
            scan.AlternatePeak, alternateMaximum);
        scan.BaseSum += baseMaximum;
        scan.AlternateSum += alternateMaximum;
    }

    static void SetOffsets(GainMapScan scan)
    {
        int channel = 0;
        while (channel < 3)
        {
            if (scan.BaseMinimum[channel] < -Epsilon)
            {
                scan.BaseOffset[channel] = System.Math.Min(
                    0.1,
                    scan.BaseOffset[channel] - scan.BaseMinimum[channel]);
            }
            if (scan.AlternateMinimum[channel] < -Epsilon)
            {
                scan.AlternateOffset[channel] = System.Math.Min(
                    0.1,
                    scan.AlternateOffset[channel]
                        - scan.AlternateMinimum[channel]);
            }
            channel++;
        }
    }

    static void FindGainRange(
        GainMapScan scan,
        AvifPixelBuffer baseLinear,
        AvifPixelBuffer alternateLinear,
        ColorMatrix toMath,
        AvifColorPrimaries mathPrimaries,
        bool multichannel)
    {
        ColorVector luma = GetLumaCoefficients(mathPrimaries);
        int y = 0;
        while (y < baseLinear.GetHeight())
        {
            int x = 0;
            while (x < baseLinear.GetWidth())
            {
                ColorVector baseValue = ReadMathPixel(
                    baseLinear, x, y, toMath);
                ColorVector alternateValue = ReadMathPixel(
                    alternateLinear, x, y, toMath);
                int count = multichannel ? 3 : 1;
                int channel = 0;
                while (channel < count)
                {
                    double raw = ComputeRawGain(
                        baseValue,
                        alternateValue,
                        channel,
                        multichannel,
                        luma,
                        scan);
                    scan.GainMinimum[channel] = System.Math.Min(
                        scan.GainMinimum[channel], raw);
                    scan.GainMaximum[channel] = System.Math.Max(
                        scan.GainMaximum[channel], raw);
                    channel++;
                }
                x++;
            }
            y++;
        }
        if (!multichannel)
        {
            int channel = 1;
            while (channel < 3)
            {
                scan.GainMinimum[channel] = scan.GainMinimum[0];
                scan.GainMaximum[channel] = scan.GainMaximum[0];
                scan.BaseOffset[channel] = scan.BaseOffset[0];
                scan.AlternateOffset[channel] = scan.AlternateOffset[0];
                channel++;
            }
        }
    }

    static void SetHeadrooms(GainMapScan scan)
    {
        double peakDifference = scan.AlternatePeak - scan.BasePeak;
        scan.Descending = peakDifference < -Epsilon
            || (System.Math.Abs(peakDifference) <= Epsilon
                && scan.AlternateSum < scan.BaseSum);
        double larger = scan.Descending ? scan.BasePeak : scan.AlternatePeak;
        double smaller = scan.Descending ? scan.AlternatePeak : scan.BasePeak;
        double difference = System.Math.Log(
            System.Math.Max(larger, Epsilon)
                / System.Math.Max(smaller, Epsilon),
            2);
        difference = System.Math.Abs(difference);
        if (difference < 0.000001)
        {
            int channel = 0;
            while (channel < 3)
            {
                difference = System.Math.Max(
                    difference,
                    System.Math.Abs(scan.GainMinimum[channel]));
                difference = System.Math.Max(
                    difference,
                    System.Math.Abs(scan.GainMaximum[channel]));
                channel++;
            }
        }
        if (difference < 0.000001)
        {
            difference = 0;
        }
        scan.BaseHeadroom = scan.Descending ? difference : 0;
        scan.AlternateHeadroom = scan.Descending ? 0 : difference;
    }

    static AvifPixelBuffer EncodePixels(
        AvifPixelBuffer baseLinear,
        AvifPixelBuffer alternateLinear,
        int width,
        int height,
        ColorMatrix toMath,
        AvifColorPrimaries mathPrimaries,
        bool multichannel,
        GainMapScan scan,
        AvifSecurityLimits limits)
    {
        AvifPixelBuffer encoded = AvifPixelBuffer.Create(
            width,
            height,
            AvifPixelFormat.Rgb,
            16,
            true,
            1,
            limits);
        if (encoded.GetCode() != AvifResultCode.Success)
        {
            return encoded;
        }
        ColorVector luma = GetLumaCoefficients(mathPrimaries);
        PackedDitherState dither = new PackedDitherState(
            AvifDitherMode.None, width);
        int y = 0;
        while (y < height)
        {
            Span<byte> row = encoded.GetRowSpan(y);
            int x = 0;
            while (x < width)
            {
                ColorVector baseValue = SampleMathPixel(
                    baseLinear, x, y, width, height, toMath);
                ColorVector alternateValue = SampleMathPixel(
                    alternateLinear, x, y, width, height, toMath);
                double red = EncodeGain(
                    baseValue, alternateValue, 0, multichannel, luma, scan);
                double green = multichannel
                    ? EncodeGain(
                        baseValue, alternateValue, 1, true, luma, scan)
                    : red;
                double blue = multichannel
                    ? EncodeGain(
                        baseValue, alternateValue, 2, true, luma, scan)
                    : red;
                PackedPixelUtility.Write(
                    encoded,
                    row,
                    x,
                    y,
                    new PackedColorSample(red, green, blue, 1),
                    dither,
                    false);
                x++;
            }
            dither.FinishRow();
            y++;
        }
        return encoded;
    }

    static double EncodeGain(
        ColorVector baseValue,
        ColorVector alternateValue,
        int channel,
        bool multichannel,
        ColorVector luma,
        GainMapScan scan)
    {
        double raw = ComputeRawGain(
            baseValue,
            alternateValue,
            channel,
            multichannel,
            luma,
            scan);
        if (scan.Descending)
        {
            raw = -raw;
        }
        double range = scan.GainMaximum[channel]
            - scan.GainMinimum[channel];
        if (range <= Epsilon)
        {
            return 0;
        }
        return System.Math.Max(0, System.Math.Min(
            1, (raw - scan.GainMinimum[channel]) / range));
    }

    static double ComputeRawGain(
        ColorVector baseValue,
        ColorVector alternateValue,
        int channel,
        bool multichannel,
        ColorVector luma,
        GainMapScan scan)
    {
        double baseChannel = multichannel
            ? GetChannel(baseValue, channel)
            : GetLuma(baseValue, luma);
        double alternateChannel = multichannel
            ? GetChannel(alternateValue, channel)
            : GetLuma(alternateValue, luma);
        double ratio = (alternateChannel + scan.AlternateOffset[channel])
            / System.Math.Max(
                baseChannel + scan.BaseOffset[channel], Epsilon);
        return System.Math.Log(System.Math.Max(ratio, Epsilon), 2);
    }

    static ColorVector ReadMathPixel(
        AvifPixelBuffer buffer,
        int x,
        int y,
        ColorMatrix toMath)
    {
        PackedColorSample sample = PackedPixelUtility.Read(
            buffer, buffer.GetRowSpan(y), x);
        return toMath.Transform(new ColorVector(
            sample.Red, sample.Green, sample.Blue));
    }

    static ColorVector SampleMathPixel(
        AvifPixelBuffer buffer,
        int x,
        int y,
        int outputWidth,
        int outputHeight,
        ColorMatrix toMath)
    {
        double sourceX = (x + 0.5) * buffer.GetWidth()
            / outputWidth - 0.5;
        double sourceY = (y + 0.5) * buffer.GetHeight()
            / outputHeight - 0.5;
        sourceX = System.Math.Max(0, System.Math.Min(
            buffer.GetWidth() - 1, sourceX));
        sourceY = System.Math.Max(0, System.Math.Min(
            buffer.GetHeight() - 1, sourceY));
        int left = (int)System.Math.Floor(sourceX);
        int top = (int)System.Math.Floor(sourceY);
        int right = System.Math.Min(buffer.GetWidth() - 1, left + 1);
        int bottom = System.Math.Min(buffer.GetHeight() - 1, top + 1);
        double horizontal = sourceX - left;
        double vertical = sourceY - top;
        ColorVector topLeft = ReadMathPixel(buffer, left, top, toMath);
        ColorVector topRight = ReadMathPixel(buffer, right, top, toMath);
        ColorVector bottomLeft = ReadMathPixel(buffer, left, bottom, toMath);
        ColorVector bottomRight = ReadMathPixel(buffer, right, bottom, toMath);
        return new ColorVector(
            Interpolate(
                topLeft.First, topRight.First,
                bottomLeft.First, bottomRight.First,
                horizontal, vertical),
            Interpolate(
                topLeft.Second, topRight.Second,
                bottomLeft.Second, bottomRight.Second,
                horizontal, vertical),
            Interpolate(
                topLeft.Third, topRight.Third,
                bottomLeft.Third, bottomRight.Third,
                horizontal, vertical));
    }

    static double Interpolate(
        double topLeft,
        double topRight,
        double bottomLeft,
        double bottomRight,
        double horizontal,
        double vertical)
    {
        double top = topLeft + (topRight - topLeft) * horizontal;
        double bottom = bottomLeft
            + (bottomRight - bottomLeft) * horizontal;
        return top + (bottom - top) * vertical;
    }

    static ColorVector GetLumaCoefficients(
        AvifColorPrimaries primaries)
    {
        ColorMatrix? matrix = ColorTables.GetRgbToXyz(primaries);
        return new ColorVector(
            (matrix ?? throw new InvalidOperationException("Invalid internal state.")).Values[3], matrix.Values[4], matrix.Values[5]);
    }

    static double GetLuma(ColorVector value, ColorVector coefficients)
    {
        return value.First * coefficients.First
            + value.Second * coefficients.Second
            + value.Third * coefficients.Third;
    }

    static double GetChannel(ColorVector value, int channel)
    {
        if (channel == 0)
        {
            return value.First;
        }
        return channel == 1 ? value.Second : value.Third;
    }

    static bool HasFiniteRange(GainMapScan scan)
    {
        int channel = 0;
        while (channel < 3)
        {
            if (double.IsNaN(scan.GainMinimum[channel])
                || double.IsInfinity(scan.GainMinimum[channel])
                || double.IsNaN(scan.GainMaximum[channel])
                || double.IsInfinity(scan.GainMaximum[channel]))
            {
                return false;
            }
            channel++;
        }
        return true;
    }

    static AvifImage CreateGainImage(
        AvifPixelBuffer encoded,
        int width,
        int height,
        int bitDepth,
        AvifChromaFormat chromaFormat,
        AvifColorConversionOptions options,
        AvifSecurityLimits limits)
    {
        AvifMatrixCoefficients matrix = chromaFormat == AvifChromaFormat.Yuv444
            ? AvifMatrixCoefficients.Identity
            : AvifMatrixCoefficients.Bt709;
        AvifImage image = AvifImage.Create(
            width,
            height,
            bitDepth,
            chromaFormat,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            limits);
        if (image.GetCode() != AvifResultCode.Success)
        {
            return image;
        }
        AvifResultCode code = image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Linear,
            matrix,
            AvifRange.Full));
        if (code == AvifResultCode.Success)
        {
            AvifColorConversionOptions conversion = CopyOptions(options);
            conversion.UseEmbeddedIccProfile = false;
            conversion.ClampOutOfGamut = true;
            conversion.PreserveAlpha = false;
            conversion.OutputPremultipliedAlpha = false;
            code = AvifColorConverter.Create(conversion).ConvertToImage(
                encoded, image);
        }
        if (code == AvifResultCode.Success)
        {
            code = image.SetColorInfo(AvifColorInfo.Create(
                AvifColorPrimaries.Unspecified,
                AvifTransferCharacteristics.Unspecified,
                matrix,
                AvifRange.Full));
        }
        if (code != AvifResultCode.Success)
        {
            image.Close();
            return AvifImage.FromCode(code);
        }
        return image;
    }

    static AvifColorConversionOptions CopyOptions(
        AvifColorConversionOptions? source)
    {
        AvifColorConversionOptions copy = new AvifColorConversionOptions();
        copy.ResamplingFilter = (source ?? throw new InvalidOperationException("Invalid internal state.")).ResamplingFilter;
        copy.DitherMode = source.DitherMode;
        copy.UseEmbeddedIccProfile = source.UseEmbeddedIccProfile;
        copy.ClampOutOfGamut = source.ClampOutOfGamut;
        copy.PreserveAlpha = source.PreserveAlpha;
        copy.OutputPremultipliedAlpha = source.OutputPremultipliedAlpha;
        copy.WorkerCount = source.WorkerCount;
        return copy;
    }
}
