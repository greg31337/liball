namespace ttf;

/// <summary>Represents resolved COLR radial-gradient geometry.</summary>
public class ColorRadialGradient : IColorRadialGradient
{
    double[] values;

    internal ColorRadialGradient(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        if (values.Length != 6)
        {
            throw new System.ArgumentException(
                "Radial-gradient geometry requires six values.",
                "values");
        }
        this.values = values;
    }

    /// <summary>Gets the start circle's center x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetStartCenterX() { return values[0]; }
    /// <summary>Gets the start circle's center y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetStartCenterY() { return values[1]; }
    /// <summary>Gets the start circle's radius.</summary>
    /// <returns>The design-unit radius.</returns>
    public double GetStartRadius() { return values[2]; }
    /// <summary>Gets the end circle's center x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetEndCenterX() { return values[3]; }
    /// <summary>Gets the end circle's center y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetEndCenterY() { return values[4]; }
    /// <summary>Gets the end circle's radius.</summary>
    /// <returns>The design-unit radius.</returns>
    public double GetEndRadius() { return values[5]; }
}
