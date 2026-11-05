namespace ttf;

/// <summary>Identifies whether output contains a subset or the complete font instance.</summary>
public enum FontSubsetEmbeddingMode
{
    /// <summary>Emit only requested glyphs and their required closure.</summary>
    Subset = 0,

    /// <summary>Emit the complete static font instance without removing glyphs.</summary>
    FullFont = 1
}
