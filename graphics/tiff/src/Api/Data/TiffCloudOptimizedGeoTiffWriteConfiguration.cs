namespace tiff;

/// <summary>Defines a tiled georeferenced base image and its decreasing-resolution internal overviews.</summary>
public class TiffCloudOptimizedGeoTiffWriteConfiguration
{
    /// <summary>Contains the full-resolution tiled image configuration.</summary>
    public TiffWriterImageConfiguration BaseImage;
    /// <summary>Contains tiled overview configurations in decreasing resolution order.</summary>
    public TiffWriterImageConfiguration[] Overviews;
    /// <summary>Contains the full-resolution geographic model metadata.</summary>
    public TiffGeoModelMetadata Model;
    /// <summary>Contains the full-resolution GeoKey directory.</summary>
    public TiffGeoKeyDirectory KeyDirectory;
    /// <summary>Contains the GeoTIFF double and ASCII parameter pools.</summary>
    public TiffGeoParameterMetadata Parameters;

    /// <summary>Creates an empty Cloud Optimized GeoTIFF write configuration.</summary>
    public TiffCloudOptimizedGeoTiffWriteConfiguration()
    {
        BaseImage = new TiffWriterImageConfiguration();
        Overviews = new TiffWriterImageConfiguration[0];
        Model = new TiffGeoModelMetadata();
        KeyDirectory = new TiffGeoKeyDirectory();
        Parameters = new TiffGeoParameterMetadata();
    }
}
