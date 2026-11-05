using System;

namespace pdf;

/// <summary>
/// Represents an image resource embedded in a PDF document.
/// </summary>
public class PdfImage : IPdfImage
{
    internal static readonly byte[] InternalImageData = new byte[0];
    /// <summary>
    /// Stores the name used by this object.
    /// </summary>
    internal string Name;
    /// <summary>
    /// Stores the numeric width used by this object.
    /// </summary>
    internal int Width;
    /// <summary>
    /// Stores the numeric height used by this object.
    /// </summary>
    internal int Height;
    /// <summary>
    /// Stores the source representation used to create this image.
    /// </summary>
    internal PdfImageFormat Format;
    /// <summary>
    /// Stores the device color space used by this image.
    /// </summary>
    internal PdfImageColorSpace ColorSpace;
    /// <summary>
    /// Exposes the rgb data currently stored by this object.
    /// </summary>
    internal byte[] RgbData;
    /// <summary>
    /// Stores encoded image data when the image is passed through without pixel decoding.
    /// </summary>
    internal byte[] EncodedData;
    /// <summary>
    /// Indicates whether this image contains encoded JPEG data.
    /// </summary>
    internal bool IsJpeg;
    /// <summary>
    /// Indicates whether this image contains decoded PNG data.
    /// </summary>
    internal bool IsPng;
    /// <summary>
    /// Stores the number of bits used by each image component.
    /// </summary>
    internal int BitsPerComponent;
    /// <summary>
    /// Stores the color space used by encoded JPEG data.
    /// </summary>
    internal PdfJpegColorSpace JpegColorSpace;
    /// <summary>
    /// Stores the color space produced by PNG decoding.
    /// </summary>
    internal PdfPngColorSpace PngColorSpace;
    /// <summary>
    /// Stores deterministic Flate-compressed image samples.
    /// </summary>
    internal byte[] CompressedData;
    /// <summary>
    /// Indicates whether this image has a separate alpha soft mask.
    /// </summary>
    internal bool HasSoftMask;
    /// <summary>
    /// Stores deterministic Flate-compressed alpha samples.
    /// </summary>
    internal byte[] SoftMaskData;
    /// <summary>
    /// Stores the alpha soft-mask sample bit depth.
    /// </summary>
    internal int SoftMaskBitsPerComponent;
    /// <summary>
    /// Indicates whether physical resolution metadata is available.
    /// </summary>
    internal bool HasDpiMetadata;
    /// <summary>
    /// Stores horizontal image resolution in dots per inch.
    /// </summary>
    internal double HorizontalDpi;
    /// <summary>
    /// Stores vertical image resolution in dots per inch.
    /// </summary>
    internal double VerticalDpi;
    /// <summary>
    /// Indicates whether CMYK sample values require inverted decoding.
    /// </summary>
    internal bool HasInvertedCmykSamples;
    /// <summary>
    /// Stores the alternate text used by this object.
    /// </summary>
    internal string AlternateText;
    /// <summary>
    /// Indicates whether this object is decorative.
    /// </summary>
    internal bool IsDecorative;

    /// <summary>
    /// Initializes a new image.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="rgbData">Packed RGB bytes in row-major order, with three bytes per pixel.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is empty or the RGB data length is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an image dimension is not positive.</exception>
    public PdfImage(string name, int width, int height, byte[] rgbData)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Image resource name cannot be empty.", "name");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Image width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Image height must be positive.");
        }

        if (rgbData == null)
        {
            throw new ArgumentNullException("rgbData");
        }

        long pixelCount = (long)width * height;
        if (rgbData != InternalImageData && (pixelCount > int.MaxValue / 3 || rgbData.Length != pixelCount * 3))
        {
            throw new ArgumentException("RGB image data must contain exactly width * height * 3 bytes.", "rgbData");
        }

        Name = name;
        Width = width;
        Height = height;
        Format = PdfImageFormat.Rgb;
        ColorSpace = PdfImageColorSpace.Rgb;
        RgbData = new byte[rgbData.Length];
        rgbData.CopyTo(RgbData, 0);
        EncodedData = new byte[0];
        IsJpeg = false;
        IsPng = false;
        BitsPerComponent = 8;
        JpegColorSpace = PdfJpegColorSpace.Rgb;
        PngColorSpace = PdfPngColorSpace.Rgb;
        CompressedData = new byte[0];
        HasSoftMask = false;
        SoftMaskData = new byte[0];
        SoftMaskBitsPerComponent = 0;
        HasDpiMetadata = false;
        HorizontalDpi = 0;
        VerticalDpi = 0;
        HasInvertedCmykSamples = false;
        AlternateText = string.Empty;
        IsDecorative = false;
    }

    internal static PdfImage CreateInternal(string name, int width, int height)
    {
        return new PdfImage(name, width, height, InternalImageData);
    }

    /// <summary>
    /// Returns the source representation used to create this image.
    /// </summary>
    /// <returns>The image source format.</returns>
    public PdfImageFormat GetFormat()
    {
        return Format;
    }

    /// <summary>
    /// Returns the device color space used by this image.
    /// </summary>
    /// <returns>The image device color space.</returns>
    public PdfImageColorSpace GetColorSpace()
    {
        return ColorSpace;
    }

    /// <summary>
    /// Returns the image width in pixels.
    /// </summary>
    /// <returns>The positive pixel width.</returns>
    public int GetPixelWidth()
    {
        return Width;
    }

    /// <summary>
    /// Returns the image height in pixels.
    /// </summary>
    /// <returns>The positive pixel height.</returns>
    public int GetPixelHeight()
    {
        return Height;
    }

    /// <summary>
    /// Indicates whether the image has alpha or transparency samples.
    /// </summary>
    /// <returns><c>true</c> when the image uses a soft mask; otherwise <c>false</c>.</returns>
    public bool HasAlpha()
    {
        return HasSoftMask;
    }

    /// <summary>
    /// Indicates whether physical resolution metadata is available.
    /// </summary>
    /// <returns><c>true</c> when horizontal and vertical DPI values are available; otherwise <c>false</c>.</returns>
    public bool HasDpi()
    {
        return HasDpiMetadata;
    }

    /// <summary>
    /// Returns horizontal resolution in dots per inch, or zero when unavailable.
    /// </summary>
    /// <returns>The horizontal dots per inch.</returns>
    public double GetHorizontalDpi()
    {
        return HorizontalDpi;
    }

    /// <summary>
    /// Returns vertical resolution in dots per inch, or zero when unavailable.
    /// </summary>
    /// <returns>The vertical dots per inch.</returns>
    public double GetVerticalDpi()
    {
        return VerticalDpi;
    }

    /// <summary>
    /// Sets physical resolution metadata for this image.
    /// </summary>
    /// <param name="horizontalDpi">The positive horizontal dots per inch.</param>
    /// <param name="verticalDpi">The positive vertical dots per inch.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a DPI value is not positive and finite.</exception>
    public void SetDpi(double horizontalDpi, double verticalDpi)
    {
        if (horizontalDpi <= 0 || double.IsNaN(horizontalDpi) || double.IsInfinity(horizontalDpi))
        {
            throw new ArgumentOutOfRangeException("horizontalDpi", "Horizontal DPI must be positive and finite.");
        }

        if (verticalDpi <= 0 || double.IsNaN(verticalDpi) || double.IsInfinity(verticalDpi))
        {
            throw new ArgumentOutOfRangeException("verticalDpi", "Vertical DPI must be positive and finite.");
        }

        HasDpiMetadata = true;
        HorizontalDpi = horizontalDpi;
        VerticalDpi = verticalDpi;
    }

    /// <summary>
    /// Sets the alternate text used for subsequent PDF generation.
    /// </summary>
    /// <param name="alternateText">The alternate text to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public void SetAlternateText(string alternateText)
    {
        if (alternateText == null)
        {
            throw new ArgumentNullException("alternateText");
        }

        if (alternateText.Length == 0)
        {
            throw new ArgumentException("Alternate text cannot be empty.", "alternateText");
        }

        AlternateText = alternateText;
        IsDecorative = false;
    }

    /// <summary>
    /// Marks this image as decorative.
    /// </summary>
    public void MarkDecorative()
    {
        AlternateText = string.Empty;
        IsDecorative = true;
    }
}
