namespace ttf;

/// <summary>Represents resolved COLR linear-gradient geometry.</summary>
public class ColorLinearGradient : IColorLinearGradient
{
    double[] values;

    internal ColorLinearGradient(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        if (values.Length != 6)
        {
            throw new System.ArgumentException(
                "Linear-gradient geometry requires six values.",
                "values");
        }
        this.values = values;
    }

    /// <summary>Gets the first point's x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetX0() { return values[0]; }
    /// <summary>Gets the first point's y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetY0() { return values[1]; }
    /// <summary>Gets the second point's x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetX1() { return values[2]; }
    /// <summary>Gets the second point's y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetY1() { return values[3]; }
    /// <summary>Gets the third point's x coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetX2() { return values[4]; }
    /// <summary>Gets the third point's y coordinate.</summary>
    /// <returns>The design-unit coordinate.</returns>
    public double GetY2() { return values[5]; }
}
