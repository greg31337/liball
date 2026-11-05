using System;

namespace pdf;

/// <summary>
/// Describes one reusable table-column sizing strategy.
/// </summary>
public class PdfTableColumnDefinition
{
    /// <summary>Stores the sizing strategy.</summary>
    public PdfTableColumnSizing Sizing;
    /// <summary>Stores the strategy value in PDF points or proportional weight units.</summary>
    public double Value;

    /// <summary>
    /// Initializes a table-column definition.
    /// </summary>
    /// <param name="sizing">The width sizing strategy.</param>
    /// <param name="value">The fixed width, proportional weight, minimum, or maximum; auto sizing requires zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the strategy or value is invalid.</exception>
    public PdfTableColumnDefinition(PdfTableColumnSizing sizing, double value)
    {
        Validate(sizing, value);
        Sizing = sizing;
        Value = value;
    }

    internal PdfTableColumnDefinition Copy()
    {
        return new PdfTableColumnDefinition(Sizing, Value);
    }

    internal static void Validate(PdfTableColumnSizing sizing, double value)
    {
        if (sizing != PdfTableColumnSizing.Fixed &&
            sizing != PdfTableColumnSizing.Proportional &&
            sizing != PdfTableColumnSizing.Auto &&
            sizing != PdfTableColumnSizing.Minimum &&
            sizing != PdfTableColumnSizing.Maximum)
        {
            throw new ArgumentOutOfRangeException("sizing", "Table column sizing strategy is not supported.");
        }

        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException("value", "Table column sizing value must be finite and non-negative.");
        }

        if (sizing != PdfTableColumnSizing.Auto && value <= 0)
        {
            throw new ArgumentOutOfRangeException("value", "This table column sizing strategy requires a positive value.");
        }

        if (sizing == PdfTableColumnSizing.Auto && value != 0)
        {
            throw new ArgumentOutOfRangeException("value", "Auto table column sizing requires a zero value.");
        }
    }
}
