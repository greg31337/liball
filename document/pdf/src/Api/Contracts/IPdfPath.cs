using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfPath"/>.
/// </summary>
public interface IPdfPath
{
    /// <summary>
    /// Moves the path's current point without drawing a segment.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <returns>The resulting path.</returns>
    PdfPath MoveTo(double x, double y);

    /// <summary>
    /// Adds a straight segment from the current path point to the supplied point.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <returns>The resulting path.</returns>
    PdfPath LineTo(double x, double y);

    /// <summary>
    /// Adds a cubic Bézier segment using two control points and an end point.
    /// </summary>
    /// <param name="x1">The first horizontal coordinate in PDF points.</param>
    /// <param name="y1">The first vertical coordinate in PDF points.</param>
    /// <param name="x2">The second horizontal coordinate in PDF points.</param>
    /// <param name="y2">The second vertical coordinate in PDF points.</param>
    /// <param name="x3">The third horizontal coordinate in PDF points.</param>
    /// <param name="y3">The third vertical coordinate in PDF points.</param>
    /// <returns>The resulting path.</returns>
    PdfPath CurveTo(double x1, double y1, double x2, double y2, double x3, double y3);

    /// <summary>
    /// Closes the current path by connecting its last point to its first point.
    /// </summary>
    /// <returns>The resulting path.</returns>
    PdfPath Close();

    /// <summary>
    /// Determines whether the path contains at least one segment command.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    bool HasCommands();
}
