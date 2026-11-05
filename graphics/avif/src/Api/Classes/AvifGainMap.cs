using System;

namespace avif;

/// <summary>Associates decoded gain-map samples with validated ISO 21496-1 metadata.</summary>
public class AvifGainMap : IAvifGainMap, IAvifDiagnosticSource
{
    AvifResultCode code;
    AvifImage? image;
    AvifGainMapMetadata? metadata;
    AvifColorInfo? alternateColorInfo;
    int quality;

    internal static AvifGainMap FromCode(AvifResultCode code)
    {
        AvifGainMap result = new AvifGainMap(null, null, null);
        result.code = code;
        return result;
    }

    /// <summary>Creates a gain map using base-color-space metadata.</summary>
    /// <param name="image">The caller-owned decoded gain-map image.</param>
    /// <param name="metadata">The parsed gain-map metadata.</param>
    /// <returns>The initialized gain-map association.</returns>
    internal static AvifGainMap Create(
        AvifImage image,
        AvifGainMapMetadata metadata)
    {
        return new AvifGainMap(image, metadata, null);
    }

    /// <summary>Creates a gain map with alternate-image CICP color information.</summary>
    /// <param name="image">The caller-owned decoded gain-map image.</param>
    /// <param name="metadata">The parsed gain-map metadata.</param>
    /// <param name="alternateColorInfo">The alternate image color information.</param>
    public AvifGainMap(
        AvifImage? image,
        AvifGainMapMetadata? metadata,
        AvifColorInfo? alternateColorInfo)
    {
        Initialize(image, metadata, alternateColorInfo);
    }

    /// <summary>Creates a gain map with alternate-image color information.</summary>
    /// <returns>The initialized gain-map association.</returns>
    internal static AvifGainMap Create(
        AvifImage? image,
        AvifGainMapMetadata? metadata,
        AvifColorInfo? alternateColorInfo)
    {
        return new AvifGainMap(image, metadata, alternateColorInfo);
    }

    internal static AvifGainMap Create(AvifResultCode code)
    {
        return FromCode(code);
    }

    /// <summary>Returns the association validation result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        return (image ?? throw new InvalidOperationException("Invalid internal state.")).GetCode();
    }

    /// <summary>Returns diagnostics for gain-map association or rendering state.</summary>
    /// <returns>The structured rendering diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            GetCode(),
            AvifDiagnosticCategory.Rendering,
            "The gain-map rendering operation did not complete successfully.");
    }

    /// <summary>Returns the caller-owned decoded gain-map image.</summary>
    /// <returns>The decoded gain-map samples.</returns>
    public AvifImage? GetImage()
    {
        return image;
    }

    /// <summary>Returns the parsed gain-map metadata.</summary>
    /// <returns>The immutable metadata object supplied at construction.</returns>
    public AvifGainMapMetadata? GetMetadata()
    {
        return metadata;
    }

    /// <summary>Returns a copy of the alternate-image color information.</summary>
    /// <returns>The alternate CICP information, or null when unavailable.</returns>
    public AvifColorInfo? GetAlternateColorInfo()
    {
        return CopyColorInfo(alternateColorInfo);
    }

    /// <summary>Sets the gain-map encoding quality override.</summary>
    /// <param name="quality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetQuality(int quality)
    {
        if (quality < -1 || quality > 100)
        {
            return AvifResultCode.InvalidArgument;
        }
        this.quality = quality;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the gain-map quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    public int GetQuality()
    {
        return quality;
    }

    void Initialize(
        AvifImage? requestedImage,
        AvifGainMapMetadata? requestedMetadata,
        AvifColorInfo? requestedAlternateColorInfo)
    {
        image = requestedImage;
        metadata = requestedMetadata;
        alternateColorInfo = CopyColorInfo(requestedAlternateColorInfo);
        quality = -1;
        if (requestedImage == null || requestedMetadata == null)
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }
        AvifResultCode imageCode = requestedImage.GetCode();
        if (imageCode != AvifResultCode.Success)
        {
            code = imageCode;
            return;
        }
        AvifResultCode metadataCode = requestedMetadata.GetCode();
        if (metadataCode != AvifResultCode.Success)
        {
            code = metadataCode;
            return;
        }
        AvifColorInfo? information = requestedImage.GetColorInfo();
        if (requestedImage.GetAlphaMode() != AvifAlphaMode.None
            || requestedImage.GetIccProfile() != null
            || (information ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries != AvifColorPrimaries.Unspecified
            || information.TransferCharacteristics
                != AvifTransferCharacteristics.Unspecified)
        {
            code = AvifResultCode.NonConformingData;
            return;
        }
        code = AvifResultCode.Success;
    }

    static AvifColorInfo? CopyColorInfo(AvifColorInfo? value)
    {
        return value == null
            ? null
            : AvifColorInfo.Create(
                value.ColorPrimaries,
                value.TransferCharacteristics,
                value.MatrixCoefficients,
                value.Range);
    }
}
