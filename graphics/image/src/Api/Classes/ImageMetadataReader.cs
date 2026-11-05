using System.IO;

namespace image;

/// <summary>Reads structured metadata from image files and streams.</summary>
public class ImageMetadataReader : IImageMetadataReader
{
    /// <summary>Creates a stateless image metadata reader.</summary>
    public ImageMetadataReader()
    {
    }

    /// <summary>Reads metadata from a file using default options.</summary>
    /// <param name="path">The image path, or null.</param>
    /// <returns>The extracted metadata document.</returns>
    public ImageMetadataDocument ReadFile(string? path)
    {
        return ImageMetadataReaderCore.ReadFile(path);
    }

    /// <summary>Reads metadata from a file using caller-supplied options.</summary>
    /// <param name="path">The image path, or null.</param>
    /// <param name="options">The options to apply, or null for defaults.</param>
    /// <returns>The extracted metadata document.</returns>
    public ImageMetadataDocument ReadFileWithOptions(string? path, ImageMetadataReaderOptions? options)
    {
        return ImageMetadataReaderCore.ReadFileWithOptions(path, options);
    }

    /// <summary>Reads metadata from a stream using default options.</summary>
    /// <param name="stream">The readable image stream, or null.</param>
    /// <returns>The extracted metadata document.</returns>
    public ImageMetadataDocument ReadStream(Stream? stream)
    {
        return ImageMetadataReaderCore.ReadStream(stream);
    }

    /// <summary>Reads metadata from a stream using caller-supplied options.</summary>
    /// <param name="stream">The readable image stream, or null.</param>
    /// <param name="options">The options to apply, or null for defaults.</param>
    /// <returns>The extracted metadata document.</returns>
    public ImageMetadataDocument ReadStreamWithOptions(Stream? stream, ImageMetadataReaderOptions? options)
    {
        return ImageMetadataReaderCore.ReadStreamWithOptions(stream, options);
    }
}
