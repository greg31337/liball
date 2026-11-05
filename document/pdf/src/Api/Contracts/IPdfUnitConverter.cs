namespace pdf;

/// <summary>
/// Defines explicit conversions from common physical units to PDF points.
/// </summary>
public interface IPdfUnitConverter
{
    /// <summary>
    /// Returns an unchanged point measurement after validating it.
    /// </summary>
    /// <param name="value">The finite point measurement.</param>
    /// <returns>The measurement in PDF points.</returns>
    double Points(double value);

    /// <summary>
    /// Converts inches to PDF points.
    /// </summary>
    /// <param name="value">The finite inch measurement.</param>
    /// <returns>The measurement in PDF points.</returns>
    double Inches(double value);

    /// <summary>
    /// Converts millimetres to PDF points.
    /// </summary>
    /// <param name="value">The finite millimetre measurement.</param>
    /// <returns>The measurement in PDF points.</returns>
    double Millimetres(double value);

    /// <summary>
    /// Converts centimetres to PDF points.
    /// </summary>
    /// <param name="value">The finite centimetre measurement.</param>
    /// <returns>The measurement in PDF points.</returns>
    double Centimetres(double value);
}
