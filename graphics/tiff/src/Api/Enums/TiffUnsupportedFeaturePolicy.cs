namespace tiff;

/// <summary>Specifies the supported unsupported feature policy values used by TIFF data.</summary>
public enum TiffUnsupportedFeaturePolicy
{
    /// <summary>Specifies error.</summary>
    Error = 0,
    /// <summary>Specifies preserve raw.</summary>
    PreserveRaw = 1,
    /// <summary>Specifies skip.</summary>
    Skip = 2,
    /// <summary>Specifies ignore.</summary>
    Ignore = 3
}
