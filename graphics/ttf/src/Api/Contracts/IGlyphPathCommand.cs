namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="GlyphPathCommand"/>.</summary>
public interface IGlyphPathCommand
{
    /// <summary>Gets the path operation kind.</summary>
    /// <returns>The operation kind.</returns>
    GlyphPathCommandKind GetKind();

    /// <summary>Gets the first curve control x coordinate.</summary>
    /// <returns>The control x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command is not a curve.</exception>
    double GetControlX();

    /// <summary>Gets the first curve control y coordinate.</summary>
    /// <returns>The control y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command is not a curve.</exception>
    double GetControlY();

    /// <summary>Gets the second cubic control x coordinate.</summary>
    /// <returns>The second control x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command is not cubic.</exception>
    double GetSecondControlX();

    /// <summary>Gets the second cubic control y coordinate.</summary>
    /// <returns>The second control y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command is not cubic.</exception>
    double GetSecondControlY();

    /// <summary>Gets the command endpoint x coordinate.</summary>
    /// <returns>The endpoint x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command closes a contour.</exception>
    double GetEndX();

    /// <summary>Gets the command endpoint y coordinate.</summary>
    /// <returns>The endpoint y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command closes a contour.</exception>
    double GetEndY();
}
