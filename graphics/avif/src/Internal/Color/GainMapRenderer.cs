using System;

namespace avif;

internal static class GainMapRenderer
{
    internal static AvifPixelBuffer Apply(
        AvifImage baseImage,
        AvifGainMap gainMap,
        double displayHeadroom,
        AvifColorConversionOptions? options,
        AvifSecurityLimits limits)
    {
        return Apply(
            baseImage,
            gainMap,
            displayHeadroom,
            options,
            limits,
            true);
    }

    internal static AvifPixelBuffer Apply(
        AvifImage? baseImage,
        AvifGainMap gainMap,
        double displayHeadroom,
        AvifColorConversionOptions? options,
        AvifSecurityLimits limits,
        bool preserveAlpha)
    {
        if (baseImage == null || gainMap == null || options == null
            || limits == null || displayHeadroom < 1
            || double.IsNaN(displayHeadroom)
            || double.IsInfinity(displayHeadroom))
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }
        AvifResultCode baseCode = baseImage.GetCode();
        if (baseCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(baseCode);
        }
        AvifResultCode gainMapCode = gainMap.GetCode();
        if (gainMapCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(gainMapCode);
        }
        if (baseImage.GetIccProfile() != null)
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.UnsupportedFeature);
        }

        AvifGainMapMetadata? metadata = gainMap.GetMetadata();
        ColorConversionContext baseContext = new ColorConversionContext(
            baseImage.GetColorInfo());
        AvifColorPrimaries mathPrimaries = baseContext.Primaries;
        if (!(metadata ?? throw new InvalidOperationException("Invalid internal state.")).UsesBaseColorSpace())
        {
            AvifColorInfo? alternate = gainMap.GetAlternateColorInfo();
            if (alternate == null)
            {
                return AvifPixelBuffer.Create(
                    AvifResultCode.UnsupportedFeature);
            }
            ColorConversionContext alternateContext =
                new ColorConversionContext(alternate);
            mathPrimaries = alternateContext.Primaries;
        }
        if (!ColorTables.IsSupported(mathPrimaries))
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.UnsupportedFeature);
        }

        AvifColorConversionOptions baseOptions = CopyOptions(options);
        baseOptions.UseEmbeddedIccProfile = false;
        baseOptions.ClampOutOfGamut = false;
        baseOptions.PreserveAlpha = true;
        baseOptions.OutputPremultipliedAlpha = false;
        AvifPixelBuffer baseRgb = AvifColorConverter.Create(baseOptions).ConvertWithDepth(
            baseImage,
            AvifPixelFormat.Rgba,
            16,
            true);
        if (baseRgb.GetCode() != AvifResultCode.Success)
        {
            return baseRgb;
        }

        AvifPixelBuffer gainRgb = ConvertGainMap(
            gainMap.GetImage(), options);
        if (gainRgb.GetCode() != AvifResultCode.Success)
        {
            baseRgb.Close();
            return gainRgb;
        }

        AvifPixelBuffer output = AvifPixelBuffer.Create(
            baseImage.GetWidth(),
            baseImage.GetHeight(),
            AvifPixelFormat.Rgba,
            16,
            true,
            1,
            limits);
        if (output.GetCode() != AvifResultCode.Success)
        {
            baseRgb.Close();
            gainRgb.Close();
            return output;
        }

        double weight = metadata.GetWeight(displayHeadroom);
        PackedDitherState dither = new PackedDitherState(
            AvifDitherMode.None,
            baseImage.GetWidth());
        int y = 0;
        while (y < baseImage.GetHeight())
        {
            Span<byte> baseRow = baseRgb.GetRowSpan(y);
            Span<byte> outputRow = output.GetRowSpan(y);
            int x = 0;
            while (x < baseImage.GetWidth())
            {
                PackedColorSample baseSample = PackedPixelUtility.Read(
                    baseRgb, baseRow, x);
                PackedColorSample result = ApplyPixel(
                    baseSample,
                    SampleGainMap(gainRgb, x, y, baseImage),
                    metadata,
                    weight,
                    mathPrimaries,
                    preserveAlpha);
                if (!IsFinite(result))
                {
                    baseRgb.Close();
                    gainRgb.Close();
                    output.Close();
                    return AvifPixelBuffer.Create(
                        AvifResultCode.RenderFailed);
                }
                PackedPixelUtility.Write(
                    output,
                    outputRow,
                    x,
                    y,
                    result,
                    dither,
                    false);
                x++;
            }
            dither.FinishRow();
            y++;
        }

        baseRgb.Close();
        gainRgb.Close();
        return output;
    }

    static AvifPixelBuffer ConvertGainMap(
        AvifImage? image,
        AvifColorConversionOptions options)
    {
        AvifImage view = (image ?? throw new InvalidOperationException("Invalid internal state.")).CreateView(
            0, 0, image.GetWidth(), image.GetHeight());
        if (view.GetCode() != AvifResultCode.Success)
        {
            return AvifPixelBuffer.Create(view.GetCode());
        }
        AvifColorInfo? sourceInfo = image.GetColorInfo();
        AvifResultCode colorCode = view.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Linear,
            (sourceInfo ?? throw new InvalidOperationException("Invalid internal state.")).MatrixCoefficients,
            sourceInfo.Range));
        if (colorCode != AvifResultCode.Success)
        {
            view.Close();
            return AvifPixelBuffer.FromCode(colorCode);
        }

        AvifColorConversionOptions gainOptions = CopyOptions(options);
        gainOptions.UseEmbeddedIccProfile = false;
        gainOptions.ClampOutOfGamut = true;
        gainOptions.PreserveAlpha = false;
        gainOptions.OutputPremultipliedAlpha = false;
        AvifPixelBuffer converted = AvifColorConverter.Create(
            gainOptions).ConvertWithDepth(
                view,
                AvifPixelFormat.Rgb,
                16,
                true);
        view.Close();
        return converted;
    }

    static PackedColorSample ApplyPixel(
        PackedColorSample baseSample,
        PackedColorSample gainSample,
        AvifGainMapMetadata metadata,
        double weight,
        AvifColorPrimaries mathPrimaries,
        bool preserveAlpha)
    {
        ColorVector? baseLinear = new ColorVector(
            baseSample.Red, baseSample.Green, baseSample.Blue);
        if (mathPrimaries != AvifColorPrimaries.Bt709)
        {
            baseLinear = ColorTables.ConvertPrimaries(
                baseLinear, AvifColorPrimaries.Bt709, mathPrimaries);
        }
        if (weight == 0)
        {
            return new PackedColorSample(
                baseSample.Red,
                baseSample.Green,
                baseSample.Blue,
                preserveAlpha ? baseSample.Alpha : 1);
        }

        double red = ApplyChannel(
            (baseLinear ?? throw new InvalidOperationException("Invalid internal state.")).First, gainSample.Red, metadata, weight, 0);
        double green = ApplyChannel(
            baseLinear.Second, gainSample.Green, metadata, weight, 1);
        double blue = ApplyChannel(
            baseLinear.Third, gainSample.Blue, metadata, weight, 2);
        ColorVector? mapped = new ColorVector(red, green, blue);
        if (mathPrimaries != AvifColorPrimaries.Bt709)
        {
            mapped = ColorTables.ConvertPrimaries(
                mapped, mathPrimaries, AvifColorPrimaries.Bt709);
        }
        return new PackedColorSample(
            System.Math.Max(0, (mapped ?? throw new InvalidOperationException("Invalid internal state.")).First),
            System.Math.Max(0, mapped.Second),
            System.Math.Max(0, mapped.Third),
            preserveAlpha ? baseSample.Alpha : 1);
    }

    static double ApplyChannel(
        double baseValue,
        double encodedGain,
        AvifGainMapMetadata metadata,
        double weight,
        int channel)
    {
        encodedGain = System.Math.Max(0, System.Math.Min(1, encodedGain));
        double gammaInverse = 1 / metadata.GetGamma(channel);
        double decoded = System.Math.Pow(encodedGain, gammaInverse);
        double minimum = metadata.GetGainMapMinimum(channel);
        double maximum = metadata.GetGainMapMaximum(channel);
        double gainLog2 = minimum + (maximum - minimum) * decoded;
        return (baseValue + metadata.GetBaseOffset(channel))
            * System.Math.Pow(2, gainLog2 * weight)
            - metadata.GetAlternateOffset(channel);
    }

    static PackedColorSample SampleGainMap(
        AvifPixelBuffer gainMap,
        int x,
        int y,
        AvifImage baseImage)
    {
        double sourceX = (x + 0.5) * gainMap.GetWidth()
            / baseImage.GetWidth() - 0.5;
        double sourceY = (y + 0.5) * gainMap.GetHeight()
            / baseImage.GetHeight() - 0.5;
        sourceX = System.Math.Max(0, System.Math.Min(
            gainMap.GetWidth() - 1, sourceX));
        sourceY = System.Math.Max(0, System.Math.Min(
            gainMap.GetHeight() - 1, sourceY));
        int left = (int)System.Math.Floor(sourceX);
        int top = (int)System.Math.Floor(sourceY);
        int right = System.Math.Min(gainMap.GetWidth() - 1, left + 1);
        int bottom = System.Math.Min(gainMap.GetHeight() - 1, top + 1);
        double horizontal = sourceX - left;
        double vertical = sourceY - top;
        PackedColorSample topLeft = ReadPixel(gainMap, left, top);
        PackedColorSample topRight = ReadPixel(gainMap, right, top);
        PackedColorSample bottomLeft = ReadPixel(gainMap, left, bottom);
        PackedColorSample bottomRight = ReadPixel(gainMap, right, bottom);
        return new PackedColorSample(
            Interpolate(
                topLeft.Red, topRight.Red,
                bottomLeft.Red, bottomRight.Red,
                horizontal, vertical),
            Interpolate(
                topLeft.Green, topRight.Green,
                bottomLeft.Green, bottomRight.Green,
                horizontal, vertical),
            Interpolate(
                topLeft.Blue, topRight.Blue,
                bottomLeft.Blue, bottomRight.Blue,
                horizontal, vertical),
            1);
    }

    static PackedColorSample ReadPixel(
        AvifPixelBuffer buffer,
        int x,
        int y)
    {
        return PackedPixelUtility.Read(buffer, buffer.GetRowSpan(y), x);
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

    static bool IsFinite(PackedColorSample value)
    {
        return !double.IsNaN(value.Red)
            && !double.IsInfinity(value.Red)
            && value.Red <= 65504
            && !double.IsNaN(value.Green)
            && !double.IsInfinity(value.Green)
            && value.Green <= 65504
            && !double.IsNaN(value.Blue)
            && !double.IsInfinity(value.Blue)
            && value.Blue <= 65504;
    }

    static AvifColorConversionOptions CopyOptions(
        AvifColorConversionOptions source)
    {
        AvifColorConversionOptions copy = new AvifColorConversionOptions();
        copy.ResamplingFilter = source.ResamplingFilter;
        copy.DitherMode = source.DitherMode;
        copy.UseEmbeddedIccProfile = source.UseEmbeddedIccProfile;
        copy.ClampOutOfGamut = source.ClampOutOfGamut;
        copy.PreserveAlpha = source.PreserveAlpha;
        copy.OutputPremultipliedAlpha = source.OutputPremultipliedAlpha;
        copy.WorkerCount = source.WorkerCount;
        return copy;
    }
}
