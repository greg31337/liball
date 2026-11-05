using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfOutlineItem"/>.
/// </summary>
public interface IPdfOutlineItem
{
    /// <summary>
    /// Adds a child to this outline item.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddChild(PdfOutlineItem item);
}
