using System;

namespace pdf;

/// <summary>
/// Stores the left, top, right, and bottom margins of a PDF page in points.
/// </summary>
public class PdfPageMargins
{
    /// <summary>
    /// Specifies the left in PDF points.
    /// </summary>
    public double Left;
    /// <summary>
    /// Specifies the top in PDF points.
    /// </summary>
    public double Top;
    /// <summary>
    /// Specifies the right in PDF points.
    /// </summary>
    public double Right;
    /// <summary>
    /// Specifies the bottom in PDF points.
    /// </summary>
    public double Bottom;

    /// <summary>
    /// Initializes a new set of page margins.
    /// </summary>
    /// <param name="left">The left in PDF points.</param>
    /// <param name="top">The top in PDF points.</param>
    /// <param name="right">The right in PDF points.</param>
    /// <param name="bottom">The bottom in PDF points.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a margin is not finite and nonnegative.</exception>
    public PdfPageMargins(double left, double top, double right, double bottom)
    {
        ValidateMargin(left, "left");
        ValidateMargin(top, "top");
        ValidateMargin(right, "right");
        ValidateMargin(bottom, "bottom");

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    internal static void ValidateMargin(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Page margin must be finite and nonnegative.");
        }
    }
}
