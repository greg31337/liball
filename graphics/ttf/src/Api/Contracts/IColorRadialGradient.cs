namespace ttf;

/// <summary>Defines resolved COLR radial-gradient geometry.</summary>
public interface IColorRadialGradient
{
    /// <summary>Gets the start circle's center x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetStartCenterX();
    /// <summary>Gets the start circle's center y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetStartCenterY();
    /// <summary>Gets the start circle's radius.</summary>
    /// <returns>The design-unit radius.</returns>
    double GetStartRadius();
    /// <summary>Gets the end circle's center x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetEndCenterX();
    /// <summary>Gets the end circle's center y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetEndCenterY();
    /// <summary>Gets the end circle's radius.</summary>
    /// <returns>The design-unit radius.</returns>
    double GetEndRadius();
}
