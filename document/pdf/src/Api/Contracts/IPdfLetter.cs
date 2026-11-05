using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfLetter"/>.
/// </summary>
public interface IPdfLetter
{
    /// <summary>
    /// Sets the sender used for subsequent PDF generation.
    /// </summary>
    /// <param name="sender">The sender to use for this operation.</param>
    void SetSender(string sender);

    /// <summary>
    /// Sets the recipient used for subsequent PDF generation.
    /// </summary>
    /// <param name="recipient">The recipient to use for this operation.</param>
    void SetRecipient(string recipient);

    /// <summary>
    /// Sets the date used for subsequent PDF generation.
    /// </summary>
    /// <param name="dateText">The date text to use for this operation.</param>
    void SetDate(string dateText);

    /// <summary>
    /// Sets the subject used for subsequent PDF generation.
    /// </summary>
    /// <param name="subject">The subject to use for this operation.</param>
    void SetSubject(string subject);

    /// <summary>
    /// Sets the body used for subsequent PDF generation.
    /// </summary>
    /// <param name="body">The body to use for this operation.</param>
    void SetBody(string body);

    /// <summary>
    /// Sets the closing used for subsequent PDF generation.
    /// </summary>
    /// <param name="closing">The closing to use for this operation.</param>
    void SetClosing(string closing);

    /// <summary>
    /// Sets the signature used for subsequent PDF generation.
    /// </summary>
    /// <param name="signature">The signature to use for this operation.</param>
    void SetSignature(string signature);

    /// <summary>
    /// Builds a letter from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    PdfDocument Build();
}
