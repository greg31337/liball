namespace qrcode;

/// <summary>Identifies how luminance samples become a binary image.</summary>
public enum QrBinarizationMode
{
    /// <summary>Selects a binarization strategy automatically.</summary>
    Automatic = 0,

    /// <summary>Uses one threshold derived from the global histogram.</summary>
    GlobalHistogram = 1,

    /// <summary>Uses thresholds derived from local image regions.</summary>
    HybridLocal = 2,

    /// <summary>Uses a binary matrix supplied by the caller.</summary>
    SuppliedBinary = 3
}
