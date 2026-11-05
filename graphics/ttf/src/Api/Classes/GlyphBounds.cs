namespace ttf;

/// <summary>Represents exact axis-aligned glyph path bounds in design units.</summary>
public class GlyphBounds : IGlyphBounds
{
    bool empty;
    double xMinimum;
    double yMinimum;
    double xMaximum;
    double yMaximum;

    internal GlyphBounds(SfntPathBounds bounds)
    {
        empty = bounds.IsEmpty();

        if (!empty)
        {
            xMinimum = bounds.GetXMinimum();
            yMinimum = bounds.GetYMinimum();
            xMaximum = bounds.GetXMaximum();
            yMaximum = bounds.GetYMaximum();
        }
    }

    /// <summary>Reports whether the outline contains no path coordinates.</summary>
    /// <returns>True when the bounds are empty.</returns>
    public bool IsEmpty()
    {
        return empty;
    }

    /// <summary>Gets the exact minimum x coordinate.</summary>
    /// <returns>The minimum x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetXMinimum()
    {
        EnsureNotEmpty();
        return xMinimum;
    }

    /// <summary>Gets the exact minimum y coordinate.</summary>
    /// <returns>The minimum y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetYMinimum()
    {
        EnsureNotEmpty();
        return yMinimum;
    }

    /// <summary>Gets the exact maximum x coordinate.</summary>
    /// <returns>The maximum x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetXMaximum()
    {
        EnsureNotEmpty();
        return xMaximum;
    }

    /// <summary>Gets the exact maximum y coordinate.</summary>
    /// <returns>The maximum y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetYMaximum()
    {
        EnsureNotEmpty();
        return yMaximum;
    }

    /// <summary>Gets the exact bounds width.</summary>
    /// <returns>The width in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetWidth()
    {
        return GetXMaximum() - GetXMinimum();
    }

    /// <summary>Gets the exact bounds height.</summary>
    /// <returns>The height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetHeight()
    {
        return GetYMaximum() - GetYMinimum();
    }

    void EnsureNotEmpty()
    {
        if (empty)
        {
            throw new System.InvalidOperationException(
                "An empty glyph outline has no coordinate bounds.");
        }
    }
}
