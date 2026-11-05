namespace ttf;

/// <summary>Defines one resolved COLR affine transformation matrix.</summary>
public interface IColorAffineTransform
{
    /// <summary>Gets the x-to-x matrix coefficient.</summary>
    /// <returns>The coefficient.</returns>
    double GetXX();
    /// <summary>Gets the x-to-y matrix coefficient.</summary>
    /// <returns>The coefficient.</returns>
    double GetYX();
    /// <summary>Gets the y-to-x matrix coefficient.</summary>
    /// <returns>The coefficient.</returns>
    double GetXY();
    /// <summary>Gets the y-to-y matrix coefficient.</summary>
    /// <returns>The coefficient.</returns>
    double GetYY();
    /// <summary>Gets the x translation in design units.</summary>
    /// <returns>The x translation.</returns>
    double GetDX();
    /// <summary>Gets the y translation in design units.</summary>
    /// <returns>The y translation.</returns>
    double GetDY();
}
