using System;

namespace pdf;

/// <summary>
/// Contains the paragraph settings applied while laying out and drawing PDF content.
/// </summary>
public class PdfParagraphStyle
{
    /// <summary>
    /// Stores the text style used by this object.
    /// </summary>
    public PdfTextStyle TextStyle = new PdfTextStyle();
    /// <summary>
    /// Specifies the line height in PDF points.
    /// </summary>
    public double LineHeight;
    /// <summary>
    /// Stores the numeric spacing before used by this object.
    /// </summary>
    public double SpacingBefore;
    /// <summary>
    /// Stores the numeric spacing after used by this object.
    /// </summary>
    public double SpacingAfter;
    /// <summary>
    /// Stores the numeric first line indent used by this object.
    /// </summary>
    public double FirstLineIndent;
    /// <summary>
    /// Stores the alignment used by this object.
    /// </summary>
    public PdfTextAlignment Alignment;

    /// <summary>
    /// Initializes a new paragraph style.
    /// </summary>
    public PdfParagraphStyle()
    {
        TextStyle = new PdfTextStyle();
        LineHeight = TextStyle.FontSize * 1.2;
        SpacingBefore = 0;
        SpacingAfter = 0;
        FirstLineIndent = 0;
        Alignment = PdfTextAlignment.Left;
    }

    /// <summary>
    /// Creates a paragraph style using the natural line height for the supplied text style.
    /// </summary>
    /// <param name="textStyle">The text style to use for this operation.</param>
    /// <returns>The resulting paragraph style.</returns>
    internal static PdfParagraphStyle FromTextStyle(PdfTextStyle textStyle)
    {
        ValidateTextStyle(textStyle);

        return Create(textStyle, textStyle.FontSize * 1.2, 0, 0, 0, PdfTextAlignment.Left);
    }

    /// <summary>
    /// Creates a paragraph style using the natural line height for the supplied text style.
    /// </summary>
    /// <param name="textStyle">The text style to use for this operation.</param>
    /// <returns>The resulting paragraph style.</returns>
    internal static PdfParagraphStyle Create(PdfTextStyle textStyle)
    {
        return FromTextStyle(textStyle);
    }

    /// <summary>
    /// Creates a left-aligned paragraph style with explicit spacing metrics.
    /// </summary>
    /// <param name="textStyle">The text style to use for this operation.</param>
    /// <param name="lineHeight">The baseline-to-baseline distance in PDF points.</param>
    /// <param name="spacingBefore">The spacing before to use for this operation.</param>
    /// <param name="spacingAfter">The spacing after to use for this operation.</param>
    /// <param name="firstLineIndent">The first line indent to use for this operation.</param>
    /// <returns>The resulting paragraph style.</returns>
    internal static PdfParagraphStyle WithMetrics(PdfTextStyle textStyle, double lineHeight, double spacingBefore, double spacingAfter, double firstLineIndent)
    {
        return Create(textStyle, lineHeight, spacingBefore, spacingAfter, firstLineIndent, PdfTextAlignment.Left);
    }

    /// <summary>
    /// Creates a left-aligned paragraph style with explicit spacing metrics.
    /// </summary>
    /// <param name="textStyle">The text style to use for this operation.</param>
    /// <param name="lineHeight">The baseline-to-baseline distance in PDF points.</param>
    /// <param name="spacingBefore">The spacing before to use for this operation.</param>
    /// <param name="spacingAfter">The spacing after to use for this operation.</param>
    /// <param name="firstLineIndent">The first line indent to use for this operation.</param>
    /// <returns>The resulting paragraph style.</returns>
    internal static PdfParagraphStyle Create(PdfTextStyle textStyle, double lineHeight, double spacingBefore, double spacingAfter, double firstLineIndent)
    {
        return WithMetrics(textStyle, lineHeight, spacingBefore, spacingAfter, firstLineIndent);
    }

    /// <summary>
    /// Creates a paragraph style with explicit spacing metrics and alignment.
    /// </summary>
    /// <param name="textStyle">The text style to use for this operation.</param>
    /// <param name="lineHeight">The baseline-to-baseline distance in PDF points.</param>
    /// <param name="spacingBefore">The spacing before to use for this operation.</param>
    /// <param name="spacingAfter">The spacing after to use for this operation.</param>
    /// <param name="firstLineIndent">The first line indent to use for this operation.</param>
    /// <param name="alignment">The alignment to use for this operation.</param>
    /// <returns>The resulting paragraph style.</returns>
    internal static PdfParagraphStyle Create(PdfTextStyle textStyle, double lineHeight, double spacingBefore, double spacingAfter, double firstLineIndent, PdfTextAlignment alignment)
    {
        PdfParagraphStyle style = new PdfParagraphStyle();
        style.Initialize(textStyle, lineHeight, spacingBefore, spacingAfter, firstLineIndent, alignment);
        return style;
    }

    internal void Initialize(PdfTextStyle textStyle, double lineHeight, double spacingBefore, double spacingAfter, double firstLineIndent, PdfTextAlignment alignment)
    {
        ValidateTextStyle(textStyle);
        ValidatePositive(lineHeight, "lineHeight", "Line height must be positive.");
        ValidateNotNegative(spacingBefore, "spacingBefore", "Spacing before cannot be negative.");
        ValidateNotNegative(spacingAfter, "spacingAfter", "Spacing after cannot be negative.");
        ValidateNotNegative(firstLineIndent, "firstLineIndent", "First line indent cannot be negative.");
        ValidateAlignment(alignment);

        TextStyle = textStyle;
        LineHeight = lineHeight;
        SpacingBefore = spacingBefore;
        SpacingAfter = spacingAfter;
        FirstLineIndent = firstLineIndent;
        Alignment = alignment;
    }

    internal static void ValidateTextStyle(PdfTextStyle textStyle)
    {
        if (textStyle == null)
        {
            throw new ArgumentNullException("textStyle");
        }
    }

    internal static void ValidatePositive(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }

    internal static void ValidateNotNegative(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }

    internal static void ValidateAlignment(PdfTextAlignment alignment)
    {
        if (alignment == PdfTextAlignment.Left)
        {
            return;
        }

        if (alignment == PdfTextAlignment.Center)
        {
            return;
        }

        if (alignment == PdfTextAlignment.Right)
        {
            return;
        }

        if (alignment == PdfTextAlignment.Justify)
        {
            return;
        }

        throw new ArgumentException("Text alignment is not supported.", "alignment");
    }

    internal PdfParagraphStyle Copy()
    {
        ValidateTextStyle(TextStyle);
        ValidatePositive(LineHeight, "LineHeight", "Line height must be positive.");
        ValidateNotNegative(SpacingBefore, "SpacingBefore", "Spacing before cannot be negative.");
        ValidateNotNegative(SpacingAfter, "SpacingAfter", "Spacing after cannot be negative.");
        ValidateNotNegative(FirstLineIndent, "FirstLineIndent", "First-line indent cannot be negative.");
        ValidateAlignment(Alignment);
        PdfParagraphStyle copied = new PdfParagraphStyle();
        copied.TextStyle = TextStyle.Copy();
        copied.LineHeight = LineHeight;
        copied.SpacingBefore = SpacingBefore;
        copied.SpacingAfter = SpacingAfter;
        copied.FirstLineIndent = FirstLineIndent;
        copied.Alignment = Alignment;
        return copied;
    }
}
