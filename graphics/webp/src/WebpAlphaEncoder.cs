using System;

namespace webp;

internal static class WebpAlphaEncoder
{
    internal static byte[] EncodeUncompressed(byte[] alpha, int width, int height)
    {
        return EncodeUncompressed(alpha, width, height, WebpConstants.AlphaFilterNone);
    }

    internal static byte[] EncodeUncompressed(byte[] alpha, int width, int height, byte filterMethod)
    {
        ArgumentNullException.ThrowIfNull(alpha);
        byte[] filteredAlpha = WebpAlphaFilter.Filter(alpha, width, height, filterMethod);
        byte[] payload = new byte[filteredAlpha.Length + 1];
        payload[0] = CreateHeader(WebpConstants.AlphaCompressionNone, filterMethod);
        CopyBytesAfterHeader(payload, filteredAlpha);
        return payload;
    }

    internal static byte[] EncodeCompressed(byte[] alpha, int width, int height, Func<WebpPixelBuffer, byte[]> losslessEncoder)
    {
        return EncodeCompressed(alpha, width, height, WebpConstants.AlphaFilterNone, losslessEncoder);
    }

    internal static byte[] EncodeCompressed(byte[] alpha, int width, int height, byte filterMethod, Func<WebpPixelBuffer, byte[]> losslessEncoder)
    {
        ArgumentNullException.ThrowIfNull(alpha);
        ArgumentNullException.ThrowIfNull(losslessEncoder);

        byte[] filteredAlpha = WebpAlphaFilter.Filter(alpha, width, height, filterMethod);
        WebpPixelBuffer alphaImage = CreateLosslessAlphaImage(filteredAlpha, width, height);
        byte[] imageStream = losslessEncoder(alphaImage);
        ValidateCompressedAlphaStream(imageStream);

        byte[] payload = new byte[imageStream.Length + 1];
        payload[0] = CreateHeader(WebpConstants.AlphaCompressionLossless, filterMethod);
        CopyBytesAfterHeader(payload, imageStream);
        return payload;
    }

    internal static void CopyBytesAfterHeader(byte[] payload, byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int index = 0;
        while (index < source.Length)
        {
            payload[index + 1] = source[index];
            index = index + 1;
        }
    }

    internal static WebpPixelBuffer CreateLosslessAlphaImage(byte[] filteredAlpha, int width, int height)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 0;
        while (index < filteredAlpha.Length)
        {
            int pixel = index * 4;
            buffer.Rgba[pixel] = 0;
            buffer.Rgba[pixel + 1] = filteredAlpha[index];
            buffer.Rgba[pixel + 2] = 0;
            buffer.Rgba[pixel + 3] = 255;
            index = index + 1;
        }

        return buffer;
    }

    internal static void ValidateCompressedAlphaStream(byte[] imageStream)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        if (imageStream.Length == 0)
        {
            throw new ArgumentException("compressed ALPH lossless stream must not be empty", "losslessEncoder");
        }
    }

    internal static byte CreateHeader(byte compressionMethod, byte filterMethod)
    {
        if (compressionMethod != WebpConstants.AlphaCompressionNone && compressionMethod != WebpConstants.AlphaCompressionLossless)
        {
            throw new ArgumentOutOfRangeException("compressionMethod");
        }

        if (filterMethod > WebpConstants.AlphaFilterGradient)
        {
            throw new ArgumentOutOfRangeException("filterMethod");
        }

        return (byte)(compressionMethod | (filterMethod << WebpConstants.AlphaFilterShift));
    }
}
