namespace svg;

internal class SvgRasterEdge
{
    internal double StartX;
    internal double StartY;
    internal double EndX;
    internal double EndY;

    internal SvgRasterEdge(SvgPoint? start, SvgPoint? end)
    {
        StartX = (start ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        StartY = start.Y;
        EndX = (end ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        EndY = end.Y;
    }
}

internal class SvgRasterState
{
    SvgPathData? path;
    SvgPixelBuffer surface;
    SvgPremultipliedRgba8 color;
    SvgFillRule fillRule;
    SvgRasterOptions? options;
    System.Collections.Generic.List<SvgRasterEdge> edges;
    SvgPoint? subpathStart;
    SvgPoint? subpathEnd;
    bool hasSubpath;
    bool hasDrawable;
    double minimumX;
    double minimumY;
    double maximumX;
    double maximumY;

    internal SvgRasterState(SvgPathData? normalizedPath, SvgPixelBuffer destination, SvgPremultipliedRgba8 fillColor, SvgFillRule rule, SvgRasterOptions? rasterOptions)
    {
        path = normalizedPath;
        surface = destination;
        color = fillColor;
        fillRule = rule;
        options = rasterOptions;
        edges = new System.Collections.Generic.List<SvgRasterEdge>();
        subpathStart = null;
        subpathEnd = null;
        hasSubpath = false;
        hasDrawable = false;
        minimumX = double.PositiveInfinity;
        minimumY = double.PositiveInfinity;
        maximumX = double.NegativeInfinity;
        maximumY = double.NegativeInfinity;
    }

    internal SvgRasterResult Run()
    {
        int commandIndex = 0;
        while (commandIndex < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            SvgRasterError error = AddCommand(path.Commands[commandIndex]);
            if (error != SvgRasterError.None) return SvgRasterMath.Failure(error);
            commandIndex = commandIndex + 1;
        }
        SvgRasterError closingError = CloseOpenSubpath();
        if (closingError != SvgRasterError.None) return SvgRasterMath.Failure(closingError);
        if (edges.Count == 0) return SvgRasterMath.EmptySuccess(0);
        SvgPixelRect bounds = Bounds();
        long work = Work(bounds);
        if (work < 0 || work > (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumWork) return SvgRasterMath.Failure(SvgRasterError.WorkLimitExceeded, bounds, edges.Count, work < 0 ? 0 : work);
        long written = Rasterize(bounds);
        return new SvgRasterResult(true, SvgRasterError.None, bounds, edges.Count, work, written);
    }

    SvgRasterError AddCommand(SvgPathCommand command)
    {
        if (command.Kind == SvgPathCommandKind.MoveTo)
        {
            SvgRasterError closingError = CloseOpenSubpath();
            if (closingError != SvgRasterError.None) return closingError;
            SvgPointTransformResult move = Transform(new SvgPoint(command.EndX, command.EndY));
            if (!move.Success) return SvgRasterMath.Map(move.Error);
            subpathStart = move.Point;
            subpathEnd = move.Point;
            hasSubpath = true;
            hasDrawable = false;
            return SvgRasterError.None;
        }
        SvgFlattenResult flattened = Flatten(command);
        if (!flattened.Success) return SvgRasterMath.Map(flattened.Error);
        SvgPointTransformResult first = Transform((flattened.Points ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[0].Point);
        if (!first.Success) return SvgRasterMath.Map(first.Error);
        int pointIndex = 1;
        while (pointIndex < flattened.Points.Count)
        {
            SvgPointTransformResult next = Transform(flattened.Points[pointIndex].Point);
            if (!next.Success) return SvgRasterMath.Map(next.Error);
            SvgRasterError edgeError = AddEdge(first.Point, next.Point);
            if (edgeError != SvgRasterError.None) return edgeError;
            first = next;
            pointIndex = pointIndex + 1;
        }
        subpathEnd = first.Point;
        hasDrawable = true;
        return SvgRasterError.None;
    }

    SvgFlattenResult Flatten(SvgPathCommand command)
    {
        SvgFlattenOptions flattenOptions = new SvgFlattenOptions();
        flattenOptions.Tolerance = (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Tolerance;
        flattenOptions.DeviceTransform = options.Transform;
        flattenOptions.MaximumSubdivisionDepth = options.MaximumSubdivisionDepth;
        int remaining = options.MaximumEdges - edges.Count;
        flattenOptions.MaximumSegments = remaining > 0 ? remaining : 1;
        SvgPoint start = new SvgPoint(command.StartX, command.StartY);
        SvgPoint end = new SvgPoint(command.EndX, command.EndY);
        if (command.Kind == SvgPathCommandKind.LineTo || command.Kind == SvgPathCommandKind.ClosePath) return new SvgLineSegment(start, end).Flatten(flattenOptions);
        if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) return new SvgQuadraticBezier(start, new SvgPoint(command.Values[0], command.Values[1]), end).Flatten(flattenOptions);
        if (command.Kind == SvgPathCommandKind.CubicCurveTo) return new SvgCubicBezier(start, new SvgPoint(command.Values[0], command.Values[1]), new SvgPoint(command.Values[2], command.Values[3]), end).Flatten(flattenOptions);
        if (command.Kind == SvgPathCommandKind.ArcTo)
        {
            SvgArcOperationResult arc = new SvgArcGeometry().FromCommand(command);
            if (!arc.Success) return new SvgFlattenResult(false, SvgFlattenError.NonFiniteResult, null, 0);
            return (arc.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Flatten(flattenOptions);
        }
        return new SvgFlattenResult(false, SvgFlattenError.InvalidArgument, null, 0);
    }

    SvgRasterError CloseOpenSubpath()
    {
        if (!hasSubpath || !hasDrawable) return SvgRasterError.None;
        hasDrawable = false;
        if ((subpathEnd ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X == (subpathStart ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X && subpathEnd.Y == subpathStart.Y) return SvgRasterError.None;
        SvgRasterError error = AddEdge(subpathEnd, subpathStart);
        if (error == SvgRasterError.None) subpathEnd = subpathStart;
        return error;
    }

    SvgRasterError AddEdge(SvgPoint? start, SvgPoint? end)
    {
        if (edges.Count >= (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumEdges) return SvgRasterError.EdgeLimitExceeded;
        edges.Add(new SvgRasterEdge(start, end));
        minimumX = System.Math.Min(minimumX, System.Math.Min((start ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (end ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X));
        minimumY = System.Math.Min(minimumY, System.Math.Min(start.Y, end.Y));
        maximumX = System.Math.Max(maximumX, System.Math.Max(start.X, end.X));
        maximumY = System.Math.Max(maximumY, System.Math.Max(start.Y, end.Y));
        return SvgRasterError.None;
    }

    SvgPointTransformResult Transform(SvgPoint point)
    {
        if ((options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform == null) return new SvgPointTransformResult(true, SvgGeometryError.None, point);
        return (options.Transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(point);
    }

    SvgPixelRect Bounds()
    {
        int left = SvgRasterMath.FloorClamp(minimumX, 0, surface.Width);
        int top = SvgRasterMath.FloorClamp(minimumY, 0, surface.Height);
        int right = SvgRasterMath.CeilingClamp(maximumX, 0, surface.Width);
        int bottom = SvgRasterMath.CeilingClamp(maximumY, 0, surface.Height);
        if ((options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Clip != null)
        {
            left = System.Math.Max(left, (options.Clip ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X);
            top = System.Math.Max(top, options.Clip.Y);
            right = System.Math.Min(right, options.Clip.Right);
            bottom = System.Math.Min(bottom, options.Clip.Bottom);
        }
        if (right < left) right = left;
        if (bottom < top) bottom = top;
        return new SvgPixelRect(left, top, right - left, bottom - top);
    }

    long Work(SvgPixelRect bounds)
    {
        long samples = (long)(options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SamplesPerAxis * options.SamplesPerAxis;
        long pixels = (long)bounds.Width * bounds.Height;
        if (pixels != 0 && samples > long.MaxValue / pixels) return -1;
        long pixelSamples = pixels * samples;
        if (pixelSamples != 0 && edges.Count > long.MaxValue / pixelSamples) return -1;
        return pixelSamples * edges.Count;
    }

    long Rasterize(SvgPixelRect bounds)
    {
        long written = 0;
        int totalSamples = (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SamplesPerAxis * options.SamplesPerAxis;
        int y = bounds.Y;
        while (y < bounds.Bottom)
        {
            int x = bounds.X;
            while (x < bounds.Right)
            {
                int covered = Coverage(x, y);
                if (covered != 0)
                {
                    int index = surface.Offset + y * surface.Stride + x * 4;
                    surface.Data[index] = Scale(color.Red, covered, totalSamples);
                    surface.Data[index + 1] = Scale(color.Green, covered, totalSamples);
                    surface.Data[index + 2] = Scale(color.Blue, covered, totalSamples);
                    surface.Data[index + 3] = Scale(color.Alpha, covered, totalSamples);
                    written = written + 1;
                }
                x = x + 1;
            }
            y = y + 1;
        }
        return written;
    }

    int Coverage(int pixelX, int pixelY)
    {
        int covered = 0;
        int sampleY = 0;
        while (sampleY < (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SamplesPerAxis)
        {
            double y = pixelY + (sampleY + 0.5) / options.SamplesPerAxis;
            int sampleX = 0;
            while (sampleX < options.SamplesPerAxis)
            {
                double x = pixelX + (sampleX + 0.5) / options.SamplesPerAxis;
                if (Inside(x, y)) covered = covered + 1;
                sampleX = sampleX + 1;
            }
            sampleY = sampleY + 1;
        }
        return covered;
    }

    bool Inside(double x, double y)
    {
        int winding = 0;
        int crossings = 0;
        int edgeIndex = 0;
        while (edgeIndex < edges.Count)
        {
            SvgRasterEdge edge = edges[edgeIndex];
            if (edge.StartY <= y && edge.EndY > y)
            {
                double intersection = edge.StartX + (y - edge.StartY) * (edge.EndX - edge.StartX) / (edge.EndY - edge.StartY);
                if (intersection > x)
                {
                    winding = winding + 1;
                    crossings = crossings + 1;
                }
            }
            else if (edge.EndY <= y && edge.StartY > y)
            {
                double intersection = edge.EndX + (y - edge.EndY) * (edge.StartX - edge.EndX) / (edge.StartY - edge.EndY);
                if (intersection > x)
                {
                    winding = winding - 1;
                    crossings = crossings + 1;
                }
            }
            edgeIndex = edgeIndex + 1;
        }
        return fillRule == SvgFillRule.Nonzero ? winding != 0 : crossings % 2 != 0;
    }

    static byte Scale(byte channel, int covered, int total)
    {
        return (byte)((channel * covered + total / 2) / total);
    }
}

internal static class SvgRasterMath
{
    internal static bool Valid(SvgPathData? path, SvgPixelBuffer surface, SvgPremultipliedRgba8 color, SvgFillRule fillRule, SvgRasterOptions? options)
    {
        if (path == null || !ValidSurface(surface) || !ValidColor(color) || options == null) return false;
        if (fillRule != SvgFillRule.Nonzero && fillRule != SvgFillRule.EvenOdd) return false;
        if (!SvgFiniteMath.IsFinite(options.Tolerance) || options.Tolerance <= 0) return false;
        if (options.SamplesPerAxis < 1 || options.SamplesPerAxis > 16 || options.MaximumSubdivisionDepth < 0 || options.MaximumEdges < 1 || options.MaximumWork < 1) return false;
        if (!ValidTransform(options.Transform) || !ValidClip(options.Clip, surface)) return false;
        return true;
    }

    internal static SvgRasterResult Failure(SvgRasterError error)
    {
        return Failure(error, new SvgPixelRect(0, 0, 0, 0), 0, 0);
    }

    internal static SvgRasterResult Failure(SvgRasterError error, SvgPixelRect bounds, int edgeCount, long workUnits)
    {
        return new SvgRasterResult(false, error, bounds, edgeCount, workUnits, 0);
    }

    internal static SvgRasterResult EmptySuccess(int edgeCount)
    {
        return new SvgRasterResult(true, SvgRasterError.None, new SvgPixelRect(0, 0, 0, 0), edgeCount, 0, 0);
    }

    internal static SvgRasterError Map(SvgPathNormalizeError error)
    {
        if (error == SvgPathNormalizeError.InvalidPath) return SvgRasterError.InvalidPath;
        if (error == SvgPathNormalizeError.NonFiniteResult) return SvgRasterError.NonFiniteResult;
        if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgRasterError.EdgeLimitExceeded;
        return SvgRasterError.InvalidArgument;
    }

    internal static SvgRasterError Map(SvgFlattenError error)
    {
        if (error == SvgFlattenError.SegmentLimitExceeded) return SvgRasterError.EdgeLimitExceeded;
        if (error == SvgFlattenError.SubdivisionLimitExceeded) return SvgRasterError.SubdivisionLimitExceeded;
        if (error == SvgFlattenError.InvalidArgument) return SvgRasterError.InvalidArgument;
        return SvgRasterError.NonFiniteResult;
    }

    internal static SvgRasterError Map(SvgGeometryError error)
    {
        return error == SvgGeometryError.InvalidArgument ? SvgRasterError.InvalidArgument : SvgRasterError.NonFiniteResult;
    }

    internal static int FloorClamp(double value, int minimum, int maximum)
    {
        if (value <= minimum) return minimum;
        if (value >= maximum) return maximum;
        return (int)System.Math.Floor(value);
    }

    internal static int CeilingClamp(double value, int minimum, int maximum)
    {
        if (value <= minimum) return minimum;
        if (value >= maximum) return maximum;
        return (int)System.Math.Ceiling(value);
    }

    static bool ValidSurface(SvgPixelBuffer? surface)
    {
        if (surface == null || surface.Data == null || surface.Offset < 0 || surface.Width < 0 || surface.Height < 0 || surface.Stride < 0) return false;
        if (surface.ColorSpace != SvgPixelColorSpace.Srgb && surface.ColorSpace != SvgPixelColorSpace.SrgbLinear) return false;
        long rowBytes = (long)surface.Width * 4;
        if (rowBytes > int.MaxValue || surface.RowBytes != rowBytes || surface.Stride < rowBytes) return false;
        long footprint = surface.Width == 0 || surface.Height == 0 ? 0 : (long)(surface.Height - 1) * surface.Stride + rowBytes;
        if (surface.ByteLength != footprint || footprint > int.MaxValue) return false;
        return (long)surface.Offset + footprint <= surface.Data.Length;
    }

    static bool ValidColor(SvgPremultipliedRgba8 color)
    {
        return color != null && color.Red <= color.Alpha && color.Green <= color.Alpha && color.Blue <= color.Alpha;
    }

    static bool ValidTransform(SvgMatrix? transform)
    {
        if (transform == null) return true;
        return SvgFiniteMath.IsFinite(transform.A) && SvgFiniteMath.IsFinite(transform.B) && SvgFiniteMath.IsFinite(transform.C) && SvgFiniteMath.IsFinite(transform.D) && SvgFiniteMath.IsFinite(transform.E) && SvgFiniteMath.IsFinite(transform.F);
    }

    static bool ValidClip(SvgPixelRect? clip, SvgPixelBuffer surface)
    {
        if (clip == null) return true;
        if (clip.Width < 0 || clip.Height < 0 || clip.X < 0 || clip.Y < 0) return false;
        long right = (long)clip.X + clip.Width;
        long bottom = (long)clip.Y + clip.Height;
        if (right != clip.Right || bottom != clip.Bottom || clip.IsEmpty != (clip.Width == 0 || clip.Height == 0)) return false;
        return right <= surface.Width && bottom <= surface.Height;
    }
}
