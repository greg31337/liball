namespace tiff;

/// <summary>Specifies the supported data type values used by TIFF data.</summary>
public enum TiffDataType
{
    /// <summary>Specifies unknown.</summary>
    Unknown = 0,
    /// <summary>Specifies byte.</summary>
    Byte = 1,
    /// <summary>Specifies ASCII.</summary>
    Ascii = 2,
    /// <summary>Specifies short.</summary>
    Short = 3,
    /// <summary>Specifies long.</summary>
    Long = 4,
    /// <summary>Specifies rational.</summary>
    Rational = 5,
    /// <summary>Specifies S byte.</summary>
    SByte = 6,
    /// <summary>Specifies undefined.</summary>
    Undefined = 7,
    /// <summary>Specifies S short.</summary>
    SShort = 8,
    /// <summary>Specifies S long.</summary>
    SLong = 9,
    /// <summary>Specifies S rational.</summary>
    SRational = 10,
    /// <summary>Specifies float.</summary>
    Float = 11,
    /// <summary>Specifies double.</summary>
    Double = 12,
    /// <summary>Specifies IFD.</summary>
    Ifd = 13,
    /// <summary>Specifies long8.</summary>
    Long8 = 16,
    /// <summary>Specifies S long8.</summary>
    SLong8 = 17,
    /// <summary>Specifies IFD 8.</summary>
    Ifd8 = 18
}
