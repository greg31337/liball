using System;

namespace pdf;

/// <summary>
/// Represents an annotation used by the PDF generation API.
/// </summary>
public class PdfAnnotation : IPdfAnnotation
{
    /// <summary>
    /// Stores the subtype used by this object.
    /// </summary>
    internal string Subtype;
    /// <summary>
    /// Stores the contents used by this object.
    /// </summary>
    internal string Contents;
    /// <summary>
    /// Stores the rectangle used by this object.
    /// </summary>
    internal PdfRectangle Rectangle;
    /// <summary>
    /// Stores the color used by this object.
    /// </summary>
    internal PdfColor Color;
    /// <summary>
    /// Indicates whether this object has color.
    /// </summary>
    internal bool HasColor;
    /// <summary>
    /// Indicates whether open is enabled.
    /// </summary>
    internal bool Open;
    /// <summary>
    /// Stores the icon name used by this object.
    /// </summary>
    internal string IconName;
    internal PdfAccessibilityNode? TagNode;
    internal int StructParent;

    /// <summary>
    /// Initializes a new annotation.
    /// </summary>
    /// <param name="subtype">The subtype to use for this operation.</param>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAnnotation(string subtype, string contents, PdfRectangle rectangle)
    {
        if (subtype == null)
        {
            throw new ArgumentNullException("subtype");
        }

        if (contents == null)
        {
            throw new ArgumentNullException("contents");
        }

        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        if (subtype.Length == 0)
        {
            throw new ArgumentException("Annotation subtype cannot be empty.", "subtype");
        }

        PdfPage.ValidateAnnotationRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

        Subtype = subtype;
        Contents = contents;
        Rectangle = new PdfRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        Color = PdfColor.Rgb(1, 1, 0);
        HasColor = false;
        Open = false;
        IconName = "Note";
        TagNode = null;
        StructParent = -1;
    }

    /// <summary>
    /// Sets the color used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetColor(PdfColor color)
    {
        if (color == null)
        {
            throw new ArgumentNullException("color");
        }

        PdfElementStyle.ValidateColor(color, "color");

        Color = PdfElementStyle.CopyColor(color);
        HasColor = true;
    }

    /// <summary>
    /// Sets the open used for subsequent PDF generation.
    /// </summary>
    /// <param name="open">The open to use for this operation.</param>
    public void SetOpen(bool open)
    {
        Open = open;
    }

    /// <summary>
    /// Sets the icon name used for subsequent PDF generation.
    /// </summary>
    /// <param name="iconName">The icon name to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public void SetIconName(string iconName)
    {
        if (iconName == null)
        {
            throw new ArgumentNullException("iconName");
        }

        if (iconName.Length == 0)
        {
            throw new ArgumentException("Annotation icon name cannot be empty.", "iconName");
        }

        IconName = iconName;
    }
}
