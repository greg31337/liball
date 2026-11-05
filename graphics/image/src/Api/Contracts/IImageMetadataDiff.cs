namespace image;

/// <summary>Defines structural comparison operations for metadata documents.</summary>
public interface IImageMetadataDiff
{
    /// <summary>Compares two metadata documents.</summary>
    /// <param name="left">The left document, or null.</param>
    /// <param name="right">The right document, or null.</param>
    /// <returns>A result describing equality and individual differences.</returns>
    ImageMetadataDiffResult Compare(ImageMetadataDocument? left, ImageMetadataDocument? right);
}
