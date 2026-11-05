namespace tiff;

/// <summary>Provides ready-to-use TIFF API services without exposing public static methods.</summary>
public static class TiffApi
{
    /// <summary>Creates TIFF readers, writers, and registries.</summary>
    public static readonly ITiffFactory Factory = new TiffFactory();
    /// <summary>Constructs and mutates public TIFF data models.</summary>
    public static readonly ITiffDataFactory Data = new TiffDataFactory();
    /// <summary>Provides common codec and raster operations.</summary>
    public static readonly ITiffCodecService Codec = new TiffCodecService();
    /// <summary>Copies metadata and raw image data.</summary>
    public static readonly ITiffCopyService Copy = new TiffCopyService();
    /// <summary>Parses and formats TIFF and EXIF date/time text.</summary>
    public static readonly ITiffDateTimeService DateTime = new TiffDateTimeService();
    /// <summary>Validates TIFF and Cloud Optimized GeoTIFF structure.</summary>
    public static readonly ITiffValidationService Validation = new TiffValidationService();
    /// <summary>Creates compact TIFF inspection models.</summary>
    public static readonly ITiffInspectionService Inspection = new TiffInspectionService();
}
