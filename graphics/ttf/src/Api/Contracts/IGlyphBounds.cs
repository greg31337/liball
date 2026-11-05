namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="GlyphBounds"/>.</summary>
public interface IGlyphBounds
{
    /// <summary>Reports whether the outline contains no path coordinates.</summary>
    /// <returns>True when the bounds are empty.</returns>
    bool IsEmpty();

    /// <summary>Gets the exact minimum x coordinate.</summary>
    /// <returns>The minimum x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetXMinimum();

    /// <summary>Gets the exact minimum y coordinate.</summary>
    /// <returns>The minimum y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetYMinimum();

    /// <summary>Gets the exact maximum x coordinate.</summary>
    /// <returns>The maximum x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetXMaximum();

    /// <summary>Gets the exact maximum y coordinate.</summary>
    /// <returns>The maximum y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetYMaximum();

    /// <summary>Gets the exact bounds width.</summary>
    /// <returns>The width in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetWidth();

    /// <summary>Gets the exact bounds height.</summary>
    /// <returns>The height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetHeight();
}
