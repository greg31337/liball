namespace ttf;

/// <summary>Contains selected global metrics scaled to a point size.</summary>
public class FontScaledMetrics : IFontScaledMetrics
{
    FontDesignMetrics designMetrics;
    double pointSize;
    double ascender;
    double descender;
    double lineGap;
    ScaledFontBounds bounds;

    internal FontScaledMetrics(FontDesignMetrics metrics, double pointSize)
    {
        this.designMetrics = metrics;
        this.pointSize = pointSize;
        ascender = Scale(metrics.GetAscender());
        descender = Scale(metrics.GetDescender());
        lineGap = Scale(metrics.GetLineGap());
        FontBounds designBounds = metrics.GetGlobalBounds();
        bounds = new ScaledFontBounds(
            Scale(designBounds.GetXMin()),
            Scale(designBounds.GetYMin()),
            Scale(designBounds.GetXMax()),
            Scale(designBounds.GetYMax()));
    }

    /// <summary>Gets the source design metrics.</summary>
    /// <returns>The design-unit result.</returns>
    public FontDesignMetrics GetDesignMetrics() { return designMetrics; }

    /// <summary>Gets the target point size.</summary>
    /// <returns>The point size.</returns>
    public double GetPointSize() { return pointSize; }

    /// <summary>Gets the scaled ascender.</summary>
    /// <returns>The ascender.</returns>
    public double GetAscender() { return ascender; }

    /// <summary>Gets the scaled descender.</summary>
    /// <returns>The descender.</returns>
    public double GetDescender() { return descender; }

    /// <summary>Gets the scaled line gap.</summary>
    /// <returns>The line gap.</returns>
    public double GetLineGap() { return lineGap; }

    /// <summary>Gets the scaled line height.</summary>
    /// <returns>Ascender minus descender plus line gap.</returns>
    public double GetLineHeight() { return ascender - descender + lineGap; }

    /// <summary>Gets the scaled global bounds.</summary>
    /// <returns>The bounds.</returns>
    public ScaledFontBounds GetGlobalBounds() { return bounds; }

    /// <summary>Reports whether cap height and x-height are available.</summary>
    /// <returns>True when both values can be read.</returns>
    public bool HasCapHeightAndXHeight() { return designMetrics.HasCapHeightAndXHeight(); }

    /// <summary>Gets the scaled cap height.</summary>
    /// <returns>The cap height.</returns>
    /// <exception cref="System.InvalidOperationException">Cap height is unavailable.</exception>
    public double GetCapHeight() { return Scale(designMetrics.GetCapHeight()); }

    /// <summary>Gets the scaled x-height.</summary>
    /// <returns>The x-height.</returns>
    /// <exception cref="System.InvalidOperationException">X-height is unavailable.</exception>
    public double GetXHeight() { return Scale(designMetrics.GetXHeight()); }

    /// <summary>Reports whether underline metrics are available.</summary>
    /// <returns>True when underline coordinates can be read.</returns>
    public bool HasUnderlineMetrics() { return designMetrics.HasUnderlineMetrics(); }

    /// <summary>Gets the scaled underline position.</summary>
    /// <returns>The underline position.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    public double GetUnderlinePosition() { return Scale(designMetrics.GetUnderlinePosition()); }

    /// <summary>Gets the scaled underline thickness.</summary>
    /// <returns>The underline thickness.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    public double GetUnderlineThickness() { return Scale(designMetrics.GetUnderlineThickness()); }

    /// <summary>Reports whether strikeout metrics are available.</summary>
    /// <returns>True when strikeout coordinates can be read.</returns>
    public bool HasStrikeoutMetrics() { return designMetrics.HasStrikeoutMetrics(); }

    /// <summary>Gets the scaled strikeout position.</summary>
    /// <returns>The strikeout position.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    public double GetStrikeoutPosition() { return Scale(designMetrics.GetStrikeoutPosition()); }

    /// <summary>Gets the scaled strikeout thickness.</summary>
    /// <returns>The strikeout thickness.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    public double GetStrikeoutThickness() { return Scale(designMetrics.GetStrikeoutThickness()); }

    /// <summary>Reports whether subscript and superscript metrics are available.</summary>
    /// <returns>True when script metrics can be read.</returns>
    public bool HasScriptMetrics() { return designMetrics.HasScriptMetrics(); }

    /// <summary>Gets the scaled subscript horizontal size.</summary>
    /// <returns>The scaled size.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public double GetSubscriptXSize() { return Scale(designMetrics.GetSubscriptXSize()); }

    /// <summary>Gets the scaled subscript vertical size.</summary>
    /// <returns>The scaled size.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public double GetSubscriptYSize() { return Scale(designMetrics.GetSubscriptYSize()); }

    /// <summary>Gets the scaled subscript horizontal offset.</summary>
    /// <returns>The scaled offset.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public double GetSubscriptXOffset() { return Scale(designMetrics.GetSubscriptXOffset()); }

    /// <summary>Gets the scaled subscript vertical offset.</summary>
    /// <returns>The scaled offset.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public double GetSubscriptYOffset() { return Scale(designMetrics.GetSubscriptYOffset()); }

    /// <summary>Gets the scaled superscript horizontal size.</summary>
    /// <returns>The scaled size.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public double GetSuperscriptXSize() { return Scale(designMetrics.GetSuperscriptXSize()); }

    /// <summary>Gets the scaled superscript vertical size.</summary>
    /// <returns>The scaled size.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public double GetSuperscriptYSize() { return Scale(designMetrics.GetSuperscriptYSize()); }

    /// <summary>Gets the scaled superscript horizontal offset.</summary>
    /// <returns>The scaled offset.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public double GetSuperscriptXOffset() { return Scale(designMetrics.GetSuperscriptXOffset()); }

    /// <summary>Gets the scaled superscript vertical offset.</summary>
    /// <returns>The scaled offset.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public double GetSuperscriptYOffset() { return Scale(designMetrics.GetSuperscriptYOffset()); }

    /// <summary>Reports whether horizontal caret metrics are available.</summary>
    /// <returns>True when caret values can be read.</returns>
    public bool HasCaretMetrics() { return designMetrics.HasCaretMetrics(); }

    /// <summary>Gets the unscaled caret slope rise.</summary>
    /// <returns>The slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    public int GetCaretSlopeRise() { return designMetrics.GetCaretSlopeRise(); }

    /// <summary>Gets the unscaled caret slope run.</summary>
    /// <returns>The slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    public int GetCaretSlopeRun() { return designMetrics.GetCaretSlopeRun(); }

    /// <summary>Gets the scaled caret offset.</summary>
    /// <returns>The caret offset.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    public double GetCaretOffset() { return Scale(designMetrics.GetCaretOffset()); }

    /// <summary>Reports whether vertical global metrics are available.</summary>
    /// <returns>True when vertical values can be read.</returns>
    public bool HasVerticalMetrics() { return designMetrics.HasVerticalMetrics(); }

    /// <summary>Gets the scaled vertical ascender.</summary>
    /// <returns>The vertical ascender.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public double GetVerticalAscender() { return Scale(designMetrics.GetVerticalAscender()); }

    /// <summary>Gets the scaled vertical descender.</summary>
    /// <returns>The vertical descender.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public double GetVerticalDescender() { return Scale(designMetrics.GetVerticalDescender()); }

    /// <summary>Gets the scaled vertical line gap.</summary>
    /// <returns>The vertical line gap.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public double GetVerticalLineGap() { return Scale(designMetrics.GetVerticalLineGap()); }

    /// <summary>Gets the scaled vertical line height.</summary>
    /// <returns>The vertical line height.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public double GetVerticalLineHeight()
    {
        return GetVerticalAscender() - GetVerticalDescender() + GetVerticalLineGap();
    }

    /// <summary>Gets the unscaled vertical caret slope rise.</summary>
    /// <returns>The vertical slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalCaretSlopeRise() { return designMetrics.GetVerticalCaretSlopeRise(); }

    /// <summary>Gets the unscaled vertical caret slope run.</summary>
    /// <returns>The vertical slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalCaretSlopeRun() { return designMetrics.GetVerticalCaretSlopeRun(); }

    /// <summary>Gets the scaled vertical caret offset.</summary>
    /// <returns>The vertical caret offset.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public double GetVerticalCaretOffset() { return Scale(designMetrics.GetVerticalCaretOffset()); }

    /// <summary>Gets the number of resolved gasp ranges.</summary>
    /// <returns>The range count, or zero when gasp is absent.</returns>
    public int GetGaspRangeCount() { return designMetrics.GetGaspRangeCount(); }

    /// <summary>Gets a resolved gasp range maximum.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The unscaled maximum pixels per em.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the ranges.</exception>
    public int GetGaspRangeMaximumPpem(int index)
    {
        return designMetrics.GetGaspRangeMaximumPpem(index);
    }

    /// <summary>Gets a resolved gasp range behavior mask.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The behavior mask.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the ranges.</exception>
    public int GetGaspRangeBehavior(int index)
    {
        return designMetrics.GetGaspRangeBehavior(index);
    }

    double Scale(int value)
    {
        return CheckedFontMath.ScaleDesignUnits(
            value,
            designMetrics.GetUnitsPerEm(),
            pointSize);
    }
}
