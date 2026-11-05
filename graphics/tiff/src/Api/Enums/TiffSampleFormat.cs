namespace tiff;

/// <summary>Specifies the supported sample format values used by TIFF data.</summary>
public enum TiffSampleFormat
{
    /// <summary>Specifies unknown.</summary>
    Unknown = 0,
    /// <summary>Specifies unsigned integer.</summary>
    UnsignedInteger = 1,
    /// <summary>Specifies signed integer.</summary>
    SignedInteger = 2,
    /// <summary>Specifies ieee floating point.</summary>
    IeeeFloatingPoint = 3,
    /// <summary>Specifies undefined.</summary>
    Undefined = 4,
    /// <summary>Specifies complex integer.</summary>
    ComplexInteger = 5,
    /// <summary>Specifies complex ieee floating point.</summary>
    ComplexIeeeFloatingPoint = 6
}
