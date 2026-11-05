namespace barcode;

internal class RasterResolvedColor
{
    internal int Red;
    internal int Green;
    internal int Blue;
    internal byte Luminance;
}

internal class RasterCanvas
{
    internal const uint DefaultForegroundRgba = 0x000000FFu;
    internal const uint DefaultBackgroundRgba = 0xFFFFFFFFu;
    internal BarcodeRenderFormat Format;
    internal BarcodePixelFormat PixelFormat;
    internal int Width;
    internal int Height;
    internal int Stride;
    internal byte[] Pixels = new byte[0];
    internal byte ForegroundSample;
    internal byte BackgroundSample;

    internal static RasterCanvas CreateRaw1(
        RenderPlan? plan,
        BarcodeRenderOptions? options)
    {
        return CreateCanvas(plan, options, BarcodeRenderFormat.Raw1);
    }

    internal static RasterCanvas CreateGray8(
        RenderPlan? plan,
        BarcodeRenderOptions? options)
    {
        return CreateCanvas(plan, options, BarcodeRenderFormat.Gray8);
    }

    static RasterCanvas CreateCanvas(
        RenderPlan? plan,
        BarcodeRenderOptions? options,
        BarcodeRenderFormat format)
    {
        if (plan == null)
        {
            throw new BarcodeRenderingException(
                "A render plan is required to create a raster canvas.");
        }

        ValidatePlanSize(plan);
        BarcodeRenderOptions settings = options == null
            ? new BarcodeRenderOptions()
            : options;
        RasterResolvedColor backgroundColor =
            ResolveBackgroundColor(settings);
        byte background = backgroundColor.Luminance;
        byte foreground = ResolveForegroundLuminance(
            settings,
            backgroundColor);
        int width = plan.Size.WidthPixels;
        int height = plan.Size.HeightPixels;
        int stride = format == BarcodeRenderFormat.Raw1
            ? ResolveRawStride(width)
            : width;
        byte[] pixels = CreatePixelBuffer(stride, height);
        if (format == BarcodeRenderFormat.Raw1)
        {
            FillRawBackground(
                pixels,
                width,
                height,
                stride,
                ToRawBit(background));
        }
        else
        {
            FillGrayBackground(pixels, background);
        }

        RenderRectangleSet rectangles =
            RectangleCoalescer.Coalesce(plan);
        FillRectangleSet(
            pixels,
            width,
            height,
            stride,
            format,
            foreground,
            rectangles.PixelRectangles);
        HumanReadableTextRenderResult text =
            HumanReadableTextRenderer.Render(plan);
        FillRectangleSet(
            pixels,
            width,
            height,
            stride,
            format,
            foreground,
            text.PixelRectangles);

        RasterCanvas canvas = new RasterCanvas();
        canvas.Format = format;
        canvas.PixelFormat = BarcodePixelFormat.Gray8;
        canvas.Width = width;
        canvas.Height = height;
        canvas.Stride = stride;
        canvas.Pixels = pixels;
        canvas.ForegroundSample = foreground;
        canvas.BackgroundSample = background;
        return canvas;
    }

    static void FillRectangleSet(
        byte[] pixels,
        int width,
        int height,
        int stride,
        BarcodeRenderFormat format,
        byte foreground,
        BarcodeRectangle[] rectangles)
    {
        int index = 0;
        while (index < rectangles.Length)
        {
            BarcodeRectangle rectangle = rectangles[index];
            ValidateRectangle(rectangle, width, height);
            if (format == BarcodeRenderFormat.Raw1)
            {
                FillRawRectangle(
                    pixels,
                    stride,
                    rectangle,
                    ToRawBit(foreground));
            }
            else
            {
                FillGrayRectangle(
                    pixels,
                    stride,
                    rectangle,
                    foreground);
            }

            index = index + 1;
        }
    }

    static RasterResolvedColor ResolveBackgroundColor(
        BarcodeRenderOptions options)
    {
        uint rgba = options.BackgroundRgba == 0u
            ? DefaultBackgroundRgba
            : options.BackgroundRgba;
        int alpha = options.TransparentBackground
            ? 0
            : ReadAlpha(rgba);
        int red = CompositeOverWhite(ReadRed(rgba), alpha);
        int green = CompositeOverWhite(ReadGreen(rgba), alpha);
        int blue = CompositeOverWhite(ReadBlue(rgba), alpha);
        RasterResolvedColor result = new RasterResolvedColor();
        result.Red = red;
        result.Green = green;
        result.Blue = blue;
        result.Luminance = ToLuminance(red, green, blue);
        return result;
    }

    static byte ResolveForegroundLuminance(
        BarcodeRenderOptions options,
        RasterResolvedColor background)
    {
        uint rgba = options.ForegroundRgba == 0u
            ? DefaultForegroundRgba
            : options.ForegroundRgba;
        int alpha = ReadAlpha(rgba);
        int red = CompositeChannel(
            ReadRed(rgba),
            background.Red,
            alpha);
        int green = CompositeChannel(
            ReadGreen(rgba),
            background.Green,
            alpha);
        int blue = CompositeChannel(
            ReadBlue(rgba),
            background.Blue,
            alpha);
        return ToLuminance(red, green, blue);
    }

    static int CompositeOverWhite(int channel, int alpha)
    {
        return ((channel * alpha)
            + (255 * (255 - alpha))
            + 127) / 255;
    }

    static int CompositeChannel(
        int channel,
        int background,
        int alpha)
    {
        return ((channel * alpha)
            + (background * (255 - alpha))
            + 127) / 255;
    }

    static byte ToLuminance(int red, int green, int blue)
    {
        return (byte)(((77 * red)
            + (150 * green)
            + (29 * blue)
            + 128) >> 8);
    }

    static int ReadRed(uint rgba)
    {
        return (int)((rgba >> 24) & 255u);
    }

    static int ReadGreen(uint rgba)
    {
        return (int)((rgba >> 16) & 255u);
    }

    static int ReadBlue(uint rgba)
    {
        return (int)((rgba >> 8) & 255u);
    }

    static int ReadAlpha(uint rgba)
    {
        return (int)(rgba & 255u);
    }

    static int ToRawBit(byte luminance)
    {
        return luminance >= 128 ? 1 : 0;
    }

    static int ResolveRawStride(int width)
    {
        long stride = ((long)width + 7L) / 8L;
        if (stride > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The packed raster stride exceeds the supported range.");
        }

        return (int)stride;
    }

    static byte[] CreatePixelBuffer(int stride, int height)
    {
        long length = (long)stride * (long)height;
        if (length < 0L || length > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The raster buffer exceeds the supported range.");
        }

        return new byte[(int)length];
    }

    static void FillGrayBackground(byte[] pixels, byte value)
    {
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = value;
            index = index + 1;
        }
    }

    static void FillRawBackground(
        byte[] pixels,
        int width,
        int height,
        int stride,
        int bit)
    {
        byte value = bit == 0 ? (byte)0 : (byte)255;
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = value;
            index = index + 1;
        }

        int remainder = width % 8;
        if (bit != 0 && remainder != 0)
        {
            byte mask = (byte)((255 << (8 - remainder)) & 255);
            int row = 0;
            while (row < height)
            {
                int lastByte = (row * stride) + stride - 1;
                pixels[lastByte] = (byte)(pixels[lastByte] & mask);
                row = row + 1;
            }
        }
    }

    static void FillGrayRectangle(
        byte[] pixels,
        int stride,
        BarcodeRectangle rectangle,
        byte value)
    {
        int left = (int)rectangle.X;
        int top = (int)rectangle.Y;
        int right = left + (int)rectangle.Width;
        int bottom = top + (int)rectangle.Height;
        int row = top;
        while (row < bottom)
        {
            int offset = (row * stride) + left;
            int column = left;
            while (column < right)
            {
                pixels[offset] = value;
                offset = offset + 1;
                column = column + 1;
            }

            row = row + 1;
        }
    }

    static void FillRawRectangle(
        byte[] pixels,
        int stride,
        BarcodeRectangle rectangle,
        int bit)
    {
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
                int byteOffset = (row * stride) + (column / 8);
                int mask = 128 >> (column % 8);
                if (bit == 0)
                {
                    pixels[byteOffset] = (byte)(
                        pixels[byteOffset] & ~mask);
                }
                else
                {
                    pixels[byteOffset] = (byte)(
                        pixels[byteOffset] | mask);
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    static void ValidatePlanSize(RenderPlan plan)
    {
        if (plan.Size == null
            || plan.Size.WidthPixels <= 0
            || plan.Size.HeightPixels <= 0)
        {
            throw new BarcodeRenderingException(
                "The render plan must contain positive raster dimensions.");
        }

        bool swapsAxes = plan.Rotation == BarcodeRotation.Clockwise90
            || plan.Rotation == BarcodeRotation.Clockwise270;
        int expectedWidth = swapsAxes
            ? plan.UnrotatedHeightPixels
            : plan.UnrotatedWidthPixels;
        int expectedHeight = swapsAxes
            ? plan.UnrotatedWidthPixels
            : plan.UnrotatedHeightPixels;
        if (plan.Size.WidthPixels != expectedWidth
            || plan.Size.HeightPixels != expectedHeight)
        {
            throw new BarcodeRenderingException(
                "The render plan raster dimensions do not match its rotation.");
        }
    }

    static void ValidateRectangle(
        BarcodeRectangle rectangle,
        int width,
        int height)
    {
        if (rectangle == null
            || double.IsNaN(rectangle.X)
            || double.IsNaN(rectangle.Y)
            || double.IsNaN(rectangle.Width)
            || double.IsNaN(rectangle.Height)
            || double.IsInfinity(rectangle.X)
            || double.IsInfinity(rectangle.Y)
            || double.IsInfinity(rectangle.Width)
            || double.IsInfinity(rectangle.Height)
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
                "A coalesced rectangle is outside the raster canvas.");
        }
    }
}
