using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfInsets"/>.
/// </summary>
public interface IPdfInsets
{
    /// <summary>
    /// Returns the horizontal calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetHorizontal();

    /// <summary>
    /// Returns the vertical calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetVertical();
}
