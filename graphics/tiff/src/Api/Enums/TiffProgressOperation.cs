namespace tiff;

/// <summary>Specifies the long-running TIFF operation represented by a progress update.</summary>
public enum TiffProgressOperation
{
    /// <summary>Specifies file opening and directory discovery.</summary>
    Open = 0,
    /// <summary>Specifies structural validation.</summary>
    Validation = 1,
    /// <summary>Specifies compression decoding.</summary>
    Decode = 2,
    /// <summary>Specifies rectangular region reading.</summary>
    RegionRead = 3,
    /// <summary>Specifies metadata or raw image-data copying.</summary>
    Copy = 4,
    /// <summary>Specifies raster or container writing.</summary>
    Write = 5
}
