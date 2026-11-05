using System;

namespace pdf;

/// <summary>
/// Defines page-label numbering beginning at an anchored document page.
/// </summary>
public class PdfPageLabelRange
{
    /// <summary>Stores the first page governed by this range.</summary>
    public PdfPage Page;
    /// <summary>Stores the numbering style used by this range.</summary>
    public PdfPageLabelStyle Style;
    /// <summary>Stores the text placed before the generated number.</summary>
    public string Prefix;
    /// <summary>Specifies the first generated number in this range.</summary>
    public int StartNumber;

    /// <summary>
    /// Creates a page-label range anchored to a document page.
    /// </summary>
    /// <param name="page">The first page governed by the range.</param>
    /// <param name="style">The numbering style.</param>
    /// <param name="prefix">The text placed before the generated number.</param>
    /// <param name="startNumber">The positive first generated number.</param>
    public PdfPageLabelRange(PdfPage page, PdfPageLabelStyle style, string prefix, int startNumber)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        PdfDocument.ValidatePageLabelStyle(style);
        if (startNumber <= 0)
        {
            throw new ArgumentOutOfRangeException("startNumber", "Page-label start number must be positive.");
        }

        if (style == PdfPageLabelStyle.None && prefix.Length == 0)
        {
            throw new ArgumentException("A prefix-only page-label range requires a non-empty prefix.", "prefix");
        }

        Page = page;
        Style = style;
        Prefix = prefix;
        StartNumber = startNumber;
    }
}
