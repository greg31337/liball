namespace ttf;

/// <summary>Exposes one sanitized SVG document selected for a glyph.</summary>
public class ColorSvgDocument : IColorSvgDocument
{
    SfntSvgDocument document;

    internal ColorSvgDocument(SfntSvgDocument document)
    {
        this.document = document;
    }

    /// <summary>Gets the first glyph covered by the shared document.</summary>
    /// <returns>The first glyph identifier.</returns>
    public int GetStartGlyphId() { return document.GetStartGlyphId(); }

    /// <summary>Gets the last glyph covered by the shared document.</summary>
    /// <returns>The last glyph identifier.</returns>
    public int GetEndGlyphId() { return document.GetEndGlyphId(); }

    /// <summary>Reports whether the stored document used gzip compression.</summary>
    /// <returns>True when the encoded bytes are gzip-compressed.</returns>
    public bool IsGzipCompressed() { return document.IsGzipCompressed(); }

    /// <summary>Gets the document's encoded storage compression.</summary>
    /// <returns>Gzip or none.</returns>
    public ColorSvgCompression GetCompression()
    {
        return document.IsGzipCompressed() ?
            ColorSvgCompression.Gzip : ColorSvgCompression.None;
    }

    /// <summary>Gets the encoded document length.</summary>
    /// <returns>The encoded byte count.</returns>
    public int GetEncodedLength() { return document.GetEncodedLength(); }

    /// <summary>Gets the decompressed XML length.</summary>
    /// <returns>The XML byte count.</returns>
    public int GetXmlLength() { return document.GetXmlLength(); }

    /// <summary>Copies the document bytes exactly as encoded in the font.</summary>
    /// <returns>A new encoded byte array.</returns>
    public byte[] CopyEncodedBytes() { return document.CopyEncodedBytes(); }

    /// <summary>Copies the bounded, decompressed SVG XML bytes.</summary>
    /// <returns>A new XML byte array.</returns>
    public byte[] CopyXmlBytes() { return document.CopyXmlBytes(); }

    /// <summary>Loads the SVG with DTD processing and external resolution disabled.</summary>
    /// <returns>A sanitized XML document with an SVG root.</returns>
    /// <exception cref="FontFormatException">The document does not have a valid SVG root.</exception>
    public System.Xml.XmlDocument LoadXml() { return document.LoadXml(); }
}
