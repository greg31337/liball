using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfFontMetrics"/>.
/// </summary>
public interface IPdfFontMetrics
{
    /// <summary>
    /// Returns the character width calculated from the object's current state.
    /// </summary>
    /// <param name="character">The character to use for this operation.</param>
    /// <returns>The calculated integer value.</returns>
    int GetCharacterWidth(char character);

    /// <summary>
    /// Measures a text width without emitting drawing commands.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    double MeasureTextWidth(string text, double fontSize);

    /// <summary>
    /// Returns the line height calculated from the object's current state.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    double GetLineHeight(double fontSize);
}
