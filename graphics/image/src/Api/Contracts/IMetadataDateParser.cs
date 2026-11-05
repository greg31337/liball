namespace image;

/// <summary>Defines parsing of date text found in common image metadata standards and formats.</summary>
public interface IMetadataDateParser
{
    /// <summary>Parses EXIF, XMP, IPTC IIM, PNG, TGA, or common EPS date text.</summary>
    /// <param name="text">The source text to parse, or null.</param>
    /// <returns>A result that preserves the original text and reports whether parsing succeeded.</returns>
    MetadataDate Parse(string? text);
}
