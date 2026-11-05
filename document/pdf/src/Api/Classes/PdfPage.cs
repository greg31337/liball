using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents one PDF page, including its dimensions, drawing canvas, margins, links, and annotations.
/// </summary>
public class PdfPage : IPdfPage
{
    /// <summary>
    /// Specifies the width in PDF points.
    /// </summary>
    internal double Width;
    /// <summary>
    /// Specifies the height in PDF points.
    /// </summary>
    internal double Height;
    /// <summary>
    /// Stores the canvas used by this object.
    /// </summary>
    internal PdfCanvas Canvas;
    /// <summary>
    /// Stores the margins used by this object.
    /// </summary>
    internal PdfPageMargins Margins;
    internal PdfRectangle MediaBox;
    internal PdfRectangle CropBox;
    internal PdfRectangle BleedBox;
    internal PdfRectangle TrimBox;
    internal PdfRectangle ArtBox;
    internal bool HasCropBox;
    internal bool HasBleedBox;
    internal bool HasTrimBox;
    internal bool HasArtBox;
    internal List<PdfInternalLink> _internalLinks;
    internal List<PdfExternalLink> _externalLinks;
    internal List<PdfAnnotation> _annotations;
    /// <summary>
    /// Exposes the internal links currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfInternalLink> InternalLinks;
    /// <summary>
    /// Exposes the external links currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfExternalLink> ExternalLinks;
    /// <summary>
    /// Exposes the annotations currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfAnnotation> Annotations;
    internal PdfDocument? OwnerDocument;

    /// <summary>
    /// Initializes a new page.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is not finite and positive.</exception>
    public PdfPage(double width, double height)
    {
        ValidateDimension(width, "width");
        ValidateDimension(height, "height");
        Width = width;
        Height = height;
        Canvas = new PdfCanvas();
        Margins = new PdfPageMargins(0, 0, 0, 0);
        MediaBox = new PdfRectangle(0, 0, width, height);
        CropBox = new PdfRectangle(0, 0, width, height);
        BleedBox = new PdfRectangle(0, 0, width, height);
        TrimBox = new PdfRectangle(0, 0, width, height);
        ArtBox = new PdfRectangle(0, 0, width, height);
        _internalLinks = new List<PdfInternalLink>();
        _externalLinks = new List<PdfExternalLink>();
        _annotations = new List<PdfAnnotation>();
        InternalLinks = _internalLinks;
        ExternalLinks = _externalLinks;
        Annotations = _annotations;
        OwnerDocument = null;
    }

    internal static void ValidateDimension(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Page dimensions must be finite and positive.");
        }
    }

    /// <summary>
    /// Returns the canvas that records drawing commands for this page.
    /// </summary>
    /// <returns>The page's drawing canvas.</returns>
    public PdfCanvas GetCanvas()
    {
        return Canvas;
    }

    /// <summary>
    /// Returns this page's dimensions in PDF points.
    /// </summary>
    /// <returns>A new dimensions value containing the page width and height.</returns>
    public PdfPageDimensions GetDimensions()
    {
        return new PdfPageDimensions(Width, Height);
    }

    /// <summary>
    /// Sets the physical medium boundary and updates the page dimensions.
    /// </summary>
    /// <param name="box">The positive finite media rectangle in default user space.</param>
    /// <exception cref="ArgumentNullException">Thrown when the box is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the rectangle is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an existing page box would fall outside the new media box.</exception>
    public void SetMediaBox(PdfRectangle box)
    {
        ValidatePageBox(box, "box");
        if (HasCropBox && !ContainsPageBox(box, CropBox) || HasBleedBox && !ContainsPageBox(box, BleedBox) || HasTrimBox && !ContainsPageBox(box, TrimBox) || HasArtBox && !ContainsPageBox(box, ArtBox))
        {
            throw new InvalidOperationException("The media box must contain every configured page box.");
        }

        if (Margins.Left + Margins.Right >= box.Width || Margins.Top + Margins.Bottom >= box.Height)
        {
            throw new InvalidOperationException("The media box must leave positive content dimensions after applying page margins.");
        }

        MediaBox = CopyPageBox(box);
        Width = box.Width;
        Height = box.Height;
        if (!HasCropBox)
        {
            CropBox = CopyPageBox(box);
        }

        if (!HasBleedBox)
        {
            BleedBox = CopyPageBox(CropBox);
        }

        if (!HasTrimBox)
        {
            TrimBox = CopyPageBox(CropBox);
        }

        if (!HasArtBox)
        {
            ArtBox = CopyPageBox(CropBox);
        }
    }

    /// <summary>
    /// Sets the visible page boundary.
    /// </summary>
    /// <param name="box">The positive finite crop rectangle contained by the media box.</param>
    public void SetCropBox(PdfRectangle box)
    {
        ValidateContainedPageBox(box, "box");
        CropBox = CopyPageBox(box);
        HasCropBox = true;
        if (!HasBleedBox)
        {
            BleedBox = CopyPageBox(box);
        }

        if (!HasTrimBox)
        {
            TrimBox = CopyPageBox(box);
        }

        if (!HasArtBox)
        {
            ArtBox = CopyPageBox(box);
        }
    }

    /// <summary>
    /// Sets the production bleed boundary.
    /// </summary>
    /// <param name="box">The positive finite bleed rectangle contained by the media box.</param>
    public void SetBleedBox(PdfRectangle box)
    {
        ValidateContainedPageBox(box, "box");
        BleedBox = CopyPageBox(box);
        HasBleedBox = true;
    }

    /// <summary>
    /// Sets the finished-page trim boundary.
    /// </summary>
    /// <param name="box">The positive finite trim rectangle contained by the media box.</param>
    public void SetTrimBox(PdfRectangle box)
    {
        ValidateContainedPageBox(box, "box");
        TrimBox = CopyPageBox(box);
        HasTrimBox = true;
    }

    /// <summary>
    /// Sets the meaningful-content art boundary.
    /// </summary>
    /// <param name="box">The positive finite art rectangle contained by the media box.</param>
    public void SetArtBox(PdfRectangle box)
    {
        ValidateContainedPageBox(box, "box");
        ArtBox = CopyPageBox(box);
        HasArtBox = true;
    }

    /// <summary>
    /// Returns a copy of the physical media boundary.
    /// </summary>
    /// <returns>The configured media box.</returns>
    public PdfRectangle GetMediaBox()
    {
        return CopyPageBox(MediaBox);
    }

    /// <summary>
    /// Returns a copy of the effective crop boundary, which defaults to the media box.
    /// </summary>
    /// <returns>The effective crop box.</returns>
    public PdfRectangle GetCropBox()
    {
        return CopyPageBox(CropBox);
    }

    /// <summary>
    /// Returns a copy of the effective bleed boundary, which defaults to the crop box.
    /// </summary>
    /// <returns>The effective bleed box.</returns>
    public PdfRectangle GetBleedBox()
    {
        return CopyPageBox(BleedBox);
    }

    /// <summary>
    /// Returns a copy of the effective trim boundary, which defaults to the crop box.
    /// </summary>
    /// <returns>The effective trim box.</returns>
    public PdfRectangle GetTrimBox()
    {
        return CopyPageBox(TrimBox);
    }

    /// <summary>
    /// Returns a copy of the effective art boundary, which defaults to the crop box.
    /// </summary>
    /// <returns>The effective art box.</returns>
    public PdfRectangle GetArtBox()
    {
        return CopyPageBox(ArtBox);
    }

    /// <summary>
    /// Sets the uniform margins used for subsequent PDF generation.
    /// </summary>
    /// <param name="margin">The margin to use for this operation.</param>
    public void SetUniformMargins(double margin)
    {
        SetMargins(margin, margin, margin, margin);
    }

    /// <summary>
    /// Sets the margins used for subsequent PDF generation.
    /// </summary>
    /// <param name="left">The left in PDF points.</param>
    /// <param name="top">The top in PDF points.</param>
    /// <param name="right">The right in PDF points.</param>
    /// <param name="bottom">The bottom in PDF points.</param>
    public void SetMargins(double left, double top, double right, double bottom)
    {
        ValidateMargins(left, top, right, bottom);
        Margins = new PdfPageMargins(left, top, right, bottom);
    }

    /// <summary>
    /// Returns the content area calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting rectangle.</returns>
    public PdfRectangle GetContentArea()
    {
        return new PdfRectangle(MediaBox.X + Margins.Left, MediaBox.Y + Margins.Bottom, GetContentWidth(), GetContentHeight());
    }

    /// <summary>
    /// Returns the content left calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetContentLeft()
    {
        return MediaBox.X + Margins.Left;
    }

    /// <summary>
    /// Returns the content right calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetContentRight()
    {
        return MediaBox.X + Width - Margins.Right;
    }

    /// <summary>
    /// Returns the content top calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetContentTop()
    {
        return MediaBox.Y + Height - Margins.Top;
    }

    /// <summary>
    /// Returns the content bottom calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetContentBottom()
    {
        return MediaBox.Y + Margins.Bottom;
    }

    /// <summary>
    /// Returns the content width calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetContentWidth()
    {
        return Width - Margins.Left - Margins.Right;
    }

    /// <summary>
    /// Returns the content height calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetContentHeight()
    {
        return Height - Margins.Top - Margins.Bottom;
    }

    /// <summary>
    /// Adds an internal link to this page and returns the stored value.
    /// </summary>
    /// <param name="destinationName">The destination name to use for this operation.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfInternalLink AddInternalLink(string destinationName, double x, double y, double width, double height)
    {
        ValidateDestinationName(destinationName);
        ValidateLinkRectangle(x, y, width, height);

        PdfInternalLink link = new PdfInternalLink(destinationName, new PdfRectangle(x, y, width, height));
        _internalLinks.Add(link);
        return link;
    }

    /// <summary>
    /// Adds an internal link to this page and returns the stored value.
    /// </summary>
    /// <param name="destinationName">The destination name to use for this operation.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfInternalLink AddInternalLinkInRectangle(string destinationName, PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return AddInternalLink(destinationName, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    /// Adds an internal link whose destination is tied to an accessibility structure node.
    /// </summary>
    /// <param name="target">The structure-aware navigation target.</param>
    /// <param name="rectangle">The clickable rectangle.</param>
    /// <returns>The stored structure-aware internal link.</returns>
    public PdfInternalLink AddStructureInternalLink(PdfStructureLinkTarget target, PdfRectangle rectangle)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        if (OwnerDocument == null || target.Destination.Page.OwnerDocument != OwnerDocument)
        {
            throw new ArgumentException("Structure link target must belong to the same document as the source page.", "target");
        }

        PdfInternalLink link = AddInternalLinkInRectangle(target.Destination.Name, rectangle);
        link.SetStructureTarget(target.StructureNode);
        return link;
    }

    /// <summary>
    /// Adds an external link to this page and returns the stored value.
    /// </summary>
    /// <param name="uri">The absolute or relative URI opened by the link.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfExternalLink AddExternalLink(string uri, double x, double y, double width, double height)
    {
        ValidateUri(uri);
        ValidateLinkRectangle(x, y, width, height);

        PdfExternalLink link = new PdfExternalLink(uri, new PdfRectangle(x, y, width, height));
        _externalLinks.Add(link);
        return link;
    }

    /// <summary>
    /// Adds an external link to this page and returns the stored value.
    /// </summary>
    /// <param name="uri">The absolute or relative URI opened by the link.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfExternalLink AddExternalLinkInRectangle(string uri, PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return AddExternalLink(uri, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    /// Adds a text annotation to this page and returns the stored value.
    /// </summary>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfAnnotation AddTextAnnotation(string contents, double x, double y, double width, double height)
    {
        ValidateAnnotationContents(contents);
        ValidateAnnotationRectangle(x, y, width, height);

        PdfAnnotation annotation = new PdfAnnotation("Text", contents, new PdfRectangle(x, y, width, height));
        _annotations.Add(annotation);
        return annotation;
    }

    /// <summary>
    /// Adds a text annotation to this page and returns the stored value.
    /// </summary>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAnnotation AddTextAnnotationInRectangle(string contents, PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return AddTextAnnotation(contents, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    /// Adds a square annotation to this page and returns the stored value.
    /// </summary>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfAnnotation AddSquareAnnotation(string contents, double x, double y, double width, double height)
    {
        ValidateAnnotationContents(contents);
        ValidateAnnotationRectangle(x, y, width, height);

        PdfAnnotation annotation = new PdfAnnotation("Square", contents, new PdfRectangle(x, y, width, height));
        _annotations.Add(annotation);
        return annotation;
    }

    /// <summary>
    /// Adds a square annotation to this page and returns the stored value.
    /// </summary>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAnnotation AddSquareAnnotationInRectangle(string contents, PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return AddSquareAnnotation(contents, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    internal void ValidateContainedPageBox(PdfRectangle box, string name)
    {
        ValidatePageBox(box, name);
        if (!ContainsPageBox(MediaBox, box))
        {
            throw new ArgumentException("Page box must be contained by the media box.", name);
        }
    }

    internal static void ValidatePageBox(PdfRectangle box, string name)
    {
        if (box == null)
        {
            throw new ArgumentNullException(name);
        }

        if (double.IsNaN(box.X) || double.IsInfinity(box.X) || double.IsNaN(box.Y) || double.IsInfinity(box.Y) || double.IsNaN(box.Width) || double.IsInfinity(box.Width) || double.IsNaN(box.Height) || double.IsInfinity(box.Height) || box.Width <= 0 || box.Height <= 0 || double.IsInfinity(box.X + box.Width) || double.IsInfinity(box.Y + box.Height))
        {
            throw new ArgumentOutOfRangeException(name, "Page box coordinates must be finite and dimensions must be positive and finite.");
        }
    }

    internal static bool ContainsPageBox(PdfRectangle outer, PdfRectangle inner)
    {
        return inner.X >= outer.X && inner.Y >= outer.Y && inner.X + inner.Width <= outer.X + outer.Width && inner.Y + inner.Height <= outer.Y + outer.Height;
    }

    internal static PdfRectangle CopyPageBox(PdfRectangle box)
    {
        return new PdfRectangle(box.X, box.Y, box.Width, box.Height);
    }

    internal void ValidateMargins(double left, double top, double right, double bottom)
    {
        if (double.IsNaN(left) || double.IsInfinity(left) || double.IsNaN(top) || double.IsInfinity(top) || double.IsNaN(right) || double.IsInfinity(right) || double.IsNaN(bottom) || double.IsInfinity(bottom))
        {
            throw new ArgumentOutOfRangeException("left", "Page margins must be finite.");
        }

        if (left < 0)
        {
            throw new ArgumentOutOfRangeException("left", "Page margins cannot be negative.");
        }

        if (top < 0)
        {
            throw new ArgumentOutOfRangeException("top", "Page margins cannot be negative.");
        }

        if (right < 0)
        {
            throw new ArgumentOutOfRangeException("right", "Page margins cannot be negative.");
        }

        if (bottom < 0)
        {
            throw new ArgumentOutOfRangeException("bottom", "Page margins cannot be negative.");
        }

        if (left + right >= Width)
        {
            throw new ArgumentException("Left and right margins must leave positive content width.");
        }

        if (top + bottom >= Height)
        {
            throw new ArgumentException("Top and bottom margins must leave positive content height.");
        }
    }

    internal static void ValidateDestinationName(string destinationName)
    {
        if (destinationName == null)
        {
            throw new ArgumentNullException("destinationName");
        }

        if (destinationName.Length == 0)
        {
            throw new ArgumentException("Destination name cannot be empty.", "destinationName");
        }
    }

    internal static void ValidateLinkRectangle(double x, double y, double width, double height)
    {
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
        {
            throw new ArgumentOutOfRangeException("x", "Link coordinates must be finite.");
        }

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Link width must be positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Link height must be positive.");
        }
    }

    internal static void ValidateUri(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        if (uri.Length == 0)
        {
            throw new ArgumentException("URI cannot be empty.", "uri");
        }
    }

    internal static void ValidateAnnotationContents(string contents)
    {
        if (contents == null)
        {
            throw new ArgumentNullException("contents");
        }
    }

    internal static void ValidateAnnotationRectangle(double x, double y, double width, double height)
    {
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
        {
            throw new ArgumentOutOfRangeException("x", "Annotation coordinates must be finite.");
        }

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Annotation width must be positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Annotation height must be positive.");
        }
    }
}
