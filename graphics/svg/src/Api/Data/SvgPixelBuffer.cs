namespace svg;

/// <summary>Represents a nonnegative-size integer pixel rectangle.</summary>
public class SvgPixelRect
{
    /// <summary>The horizontal pixel coordinate.</summary>
    public int X;
    /// <summary>The vertical pixel coordinate.</summary>
    public int Y;
    /// <summary>The nonnegative pixel width.</summary>
    public int Width;
    /// <summary>The nonnegative pixel height.</summary>
    public int Height;
    /// <summary>The exclusive horizontal edge.</summary>
    public int Right;
    /// <summary>The exclusive vertical edge.</summary>
    public int Bottom;
    /// <summary>Whether either dimension is zero.</summary>
    public bool IsEmpty;

    /// <summary>Creates an integer pixel rectangle with checked exclusive edges.</summary>
    /// <param name="x">The horizontal pixel coordinate.</param>
    /// <param name="y">The vertical pixel coordinate.</param>
    /// <param name="width">The nonnegative pixel width.</param>
    /// <param name="height">The nonnegative pixel height.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for negative dimensions or overflowing exclusive edges.</exception>
    public SvgPixelRect(int x, int y, int width, int height)
    {
        long right = (long)x + width;
        long bottom = (long)y + height;
        if (width < 0 || height < 0 || right < int.MinValue || right > int.MaxValue || bottom < int.MinValue || bottom > int.MaxValue) throw new System.ArgumentOutOfRangeException("width");
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Right = (int)right;
        Bottom = (int)bottom;
        IsEmpty = width == 0 || height == 0;
    }
}

/// <summary>Contains one alpha-premultiplied red, green, blue, alpha byte quadruple.</summary>
public class SvgPremultipliedRgba8
{
    /// <summary>The red channel, no greater than Alpha.</summary>
    public byte Red;
    /// <summary>The green channel, no greater than Alpha.</summary>
    public byte Green;
    /// <summary>The blue channel, no greater than Alpha.</summary>
    public byte Blue;
    /// <summary>The alpha channel.</summary>
    public byte Alpha;

    /// <summary>Creates one validated premultiplied RGBA8 pixel.</summary>
    /// <param name="red">The premultiplied red channel.</param>
    /// <param name="green">The premultiplied green channel.</param>
    /// <param name="blue">The premultiplied blue channel.</param>
    /// <param name="alpha">The alpha channel.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a color channel exceeds alpha.</exception>
    public SvgPremultipliedRgba8(byte red, byte green, byte blue, byte alpha)
    {
        if (red > alpha || green > alpha || blue > alpha) throw new System.ArgumentOutOfRangeException("red");
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    /// <summary>Premultiplies straight RGBA8 channels using round-to-nearest integer arithmetic.</summary>
    /// <param name="red">The straight red channel.</param>
    /// <param name="green">The straight green channel.</param>
    /// <param name="blue">The straight blue channel.</param>
    /// <param name="alpha">The alpha channel.</param>
    /// <returns>The alpha-premultiplied pixel.</returns>
    internal static SvgPremultipliedRgba8 FromStraight(byte red, byte green, byte blue, byte alpha)
    {
        byte premultipliedRed = Premultiply(red, alpha);
        byte premultipliedGreen = Premultiply(green, alpha);
        byte premultipliedBlue = Premultiply(blue, alpha);
        return new SvgPremultipliedRgba8(premultipliedRed, premultipliedGreen, premultipliedBlue, alpha);
    }

    internal static byte Premultiply(byte channel, byte alpha)
    {
        return (byte)((channel * alpha + 127) / 255);
    }
}

/// <summary>Contains a created, wrapped, or shared RGBA8 pixel buffer.</summary>
public class SvgPixelBufferResult
{
    /// <summary>Whether the buffer operation succeeded.</summary>
    public bool Success;
    /// <summary>The stable buffer outcome.</summary>
    public SvgPixelBufferError Error;
    /// <summary>The resulting buffer, or <c>null</c>.</summary>
    public SvgPixelBuffer? Buffer;

    internal SvgPixelBufferResult(bool success, SvgPixelBufferError error, SvgPixelBuffer? buffer)
    {
        Success = success;
        Error = error;
        Buffer = buffer;
    }
}

/// <summary>Contains a completed clear, write, or copy operation.</summary>
public class SvgPixelOperationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success;
    /// <summary>The stable operation outcome.</summary>
    public SvgPixelBufferError Error;
    /// <summary>The number of pixels written.</summary>
    public long PixelsWritten;

    internal SvgPixelOperationResult(bool success, SvgPixelBufferError error, long pixelsWritten)
    {
        Success = success;
        Error = error;
        PixelsWritten = pixelsWritten;
    }
}

/// <summary>Contains one read premultiplied pixel or a stable failure.</summary>
public class SvgPixelReadResult
{
    /// <summary>Whether the pixel was read.</summary>
    public bool Success;
    /// <summary>The stable read outcome.</summary>
    public SvgPixelBufferError Error;
    /// <summary>The independent pixel value, or <c>null</c>.</summary>
    public SvgPremultipliedRgba8? Pixel;

    internal SvgPixelReadResult(bool success, SvgPixelBufferError error, SvgPremultipliedRgba8? pixel)
    {
        Success = success;
        Error = error;
        Pixel = pixel;
    }
}

/// <summary>Stores bounded alpha-premultiplied RGBA8 rows with explicit stride and color space.</summary>
public class SvgPixelBuffer
{
    /// <summary>The shared RGBA8 backing bytes in red, green, blue, alpha order.</summary>
    public byte[] Data;
    /// <summary>The byte offset of this view's top-left pixel.</summary>
    public int Offset;
    /// <summary>The positive or zero byte distance between adjacent rows.</summary>
    public int Stride;
    /// <summary>The view width in pixels.</summary>
    public int Width;
    /// <summary>The view height in pixels.</summary>
    public int Height;
    /// <summary>The active bytes in each row, exactly four times Width.</summary>
    public int RowBytes;
    /// <summary>The byte footprint from Offset through the final active row.</summary>
    public long ByteLength;
    /// <summary>The concrete channel color space.</summary>
    public SvgPixelColorSpace ColorSpace;
    /// <summary>Whether this buffer allocated and initially owned Data.</summary>
    public bool OwnsData;

    internal SvgPixelBuffer(byte[] data, int offset, int width, int height, int stride, SvgPixelColorSpace colorSpace, bool ownsData)
    {
        Data = data;
        Offset = offset;
        Stride = stride;
        Width = width;
        Height = height;
        RowBytes = width * 4;
        ByteLength = width == 0 || height == 0 ? 0 : (long)(height - 1) * stride + RowBytes;
        ColorSpace = colorSpace;
        OwnsData = ownsData;
    }

    /// <summary>Allocates a tightly packed transparent buffer using secure raster limits.</summary>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <param name="colorSpace">The concrete channel color space.</param>
    /// <returns>The owned buffer or a stable failure.</returns>
    internal static SvgPixelBufferResult Create(int width, int height, SvgPixelColorSpace colorSpace)
    {
        return Create(width, height, colorSpace, new SvgRasterLimits());
    }

    /// <summary>Allocates a tightly packed transparent buffer with explicit raster limits.</summary>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <param name="colorSpace">The concrete channel color space.</param>
    /// <param name="limits">The allocation dimension and byte limits.</param>
    /// <returns>The owned buffer or a stable failure.</returns>
    internal static SvgPixelBufferResult Create(int width, int height, SvgPixelColorSpace colorSpace, SvgRasterLimits limits)
    {
        SvgPixelBufferError validation = ValidateDimensions(width, height, colorSpace, limits);
        if (validation != SvgPixelBufferError.None) return Failure(validation);
        long rowBytes = (long)width * 4;
        if (rowBytes > int.MaxValue) return Failure(SvgPixelBufferError.ByteLimitExceeded);
        long byteLength = rowBytes * height;
        if (byteLength > System.Array.MaxLength || byteLength > limits.MaxPixelBytes) return Failure(SvgPixelBufferError.ByteLimitExceeded);
        byte[] data = new byte[(int)byteLength];
        SvgPixelBuffer buffer = new SvgPixelBuffer(data, 0, width, height, (int)rowBytes, colorSpace, true);
        return new SvgPixelBufferResult(true, SvgPixelBufferError.None, buffer);
    }

    /// <summary>Wraps and validates premultiplied rows in caller-owned storage.</summary>
    /// <param name="data">The caller-owned backing bytes.</param>
    /// <param name="offset">The top-left byte offset.</param>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <param name="stride">The nonnegative row stride, at least four times Width.</param>
    /// <param name="colorSpace">The concrete channel color space.</param>
    /// <returns>The sharing buffer or a stable failure.</returns>
    internal static SvgPixelBufferResult Wrap(byte[] data, int offset, int width, int height, int stride, SvgPixelColorSpace colorSpace)
    {
        return Wrap(data, offset, width, height, stride, colorSpace, new SvgRasterLimits());
    }

    /// <summary>Wraps and validates premultiplied rows with explicit raster limits.</summary>
    /// <param name="data">The caller-owned backing bytes.</param>
    /// <param name="offset">The top-left byte offset.</param>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <param name="stride">The nonnegative row stride, at least four times Width.</param>
    /// <param name="colorSpace">The concrete channel color space.</param>
    /// <param name="limits">The dimension and byte-footprint limits.</param>
    /// <returns>The sharing buffer or a stable failure.</returns>
    internal static SvgPixelBufferResult Wrap(byte[]? data, int offset, int width, int height, int stride, SvgPixelColorSpace colorSpace, SvgRasterLimits limits)
    {
        if (data == null || offset < 0 || offset > data.Length || stride < 0) return Failure(SvgPixelBufferError.InvalidArgument);
        SvgPixelBufferError validation = ValidateDimensions(width, height, colorSpace, limits);
        if (validation != SvgPixelBufferError.None) return Failure(validation);
        long rowBytes = (long)width * 4;
        if (rowBytes > int.MaxValue || stride < rowBytes) return Failure(SvgPixelBufferError.InvalidArgument);
        long footprint = width == 0 || height == 0 ? 0 : (long)(height - 1) * stride + rowBytes;
        if (footprint > limits.MaxPixelBytes || footprint > int.MaxValue) return Failure(SvgPixelBufferError.ByteLimitExceeded);
        if ((long)offset + footprint > data.Length) return Failure(SvgPixelBufferError.BoundsExceeded);
        SvgPixelBuffer buffer = new SvgPixelBuffer(data, offset, width, height, stride, colorSpace, false);
        if (!Premultiplied(buffer)) return Failure(SvgPixelBufferError.InvalidPremultipliedColor);
        return new SvgPixelBufferResult(true, SvgPixelBufferError.None, buffer);
    }

    /// <summary>Reads one independent premultiplied pixel.</summary>
    /// <param name="x">The zero-based view x coordinate.</param>
    /// <param name="y">The zero-based view y coordinate.</param>
    /// <returns>The pixel or a stable failure.</returns>
    internal SvgPixelReadResult Read(int x, int y)
    {
        SvgPixelBufferError validation = ValidateBuffer(this);
        if (validation != SvgPixelBufferError.None) return ReadFailure(validation);
        if (x < 0 || y < 0 || x >= Width || y >= Height) return ReadFailure(SvgPixelBufferError.BoundsExceeded);
        int index = Offset + y * Stride + x * 4;
        byte alpha = Data[index + 3];
        if (Data[index] > alpha || Data[index + 1] > alpha || Data[index + 2] > alpha) return ReadFailure(SvgPixelBufferError.InvalidPremultipliedColor);
        SvgPremultipliedRgba8 pixel = new SvgPremultipliedRgba8(Data[index], Data[index + 1], Data[index + 2], alpha);
        return new SvgPixelReadResult(true, SvgPixelBufferError.None, pixel);
    }

    /// <summary>Writes one validated premultiplied pixel.</summary>
    /// <param name="x">The zero-based view x coordinate.</param>
    /// <param name="y">The zero-based view y coordinate.</param>
    /// <param name="pixel">The premultiplied pixel.</param>
    /// <returns>The write result.</returns>
    internal SvgPixelOperationResult Write(int x, int y, SvgPremultipliedRgba8 pixel)
    {
        SvgPixelBufferError validation = ValidateBuffer(this);
        if (validation != SvgPixelBufferError.None) return OperationFailure(validation);
        if (!ValidPixel(pixel)) return OperationFailure(SvgPixelBufferError.InvalidPremultipliedColor);
        if (x < 0 || y < 0 || x >= Width || y >= Height) return OperationFailure(SvgPixelBufferError.BoundsExceeded);
        int index = Offset + y * Stride + x * 4;
        Store(Data, index, pixel);
        return new SvgPixelOperationResult(true, SvgPixelBufferError.None, 1);
    }

    /// <summary>Clears the complete view to transparent black.</summary>
    /// <returns>The clear result.</returns>
    internal SvgPixelOperationResult Clear()
    {
        return ClearWithPixel(new SvgPremultipliedRgba8(0, 0, 0, 0));
    }

    /// <summary>Clears the complete view to one premultiplied pixel.</summary>
    /// <param name="pixel">The premultiplied fill pixel.</param>
    /// <returns>The clear result.</returns>
    internal SvgPixelOperationResult ClearWithPixel(SvgPremultipliedRgba8 pixel)
    {
        SvgPixelBufferError validation = ValidateBuffer(this);
        if (validation != SvgPixelBufferError.None) return OperationFailure(validation);
        return ClearRectangle(new SvgPixelRect(0, 0, Width, Height), pixel);
    }

    /// <summary>Clears one in-bounds rectangle without modifying row padding.</summary>
    /// <param name="rectangle">The view-local rectangle.</param>
    /// <param name="pixel">The premultiplied fill pixel.</param>
    /// <returns>The clear result.</returns>
    internal SvgPixelOperationResult ClearRectangle(SvgPixelRect rectangle, SvgPremultipliedRgba8 pixel)
    {
        SvgPixelBufferError validation = ValidateBuffer(this);
        if (validation != SvgPixelBufferError.None) return OperationFailure(validation);
        if (!ValidPixel(pixel)) return OperationFailure(SvgPixelBufferError.InvalidPremultipliedColor);
        if (!ValidRectangle(rectangle)) return OperationFailure(SvgPixelBufferError.InvalidArgument);
        if (!Contains(this, rectangle)) return OperationFailure(SvgPixelBufferError.BoundsExceeded);
        int y = rectangle.Y;
        while (y < rectangle.Bottom)
        {
            int index = Offset + y * Stride + rectangle.X * 4;
            int x = rectangle.X;
            while (x < rectangle.Right)
            {
                Store(Data, index, pixel);
                index = index + 4;
                x = x + 1;
            }
            y = y + 1;
        }
        return new SvgPixelOperationResult(true, SvgPixelBufferError.None, (long)rectangle.Width * rectangle.Height);
    }

    /// <summary>Copies the complete source buffer to one destination origin.</summary>
    /// <param name="source">The source buffer.</param>
    /// <param name="destinationX">The destination x coordinate.</param>
    /// <param name="destinationY">The destination y coordinate.</param>
    /// <returns>The overlap-safe raw copy result.</returns>
    internal SvgPixelOperationResult CopyFrom(SvgPixelBuffer? source, int destinationX, int destinationY)
    {
        if (source == null) return OperationFailure(SvgPixelBufferError.InvalidArgument);
        SvgPixelBufferError validation = ValidateBuffer(source);
        if (validation != SvgPixelBufferError.None) return OperationFailure(validation);
        return CopyRectangleFrom(source, new SvgPixelRect(0, 0, source.Width, source.Height), destinationX, destinationY);
    }

    /// <summary>Copies one source rectangle to a destination origin without color conversion.</summary>
    /// <param name="source">The source buffer.</param>
    /// <param name="sourceRectangle">The source-local rectangle.</param>
    /// <param name="destinationX">The destination x coordinate.</param>
    /// <param name="destinationY">The destination y coordinate.</param>
    /// <returns>The color-space-checked overlap-safe copy result.</returns>
    internal SvgPixelOperationResult CopyRectangleFrom(SvgPixelBuffer source, SvgPixelRect sourceRectangle, int destinationX, int destinationY)
    {
        SvgPixelBufferError destinationValidation = ValidateBuffer(this);
        SvgPixelBufferError sourceValidation = ValidateBuffer(source);
        if (destinationValidation != SvgPixelBufferError.None) return OperationFailure(destinationValidation);
        if (sourceValidation != SvgPixelBufferError.None) return OperationFailure(sourceValidation);
        if (!ValidRectangle(sourceRectangle)) return OperationFailure(SvgPixelBufferError.InvalidArgument);
        if (ColorSpace != source.ColorSpace) return OperationFailure(SvgPixelBufferError.ColorSpaceMismatch);
        if (!Contains(source, sourceRectangle)) return OperationFailure(SvgPixelBufferError.BoundsExceeded);
        long destinationRight = (long)destinationX + sourceRectangle.Width;
        long destinationBottom = (long)destinationY + sourceRectangle.Height;
        if (destinationX < 0 || destinationY < 0 || destinationRight > Width || destinationBottom > Height) return OperationFailure(SvgPixelBufferError.BoundsExceeded);
        if (!Premultiplied(source, sourceRectangle)) return OperationFailure(SvgPixelBufferError.InvalidPremultipliedColor);
        if (sourceRectangle.IsEmpty) return new SvgPixelOperationResult(true, SvgPixelBufferError.None, 0);
        int sourceStart = source.Offset + sourceRectangle.Y * source.Stride + sourceRectangle.X * 4;
        int destinationStart = Offset + destinationY * Stride + destinationX * 4;
        int rowBytes = sourceRectangle.Width * 4;
        bool reverse = source.Data == Data && (destinationStart > sourceStart || destinationStart == sourceStart && Stride > source.Stride);
        int row = reverse ? sourceRectangle.Height - 1 : 0;
        int end = reverse ? -1 : sourceRectangle.Height;
        int step = reverse ? -1 : 1;
        while (row != end)
        {
            int sourceIndex = sourceStart + row * source.Stride;
            int destinationIndex = destinationStart + row * Stride;
            System.Array.Copy(source.Data, sourceIndex, Data, destinationIndex, rowBytes);
            row = row + step;
        }
        return new SvgPixelOperationResult(true, SvgPixelBufferError.None, (long)sourceRectangle.Width * sourceRectangle.Height);
    }

    /// <summary>Creates a shared view over one in-bounds rectangle.</summary>
    /// <param name="rectangle">The view-local rectangle.</param>
    /// <returns>The shared subsurface or a stable failure.</returns>
    internal SvgPixelBufferResult Subsurface(SvgPixelRect rectangle)
    {
        SvgPixelBufferError validation = ValidateBuffer(this);
        if (validation != SvgPixelBufferError.None) return Failure(validation);
        if (!ValidRectangle(rectangle)) return Failure(SvgPixelBufferError.InvalidArgument);
        if (!Contains(this, rectangle)) return Failure(SvgPixelBufferError.BoundsExceeded);
        int offset = rectangle.IsEmpty ? Offset : Offset + rectangle.Y * Stride + rectangle.X * 4;
        SvgPixelBuffer buffer = new SvgPixelBuffer(Data, offset, rectangle.Width, rectangle.Height, Stride, ColorSpace, false);
        return new SvgPixelBufferResult(true, SvgPixelBufferError.None, buffer);
    }

    internal static SvgPixelBufferError ValidateDimensions(int width, int height, SvgPixelColorSpace colorSpace, SvgRasterLimits limits)
    {
        if (width < 0 || height < 0 || !Known(colorSpace) || !ValidLimits(limits)) return SvgPixelBufferError.InvalidArgument;
        long pixels = (long)width * height;
        if (width > limits.MaxWidth || height > limits.MaxHeight || pixels > limits.MaxSurfacePixels) return SvgPixelBufferError.DimensionLimitExceeded;
        return SvgPixelBufferError.None;
    }

    internal static SvgPixelBufferError ValidateBuffer(SvgPixelBuffer? buffer)
    {
        if (buffer == null || buffer.Data == null || buffer.Offset < 0 || buffer.Width < 0 || buffer.Height < 0 || buffer.Stride < 0 || !Known(buffer.ColorSpace)) return SvgPixelBufferError.InvalidArgument;
        long rowBytes = (long)buffer.Width * 4;
        if (rowBytes > int.MaxValue || buffer.RowBytes != rowBytes || buffer.Stride < rowBytes) return SvgPixelBufferError.InvalidArgument;
        long footprint = buffer.Width == 0 || buffer.Height == 0 ? 0 : (long)(buffer.Height - 1) * buffer.Stride + rowBytes;
        if (buffer.ByteLength != footprint || footprint > int.MaxValue) return SvgPixelBufferError.InvalidArgument;
        if ((long)buffer.Offset + footprint > buffer.Data.Length) return SvgPixelBufferError.BoundsExceeded;
        return SvgPixelBufferError.None;
    }

    internal static bool ValidLimits(SvgRasterLimits limits)
    {
        return limits != null && limits.MaxWidth > 0 && limits.MaxHeight > 0 && limits.MaxSurfacePixels > 0 && limits.MaxPixelBytes > 0;
    }

    internal static bool Known(SvgPixelColorSpace colorSpace)
    {
        return colorSpace == SvgPixelColorSpace.Srgb || colorSpace == SvgPixelColorSpace.SrgbLinear;
    }

    internal static bool Contains(SvgPixelBuffer buffer, SvgPixelRect rectangle)
    {
        return buffer != null && ValidRectangle(rectangle) && rectangle.X >= 0 && rectangle.Y >= 0 && rectangle.Right <= buffer.Width && rectangle.Bottom <= buffer.Height;
    }

    internal static bool ValidRectangle(SvgPixelRect? rectangle)
    {
        if (rectangle == null || rectangle.Width < 0 || rectangle.Height < 0) return false;
        long right = (long)rectangle.X + rectangle.Width;
        long bottom = (long)rectangle.Y + rectangle.Height;
        if (right < int.MinValue || right > int.MaxValue || bottom < int.MinValue || bottom > int.MaxValue) return false;
        return rectangle.Right == right && rectangle.Bottom == bottom && rectangle.IsEmpty == (rectangle.Width == 0 || rectangle.Height == 0);
    }

    internal static bool ValidPixel(SvgPremultipliedRgba8? pixel)
    {
        return pixel != null && pixel.Red <= pixel.Alpha && pixel.Green <= pixel.Alpha && pixel.Blue <= pixel.Alpha;
    }

    internal static bool Premultiplied(SvgPixelBuffer buffer)
    {
        return Premultiplied(buffer, new SvgPixelRect(0, 0, buffer.Width, buffer.Height));
    }

    internal static bool Premultiplied(SvgPixelBuffer buffer, SvgPixelRect rectangle)
    {
        int y = rectangle.Y;
        while (y < rectangle.Bottom)
        {
            int index = buffer.Offset + y * buffer.Stride + rectangle.X * 4;
            int x = rectangle.X;
            while (x < rectangle.Right)
            {
                byte alpha = buffer.Data[index + 3];
                if (buffer.Data[index] > alpha || buffer.Data[index + 1] > alpha || buffer.Data[index + 2] > alpha) return false;
                index = index + 4;
                x = x + 1;
            }
            y = y + 1;
        }
        return true;
    }

    internal static void Store(byte[] data, int index, SvgPremultipliedRgba8? pixel)
    {
        data[index] = (pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red;
        data[index + 1] = pixel.Green;
        data[index + 2] = pixel.Blue;
        data[index + 3] = pixel.Alpha;
    }

    internal static SvgPixelBufferResult Failure(SvgPixelBufferError error)
    {
        return new SvgPixelBufferResult(false, error, null);
    }

    internal static SvgPixelOperationResult OperationFailure(SvgPixelBufferError error)
    {
        return new SvgPixelOperationResult(false, error, 0);
    }

    internal static SvgPixelReadResult ReadFailure(SvgPixelBufferError error)
    {
        return new SvgPixelReadResult(false, error, null);
    }
}
