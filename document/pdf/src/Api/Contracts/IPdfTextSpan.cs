using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfTextSpan"/>.
/// </summary>
public interface IPdfTextSpan
{
    /// <summary>
    /// Measures a width without emitting drawing commands.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double MeasureWidth();
}
