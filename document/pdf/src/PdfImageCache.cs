using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

internal class PdfImageCache
{
    internal List<PdfImage> _images;
    internal IReadOnlyList<PdfImage> Images;

    internal PdfImageCache()
    {
        _images = new List<PdfImage>();
        Images = _images;
    }

    internal PdfImage AddRgbImage(int width, int height, byte[] rgbData)
    {
        ValidateRgbImage(width, height, rgbData);

        foreach (PdfImage image in _images)
        {
            if (MatchesRgbImage(image, width, height, rgbData))
            {
                return image;
            }
        }

        PdfImage newImage = new PdfImage("Im" + (_images.Count + 1).ToString(CultureInfo.InvariantCulture), width, height, rgbData);
        _images.Add(newImage);
        return newImage;
    }

    internal PdfImage AddGrayscaleImage(int width, int height, byte[] grayscaleData)
    {
        ValidateGrayscaleImage(width, height, grayscaleData);
        foreach (PdfImage image in _images)
        {
            if (MatchesRawImage(image, PdfImageFormat.Grayscale, width, height, grayscaleData))
            {
                return image;
            }
        }

        PdfImage newImage = PdfImage.CreateInternal("Im" + (_images.Count + 1).ToString(CultureInfo.InvariantCulture), width, height);
        newImage.RgbData = CopyBytes(grayscaleData);
        newImage.Format = PdfImageFormat.Grayscale;
        newImage.ColorSpace = PdfImageColorSpace.Grayscale;
        _images.Add(newImage);
        return newImage;
    }

    internal PdfImage AddRgbaImage(int width, int height, byte[] rgbaData)
    {
        ValidateRgbaImage(width, height, rgbaData);
        foreach (PdfImage image in _images)
        {
            if (MatchesRawEncodedImage(image, PdfImageFormat.Rgba, width, height, rgbaData))
            {
                return image;
            }
        }

        int pixelCount = width * height;
        byte[] rgbData = new byte[pixelCount * 3];
        byte[] alphaData = new byte[pixelCount];
        for (int pixel = 0; pixel < pixelCount; pixel++)
        {
            rgbData[pixel * 3] = rgbaData[pixel * 4];
            rgbData[pixel * 3 + 1] = rgbaData[pixel * 4 + 1];
            rgbData[pixel * 3 + 2] = rgbaData[pixel * 4 + 2];
            alphaData[pixel] = rgbaData[pixel * 4 + 3];
        }

        PdfImage newImage = new PdfImage("Im" + (_images.Count + 1).ToString(CultureInfo.InvariantCulture), width, height, rgbData);
        newImage.Format = PdfImageFormat.Rgba;
        newImage.ColorSpace = PdfImageColorSpace.Rgb;
        newImage.EncodedData = CopyBytes(rgbaData);
        newImage.HasSoftMask = true;
        newImage.SoftMaskData = alphaData;
        newImage.SoftMaskBitsPerComponent = 8;
        _images.Add(newImage);
        return newImage;
    }

    internal PdfImage AddJpegImage(PdfJpegData jpegData)
    {
        if (jpegData == null)
        {
            throw new ArgumentNullException("jpegData");
        }

        foreach (PdfImage image in _images)
        {
            if (MatchesJpegImage(image, jpegData.EncodedData))
            {
                return image;
            }
        }

        byte[] copiedData = CopyBytes(jpegData.EncodedData);
        PdfImage newImage = PdfImage.CreateInternal("Im" + (_images.Count + 1).ToString(CultureInfo.InvariantCulture), jpegData.Width, jpegData.Height);
        newImage.EncodedData = copiedData;
        newImage.IsJpeg = true;
        newImage.Format = PdfImageFormat.Jpeg;
        if (jpegData.ColorSpace == PdfJpegColorSpace.Grayscale)
        {
            newImage.ColorSpace = PdfImageColorSpace.Grayscale;
        }
        else if (jpegData.ColorSpace == PdfJpegColorSpace.Cmyk)
        {
            newImage.ColorSpace = PdfImageColorSpace.Cmyk;
        }
        else
        {
            newImage.ColorSpace = PdfImageColorSpace.Rgb;
        }
        newImage.BitsPerComponent = jpegData.BitsPerComponent;
        newImage.JpegColorSpace = jpegData.ColorSpace;
        newImage.HasDpiMetadata = jpegData.HasDpi;
        newImage.HorizontalDpi = jpegData.HorizontalDpi;
        newImage.VerticalDpi = jpegData.VerticalDpi;
        newImage.HasInvertedCmykSamples = jpegData.HasInvertedCmykSamples;
        _images.Add(newImage);
        return newImage;
    }

    internal PdfImage AddPngImage(PdfPngData pngData)
    {
        if (pngData == null)
        {
            throw new ArgumentNullException("pngData");
        }

        foreach (PdfImage image in _images)
        {
            if (MatchesPngImage(image, pngData.EncodedData))
            {
                return image;
            }
        }

        PdfImage newImage = PdfImage.CreateInternal("Im" + (_images.Count + 1).ToString(CultureInfo.InvariantCulture), pngData.Width, pngData.Height);
        newImage.EncodedData = CopyBytes(pngData.EncodedData);
        newImage.IsPng = true;
        newImage.Format = PdfImageFormat.Png;
        newImage.ColorSpace = pngData.ColorSpace == PdfPngColorSpace.Grayscale ? PdfImageColorSpace.Grayscale : PdfImageColorSpace.Rgb;
        newImage.BitsPerComponent = pngData.OutputBitsPerComponent;
        newImage.PngColorSpace = pngData.ColorSpace;
        newImage.CompressedData = CopyBytes(pngData.CompressedPixelData);
        newImage.HasSoftMask = pngData.HasAlpha;
        newImage.SoftMaskData = CopyBytes(pngData.CompressedAlphaData);
        newImage.SoftMaskBitsPerComponent = pngData.AlphaBitsPerComponent;
        newImage.HasDpiMetadata = pngData.HasDpi;
        newImage.HorizontalDpi = pngData.HorizontalDpi;
        newImage.VerticalDpi = pngData.VerticalDpi;
        _images.Add(newImage);
        return newImage;
    }

    internal static void ValidateRgbImage(int width, int height, byte[] rgbData)
    {
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
        if (pixelCount > int.MaxValue / 3 || rgbData.Length != pixelCount * 3)
        {
            throw new ArgumentException("RGB image data must contain exactly width * height * 3 bytes.", "rgbData");
        }
    }

    internal static void ValidateGrayscaleImage(int width, int height, byte[] grayscaleData)
    {
        ValidateDimensions(width, height);
        if (grayscaleData == null)
        {
            throw new ArgumentNullException("grayscaleData");
        }

        long pixelCount = (long)width * height;
        if (grayscaleData.Length != pixelCount)
        {
            throw new ArgumentException("Grayscale image data must contain exactly width * height bytes.", "grayscaleData");
        }
    }

    internal static void ValidateRgbaImage(int width, int height, byte[] rgbaData)
    {
        ValidateDimensions(width, height);
        if (rgbaData == null)
        {
            throw new ArgumentNullException("rgbaData");
        }

        long pixelCount = (long)width * height;
        if (pixelCount > int.MaxValue / 4 || rgbaData.Length != pixelCount * 4)
        {
            throw new ArgumentException("RGBA image data must contain exactly width * height * 4 bytes.", "rgbaData");
        }
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Image width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Image height must be positive.");
        }
    }

    internal static bool MatchesRgbImage(PdfImage image, int width, int height, byte[] rgbData)
    {
        if (image.Format != PdfImageFormat.Rgb)
        {
            return false;
        }

        if (image.Width != width)
        {
            return false;
        }

        if (image.Height != height)
        {
            return false;
        }

        if (image.RgbData.Length != rgbData.Length)
        {
            return false;
        }

        for (int index = 0; index < rgbData.Length; index++)
        {
            if (image.RgbData[index] != rgbData[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static bool MatchesRawImage(PdfImage image, PdfImageFormat format, int width, int height, byte[] data)
    {
        if (image.Format != format || image.Width != width || image.Height != height || image.RgbData.Length != data.Length)
        {
            return false;
        }

        for (int index = 0; index < data.Length; index++)
        {
            if (image.RgbData[index] != data[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static bool MatchesRawEncodedImage(PdfImage image, PdfImageFormat format, int width, int height, byte[] data)
    {
        if (image.Format != format || image.Width != width || image.Height != height || image.EncodedData.Length != data.Length)
        {
            return false;
        }

        for (int index = 0; index < data.Length; index++)
        {
            if (image.EncodedData[index] != data[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static bool MatchesJpegImage(PdfImage image, byte[] jpegData)
    {
        if (!image.IsJpeg || image.EncodedData.Length != jpegData.Length)
        {
            return false;
        }

        for (int index = 0; index < jpegData.Length; index++)
        {
            if (image.EncodedData[index] != jpegData[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static bool MatchesPngImage(PdfImage image, byte[] pngData)
    {
        if (!image.IsPng || image.EncodedData.Length != pngData.Length)
        {
            return false;
        }

        for (int index = 0; index < pngData.Length; index++)
        {
            if (image.EncodedData[index] != pngData[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static byte[] CopyBytes(byte[] value)
    {
        byte[] copied = new byte[value.Length];
        Array.Copy(value, copied, value.Length);
        return copied;
    }
}
