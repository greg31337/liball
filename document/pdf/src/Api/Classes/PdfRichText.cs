using System;
using System.Collections.Generic;
using System.Text;

namespace pdf;

/// <summary>
/// Combines styled text spans and measures or draws them as one line of rich text.
/// </summary>
public class PdfRichText : IPdfRichText
{
    internal List<PdfTextSpan> _spans = new List<PdfTextSpan>();
    /// <summary>
    /// Exposes the spans currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfTextSpan> Spans;

    /// <summary>
    /// Initializes a new rich text.
    /// </summary>
    public PdfRichText()
    {
        Spans = _spans;
    }

    /// <summary>
    /// Adds a span to this rich text and returns the stored value.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTextSpan AddSpan(string text)
    {
        return AddStyledSpan(text, new PdfTextStyle());
    }

    /// <summary>
    /// Adds a span to this rich text and returns the stored value.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTextSpan AddStyledSpan(string text, PdfTextStyle style)
    {
        PdfTextSpan span = new PdfTextSpan(text, style);
        _spans.Add(span);
        return span;
    }

    /// <summary>
    /// Adds a span to this rich text.
    /// </summary>
    /// <param name="span">The span to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddExistingSpan(PdfTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        _spans.Add(span);
    }

    /// <summary>
    /// Returns the text calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string GetText()
    {
        StringBuilder text = new StringBuilder();

        foreach (PdfTextSpan span in _spans)
        {
            text.Append(span.Text);
        }

        return text.ToString();
    }

    /// <summary>
    /// Measures a width without emitting drawing commands.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double MeasureWidth()
    {
        double width = 0;

        foreach (PdfTextSpan span in _spans)
        {
            width += span.MeasureWidth();
        }

        return width;
    }

    /// <summary>
    /// Returns the line height calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetLineHeight()
    {
        double lineHeight = 0;

        foreach (PdfTextSpan span in _spans)
        {
            PdfFontMetrics metrics = PdfFontMetrics.GetMetrics(span.Style.Font);
            double spanLineHeight = metrics.GetLineHeight(span.Style.FontSize);
            if (spanLineHeight > lineHeight)
            {
                lineHeight = spanLineHeight;
            }
        }

        return lineHeight;
    }
}
