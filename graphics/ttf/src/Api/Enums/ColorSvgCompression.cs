namespace ttf;

/// <summary>Identifies the storage compression of an OpenType SVG document.</summary>
public enum ColorSvgCompression
{
    /// <summary>The encoded bytes contain plain XML.</summary>
    None = 0,
    /// <summary>The encoded bytes contain gzip-compressed XML.</summary>
    Gzip = 1
}
