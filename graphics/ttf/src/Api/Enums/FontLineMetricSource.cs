namespace ttf;

/// <summary>Identifies the table fields selected for horizontal line metrics.</summary>
public enum FontLineMetricSource
{
    /// <summary>OS/2 sTypoAscender, sTypoDescender, and sTypoLineGap.</summary>
    Typographic = 0,

    /// <summary>OS/2 usWinAscent and the negated usWinDescent, with zero line gap.</summary>
    Windows = 1,

    /// <summary>hhea ascender, descender, and lineGap.</summary>
    HorizontalHeader = 2
}
