namespace ttf;

/// <summary>Exposes validated unscaled metric sources without selecting a layout policy.</summary>
/// <remarks>
/// Values are expressed in font design units. Source selection and scaling are
/// separate operations so callers can inspect the original OpenType metadata.
/// </remarks>
public class FontMetrics : IFontMetrics
{
    int unitsPerEm;
    int glyphCount;
    FontBounds? globalBounds;
    SfntHheaTable? horizontal;
    SfntVheaTable? vertical;
    SfntOs2Table? os2;
    SfntPostTable? post;

    internal FontMetrics(
        SfntHeadTable? head,
        SfntMaxpTable? maxp,
        SfntHheaTable? horizontal,
        SfntVheaTable? vertical,
        SfntOs2Table? os2,
        SfntPostTable? post)
    {
        if (head != null)
        {
            unitsPerEm = head.GetUnitsPerEm();
            globalBounds = new FontBounds(
                head.GetXMin(), head.GetYMin(), head.GetXMax(), head.GetYMax());
        }
        if (maxp != null) { glyphCount = maxp.GetGlyphCount(); }
        this.horizontal = horizontal;
        this.vertical = vertical;
        this.os2 = os2;
        this.post = post;
    }

    /// <summary>Gets the design-unit scale.</summary>
    /// <returns>Units per em, or zero when head is unavailable.</returns>
    public int GetUnitsPerEm() { return unitsPerEm; }

    /// <summary>Gets the number of glyphs.</summary>
    /// <returns>The glyph count, or zero when maxp is unavailable.</returns>
    public int GetGlyphCount() { return glyphCount; }

    /// <summary>Gets the global head bounds.</summary>
    /// <returns>The bounds, or null when head is unavailable.</returns>
    public FontBounds? GetGlobalBounds() { return globalBounds; }

    /// <summary>Reports whether horizontal header metrics are available.</summary>
    /// <returns>True when hhea was parsed.</returns>
    public bool HasHorizontalMetrics() { return horizontal != null; }

    /// <summary>Gets the hhea ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    public int GetHorizontalAscender() { EnsureHorizontal(); System.ArgumentNullException.ThrowIfNull(horizontal); return horizontal.GetAscender(); }

    /// <summary>Gets the hhea descender.</summary>
    /// <returns>The descender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    public int GetHorizontalDescender() { EnsureHorizontal(); System.ArgumentNullException.ThrowIfNull(horizontal); return horizontal.GetDescender(); }

    /// <summary>Gets the hhea line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    public int GetHorizontalLineGap() { EnsureHorizontal(); System.ArgumentNullException.ThrowIfNull(horizontal); return horizontal.GetLineGap(); }

    /// <summary>Gets the maximum horizontal advance.</summary>
    /// <returns>The maximum advance in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    public int GetMaximumAdvanceWidth() { EnsureHorizontal(); System.ArgumentNullException.ThrowIfNull(horizontal); return horizontal.GetAdvanceWidthMaximum(); }

    /// <summary>Gets the horizontal caret slope rise.</summary>
    /// <returns>The caret slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    public int GetHorizontalCaretSlopeRise() { EnsureHorizontal(); System.ArgumentNullException.ThrowIfNull(horizontal); return horizontal.GetCaretSlopeRise(); }

    /// <summary>Gets the horizontal caret slope run.</summary>
    /// <returns>The caret slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Horizontal metrics are unavailable.</exception>
    public int GetHorizontalCaretSlopeRun() { EnsureHorizontal(); System.ArgumentNullException.ThrowIfNull(horizontal); return horizontal.GetCaretSlopeRun(); }

    /// <summary>Reports whether vertical header metrics are available.</summary>
    /// <returns>True when vhea was parsed.</returns>
    public bool HasVerticalMetrics() { return vertical != null; }

    /// <summary>Gets the vhea ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalAscender() { EnsureVertical(); System.ArgumentNullException.ThrowIfNull(vertical); return vertical.GetAscender(); }

    /// <summary>Gets the vhea descender.</summary>
    /// <returns>The descender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalDescender() { EnsureVertical(); System.ArgumentNullException.ThrowIfNull(vertical); return vertical.GetDescender(); }

    /// <summary>Gets the vhea line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalLineGap() { EnsureVertical(); System.ArgumentNullException.ThrowIfNull(vertical); return vertical.GetLineGap(); }

    /// <summary>Gets the maximum vertical advance.</summary>
    /// <returns>The maximum advance in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetMaximumAdvanceHeight() { EnsureVertical(); System.ArgumentNullException.ThrowIfNull(vertical); return vertical.GetAdvanceHeightMaximum(); }

    /// <summary>Reports whether OS/2 typographic and Windows metrics are available.</summary>
    /// <returns>True when the parsed OS/2 form contains those fields.</returns>
    public bool HasOs2Metrics() { return os2 != null && os2.HasTypographicMetrics(); }

    /// <summary>Gets the OS/2 typographic ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    public int GetTypographicAscender() { EnsureOs2Metrics(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetTypographicAscender(); }

    /// <summary>Gets the OS/2 typographic descender.</summary>
    /// <returns>The descender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    public int GetTypographicDescender() { EnsureOs2Metrics(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetTypographicDescender(); }

    /// <summary>Gets the OS/2 typographic line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    public int GetTypographicLineGap() { EnsureOs2Metrics(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetTypographicLineGap(); }

    /// <summary>Gets the OS/2 Windows ascent.</summary>
    /// <returns>The unsigned ascent in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    public int GetWindowsAscent() { EnsureOs2Metrics(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetWindowsAscent(); }

    /// <summary>Gets the OS/2 Windows descent magnitude.</summary>
    /// <returns>The unsigned descent magnitude in design units.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metrics are unavailable.</exception>
    public int GetWindowsDescent() { EnsureOs2Metrics(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetWindowsDescent(); }

    /// <summary>Reports whether OS/2 x-height and cap-height fields are available.</summary>
    /// <returns>True for OS/2 version 2 or later.</returns>
    public bool HasXHeightAndCapHeight() { return os2 != null && os2.HasVersion2Metrics(); }

    /// <summary>Gets the OS/2 x-height.</summary>
    /// <returns>The x-height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The field is unavailable.</exception>
    public int GetXHeight() { EnsureVersion2Metrics(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetXHeight(); }

    /// <summary>Gets the OS/2 cap height.</summary>
    /// <returns>The cap height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The field is unavailable.</exception>
    public int GetCapHeight() { EnsureVersion2Metrics(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetCapHeight(); }

    /// <summary>Reports whether post underline metrics are available.</summary>
    /// <returns>True when post was parsed.</returns>
    public bool HasUnderlineMetrics() { return post != null; }

    /// <summary>Gets the post underline position.</summary>
    /// <returns>The position in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    public int GetUnderlinePosition() { EnsurePost(); System.ArgumentNullException.ThrowIfNull(post); return post.GetUnderlinePosition(); }

    /// <summary>Gets the post underline thickness.</summary>
    /// <returns>The thickness in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    public int GetUnderlineThickness() { EnsurePost(); System.ArgumentNullException.ThrowIfNull(post); return post.GetUnderlineThickness(); }

    /// <summary>Reports whether OS/2 strikeout metrics are available.</summary>
    /// <returns>True when OS/2 was parsed.</returns>
    public bool HasStrikeoutMetrics() { return os2 != null; }

    /// <summary>Gets the OS/2 strikeout position.</summary>
    /// <returns>The position in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    public int GetStrikeoutPosition() { EnsureOs2(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetStrikeoutPosition(); }

    /// <summary>Gets the OS/2 strikeout thickness.</summary>
    /// <returns>The thickness in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    public int GetStrikeoutThickness() { EnsureOs2(); System.ArgumentNullException.ThrowIfNull(os2); return os2.GetStrikeoutSize(); }

    void EnsureHorizontal()
    {
        if (horizontal == null) { throw new System.InvalidOperationException("Horizontal metrics are unavailable."); }
    }

    void EnsureVertical()
    {
        if (vertical == null) { throw new System.InvalidOperationException("Vertical metrics are unavailable."); }
    }

    void EnsureOs2()
    {
        if (os2 == null) { throw new System.InvalidOperationException("OS/2 metrics are unavailable."); }
    }

    void EnsureOs2Metrics()
    {
        if (!HasOs2Metrics()) { throw new System.InvalidOperationException("OS/2 typographic metrics are unavailable."); }
    }

    void EnsureVersion2Metrics()
    {
        if (!HasXHeightAndCapHeight()) { throw new System.InvalidOperationException("OS/2 x-height and cap-height are unavailable."); }
    }

    void EnsurePost()
    {
        if (post == null) { throw new System.InvalidOperationException("Underline metrics are unavailable."); }
    }
}
