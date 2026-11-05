namespace image;

/// <summary>Defines text formatting operations for image metadata values.</summary>
public interface IImageMetadataFormatter
{
    /// <summary>Formats a metadata document as readable plain text.</summary>
    /// <param name="document">The document to format, or null.</param>
    /// <returns>The plain-text representation.</returns>
    string ToText(ImageMetadataDocument? document);

    /// <summary>Formats a metadata document as JSON.</summary>
    /// <param name="document">The document to format, or null.</param>
    /// <returns>The JSON representation.</returns>
    string ToJson(ImageMetadataDocument? document);

    /// <summary>Formats a geographic coordinate as invariant-culture display text.</summary>
    /// <param name="coordinate">The coordinate to format, or null.</param>
    /// <returns>The display text, or an empty string for an invalid coordinate.</returns>
    string ToGpsText(GpsCoordinate? coordinate);
}
