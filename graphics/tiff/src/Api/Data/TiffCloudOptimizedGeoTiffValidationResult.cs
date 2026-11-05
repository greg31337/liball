namespace tiff;

/// <summary>Contains the values and diagnostics produced by a Cloud Optimized GeoTIFF validation operation.</summary>
public class TiffCloudOptimizedGeoTiffValidationResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Specifies the strict or permissive policy used by validation.</summary>
    public TiffValidationMode Mode;
    /// <summary>Contains the number of error diagnostics.</summary>
    public int ErrorCount;
    /// <summary>Contains the number of warning diagnostics.</summary>
    public int WarningCount;
    /// <summary>Contains the parsed TIFF header.</summary>
    public TiffHeader Header;
    /// <summary>Contains the image count.</summary>
    public int ImageCount;
    /// <summary>Contains the full-resolution image count.</summary>
    public int FullResolutionImageCount;
    /// <summary>Contains the overview image count.</summary>
    public int OverviewImageCount;
    /// <summary>Contains the number of overview images stored through SubIFDs.</summary>
    public int InternalOverviewImageCount;
    /// <summary>Indicates whether all image directories and tile ranges follow range-friendly ordering.</summary>
    public bool HasRangeFriendlyLayout;
    /// <summary>Contains the first non-sparse tile-data offset.</summary>
    public ulong FirstTileOffset;
    /// <summary>Contains the end offset of the last non-sparse tile range.</summary>
    public ulong LastTileEndOffset;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty Cloud Optimized GeoTIFF validation result.</summary>
    public TiffCloudOptimizedGeoTiffValidationResult()
    {
        Success = false;
        Mode = TiffValidationMode.Strict;
        ErrorCount = 0;
        WarningCount = 0;
        Header = new TiffHeader();
        ImageCount = 0;
        FullResolutionImageCount = 0;
        OverviewImageCount = 0;
        InternalOverviewImageCount = 0;
        HasRangeFriendlyLayout = false;
        FirstTileOffset = 0;
        LastTileEndOffset = 0;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffCloudOptimizedGeoTiffValidationResult Create(TiffHeader header, int imageCount, int fullResolutionImageCount, int overviewCount, TiffValidationMode mode, TiffDiagnostics diagnostics)
    {
        TiffCloudOptimizedGeoTiffValidationResult value = new TiffCloudOptimizedGeoTiffValidationResult();
        value.Initialize(header, imageCount, fullResolutionImageCount, overviewCount, mode, diagnostics);
        return value;
    }

    internal void Initialize(TiffHeader header, int imageCount, int fullResolutionImageCount, int overviewCount, TiffValidationMode mode, TiffDiagnostics diagnostics)
    {
        Header = TiffFile.CopyHeader(header);
        ImageCount = imageCount;
        FullResolutionImageCount = fullResolutionImageCount;
        OverviewImageCount = overviewCount;
        Mode = mode;
        Diagnostics = diagnostics;
        ErrorCount = diagnostics.ErrorCount();
        WarningCount = diagnostics.WarningCount();
        Success = !Diagnostics.HasErrors();
    }
}
