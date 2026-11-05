namespace ttf;

/// <summary>Represents scaled axis-aligned font bounds.</summary>
public class ScaledFontBounds : IScaledFontBounds
{
    double xMin;
    double yMin;
    double xMax;
    double yMax;

    internal ScaledFontBounds(double xMin, double yMin, double xMax, double yMax)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
    }

    /// <summary>Gets the scaled minimum x coordinate.</summary>
    /// <returns>The minimum x coordinate.</returns>
    public double GetXMin() { return xMin; }

    /// <summary>Gets the scaled minimum y coordinate.</summary>
    /// <returns>The minimum y coordinate.</returns>
    public double GetYMin() { return yMin; }

    /// <summary>Gets the scaled maximum x coordinate.</summary>
    /// <returns>The maximum x coordinate.</returns>
    public double GetXMax() { return xMax; }

    /// <summary>Gets the scaled maximum y coordinate.</summary>
    /// <returns>The maximum y coordinate.</returns>
    public double GetYMax() { return yMax; }

    /// <summary>Gets the scaled width.</summary>
    /// <returns>The width.</returns>
    public double GetWidth() { return xMax - xMin; }

    /// <summary>Gets the scaled height.</summary>
    /// <returns>The height.</returns>
    public double GetHeight() { return yMax - yMin; }
}
