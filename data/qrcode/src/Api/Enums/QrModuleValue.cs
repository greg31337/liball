namespace qrcode;

/// <summary>Identifies the sampled state of a symbol module.</summary>
public enum QrModuleValue
{
    /// <summary>The module state could not be determined.</summary>
    Unknown = 0,

    /// <summary>The module is light.</summary>
    Light = 1,

    /// <summary>The module is dark.</summary>
    Dark = 2
}
