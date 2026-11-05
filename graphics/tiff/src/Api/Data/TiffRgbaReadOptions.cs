namespace tiff;

/// <summary>Controls conversion of source samples to display-oriented RGBA8 or RGBA16 pixels.</summary>
/// <remarks>By default orientation, fill order, and photometric interpretation are applied, alpha is returned unassociated, and source values use their sample-format range.</remarks>
public class TiffRgbaReadOptions
{
    /// <summary>Indicates whether the TIFF orientation is applied to the returned raster.</summary>
    public bool ApplyOrientation;
    /// <summary>Specifies whether returned color channels are associated with or unassociated from alpha.</summary>
    public TiffExtraSample AlphaAssociation;
    /// <summary>Indicates whether declared minimum and maximum sample values are scaled to the full output range.</summary>
    public bool ApplyMinMaxScaling;
    /// <summary>Indicates whether the TIFF fill order is applied to packed source samples.</summary>
    public bool ApplyFillOrder;
    /// <summary>Indicates whether the TIFF photometric interpretation is converted to RGB instead of treating source color channels as direct intensities.</summary>
    public bool ApplyPhotometricConversion;
    /// <summary>Indicates whether TIFF predictor transforms are reversed before sample conversion.</summary>
    public bool ApplyPredictor;
    /// <summary>Indicates whether multibyte file samples are converted to host byte order.</summary>
    public bool ApplyByteOrderConversion;
    /// <summary>Indicates whether the first unspecified extra sample is interpreted as alpha.</summary>
    public bool UseUnspecifiedExtraSampleAsAlpha;
    /// <summary>Specifies how CFA samples are converted to RGB.</summary>
    public TiffCfaConversionMode CfaConversionMode;
    /// <summary>Specifies how unsupported compression or photometric features are handled.</summary>
    public TiffUnsupportedFeaturePolicy UnsupportedFeaturePolicy;

    /// <summary>Creates RGBA conversion options with display-oriented defaults.</summary>
    public TiffRgbaReadOptions()
    {
        ApplyOrientation = true;
        AlphaAssociation = TiffExtraSample.UnassociatedAlpha;
        ApplyMinMaxScaling = false;
        ApplyFillOrder = true;
        ApplyPhotometricConversion = true;
        ApplyPredictor = true;
        ApplyByteOrderConversion = true;
        UseUnspecifiedExtraSampleAsAlpha = true;
        CfaConversionMode = TiffCfaConversionMode.Bilinear;
        UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.Error;
    }

    internal TiffRgbaReadOptions Copy()
    {
        TiffRgbaReadOptions copy = new TiffRgbaReadOptions();
        copy.ApplyOrientation = ApplyOrientation;
        copy.AlphaAssociation = AlphaAssociation;
        copy.ApplyMinMaxScaling = ApplyMinMaxScaling;
        copy.ApplyFillOrder = ApplyFillOrder;
        copy.ApplyPhotometricConversion = ApplyPhotometricConversion;
        copy.ApplyPredictor = ApplyPredictor;
        copy.ApplyByteOrderConversion = ApplyByteOrderConversion;
        copy.UseUnspecifiedExtraSampleAsAlpha = UseUnspecifiedExtraSampleAsAlpha;
        copy.CfaConversionMode = CfaConversionMode;
        copy.UnsupportedFeaturePolicy = UnsupportedFeaturePolicy;
        return copy;
    }
}
