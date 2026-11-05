using System;

namespace pdf;

/// <summary>
/// Represents a letter used by the PDF generation API.
/// </summary>
public class PdfLetter : IPdfLetter
{
    /// <summary>
    /// Stores the sender used by this object.
    /// </summary>
    internal string Sender = string.Empty;
    /// <summary>
    /// Stores the recipient used by this object.
    /// </summary>
    internal string Recipient = string.Empty;
    /// <summary>
    /// Stores the date text used by this object.
    /// </summary>
    internal string DateText = string.Empty;
    /// <summary>
    /// Stores the subject used by this object.
    /// </summary>
    internal string Subject = string.Empty;
    /// <summary>
    /// Stores the body used by this object.
    /// </summary>
    internal string Body = string.Empty;
    /// <summary>
    /// Stores the closing used by this object.
    /// </summary>
    internal string Closing = string.Empty;
    /// <summary>
    /// Stores the signature used by this object.
    /// </summary>
    internal string Signature = string.Empty;
    internal bool Built;
    /// <summary>
    /// Stores the document used by this object.
    /// </summary>
    internal PdfDocument Document = new PdfDocument();
    /// <summary>
    /// Stores the layout used by this object.
    /// </summary>
    internal PdfLayoutDocument Layout = PdfLayoutDocument.Default(new PdfDocument());
    /// <summary>
    /// Stores the body style used by this object.
    /// </summary>
    internal PdfParagraphStyle BodyStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the heading style used by this object.
    /// </summary>
    internal PdfParagraphStyle HeadingStyle = new PdfParagraphStyle();

    /// <summary>
    /// Initializes a new letter.
    /// </summary>
    public PdfLetter()
    {
        Document = new PdfDocument();
        Layout = new PdfLayoutDocument(Document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(72, 72, 72, 72));
        BodyStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Regular), 14, 0, 0, 0);
        HeadingStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Bold), 14, 0, 0, 0);
        Built = false;
    }

    /// <summary>
    /// Sets the sender used for subsequent PDF generation.
    /// </summary>
    /// <param name="sender">The sender to use for this operation.</param>
    public void SetSender(string sender)
    {
        EnsureNotBuilt();
        ValidateText(sender, "sender");
        Sender = sender;
    }

    /// <summary>
    /// Sets the recipient used for subsequent PDF generation.
    /// </summary>
    /// <param name="recipient">The recipient to use for this operation.</param>
    public void SetRecipient(string recipient)
    {
        EnsureNotBuilt();
        ValidateText(recipient, "recipient");
        Recipient = recipient;
    }

    /// <summary>
    /// Sets the date used for subsequent PDF generation.
    /// </summary>
    /// <param name="dateText">The date text to use for this operation.</param>
    public void SetDate(string dateText)
    {
        EnsureNotBuilt();
        ValidateText(dateText, "dateText");
        DateText = dateText;
    }

    /// <summary>
    /// Sets the subject used for subsequent PDF generation.
    /// </summary>
    /// <param name="subject">The subject to use for this operation.</param>
    public void SetSubject(string subject)
    {
        EnsureNotBuilt();
        ValidateText(subject, "subject");
        Subject = subject;
    }

    /// <summary>
    /// Sets the body used for subsequent PDF generation.
    /// </summary>
    /// <param name="body">The body to use for this operation.</param>
    public void SetBody(string body)
    {
        EnsureNotBuilt();
        ValidateText(body, "body");
        Body = body;
    }

    /// <summary>
    /// Sets the closing used for subsequent PDF generation.
    /// </summary>
    /// <param name="closing">The closing to use for this operation.</param>
    public void SetClosing(string closing)
    {
        EnsureNotBuilt();
        ValidateText(closing, "closing");
        Closing = closing;
    }

    /// <summary>
    /// Sets the signature used for subsequent PDF generation.
    /// </summary>
    /// <param name="signature">The signature to use for this operation.</param>
    public void SetSignature(string signature)
    {
        EnsureNotBuilt();
        ValidateText(signature, "signature");
        Signature = signature;
    }

    /// <summary>
    /// Builds a letter from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            AddOptionalText(Sender, BodyStyle, 14);
            AddOptionalText(DateText, BodyStyle, 18);
            AddOptionalText(Recipient, BodyStyle, 18);
            AddOptionalText(Subject, HeadingStyle, 14);
            AddOptionalText(Body, BodyStyle, 24);
            AddOptionalText(Closing, BodyStyle, 8);
            AddOptionalText(Signature, HeadingStyle, 0);
            Layout.Render();
            Built = true;
        }

        return Document;
    }

    internal void AddOptionalText(string text, PdfParagraphStyle style, double spaceAfter)
    {
        if (text.Length == 0)
        {
            return;
        }

        Layout.AddTextBlock(new PdfTextBlock(text, style));
        if (spaceAfter > 0)
        {
            Layout.AddBlock(PdfBlock.WithSize(0, spaceAfter));
        }
    }

    internal void ValidateText(string text, string name)
    {
        if (text == null)
        {
            throw new ArgumentNullException(name);
        }
    }

    internal void EnsureNotBuilt()
    {
        if (Built)
        {
            throw new InvalidOperationException("Letter has already been built.");
        }
    }
}
