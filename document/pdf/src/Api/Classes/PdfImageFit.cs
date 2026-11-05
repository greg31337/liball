using System;

namespace pdf;

/// <summary>
/// Represents an image fit used by the PDF generation API.
/// </summary>
public class PdfImageFit : IPdfImageFit
{
    /// <summary>
    /// Creates an image-fit calculator.
    /// </summary>
    public PdfImageFit()
    {
    }

    /// <summary>
    /// Creates and returns an image placement configured for stretch.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting image placement.</returns>
    public PdfImagePlacement Stretch(double x, double y, double width, double height)
    {
        ValidateBox(width, height);

        return new PdfImagePlacement(x, y, width, height);
    }

    /// <summary>
    /// Creates and returns an image placement configured for preserve aspect fit.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting image placement.</returns>
    public PdfImagePlacement PreserveAspectFit(PdfImage image, double x, double y, double width, double height)
    {
        ValidateImage(image);
        ValidateBox(width, height);

        double scale = width / image.Width;
        double heightScale = height / image.Height;
        if (heightScale < scale)
        {
            scale = heightScale;
        }

        double drawWidth = image.Width * scale;
        double drawHeight = image.Height * scale;
        double drawX = x + (width - drawWidth) / 2;
        double drawY = y + (height - drawHeight) / 2;

        return new PdfImagePlacement(drawX, drawY, drawWidth, drawHeight);
    }

    /// <summary>
    /// Creates and returns an image placement configured for crop fill.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting image placement.</returns>
    public PdfImagePlacement CropFill(PdfImage image, double x, double y, double width, double height)
    {
        ValidateImage(image);
        ValidateBox(width, height);

        double scale = width / image.Width;
        double heightScale = height / image.Height;
        if (heightScale > scale)
        {
            scale = heightScale;
        }

        double drawWidth = image.Width * scale;
        double drawHeight = image.Height * scale;
        double drawX = x + (width - drawWidth) / 2;
        double drawY = y + (height - drawHeight) / 2;

        return new PdfImagePlacement(drawX, drawY, drawWidth, drawHeight);
    }

    internal static void ValidateImage(PdfImage image)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }
    }

    internal static void ValidateBox(double width, double height)
    {
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Image box width must be finite and positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Image box height must be finite and positive.");
        }
    }
}
