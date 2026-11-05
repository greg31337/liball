namespace ttf;

/// <summary>Identifies the source used to derive a PDF descriptor metric.</summary>
public enum PdfFontDescriptorMetricSource
{
    /// <summary>No reliable source value was available.</summary>
    Unavailable = 0,

    /// <summary>The OpenType <c>head</c> table supplied the value.</summary>
    Head = 1,

    /// <summary>The recommended OpenType line-metric selection supplied the value.</summary>
    RecommendedLineMetrics = 2,

    /// <summary>The OpenType OS/2 table supplied the value.</summary>
    Os2 = 3,

    /// <summary>The bounds of a cmap-selected glyph supplied the value.</summary>
    CmapGlyphBounds = 4,

    /// <summary>The selected ascent supplied a required fallback value.</summary>
    AscentFallback = 5,

    /// <summary>The OpenType <c>post</c> table supplied the value.</summary>
    Post = 6,

    /// <summary>The CFF Top DICT supplied the value.</summary>
    CffTopDictionary = 7,

    /// <summary>The embedded <c>.notdef</c> advance supplied the value.</summary>
    NotdefAdvance = 8,

    /// <summary>The CFF standard vertical stem width supplied the value.</summary>
    CffStandardVerticalWidth = 9,

    /// <summary>The OS/2 weight class supplied a stable stem estimate.</summary>
    Os2WeightClass = 10,

    /// <summary>A conservative normal-weight stem estimate supplied the value.</summary>
    DefaultStemEstimate = 11,

    /// <summary>The selected <c>slnt</c> variation coordinate supplied the value.</summary>
    VariationSlantAxis = 12,

    /// <summary>The selected <c>wght</c> variation coordinate supplied the value.</summary>
    VariationWeightAxis = 13,

    /// <summary>The resolved instance glyph outlines supplied the value.</summary>
    ResolvedGlyphBounds = 14
}
