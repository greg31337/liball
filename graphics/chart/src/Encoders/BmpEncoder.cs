namespace chart;

internal class BmpEncoder
{
    internal const int FileHeaderSize = 14;
    internal const int InfoHeaderSize = 40;
    internal const int PixelDataOffset = 54;

    internal static byte[] Encode(RawBitmap bitmap, int bitsPerPixel, bool topDown)
    {
        ValidateEncodeInputs(bitmap, bitsPerPixel);

        int rowStride = CalculateRowStride(bitmap.Width, bitsPerPixel);
        long imageSizeValue = (long)rowStride * bitmap.Height;
        long fileSizeValue = PixelDataOffset + imageSizeValue;
        if (imageSizeValue > int.MaxValue || fileSizeValue > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("bitmap");
        }

        int imageSize = (int)imageSizeValue;
        int fileSize = (int)fileSizeValue;
        byte[] bytes = new byte[fileSize];

        bytes[0] = 66;
        bytes[1] = 77;
        WriteInt32(bytes, 2, fileSize);
        WriteInt16(bytes, 6, 0);
        WriteInt16(bytes, 8, 0);
        WriteInt32(bytes, 10, PixelDataOffset);

        WriteInt32(bytes, 14, InfoHeaderSize);
        WriteInt32(bytes, 18, bitmap.Width);
        if (topDown)
        {
            WriteInt32(bytes, 22, 0 - bitmap.Height);
        }
        else
        {
            WriteInt32(bytes, 22, bitmap.Height);
        }

        WriteInt16(bytes, 26, 1);
        WriteInt16(bytes, 28, bitsPerPixel);
        WriteInt32(bytes, 30, 0);
        WriteInt32(bytes, 34, imageSize);
        WriteInt32(bytes, 38, 0);
        WriteInt32(bytes, 42, 0);
        WriteInt32(bytes, 46, 0);
        WriteInt32(bytes, 50, 0);

        WritePixelData(bitmap, bytes, PixelDataOffset, rowStride, bitsPerPixel, topDown);
        return bytes;
    }

    internal static byte[] EncodeWithOptions(RawBitmap bitmap, ChartBmpOptions options)
    {
        ValidateOptions(bitmap, options);
        int bitsPerPixel = (int)options.BitDepth;
        bool topDown = options.RowOrientation == ChartBmpRowOrientation.TopDown;
        int rowStride = CalculateOptionRowStride(bitmap.Width, bitsPerPixel);
        ChartColor[] palette = new ChartColor[0];
        if (bitsPerPixel == 8)
        {
            palette = CreateBmpPalette(bitmap);
        }

        long pixelOffsetValue = PixelDataOffset + (long)palette.Length * 4;
        long imageSizeValue = (long)rowStride * bitmap.Height;
        long fileSizeValue = pixelOffsetValue + imageSizeValue;
        if (pixelOffsetValue > int.MaxValue || imageSizeValue > int.MaxValue || fileSizeValue > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("bitmap");
        }

        int pixelOffset = (int)pixelOffsetValue;
        int imageSize = (int)imageSizeValue;
        int fileSize = (int)fileSizeValue;
        byte[] bytes = new byte[fileSize];
        bytes[0] = 66;
        bytes[1] = 77;
        WriteInt32(bytes, 2, fileSize);
        WriteInt16(bytes, 6, 0);
        WriteInt16(bytes, 8, 0);
        WriteInt32(bytes, 10, pixelOffset);
        WriteInt32(bytes, 14, InfoHeaderSize);
        WriteInt32(bytes, 18, bitmap.Width);
        if (topDown)
        {
            WriteInt32(bytes, 22, 0 - bitmap.Height);
        }
        else
        {
            WriteInt32(bytes, 22, bitmap.Height);
        }

        WriteInt16(bytes, 26, 1);
        WriteInt16(bytes, 28, bitsPerPixel);
        WriteInt32(bytes, 30, 0);
        WriteInt32(bytes, 34, imageSize);
        WriteInt32(bytes, 38, options.HorizontalPixelsPerMeter);
        WriteInt32(bytes, 42, options.VerticalPixelsPerMeter);
        WriteInt32(bytes, 46, palette.Length);
        WriteInt32(bytes, 50, 0);
        if (palette.Length > 0)
        {
            WritePalette(bytes, PixelDataOffset, palette);
            WriteIndexedPixelData(bitmap, bytes, pixelOffset, rowStride, topDown);
        }
        else
        {
            WritePixelData(bitmap, bytes, pixelOffset, rowStride, bitsPerPixel, topDown);
        }

        return bytes;
    }

    internal static void WriteStreamWithOptions(RawBitmap bitmap, System.IO.Stream stream, ChartBmpOptions options)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("Stream must be writable.");
        }

        byte[] bytes = EncodeWithOptions(bitmap, options);
        stream.Write(bytes, 0, bytes.Length);
    }

    internal static void WriteFileWithOptions(RawBitmap bitmap, string path, ChartBmpOptions options)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        System.IO.File.WriteAllBytes(path, EncodeWithOptions(bitmap, options));
    }

    internal static void WriteFile(RawBitmap bitmap, string path, int bitsPerPixel, bool topDown)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        System.IO.File.WriteAllBytes(path, Encode(bitmap, bitsPerPixel, topDown));
    }

    internal static void WritePixelData(RawBitmap bitmap, byte[] bytes, int offset, int rowStride, int bitsPerPixel, bool topDown)
    {
        int outputY = 0;
        while (outputY < bitmap.Height)
        {
            int sourceY = outputY;
            if (!topDown)
            {
                sourceY = bitmap.Height - outputY - 1;
            }

            int rowOffset = offset + outputY * rowStride;
            int x = 0;
            while (x < bitmap.Width)
            {
                ChartColor color = bitmap.GetPixel(x, sourceY);
                int pixelOffset = rowOffset + x * (bitsPerPixel / 8);
                bytes[pixelOffset] = color.B;
                bytes[pixelOffset + 1] = color.G;
                bytes[pixelOffset + 2] = color.R;
                if (bitsPerPixel == 32)
                {
                    bytes[pixelOffset + 3] = color.A;
                }

                x = x + 1;
            }

            outputY = outputY + 1;
        }
    }

    internal static int CalculateRowStride(int width, int bitsPerPixel)
    {
        if (width <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (bitsPerPixel == 24)
        {
            long rowStride = ((long)width * 3 + 3) / 4 * 4;
            if (rowStride > int.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("width");
            }

            return (int)rowStride;
        }

        if (bitsPerPixel == 32)
        {
            long rowStride = (long)width * 4;
            if (rowStride > int.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("width");
            }

            return (int)rowStride;
        }

        throw new System.ArgumentException("BMP encoder supports only 24-bit and 32-bit output.");
    }

    internal static int CalculateOptionRowStride(int width, int bitsPerPixel)
    {
        if (width <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }
        if (bitsPerPixel == 8)
        {
            long rowStride = ((long)width + 3) / 4 * 4;
            if (rowStride > int.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("width");
            }

            return (int)rowStride;
        }

        return CalculateRowStride(width, bitsPerPixel);
    }

    internal static ChartColor[] CreateBmpPalette(RawBitmap bitmap)
    {
        if (bitmap.PixelFormat == ChartPixelFormat.Indexed8)
        {
            return bitmap.GetPalette();
        }

        ChartColor[] palette = new ChartColor[256];
        int index = 0;
        while (index < palette.Length)
        {
            byte value = (byte)index;
            palette[index] = new ChartColor(value, value, value, 255);
            index = index + 1;
        }

        return palette;
    }

    internal static void WritePalette(byte[] bytes, int offset, ChartColor[] palette)
    {
        int index = 0;
        while (index < palette.Length)
        {
            int entryOffset = offset + index * 4;
            bytes[entryOffset] = palette[index].B;
            bytes[entryOffset + 1] = palette[index].G;
            bytes[entryOffset + 2] = palette[index].R;
            bytes[entryOffset + 3] = 0;
            index = index + 1;
        }
    }

    internal static void WriteIndexedPixelData(RawBitmap bitmap, byte[] bytes, int offset, int rowStride, bool topDown)
    {
        int outputY = 0;
        while (outputY < bitmap.Height)
        {
            int sourceY = outputY;
            if (!topDown)
            {
                sourceY = bitmap.Height - outputY - 1;
            }

            int rowOffset = offset + outputY * rowStride;
            int x = 0;
            while (x < bitmap.Width)
            {
                if (bitmap.PixelFormat == ChartPixelFormat.Indexed8)
                {
                    bytes[rowOffset + x] = (byte)bitmap.GetPaletteIndex(x, sourceY);
                }
                else
                {
                    bytes[rowOffset + x] = RawBitmap.Luminance(bitmap.GetPixel(x, sourceY));
                }

                x = x + 1;
            }

            outputY = outputY + 1;
        }
    }

    internal static void ValidateOptions(RawBitmap bitmap, ChartBmpOptions options)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.BitDepth != ChartBmpBitDepth.Indexed8 && options.BitDepth != ChartBmpBitDepth.Rgb24 && options.BitDepth != ChartBmpBitDepth.Rgba32)
        {
            throw new System.ArgumentOutOfRangeException("BitDepth");
        }
        if (options.RowOrientation != ChartBmpRowOrientation.TopDown && options.RowOrientation != ChartBmpRowOrientation.BottomUp)
        {
            throw new System.ArgumentOutOfRangeException("RowOrientation");
        }
        if (options.HorizontalPixelsPerMeter < 0)
        {
            throw new System.ArgumentOutOfRangeException("HorizontalPixelsPerMeter");
        }
        if (options.VerticalPixelsPerMeter < 0)
        {
            throw new System.ArgumentOutOfRangeException("VerticalPixelsPerMeter");
        }

        CalculateOptionRowStride(bitmap.Width, (int)options.BitDepth);
        if (bitmap.Height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("bitmap");
        }
    }

    internal static void ValidateEncodeInputs(RawBitmap bitmap, int bitsPerPixel)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        if (bitmap.Height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("bitmap");
        }

        CalculateRowStride(bitmap.Width, bitsPerPixel);
    }

    internal static void WriteInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 255);
        bytes[offset + 1] = (byte)((value >> 8) & 255);
    }

    internal static void WriteInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 255);
        bytes[offset + 1] = (byte)((value >> 8) & 255);
        bytes[offset + 2] = (byte)((value >> 16) & 255);
        bytes[offset + 3] = (byte)((value >> 24) & 255);
    }

    internal static int ReadInt16(byte[] bytes, int offset)
    {
        return bytes[offset] | (bytes[offset + 1] << 8);
    }

    internal static int ReadInt32(byte[] bytes, int offset)
    {
        return bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
    }
}
