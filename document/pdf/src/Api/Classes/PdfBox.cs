using System;

namespace pdf;

/// <summary>
/// Represents a box used by the PDF generation API.
/// </summary>
public class PdfBox : IPdfBox
{
    /// <summary>
    /// Stores the content rectangle used by this object.
    /// </summary>
    internal PdfRectangle ContentRectangle = new PdfRectangle(0, 0, 0, 0);
    /// <summary>
    /// Stores the padding used by this object.
    /// </summary>
    internal PdfInsets Padding = PdfInsets.None();
    /// <summary>
    /// Stores the border used by this object.
    /// </summary>
    internal PdfInsets Border = PdfInsets.None();
    /// <summary>
    /// Stores the margin used by this object.
    /// </summary>
    internal PdfInsets Margin = PdfInsets.None();

    /// <summary>
    /// Initializes a new box.
    /// </summary>
    /// <param name="contentRectangle">The content rectangle to use for this operation.</param>
    /// <param name="padding">The padding to use for this operation.</param>
    /// <param name="border">The border to use for this operation.</param>
    /// <param name="margin">The margin to use for this operation.</param>
    public PdfBox(PdfRectangle contentRectangle, PdfInsets padding, PdfInsets border, PdfInsets margin)
    {
        Initialize(contentRectangle, padding, border, margin);
    }

    /// <summary>
    /// Creates a box from content coordinates without insets.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting box.</returns>
    internal static PdfBox FromCoordinates(double x, double y, double width, double height)
    {
        return FromContentRectangle(new PdfRectangle(x, y, width, height));
    }

    /// <summary>
    /// Creates a box from a content rectangle without insets.
    /// </summary>
    /// <param name="contentRectangle">The content rectangle to use for this operation.</param>
    /// <returns>The resulting box.</returns>
    internal static PdfBox FromContentRectangle(PdfRectangle contentRectangle)
    {
        return new PdfBox(contentRectangle, PdfInsets.None(), PdfInsets.None(), PdfInsets.None());
    }

    /// <summary>
    /// Returns the padding rectangle calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting rectangle.</returns>
    public PdfRectangle GetPaddingRectangle()
    {
        return Expand(ContentRectangle, Padding);
    }

    /// <summary>
    /// Returns the border rectangle calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting rectangle.</returns>
    public PdfRectangle GetBorderRectangle()
    {
        PdfRectangle paddingRectangle = GetPaddingRectangle();
        return Expand(paddingRectangle, Border);
    }

    /// <summary>
    /// Returns the margin rectangle calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting rectangle.</returns>
    public PdfRectangle GetMarginRectangle()
    {
        PdfRectangle borderRectangle = GetBorderRectangle();
        return Expand(borderRectangle, Margin);
    }

    /// <summary>
    /// Returns the content size calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting layout size.</returns>
    public PdfLayoutSize GetContentSize()
    {
        return new PdfLayoutSize(ContentRectangle.Width, ContentRectangle.Height);
    }

    /// <summary>
    /// Returns the outer size calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting layout size.</returns>
    public PdfLayoutSize GetOuterSize()
    {
        return new PdfLayoutSize(GetOuterWidth(), GetOuterHeight());
    }

    /// <summary>
    /// Returns the outer width calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetOuterWidth()
    {
        return ContentRectangle.Width + Padding.GetHorizontal() + Border.GetHorizontal() + Margin.GetHorizontal();
    }

    /// <summary>
    /// Returns the outer height calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetOuterHeight()
    {
        return ContentRectangle.Height + Padding.GetVertical() + Border.GetVertical() + Margin.GetVertical();
    }

    /// <summary>
    /// Creates and returns a box from the supplied margin rectangle.
    /// </summary>
    /// <param name="marginRectangle">The margin rectangle to use for this operation.</param>
    /// <param name="padding">The padding to use for this operation.</param>
    /// <param name="border">The border to use for this operation.</param>
    /// <param name="margin">The margin to use for this operation.</param>
    /// <returns>The resulting box.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    internal static PdfBox FromMarginRectangle(PdfRectangle marginRectangle, PdfInsets padding, PdfInsets border, PdfInsets margin)
    {
        ValidateRectangle(marginRectangle);
        ValidateInsets(padding, "padding");
        ValidateInsets(border, "border");
        ValidateInsets(margin, "margin");

        double insetWidth = padding.GetHorizontal() + border.GetHorizontal() + margin.GetHorizontal();
        double insetHeight = padding.GetVertical() + border.GetVertical() + margin.GetVertical();
        if (insetWidth > marginRectangle.Width)
        {
            throw new ArgumentException("Insets cannot exceed margin rectangle width.");
        }

        if (insetHeight > marginRectangle.Height)
        {
            throw new ArgumentException("Insets cannot exceed margin rectangle height.");
        }

        double contentX = marginRectangle.X + margin.Left + border.Left + padding.Left;
        double contentY = marginRectangle.Y + margin.Bottom + border.Bottom + padding.Bottom;
        double contentWidth = marginRectangle.Width - insetWidth;
        double contentHeight = marginRectangle.Height - insetHeight;
        PdfRectangle contentRectangle = new PdfRectangle(contentX, contentY, contentWidth, contentHeight);

        return new PdfBox(contentRectangle, padding, border, margin);
    }

    internal void Initialize(PdfRectangle contentRectangle, PdfInsets padding, PdfInsets border, PdfInsets margin)
    {
        ValidateRectangle(contentRectangle);
        ValidateInsets(padding, "padding");
        ValidateInsets(border, "border");
        ValidateInsets(margin, "margin");

        ContentRectangle = contentRectangle;
        Padding = padding;
        Border = border;
        Margin = margin;
    }

    internal static PdfRectangle Expand(PdfRectangle rectangle, PdfInsets insets)
    {
        return new PdfRectangle(
            rectangle.X - insets.Left,
            rectangle.Y - insets.Bottom,
            rectangle.Width + insets.GetHorizontal(),
            rectangle.Height + insets.GetVertical());
    }

    internal static void ValidateRectangle(PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("contentRectangle");
        }

        if (double.IsNaN(rectangle.X) || double.IsInfinity(rectangle.X) || double.IsNaN(rectangle.Y) || double.IsInfinity(rectangle.Y))
        {
            throw new ArgumentOutOfRangeException("contentRectangle", "Box rectangle coordinates must be finite.");
        }

        if (double.IsNaN(rectangle.Width) || double.IsInfinity(rectangle.Width) || rectangle.Width < 0)
        {
            throw new ArgumentOutOfRangeException("contentRectangle", "Box rectangle width must be finite and nonnegative.");
        }

        if (double.IsNaN(rectangle.Height) || double.IsInfinity(rectangle.Height) || rectangle.Height < 0)
        {
            throw new ArgumentOutOfRangeException("contentRectangle", "Box rectangle height must be finite and nonnegative.");
        }
    }

    internal static void ValidateInsets(PdfInsets insets, string name)
    {
        if (insets == null)
        {
            throw new ArgumentNullException(name);
        }
    }
}
