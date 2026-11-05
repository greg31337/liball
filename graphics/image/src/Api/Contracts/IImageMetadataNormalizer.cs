namespace image;

/// <summary>Defines operations that build normalized metadata summaries.</summary>
public interface IImageMetadataNormalizer
{
    /// <summary>Builds and assigns a normalized summary for a metadata document.</summary>
    /// <param name="document">The document to summarize, or null.</param>
    /// <returns>The generated summary.</returns>
    ImageMetadataSummary BuildSummary(ImageMetadataDocument? document);
}
