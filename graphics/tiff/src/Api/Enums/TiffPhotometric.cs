namespace tiff;

/// <summary>Specifies the supported photometric values used by TIFF data.</summary>
public enum TiffPhotometric
{
    /// <summary>Specifies white is zero.</summary>
    WhiteIsZero = 0,
    /// <summary>Specifies black is zero.</summary>
    BlackIsZero = 1,
    /// <summary>Specifies RGB.</summary>
    Rgb = 2,
    /// <summary>Specifies palette.</summary>
    Palette = 3,
    /// <summary>Specifies transparency mask.</summary>
    TransparencyMask = 4,
    /// <summary>Specifies CMYK.</summary>
    Cmyk = 5,
    /// <summary>Specifies Y cb cr.</summary>
    YCbCr = 6,
    /// <summary>Specifies cie lab.</summary>
    CieLab = 8,
    /// <summary>Specifies ICC lab.</summary>
    IccLab = 9,
    /// <summary>Specifies itu lab.</summary>
    ItuLab = 10,
    /// <summary>Specifies color filter array.</summary>
    ColorFilterArray = 32803,
    /// <summary>Specifies log L.</summary>
    LogL = 32844,
    /// <summary>Specifies log luv.</summary>
    LogLuv = 32845,
    /// <summary>Specifies linear raw.</summary>
    LinearRaw = 34892
}
