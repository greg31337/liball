using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfCertificate"/>.
/// </summary>
public interface IPdfCertificate
{
    /// <summary>
    /// Sets the description used for subsequent PDF generation.
    /// </summary>
    /// <param name="description">The description to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetDescription(string description);

    /// <summary>
    /// Sets the issuer used for subsequent PDF generation.
    /// </summary>
    /// <param name="issuer">A value indicating whether issuer.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetIssuer(string issuer);

    /// <summary>
    /// Sets the date used for subsequent PDF generation.
    /// </summary>
    /// <param name="dateText">The date text to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetDate(string dateText);

    /// <summary>
    /// Builds a certificate from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    PdfDocument Build();
}
