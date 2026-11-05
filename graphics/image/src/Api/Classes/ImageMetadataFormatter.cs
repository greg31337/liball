namespace image;

/// <summary>Formats metadata documents and related values for display or interchange.</summary>
public class ImageMetadataFormatter : IImageMetadataFormatter
{
    /// <summary>Creates a stateless metadata formatter.</summary>
    public ImageMetadataFormatter()
    {
    }

    /// <summary>Formats a metadata document as readable plain text.</summary>
    /// <param name="document">The document to format, or null.</param>
    /// <returns>The plain-text representation.</returns>
    public string ToText(ImageMetadataDocument? document)
    {
        return ImageMetadataFormatterCore.ToText(document);
    }

    /// <summary>Formats a metadata document as JSON.</summary>
    /// <param name="document">The document to format, or null.</param>
    /// <returns>The JSON representation.</returns>
    public string ToJson(ImageMetadataDocument? document)
    {
        return ImageMetadataFormatterCore.ToJson(document);
    }

    /// <summary>Formats a geographic coordinate as invariant-culture display text.</summary>
    /// <param name="coordinate">The coordinate to format, or null.</param>
    /// <returns>The display text, or an empty string for an invalid coordinate.</returns>
    public string ToGpsText(GpsCoordinate? coordinate)
    {
        return GpsCoordinate.FormatDisplay(coordinate);
    }
}
