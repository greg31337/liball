using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfBox"/>.
/// </summary>
public interface IPdfBox
{
    /// <summary>
    /// Returns the padding rectangle calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting rectangle.</returns>
    PdfRectangle GetPaddingRectangle();

    /// <summary>
    /// Returns the border rectangle calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting rectangle.</returns>
    PdfRectangle GetBorderRectangle();

    /// <summary>
    /// Returns the margin rectangle calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting rectangle.</returns>
    PdfRectangle GetMarginRectangle();

    /// <summary>
    /// Returns the content size calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting layout size.</returns>
    PdfLayoutSize GetContentSize();

    /// <summary>
    /// Returns the outer size calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting layout size.</returns>
    PdfLayoutSize GetOuterSize();

    /// <summary>
    /// Returns the outer width calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetOuterWidth();

    /// <summary>
    /// Returns the outer height calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetOuterHeight();
}
