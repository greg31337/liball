namespace tiff;

/// <summary>Specifies how color-filter-array samples are converted for RGBA output.</summary>
public enum TiffCfaConversionMode
{
    /// <summary>Reconstructs RGB channels by bilinear interpolation of neighboring CFA sites.</summary>
    Bilinear = 0,
    /// <summary>Returns each CFA sample as a grayscale intensity without demosaicing.</summary>
    Grayscale = 1,
    /// <summary>Rejects CFA input as unsupported for RGBA conversion.</summary>
    Reject = 2
}
