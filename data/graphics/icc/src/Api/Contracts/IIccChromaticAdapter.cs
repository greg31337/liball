namespace icc;

/// <summary>
/// Builds, applies, and inverts Bradford or explicit chromatic-adaptation matrices.
/// </summary>
public interface IIccChromaticAdapter
{
    /// <summary>
    /// Builds a Bradford adaptation matrix between two XYZ white points.
    /// </summary>
    /// <param name="sourceWhitePoint">The source XYZ white point.</param>
    /// <param name="destinationWhitePoint">The destination XYZ white point.</param>
    /// <returns>The row-major three-by-three adaptation matrix.</returns>
    double[] BuildBradfordMatrix(double[] sourceWhitePoint, double[] destinationWhitePoint);

    /// <summary>
    /// Applies a row-major three-by-three adaptation matrix to an XYZ colour.
    /// </summary>
    /// <param name="matrix">The adaptation matrix.</param>
    /// <param name="xyz">The XYZ colour.</param>
    /// <returns>The adapted XYZ colour.</returns>
    double[] ApplyAdaptationMatrix(double[] matrix, double[] xyz);

    /// <summary>
    /// Inverts a row-major three-by-three adaptation matrix.
    /// </summary>
    /// <param name="matrix">The adaptation matrix.</param>
    /// <returns>The inverse adaptation matrix.</returns>
    double[] InvertAdaptationMatrix(double[] matrix);
}
