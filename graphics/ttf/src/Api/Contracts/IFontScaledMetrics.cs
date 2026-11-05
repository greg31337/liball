namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontScaledMetrics"/>.</summary>
public interface IFontScaledMetrics
{
    /// <summary>Gets the source design metrics.</summary>
    /// <returns>The design-unit result.</returns>
    FontDesignMetrics GetDesignMetrics();

    /// <summary>Gets the target point size.</summary>
    /// <returns>The point size.</returns>
    double GetPointSize();

    /// <summary>Gets the scaled ascender.</summary>
    /// <returns>The ascender.</returns>
    double GetAscender();

    /// <summary>Gets the scaled descender.</summary>
    /// <returns>The descender.</returns>
    double GetDescender();

    /// <summary>Gets the scaled line gap.</summary>
    /// <returns>The line gap.</returns>
    double GetLineGap();

    /// <summary>Gets the scaled line height.</summary>
    /// <returns>Ascender minus descender plus line gap.</returns>
    double GetLineHeight();

    /// <summary>Gets the scaled global bounds.</summary>
    /// <returns>The bounds.</returns>
    ScaledFontBounds GetGlobalBounds();

    /// <summary>Reports whether cap height and x-height are available.</summary>
    /// <returns>True when both values can be read.</returns>
    bool HasCapHeightAndXHeight();

    /// <summary>Gets the scaled cap height.</summary>
    /// <returns>The cap height.</returns>
    /// <exception cref="System.InvalidOperationException">Cap height is unavailable.</exception>
    double GetCapHeight();

    /// <summary>Gets the scaled x-height.</summary>
    /// <returns>The x-height.</returns>
    /// <exception cref="System.InvalidOperationException">X-height is unavailable.</exception>
    double GetXHeight();

    /// <summary>Reports whether underline metrics are available.</summary>
    /// <returns>True when underline coordinates can be read.</returns>
    bool HasUnderlineMetrics();

    /// <summary>Gets the scaled underline position.</summary>
    /// <returns>The underline position.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    double GetUnderlinePosition();

    /// <summary>Gets the scaled underline thickness.</summary>
    /// <returns>The underline thickness.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    double GetUnderlineThickness();

    /// <summary>Reports whether strikeout metrics are available.</summary>
    /// <returns>True when strikeout coordinates can be read.</returns>
    bool HasStrikeoutMetrics();

    /// <summary>Gets the scaled strikeout position.</summary>
    /// <returns>The strikeout position.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    double GetStrikeoutPosition();

    /// <summary>Gets the scaled strikeout thickness.</summary>
    /// <returns>The strikeout thickness.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    double GetStrikeoutThickness();

    /// <summary>Reports whether subscript and superscript metrics are available.</summary>
    /// <returns>True when script metrics can be read.</returns>
    bool HasScriptMetrics();

    /// <summary>Gets the scaled subscript horizontal size.</summary>
    /// <returns>The scaled size.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    double GetSubscriptXSize();

    /// <summary>Gets the scaled subscript vertical size.</summary>
    /// <returns>The scaled size.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    double GetSubscriptYSize();

    /// <summary>Gets the scaled subscript horizontal offset.</summary>
    /// <returns>The scaled offset.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    double GetSubscriptXOffset();

    /// <summary>Gets the scaled subscript vertical offset.</summary>
    /// <returns>The scaled offset.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    double GetSubscriptYOffset();

    /// <summary>Gets the scaled superscript horizontal size.</summary>
    /// <returns>The scaled size.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    double GetSuperscriptXSize();

    /// <summary>Gets the scaled superscript vertical size.</summary>
    /// <returns>The scaled size.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    double GetSuperscriptYSize();

    /// <summary>Gets the scaled superscript horizontal offset.</summary>
    /// <returns>The scaled offset.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    double GetSuperscriptXOffset();

    /// <summary>Gets the scaled superscript vertical offset.</summary>
    /// <returns>The scaled offset.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    double GetSuperscriptYOffset();

    /// <summary>Reports whether horizontal caret metrics are available.</summary>
    /// <returns>True when caret values can be read.</returns>
    bool HasCaretMetrics();

    /// <summary>Gets the unscaled caret slope rise.</summary>
    /// <returns>The slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    int GetCaretSlopeRise();

    /// <summary>Gets the unscaled caret slope run.</summary>
    /// <returns>The slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    int GetCaretSlopeRun();

    /// <summary>Gets the scaled caret offset.</summary>
    /// <returns>The caret offset.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    double GetCaretOffset();

    /// <summary>Reports whether vertical global metrics are available.</summary>
    /// <returns>True when vertical values can be read.</returns>
    bool HasVerticalMetrics();

    /// <summary>Gets the scaled vertical ascender.</summary>
    /// <returns>The vertical ascender.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    double GetVerticalAscender();

    /// <summary>Gets the scaled vertical descender.</summary>
    /// <returns>The vertical descender.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    double GetVerticalDescender();

    /// <summary>Gets the scaled vertical line gap.</summary>
    /// <returns>The vertical line gap.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    double GetVerticalLineGap();

    /// <summary>Gets the scaled vertical line height.</summary>
    /// <returns>The vertical line height.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    double GetVerticalLineHeight();

    /// <summary>Gets the unscaled vertical caret slope rise.</summary>
    /// <returns>The vertical slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalCaretSlopeRise();

    /// <summary>Gets the unscaled vertical caret slope run.</summary>
    /// <returns>The vertical slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    int GetVerticalCaretSlopeRun();

    /// <summary>Gets the scaled vertical caret offset.</summary>
    /// <returns>The vertical caret offset.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    double GetVerticalCaretOffset();

    /// <summary>Gets the number of resolved gasp ranges.</summary>
    /// <returns>The range count, or zero when gasp is absent.</returns>
    int GetGaspRangeCount();

    /// <summary>Gets a resolved gasp range maximum.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The unscaled maximum pixels per em.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the ranges.</exception>
    int GetGaspRangeMaximumPpem(int index);

    /// <summary>Gets a resolved gasp range behavior mask.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The behavior mask.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the ranges.</exception>
    int GetGaspRangeBehavior(int index);
}
