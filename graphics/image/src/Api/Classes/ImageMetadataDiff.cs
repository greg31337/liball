namespace image;

/// <summary>Compares metadata documents structurally.</summary>
public class ImageMetadataDiff : IImageMetadataDiff
{
    /// <summary>Creates a stateless metadata comparison service.</summary>
    public ImageMetadataDiff()
    {
    }

    /// <summary>Compares two metadata documents.</summary>
    /// <param name="left">The left document, or null.</param>
    /// <param name="right">The right document, or null.</param>
    /// <returns>A result describing equality and individual differences.</returns>
    public ImageMetadataDiffResult Compare(ImageMetadataDocument? left, ImageMetadataDocument? right)
    {
        return ImageMetadataDiffCore.Compare(left, right);
    }
}
