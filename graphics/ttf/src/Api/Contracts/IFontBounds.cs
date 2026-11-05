namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontBounds"/>.</summary>
public interface IFontBounds
{
    /// <summary>Gets the minimum x coordinate.</summary>
    /// <returns>The minimum x coordinate.</returns>
    int GetXMin();

    /// <summary>Gets the minimum y coordinate.</summary>
    /// <returns>The minimum y coordinate.</returns>
    int GetYMin();

    /// <summary>Gets the maximum x coordinate.</summary>
    /// <returns>The maximum x coordinate.</returns>
    int GetXMax();

    /// <summary>Gets the maximum y coordinate.</summary>
    /// <returns>The maximum y coordinate.</returns>
    int GetYMax();

    /// <summary>Gets the rectangle width.</summary>
    /// <returns>The width in design units.</returns>
    int GetWidth();

    /// <summary>Gets the rectangle height.</summary>
    /// <returns>The height in design units.</returns>
    int GetHeight();
}
