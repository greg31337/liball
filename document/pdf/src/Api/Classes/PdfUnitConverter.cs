using System;

namespace pdf;

/// <summary>
/// Converts point, inch, millimetre, and centimetre measurements to PDF points.
/// </summary>
public class PdfUnitConverter : IPdfUnitConverter
{
    /// <summary>
    /// Initializes a unit converter.
    /// </summary>
    public PdfUnitConverter()
    {
    }

    /// <summary>
    /// Returns an unchanged point measurement after validating it.
    /// </summary>
    /// <param name="value">The finite point measurement.</param>
    /// <returns>The measurement in PDF points.</returns>
    public double Points(double value)
    {
        Validate(value);
        return value;
    }

    /// <summary>
    /// Converts inches to PDF points.
    /// </summary>
    /// <param name="value">The finite inch measurement.</param>
    /// <returns>The measurement in PDF points.</returns>
    public double Inches(double value)
    {
        Validate(value);
        return value * 72;
    }

    /// <summary>
    /// Converts millimetres to PDF points.
    /// </summary>
    /// <param name="value">The finite millimetre measurement.</param>
    /// <returns>The measurement in PDF points.</returns>
    public double Millimetres(double value)
    {
        Validate(value);
        return value * 72 / 25.4;
    }

    /// <summary>
    /// Converts centimetres to PDF points.
    /// </summary>
    /// <param name="value">The finite centimetre measurement.</param>
    /// <returns>The measurement in PDF points.</returns>
    public double Centimetres(double value)
    {
        Validate(value);
        return value * 72 / 2.54;
    }

    internal static void Validate(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException("value", "Unit measurements must be finite.");
        }
    }
}
