namespace raw;

internal static class RawOptionValidation
{
    internal static System.Collections.Generic.List<RawValidationMessage> ValidateLimits(RawLimits? limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (limits == null)
        {
            AddError(messages, "Limits", "Resource limits are required.");
            return messages;
        }

        RawValidationReport report = limits.Validate();
        int index = 0;
        while (index < (report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            messages.Add(report.Messages[index]);
            index++;
        }
        return messages;
    }

    internal static void AddPositive(System.Collections.Generic.List<RawValidationMessage> messages, long value, string context)
    {
        if (value <= 0)
        {
            AddError(messages, context, "The limit must be greater than zero.");
        }
    }

    internal static void AddNonNegative(System.Collections.Generic.List<RawValidationMessage> messages, int value, string context)
    {
        if (value < 0)
        {
            AddError(messages, context, "The value cannot be negative.");
        }
    }

    internal static void AddError(System.Collections.Generic.List<RawValidationMessage> messages, string context, string message)
    {
        messages.Add(new RawValidationMessage(RawValidationCategory.Semantic, RawDiagnosticCode.InvalidArgument, RawSeverity.Error, RawFormat.Unknown, -1, context, message, false));
    }

    internal static RawValidationReport CreateReport(bool strict, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        return new RawValidationReport(strict, messages.ToArray());
    }

    internal static void ValidateDemosaicMode(System.Collections.Generic.List<RawValidationMessage> messages, RawDemosaicMode value)
    {
        switch (value)
        {
            case RawDemosaicMode.None:
            case RawDemosaicMode.NearestNeighbor:
            case RawDemosaicMode.Bilinear:
            case RawDemosaicMode.Vng:
            case RawDemosaicMode.Ahd:
                return;
            default:
                AddError(messages, "Render.DemosaicMode", "The demosaic mode is not defined.");
                return;
        }
    }

    internal static void ValidateWhiteBalanceMode(System.Collections.Generic.List<RawValidationMessage> messages, RawWhiteBalanceMode value)
    {
        switch (value)
        {
            case RawWhiteBalanceMode.None:
            case RawWhiteBalanceMode.Camera:
            case RawWhiteBalanceMode.Daylight:
            case RawWhiteBalanceMode.Auto:
            case RawWhiteBalanceMode.UserMultipliers:
            case RawWhiteBalanceMode.TemperatureAndTint:
                return;
            default:
                AddError(messages, "Render.WhiteBalanceMode", "The white-balance mode is not defined.");
                return;
        }
    }

    internal static void ValidateWhiteBalanceConfiguration(System.Collections.Generic.List<RawValidationMessage> messages, RawWhiteBalanceMode mode, RawWhiteBalance? whiteBalance, RawArea? area)
    {
        if (area != null && (area.Kind != RawAreaKind.UserCrop || area.X < 0 || area.Y < 0 || area.Width <= 0 || area.Height <= 0))
        {
            AddError(messages, "Render.WhiteBalanceArea", "The auto-white-balance area must be a nonempty user region with nonnegative coordinates.");
        }
        if (whiteBalance != null && !RawWhiteBalanceProcessor.ValidCalibration(whiteBalance, false))
        {
            AddError(messages, "Render.WhiteBalance", "The caller white-balance calibration is invalid.");
        }
        if (mode == RawWhiteBalanceMode.UserMultipliers && (whiteBalance == null || whiteBalance.Source != RawWhiteBalanceMode.UserMultipliers || whiteBalance.Multipliers == null || whiteBalance.Multipliers.Length != 3))
        {
            AddError(messages, "Render.WhiteBalance", "User-multiplier mode requires three RGB multipliers from a user calibration.");
        }
        if (mode == RawWhiteBalanceMode.TemperatureAndTint && (whiteBalance == null || whiteBalance.Source != RawWhiteBalanceMode.TemperatureAndTint || whiteBalance.TemperatureKelvin < 1000.0 || whiteBalance.TemperatureKelvin > 50000.0))
        {
            AddError(messages, "Render.WhiteBalance", "Temperature-and-tint mode requires a caller calibration between 1000 and 50000 kelvin.");
        }
        if (mode == RawWhiteBalanceMode.Daylight && whiteBalance != null && (whiteBalance.Source != RawWhiteBalanceMode.Daylight || whiteBalance.Multipliers == null || whiteBalance.Multipliers.Length != 3))
        {
            AddError(messages, "Render.WhiteBalance", "A daylight override must contain three RGB multipliers and use the daylight source.");
        }
    }

    internal static void ValidateHighlightMode(System.Collections.Generic.List<RawValidationMessage> messages, RawHighlightMode value)
    {
        switch (value)
        {
            case RawHighlightMode.Clip:
            case RawHighlightMode.Blend:
            case RawHighlightMode.Reconstruct:
                return;
            default:
                AddError(messages, "Render.HighlightMode", "The highlight mode is not defined.");
                return;
        }
    }

    internal static void ValidateColorSpace(System.Collections.Generic.List<RawValidationMessage> messages, RawColorSpace value)
    {
        switch (value)
        {
            case RawColorSpace.RawCamera:
            case RawColorSpace.XyzD50:
            case RawColorSpace.XyzD65:
            case RawColorSpace.Srgb:
            case RawColorSpace.DisplayP3:
            case RawColorSpace.AdobeRgb:
            case RawColorSpace.ProPhotoRgb:
            case RawColorSpace.Rec2020:
            case RawColorSpace.AcesCg:
                return;
            default:
                AddError(messages, "Render.ColorSpace", "The color space is not defined.");
                return;
        }
    }

    internal static void ValidateChromaticAdaptation(System.Collections.Generic.List<RawValidationMessage> messages, RawChromaticAdaptationMode value)
    {
        switch (value)
        {
            case RawChromaticAdaptationMode.None:
            case RawChromaticAdaptationMode.Bradford:
            case RawChromaticAdaptationMode.Cat02:
                return;
            default:
                AddError(messages, "Render.ChromaticAdaptation", "The chromatic adaptation mode is not defined.");
                return;
        }
    }

    internal static void ValidateTransferFunction(System.Collections.Generic.List<RawValidationMessage> messages, RawTransferFunction value)
    {
        switch (value)
        {
            case RawTransferFunction.Linear:
            case RawTransferFunction.Srgb:
            case RawTransferFunction.Gamma:
            case RawTransferFunction.Rec2020:
            case RawTransferFunction.ProPhotoRgb:
                return;
            default:
                AddError(messages, "Render.TransferFunction", "The transfer function is not defined.");
                return;
        }
    }

    internal static void ValidateResampler(System.Collections.Generic.List<RawValidationMessage> messages, RawResampler value)
    {
        switch (value)
        {
            case RawResampler.NearestNeighbor:
            case RawResampler.Bilinear:
            case RawResampler.Bicubic:
            case RawResampler.Lanczos:
                return;
            default:
                AddError(messages, "Render.Resampler", "The resampler is not defined.");
                return;
        }
    }

    internal static void ValidateMetadataPrecedence(System.Collections.Generic.List<RawValidationMessage> messages, RawMetadataPrecedence value)
    {
        switch (value)
        {
            case RawMetadataPrecedence.None:
            case RawMetadataPrecedence.Exif:
            case RawMetadataPrecedence.Xmp:
            case RawMetadataPrecedence.Iptc:
                return;
            default:
                AddError(messages, "Metadata.Precedence", "The metadata precedence is not defined.");
                return;
        }
    }

    internal static void ValidateWriteMode(System.Collections.Generic.List<RawValidationMessage> messages, RawWriteMode value)
    {
        switch (value)
        {
            case RawWriteMode.GenericRaw:
            case RawWriteMode.NewContainer:
            case RawWriteMode.PreservingRewrite:
            case RawWriteMode.MetadataPatch:
                return;
            default:
                AddError(messages, "Write.Mode", "The write mode is not defined.");
                return;
        }
    }

    internal static void ValidateUnknownDataPolicy(System.Collections.Generic.List<RawValidationMessage> messages, RawUnknownDataPolicy value)
    {
        switch (value)
        {
            case RawUnknownDataPolicy.Preserve:
            case RawUnknownDataPolicy.Reject:
            case RawUnknownDataPolicy.DropWithConsent:
                return;
            default:
                AddError(messages, "Write.UnknownDataPolicy", "The unknown-data policy is not defined.");
                return;
        }
    }
}
