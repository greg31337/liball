namespace tiff;

/// <summary>Specifies the coordinate model declared by <c>GTModelTypeGeoKey</c>.</summary>
public enum TiffGeoModelType
{
    /// <summary>Specifies an absent or unrecognized model type.</summary>
    Unknown = 0,
    /// <summary>Specifies a projected coordinate system.</summary>
    Projected = 1,
    /// <summary>Specifies a geographic latitude/longitude coordinate system.</summary>
    Geographic = 2,
    /// <summary>Specifies a geocentric coordinate system.</summary>
    Geocentric = 3,
    /// <summary>Specifies a user-defined coordinate model.</summary>
    UserDefined = 32767
}
