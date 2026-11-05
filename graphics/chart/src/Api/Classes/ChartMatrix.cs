namespace chart;
/// <summary>
/// Represents a matrix used by the chart model or rendering pipeline.
/// </summary>
public class ChartMatrix : IChartMatrix
{
    internal double M11;
    internal double M12;
    internal double M21;
    internal double M22;
    internal double OffsetX;
    internal double OffsetY;
    /// <summary>
    /// Initializes a matrix from the supplied m11, m12, m21, m22, offset x, and offset y.
    /// </summary>
    /// <param name = "m11">The m11.</param>
    /// <param name = "m12">The m12.</param>
    /// <param name = "m21">The m21.</param>
    /// <param name = "m22">The m22.</param>
    /// <param name = "offsetX">The offset x.</param>
    /// <param name = "offsetY">The offset y.</param>
    public ChartMatrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY)
    {
        M11 = m11;
        M12 = m12;
        M21 = m21;
        M22 = m22;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    internal static ChartMatrix Identity()
    {
        return new ChartMatrix(1.0, 0.0, 0.0, 1.0, 0.0, 0.0);
    }

    internal static ChartMatrix Translation(double x, double y)
    {
        return new ChartMatrix(1.0, 0.0, 0.0, 1.0, x, y);
    }

    internal static ChartMatrix Scale(double x, double y)
    {
        return new ChartMatrix(x, 0.0, 0.0, y, 0.0, 0.0);
    }

    internal static ChartMatrix Rotation(double radians)
    {
        double cosine = System.Math.Cos(radians);
        double sine = System.Math.Sin(radians);
        return new ChartMatrix(cosine, sine, 0.0 - sine, cosine, 0.0, 0.0);
    }

    /// <summary>
    /// Transforms point using the supplied data and configuration.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <returns>The resulting point.</returns>
    public ChartPoint TransformPoint(ChartPoint point)
    {
        double x = point.X * M11 + point.Y * M21 + OffsetX;
        double y = point.X * M12 + point.Y * M22 + OffsetY;
        return new ChartPoint(x, y);
    }

    /// <summary>
    /// Applies the multiply operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "matrix">The matrix.</param>
    /// <returns>The resulting matrix.</returns>
    public ChartMatrix Multiply(ChartMatrix matrix)
    {
        double m11 = M11 * matrix.M11 + M12 * matrix.M21;
        double m12 = M11 * matrix.M12 + M12 * matrix.M22;
        double m21 = M21 * matrix.M11 + M22 * matrix.M21;
        double m22 = M21 * matrix.M12 + M22 * matrix.M22;
        double offsetX = OffsetX * matrix.M11 + OffsetY * matrix.M21 + matrix.OffsetX;
        double offsetY = OffsetX * matrix.M12 + OffsetY * matrix.M22 + matrix.OffsetY;
        return new ChartMatrix(m11, m12, m21, m22, offsetX, offsetY);
    }

    /// <summary>
    /// Applies the determinant operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Determinant()
    {
        return M11 * M22 - M12 * M21;
    }

    /// <summary>
    /// Applies the invert operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The inverted matrix, or <c>null</c> when this matrix is singular.</returns>
    public ChartMatrix? Invert()
    {
        double determinant = Determinant();
        if (determinant == 0.0)
        {
            return null;
        }

        double scale = 1.0 / determinant;
        double m11 = M22 * scale;
        double m12 = (0.0 - M12) * scale;
        double m21 = (0.0 - M21) * scale;
        double m22 = M11 * scale;
        double offsetX = (M21 * OffsetY - M22 * OffsetX) * scale;
        double offsetY = (M12 * OffsetX - M11 * OffsetY) * scale;
        return new ChartMatrix(m11, m12, m21, m22, offsetX, offsetY);
    }

    /// <summary>Returns the current m11.</summary>
    /// <returns>The current field value.</returns>
    public double GetM11()
    {
        return M11;
    }

    /// <summary>Replaces the current m11.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetM11(double value)
    {
        M11 = value;
    }

    /// <summary>Returns the current m12.</summary>
    /// <returns>The current field value.</returns>
    public double GetM12()
    {
        return M12;
    }

    /// <summary>Replaces the current m12.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetM12(double value)
    {
        M12 = value;
    }

    /// <summary>Returns the current m21.</summary>
    /// <returns>The current field value.</returns>
    public double GetM21()
    {
        return M21;
    }

    /// <summary>Replaces the current m21.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetM21(double value)
    {
        M21 = value;
    }

    /// <summary>Returns the current m22.</summary>
    /// <returns>The current field value.</returns>
    public double GetM22()
    {
        return M22;
    }

    /// <summary>Replaces the current m22.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetM22(double value)
    {
        M22 = value;
    }

    /// <summary>Returns the current offset x.</summary>
    /// <returns>The current field value.</returns>
    public double GetOffsetX()
    {
        return OffsetX;
    }

    /// <summary>Replaces the current offset x.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetOffsetX(double value)
    {
        OffsetX = value;
    }

    /// <summary>Returns the current offset y.</summary>
    /// <returns>The current field value.</returns>
    public double GetOffsetY()
    {
        return OffsetY;
    }

    /// <summary>Replaces the current offset y.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetOffsetY(double value)
    {
        OffsetY = value;
    }
}
