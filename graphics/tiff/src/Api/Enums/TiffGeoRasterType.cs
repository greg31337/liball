namespace tiff;

/// <summary>Specifies whether GeoTIFF raster coordinates identify pixel areas or pixel points.</summary>
public enum TiffGeoRasterType
{
    /// <summary>Specifies absent or unrecognized raster semantics.</summary>
    Unknown = 0,
    /// <summary>Specifies that integer raster coordinates lie on pixel-area corners.</summary>
    PixelIsArea = 1,
    /// <summary>Specifies that integer raster coordinates identify pixel sample points.</summary>
    PixelIsPoint = 2
}
