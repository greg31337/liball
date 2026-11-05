using System;
using System.Collections.Generic;
using System.Text;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfRichText"/>.
/// </summary>
public interface IPdfRichText
{
    /// <summary>
    /// Adds a span to this rich text and returns the stored value.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTextSpan AddSpan(string text);

    /// <summary>
    /// Adds a span to this rich text and returns the stored value.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTextSpan AddStyledSpan(string text, PdfTextStyle style);

    /// <summary>
    /// Adds a span to this rich text.
    /// </summary>
    /// <param name="span">The span to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddExistingSpan(PdfTextSpan span);

    /// <summary>
    /// Returns the text calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting text.</returns>
    string GetText();

    /// <summary>
    /// Measures a width without emitting drawing commands.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double MeasureWidth();

    /// <summary>
    /// Returns the line height calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetLineHeight();
}
