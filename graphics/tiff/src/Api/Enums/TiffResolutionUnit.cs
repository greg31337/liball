namespace tiff;

/// <summary>Specifies the unit used by TIFF resolution and position values.</summary>
public enum TiffResolutionUnit
{
    /// <summary>Specifies an unknown unit.</summary>
    Unknown = 0,
    /// <summary>Specifies that no absolute unit is defined.</summary>
    None = 1,
    /// <summary>Specifies inches.</summary>
    Inch = 2,
    /// <summary>Specifies centimeters.</summary>
    Centimeter = 3
}
