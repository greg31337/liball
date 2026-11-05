using System;

namespace avif;

internal static class ToneMapRenderer
{
    internal static AvifPixelBuffer Render(
        AvifToneMap toneMap,
        AvifRenderOptions options)
    {
        AvifResultCode validation = Validate(toneMap, options);
        if (validation != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(validation);
        }

        AvifImage? baseImage = toneMap.GetBaseImage();
        if (options.ToneMapMode == AvifToneMapMode.BaseImage
            || (options.ToneMapMode == AvifToneMapMode.Automatic
                && !toneMap.IsAlternateImagePreferred()))
        {
            return RenderBase(baseImage, options);
        }

        AvifGainMap? gainMap = toneMap.GetGainMap();
        AvifResultCode gainMapCode = (gainMap ?? throw new InvalidOperationException("Invalid internal state.")).GetCode();
        if (gainMapCode != AvifResultCode.Success)
        {
            if (options.ToneMapMode == AvifToneMapMode.Automatic
                && IsUnsupported(gainMapCode))
            {
                return RenderBase(baseImage, options);
            }
            return AvifPixelBuffer.FromCode(gainMapCode);
        }

        if (options.ToneMapMode == AvifToneMapMode.Automatic
            && (gainMap.GetMetadata() ?? throw new InvalidOperationException("Invalid internal state.")).GetWeight(options.DisplayHeadroom) == 0)
        {
            return RenderBase(baseImage, options);
        }

        AvifColorConversionOptions conversion = CopyConversion(
            options.ColorConversion);
        AvifPixelBuffer reconstructed = GainMapRenderer.Apply(
            baseImage,
            gainMap,
            options.DisplayHeadroom,
            conversion,
            options.Limits,
            options.PreserveAlpha);
        if (options.ToneMapMode == AvifToneMapMode.Automatic
            && IsUnsupported(reconstructed.GetCode()))
        {
            reconstructed.Close();
            return RenderBase(baseImage, options);
        }
        return reconstructed;
    }

    static AvifPixelBuffer RenderBase(
        AvifImage? baseImage,
        AvifRenderOptions options)
    {
        AvifPixelBuffer output = AvifPixelBuffer.Create(
            (baseImage ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(),
            baseImage.GetHeight(),
            AvifPixelFormat.Rgba,
            16,
            true,
            1,
            options.Limits);
        if (output.GetCode() != AvifResultCode.Success)
        {
            return output;
        }

        AvifColorConversionOptions conversion = CopyConversion(
            options.ColorConversion);
        conversion.PreserveAlpha = options.PreserveAlpha;
        conversion.OutputPremultipliedAlpha = false;
        AvifColorConverter converter = AvifColorConverter.Create();
        AvifResultCode settingsCode = converter.SetOptions(conversion);
        if (settingsCode != AvifResultCode.Success)
        {
            output.Close();
            return AvifPixelBuffer.FromCode(settingsCode);
        }
        AvifResultCode conversionCode = converter.ConvertToPixelBuffer(
            baseImage, output);
        if (conversionCode != AvifResultCode.Success)
        {
            output.Close();
            return AvifPixelBuffer.FromCode(conversionCode);
        }
        return output;
    }

    static AvifResultCode Validate(
        AvifToneMap toneMap,
        AvifRenderOptions options)
    {
        if (toneMap == null || options == null
            || options.ColorConversion == null || options.Limits == null
            || options.DisplayHeadroom < 1
            || float.IsNaN(options.DisplayHeadroom)
            || float.IsInfinity(options.DisplayHeadroom))
        {
            return AvifResultCode.InvalidArgument;
        }
        if (options.ToneMapMode != AvifToneMapMode.Automatic
            && options.ToneMapMode != AvifToneMapMode.BaseImage
            && options.ToneMapMode != AvifToneMapMode.ApplyGainMap)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode associationCode = toneMap.GetAssociationCode();
        if (associationCode != AvifResultCode.Success)
        {
            return associationCode;
        }
        AvifImage? baseImage = toneMap.GetBaseImage();
        if (baseImage == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode baseCode = baseImage.GetCode();
        if (baseCode != AvifResultCode.Success)
        {
            return baseCode;
        }
        if (ResourceGuard.ValidateLimits(options.Limits)
            != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifColorConverter converter = AvifColorConverter.Create();
        return converter.SetOptions(CopyConversion(options.ColorConversion));
    }

    static bool IsUnsupported(AvifResultCode value)
    {
        return value == AvifResultCode.UnsupportedFeature
            || value == AvifResultCode.UnsupportedEssentialProperty
            || value == AvifResultCode.UnsupportedProtection;
    }

    static AvifColorConversionOptions CopyConversion(
        AvifColorConversionOptions value)
    {
        AvifColorConversionOptions copy = new AvifColorConversionOptions();
        copy.ResamplingFilter = value.ResamplingFilter;
        copy.DitherMode = value.DitherMode;
        copy.UseEmbeddedIccProfile = value.UseEmbeddedIccProfile;
        copy.ClampOutOfGamut = value.ClampOutOfGamut;
        copy.PreserveAlpha = value.PreserveAlpha;
        copy.OutputPremultipliedAlpha = value.OutputPremultipliedAlpha;
        copy.WorkerCount = value.WorkerCount;
        return copy;
    }
}
