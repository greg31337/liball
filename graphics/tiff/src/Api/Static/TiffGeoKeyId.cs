namespace tiff;

/// <summary>Defines commonly used public GeoTIFF key identifiers.</summary>
public static class TiffGeoKeyId
{
    /// <summary>Identifies <c>GTModelTypeGeoKey</c>.</summary>
    public const int ModelType = 1024;
    /// <summary>Identifies <c>GTRasterTypeGeoKey</c>.</summary>
    public const int RasterType = 1025;
    /// <summary>Identifies <c>GTCitationGeoKey</c>.</summary>
    public const int Citation = 1026;
    /// <summary>Identifies <c>GeographicTypeGeoKey</c>.</summary>
    public const int GeographicCrs = 2048;
    /// <summary>Identifies <c>GeogCitationGeoKey</c>.</summary>
    public const int GeographicCitation = 2049;
    /// <summary>Identifies <c>ProjectedCRSGeoKey</c>.</summary>
    public const int ProjectedCrs = 3072;
    /// <summary>Identifies <c>PCSCitationGeoKey</c>.</summary>
    public const int ProjectedCitation = 3073;
    /// <summary>Identifies <c>VerticalGeoKey</c>.</summary>
    public const int VerticalCrs = 4096;
    /// <summary>Identifies <c>VerticalCitationGeoKey</c>.</summary>
    public const int VerticalCitation = 4097;
}
