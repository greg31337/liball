namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ScaledFontBounds"/>.</summary>
public interface IScaledFontBounds
{
    /// <summary>Gets the scaled minimum x coordinate.</summary>
    /// <returns>The minimum x coordinate.</returns>
    double GetXMin();

    /// <summary>Gets the scaled minimum y coordinate.</summary>
    /// <returns>The minimum y coordinate.</returns>
    double GetYMin();

    /// <summary>Gets the scaled maximum x coordinate.</summary>
    /// <returns>The maximum x coordinate.</returns>
    double GetXMax();

    /// <summary>Gets the scaled maximum y coordinate.</summary>
    /// <returns>The maximum y coordinate.</returns>
    double GetYMax();

    /// <summary>Gets the scaled width.</summary>
    /// <returns>The width.</returns>
    double GetWidth();

    /// <summary>Gets the scaled height.</summary>
    /// <returns>The height.</returns>
    double GetHeight();
}
