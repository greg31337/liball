using System;

namespace webp;

internal class WebpVp8Yuv420Buffer
{
    internal int Width;
    internal int Height;
    internal int YStride;
    internal int UvStride;
    internal byte[] Y;
    internal byte[] U;
    internal byte[] V;

    internal WebpVp8Yuv420Buffer()
    {
        Width = 0;
        Height = 0;
        YStride = 0;
        UvStride = 0;
        Y = Array.Empty<byte>();
        U = Array.Empty<byte>();
        V = Array.Empty<byte>();
    }
}

internal static class WebpVp8RgbaToYuvConverter
{
    internal const int YuvFix = 16;
    internal const int YuvHalf = 1 << (YuvFix - 1);

    internal static WebpVp8Yuv420Buffer ConvertRgbaToYuv420(WebpPixelBuffer buffer)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        return ConvertRgbaToYuv420(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride);
    }

    internal static WebpVp8Yuv420Buffer ConvertRgbaToYuv420(byte[] rgba, int width, int height, int rgbaStride)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(rgba, width, height, rgbaStride);

        WebpVp8Yuv420Buffer yuv = new WebpVp8Yuv420Buffer();
        yuv.Width = width;
        yuv.Height = height;
        yuv.YStride = width;
        yuv.UvStride = (width + 1) >> 1;
        int uvHeight = (height + 1) >> 1;
        yuv.Y = new byte[yuv.YStride * height];
        yuv.U = new byte[yuv.UvStride * uvHeight];
        yuv.V = new byte[yuv.UvStride * uvHeight];
        FillYPlane(rgba, width, height, rgbaStride, yuv);
        FillChromaPlanes(rgba, width, height, rgbaStride, yuv);
        return yuv;
    }

    internal static byte RgbToY(int red, int green, int blue)
    {
        ValidateRgb(red, "red");
        ValidateRgb(green, "green");
        ValidateRgb(blue, "blue");
        int luma = (16839 * red) + (33059 * green) + (6420 * blue);
        return (byte)((luma + YuvHalf + (16 << YuvFix)) >> YuvFix);
    }

    internal static byte RgbToU(int red, int green, int blue)
    {
        ValidateRgb(red, "red");
        ValidateRgb(green, "green");
        ValidateRgb(blue, "blue");
        int u = (-9719 * red) - (19081 * green) + (28800 * blue);
        return ClipUv(u, YuvHalf);
    }

    internal static byte RgbToV(int red, int green, int blue)
    {
        ValidateRgb(red, "red");
        ValidateRgb(green, "green");
        ValidateRgb(blue, "blue");
        int v = (28800 * red) - (24116 * green) - (4684 * blue);
        return ClipUv(v, YuvHalf);
    }

    internal static void FillYPlane(byte[] rgba, int width, int height, int rgbaStride, WebpVp8Yuv420Buffer yuv)
    {
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                int rgbaOffset = (row * rgbaStride) + (column * 4);
                yuv.Y[(row * yuv.YStride) + column] = RgbToY(rgba[rgbaOffset], rgba[rgbaOffset + 1], rgba[rgbaOffset + 2]);
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static void FillChromaPlanes(byte[] rgba, int width, int height, int rgbaStride, WebpVp8Yuv420Buffer yuv)
    {
        int uvHeight = (height + 1) >> 1;
        int uvRow = 0;
        while (uvRow < uvHeight)
        {
            int uvColumn = 0;
            while (uvColumn < yuv.UvStride)
            {
                int redSum = 0;
                int greenSum = 0;
                int blueSum = 0;
                int sampleCount = 0;
                int rowOffset = 0;
                while (rowOffset < 2)
                {
                    int sourceRow = (uvRow * 2) + rowOffset;
                    if (sourceRow < height)
                    {
                        int columnOffset = 0;
                        while (columnOffset < 2)
                        {
                            int sourceColumn = (uvColumn * 2) + columnOffset;
                            if (sourceColumn < width)
                            {
                                int rgbaOffset = (sourceRow * rgbaStride) + (sourceColumn * 4);
                                redSum = redSum + rgba[rgbaOffset];
                                greenSum = greenSum + rgba[rgbaOffset + 1];
                                blueSum = blueSum + rgba[rgbaOffset + 2];
                                sampleCount = sampleCount + 1;
                            }

                            columnOffset = columnOffset + 1;
                        }
                    }

                    rowOffset = rowOffset + 1;
                }

                int red = (redSum + (sampleCount >> 1)) / sampleCount;
                int green = (greenSum + (sampleCount >> 1)) / sampleCount;
                int blue = (blueSum + (sampleCount >> 1)) / sampleCount;
                int uvOffset = (uvRow * yuv.UvStride) + uvColumn;
                yuv.U[uvOffset] = RgbToU(red, green, blue);
                yuv.V[uvOffset] = RgbToV(red, green, blue);
                uvColumn = uvColumn + 1;
            }

            uvRow = uvRow + 1;
        }
    }

    internal static byte ClipUv(int value, int rounding)
    {
        int uv = (value + rounding + (128 << YuvFix)) >> YuvFix;
        if ((uv & ~255) == 0)
        {
            return (byte)uv;
        }

        if (uv < 0)
        {
            return 0;
        }

        return 255;
    }

    internal static void ValidateRgb(int value, string name)
    {
        if (value < 0 || value > 255)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
