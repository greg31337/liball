using System;

namespace pdf;

/// <summary>
/// Represents a text span used by the PDF generation API.
/// </summary>
public class PdfTextSpan : IPdfTextSpan
{
    /// <summary>
    /// Stores the text used by this object.
    /// </summary>
    internal string Text = string.Empty;
    /// <summary>
    /// Stores the style used by this object.
    /// </summary>
    internal PdfTextStyle Style = new PdfTextStyle();

    /// <summary>
    /// Initializes a new text span.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    public PdfTextSpan(string text, PdfTextStyle style)
    {
        ValidateText(text);
        ValidateStyle(style);

        Text = text;
        Style = style;
    }

    /// <summary>
    /// Creates a text span with the default text style.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <returns>The resulting text span.</returns>
    internal static PdfTextSpan Plain(string text)
    {
        return new PdfTextSpan(text, new PdfTextStyle());
    }

    /// <summary>
    /// Measures a width without emitting drawing commands.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double MeasureWidth()
    {
        return PdfCanvas.MeasureText(Text, Style);
    }

    internal static void ValidateText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }
    }

    internal static void ValidateStyle(PdfTextStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }
    }
}
