namespace svg;

internal class SvgImageDrawState
{
    SvgSceneNode node;
    SvgPixelBuffer? destination;
    SvgImageDrawOptions options;
    SvgPixelBuffer? source;
    SvgMatrix? pixelsToLocal;
    SvgMatrix? pixelsToSource;
    SvgImageSamplingMode mode;
    double opacity;

    internal SvgImageDrawState(SvgSceneNode imageNode, SvgPixelBuffer? target, SvgImageDrawOptions drawOptions)
    {
        node = imageNode;
        destination = target;
        options = drawOptions;
        source = null;
        pixelsToLocal = null;
        pixelsToSource = null;
        mode = SvgImageSamplingMode.Auto;
        opacity = 1;
    }

    internal SvgImageDrawResult Run()
    {
        SvgImageSampleError validation = Validate();
        if (validation != SvgImageSampleError.None) return Failure(validation);
        SvgPixelRect? bounds = Bounds();
        if (bounds == null) return Failure(SvgImageSampleError.NonFiniteResult);
        long work = (long)bounds.Width * bounds.Height;
        if (work > options.MaximumPixels) return Failure(SvgImageSampleError.WorkLimitExceeded, bounds, work);
        SvgImageDrawResult result = new SvgImageDrawResult();
        result.Bounds = bounds;
        result.PixelsTested = work;
        if (bounds.IsEmpty)
        {
            result.Success = true;
            return result;
        }
        int y = bounds.Y;
        while (y < bounds.Bottom)
        {
            int x = bounds.X;
            while (x < bounds.Right)
            {
                SvgPoint pixel = new SvgPoint(x + 0.5, y + 0.5);
                SvgPointTransformResult local = (pixelsToLocal ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(pixel);
                if (!local.Success) return Failure(SvgImageSampleError.NonFiniteResult, bounds, work);
                if (SvgImageSampleMath.Contains((node.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bounds, (local.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, local.Point.Y))
                {
                    SvgPointTransformResult mapped = (pixelsToSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(pixel);
                    if (!mapped.Success) return Failure(SvgImageSampleError.NonFiniteResult, bounds, work);
                    SvgImageSampleResult sampled = SvgImageSampleMath.Source(source, (mapped.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, mapped.Point.Y, mode, opacity);
                    if (!sampled.Success) return Failure(sampled.Error, bounds, work);
                    if (sampled.HasPixel)
                    {
                        Store(x, y, sampled.Pixel);
                        result.PixelsWritten = result.PixelsWritten + 1;
                    }
                }
                x = x + 1;
            }
            y = y + 1;
        }
        result.Success = true;
        return result;
    }

    SvgImageSampleError Validate()
    {
        if (node == null || node.Kind != SvgSceneNodeKind.Image || !node.HasValidTag() || destination == null || options == null) return SvgImageSampleError.InvalidArgument;
        if (!SvgImageSampleMath.ValidBuffer(destination) || !SvgImageSampleMath.ValidMode(options.SamplingMode) || !SvgFiniteMath.IsFinite(options.Opacity) || options.Opacity < 0 || options.Opacity > 1 || options.MaximumPixels < 1 || !SvgImageSampleMath.ValidClip(options.Clip, destination)) return SvgImageSampleError.InvalidArgument;
        if (!SvgFiniteMath.IsFinite(node.Opacity) || node.Opacity < 0 || node.Opacity > 1 || !SvgImageSampleMath.ValidImage(node.Image) || !SvgImageSampleMath.ValidMatrix(node.AbsoluteTransform)) return SvgImageSampleError.InvalidArgument;
        if ((node.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Resource == null || !(node.Image.Resource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).HasValidTag() || node.Image.Resource.Kind != SvgSceneResourceKind.Image || (node.Image.Resource.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DecodedPixels == null) return SvgImageSampleError.MissingDecodedPixels;
        source = node.Image.Resource.Image.DecodedPixels;
        if (!SvgImageSampleMath.ValidBuffer(source)) return SvgImageSampleError.InvalidArgument;
        if ((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ColorSpace != destination.ColorSpace) return SvgImageSampleError.ColorSpaceMismatch;
        SvgMatrixOperationResult localInverse = node.AbsoluteTransform.Invert();
        if (!localInverse.Success) return SvgImageSampleMath.MapMatrix(localInverse.Error);
        SvgMatrixOperationResult combined = node.AbsoluteTransform.Multiply(node.Image.ContentTransform);
        if (!combined.Success) return SvgImageSampleMath.MapMatrix(combined.Error);
        SvgMatrixOperationResult sourceInverse = (combined.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Invert();
        if (!sourceInverse.Success) return SvgImageSampleMath.MapMatrix(sourceInverse.Error);
        pixelsToLocal = localInverse.Matrix;
        pixelsToSource = sourceInverse.Matrix;
        mode = SvgImageSampleMath.Mode(options.SamplingMode, node.Image.Rendering);
        opacity = options.Opacity * node.Opacity;
        return SvgImageSampleError.None;
    }

    SvgPixelRect? Bounds()
    {
        SvgRectTransformResult transformed = node.AbsoluteTransform.TransformRect(((node.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!transformed.Success) return null;
        int left = SvgRasterMath.FloorClamp((transformed.Rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, 0, (destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width);
        int top = SvgRasterMath.FloorClamp(transformed.Rectangle.Y, 0, destination.Height);
        int right = SvgRasterMath.CeilingClamp(transformed.Rectangle.X + transformed.Rectangle.Width, 0, destination.Width);
        int bottom = SvgRasterMath.CeilingClamp(transformed.Rectangle.Y + transformed.Rectangle.Height, 0, destination.Height);
        if (options.Clip != null)
        {
            if (left < options.Clip.X) left = options.Clip.X;
            if (top < options.Clip.Y) top = options.Clip.Y;
            if (right > options.Clip.Right) right = options.Clip.Right;
            if (bottom > options.Clip.Bottom) bottom = options.Clip.Bottom;
        }
        if (right < left) right = left;
        if (bottom < top) bottom = top;
        return new SvgPixelRect(left, top, right - left, bottom - top);
    }

    void Store(int x, int y, SvgPremultipliedRgba8 pixel)
    {
        int index = (destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + y * destination.Stride + x * 4;
        destination.Data[index] = pixel.Red;
        destination.Data[index + 1] = pixel.Green;
        destination.Data[index + 2] = pixel.Blue;
        destination.Data[index + 3] = pixel.Alpha;
    }

    static SvgImageDrawResult Failure(SvgImageSampleError error)
    {
        return Failure(error, new SvgPixelRect(0, 0, 0, 0), 0);
    }

    static SvgImageDrawResult Failure(SvgImageSampleError error, SvgPixelRect bounds, long work)
    {
        SvgImageDrawResult result = new SvgImageDrawResult();
        result.Error = error;
        result.Bounds = bounds;
        result.PixelsTested = work;
        return result;
    }
}

internal static class SvgImageSampleMath
{
    internal static SvgImageSampleResult? Validate(SvgSceneNode? node, SvgPoint? point, SvgImageSampleOptions? options)
    {
        if (node == null || node.Kind != SvgSceneNodeKind.Image || !node.HasValidTag() || point == null || options == null) return Failure(SvgImageSampleError.InvalidArgument);
        if (!SvgFiniteMath.IsFinite(point.X) || !SvgFiniteMath.IsFinite(point.Y) || !ValidMode(options.SamplingMode) || !SvgFiniteMath.IsFinite(options.Opacity) || options.Opacity < 0 || options.Opacity > 1 || !ValidImage(node.Image)) return Failure(SvgImageSampleError.InvalidArgument);
        if ((node.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Resource == null || !(node.Image.Resource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).HasValidTag() || node.Image.Resource.Kind != SvgSceneResourceKind.Image || (node.Image.Resource.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DecodedPixels == null) return Failure(SvgImageSampleError.MissingDecodedPixels);
        if (!ValidBuffer(node.Image.Resource.Image.DecodedPixels)) return Failure(SvgImageSampleError.InvalidArgument);
        return null;
    }

    internal static SvgImageSampleResult Source(SvgPixelBuffer? source, double x, double y, SvgImageSamplingMode mode, double opacity)
    {
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return Failure(SvgImageSampleError.NonFiniteResult);
        if (x < 0 || y < 0 || x >= (source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width || y >= source.Height) return NoPixel((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ColorSpace);
        SvgPremultipliedRgba8 pixel;
        if (mode == SvgImageSamplingMode.Nearest)
        {
            SvgPixelReadResult read = source.Read((int)System.Math.Floor(x), (int)System.Math.Floor(y));
            if (!read.Success) return Failure(SvgImageSampleError.InvalidArgument);
            pixel = (read.Pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        else
        {
            SvgImageSampleResult interpolated = Bilinear(source, x, y);
            if (!interpolated.Success) return interpolated;
            pixel = interpolated.Pixel;
        }
        SvgImageSampleResult result = new SvgImageSampleResult();
        result.Success = true;
        result.HasPixel = true;
        result.Pixel = Scale(pixel, opacity);
        result.SourceX = x;
        result.SourceY = y;
        result.ColorSpace = source.ColorSpace;
        return result;
    }

    internal static SvgImageSamplingMode Mode(SvgImageSamplingMode requested, SvgImageRendering hint)
    {
        if (requested != SvgImageSamplingMode.Auto) return requested;
        if (hint == SvgImageRendering.CrispEdges || hint == SvgImageRendering.Pixelated || hint == SvgImageRendering.OptimizeSpeed) return SvgImageSamplingMode.Nearest;
        return SvgImageSamplingMode.Bilinear;
    }

    internal static bool ValidMode(SvgImageSamplingMode mode)
    {
        return mode == SvgImageSamplingMode.Auto || mode == SvgImageSamplingMode.Nearest || mode == SvgImageSamplingMode.Bilinear;
    }

    internal static bool ValidImage(SvgSceneImageData? image)
    {
        if (image == null || image.Bounds == null || image.ContentTransform == null || image.AspectRatio == null) return false;
        SvgRect bounds = image.Bounds;
        double right = bounds.X + bounds.Width;
        double bottom = bounds.Y + bounds.Height;
        if (!SvgFiniteMath.IsFinite(bounds.X) || !SvgFiniteMath.IsFinite(bounds.Y) || !SvgFiniteMath.IsFinite(bounds.Width) || !SvgFiniteMath.IsFinite(bounds.Height) || !SvgFiniteMath.IsFinite(right) || !SvgFiniteMath.IsFinite(bottom) || bounds.Width <= 0 || bounds.Height <= 0 || bounds.IsEmpty) return false;
        if (!ValidMatrix(image.ContentTransform) || (int)image.Rendering < 0 || (int)image.Rendering > 6) return false;
        return SvgAspectRatioNames.IsAlignment(image.AspectRatio.Alignment) && SvgAspectRatioNames.IsScale(image.AspectRatio.Scale);
    }

    internal static bool ValidBuffer(SvgPixelBuffer? buffer)
    {
        if (buffer == null || buffer.Data == null || buffer.Offset < 0 || buffer.Width <= 0 || buffer.Height <= 0 || buffer.Stride < 0) return false;
        if (buffer.ColorSpace != SvgPixelColorSpace.Srgb && buffer.ColorSpace != SvgPixelColorSpace.SrgbLinear) return false;
        long rowBytes = (long)buffer.Width * 4;
        if (rowBytes > int.MaxValue || buffer.RowBytes != rowBytes || buffer.Stride < rowBytes) return false;
        long footprint = (long)(buffer.Height - 1) * buffer.Stride + rowBytes;
        return buffer.ByteLength == footprint && footprint <= int.MaxValue && (long)buffer.Offset + footprint <= buffer.Data.Length;
    }

    internal static bool ValidClip(SvgPixelRect? clip, SvgPixelBuffer destination)
    {
        if (clip == null) return true;
        if (clip.X < 0 || clip.Y < 0 || clip.Width < 0 || clip.Height < 0) return false;
        long right = (long)clip.X + clip.Width;
        long bottom = (long)clip.Y + clip.Height;
        if (right != clip.Right || bottom != clip.Bottom || clip.IsEmpty != (clip.Width == 0 || clip.Height == 0)) return false;
        return right <= destination.Width && bottom <= destination.Height;
    }

    internal static bool Contains(SvgRect? rectangle, double x, double y)
    {
        return x >= (rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X && y >= rectangle.Y && x < rectangle.X + rectangle.Width && y < rectangle.Y + rectangle.Height;
    }

    internal static SvgImageSampleResult MatrixFailure(SvgGeometryError error)
    {
        return Failure(MapMatrix(error));
    }

    internal static SvgImageSampleError MapMatrix(SvgGeometryError error)
    {
        if (error == SvgGeometryError.SingularMatrix) return SvgImageSampleError.SingularTransform;
        if (error == SvgGeometryError.InvalidArgument) return SvgImageSampleError.InvalidArgument;
        return SvgImageSampleError.NonFiniteResult;
    }

    internal static SvgImageSampleResult Failure(SvgImageSampleError error)
    {
        SvgImageSampleResult result = new SvgImageSampleResult();
        result.Error = error;
        return result;
    }

    internal static SvgImageSampleResult NoPixel(SvgPixelColorSpace colorSpace)
    {
        SvgImageSampleResult result = new SvgImageSampleResult();
        result.Success = true;
        result.ColorSpace = colorSpace;
        return result;
    }

    static SvgImageSampleResult Bilinear(SvgPixelBuffer source, double x, double y)
    {
        double centeredX = x - 0.5;
        double centeredY = y - 0.5;
        int rawX = (int)System.Math.Floor(centeredX);
        int rawY = (int)System.Math.Floor(centeredY);
        double amountX = centeredX - rawX;
        double amountY = centeredY - rawY;
        int x0 = ClampIndex(rawX, source.Width);
        int y0 = ClampIndex(rawY, source.Height);
        int x1 = ClampIndex(rawX + 1, source.Width);
        int y1 = ClampIndex(rawY + 1, source.Height);
        SvgPixelReadResult topLeft = source.Read(x0, y0);
        SvgPixelReadResult topRight = source.Read(x1, y0);
        SvgPixelReadResult bottomLeft = source.Read(x0, y1);
        SvgPixelReadResult bottomRight = source.Read(x1, y1);
        if (!topLeft.Success || !topRight.Success || !bottomLeft.Success || !bottomRight.Success) return Failure(SvgImageSampleError.InvalidArgument);
        double topRed = Mix((topLeft.Pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red, (topRight.Pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red, amountX);
        double topGreen = Mix(topLeft.Pixel.Green, topRight.Pixel.Green, amountX);
        double topBlue = Mix(topLeft.Pixel.Blue, topRight.Pixel.Blue, amountX);
        double topAlpha = Mix(topLeft.Pixel.Alpha, topRight.Pixel.Alpha, amountX);
        double bottomRed = Mix((bottomLeft.Pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red, (bottomRight.Pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red, amountX);
        double bottomGreen = Mix(bottomLeft.Pixel.Green, bottomRight.Pixel.Green, amountX);
        double bottomBlue = Mix(bottomLeft.Pixel.Blue, bottomRight.Pixel.Blue, amountX);
        double bottomAlpha = Mix(bottomLeft.Pixel.Alpha, bottomRight.Pixel.Alpha, amountX);
        SvgImageSampleResult result = new SvgImageSampleResult();
        result.Success = true;
        result.HasPixel = true;
        result.Pixel = new SvgPremultipliedRgba8(Byte(Mix(topRed, bottomRed, amountY)), Byte(Mix(topGreen, bottomGreen, amountY)), Byte(Mix(topBlue, bottomBlue, amountY)), Byte(Mix(topAlpha, bottomAlpha, amountY)));
        result.ColorSpace = source.ColorSpace;
        return result;
    }

    static SvgPremultipliedRgba8 Scale(SvgPremultipliedRgba8? pixel, double opacity)
    {
        return new SvgPremultipliedRgba8(Byte((pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red * opacity), Byte(pixel.Green * opacity), Byte(pixel.Blue * opacity), Byte(pixel.Alpha * opacity));
    }

    static int ClampIndex(int value, int length)
    {
        if (value < 0) return 0;
        if (value >= length) return length - 1;
        return value;
    }

    static double Mix(double first, double second, double amount)
    {
        return first + (second - first) * amount;
    }

    static byte Byte(double value)
    {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (byte)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    internal static bool ValidMatrix(SvgMatrix matrix)
    {
        return matrix != null && SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }
}
