namespace ttf;

/// <summary>Defines resolved COLR sweep-gradient geometry.</summary>
public interface IColorSweepGradient
{
    /// <summary>Gets the center x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetCenterX();
    /// <summary>Gets the center y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetCenterY();
    /// <summary>Gets the start angle in half-turn units.</summary>
    /// <returns>The start angle.</returns>
    double GetStartAngle();
    /// <summary>Gets the end angle in half-turn units.</summary>
    /// <returns>The end angle.</returns>
    double GetEndAngle();
}
