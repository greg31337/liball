namespace ttf;

/// <summary>Defines resolved COLR linear-gradient geometry.</summary>
public interface IColorLinearGradient
{
    /// <summary>Gets the first point's x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetX0();
    /// <summary>Gets the first point's y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetY0();
    /// <summary>Gets the second point's x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetX1();
    /// <summary>Gets the second point's y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetY1();
    /// <summary>Gets the third point's x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetX2();
    /// <summary>Gets the third point's y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    double GetY2();
}
