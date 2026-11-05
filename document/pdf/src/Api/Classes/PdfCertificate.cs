using System;

namespace pdf;

/// <summary>
/// Represents a certificate used by the PDF generation API.
/// </summary>
public class PdfCertificate : IPdfCertificate
{
    /// <summary>
    /// Stores the recipient used by this object.
    /// </summary>
    internal string Recipient = string.Empty;
    /// <summary>
    /// Stores the award title used by this object.
    /// </summary>
    internal string AwardTitle = string.Empty;
    /// <summary>
    /// Stores the description used by this object.
    /// </summary>
    internal string Description = string.Empty;
    /// <summary>
    /// Stores the issuer used by this object.
    /// </summary>
    internal string Issuer = string.Empty;
    /// <summary>
    /// Stores the date text used by this object.
    /// </summary>
    internal string DateText = string.Empty;
    internal bool Built;
    /// <summary>
    /// Stores the document used by this object.
    /// </summary>
    internal PdfDocument Document = new PdfDocument();

    /// <summary>
    /// Initializes a new certificate.
    /// </summary>
    /// <param name="recipient">The recipient to use for this operation.</param>
    /// <param name="awardTitle">The award title to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfCertificate(string recipient, string awardTitle)
    {
        if (recipient == null)
        {
            throw new ArgumentNullException("recipient");
        }

        if (awardTitle == null)
        {
            throw new ArgumentNullException("awardTitle");
        }

        Recipient = recipient;
        AwardTitle = awardTitle;
        Description = string.Empty;
        Issuer = string.Empty;
        DateText = string.Empty;
        Built = false;
        Document = new PdfDocument();
    }

    /// <summary>
    /// Sets the description used for subsequent PDF generation.
    /// </summary>
    /// <param name="description">The description to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetDescription(string description)
    {
        EnsureNotBuilt();
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        Description = description;
    }

    /// <summary>
    /// Sets the issuer used for subsequent PDF generation.
    /// </summary>
    /// <param name="issuer">A value indicating whether issuer.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetIssuer(string issuer)
    {
        EnsureNotBuilt();
        if (issuer == null)
        {
            throw new ArgumentNullException("issuer");
        }

        Issuer = issuer;
    }

    /// <summary>
    /// Sets the date used for subsequent PDF generation.
    /// </summary>
    /// <param name="dateText">The date text to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetDate(string dateText)
    {
        EnsureNotBuilt();
        if (dateText == null)
        {
            throw new ArgumentNullException("dateText");
        }

        DateText = dateText;
    }

    /// <summary>
    /// Builds a certificate from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfPage page = Document.AddPageWithDimensions(new PdfPageSizeFactory().Landscape(new PdfPageSizeFactory().Letter()));
            PdfTextStyle titleStyle = PdfTextStyle.WithFont(30, PdfFont.Bold);
            PdfTextStyle recipientStyle = PdfTextStyle.WithFont(24, PdfFont.Bold);
            PdfTextStyle bodyStyle = PdfTextStyle.WithColor(12, PdfFont.Regular, PdfColor.Gray(0.25));

            page.Canvas.SetStrokeColorValue(PdfColor.Gray(0.45));
            page.Canvas.SetLineWidth(2);
            page.Canvas.StrokeRectangle(48, 48, page.Width - 96, page.Height - 96);
            page.Canvas.SetStrokeColorValue(PdfColor.Gray(0.75));
            page.Canvas.SetLineWidth(0.8);
            page.Canvas.StrokeRectangle(60, 60, page.Width - 120, page.Height - 120);
            page.Canvas.DrawAlignedStyledText(AwardTitle, 72, 420, page.Width - 144, titleStyle, PdfTextAlignment.Center);
            page.Canvas.DrawAlignedStyledText("Presented to", 72, 365, page.Width - 144, bodyStyle, PdfTextAlignment.Center);
            page.Canvas.DrawAlignedStyledText(Recipient, 72, 320, page.Width - 144, recipientStyle, PdfTextAlignment.Center);
            if (Description.Length > 0)
            {
                page.Canvas.DrawAlignedStyledText(Description, 96, 270, page.Width - 192, bodyStyle, PdfTextAlignment.Center);
            }

            if (Issuer.Length > 0)
            {
                page.Canvas.DrawAlignedStyledText(Issuer, 96, 126, 260, bodyStyle, PdfTextAlignment.Left);
            }

            if (DateText.Length > 0)
            {
                page.Canvas.DrawAlignedStyledText(DateText, page.Width - 356, 126, 260, bodyStyle, PdfTextAlignment.Right);
            }

            Built = true;
        }

        return Document;
    }

    internal void EnsureNotBuilt()
    {
        if (Built)
        {
            throw new InvalidOperationException("Certificate has already been built.");
        }
    }
}
