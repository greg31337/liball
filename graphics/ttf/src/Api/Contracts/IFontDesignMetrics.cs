namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontDesignMetrics"/>.</summary>
public interface IFontDesignMetrics
{
    /// <summary>Gets the requested selection policy.</summary>
    /// <returns>The selection policy.</returns>
    FontMetricSelectionPolicy GetSelectionPolicy();

    /// <summary>Gets the selected line-metric source.</summary>
    /// <returns>The selected source.</returns>
    FontLineMetricSource GetLineMetricSource();

    /// <summary>Gets the design-unit scale.</summary>
    /// <returns>Units per em.</returns>
    int GetUnitsPerEm();

    /// <summary>Gets the selected ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    int GetAscender();

    /// <summary>Gets the selected descender.</summary>
    /// <returns>The descender in design units.</returns>
    int GetDescender();

    /// <summary>Gets the selected line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    int GetLineGap();

    /// <summary>Gets the selected line height.</summary>
    /// <returns>Ascender minus descender plus line gap.</returns>
    int GetLineHeight();

    /// <summary>Gets the global head bounds.</summary>
    /// <returns>The global bounds.</returns>
    FontBounds GetGlobalBounds();

    /// <summary>Reports whether cap height and x-height are available.</summary>
    /// <returns>True when OS/2 version 2 fields are present.</returns>
    bool HasCapHeightAndXHeight();

    /// <summary>Gets the cap height.</summary>
    /// <returns>The cap height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Cap height is unavailable.</exception>
    int GetCapHeight();

    /// <summary>Gets the x-height.</summary>
    /// <returns>The x-height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">X-height is unavailable.</exception>
    int GetXHeight();

    /// <summary>Reports whether underline metrics are available.</summary>
    /// <returns>True when post was parsed.</returns>
    bool HasUnderlineMetrics();

    /// <summary>Gets the underline position.</summary>
    /// <returns>The position in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    int GetUnderlinePosition();

    /// <summary>Gets the underline thickness.</summary>
    /// <returns>The thickness in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    int GetUnderlineThickness();

    /// <summary>Reports whether strikeout metrics are available.</summary>
    /// <returns>True when OS/2 was parsed.</returns>
    bool HasStrikeoutMetrics();

    /// <summary>Gets the strikeout position.</summary>
    /// <returns>The position in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    int GetStrikeoutPosition();

    /// <summary>Gets the strikeout thickness.</summary>
    /// <returns>The thickness in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    int GetStrikeoutThickness();

    /// <summary>Reports whether subscript and superscript metrics are available.</summary>
    /// <returns>True when OS/2 script metrics are available.</returns>
    bool HasScriptMetrics();

    /// <summary>Gets the subscript horizontal size.</summary>
    /// <returns>The size in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    int GetSubscriptXSize();

    /// <summary>Gets the subscript vertical size.</summary>
    /// <returns>The size in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    int GetSubscriptYSize();

    /// <summary>Gets the subscript horizontal offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    int GetSubscriptXOffset();

    /// <summary>Gets the subscript vertical offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    int GetSubscriptYOffset();

    /// <summary>Gets the superscript horizontal size.</summary>
    /// <returns>The size in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    int GetSuperscriptXSize();

    /// <summary>Gets the superscript vertical size.</summary>
    /// <returns>The size in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    int GetSuperscriptYSize();

    /// <summary>Gets the superscript horizontal offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    int GetSuperscriptXOffset();

    /// <summary>Gets the superscript vertical offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    int GetSuperscriptYOffset();

    /// <summary>Reports whether horizontal caret metrics are available.</summary>
    /// <returns>True when hhea was parsed.</returns>
    bool HasCaretMetrics();

    /// <summary>Gets the caret slope rise.</summary>
    /// <returns>The unscaled slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    int GetCaretSlopeRise();

    /// <summary>Gets the caret slope run.</summary>
    /// <returns>The unscaled slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    int GetCaretSlopeRun();

    /// <summary>Gets the caret offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    int GetCaretOffset();

    /// <summary>Reports whether vertical global metrics are available.</summary>
    /// <returns>True when vhea was parsed.</returns>
    bool HasVerticalMetrics();

    /// <summary>Gets the vertical ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalAscender();

    /// <summary>Gets the vertical descender.</summary>
    /// <returns>The descender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalDescender();

    /// <summary>Gets the vertical line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalLineGap();

    /// <summary>Gets the vertical line height.</summary>
    /// <returns>Ascender minus descender plus line gap.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalLineHeight();

    /// <summary>Gets the vertical caret slope rise.</summary>
    /// <returns>The unscaled slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalCaretSlopeRise();

    /// <summary>Gets the vertical caret slope run.</summary>
    /// <returns>The unscaled slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalCaretSlopeRun();

    /// <summary>Gets the vertical caret offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalCaretOffset();

    /// <summary>Gets the number of resolved gasp ranges.</summary>
    /// <returns>The range count, or zero when gasp is absent.</returns>
    int GetGaspRangeCount();

    /// <summary>Gets a resolved gasp range maximum.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The maximum pixels per em.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the ranges.</exception>
    int GetGaspRangeMaximumPpem(int index);

    /// <summary>Gets a resolved gasp range behavior mask.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The behavior mask.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the ranges.</exception>
    int GetGaspRangeBehavior(int index);

    /// <summary>Scales all coordinate metrics to a point size.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>A scaled immutable result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>pointSize</c> is negative or non-finite.</exception>
    FontScaledMetrics Scale(double pointSize);
}
