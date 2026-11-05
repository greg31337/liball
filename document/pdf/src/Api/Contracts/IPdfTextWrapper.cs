using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfTextWrapper"/>.
/// </summary>
public interface IPdfTextWrapper
{
    /// <summary>
    /// Splits the supplied text into lines that fit the requested width.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <returns>The resulting values in their processing order.</returns>
    string[] Wrap(string text);
}
