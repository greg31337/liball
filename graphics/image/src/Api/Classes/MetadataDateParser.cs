namespace image;

/// <summary>Parses date text found in common image metadata standards and formats.</summary>
public class MetadataDateParser : IMetadataDateParser
{
    /// <summary>Creates a stateless metadata date parser.</summary>
    public MetadataDateParser()
    {
    }

    /// <summary>Parses EXIF, XMP, IPTC IIM, PNG, TGA, or common EPS date text.</summary>
    /// <param name="text">The source text to parse, or null.</param>
    /// <returns>A result that preserves the original text and reports whether parsing succeeded.</returns>
    public MetadataDate Parse(string? text)
    {
        return MetadataDateParserCore.Parse(text);
    }
}
