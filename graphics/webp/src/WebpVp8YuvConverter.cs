using System;

namespace webp;

internal static class WebpVp8YuvConverter
{
    internal const int YuvFix2 = 6;
    internal const int YuvMask2 = (256 << YuvFix2) - 1;

    internal static WebpPixelBuffer ConvertYuv420ToRgba(byte[] yPlane, byte[] uPlane, byte[] vPlane, int width, int height, int yStride, int uvStride)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        ConvertYuv420ToRgba(yPlane, uPlane, vPlane, width, height, yStride, uvStride, buffer.Rgba, buffer.Stride);
        return buffer;
    }

    internal static void ConvertYuv420ToRgba(byte[] yPlane, byte[] uPlane, byte[] vPlane, int width, int height, int yStride, int uvStride, byte[] rgba, int rgbaStride)
    {
        ArgumentNullException.ThrowIfNull(yPlane);
        ArgumentNullException.ThrowIfNull(uPlane);
        ArgumentNullException.ThrowIfNull(vPlane);
        ArgumentNullException.ThrowIfNull(rgba);
        ValidateDimensions(width, height);
        ValidateYuvPlanes(yPlane, uPlane, vPlane, width, height, yStride, uvStride);
        WebpPixelBufferHelper.ValidateRgbaBuffer(rgba, width, height, rgbaStride);

        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                int y = yPlane[(row * yStride) + column];
                int uvOffset = ((row >> 1) * uvStride) + (column >> 1);
                int u = uPlane[uvOffset];
                int v = vPlane[uvOffset];
                int destinationOffset = (row * rgbaStride) + (column * 4);
                ConvertPixelToRgba(y, u, v, rgba, destinationOffset);
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static void ConvertPixelToRgba(int y, int u, int v, byte[] rgba, int offset)
    {
        ArgumentNullException.ThrowIfNull(rgba);
        ValidateSample(y, "y");
        ValidateSample(u, "u");
        ValidateSample(v, "v");
        if (offset < 0 || offset + 3 >= rgba.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        rgba[offset] = YuvToR(y, v);
        rgba[offset + 1] = YuvToG(y, u, v);
        rgba[offset + 2] = YuvToB(y, u);
        rgba[offset + 3] = 255;
    }

    internal static byte YuvToR(int y, int v)
    {
        ValidateSample(y, "y");
        ValidateSample(v, "v");
        return Clip8(MultHi(y, 19077) + MultHi(v, 26149) - 14234);
    }

    internal static byte YuvToG(int y, int u, int v)
    {
        ValidateSample(y, "y");
        ValidateSample(u, "u");
        ValidateSample(v, "v");
        return Clip8(MultHi(y, 19077) - MultHi(u, 6419) - MultHi(v, 13320) + 8708);
    }

    internal static byte YuvToB(int y, int u)
    {
        ValidateSample(y, "y");
        ValidateSample(u, "u");
        return Clip8(MultHi(y, 19077) + MultHi(u, 33050) - 17685);
    }

    internal static int MultHi(int value, int coefficient)
    {
        return (value * coefficient) >> 8;
    }

    internal static byte Clip8(int value)
    {
        if ((value & ~YuvMask2) == 0)
        {
            return (byte)(value >> YuvFix2);
        }

        if (value < 0)
        {
            return 0;
        }

        return 255;
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidateYuvPlanes(byte[] yPlane, byte[] uPlane, byte[] vPlane, int width, int height, int yStride, int uvStride)
    {
        if (yStride < width)
        {
            throw new ArgumentOutOfRangeException("yStride");
        }

        int uvWidth = (width + 1) >> 1;
        int uvHeight = (height + 1) >> 1;
        if (uvStride < uvWidth)
        {
            throw new ArgumentOutOfRangeException("uvStride");
        }

        int yRequired = ((height - 1) * yStride) + width;
        int uvRequired = ((uvHeight - 1) * uvStride) + uvWidth;
        if (yPlane.Length < yRequired)
        {
            throw new ArgumentException("Y plane is too small for the dimensions and stride", "yPlane");
        }

        if (uPlane.Length < uvRequired)
        {
            throw new ArgumentException("U plane is too small for the dimensions and stride", "uPlane");
        }

        if (vPlane.Length < uvRequired)
        {
            throw new ArgumentException("V plane is too small for the dimensions and stride", "vPlane");
        }
    }

    internal static void ValidateSample(int value, string name)
    {
        if (value < 0 || value > 255)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
