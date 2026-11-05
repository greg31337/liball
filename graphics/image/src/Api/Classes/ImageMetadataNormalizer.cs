namespace image;

/// <summary>Builds normalized summaries from extracted metadata.</summary>
public class ImageMetadataNormalizer : IImageMetadataNormalizer
{
    /// <summary>Creates a stateless metadata normalizer.</summary>
    public ImageMetadataNormalizer()
    {
    }

    /// <summary>Builds and assigns a normalized summary for a metadata document.</summary>
    /// <param name="document">The document to summarize, or null.</param>
    /// <returns>The generated summary.</returns>
    public ImageMetadataSummary BuildSummary(ImageMetadataDocument? document)
    {
        return ImageMetadataNormalizerCore.BuildSummary(document);
    }
}
