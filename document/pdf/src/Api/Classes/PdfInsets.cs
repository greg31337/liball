using System;

namespace pdf;

/// <summary>
/// Stores four edge thicknesses used for layout padding, borders, or margins.
/// </summary>
public class PdfInsets : IPdfInsets
{
    /// <summary>
    /// Specifies the left in PDF points.
    /// </summary>
    internal double Left;
    /// <summary>
    /// Specifies the top in PDF points.
    /// </summary>
    internal double Top;
    /// <summary>
    /// Specifies the right in PDF points.
    /// </summary>
    internal double Right;
    /// <summary>
    /// Specifies the bottom in PDF points.
    /// </summary>
    internal double Bottom;

    /// <summary>
    /// Initializes a new set of edge insets.
    /// </summary>
    /// <param name="left">The left in PDF points.</param>
    /// <param name="top">The top in PDF points.</param>
    /// <param name="right">The right in PDF points.</param>
    /// <param name="bottom">The bottom in PDF points.</param>
    public PdfInsets(double left, double top, double right, double bottom)
    {
        ValidateNotNegative(left, "left");
        ValidateNotNegative(top, "top");
        ValidateNotNegative(right, "right");
        ValidateNotNegative(bottom, "bottom");

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// Returns the horizontal calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetHorizontal()
    {
        return Left + Right;
    }

    /// <summary>
    /// Returns the vertical calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetVertical()
    {
        return Top + Bottom;
    }

    /// <summary>
    /// Creates edge insets with every edge set to zero.
    /// </summary>
    /// <returns>The resulting insets.</returns>
    internal static PdfInsets None()
    {
        return new PdfInsets(0, 0, 0, 0);
    }

    /// <summary>
    /// Creates edge insets that use the same value on every edge.
    /// </summary>
    /// <param name="value">The value to use for this operation.</param>
    /// <returns>The resulting insets.</returns>
    internal static PdfInsets Uniform(double value)
    {
        return new PdfInsets(value, value, value, value);
    }

    /// <summary>
    /// Creates edge insets with shared vertical and horizontal values.
    /// </summary>
    /// <param name="vertical">The vertical to use for this operation.</param>
    /// <param name="horizontal">The horizontal to use for this operation.</param>
    /// <returns>The resulting insets.</returns>
    internal static PdfInsets VerticalHorizontal(double vertical, double horizontal)
    {
        return new PdfInsets(horizontal, vertical, horizontal, vertical);
    }

    internal static void ValidateNotNegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Insets must be finite and nonnegative.");
        }
    }
}
