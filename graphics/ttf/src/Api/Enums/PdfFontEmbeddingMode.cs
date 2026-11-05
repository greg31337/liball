namespace ttf;

/// <summary>Identifies whether the embedded font program is subset or complete.</summary>
public enum PdfFontEmbeddingMode
{
    /// <summary>Embed only used glyphs and their required closure.</summary>
    Subset = 0,

    /// <summary>Embed the complete static font instance.</summary>
    FullFont = 1
}
