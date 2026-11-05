namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontMetrics"/>.</summary>
public interface IFontMetrics
{
    /// <summary>Gets the design-unit scale.</summary>
    /// <returns>Units per em, or zero when head is unavailable.</returns>
    int GetUnitsPerEm();

    /// <summary>Gets the number of glyphs.</summary>
    /// <returns>The glyph count, or zero when maxp is unavailable.</returns>
    int GetGlyphCount();

    /// <summary>Gets the global head bounds.</summary>
    /// <returns>The bounds, or null when head is unavailable.</returns>
    FontBounds? GetGlobalBounds();

    /// <summary>Reports whether horizontal header metrics are available.</summary>
    /// <returns>True when hhea was parsed.</returns>
    bool HasHorizontalMetrics();

    /// <summary>Gets the hhea ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    int GetHorizontalAscender();

    /// <summary>Gets the hhea descender.</summary>
    /// <returns>The descender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    int GetHorizontalDescender();

    /// <summary>Gets the hhea line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    int GetHorizontalLineGap();

    /// <summary>Gets the maximum horizontal advance.</summary>
    /// <returns>The maximum advance in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    int GetMaximumAdvanceWidth();

    /// <summary>Gets the horizontal caret slope rise.</summary>
    /// <returns>The caret slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    int GetHorizontalCaretSlopeRise();

    /// <summary>Gets the horizontal caret slope run.</summary>
    /// <returns>The caret slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    int GetHorizontalCaretSlopeRun();

    /// <summary>Reports whether vertical header metrics are available.</summary>
    /// <returns>True when vhea was parsed.</returns>
    bool HasVerticalMetrics();

    /// <summary>Gets the vhea ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalAscender();

    /// <summary>Gets the vhea descender.</summary>
    /// <returns>The descender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalDescender();

    /// <summary>Gets the vhea line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalLineGap();

    /// <summary>Gets the maximum vertical advance.</summary>
    /// <returns>The maximum advance in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetMaximumAdvanceHeight();

    /// <summary>Reports whether OS/2 typographic and Windows metrics are available.</summary>
    /// <returns>True when the parsed OS/2 form contains those fields.</returns>
    bool HasOs2Metrics();

    /// <summary>Gets the OS/2 typographic ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    int GetTypographicAscender();

    /// <summary>Gets the OS/2 typographic descender.</summary>
    /// <returns>The descender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    int GetTypographicDescender();

    /// <summary>Gets the OS/2 typographic line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    int GetTypographicLineGap();

    /// <summary>Gets the OS/2 Windows ascent.</summary>
    /// <returns>The unsigned ascent in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    int GetWindowsAscent();

    /// <summary>Gets the OS/2 Windows descent magnitude.</summary>
    /// <returns>The unsigned descent magnitude in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    int GetWindowsDescent();

    /// <summary>Reports whether OS/2 x-height and cap-height fields are available.</summary>
    /// <returns>True for OS/2 version 2 or later.</returns>
    bool HasXHeightAndCapHeight();

    /// <summary>Gets the OS/2 x-height.</summary>
    /// <returns>The x-height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The field is unavailable.</exception>
    int GetXHeight();

    /// <summary>Gets the OS/2 cap height.</summary>
    /// <returns>The cap height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The field is unavailable.</exception>
    int GetCapHeight();

    /// <summary>Reports whether post underline metrics are available.</summary>
    /// <returns>True when post was parsed.</returns>
    bool HasUnderlineMetrics();

    /// <summary>Gets the post underline position.</summary>
    /// <returns>The position in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    int GetUnderlinePosition();

    /// <summary>Gets the post underline thickness.</summary>
    /// <returns>The thickness in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    int GetUnderlineThickness();

    /// <summary>Reports whether OS/2 strikeout metrics are available.</summary>
    /// <returns>True when OS/2 was parsed.</returns>
    bool HasStrikeoutMetrics();

    /// <summary>Gets the OS/2 strikeout position.</summary>
    /// <returns>The position in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    int GetStrikeoutPosition();

    /// <summary>Gets the OS/2 strikeout thickness.</summary>
    /// <returns>The thickness in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    int GetStrikeoutThickness();
}
