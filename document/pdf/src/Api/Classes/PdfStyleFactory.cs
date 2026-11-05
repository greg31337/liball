namespace pdf;

/// <summary>
/// Creates common insets, colors, reusable layout styles, themes, unit converters, and text styles.
/// </summary>
public class PdfStyleFactory : IPdfStyleFactory
{
    /// <summary>
    /// Creates a style factory.
    /// </summary>
    public PdfStyleFactory()
    {
    }

    /// <summary>
    /// Creates insets with every edge set to zero.
    /// </summary>
    /// <returns>The configured insets.</returns>
    public PdfInsets CreateNoInsets()
    {
        return PdfInsets.None();
    }

    /// <summary>
    /// Creates insets with the same value on every edge.
    /// </summary>
    /// <param name="value">The non-negative edge size in PDF points.</param>
    /// <returns>The configured insets.</returns>
    public PdfInsets CreateUniformInsets(double value)
    {
        return PdfInsets.Uniform(value);
    }

    /// <summary>
    /// Creates insets with shared vertical and horizontal values.
    /// </summary>
    /// <param name="vertical">The non-negative top and bottom size in PDF points.</param>
    /// <param name="horizontal">The non-negative left and right size in PDF points.</param>
    /// <returns>The configured insets.</returns>
    public PdfInsets CreateVerticalHorizontalInsets(double vertical, double horizontal)
    {
        return PdfInsets.VerticalHorizontal(vertical, horizontal);
    }

    /// <summary>
    /// Creates a reusable element style with PDF defaults.
    /// </summary>
    /// <returns>The configured element style.</returns>
    public PdfElementStyle CreateElementStyle()
    {
        return new PdfElementStyle();
    }

    /// <summary>
    /// Creates a reusable table style with separate header, body, and footer cell styles.
    /// </summary>
    /// <returns>The configured table style.</returns>
    public PdfTableStyle CreateTableStyle()
    {
        return new PdfTableStyle();
    }

    /// <summary>
    /// Creates a document theme with reusable paragraph, block, and table defaults.
    /// </summary>
    /// <returns>The configured document theme.</returns>
    public PdfDocumentTheme CreateDocumentTheme()
    {
        return new PdfDocumentTheme();
    }

    /// <summary>
    /// Creates a converter for point, inch, millimetre, and centimetre measurements.
    /// </summary>
    /// <returns>The configured unit converter.</returns>
    public PdfUnitConverter CreateUnitConverter()
    {
        return new PdfUnitConverter();
    }

    /// <summary>
    /// Creates a device RGB color.
    /// </summary>
    /// <param name="red">The normalized red channel, from zero through one.</param>
    /// <param name="green">The normalized green channel, from zero through one.</param>
    /// <param name="blue">The normalized blue channel, from zero through one.</param>
    /// <returns>The configured color.</returns>
    public PdfColor CreateRgbColor(double red, double green, double blue)
    {
        return PdfColor.Rgb(red, green, blue);
    }

    /// <summary>
    /// Creates a device gray color.
    /// </summary>
    /// <param name="gray">The normalized gray channel, from zero for black through one for white.</param>
    /// <returns>The configured color.</returns>
    public PdfColor CreateGrayColor(double gray)
    {
        return PdfColor.Gray(gray);
    }

    /// <summary>
    /// Creates a device CMYK color.
    /// </summary>
    /// <param name="cyan">The normalized cyan channel, from zero through one.</param>
    /// <param name="magenta">The normalized magenta channel, from zero through one.</param>
    /// <param name="yellow">The normalized yellow channel, from zero through one.</param>
    /// <param name="black">The normalized black channel, from zero through one.</param>
    /// <returns>The configured color.</returns>
    public PdfColor CreateCmykColor(double cyan, double magenta, double yellow, double black)
    {
        return PdfColor.Cmyk(cyan, magenta, yellow, black);
    }

    /// <summary>
    /// Creates a regular text style with the supplied font size.
    /// </summary>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    /// <returns>The configured text style.</returns>
    public PdfTextStyle CreateRegularTextStyle(double fontSize)
    {
        return PdfTextStyle.Regular(fontSize);
    }

    /// <summary>
    /// Creates a bold text style with the supplied font size.
    /// </summary>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    /// <returns>The configured text style.</returns>
    public PdfTextStyle CreateBoldTextStyle(double fontSize)
    {
        return PdfTextStyle.Bold(fontSize);
    }

    /// <summary>
    /// Creates a text style with an explicit standard font and size.
    /// </summary>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    /// <param name="font">The standard font to use.</param>
    /// <returns>The configured text style.</returns>
    public PdfTextStyle CreateTextStyle(double fontSize, PdfFont font)
    {
        return PdfTextStyle.Create(fontSize, font);
    }

    /// <summary>
    /// Creates a text style with an explicit standard font, size, and fill color.
    /// </summary>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    /// <param name="font">The standard font to use.</param>
    /// <param name="fillColor">The text fill color.</param>
    /// <returns>The configured text style.</returns>
    public PdfTextStyle CreateColoredTextStyle(double fontSize, PdfFont font, PdfColor fillColor)
    {
        return PdfTextStyle.Create(fontSize, font, fillColor);
    }

    /// <summary>
    /// Creates a left-aligned paragraph style with natural line height and no spacing.
    /// </summary>
    /// <param name="textStyle">The text style used by the paragraph.</param>
    /// <returns>The configured paragraph style.</returns>
    public PdfParagraphStyle CreateParagraphStyle(PdfTextStyle textStyle)
    {
        return PdfParagraphStyle.Create(textStyle);
    }

    /// <summary>
    /// Creates a left-aligned paragraph style with explicit metrics.
    /// </summary>
    /// <param name="textStyle">The text style used by the paragraph.</param>
    /// <param name="lineHeight">The positive baseline-to-baseline distance in PDF points.</param>
    /// <param name="spacingBefore">The non-negative spacing before the paragraph.</param>
    /// <param name="spacingAfter">The non-negative spacing after the paragraph.</param>
    /// <param name="firstLineIndent">The non-negative first-line indent.</param>
    /// <returns>The configured paragraph style.</returns>
    public PdfParagraphStyle CreateParagraphStyleWithMetrics(PdfTextStyle textStyle, double lineHeight, double spacingBefore, double spacingAfter, double firstLineIndent)
    {
        return PdfParagraphStyle.Create(textStyle, lineHeight, spacingBefore, spacingAfter, firstLineIndent);
    }

    /// <summary>
    /// Creates a paragraph style with explicit metrics and alignment.
    /// </summary>
    /// <param name="textStyle">The text style used by the paragraph.</param>
    /// <param name="lineHeight">The positive baseline-to-baseline distance in PDF points.</param>
    /// <param name="spacingBefore">The non-negative spacing before the paragraph.</param>
    /// <param name="spacingAfter">The non-negative spacing after the paragraph.</param>
    /// <param name="firstLineIndent">The non-negative first-line indent.</param>
    /// <param name="alignment">The paragraph text alignment.</param>
    /// <returns>The configured paragraph style.</returns>
    public PdfParagraphStyle CreateAlignedParagraphStyle(PdfTextStyle textStyle, double lineHeight, double spacingBefore, double spacingAfter, double firstLineIndent, PdfTextAlignment alignment)
    {
        return PdfParagraphStyle.Create(textStyle, lineHeight, spacingBefore, spacingAfter, firstLineIndent, alignment);
    }
}
