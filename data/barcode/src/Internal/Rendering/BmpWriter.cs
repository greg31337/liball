namespace barcode;

internal class BmpColor
{
    internal int Red;
    internal int Green;
    internal int Blue;
    internal int Alpha;
}

internal class BmpWriteResult
{
    internal byte[] Bytes = new byte[0];
    internal int BitsPerPixel;
    internal int Stride;
    internal int PixelOffset;
    internal int PaletteEntries;
}

internal static class BmpWriter
{
    internal const int IndexedBitsPerPixel = 1;
    internal const int TruecolorBitsPerPixel = 24;
    internal const int AlphaBitsPerPixel = 32;
    internal const int BitmapInfoHeaderSize = 40;
    internal const int BitmapV4HeaderSize = 108;
    internal const int BitmapFileHeaderSize = 14;
    internal const int Uncompressed = 0;
    internal const int BitFields = 3;
    internal const uint RedMask = 0x00FF0000u;
    internal const uint GreenMask = 0x0000FF00u;
    internal const uint BlueMask = 0x000000FFu;
    internal const uint AlphaMask = 0xFF000000u;
    internal const uint SrgbColorSpace = 0x73524742u;

    internal static BmpWriteResult Write(
        RenderPlan? plan,
        BarcodeRenderOptions? options)
    {
        if (plan == null)
        {
            throw new BarcodeRenderingException(
                "A render plan is required to write a BMP image.");
        }

        ValidatePlan(plan);
        BarcodeRenderOptions settings = options == null
            ? new BarcodeRenderOptions()
            : options;
        BmpColor background = ResolveBackground(settings);
        BmpColor foreground = ResolveForeground(settings, background);
        int bitsPerPixel = ResolveBitsPerPixel(foreground, background);
        int paletteEntries = bitsPerPixel == IndexedBitsPerPixel ? 2 : 0;
        int dibHeaderSize = bitsPerPixel == AlphaBitsPerPixel
            ? BitmapV4HeaderSize
            : BitmapInfoHeaderSize;
        int pixelOffset = AddDimensions(
            BitmapFileHeaderSize + dibHeaderSize,
            paletteEntries * 4);
        int stride = ResolveStride(plan.Size.WidthPixels, bitsPerPixel);
        int imageSize = MultiplyDimensions(
            stride,
            plan.Size.HeightPixels);
        int fileSize = AddDimensions(pixelOffset, imageSize);
        byte[] bytes = new byte[fileSize];
        WriteHeaders(
            bytes,
            plan,
            bitsPerPixel,
            stride,
            imageSize,
            fileSize,
            pixelOffset,
            paletteEntries);
        if (paletteEntries != 0)
        {
            WritePalette(bytes, background, foreground);
        }

        FillBackground(
            bytes,
            pixelOffset,
            plan.Size.WidthPixels,
            plan.Size.HeightPixels,
            stride,
            bitsPerPixel,
            background);
        RenderRectangleSet rectangles = RectangleCoalescer.Coalesce(plan);
        FillRectangles(
            bytes,
            pixelOffset,
            plan.Size.WidthPixels,
            plan.Size.HeightPixels,
            stride,
            bitsPerPixel,
            foreground,
            rectangles.PixelRectangles);
        HumanReadableTextRenderResult text =
            HumanReadableTextRenderer.Render(plan);
        FillRectangles(
            bytes,
            pixelOffset,
            plan.Size.WidthPixels,
            plan.Size.HeightPixels,
            stride,
            bitsPerPixel,
            foreground,
            text.PixelRectangles);

        BmpWriteResult result = new BmpWriteResult();
        result.Bytes = bytes;
        result.BitsPerPixel = bitsPerPixel;
        result.Stride = stride;
        result.PixelOffset = pixelOffset;
        result.PaletteEntries = paletteEntries;
        return result;
    }

    static BmpColor ResolveBackground(BarcodeRenderOptions options)
    {
        uint rgba = options.BackgroundRgba == 0u
            ? RasterCanvas.DefaultBackgroundRgba
            : options.BackgroundRgba;
        BmpColor result = ReadColor(rgba);
        if (options.TransparentBackground)
        {
            result.Alpha = 0;
        }

        return result;
    }

    static BmpColor ResolveForeground(
        BarcodeRenderOptions options,
        BmpColor background)
    {
        uint rgba = options.ForegroundRgba == 0u
            ? RasterCanvas.DefaultForegroundRgba
            : options.ForegroundRgba;
        BmpColor foreground = ReadColor(rgba);
        return CompositeColors(foreground, background);
    }

    static BmpColor CompositeColors(
        BmpColor foreground,
        BmpColor background)
    {
        int inverseForegroundAlpha = 255 - foreground.Alpha;
        int alphaNumerator = (foreground.Alpha * 255)
            + (background.Alpha * inverseForegroundAlpha);
        int outputAlpha = (alphaNumerator + 127) / 255;
        BmpColor result = new BmpColor();
        result.Alpha = outputAlpha;
        if (outputAlpha == 0)
        {
            return result;
        }

        result.Red = CompositeColorChannel(
            foreground.Red,
            foreground.Alpha,
            background.Red,
            background.Alpha,
            inverseForegroundAlpha,
            alphaNumerator);
        result.Green = CompositeColorChannel(
            foreground.Green,
            foreground.Alpha,
            background.Green,
            background.Alpha,
            inverseForegroundAlpha,
            alphaNumerator);
        result.Blue = CompositeColorChannel(
            foreground.Blue,
            foreground.Alpha,
            background.Blue,
            background.Alpha,
            inverseForegroundAlpha,
            alphaNumerator);
        return result;
    }

    static int CompositeColorChannel(
        int foreground,
        int foregroundAlpha,
        int background,
        int backgroundAlpha,
        int inverseForegroundAlpha,
        int denominator)
    {
        int numerator = (foreground * foregroundAlpha * 255)
            + (background
                * backgroundAlpha
                * inverseForegroundAlpha);
        return (numerator + (denominator / 2)) / denominator;
    }

    static BmpColor ReadColor(uint rgba)
    {
        BmpColor result = new BmpColor();
        result.Red = (int)((rgba >> 24) & 255u);
        result.Green = (int)((rgba >> 16) & 255u);
        result.Blue = (int)((rgba >> 8) & 255u);
        result.Alpha = (int)(rgba & 255u);
        return result;
    }

    static int ResolveBitsPerPixel(
        BmpColor foreground,
        BmpColor background)
    {
        if (foreground.Alpha != 255 || background.Alpha != 255)
        {
            return AlphaBitsPerPixel;
        }

        if (IsDefaultForeground(foreground)
            && IsDefaultBackground(background))
        {
            return IndexedBitsPerPixel;
        }

        return TruecolorBitsPerPixel;
    }

    static bool IsDefaultForeground(BmpColor color)
    {
        return color.Red == 0
            && color.Green == 0
            && color.Blue == 0
            && color.Alpha == 255;
    }

    static bool IsDefaultBackground(BmpColor color)
    {
        return color.Red == 255
            && color.Green == 255
            && color.Blue == 255
            && color.Alpha == 255;
    }

    static int ResolveStride(int width, int bitsPerPixel)
    {
        long rowBits = (long)width * (long)bitsPerPixel;
        long stride = ((rowBits + 31L) / 32L) * 4L;
        if (stride <= 0L || stride > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The BMP row stride exceeds the supported range.");
        }

        return (int)stride;
    }

    static void WriteHeaders(
        byte[] bytes,
        RenderPlan plan,
        int bitsPerPixel,
        int stride,
        int imageSize,
        int fileSize,
        int pixelOffset,
        int paletteEntries)
    {
        bytes[0] = 66;
        bytes[1] = 77;
        WriteLittleEndianInteger(bytes, 2, fileSize);
        WriteLittleEndianInteger(bytes, 10, pixelOffset);
        int dibHeaderSize = bitsPerPixel == AlphaBitsPerPixel
            ? BitmapV4HeaderSize
            : BitmapInfoHeaderSize;
        WriteLittleEndianInteger(bytes, 14, dibHeaderSize);
        WriteLittleEndianInteger(bytes, 18, plan.Size.WidthPixels);
        WriteLittleEndianInteger(bytes, 22, plan.Size.HeightPixels);
        WriteLittleEndianShort(bytes, 26, 1);
        WriteLittleEndianShort(bytes, 28, bitsPerPixel);
        WriteLittleEndianInteger(
            bytes,
            30,
            bitsPerPixel == AlphaBitsPerPixel
                ? BitFields
                : Uncompressed);
        WriteLittleEndianInteger(bytes, 34, imageSize);
        int pixelsPerMetre = ResolvePixelsPerMetre(plan.Size.DotsPerInch);
        WriteLittleEndianInteger(bytes, 38, pixelsPerMetre);
        WriteLittleEndianInteger(bytes, 42, pixelsPerMetre);
        WriteLittleEndianInteger(bytes, 46, paletteEntries);
        if (dibHeaderSize == BitmapV4HeaderSize)
        {
            WriteLittleEndianUnsignedInteger(bytes, 54, RedMask);
            WriteLittleEndianUnsignedInteger(bytes, 58, GreenMask);
            WriteLittleEndianUnsignedInteger(bytes, 62, BlueMask);
            WriteLittleEndianUnsignedInteger(bytes, 66, AlphaMask);
            WriteLittleEndianUnsignedInteger(bytes, 70, SrgbColorSpace);
        }

        int expectedImageSize = MultiplyDimensions(
            stride,
            plan.Size.HeightPixels);
        if (expectedImageSize != imageSize)
        {
            throw new BarcodeRenderingException(
                "The BMP image size is inconsistent.");
        }
    }

    static int ResolvePixelsPerMetre(double dotsPerInch)
    {
        if (dotsPerInch <= 0.0)
        {
            return 0;
        }

        double value = dotsPerInch * 10000.0 / 254.0;
        if (double.IsNaN(value)
            || double.IsInfinity(value)
            || value > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The BMP physical resolution exceeds the supported range.");
        }

        return (int)global::System.Math.Floor(value + 0.5);
    }

    static void WritePalette(
        byte[] bytes,
        BmpColor background,
        BmpColor foreground)
    {
        int offset = BitmapFileHeaderSize + BitmapInfoHeaderSize;
        WritePaletteColor(bytes, offset, background);
        WritePaletteColor(bytes, offset + 4, foreground);
    }

    static void WritePaletteColor(
        byte[] bytes,
        int offset,
        BmpColor color)
    {
        bytes[offset] = (byte)color.Blue;
        bytes[offset + 1] = (byte)color.Green;
        bytes[offset + 2] = (byte)color.Red;
        bytes[offset + 3] = 0;
    }

    static void FillBackground(
        byte[] bytes,
        int pixelOffset,
        int width,
        int height,
        int stride,
        int bitsPerPixel,
        BmpColor background)
    {
        if (bitsPerPixel == IndexedBitsPerPixel)
        {
            return;
        }

        int row = 0;
        while (row < height)
        {
            int targetRow = height - row - 1;
            int rowOffset = pixelOffset + (targetRow * stride);
            int column = 0;
            while (column < width)
            {
                WriteColorPixel(
                    bytes,
                    rowOffset,
                    column,
                    bitsPerPixel,
                    background);
                column = column + 1;
            }

            row = row + 1;
        }
    }

    static void FillRectangles(
        byte[] bytes,
        int pixelOffset,
        int width,
        int height,
        int stride,
        int bitsPerPixel,
        BmpColor foreground,
        BarcodeRectangle[] rectangles)
    {
        int rectangleIndex = 0;
        while (rectangleIndex < rectangles.Length)
        {
            BarcodeRectangle rectangle = rectangles[rectangleIndex];
            ValidateRectangle(rectangle, width, height);
            int left = (int)rectangle.X;
            int top = (int)rectangle.Y;
            int right = left + (int)rectangle.Width;
            int bottom = top + (int)rectangle.Height;
            int row = top;
            while (row < bottom)
            {
                int targetRow = height - row - 1;
                int rowOffset = pixelOffset + (targetRow * stride);
                int column = left;
                while (column < right)
                {
                    if (bitsPerPixel == IndexedBitsPerPixel)
                    {
                        int byteOffset = rowOffset + (column / 8);
                        int shift = 7 - (column % 8);
                        bytes[byteOffset] = (byte)(
                            bytes[byteOffset] | (1 << shift));
                    }
                    else
                    {
                        WriteColorPixel(
                            bytes,
                            rowOffset,
                            column,
                            bitsPerPixel,
                            foreground);
                    }

                    column = column + 1;
                }

                row = row + 1;
            }

            rectangleIndex = rectangleIndex + 1;
        }
    }

    static void WriteColorPixel(
        byte[] bytes,
        int rowOffset,
        int column,
        int bitsPerPixel,
        BmpColor color)
    {
        int bytesPerPixel = bitsPerPixel / 8;
        int offset = rowOffset + (column * bytesPerPixel);
        bytes[offset] = (byte)color.Blue;
        bytes[offset + 1] = (byte)color.Green;
        bytes[offset + 2] = (byte)color.Red;
        if (bitsPerPixel == AlphaBitsPerPixel)
        {
            bytes[offset + 3] = (byte)color.Alpha;
        }
    }

    static void WriteLittleEndianShort(
        byte[] target,
        int offset,
        int value)
    {
        target[offset] = (byte)(value & 255);
        target[offset + 1] = (byte)((value >> 8) & 255);
    }

    static void WriteLittleEndianInteger(
        byte[] target,
        int offset,
        int value)
    {
        target[offset] = (byte)(value & 255);
        target[offset + 1] = (byte)((value >> 8) & 255);
        target[offset + 2] = (byte)((value >> 16) & 255);
        target[offset + 3] = (byte)((value >> 24) & 255);
    }

    static void WriteLittleEndianUnsignedInteger(
        byte[] target,
        int offset,
        uint value)
    {
        target[offset] = (byte)(value & 255u);
        target[offset + 1] = (byte)((value >> 8) & 255u);
        target[offset + 2] = (byte)((value >> 16) & 255u);
        target[offset + 3] = (byte)((value >> 24) & 255u);
    }

    static int AddDimensions(int first, int second)
    {
        long result = (long)first + (long)second;
        if (result < 0L || result > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The BMP dimensions exceed the supported range.");
        }

        return (int)result;
    }

    static int MultiplyDimensions(int first, int second)
    {
        long result = (long)first * (long)second;
        if (result < 0L || result > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The BMP buffer exceeds the supported range.");
        }

        return (int)result;
    }

    static void ValidatePlan(RenderPlan plan)
    {
        if (plan.Size == null
            || plan.Size.WidthPixels <= 0
            || plan.Size.HeightPixels <= 0)
        {
            throw new BarcodeRenderingException(
                "The render plan must contain positive BMP dimensions.");
        }
    }

    static void ValidateRectangle(
        BarcodeRectangle rectangle,
        int width,
        int height)
    {
        if (rectangle == null
            || rectangle.X != global::System.Math.Floor(rectangle.X)
            || rectangle.Y != global::System.Math.Floor(rectangle.Y)
            || rectangle.Width
                != global::System.Math.Floor(rectangle.Width)
            || rectangle.Height
                != global::System.Math.Floor(rectangle.Height)
            || rectangle.X < 0.0
            || rectangle.Y < 0.0
            || rectangle.Width <= 0.0
            || rectangle.Height <= 0.0
            || rectangle.X + rectangle.Width > width
            || rectangle.Y + rectangle.Height > height)
        {
            throw new BarcodeRenderingException(
                "A coalesced rectangle is outside the BMP canvas.");
        }
    }
}
