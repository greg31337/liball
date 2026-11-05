using System;

namespace pdf;

/// <summary>
/// Represents a paper section used by the PDF generation API.
/// </summary>
public class PdfPaperSection
{
    /// <summary>
    /// Stores the heading used by this object.
    /// </summary>
    public string Heading = string.Empty;

    /// <summary>
    /// Stores the heading level used when the section is rendered.
    /// </summary>
    public PdfPaperHeadingLevel Level = PdfPaperHeadingLevel.Level1;

    /// <summary>
    /// Initializes a new paper section.
    /// </summary>
    /// <param name="heading">The heading to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfPaperSection(string heading)
    {
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        Heading = heading;
        Level = PdfPaperHeadingLevel.Level1;
    }

    internal PdfTextBlock CreateBlock(PdfParagraphStyle headingStyle)
    {
        if (headingStyle == null)
        {
            throw new ArgumentNullException("headingStyle");
        }

        return new PdfTextBlock(Heading, headingStyle);
    }
}
