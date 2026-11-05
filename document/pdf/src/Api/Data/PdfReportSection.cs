using System;

namespace pdf;

/// <summary>
/// Represents a report section used by the PDF generation API.
/// </summary>
public class PdfReportSection
{
    /// <summary>
    /// Stores the heading used by this object.
    /// </summary>
    public string Heading = string.Empty;
    /// <summary>
    /// Stores the text used by this object.
    /// </summary>
    public string Text = string.Empty;

    /// <summary>
    /// Initializes a new report section.
    /// </summary>
    /// <param name="heading">The heading to use for this operation.</param>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfReportSection(string heading, string text)
    {
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Heading = heading;
        Text = text;
    }

    internal PdfStackBlock CreateBlock(PdfParagraphStyle headingStyle, PdfParagraphStyle textStyle)
    {
        if (headingStyle == null)
        {
            throw new ArgumentNullException("headingStyle");
        }

        if (textStyle == null)
        {
            throw new ArgumentNullException("textStyle");
        }

        PdfStackBlock stack = PdfStackBlock.WithSpacing(6);
        stack.AddTextBlock(new PdfTextBlock(Heading, headingStyle));
        stack.AddTextBlock(new PdfTextBlock(Text, textStyle));
        return stack;
    }
}
