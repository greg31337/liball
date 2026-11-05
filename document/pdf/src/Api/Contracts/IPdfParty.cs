using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfParty"/>.
/// </summary>
public interface IPdfParty
{
    /// <summary>
    /// Adds a line to this party.
    /// </summary>
    /// <param name="line">The line to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddLine(string line);

    /// <summary>
    /// Sets the tax identifier used for subsequent PDF generation.
    /// </summary>
    /// <param name="taxIdentifier">The tax identifier to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetTaxIdentifier(string taxIdentifier);

    /// <summary>
    /// Sets the email used for subsequent PDF generation.
    /// </summary>
    /// <param name="email">The email to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetEmail(string email);
}
