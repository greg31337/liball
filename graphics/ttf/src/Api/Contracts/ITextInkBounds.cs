namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="TextInkBounds"/>.</summary>
public interface ITextInkBounds
{
    /// <summary>Reports whether no outline contributes ink.</summary>
    /// <returns>True when the bounds are empty.</returns>
    bool IsEmpty();

    /// <summary>Gets the exact scaled minimum x coordinate.</summary>
    /// <returns>The minimum x coordinate.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetXMinimum();

    /// <summary>Gets the exact scaled minimum y coordinate.</summary>
    /// <returns>The minimum y coordinate.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetYMinimum();

    /// <summary>Gets the exact scaled maximum x coordinate.</summary>
    /// <returns>The maximum x coordinate.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetXMaximum();

    /// <summary>Gets the exact scaled maximum y coordinate.</summary>
    /// <returns>The maximum y coordinate.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetYMaximum();

    /// <summary>Gets the exact scaled ink width.</summary>
    /// <returns>The width.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetWidth();

    /// <summary>Gets the exact scaled ink height.</summary>
    /// <returns>The height.</returns>
    /// <exception cref="System.InvalidOperationException">The bounds are empty.</exception>
    double GetHeight();
}
