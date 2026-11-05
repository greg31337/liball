using System;

namespace pdf;

/// <summary>
/// Contains the outcome of layout.
/// </summary>
public class PdfLayoutResult
{
    /// <summary>
    /// Stores the size used by this object.
    /// </summary>
    public PdfLayoutSize Size = new PdfLayoutSize(0, 0);
    /// <summary>
    /// Stores the box used by this object.
    /// </summary>
    public PdfBox Box = PdfBox.FromCoordinates(0, 0, 0, 0);
    /// <summary>
    /// Indicates whether fits is enabled.
    /// </summary>
    public bool Fits;
    /// <summary>
    /// Exposes the lines currently stored by this object.
    /// </summary>
    public string[] Lines = new string[0];
    internal bool HasDebugBounds;
    internal PdfRectangle DebugBounds = new PdfRectangle(0, 0, 0, 0);

    /// <summary>
    /// Creates a layout result that includes wrapped text lines.
    /// </summary>
    /// <param name="size">The size to use for this operation.</param>
    /// <param name="box">The box to use for this operation.</param>
    /// <param name="fits">The fits to use for this operation.</param>
    public PdfLayoutResult(PdfLayoutSize size, PdfBox box, bool fits)
    {
        ValidateSize(size);
        ValidateBox(box);

        Size = size;
        Box = box;
        Fits = fits;
        Lines = new string[0];
        HasDebugBounds = false;
        DebugBounds = new PdfRectangle(0, 0, 0, 0);
    }

    /// <summary>
    /// Initializes a new layout result.
    /// </summary>
    /// <param name="size">The size to use for this operation.</param>
    /// <param name="box">The box to use for this operation.</param>
    /// <param name="fits">The fits to use for this operation.</param>
    /// <param name="lines">The lines to use for this operation.</param>
    /// <returns>The resulting layout result.</returns>
    internal static PdfLayoutResult WithLines(PdfLayoutSize size, PdfBox box, bool fits, string[] lines)
    {
        ValidateLines(lines);

        PdfLayoutResult result = new PdfLayoutResult(size, box, fits);
        result.Lines = lines;
        return result;
    }

    internal static void ValidateSize(PdfLayoutSize size)
    {
        if (size == null)
        {
            throw new ArgumentNullException("size");
        }
    }

    internal static void ValidateBox(PdfBox box)
    {
        if (box == null)
        {
            throw new ArgumentNullException("box");
        }
    }

    internal static void ValidateLines(string[] lines)
    {
        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }
    }

    internal void SetDebugBounds(PdfRectangle bounds)
    {
        if (bounds == null)
        {
            throw new ArgumentNullException("bounds");
        }

        HasDebugBounds = true;
        DebugBounds = bounds;
    }
}
