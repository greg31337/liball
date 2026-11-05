using System;

namespace pdf;

/// <summary>
/// Represents a image element in the paginated PDF layout system.
/// </summary>
public class PdfImageBlock : IPdfLayoutElement, IPdfImageBlock
{
    /// <summary>
    /// Stores the image used by this object.
    /// </summary>
    internal PdfImage Image;
    /// <summary>
    /// Specifies the width in PDF points.
    /// </summary>
    internal double Width;
    /// <summary>
    /// Specifies the height in PDF points.
    /// </summary>
    internal double Height;
    /// <summary>
    /// Stores how the image is placed inside its layout box.
    /// </summary>
    internal PdfImageFitMode FitMode;

    /// <summary>
    /// Initializes a new image block.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfImageBlock(PdfImage image, double width, double height)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        ValidatePositive(width, "width", "Image block width must be positive.");
        ValidatePositive(height, "height", "Image block height must be positive.");

        Image = image;
        Width = width;
        Height = height;
        FitMode = PdfImageFitMode.Stretch;
    }

    /// <summary>
    /// Sets how the image is placed and clipped inside its layout box.
    /// </summary>
    /// <param name="fitMode">The image fit mode to use.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the fit mode is unsupported.</exception>
    public void SetFitMode(PdfImageFitMode fitMode)
    {
        if (fitMode != PdfImageFitMode.Stretch && fitMode != PdfImageFitMode.PreserveAspectFit && fitMode != PdfImageFitMode.CropFill)
        {
            throw new ArgumentOutOfRangeException("fitMode", "Image fit mode is not supported.");
        }

        FitMode = fitMode;
    }

    /// <summary>
    /// Returns how the image is placed and clipped inside its layout box.
    /// </summary>
    /// <returns>The current image fit mode.</returns>
    public PdfImageFitMode GetFitMode()
    {
        return FitMode;
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);

        PdfLayoutSize size = new PdfLayoutSize(Width, Height);
        PdfBox box = PdfBox.FromCoordinates(0, 0, Width, Height);
        bool fits = PdfBlock.SizeFits(size, context);
        return new PdfLayoutResult(size, box, fits);
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfBlock.ValidateDrawContext(context);

        PdfLayoutResult result = Measure(context.ToLayoutContext());
        if (FitMode == PdfImageFitMode.PreserveAspectFit)
        {
            context.Page.Canvas.DrawImagePreserveAspectFit(Image, context.X, context.Y, Width, Height);
        }
        else if (FitMode == PdfImageFitMode.CropFill)
        {
            context.Page.Canvas.DrawImageCropFill(Image, context.X, context.Y, Width, Height);
        }
        else
        {
            context.Page.Canvas.DrawImage(Image, context.X, context.Y, Width, Height);
        }

        return result;
    }

    /// <summary>
    /// Flows this layout element through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return cursor.DrawItem(this);
    }

    internal static void ValidatePositive(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }
}
