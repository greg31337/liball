namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorSvgDocument"/>.</summary>
public interface IColorSvgDocument
{
    /// <summary>Gets the first glyph covered by the shared document.</summary>
    /// <returns>The first glyph identifier.</returns>
    int GetStartGlyphId();

    /// <summary>Gets the last glyph covered by the shared document.</summary>
    /// <returns>The last glyph identifier.</returns>
    int GetEndGlyphId();

    /// <summary>Reports whether the stored document used gzip compression.</summary>
    /// <returns>True when the encoded bytes are gzip-compressed.</returns>
    bool IsGzipCompressed();

    /// <summary>Gets the document's encoded storage compression.</summary>
    /// <returns>Gzip or none.</returns>
    ColorSvgCompression GetCompression();

    /// <summary>Gets the encoded document length.</summary>
    /// <returns>The encoded byte count.</returns>
    int GetEncodedLength();

    /// <summary>Gets the decompressed XML length.</summary>
    /// <returns>The XML byte count.</returns>
    int GetXmlLength();

    /// <summary>Copies the document bytes exactly as encoded in the font.</summary>
    /// <returns>A new encoded byte array.</returns>
    byte[] CopyEncodedBytes();

    /// <summary>Copies the bounded, decompressed SVG XML bytes.</summary>
    /// <returns>A new XML byte array.</returns>
    byte[] CopyXmlBytes();

    /// <summary>Loads the SVG with DTD processing and external resolution disabled.</summary>
    /// <returns>A sanitized XML document with an SVG root.</returns>
    /// <exception cref="FontFormatException">The document does not have a valid SVG root.</exception>
    System.Xml.XmlDocument LoadXml();
}
