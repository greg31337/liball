using System;

namespace pdf;

/// <summary>
/// Represents a list item used by the PDF generation API.
/// </summary>
public class PdfListItem
{
    /// <summary>
    /// Stores the item used by this object.
    /// </summary>
    public IPdfLayoutElement Item = new PdfBlock();

    /// <summary>
    /// Initializes a new list item.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfListItem(IPdfLayoutElement item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        Item = item;
    }
}
