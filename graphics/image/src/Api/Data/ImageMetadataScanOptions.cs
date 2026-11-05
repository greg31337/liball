namespace image;

/// <summary>Controls directory traversal, file filtering, and metadata reader behavior for a scan.</summary>
public class ImageMetadataScanOptions
{
    /// <summary>Indicates whether files in descendant directories are included.</summary>
    public bool Recursive;
    /// <summary>The accepted filename extensions, with or without a leading period, or an empty array to accept every extension.</summary>
    public string[] Extensions;
    /// <summary>The accepted detected image formats, or an empty array to accept every detected format.</summary>
    public ImageFormatKind[] Formats;
    /// <summary>The safety and parser options passed to the metadata reader for every included file.</summary>
    public ImageMetadataReaderOptions ReaderOptions;

    /// <summary>Creates a non-recursive, unfiltered scan using default reader options.</summary>
    public ImageMetadataScanOptions()
    {
        Recursive = false;
        Extensions = new string[0];
        Formats = new ImageFormatKind[0];
        ReaderOptions = new ImageMetadataReaderOptions();
    }
}
