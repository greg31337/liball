using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents an outline item used by the PDF generation API.
/// </summary>
public class PdfOutlineItem : IPdfOutlineItem
{
    internal List<PdfOutlineItem> _children;
    /// <summary>
    /// Stores the title used by this object.
    /// </summary>
    internal string Title;
    /// <summary>
    /// Stores the page used by this object.
    /// </summary>
    internal PdfPage Page;
    /// <summary>
    /// Specifies the x in PDF points.
    /// </summary>
    internal double X;
    /// <summary>
    /// Specifies the y in PDF points.
    /// </summary>
    internal double Y;
    /// <summary>
    /// Stores the parent outline item, or null when this is a root item.
    /// </summary>
    internal PdfOutlineItem? Parent;
    /// <summary>
    /// Exposes the children currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfOutlineItem> Children;

    /// <summary>
    /// Initializes a new outline item.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfOutlineItem(string title, PdfPage page, double x, double y)
    {
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        PdfDocument.ValidateOutlineTitle(title);
        PdfNamedDestination.ValidateCoordinates(x, y);

        _children = new List<PdfOutlineItem>();
        Title = title;
        Page = page;
        X = x;
        Y = y;
        Parent = null;
        Children = _children;
    }

    /// <summary>
    /// Adds a child to this outline item.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the item is already parented or would create a cycle.</exception>
    public void AddChild(PdfOutlineItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        if (item == this)
        {
            throw new ArgumentException("An outline item cannot be its own child.", "item");
        }

        PdfOutlineItem? ancestor = this;
        while (ancestor != null)
        {
            if (ancestor == item)
            {
                throw new ArgumentException("Adding this outline item would create a cycle.", "item");
            }

            ancestor = ancestor.Parent;
        }

        if (item.Parent != null && item.Parent != this)
        {
            throw new ArgumentException("An outline item cannot be assigned to multiple parents.", "item");
        }

        foreach (PdfOutlineItem child in _children)
        {
            if (child == item)
            {
                throw new ArgumentException("The outline child has already been added.", "item");
            }
        }

        item.Parent = this;
        _children.Add(item);
    }
}
