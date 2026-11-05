namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ScaledGlyphMetrics"/>.</summary>
public interface IScaledGlyphMetrics
{
    /// <summary>Gets the source design metrics.</summary>
    /// <returns>The design-unit result.</returns>
    GlyphMetrics GetDesignMetrics();

    /// <summary>Gets the target point size.</summary>
    /// <returns>The point size.</returns>
    double GetPointSize();

    /// <summary>Gets the scaled horizontal advance.</summary>
    /// <returns>The advance.</returns>
    double GetAdvanceWidth();

    /// <summary>Gets the scaled left side bearing.</summary>
    /// <returns>The bearing.</returns>
    double GetLeftSideBearing();

    /// <summary>Gets the scaled vertical advance.</summary>
    /// <returns>The advance.</returns>
    double GetAdvanceHeight();

    /// <summary>Gets the scaled top side bearing.</summary>
    /// <returns>The bearing.</returns>
    double GetTopSideBearing();

    /// <summary>Gets the scaled vertical-origin y coordinate.</summary>
    /// <returns>The y coordinate.</returns>
    double GetVerticalOriginY();
}
