namespace tiff;

/// <summary>Specifies the sample layout written by a rectangular region read.</summary>
public enum TiffRegionOutput
{
    /// <summary>Preserves the decoded source layout, returning the selected plane for planar-separate images.</summary>
    Source = 0,
    /// <summary>Returns all byte-aligned source samples in host byte order and interleaved pixel order.</summary>
    Normalized = 1
}
