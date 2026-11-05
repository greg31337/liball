using System;

namespace pdf;

/// <summary>
/// Defines a reusable page-template layer with stable ordering and optional regional clipping.
/// </summary>
public class PdfContentLayer : IPdfContentLayer
{
    internal PdfPageTemplate Template = new PdfPageTemplate(new PdfBlock(), 0);
    internal PdfContentLayerPlacement Placement;
    internal int Order;
    internal bool HasBounds;
    internal PdfRectangle Bounds = new PdfRectangle(0, 0, 0, 0);
    internal bool ClipToBounds;

    /// <summary>
    /// Initializes a reusable ordered content layer.
    /// </summary>
    /// <param name="template">The reusable page template drawn by this layer.</param>
    /// <param name="placement">The layer band relative to normal page content.</param>
    /// <param name="order">The order within the selected band; lower values draw first.</param>
    /// <exception cref="ArgumentNullException">Thrown when template is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when placement is not supported.</exception>
    public PdfContentLayer(PdfPageTemplate template, PdfContentLayerPlacement placement, int order)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        ValidatePlacement(placement);
        Template = template;
        Placement = placement;
        Order = order;
        HasBounds = false;
        Bounds = new PdfRectangle(0, 0, 0, 0);
        ClipToBounds = true;
    }

    /// <summary>
    /// Sets the page-relative region used to measure, draw, clip, and debug the layer.
    /// </summary>
    /// <param name="bounds">The layer region in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when bounds is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a region dimension is not positive.</exception>
    public void SetBounds(PdfRectangle bounds)
    {
        if (bounds == null)
        {
            throw new ArgumentNullException("bounds");
        }

        ValidateCoordinate(bounds.X, "bounds");
        ValidateCoordinate(bounds.Y, "bounds");
        if (double.IsNaN(bounds.Width) || double.IsInfinity(bounds.Width) || bounds.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("bounds", "Content layer width must be finite and positive.");
        }

        if (double.IsNaN(bounds.Height) || double.IsInfinity(bounds.Height) || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("bounds", "Content layer height must be finite and positive.");
        }

        Bounds = new PdfRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        HasBounds = true;
    }

    /// <summary>
    /// Clips layer drawing to its configured region.
    /// </summary>
    public void EnableClipping()
    {
        ClipToBounds = true;
    }

    /// <summary>
    /// Allows layer drawing to extend beyond its configured region.
    /// </summary>
    public void DisableClipping()
    {
        ClipToBounds = false;
    }

    internal PdfCanvas DrawForPage(PdfPage page, int pageNumber, int totalPages)
    {
        double x = 0;
        double y = 0;
        double width = page.Width;
        double height = Template.Height;
        if (height <= 0)
        {
            height = page.Height;
        }

        if (HasBounds)
        {
            x = Bounds.X;
            y = Bounds.Y;
            width = Bounds.Width;
            height = Bounds.Height;
        }

        PdfPage layerPage = new PdfPage(page.Width, page.Height);
        layerPage.SetMargins(page.Margins.Left, page.Margins.Top, page.Margins.Right, page.Margins.Bottom);
        if (ClipToBounds)
        {
            layerPage.Canvas.SaveState();
            layerPage.Canvas.ClipRectangle(x, y, width, height);
        }

        Template.DrawForPageInRegion(layerPage, x, y, width, height, pageNumber, totalPages);
        if (ClipToBounds)
        {
            layerPage.Canvas.RestoreState();
        }

        return layerPage.Canvas;
    }

    internal PdfCanvas DrawDebugForPage(PdfPage page, PdfColor color, double lineWidth)
    {
        double x = 0;
        double y = 0;
        double width = page.Width;
        double height = Template.Height;
        if (height <= 0)
        {
            height = page.Height;
        }

        if (HasBounds)
        {
            x = Bounds.X;
            y = Bounds.Y;
            width = Bounds.Width;
            height = Bounds.Height;
        }

        PdfCanvas canvas = new PdfCanvas();
        canvas.SaveState();
        canvas.SetStrokeColorValue(color);
        canvas.SetLineWidth(lineWidth);
        canvas.StrokeRectangle(x, y, width, height);
        canvas.RestoreState();
        return canvas;
    }

    internal static void ValidatePlacement(PdfContentLayerPlacement placement)
    {
        if (placement != PdfContentLayerPlacement.Background &&
            placement != PdfContentLayerPlacement.Watermark &&
            placement != PdfContentLayerPlacement.Foreground)
        {
            throw new ArgumentOutOfRangeException("placement", "Content layer placement is not supported.");
        }
    }

    internal static void ValidateCoordinate(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, "Content layer coordinates must be finite.");
        }
    }
}
