namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartMatrix"/>.</summary>
public interface IChartMatrix
{
    /// <summary>
    /// Transforms point using the supplied data and configuration.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <returns>The resulting point.</returns>
    ChartPoint TransformPoint(ChartPoint point);
    /// <summary>
    /// Applies the multiply operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "matrix">The matrix.</param>
    /// <returns>The resulting matrix.</returns>
    ChartMatrix Multiply(ChartMatrix matrix);
    /// <summary>
    /// Applies the determinant operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Determinant();
    /// <summary>
    /// Applies the invert operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The inverted matrix, or <c>null</c> when this matrix is singular.</returns>
    ChartMatrix? Invert();
    /// <summary>Returns the current m11.</summary>
    /// <returns>The current field value.</returns>
    double GetM11();
    /// <summary>Replaces the current m11.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetM11(double value);
    /// <summary>Returns the current m12.</summary>
    /// <returns>The current field value.</returns>
    double GetM12();
    /// <summary>Replaces the current m12.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetM12(double value);
    /// <summary>Returns the current m21.</summary>
    /// <returns>The current field value.</returns>
    double GetM21();
    /// <summary>Replaces the current m21.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetM21(double value);
    /// <summary>Returns the current m22.</summary>
    /// <returns>The current field value.</returns>
    double GetM22();
    /// <summary>Replaces the current m22.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetM22(double value);
    /// <summary>Returns the current offset x.</summary>
    /// <returns>The current field value.</returns>
    double GetOffsetX();
    /// <summary>Replaces the current offset x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetOffsetX(double value);
    /// <summary>Returns the current offset y.</summary>
    /// <returns>The current field value.</returns>
    double GetOffsetY();
    /// <summary>Replaces the current offset y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetOffsetY(double value);
}
