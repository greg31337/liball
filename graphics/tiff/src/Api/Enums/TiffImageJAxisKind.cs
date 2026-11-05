namespace tiff;

/// <summary>Identifies a non-spatial ImageJ hyperstack axis.</summary>
public enum TiffImageJAxisKind
{
    /// <summary>Identifies an unspecified axis.</summary>
    Unknown = 0,
    /// <summary>Identifies the channel axis.</summary>
    Channel = 1,
    /// <summary>Identifies the depth or slice axis.</summary>
    Depth = 2,
    /// <summary>Identifies the time or frame axis.</summary>
    Time = 3
}
