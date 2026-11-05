namespace svg;

internal class SvgCompositeState
{
    SvgPixelBuffer? source;
    SvgPixelBuffer? destination;
    SvgCompositeOptions options;
    SvgPixelRect? sourceRectangle;
    SvgPixelRect? destinationBounds;
    SvgPixelRect? clippedSource;
    SvgPixelBuffer? effectiveSource;
    bool snapshot;

    internal SvgCompositeState(SvgPixelBuffer? sourceBuffer, SvgPixelBuffer? destinationBuffer, SvgCompositeOptions compositeOptions)
    {
        source = sourceBuffer;
        destination = destinationBuffer;
        options = compositeOptions;
        sourceRectangle = null;
        destinationBounds = null;
        clippedSource = null;
        effectiveSource = null;
        snapshot = false;
    }

    internal SvgCompositeResult Run()
    {
        SvgCompositeError validation = Validate();
        if (validation != SvgCompositeError.None) return Failure(validation);
        if (!Clip()) return Failure(SvgCompositeError.InvalidArgument);
        long work = (long)(destinationBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width * destinationBounds.Height;
        if (work > options.MaximumPixels) return FailureWithWork(SvgCompositeError.WorkLimitExceeded, work);
        if (!SvgCompositeMath.Premultiplied(source, clippedSource) || !SvgCompositeMath.Premultiplied(destination, destinationBounds)) return FailureWithWork(SvgCompositeError.InvalidPremultipliedPixel, work);
        SvgCompositeError prepared = PrepareSource();
        if (prepared != SvgCompositeError.None) return FailureWithWork(prepared, work);
        int y = 0;
        while (y < destinationBounds.Height)
        {
            int x = 0;
            while (x < destinationBounds.Width)
            {
                int sourceIndex = (effectiveSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + ((clippedSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Y + y) * effectiveSource.Stride + (clippedSource.X + x) * 4;
                int destinationIndex = (destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + (destinationBounds.Y + y) * destination.Stride + (destinationBounds.X + x) * 4;
                SvgPorterDuffMath.Composite(effectiveSource.Data, sourceIndex, effectiveSource.ColorSpace, destination.Data, destinationIndex, destination.ColorSpace, options.Operator, options.Opacity, options.BlendMode, options.BlendColorSpace, options.Isolated);
                x = x + 1;
            }
            y = y + 1;
        }
        SvgCompositeResult result = new SvgCompositeResult();
        result.Success = true;
        result.Bounds = destinationBounds;
        result.PixelsProcessed = work;
        result.UsedSnapshot = snapshot;
        return result;
    }

    SvgCompositeError Validate()
    {
        if (!SvgCompositeMath.ValidBuffer(source) || !SvgCompositeMath.ValidBuffer(destination) || options == null) return SvgCompositeError.InvalidArgument;
        if ((int)options.Operator < 0 || (int)options.Operator > 12 || !SvgBlendModeMath.ValidMode(options.BlendMode) || !SvgBlendModeMath.ValidSpace(options.BlendColorSpace) || options.Operator != SvgCompositeOperator.SourceOver && options.BlendMode != SvgBlendMode.Normal || !SvgFiniteMath.IsFinite(options.Opacity) || options.Opacity < 0 || options.Opacity > 1 || options.MaximumPixels < 1 || !SvgCompositeMath.ValidLimits(options.RasterLimits)) return SvgCompositeError.InvalidArgument;
        sourceRectangle = options.SourceRectangle == null ? new SvgPixelRect(0, 0, (source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width, source.Height) : options.SourceRectangle;
        if (!SvgCompositeMath.ValidRectangle(sourceRectangle) || !SvgCompositeMath.Contains(source, sourceRectangle) || !SvgCompositeMath.ValidClip(options.Clip, (destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")))) return SvgCompositeError.InvalidArgument;
        return SvgCompositeError.None;
    }

    bool Clip()
    {
        long rawRight = (long)options.DestinationX + (sourceRectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width;
        long rawBottom = (long)options.DestinationY + sourceRectangle.Height;
        long left = options.DestinationX;
        long top = options.DestinationY;
        long right = rawRight;
        long bottom = rawBottom;
        if (left < 0) left = 0;
        if (top < 0) top = 0;
        if (left > (destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width) left = destination.Width;
        if (top > destination.Height) top = destination.Height;
        if (right < 0) right = 0;
        if (bottom < 0) bottom = 0;
        if (right > destination.Width) right = destination.Width;
        if (bottom > destination.Height) bottom = destination.Height;
        if (options.Clip != null)
        {
            if (left < options.Clip.X) left = options.Clip.X;
            if (top < options.Clip.Y) top = options.Clip.Y;
            if (right > options.Clip.Right) right = options.Clip.Right;
            if (bottom > options.Clip.Bottom) bottom = options.Clip.Bottom;
        }
        if (right < left) right = left;
        if (bottom < top) bottom = top;
        if (left < 0 || top < 0 || right > int.MaxValue || bottom > int.MaxValue) return false;
        destinationBounds = new SvgPixelRect((int)left, (int)top, (int)(right - left), (int)(bottom - top));
        if (destinationBounds.IsEmpty)
        {
            clippedSource = new SvgPixelRect(sourceRectangle.X, sourceRectangle.Y, 0, 0);
            return true;
        }
        long sourceX = (long)sourceRectangle.X + left - options.DestinationX;
        long sourceY = (long)sourceRectangle.Y + top - options.DestinationY;
        if (sourceX < 0 || sourceY < 0 || sourceX > int.MaxValue || sourceY > int.MaxValue) return false;
        clippedSource = new SvgPixelRect((int)sourceX, (int)sourceY, destinationBounds.Width, destinationBounds.Height);
        return SvgCompositeMath.Contains(source, clippedSource);
    }

    SvgCompositeError PrepareSource()
    {
        effectiveSource = source;
        if ((destinationBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsEmpty || !SvgCompositeMath.RegionsMayOverlap(source, clippedSource, (destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), destinationBounds)) return SvgCompositeError.None;
        SvgPixelBufferResult created = SvgPixelBuffer.Create((clippedSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width, clippedSource.Height, (source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ColorSpace, options.RasterLimits);
        if (!created.Success) return SvgCompositeMath.MapAllocation(created.Error);
        SvgPixelOperationResult copied = (created.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CopyRectangleFrom(source, clippedSource, 0, 0);
        if (!copied.Success) return SvgCompositeError.BufferOperationFailed;
        effectiveSource = created.Buffer;
        clippedSource = new SvgPixelRect(0, 0, clippedSource.Width, clippedSource.Height);
        snapshot = true;
        return SvgCompositeError.None;
    }

    SvgCompositeResult Failure(SvgCompositeError error)
    {
        return FailureWithWork(error, 0);
    }

    SvgCompositeResult FailureWithWork(SvgCompositeError error, long work)
    {
        SvgCompositeResult result = new SvgCompositeResult();
        result.Error = error;
        if (destinationBounds != null) result.Bounds = destinationBounds;
        result.PixelsProcessed = work;
        return result;
    }
}

internal static class SvgPorterDuffMath
{
    internal static void Composite(byte[] source, int sourceIndex, SvgPixelColorSpace sourceSpace, byte[] destination, int destinationIndex, SvgPixelColorSpace destinationSpace, SvgCompositeOperator operation, double opacity, SvgBlendMode blendMode, SvgBlendColorSpace blendColorSpace, bool isolated)
    {
        SvgPixelColorSpace workingSpace = destinationSpace;
        if (blendColorSpace == SvgBlendColorSpace.Srgb) workingSpace = SvgPixelColorSpace.Srgb;
        if (blendColorSpace == SvgBlendColorSpace.SrgbLinear) workingSpace = SvgPixelColorSpace.SrgbLinear;
        if (blendMode != SvgBlendMode.Normal || sourceSpace != destinationSpace || workingSpace != destinationSpace)
        {
            SvgBlendModeMath.Composite(source, sourceIndex, sourceSpace, destination, destinationIndex, destinationSpace, operation, opacity, blendMode, blendColorSpace, isolated);
            return;
        }
        int sourceAlpha = Scale(source[sourceIndex + 3], opacity);
        int sourceRed = Scale(source[sourceIndex], opacity);
        int sourceGreen = Scale(source[sourceIndex + 1], opacity);
        int sourceBlue = Scale(source[sourceIndex + 2], opacity);
        int destinationAlpha = destination[destinationIndex + 3];
        int sourceFactor = SourceFactor(operation, sourceAlpha, destinationAlpha);
        int destinationFactor = DestinationFactor(operation, sourceAlpha, destinationAlpha);
        destination[destinationIndex] = Channel(sourceRed, destination[destinationIndex], sourceFactor, destinationFactor);
        destination[destinationIndex + 1] = Channel(sourceGreen, destination[destinationIndex + 1], sourceFactor, destinationFactor);
        destination[destinationIndex + 2] = Channel(sourceBlue, destination[destinationIndex + 2], sourceFactor, destinationFactor);
        destination[destinationIndex + 3] = Channel(sourceAlpha, destinationAlpha, sourceFactor, destinationFactor);
    }

    static int SourceFactor(SvgCompositeOperator operation, int sourceAlpha, int destinationAlpha)
    {
        if (operation == SvgCompositeOperator.Clear || operation == SvgCompositeOperator.Destination || operation == SvgCompositeOperator.DestinationIn || operation == SvgCompositeOperator.DestinationOut) return 0;
        if (operation == SvgCompositeOperator.SourceIn || operation == SvgCompositeOperator.SourceAtop) return destinationAlpha;
        if (operation == SvgCompositeOperator.DestinationOver || operation == SvgCompositeOperator.SourceOut || operation == SvgCompositeOperator.DestinationAtop || operation == SvgCompositeOperator.Xor) return 255 - destinationAlpha;
        return 255;
    }

    static int DestinationFactor(SvgCompositeOperator operation, int sourceAlpha, int destinationAlpha)
    {
        if (operation == SvgCompositeOperator.Clear || operation == SvgCompositeOperator.Copy || operation == SvgCompositeOperator.SourceIn || operation == SvgCompositeOperator.SourceOut) return 0;
        if (operation == SvgCompositeOperator.DestinationIn || operation == SvgCompositeOperator.DestinationAtop) return sourceAlpha;
        if (operation == SvgCompositeOperator.SourceOver || operation == SvgCompositeOperator.SourceAtop || operation == SvgCompositeOperator.DestinationOut || operation == SvgCompositeOperator.Xor) return 255 - sourceAlpha;
        return 255;
    }

    static byte Channel(int source, int destination, int sourceFactor, int destinationFactor)
    {
        int value = Multiply(source, sourceFactor) + Multiply(destination, destinationFactor);
        return value > 255 ? (byte)255 : (byte)value;
    }

    static int Multiply(int value, int factor)
    {
        return (value * factor + 127) / 255;
    }

    static int Scale(int value, double opacity)
    {
        double scaled = value * opacity;
        if (scaled <= 0) return 0;
        if (scaled >= 255) return 255;
        return (int)System.Math.Round(scaled, System.MidpointRounding.AwayFromZero);
    }
}

internal static class SvgCompositeMath
{
    internal static bool ValidBuffer(SvgPixelBuffer? buffer)
    {
        if (buffer == null || buffer.Data == null || buffer.Offset < 0 || buffer.Width < 0 || buffer.Height < 0 || buffer.Stride < 0 || !Known(buffer.ColorSpace)) return false;
        long rowBytes = (long)buffer.Width * 4;
        if (rowBytes > int.MaxValue || buffer.RowBytes != rowBytes || buffer.Stride < rowBytes) return false;
        long footprint = buffer.Width == 0 || buffer.Height == 0 ? 0 : (long)(buffer.Height - 1) * buffer.Stride + rowBytes;
        return buffer.ByteLength == footprint && footprint <= int.MaxValue && (long)buffer.Offset + footprint <= buffer.Data.Length;
    }

    internal static bool ValidRectangle(SvgPixelRect? rectangle)
    {
        if (rectangle == null || rectangle.Width < 0 || rectangle.Height < 0) return false;
        long right = (long)rectangle.X + rectangle.Width;
        long bottom = (long)rectangle.Y + rectangle.Height;
        if (right < int.MinValue || right > int.MaxValue || bottom < int.MinValue || bottom > int.MaxValue) return false;
        return rectangle.Right == right && rectangle.Bottom == bottom && rectangle.IsEmpty == (rectangle.Width == 0 || rectangle.Height == 0);
    }

    internal static bool Contains(SvgPixelBuffer? buffer, SvgPixelRect rectangle)
    {
        return ValidRectangle(rectangle) && rectangle.X >= 0 && rectangle.Y >= 0 && rectangle.Right <= (buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width && rectangle.Bottom <= buffer.Height;
    }

    internal static bool ValidClip(SvgPixelRect? clip, SvgPixelBuffer buffer)
    {
        return clip == null || Contains(buffer, clip);
    }

    internal static bool ValidLimits(SvgRasterLimits limits)
    {
        return limits != null && limits.MaxWidth > 0 && limits.MaxHeight > 0 && limits.MaxSurfacePixels > 0 && limits.MaxPixelBytes > 0;
    }

    internal static bool Known(SvgPixelColorSpace colorSpace)
    {
        return colorSpace == SvgPixelColorSpace.Srgb || colorSpace == SvgPixelColorSpace.SrgbLinear;
    }

    internal static bool Premultiplied(SvgPixelBuffer? buffer, SvgPixelRect? rectangle)
    {
        int y = (rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Y;
        while (y < rectangle.Bottom)
        {
            int index = (buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + y * buffer.Stride + rectangle.X * 4;
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

    internal static bool RegionsMayOverlap(SvgPixelBuffer? first, SvgPixelRect? firstRectangle, SvgPixelBuffer second, SvgPixelRect secondRectangle)
    {
        if (!object.ReferenceEquals((first ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Data, second.Data) || (firstRectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsEmpty || secondRectangle.IsEmpty) return false;
        long firstStart = first.Offset + (long)firstRectangle.Y * first.Stride + (long)firstRectangle.X * 4;
        long firstEnd = first.Offset + (long)(firstRectangle.Bottom - 1) * first.Stride + (long)firstRectangle.Right * 4;
        long secondStart = second.Offset + (long)secondRectangle.Y * second.Stride + (long)secondRectangle.X * 4;
        long secondEnd = second.Offset + (long)(secondRectangle.Bottom - 1) * second.Stride + (long)secondRectangle.Right * 4;
        return firstStart < secondEnd && secondStart < firstEnd;
    }

    internal static bool ValidBounds(SvgRect? bounds)
    {
        if (bounds == null || !SvgFiniteMath.IsFinite(bounds.X) || !SvgFiniteMath.IsFinite(bounds.Y) || !SvgFiniteMath.IsFinite(bounds.Width) || !SvgFiniteMath.IsFinite(bounds.Height) || bounds.Width < 0 || bounds.Height < 0) return false;
        return SvgFiniteMath.IsFinite(bounds.X + bounds.Width) && SvgFiniteMath.IsFinite(bounds.Y + bounds.Height);
    }

    internal static SvgPixelRect? PixelBounds(SvgRect? bounds)
    {
        if (!ValidBounds(bounds)) return null;
        double leftValue = System.Math.Floor((bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X);
        double topValue = System.Math.Floor(bounds.Y);
        double rightValue = System.Math.Ceiling(bounds.X + bounds.Width);
        double bottomValue = System.Math.Ceiling(bounds.Y + bounds.Height);
        if (leftValue < int.MinValue || leftValue > int.MaxValue || topValue < int.MinValue || topValue > int.MaxValue || rightValue < int.MinValue || rightValue > int.MaxValue || bottomValue < int.MinValue || bottomValue > int.MaxValue) return null;
        long width = (long)rightValue - (long)leftValue;
        long height = (long)bottomValue - (long)topValue;
        if (width < 0 || height < 0 || width > int.MaxValue || height > int.MaxValue) return null;
        if (bounds.IsEmpty) return new SvgPixelRect((int)leftValue, (int)topValue, 0, 0);
        return new SvgPixelRect((int)leftValue, (int)topValue, (int)width, (int)height);
    }

    internal static SvgRasterLimits CopyLimits(SvgRasterLimits source)
    {
        SvgRasterLimits copy = new SvgRasterLimits();
        copy.MaxWidth = source.MaxWidth;
        copy.MaxHeight = source.MaxHeight;
        copy.MaxSurfacePixels = source.MaxSurfacePixels;
        copy.MaxPixelBytes = source.MaxPixelBytes;
        copy.MaxPngChunks = source.MaxPngChunks;
        copy.MaxPngDecodedBytes = source.MaxPngDecodedBytes;
        return copy;
    }

    internal static SvgCompositeError MapAllocation(SvgPixelBufferError error)
    {
        if (error == SvgPixelBufferError.DimensionLimitExceeded || error == SvgPixelBufferError.ByteLimitExceeded) return SvgCompositeError.AllocationLimitExceeded;
        return SvgCompositeError.InvalidArgument;
    }
}
