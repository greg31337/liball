namespace ttf;

/// <summary>Represents resolved COLR sweep-gradient geometry.</summary>
public class ColorSweepGradient : IColorSweepGradient
{
    double[] values;

    internal ColorSweepGradient(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        if (values.Length != 4)
        {
            throw new System.ArgumentException(
                "Sweep-gradient geometry requires four values.",
                "values");
        }
        this.values = values;
    }

    /// <summary>Gets the center x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetCenterX() { return values[0]; }
    /// <summary>Gets the center y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetCenterY() { return values[1]; }
    /// <summary>Gets the start angle in half-turn units.</summary>
    /// <returns>The start angle.</returns>
    public double GetStartAngle() { return values[2]; }
    /// <summary>Gets the end angle in half-turn units.</summary>
    /// <returns>The end angle.</returns>
    public double GetEndAngle() { return values[3]; }
}
