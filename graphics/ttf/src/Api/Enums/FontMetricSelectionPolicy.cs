namespace ttf;

/// <summary>Selects the source of horizontal line metrics.</summary>
public enum FontMetricSelectionPolicy
{
    /// <summary>Use OS/2 typographic metrics when requested, then Windows metrics, then hhea.</summary>
    OpenTypeRecommended = 0,

    /// <summary>Require OS/2 typographic ascender, descender, and line gap.</summary>
    Typographic = 1,

    /// <summary>Require OS/2 Windows ascent and descent with a zero line gap.</summary>
    Windows = 2,

    /// <summary>Require hhea ascender, descender, and line gap.</summary>
    HorizontalHeader = 3
}
