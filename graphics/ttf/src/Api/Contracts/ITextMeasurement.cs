namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="TextMeasurement"/>.</summary>
public interface ITextMeasurement
{
    /// <summary>Gets the point size used for scaling.</summary>
    /// <returns>The point size.</returns>
    double GetPointSize();

    /// <summary>Gets the signed horizontal advance.</summary>
    /// <returns>The horizontal advance.</returns>
    double GetXAdvance();

    /// <summary>Gets the signed vertical advance.</summary>
    /// <returns>The vertical advance.</returns>
    double GetYAdvance();

    /// <summary>Gets the exact scaled positioned ink bounds.</summary>
    /// <returns>The immutable ink bounds.</returns>
    TextInkBounds GetInkBounds();
}
