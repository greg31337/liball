namespace tiff;

/// <summary>Identifies the resolved storage form of a GeoTIFF key value.</summary>
public enum TiffGeoKeyValueKind
{
    /// <summary>Specifies an unresolved value.</summary>
    None = 0,
    /// <summary>Specifies one or more unsigned SHORT values represented as integers.</summary>
    Short = 1,
    /// <summary>Specifies one or more DOUBLE parameter values.</summary>
    Double = 2,
    /// <summary>Specifies an ASCII parameter value.</summary>
    Ascii = 3
}
