namespace ttf;

/// <summary>Defines inspection of the OpenType gasp table.</summary>
public interface IFontGridFittingBehavior
{
    /// <summary>Gets the table version.</summary>
    /// <returns>The version.</returns>
    int GetVersion();

    /// <summary>Gets the number of behavior ranges.</summary>
    /// <returns>The range count.</returns>
    int GetRangeCount();

    /// <summary>Gets one range's inclusive maximum pixels per em.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The maximum pixels per em.</returns>
    int GetMaximumPixelsPerEm(int index);

    /// <summary>Gets one range's raw behavior mask.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The behavior mask.</returns>
    int GetBehavior(int index);

    /// <summary>Reports whether a range requests grid fitting.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>True when requested.</returns>
    bool UsesGridFitting(int index);

    /// <summary>Reports whether a range requests grayscale rendering.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>True when requested.</returns>
    bool UsesGrayscaleRendering(int index);

    /// <summary>Reports whether a range requests symmetric grid fitting.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>True when requested.</returns>
    bool UsesSymmetricGridFitting(int index);

    /// <summary>Reports whether a range requests symmetric smoothing.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>True when requested.</returns>
    bool UsesSymmetricSmoothing(int index);

    /// <summary>Finds the behavior range for a pixels-per-em size.</summary>
    /// <param name="pixelsPerEm">The size from zero through 65535.</param>
    /// <returns>The range index, or negative one when absent.</returns>
    int FindRangeIndex(int pixelsPerEm);
}
