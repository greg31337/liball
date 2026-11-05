namespace chart;
/// <summary>Stores PNG color, filtering, compression, and metadata settings.</summary>
public class ChartPngOptions
{
    /// <summary>The output PNG color encoding.</summary>
    public ChartPngColorType ColorType;
    /// <summary>The scanline filter selection.</summary>
    public ChartPngFilter Filter;
    /// <summary>The built-in DEFLATE compression setting.</summary>
    public ChartPngCompression Compression;
    /// <summary>The textual metadata written before image data.</summary>
    public ChartPngMetadata Metadata;
    /// <summary>Initializes automatic-color PNG options with adaptive filtering and optimal compression.</summary>
    public ChartPngOptions()
    {
        ColorType = ChartPngColorType.Automatic;
        Filter = ChartPngFilter.Adaptive;
        Compression = ChartPngCompression.Optimal;
        Metadata = new ChartPngMetadata();
    }
}
