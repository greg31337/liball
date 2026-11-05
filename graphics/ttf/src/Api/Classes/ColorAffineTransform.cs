namespace ttf;

/// <summary>Represents one resolved COLR affine transformation matrix.</summary>
public class ColorAffineTransform : IColorAffineTransform
{
    double xx;
    double yx;
    double xy;
    double yy;
    double dx;
    double dy;

    internal ColorAffineTransform(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        if (values.Length != 6)
        {
            throw new System.ArgumentException(
                "An affine transform requires six values.",
                "values");
        }

        xx = values[0];
        yx = values[1];
        xy = values[2];
        yy = values[3];
        dx = values[4];
        dy = values[5];
    }

    /// <summary>Gets the x-to-x matrix coefficient.</summary>
    /// <returns>The coefficient.</returns>
    public double GetXX() { return xx; }
    /// <summary>Gets the x-to-y matrix coefficient.</summary>
    /// <returns>The coefficient.</returns>
    public double GetYX() { return yx; }
    /// <summary>Gets the y-to-x matrix coefficient.</summary>
    /// <returns>The coefficient.</returns>
    public double GetXY() { return xy; }
    /// <summary>Gets the y-to-y matrix coefficient.</summary>
    /// <returns>The coefficient.</returns>
    public double GetYY() { return yy; }
    /// <summary>Gets the x translation in design units.</summary>
    /// <returns>The x translation.</returns>
    public double GetDX() { return dx; }
    /// <summary>Gets the y translation in design units.</summary>
    /// <returns>The y translation.</returns>
    public double GetDY() { return dy; }
}
