namespace barcode;

internal class PngColor
{
    internal int Red;
    internal int Green;
    internal int Blue;
    internal int Alpha;
}

internal class PngFilterResult
{
    internal byte[] Scanlines = new byte[0];
    internal byte[] FilterTypes = new byte[0];
}

internal class PngWriteResult
{
    internal byte[] Bytes = new byte[0];
    internal int ColorType;
    internal int BitDepth;
    internal byte[] FilterTypes = new byte[0];
}

internal static class PngWriter
{
    internal const int GrayscaleColorType = 0;
    internal const int TruecolorColorType = 2;
    internal const int GrayscaleAlphaColorType = 4;
    internal const int TruecolorAlphaColorType = 6;
    internal const int EightBitDepth = 8;

    internal static PngWriteResult Write(
        RenderPlan? plan,
        BarcodeRenderOptions? options)
    {
        if (plan == null)
        {
            throw new BarcodeRenderingException(
                "A render plan is required to write a PNG image.");
        }

        ValidatePlan(plan);
        BarcodeRenderOptions settings = options == null
            ? new BarcodeRenderOptions()
            : options;
        PngColor background = ResolveBackground(settings);
        PngColor foreground = ResolveForeground(settings, background);
        int colorType = ResolveColorType(foreground, background);
        int channelCount = ResolveChannelCount(colorType);
        RenderRectangleSet rectangles = RectangleCoalescer.Coalesce(plan);
        byte[] pixels = CreatePixels(
            plan.Size.WidthPixels,
            plan.Size.HeightPixels,
            channelCount,
            background);
        FillRectangles(
            pixels,
            plan.Size.WidthPixels,
            plan.Size.HeightPixels,
            channelCount,
            colorType,
            foreground,
            rectangles.PixelRectangles);
        HumanReadableTextRenderResult text =
            HumanReadableTextRenderer.Render(plan);
        FillRectangles(
            pixels,
            plan.Size.WidthPixels,
            plan.Size.HeightPixels,
            channelCount,
            colorType,
            foreground,
            text.PixelRectangles);
        PngFilterResult filtered = FilterScanlines(
            pixels,
            plan.Size.WidthPixels,
            plan.Size.HeightPixels,
            channelCount);
        byte[] compressed = Compress(filtered.Scanlines);
        byte[] bytes = WritePng(
            plan,
            colorType,
            compressed);

        PngWriteResult result = new PngWriteResult();
        result.Bytes = bytes;
        result.ColorType = colorType;
        result.BitDepth = EightBitDepth;
        result.FilterTypes = filtered.FilterTypes;
        return result;
    }

    static PngColor ResolveBackground(BarcodeRenderOptions options)
    {
        uint rgba = options.BackgroundRgba == 0u
            ? RasterCanvas.DefaultBackgroundRgba
            : options.BackgroundRgba;
        PngColor result = ReadColor(rgba);
        if (options.TransparentBackground)
        {
            result.Alpha = 0;
        }

        return result;
    }

    static PngColor ResolveForeground(
        BarcodeRenderOptions options,
        PngColor background)
    {
        uint rgba = options.ForegroundRgba == 0u
            ? RasterCanvas.DefaultForegroundRgba
            : options.ForegroundRgba;
        PngColor foreground = ReadColor(rgba);
        return CompositeColors(foreground, background);
    }

    static PngColor CompositeColors(
        PngColor foreground,
        PngColor background)
    {
        int inverseForegroundAlpha = 255 - foreground.Alpha;
        int alphaNumerator = (foreground.Alpha * 255)
            + (background.Alpha * inverseForegroundAlpha);
        int outputAlpha = (alphaNumerator + 127) / 255;
        PngColor result = new PngColor();
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

    static PngColor ReadColor(uint rgba)
    {
        PngColor result = new PngColor();
        result.Red = (int)((rgba >> 24) & 255u);
        result.Green = (int)((rgba >> 16) & 255u);
        result.Blue = (int)((rgba >> 8) & 255u);
        result.Alpha = (int)(rgba & 255u);
        return result;
    }

    static int ResolveColorType(PngColor foreground, PngColor background)
    {
        bool grayscale = IsGrayscale(foreground)
            && IsGrayscale(background);
        bool hasAlpha = foreground.Alpha != 255
            || background.Alpha != 255;
        if (grayscale)
        {
            return hasAlpha
                ? GrayscaleAlphaColorType
                : GrayscaleColorType;
        }

        return hasAlpha
            ? TruecolorAlphaColorType
            : TruecolorColorType;
    }

    static bool IsGrayscale(PngColor color)
    {
        return color.Red == color.Green
            && color.Green == color.Blue;
    }

    static int ResolveChannelCount(int colorType)
    {
        switch (colorType)
        {
            case GrayscaleColorType:
                return 1;
            case TruecolorColorType:
                return 3;
            case GrayscaleAlphaColorType:
                return 2;
            case TruecolorAlphaColorType:
                return 4;
            default:
                throw new BarcodeRenderingException(
                    "The PNG color type is unsupported.");
        }
    }

    static byte[] CreatePixels(
        int width,
        int height,
        int channelCount,
        PngColor background)
    {
        int rowBytes = MultiplyDimensions(width, channelCount);
        byte[] pixels = new byte[MultiplyDimensions(rowBytes, height)];
        int pixelCount = MultiplyDimensions(width, height);
        int pixel = 0;
        while (pixel < pixelCount)
        {
            WritePixel(
                pixels,
                pixel * channelCount,
                channelCount,
                background);
            pixel = pixel + 1;
        }

        return pixels;
    }

    static void FillRectangles(
        byte[] pixels,
        int width,
        int height,
        int channelCount,
        int colorType,
        PngColor foreground,
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
                int column = left;
                while (column < right)
                {
                    int pixelOffset = ((row * width) + column)
                        * channelCount;
                    WritePixelForColorType(
                        pixels,
                        pixelOffset,
                        colorType,
                        foreground);
                    column = column + 1;
                }

                row = row + 1;
            }

            rectangleIndex = rectangleIndex + 1;
        }
    }

    static void WritePixel(
        byte[] pixels,
        int offset,
        int channelCount,
        PngColor color)
    {
        if (channelCount == 1)
        {
            pixels[offset] = (byte)color.Red;
        }
        else if (channelCount == 2)
        {
            pixels[offset] = (byte)color.Red;
            pixels[offset + 1] = (byte)color.Alpha;
        }
        else if (channelCount == 3)
        {
            pixels[offset] = (byte)color.Red;
            pixels[offset + 1] = (byte)color.Green;
            pixels[offset + 2] = (byte)color.Blue;
        }
        else
        {
            pixels[offset] = (byte)color.Red;
            pixels[offset + 1] = (byte)color.Green;
            pixels[offset + 2] = (byte)color.Blue;
            pixels[offset + 3] = (byte)color.Alpha;
        }
    }

    static void WritePixelForColorType(
        byte[] pixels,
        int offset,
        int colorType,
        PngColor color)
    {
        WritePixel(
            pixels,
            offset,
            ResolveChannelCount(colorType),
            color);
    }

    static PngFilterResult FilterScanlines(
        byte[] pixels,
        int width,
        int height,
        int bytesPerPixel)
    {
        int rowBytes = MultiplyDimensions(width, bytesPerPixel);
        int outputRowBytes = AddDimensions(rowBytes, 1);
        byte[] output = new byte[
            MultiplyDimensions(outputRowBytes, height)];
        byte[] filters = new byte[height];
        byte[] candidate = new byte[rowBytes];
        byte[] best = new byte[rowBytes];
        int row = 0;
        while (row < height)
        {
            long bestScore = long.MaxValue;
            int bestFilter = 0;
            int filter = 0;
            while (filter <= 4)
            {
                long score = FilterRow(
                    pixels,
                    row,
                    rowBytes,
                    bytesPerPixel,
                    filter,
                    candidate);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestFilter = filter;
                    CopyBytes(candidate, best);
                }

                filter = filter + 1;
            }

            int outputOffset = row * outputRowBytes;
            output[outputOffset] = (byte)bestFilter;
            CopyBytesAt(best, output, outputOffset + 1);
            filters[row] = (byte)bestFilter;
            row = row + 1;
        }

        PngFilterResult result = new PngFilterResult();
        result.Scanlines = output;
        result.FilterTypes = filters;
        return result;
    }

    static long FilterRow(
        byte[] pixels,
        int row,
        int rowBytes,
        int bytesPerPixel,
        int filter,
        byte[] output)
    {
        int rowOffset = row * rowBytes;
        int previousOffset = rowOffset - rowBytes;
        long score = 0L;
        int index = 0;
        while (index < rowBytes)
        {
            int source = pixels[rowOffset + index];
            int left = index < bytesPerPixel
                ? 0
                : pixels[rowOffset + index - bytesPerPixel];
            int above = row == 0
                ? 0
                : pixels[previousOffset + index];
            int upperLeft = row == 0 || index < bytesPerPixel
                ? 0
                : pixels[previousOffset + index - bytesPerPixel];
            int predictor = ResolvePredictor(
                filter,
                left,
                above,
                upperLeft);
            int filtered = (source - predictor) & 255;
            output[index] = (byte)filtered;
            score = score + (filtered < 128
                ? filtered
                : 256 - filtered);
            index = index + 1;
        }

        return score;
    }

    static int ResolvePredictor(
        int filter,
        int left,
        int above,
        int upperLeft)
    {
        switch (filter)
        {
            case 0:
                return 0;
            case 1:
                return left;
            case 2:
                return above;
            case 3:
                return (left + above) / 2;
            case 4:
                return PaethPredictor(left, above, upperLeft);
            default:
                return 0;
        }
    }

    static int PaethPredictor(int left, int above, int upperLeft)
    {
        int estimate = left + above - upperLeft;
        int leftDistance = global::System.Math.Abs(estimate - left);
        int aboveDistance = global::System.Math.Abs(estimate - above);
        int upperLeftDistance = global::System.Math.Abs(
            estimate - upperLeft);
        if (leftDistance <= aboveDistance
            && leftDistance <= upperLeftDistance)
        {
            return left;
        }

        if (aboveDistance <= upperLeftDistance)
        {
            return above;
        }

        return upperLeft;
    }

    static byte[] Compress(byte[] source)
    {
        using (global::System.IO.MemoryStream output =
            new global::System.IO.MemoryStream())
        {
            using (global::System.IO.Compression.ZLibStream compressor =
                new global::System.IO.Compression.ZLibStream(
                    output,
                    global::System.IO.Compression.CompressionLevel.Optimal,
                    true))
            {
                compressor.Write(source, 0, source.Length);
            }

            return output.ToArray();
        }
    }

    static byte[] WritePng(
        RenderPlan plan,
        int colorType,
        byte[] compressed)
    {
        using (global::System.IO.MemoryStream output =
            new global::System.IO.MemoryStream())
        {
            WriteSignature(output);
            WriteChunk(
                output,
                "IHDR",
                CreateHeader(
                    plan.Size.WidthPixels,
                    plan.Size.HeightPixels,
                    colorType));
            if (plan.Size.DotsPerInch > 0.0)
            {
                WriteChunk(
                    output,
                    "pHYs",
                    CreatePhysicalResolution(plan.Size.DotsPerInch));
            }

            WriteChunk(output, "IDAT", compressed);
            WriteChunk(output, "IEND", new byte[0]);
            return output.ToArray();
        }
    }

    static void WriteSignature(global::System.IO.MemoryStream output)
    {
        output.WriteByte(137);
        output.WriteByte(80);
        output.WriteByte(78);
        output.WriteByte(71);
        output.WriteByte(13);
        output.WriteByte(10);
        output.WriteByte(26);
        output.WriteByte(10);
    }

    static byte[] CreateHeader(int width, int height, int colorType)
    {
        byte[] header = new byte[13];
        WriteBigEndianIntegerToArray(header, 0, width);
        WriteBigEndianIntegerToArray(header, 4, height);
        header[8] = EightBitDepth;
        header[9] = (byte)colorType;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        return header;
    }

    static byte[] CreatePhysicalResolution(double dotsPerInch)
    {
        double pixelsPerMetreValue = dotsPerInch
            * 10000.0
            / 254.0;
        if (double.IsNaN(pixelsPerMetreValue)
            || double.IsInfinity(pixelsPerMetreValue)
            || pixelsPerMetreValue > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The PNG physical resolution exceeds the supported range.");
        }

        int pixelsPerMetre = (int)global::System.Math.Floor(
            pixelsPerMetreValue + 0.5);
        byte[] resolution = new byte[9];
        WriteBigEndianIntegerToArray(resolution, 0, pixelsPerMetre);
        WriteBigEndianIntegerToArray(resolution, 4, pixelsPerMetre);
        resolution[8] = 1;
        return resolution;
    }

    static void WriteChunk(
        global::System.IO.MemoryStream output,
        string type,
        byte[] data)
    {
        byte[] typeBytes = new byte[4];
        int index = 0;
        while (index < typeBytes.Length)
        {
            typeBytes[index] = (byte)type[index];
            index = index + 1;
        }

        WriteBigEndianIntegerToStream(output, data.Length);
        output.Write(typeBytes, 0, typeBytes.Length);
        output.Write(data, 0, data.Length);
        uint crc = CalculateCrc(typeBytes, data);
        WriteBigEndianUnsignedInteger(output, crc);
    }

    static uint CalculateCrc(byte[] type, byte[] data)
    {
        uint crc = 0xFFFFFFFFu;
        int index = 0;
        while (index < type.Length)
        {
            crc = UpdateCrc(crc, type[index]);
            index = index + 1;
        }

        index = 0;
        while (index < data.Length)
        {
            crc = UpdateCrc(crc, data[index]);
            index = index + 1;
        }

        return crc ^ 0xFFFFFFFFu;
    }

    static uint UpdateCrc(uint crc, byte value)
    {
        uint current = crc ^ value;
        int bit = 0;
        while (bit < 8)
        {
            current = (current & 1u) == 0u
                ? current >> 1
                : (current >> 1) ^ 0xEDB88320u;
            bit = bit + 1;
        }

        return current;
    }

    static void WriteBigEndianIntegerToArray(
        byte[] target,
        int offset,
        int value)
    {
        target[offset] = (byte)((value >> 24) & 255);
        target[offset + 1] = (byte)((value >> 16) & 255);
        target[offset + 2] = (byte)((value >> 8) & 255);
        target[offset + 3] = (byte)(value & 255);
    }

    static void WriteBigEndianIntegerToStream(
        global::System.IO.MemoryStream output,
        int value)
    {
        output.WriteByte((byte)((value >> 24) & 255));
        output.WriteByte((byte)((value >> 16) & 255));
        output.WriteByte((byte)((value >> 8) & 255));
        output.WriteByte((byte)(value & 255));
    }

    static void WriteBigEndianUnsignedInteger(
        global::System.IO.MemoryStream output,
        uint value)
    {
        output.WriteByte((byte)((value >> 24) & 255u));
        output.WriteByte((byte)((value >> 16) & 255u));
        output.WriteByte((byte)((value >> 8) & 255u));
        output.WriteByte((byte)(value & 255u));
    }

    static void CopyBytes(byte[] source, byte[] target)
    {
        int index = 0;
        while (index < source.Length)
        {
            target[index] = source[index];
            index = index + 1;
        }
    }

    static void CopyBytesAt(
        byte[] source,
        byte[] target,
        int targetOffset)
    {
        int index = 0;
        while (index < source.Length)
        {
            target[targetOffset + index] = source[index];
            index = index + 1;
        }
    }

    static int AddDimensions(int first, int second)
    {
        long result = (long)first + (long)second;
        if (result > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The PNG dimensions exceed the supported range.");
        }

        return (int)result;
    }

    static int MultiplyDimensions(int first, int second)
    {
        long result = (long)first * (long)second;
        if (result < 0L || result > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The PNG buffer exceeds the supported range.");
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
                "The render plan must contain positive PNG dimensions.");
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
                "A coalesced rectangle is outside the PNG canvas.");
        }
    }
}
