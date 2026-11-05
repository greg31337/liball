namespace svg;

internal class SvgQuadraticFlattenInterval
{
    internal SvgQuadraticBezier? Segment;
    internal double StartAmount;
    internal double EndAmount;
    internal int Depth;
    internal SvgQuadraticFlattenInterval(SvgQuadraticBezier? segment, double startAmount, double endAmount, int depth) { Segment = segment; StartAmount = startAmount; EndAmount = endAmount; Depth = depth; }
}

internal class SvgCubicFlattenInterval
{
    internal SvgCubicBezier? Segment;
    internal double StartAmount;
    internal double EndAmount;
    internal int Depth;
    internal SvgCubicFlattenInterval(SvgCubicBezier? segment, double startAmount, double endAmount, int depth) { Segment = segment; StartAmount = startAmount; EndAmount = endAmount; Depth = depth; }
}

internal class SvgArcFlattenInterval
{
    internal SvgArc? Segment;
    internal double StartAmount;
    internal double EndAmount;
    internal int Depth;
    internal SvgArcFlattenInterval(SvgArc? segment, double startAmount, double endAmount, int depth) { Segment = segment; StartAmount = startAmount; EndAmount = endAmount; Depth = depth; }
}

internal class SvgDistanceResult
{
    internal bool Success;
    internal double Distance;
    internal SvgDistanceResult(bool success, double distance) { Success = success; Distance = distance; }
}

internal static class SvgFlattenMath
{
    internal static SvgFlattenResult Flatten(SvgLineSegment line, SvgFlattenOptions options)
    {
        if (!ValidLine(line) || !ValidOptions(options)) return Failure(SvgFlattenError.InvalidArgument);
        if (!Measurable(line.Start, options) || !Measurable(line.End, options)) return Failure(SvgFlattenError.NonFiniteResult);
        if (options.MaximumSegments < 1) return Failure(SvgFlattenError.SegmentLimitExceeded);
        System.Collections.Generic.List<SvgFlattenedPoint> points = Start(line.Start);
        points.Add(new SvgFlattenedPoint(line.End, 1));
        return Success(points, 1);
    }

    internal static SvgFlattenResult Flatten(SvgQuadraticBezier quadratic, SvgFlattenOptions options)
    {
        if (!ValidQuadratic(quadratic) || !ValidOptions(options)) return Failure(SvgFlattenError.InvalidArgument);
        System.Collections.Generic.List<SvgFlattenedPoint> points = Start(quadratic.Start);
        System.Collections.Generic.Stack<SvgQuadraticFlattenInterval> stack = new System.Collections.Generic.Stack<SvgQuadraticFlattenInterval>();
        stack.Push(new SvgQuadraticFlattenInterval(quadratic, 0, 1, 0));
        while (stack.Count != 0)
        {
            SvgQuadraticFlattenInterval interval = stack.Pop();
            SvgDistanceResult flatness = Distance((interval.Segment ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Control, interval.Segment.Start, interval.Segment.End, options);
            if (!flatness.Success) return Failure(SvgFlattenError.NonFiniteResult);
            if (flatness.Distance <= options.Tolerance)
            {
                SvgFlattenResult? added = Add(points, interval.Segment.End, interval.EndAmount, options);
                if (added != null) return added;
            }
            else
            {
                if (interval.Depth >= options.MaximumSubdivisionDepth) return Failure(SvgFlattenError.SubdivisionLimitExceeded);
                SvgQuadraticSplitResult split = interval.Segment.Split(0.5);
                if (!split.Success) return Failure(SvgFlattenError.NonFiniteResult);
                double middle = (interval.StartAmount + interval.EndAmount) / 2;
                stack.Push(new SvgQuadraticFlattenInterval(split.Second, middle, interval.EndAmount, interval.Depth + 1));
                stack.Push(new SvgQuadraticFlattenInterval(split.First, interval.StartAmount, middle, interval.Depth + 1));
            }
        }
        return Success(points, points.Count - 1);
    }

    internal static SvgFlattenResult Flatten(SvgCubicBezier cubic, SvgFlattenOptions options)
    {
        if (!ValidCubic(cubic) || !ValidOptions(options)) return Failure(SvgFlattenError.InvalidArgument);
        System.Collections.Generic.List<SvgFlattenedPoint> points = Start(cubic.Start);
        System.Collections.Generic.Stack<SvgCubicFlattenInterval> stack = new System.Collections.Generic.Stack<SvgCubicFlattenInterval>();
        stack.Push(new SvgCubicFlattenInterval(cubic, 0, 1, 0));
        while (stack.Count != 0)
        {
            SvgCubicFlattenInterval interval = stack.Pop();
            SvgDistanceResult first = Distance((interval.Segment ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Control1, interval.Segment.Start, interval.Segment.End, options);
            SvgDistanceResult second = Distance(interval.Segment.Control2, interval.Segment.Start, interval.Segment.End, options);
            if (!first.Success || !second.Success) return Failure(SvgFlattenError.NonFiniteResult);
            if (System.Math.Max(first.Distance, second.Distance) <= options.Tolerance)
            {
                SvgFlattenResult? added = Add(points, interval.Segment.End, interval.EndAmount, options);
                if (added != null) return added;
            }
            else
            {
                if (interval.Depth >= options.MaximumSubdivisionDepth) return Failure(SvgFlattenError.SubdivisionLimitExceeded);
                SvgCubicSplitResult split = interval.Segment.Split(0.5);
                if (!split.Success) return Failure(SvgFlattenError.NonFiniteResult);
                double middle = (interval.StartAmount + interval.EndAmount) / 2;
                stack.Push(new SvgCubicFlattenInterval(split.Second, middle, interval.EndAmount, interval.Depth + 1));
                stack.Push(new SvgCubicFlattenInterval(split.First, interval.StartAmount, middle, interval.Depth + 1));
            }
        }
        return Success(points, points.Count - 1);
    }

    internal static SvgFlattenResult Flatten(SvgArc arc, SvgFlattenOptions options)
    {
        if (!SvgArcMath.ValidArc(arc) || !ValidOptions(options)) return Failure(SvgFlattenError.InvalidArgument);
        System.Collections.Generic.List<SvgFlattenedPoint> points = Start(arc.Start);
        if (arc.IsPoint) return Success(points, 0);
        if (arc.IsLine)
        {
            if (!Measurable(arc.Start, options) || !Measurable(arc.End, options)) return Failure(SvgFlattenError.NonFiniteResult);
            SvgFlattenResult? added = Add(points, arc.End, 1, options);
            return added == null ? Success(points, 1) : added;
        }
        System.Collections.Generic.Stack<SvgArcFlattenInterval> stack = new System.Collections.Generic.Stack<SvgArcFlattenInterval>();
        stack.Push(new SvgArcFlattenInterval(arc, 0, 1, 0));
        while (stack.Count != 0)
        {
            SvgArcFlattenInterval interval = stack.Pop();
            SvgPointOperationResult middlePoint = (interval.Segment ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Evaluate(0.5);
            if (!middlePoint.Success) return Failure(SvgFlattenError.NonFiniteResult);
            SvgDistanceResult flatness = Distance(middlePoint.Point, interval.Segment.Start, interval.Segment.End, options);
            if (!flatness.Success) return Failure(SvgFlattenError.NonFiniteResult);
            bool angleSmall = System.Math.Abs(interval.Segment.SweepAngle) <= System.Math.PI / 2;
            if (angleSmall && flatness.Distance <= options.Tolerance)
            {
                SvgFlattenResult? added = Add(points, interval.Segment.End, interval.EndAmount, options);
                if (added != null) return added;
            }
            else
            {
                if (interval.Depth >= options.MaximumSubdivisionDepth) return Failure(SvgFlattenError.SubdivisionLimitExceeded);
                SvgArcSplitResult split = interval.Segment.Split(0.5);
                if (!split.Success) return Failure(SvgFlattenError.NonFiniteResult);
                double middle = (interval.StartAmount + interval.EndAmount) / 2;
                stack.Push(new SvgArcFlattenInterval(split.Second, middle, interval.EndAmount, interval.Depth + 1));
                stack.Push(new SvgArcFlattenInterval(split.First, interval.StartAmount, middle, interval.Depth + 1));
            }
        }
        return Success(points, points.Count - 1);
    }

    static SvgDistanceResult Distance(SvgPoint? point, SvgPoint start, SvgPoint end, SvgFlattenOptions options)
    {
        SvgPointOperationResult measuredPoint = Measure(point, options);
        SvgPointOperationResult measuredStart = Measure(start, options);
        SvgPointOperationResult measuredEnd = Measure(end, options);
        if (!measuredPoint.Success || !measuredStart.Success || !measuredEnd.Success) return new SvgDistanceResult(false, 0);
        SvgFiniteResult dx = SvgFiniteMath.Subtract((measuredEnd.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (measuredStart.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X);
        SvgFiniteResult dy = SvgFiniteMath.Subtract(measuredEnd.Point.Y, measuredStart.Point.Y);
        SvgFiniteResult px = SvgFiniteMath.Subtract((measuredPoint.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, measuredStart.Point.X);
        SvgFiniteResult py = SvgFiniteMath.Subtract(measuredPoint.Point.Y, measuredStart.Point.Y);
        if (!dx.Success || !dy.Success || !px.Success || !py.Success) return new SvgDistanceResult(false, 0);
        double length = Hypotenuse(dx.Value, dy.Value);
        if (!SvgFiniteMath.IsFinite(length)) return new SvgDistanceResult(false, 0);
        if (length == 0)
        {
            double distance = Hypotenuse(px.Value, py.Value);
            return new SvgDistanceResult(SvgFiniteMath.IsFinite(distance), distance);
        }
        double distanceToLine = System.Math.Abs(dx.Value / length * py.Value - dy.Value / length * px.Value);
        if (!SvgFiniteMath.IsFinite(distanceToLine)) return new SvgDistanceResult(false, 0);
        return new SvgDistanceResult(true, distanceToLine);
    }

    static SvgPointOperationResult Measure(SvgPoint? point, SvgFlattenOptions options)
    {
        if (options.DeviceTransform == null) return new SvgPointOperationResult(true, SvgGeometryError.None, new SvgPoint((point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, point.Y));
        SvgPointTransformResult transformed = options.DeviceTransform.TransformPoint((point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!transformed.Success) return new SvgPointOperationResult(false, transformed.Error, null);
        return new SvgPointOperationResult(true, SvgGeometryError.None, transformed.Point);
    }

    static bool Measurable(SvgPoint point, SvgFlattenOptions options)
    {
        return Measure(point, options).Success;
    }

    static SvgFlattenResult? Add(System.Collections.Generic.List<SvgFlattenedPoint> points, SvgPoint point, double amount, SvgFlattenOptions options)
    {
        if (points.Count - 1 >= options.MaximumSegments) return Failure(SvgFlattenError.SegmentLimitExceeded);
        points.Add(new SvgFlattenedPoint(point, amount));
        return null;
    }

    static System.Collections.Generic.List<SvgFlattenedPoint> Start(SvgPoint point)
    {
        System.Collections.Generic.List<SvgFlattenedPoint> points = new System.Collections.Generic.List<SvgFlattenedPoint>();
        points.Add(new SvgFlattenedPoint(point, 0));
        return points;
    }

    static bool ValidOptions(SvgFlattenOptions? options)
    {
        if (options == null || !SvgFiniteMath.IsFinite(options.Tolerance) || options.Tolerance <= 0 || options.MaximumSubdivisionDepth < 0 || options.MaximumSegments < 1) return false;
        if (options.DeviceTransform == null) return true;
        SvgMatrix matrix = options.DeviceTransform;
        return SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }

    static bool ValidLine(SvgLineSegment line) { return line != null && SvgCurveMath.ValidPoint(line.Start) && SvgCurveMath.ValidPoint(line.End); }
    static bool ValidQuadratic(SvgQuadraticBezier quadratic) { return quadratic != null && SvgCurveMath.ValidPoint(quadratic.Start) && SvgCurveMath.ValidPoint(quadratic.Control) && SvgCurveMath.ValidPoint(quadratic.End); }
    static bool ValidCubic(SvgCubicBezier cubic) { return cubic != null && SvgCurveMath.ValidPoint(cubic.Start) && SvgCurveMath.ValidPoint(cubic.Control1) && SvgCurveMath.ValidPoint(cubic.Control2) && SvgCurveMath.ValidPoint(cubic.End); }

    static double Hypotenuse(double x, double y)
    {
        double absoluteX = System.Math.Abs(x);
        double absoluteY = System.Math.Abs(y);
        double maximum = System.Math.Max(absoluteX, absoluteY);
        if (!SvgFiniteMath.IsFinite(maximum) || maximum == 0) return maximum;
        double minimum = System.Math.Min(absoluteX, absoluteY);
        double ratio = minimum / maximum;
        return maximum * System.Math.Sqrt(1 + ratio * ratio);
    }

    static SvgFlattenResult Success(System.Collections.Generic.List<SvgFlattenedPoint> points, int segmentCount) { return new SvgFlattenResult(true, SvgFlattenError.None, points, segmentCount); }
    static SvgFlattenResult Failure(SvgFlattenError error) { return new SvgFlattenResult(false, error, null, 0); }
}
