namespace ttf;

/// <summary>Represents exact scaled axis-aligned ink bounds.</summary>
public class TextInkBounds : ITextInkBounds
{
    bool empty;
    double xMinimum;
    double yMinimum;
    double xMaximum;
    double yMaximum;

    internal static TextInkBounds Create()
    {
        TextInkBounds bounds = new TextInkBounds(0, 0, 0, 0);
        bounds.empty = true;
        return bounds;
    }

    internal TextInkBounds(
        double xMinimum,
        double yMinimum,
        double xMaximum,
        double yMaximum)
    {
        empty = false;
        this.xMinimum = xMinimum;
        this.yMinimum = yMinimum;
        this.xMaximum = xMaximum;
        this.yMaximum = yMaximum;
    }

    internal static TextInkBounds Create(
        double xMinimum,
        double yMinimum,
        double xMaximum,
        double yMaximum)
    {
        return new TextInkBounds(
            xMinimum,
            yMinimum,
            xMaximum,
            yMaximum);
    }

    /// <summary>Reports whether no outline contributes ink.</summary>
    /// <returns>True when the bounds are empty.</returns>
    public bool IsEmpty()
    {
        return empty;
    }

    /// <summary>Gets the exact scaled minimum x coordinate.</summary>
    /// <returns>The minimum x coordinate.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetXMinimum()
    {
        EnsureNotEmpty();
        return xMinimum;
    }

    /// <summary>Gets the exact scaled minimum y coordinate.</summary>
    /// <returns>The minimum y coordinate.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetYMinimum()
    {
        EnsureNotEmpty();
        return yMinimum;
    }

    /// <summary>Gets the exact scaled maximum x coordinate.</summary>
    /// <returns>The maximum x coordinate.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetXMaximum()
    {
        EnsureNotEmpty();
        return xMaximum;
    }

    /// <summary>Gets the exact scaled maximum y coordinate.</summary>
    /// <returns>The maximum y coordinate.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetYMaximum()
    {
        EnsureNotEmpty();
        return yMaximum;
    }

    /// <summary>Gets the exact scaled ink width.</summary>
    /// <returns>The width.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    public double GetWidth()
    {
        return GetXMaximum() - GetXMinimum();
    }

    /// <summary>Gets the exact scaled ink height.</summary>
    /// <returns>The height.</returns>
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
                "Empty text ink has no coordinate bounds.");
        }
    }
}
