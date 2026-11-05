namespace raw;

internal static class RawRenderPipeline
{
    internal static RawRgbImage Render(RawSensorImage sensorImage, RawRenderOptions options)
    {
        sensorImage = SelectCrop(sensorImage, options);
        Validate(sensorImage, options);
        double illuminantTemperatureKelvin = ResolveIlluminantTemperature(sensorImage, options);
        options.CheckCancellation();
        RawRgbImage current;
        if (options.HalfSize)
        {
            RawPreviewOptions previewOptions = new RawPreviewOptions();
            previewOptions.Limits = options.Limits;
            previewOptions.CancellationToken = options.CancellationToken;
            current = RawFastProcessor.CreateHalfSizePreview(sensorImage, previewOptions);
        }
        else
        {
            current = RawDemosaicer.Reconstruct(sensorImage, options);
        }
        options.CheckCancellation();
        bool grayscale = current.ChannelCount == 1;
        if (grayscale)
        {
            return RawOutputProcessor.Create(current, null, options);
        }
        current = RawWhiteBalanceProcessor.Apply(current, sensorImage.WhiteBalance, options);
        options.CheckCancellation();
        RawChromaticAberration? postDemosaicChromaticAberration = PostDemosaicChromaticAberration(sensorImage, options);
        current = RawDetailProcessor.Apply(current, postDemosaicChromaticAberration, options);
        options.CheckCancellation();
        current = RawExposureProcessor.Apply(current, options);
        options.CheckCancellation();
        RawRenderOptions linearOutput = CopyOptions(options);
        linearOutput.TransferFunction = RawTransferFunction.Linear;
        if (options.ColorSpace == RawColorSpace.RawCamera)
        {
            linearOutput.ColorSpace = RawColorSpace.RawCamera;
            current = RawOutputColorProcessor.Convert(current, linearOutput);
        }
        else
        {
            RawRenderOptions xyzOptions = CopyOptions(options);
            xyzOptions.ColorSpace = IntermediateXyz(options.ColorSpace);
            xyzOptions.TransferFunction = RawTransferFunction.Linear;
            current = RawCameraColorProcessor.Convert(current, sensorImage.ColorProfiles, illuminantTemperatureKelvin, xyzOptions);
            options.CheckCancellation();
            current = RawOutputColorProcessor.Convert(current, linearOutput);
        }
        options.CheckCancellation();
        current = RawToneProcessor.Apply(current, options);
        options.CheckCancellation();
        return RawOutputProcessor.Create(current, null, options);
    }

    internal static void Validate(RawSensorImage sensorImage, RawRenderOptions options)
    {
        if (options == null || !options.Validate().IsValid() || sensorImage == null || sensorImage.Info == null || !sensorImage.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid normalized sensor image and render options are required for the assembled render pipeline.", -1);
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
        RawOutputColorProcessor.ValidateTransfer(options.ColorSpace, options.TransferFunction);
        if (options.ColorSpace == RawColorSpace.XyzD50 || options.ColorSpace == RawColorSpace.XyzD65)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The assembled render pipeline produces gray, RGB, or RGBA output; use the individual color stage for XYZ output.", -1);
        }
        if (options.HalfSize && sensorImage.Info.Cfa == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Half-size rendering requires a compatible color filter array.", -1);
        }
        if (!options.HalfSize && sensorImage.Info.Cfa != null && options.DemosaicMode == RawDemosaicMode.None)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The assembled render pipeline cannot emit an unreconstructed color-filter-array mosaic.", -1);
        }
        if (options.ApplyPostDemosaicChromaticAberration && sensorImage.ChromaticAberrationApplied)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Chromatic aberration cannot be corrected both before and after demosaic.", -1);
        }
        bool grayscale = sensorImage.Info.Cfa == null && sensorImage.Info.GetChannelCount() == 1;
        if (grayscale && !NeutralGrayscaleOptions(options))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Grayscale rendering requires raw-camera linear output and neutral RGB-only processing controls.", -1);
        }
        if (!grayscale && options.ColorSpace != RawColorSpace.RawCamera)
        {
            RawCameraColorProcessor.ValidateProfiles(sensorImage.ColorProfiles);
        }
    }

    internal static bool NeutralGrayscaleOptions(RawRenderOptions options)
    {
        return !options.HalfSize && options.WhiteBalanceMode == RawWhiteBalanceMode.None && options.HighlightMode == RawHighlightMode.Clip && options.ColorSpace == RawColorSpace.RawCamera && options.TransferFunction == RawTransferFunction.Linear && options.ExposureCompensation == 0.0 && !options.AutoBrightness && options.ShadowScale == 1.0 && options.SaturationProtection == 0.0 && options.DenoiseMode == RawDenoiseMode.None && options.FalseColorSuppression == 0.0 && !options.ApplyPostDemosaicChromaticAberration && options.CaptureSharpening == 0.0 && options.ToneCurve.Length == 0 && options.Contrast == 1.0 && options.GamutMode == RawGamutMode.None;
    }

    internal static double ResolveIlluminantTemperature(RawSensorImage sensorImage, RawRenderOptions options)
    {
        if (options.WhiteBalance != null && options.WhiteBalance.TemperatureKelvin >= 1000.0 && options.WhiteBalance.TemperatureKelvin <= 50000.0)
        {
            return options.WhiteBalance.TemperatureKelvin;
        }
        if (sensorImage.WhiteBalance != null && sensorImage.WhiteBalance.TemperatureKelvin >= 1000.0 && sensorImage.WhiteBalance.TemperatureKelvin <= 50000.0)
        {
            return sensorImage.WhiteBalance.TemperatureKelvin;
        }
        return 6500.0;
    }

    internal static RawColorSpace IntermediateXyz(RawColorSpace outputColorSpace)
    {
        return outputColorSpace == RawColorSpace.ProPhotoRgb ? RawColorSpace.XyzD50 : RawColorSpace.XyzD65;
    }

    internal static RawChromaticAberration? PostDemosaicChromaticAberration(RawSensorImage sensorImage, RawRenderOptions options)
    {
        if (sensorImage.ChromaticAberration == null)
        {
            return null;
        }
        RawArea selected = RawDefectProcessor.SelectedArea(sensorImage);
        double divisor = options.HalfSize ? 2.0 : 1.0;
        double centerX = (sensorImage.ChromaticAberration.CenterX - selected.X) / divisor;
        double centerY = (sensorImage.ChromaticAberration.CenterY - selected.Y) / divisor;
        return new RawChromaticAberration(centerX, centerY, sensorImage.ChromaticAberration.RedScale, sensorImage.ChromaticAberration.BlueScale);
    }

    internal static RawRenderOptions CopyOptions(RawRenderOptions source)
    {
        RawRenderOptions result = new RawRenderOptions();
        result.Limits = source.Limits;
        result.CancellationToken = source.CancellationToken;
        result.ImageIndex = source.ImageIndex;
        result.PlaneIndex = source.PlaneIndex;
        result.UserCrop = source.UserCrop;
        result.DemosaicMode = source.DemosaicMode;
        result.WhiteBalanceMode = source.WhiteBalanceMode;
        result.WhiteBalance = source.WhiteBalance;
        result.WhiteBalanceArea = source.WhiteBalanceArea;
        result.HighlightMode = source.HighlightMode;
        result.ColorSpace = source.ColorSpace;
        result.ChromaticAdaptation = source.ChromaticAdaptation;
        result.TransferFunction = source.TransferFunction;
        result.Resampler = source.Resampler;
        result.OutputBitDepth = source.OutputBitDepth;
        result.OutputWidth = source.OutputWidth;
        result.OutputHeight = source.OutputHeight;
        result.ExposureCompensation = source.ExposureCompensation;
        result.AutoBrightness = source.AutoBrightness;
        result.AutoBrightnessClippingThreshold = source.AutoBrightnessClippingThreshold;
        result.ShadowScale = source.ShadowScale;
        result.SaturationProtection = source.SaturationProtection;
        result.DenoiseMode = source.DenoiseMode;
        result.DenoiseStrength = source.DenoiseStrength;
        result.FalseColorSuppression = source.FalseColorSuppression;
        result.ApplyPostDemosaicChromaticAberration = source.ApplyPostDemosaicChromaticAberration;
        result.CaptureSharpening = source.CaptureSharpening;
        result.ToneCurve = source.ToneCurve;
        result.Contrast = source.Contrast;
        result.GamutMode = source.GamutMode;
        result.Gamma = source.Gamma;
        result.OrderedDithering = source.OrderedDithering;
        result.HalfSize = source.HalfSize;
        result.ApplyOrientation = source.ApplyOrientation;
        return result;
    }

    internal static RawSensorImage SelectCrop(RawSensorImage source, RawRenderOptions options)
    {
        if (source == null || options == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A sensor image and render options are required for crop selection.", -1);
        }
        if (options.UserCrop == null)
        {
            return source;
        }
        RawCropModel crops = new RawCropModel(source.Info, options.UserCrop);
        RawSensorImage result = new RawSensorImage(source.Info, source.IntegerSamples, source.FloatingSamples, source.RowStrideSamples, false, source.StoredImage, crops.UserCrop, source.MaskedAreas, source.BlackLevel, source.WhiteLevels, source.WhiteBalance, source.ColorProfiles, source.Diagnostics);
        result.CropModel = crops;
        result.ClippingMask = source.ClippingMask;
        result.DefectMap = source.DefectMap;
        result.BandingCorrection = source.BandingCorrection;
        result.GainMaps = source.GainMaps;
        result.ChromaticAberration = source.ChromaticAberration;
        result.GainMapsApplied = source.GainMapsApplied;
        result.ChromaticAberrationApplied = source.ChromaticAberrationApplied;
        return result;
    }
}
