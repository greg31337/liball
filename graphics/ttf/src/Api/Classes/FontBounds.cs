namespace ttf;

/// <summary>Represents an axis-aligned rectangle in font design units.</summary>
public class FontBounds : IFontBounds
{
    int xMin;
    int yMin;
    int xMax;
    int yMax;

    internal FontBounds(int xMin, int yMin, int xMax, int yMax)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
    }

    /// <summary>Gets the minimum x coordinate.</summary>
    /// <returns>The minimum x coordinate.</returns>
    public int GetXMin() { return xMin; }

    /// <summary>Gets the minimum y coordinate.</summary>
    /// <returns>The minimum y coordinate.</returns>
    public int GetYMin() { return yMin; }

    /// <summary>Gets the maximum x coordinate.</summary>
    /// <returns>The maximum x coordinate.</returns>
    public int GetXMax() { return xMax; }

    /// <summary>Gets the maximum y coordinate.</summary>
    /// <returns>The maximum y coordinate.</returns>
    public int GetYMax() { return yMax; }

    /// <summary>Gets the rectangle width.</summary>
    /// <returns>The width in design units.</returns>
    public int GetWidth() { return xMax - xMin; }

    /// <summary>Gets the rectangle height.</summary>
    /// <returns>The height in design units.</returns>
    public int GetHeight() { return yMax - yMin; }
}
