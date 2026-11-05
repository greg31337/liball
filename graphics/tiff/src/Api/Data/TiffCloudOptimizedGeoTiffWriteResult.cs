namespace tiff;

/// <summary>Contains image indices and diagnostics produced while configuring Cloud Optimized GeoTIFF output.</summary>
public class TiffCloudOptimizedGeoTiffWriteResult
{
    /// <summary>Indicates whether the configuration was accepted without errors.</summary>
    public bool Success;
    /// <summary>Contains the top-level full-resolution image index.</summary>
    public int BaseImageIndex;
    /// <summary>Contains child indices for internal overviews in decreasing resolution order.</summary>
    public int[] OverviewImageIndices;
    /// <summary>Contains warnings and errors produced while configuring the writer.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty Cloud Optimized GeoTIFF write result.</summary>
    public TiffCloudOptimizedGeoTiffWriteResult()
    {
        Success = false;
        BaseImageIndex = -1;
        OverviewImageIndices = new int[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffCloudOptimizedGeoTiffWriteResult Create(int baseImageIndex, int[] overviewImageIndices, TiffDiagnostics diagnostics)
    {
        TiffCloudOptimizedGeoTiffWriteResult result = new TiffCloudOptimizedGeoTiffWriteResult();
        result.BaseImageIndex = baseImageIndex;
        result.OverviewImageIndices = CopyIndices(overviewImageIndices);
        result.Diagnostics = diagnostics;
        result.Success = baseImageIndex >= 0 && !diagnostics.HasErrors();
        return result;
    }

    internal static int[] CopyIndices(int[] values)
    {
        if (values == null)
        {
            return new int[0];
        }

        int[] copy = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
