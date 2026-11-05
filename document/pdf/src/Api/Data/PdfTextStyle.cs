using System;

namespace pdf;

/// <summary>
/// Contains the text settings applied while laying out and drawing PDF content.
/// </summary>
public class PdfTextStyle
{
    /// <summary>
    /// Stores the font used by this object.
    /// </summary>
    public PdfFont Font;
    /// <summary>
    /// Stores the numeric font size used by this object.
    /// </summary>
    public double FontSize;
    /// <summary>
    /// Stores the fill color used by this object.
    /// </summary>
    public PdfColor FillColor;
    internal PdfTrueTypeFont? TrueTypeFont;

    /// <summary>
    /// Initializes a new text style.
    /// </summary>
    public PdfTextStyle()
    {
        Font = PdfFont.Regular;
        FontSize = 12;
        FillColor = PdfColor.Rgb(0, 0, 0);
        TrueTypeFont = null;
    }

    /// <summary>
    /// Creates and returns a text style configured for regular.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The resulting text style.</returns>
    internal static PdfTextStyle Regular(double fontSize)
    {
        return WithFont(fontSize, PdfFont.Regular);
    }

    /// <summary>
    /// Creates and returns a text style configured for bold.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The resulting text style.</returns>
    internal static PdfTextStyle Bold(double fontSize)
    {
        return WithFont(fontSize, PdfFont.Bold);
    }

    /// <summary>
    /// Creates a regular text style with the supplied size.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The resulting text style.</returns>
    internal static PdfTextStyle Create(double fontSize)
    {
        return Regular(fontSize);
    }

    /// <summary>
    /// Creates a text style with the supplied font and size.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting text style.</returns>
    internal static PdfTextStyle Create(double fontSize, PdfFont font)
    {
        return WithFont(fontSize, font);
    }

    /// <summary>
    /// Creates a text style with the supplied font, size, and color.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    /// <param name="fillColor">The fill color to use for this operation.</param>
    /// <returns>The resulting text style.</returns>
    internal static PdfTextStyle Create(double fontSize, PdfFont font, PdfColor fillColor)
    {
        return WithColor(fontSize, font, fillColor);
    }

    /// <summary>
    /// Creates a text style with the supplied font and size.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting text style.</returns>
    internal static PdfTextStyle WithFont(double fontSize, PdfFont font)
    {
        ValidateFontSize(fontSize);

        PdfTextStyle style = new PdfTextStyle();
        style.Font = font;
        style.FontSize = fontSize;
        return style;
    }

    /// <summary>
    /// Creates a copy that uses the supplied color.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    /// <param name="fillColor">The fill color to use for this operation.</param>
    /// <returns>The resulting text style.</returns>
    internal static PdfTextStyle WithColor(double fontSize, PdfFont font, PdfColor fillColor)
    {
        ValidateFontSize(fontSize);
        ValidateFillColor(fillColor);

        PdfTextStyle style = new PdfTextStyle();
        style.Font = font;
        style.FontSize = fontSize;
        style.FillColor = fillColor;
        return style;
    }

    internal static void ValidateFontSize(double fontSize)
    {
        if (double.IsNaN(fontSize) || double.IsInfinity(fontSize) || fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException("fontSize", "Font size must be finite and positive.");
        }
    }

    internal static void ValidateFillColor(PdfColor fillColor)
    {
        if (fillColor == null)
        {
            throw new ArgumentNullException("fillColor");
        }
    }

    internal PdfTextStyle Copy()
    {
        ValidateFontSize(FontSize);
        ValidateFillColor(FillColor);
        PdfTextStyle copied = new PdfTextStyle();
        copied.Font = Font;
        copied.FontSize = FontSize;
        copied.FillColor = PdfElementStyle.CopyColor(FillColor);
        copied.TrueTypeFont = TrueTypeFont;
        return copied;
    }
}
