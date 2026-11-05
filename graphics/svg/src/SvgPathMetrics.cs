namespace svg;

internal class SvgMeasuredSegment
{
    internal SvgPathCommandKind Kind;
    internal SvgLineSegment? Line;
    internal SvgQuadraticBezier? Quadratic;
    internal SvgCubicBezier? Cubic;
    internal SvgArc? Arc;
    internal SvgFlattenResult? Flattened;
    internal double Length;
    internal double StartLength;
    internal int SubpathIndex;

    internal SvgMeasuredSegment(SvgPathCommandKind kind, int subpathIndex)
    {
        Kind = kind; SubpathIndex = subpathIndex; Line = null; Quadratic = null; Cubic = null; Arc = null; Flattened = null; Length = 0; StartLength = 0;
    }
}

internal class SvgPathMeasureState
{
    internal SvgPathMetricError Error;
    SvgPathMeasureOptions? options;
    SvgPathData? normalized;
    System.Collections.Generic.List<SvgMeasuredSegment> segments;
    System.Collections.Generic.List<double> subpathLengths;
    double totalLength;
    int flattenedSegments;

    SvgPathMeasureState(SvgPathMeasureOptions? measureOptions)
    {
        options = measureOptions; Error = SvgPathMetricError.None; normalized = null; segments = new System.Collections.Generic.List<SvgMeasuredSegment>(); subpathLengths = new System.Collections.Generic.List<double>(); totalLength = 0; flattenedSegments = 0;
    }

    internal static SvgPathMeasureState Create(SvgPathData? path, SvgPathMeasureOptions? options)
    {
        SvgPathMeasureState state = new SvgPathMeasureState(options);
        if (!ValidOptions(options) || path == null) { state.Error = SvgPathMetricError.InvalidArgument; return state; }
        SvgPathNormalizeResult normalized = path.Normalize();
        if (!normalized.Success)
        {
            if (normalized.Error == SvgPathNormalizeError.InvalidPath) state.Error = SvgPathMetricError.InvalidPath;
            else if (normalized.Error == SvgPathNormalizeError.NonFiniteResult) state.Error = SvgPathMetricError.NonFiniteResult;
            else state.Error = SvgPathMetricError.InvalidArgument;
            return state;
        }
        state.normalized = normalized.PathData;
        state.Build();
        return state;
    }

    internal SvgPathMetricResult MetricResult()
    {
        double calibrated = (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).PathLength == null ? totalLength : (options.PathLength ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value;
        double scale = 1;
        if (options.PathLength != null)
        {
            if (options.PathLength.Value == 0) return new SvgPathMetrics().MetricFailure(SvgPathMetricError.InvalidArgument);
            else scale = totalLength / options.PathLength.Value;
        }
        if (!SvgFiniteMath.IsFinite(scale)) return new SvgPathMetrics().MetricFailure(SvgPathMetricError.NonFiniteResult);
        return new SvgPathMetricResult(true, SvgPathMetricError.None, totalLength, calibrated, scale, new System.Collections.Generic.List<double>(subpathLengths));
    }

    internal SvgPathLocationResult Location(double distance)
    {
        SvgFiniteResult actual = ActualDistance(distance);
        if (!actual.Success) return new SvgPathMetrics().LocationFailure(DistanceError(distance));
        return LocationActual(actual.Value);
    }

    SvgPathLocationResult LocationActual(double actualDistance)
    {
        if (segments.Count == 0)
        {
            SvgPoint point = FirstPoint();
            return new SvgPathLocationResult(true, SvgPathMetricError.None, point, new SvgVector(0, 0), new SvgVector(0, 0), 0, -1);
        }
        int index = SelectSegment(actualDistance);
        SvgMeasuredSegment segment = segments[index];
        double local = actualDistance - segment.StartLength;
        double amount = Parameter(segment, local);
        SvgPointOperationResult pointResult = Evaluate(segment, amount);
        if (!pointResult.Success) return new SvgPathMetrics().LocationFailure(SvgPathMetricError.NonFiniteResult);
        SvgVectorOperationResult derivative = Derivative(segment, amount);
        SvgVector tangent = UnitTangent(derivative, segment);
        SvgVector normal = new SvgVector(-tangent.Y, tangent.X);
        return new SvgPathLocationResult(true, SvgPathMetricError.None, pointResult.Point, tangent, normal, actualDistance, index);
    }

    internal SvgPathExtractResult Extract(double startDistance, double endDistance)
    {
        SvgFiniteResult actualStart = ActualDistance(startDistance);
        SvgFiniteResult actualEnd = ActualDistance(endDistance);
        if (!actualStart.Success) return new SvgPathMetrics().ExtractFailure(DistanceError(startDistance));
        if (!actualEnd.Success) return new SvgPathMetrics().ExtractFailure(DistanceError(endDistance));
        if (actualEnd.Value < actualStart.Value) return new SvgPathMetrics().ExtractFailure(SvgPathMetricError.InvalidArgument);
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        if (actualStart.Value == actualEnd.Value)
        {
            SvgPathLocationResult pointLocation = LocationActual(actualStart.Value);
            if (!pointLocation.Success) return new SvgPathMetrics().ExtractFailure(pointLocation.Error);
            builder.MoveTo((pointLocation.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, pointLocation.Point.Y);
            return new SvgPathExtractResult(true, SvgPathMetricError.None, builder.Build());
        }
        bool hasOutput = false;
        int outputSubpath = -1;
        int index = 0;
        while (index < segments.Count)
        {
            SvgMeasuredSegment segment = segments[index];
            double segmentEnd = segment.StartLength + segment.Length;
            if (segment.Length > 0 && segmentEnd > actualStart.Value && segment.StartLength < actualEnd.Value)
            {
                double localStart = System.Math.Max(0, actualStart.Value - segment.StartLength);
                double localEnd = System.Math.Min(segment.Length, actualEnd.Value - segment.StartLength);
                if (localEnd >= localStart)
                {
                    double startAmount = Parameter(segment, localStart);
                    double endAmount = Parameter(segment, localEnd);
                    SvgMeasuredSegment? slice = Slice(segment, startAmount, endAmount);
                    if (slice == null) return new SvgPathMetrics().ExtractFailure(SvgPathMetricError.NonFiniteResult);
                    SvgPoint startPoint = StartPoint(slice);
                    if (!hasOutput || outputSubpath != segment.SubpathIndex)
                    {
                        builder.MoveTo(startPoint.X, startPoint.Y);
                        hasOutput = true;
                        outputSubpath = segment.SubpathIndex;
                    }
                    Add(builder, slice);
                }
            }
            index = index + 1;
        }
        if (!hasOutput)
        {
            SvgPathLocationResult location = LocationActual(actualStart.Value);
            if (!location.Success) return new SvgPathMetrics().ExtractFailure(location.Error);
            builder.MoveTo((location.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, location.Point.Y);
        }
        return new SvgPathExtractResult(true, SvgPathMetricError.None, builder.Build());
    }

    void Build()
    {
        int subpathCount = (normalized ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GetSubpathCount();
        int subpathIndex = 0;
        while (subpathIndex < subpathCount) { subpathLengths.Add(0); subpathIndex = subpathIndex + 1; }
        int index = 0;
        while (index < normalized.Commands.Count && Error == SvgPathMetricError.None)
        {
            SvgPathCommand command = normalized.Commands[index];
            if (command.Kind != SvgPathCommandKind.MoveTo)
            {
                SvgMeasuredSegment? segment = CreateSegment(command);
                if (segment != null) MeasureSegment(segment);
            }
            index = index + 1;
        }
    }

    SvgMeasuredSegment? CreateSegment(SvgPathCommand command)
    {
        SvgMeasuredSegment segment = new SvgMeasuredSegment(command.Kind, command.SubpathIndex);
        SvgPoint start = new SvgPoint(command.StartX, command.StartY);
        SvgPoint end = new SvgPoint(command.EndX, command.EndY);
        if (command.Kind == SvgPathCommandKind.LineTo || command.Kind == SvgPathCommandKind.ClosePath) segment.Line = new SvgLineSegment(start, end);
        else if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) segment.Quadratic = new SvgQuadraticBezier(start, new SvgPoint(command.Values[0], command.Values[1]), end);
        else if (command.Kind == SvgPathCommandKind.CubicCurveTo) segment.Cubic = new SvgCubicBezier(start, new SvgPoint(command.Values[0], command.Values[1]), new SvgPoint(command.Values[2], command.Values[3]), end);
        else if (command.Kind == SvgPathCommandKind.ArcTo)
        {
            SvgArcOperationResult arc = new SvgArcGeometry().FromCommand(command);
            if (!arc.Success) { Error = SvgPathMetricError.NonFiniteResult; return null; }
            segment.Arc = arc.Arc;
        }
        else { Error = SvgPathMetricError.InvalidPath; return null; }
        return segment;
    }

    void MeasureSegment(SvgMeasuredSegment segment)
    {
        SvgFlattenOptions flatten = new SvgFlattenOptions();
        flatten.Tolerance = (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Tolerance;
        flatten.MaximumSubdivisionDepth = options.MaximumSubdivisionDepth;
        flatten.MaximumSegments = options.MaximumFlattenedSegments;
        if (segment.Line != null) segment.Flattened = segment.Line.Flatten(flatten);
        else if (segment.Quadratic != null) segment.Flattened = segment.Quadratic.Flatten(flatten);
        else if (segment.Cubic != null) segment.Flattened = segment.Cubic.Flatten(flatten);
        else segment.Flattened = (segment.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Flatten(flatten);
        if (!segment.Flattened.Success) { Error = Map(segment.Flattened.Error); return; }
        flattenedSegments = flattenedSegments + segment.Flattened.SegmentCount;
        if (flattenedSegments > options.MaximumFlattenedSegments) { Error = SvgPathMetricError.SegmentLimitExceeded; return; }
        double length = 0;
        int index = 1;
        while (index < (segment.Flattened.Points ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            double addition = Distance(segment.Flattened.Points[index - 1].Point, segment.Flattened.Points[index].Point);
            length = length + addition;
            if (!SvgFiniteMath.IsFinite(length)) { Error = SvgPathMetricError.NonFiniteResult; return; }
            index = index + 1;
        }
        segment.Length = SvgFiniteMath.NormalizeZero(length);
        segment.StartLength = totalLength;
        totalLength = totalLength + length;
        if (!SvgFiniteMath.IsFinite(totalLength)) { Error = SvgPathMetricError.NonFiniteResult; return; }
        if (segment.SubpathIndex >= 0 && segment.SubpathIndex < subpathLengths.Count) subpathLengths[segment.SubpathIndex] = subpathLengths[segment.SubpathIndex] + length;
        segments.Add(segment);
    }

    SvgFiniteResult ActualDistance(double distance)
    {
        if (!SvgFiniteMath.IsFinite(distance) || distance < 0) return new SvgFiniteResult(false, 0, "Invalid path distance.");
        double actual = distance;
        if ((options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).PathLength != null)
        {
            if (!SvgFiniteMath.IsFinite((options.PathLength ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value) || options.PathLength.Value <= 0) return new SvgFiniteResult(false, 0, "Invalid pathLength calibration.");
            actual = distance * (totalLength / options.PathLength.Value);
        }
        if (!SvgFiniteMath.IsFinite(actual)) return new SvgFiniteResult(false, 0, "Path distance is not finite.");
        if (actual > totalLength) actual = totalLength;
        return new SvgFiniteResult(true, SvgFiniteMath.NormalizeZero(actual), "");
    }

    SvgPathMetricError DistanceError(double distance)
    {
        if (!SvgFiniteMath.IsFinite(distance) || distance < 0) return SvgPathMetricError.InvalidArgument;
        SvgPathMetricResult metric = MetricResult();
        if (!metric.Success) return metric.Error;
        return SvgPathMetricError.NonFiniteResult;
    }

    int SelectSegment(double distance)
    {
        int lastPositive = -1;
        int index = 0;
        while (index < segments.Count)
        {
            SvgMeasuredSegment segment = segments[index];
            if (segment.Length > 0)
            {
                lastPositive = index;
                if (distance < segment.StartLength + segment.Length || distance == 0) return index;
            }
            index = index + 1;
        }
        return lastPositive >= 0 ? lastPositive : 0;
    }

    double Parameter(SvgMeasuredSegment segment, double localLength)
    {
        if (segment.Length == 0) return 0;
        if (localLength <= 0) return 0;
        if (localLength >= segment.Length) return 1;
        double accumulated = 0;
        int index = 1;
        while (index < ((segment.Flattened ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Points ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgFlattenedPoint first = (segment.Flattened.Points ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[index - 1];
            SvgFlattenedPoint second = segment.Flattened.Points[index];
            double edge = Distance(first.Point, second.Point);
            if (accumulated + edge >= localLength)
            {
                double fraction = edge == 0 ? 0 : (localLength - accumulated) / edge;
                return first.Amount + (second.Amount - first.Amount) * fraction;
            }
            accumulated = accumulated + edge;
            index = index + 1;
        }
        return 1;
    }

    SvgPointOperationResult Evaluate(SvgMeasuredSegment segment, double amount)
    {
        if (segment.Line != null) return segment.Line.Evaluate(amount);
        if (segment.Quadratic != null) return segment.Quadratic.Evaluate(amount);
        if (segment.Cubic != null) return segment.Cubic.Evaluate(amount);
        return (segment.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Evaluate(amount);
    }

    SvgVectorOperationResult Derivative(SvgMeasuredSegment segment, double amount)
    {
        if (segment.Line != null) return segment.Line.Derivative(amount);
        if (segment.Quadratic != null) return segment.Quadratic.Derivative(amount);
        if (segment.Cubic != null) return segment.Cubic.Derivative(amount);
        return (segment.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Derivative(amount);
    }

    SvgVector UnitTangent(SvgVectorOperationResult derivative, SvgMeasuredSegment segment)
    {
        if (derivative.Success)
        {
            double length = Hypotenuse((derivative.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, derivative.Vector.Y);
            if (length > 0 && SvgFiniteMath.IsFinite(length)) return new SvgVector(derivative.Vector.X / length, derivative.Vector.Y / length);
        }
        SvgPoint start = StartPoint(segment);
        SvgPoint end = EndPoint(segment);
        double x = end.X - start.X;
        double y = end.Y - start.Y;
        double fallback = Hypotenuse(x, y);
        if (fallback > 0 && SvgFiniteMath.IsFinite(fallback)) return new SvgVector(x / fallback, y / fallback);
        return new SvgVector(0, 0);
    }

    SvgMeasuredSegment? Slice(SvgMeasuredSegment segment, double startAmount, double endAmount)
    {
        if (startAmount == 0 && endAmount == 1) return segment;
        if (segment.Line != null)
        {
            SvgPointOperationResult start = segment.Line.Evaluate(startAmount); SvgPointOperationResult end = segment.Line.Evaluate(endAmount);
            if (!start.Success || !end.Success) return null;
            SvgMeasuredSegment result = new SvgMeasuredSegment(SvgPathCommandKind.LineTo, segment.SubpathIndex); result.Line = new SvgLineSegment((start.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (end.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); return result;
        }
        if (segment.Quadratic != null)
        {
            SvgQuadraticBezier value = segment.Quadratic;
            if (endAmount < 1) { SvgQuadraticSplitResult? endSplit = value.Split(endAmount); if (!endSplit.Success) return null; value = (endSplit.First ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")); }
            if (startAmount > 0) { double relative = endAmount == 0 ? 0 : startAmount / endAmount; SvgQuadraticSplitResult? startSplit = (value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Split(relative); if (!startSplit.Success) return null; value = (startSplit.Second ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")); }
            SvgMeasuredSegment result = new SvgMeasuredSegment(SvgPathCommandKind.QuadraticCurveTo, segment.SubpathIndex); result.Quadratic = value; return result;
        }
        if (segment.Cubic != null)
        {
            SvgCubicBezier value = segment.Cubic;
            if (endAmount < 1) { SvgCubicSplitResult? endSplit = value.Split(endAmount); if (!endSplit.Success) return null; value = (endSplit.First ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")); }
            if (startAmount > 0) { double relative = endAmount == 0 ? 0 : startAmount / endAmount; SvgCubicSplitResult? startSplit = (value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Split(relative); if (!startSplit.Success) return null; value = (startSplit.Second ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")); }
            SvgMeasuredSegment result = new SvgMeasuredSegment(SvgPathCommandKind.CubicCurveTo, segment.SubpathIndex); result.Cubic = value; return result;
        }
        SvgArc? arc = segment.Arc;
        if (endAmount < 1) { SvgArcSplitResult? endSplit = (arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Split(endAmount); if (!endSplit.Success) return null; arc = endSplit.First; }
        if (startAmount > 0) { double relative = endAmount == 0 ? 0 : startAmount / endAmount; SvgArcSplitResult? startSplit = (arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Split(relative); if (!startSplit.Success) return null; arc = startSplit.Second; }
        SvgMeasuredSegment arcResult = new SvgMeasuredSegment(SvgPathCommandKind.ArcTo, segment.SubpathIndex); arcResult.Arc = arc; return arcResult;
    }

    static void Add(SvgGeneratedPathBuilder builder, SvgMeasuredSegment segment)
    {
        if (segment.Line != null) builder.LineTo(segment.Line.End.X, segment.Line.End.Y);
        else if (segment.Quadratic != null) builder.QuadraticTo(segment.Quadratic.Control.X, segment.Quadratic.Control.Y, segment.Quadratic.End.X, segment.Quadratic.End.Y);
        else if (segment.Cubic != null) builder.CubicTo(segment.Cubic.Control1.X, segment.Cubic.Control1.Y, segment.Cubic.Control2.X, segment.Cubic.Control2.Y, segment.Cubic.End.X, segment.Cubic.End.Y);
        else
        {
            bool large = System.Math.Abs((segment.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SweepAngle) > System.Math.PI;
            bool sweep = segment.Arc.SweepAngle > 0;
            double rotation = segment.Arc.Rotation * 180 / System.Math.PI;
            builder.ArcToWithParameters(segment.Arc.RadiusX, segment.Arc.RadiusY, rotation, large, sweep, segment.Arc.End.X, segment.Arc.End.Y);
        }
    }

    SvgPoint FirstPoint()
    {
        int index = 0;
        while (index < (normalized ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count) { if (normalized.Commands[index].Kind == SvgPathCommandKind.MoveTo) return new SvgPoint(normalized.Commands[index].EndX, normalized.Commands[index].EndY); index = index + 1; }
        return new SvgPoint(0, 0);
    }

    static SvgPoint StartPoint(SvgMeasuredSegment segment) { if (segment.Line != null) return segment.Line.Start; if (segment.Quadratic != null) return segment.Quadratic.Start; if (segment.Cubic != null) return segment.Cubic.Start; return (segment.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Start; }
    static SvgPoint EndPoint(SvgMeasuredSegment segment) { if (segment.Line != null) return segment.Line.End; if (segment.Quadratic != null) return segment.Quadratic.End; if (segment.Cubic != null) return segment.Cubic.End; return (segment.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).End; }
    static double Distance(SvgPoint first, SvgPoint second) { return Hypotenuse(second.X - first.X, second.Y - first.Y); }
    static double Hypotenuse(double x, double y) { double ax = System.Math.Abs(x); double ay = System.Math.Abs(y); double maximum = System.Math.Max(ax, ay); if (maximum == 0 || !SvgFiniteMath.IsFinite(maximum)) return maximum; double ratio = System.Math.Min(ax, ay) / maximum; return maximum * System.Math.Sqrt(1 + ratio * ratio); }
    static bool ValidOptions(SvgPathMeasureOptions? options) { return options != null && SvgFiniteMath.IsFinite(options.Tolerance) && options.Tolerance > 0 && options.MaximumSubdivisionDepth >= 0 && options.MaximumFlattenedSegments > 0 && (options.PathLength == null || SvgFiniteMath.IsFinite(options.PathLength.Value) && options.PathLength.Value >= 0); }
    static SvgPathMetricError Map(SvgFlattenError error) { if (error == SvgFlattenError.SegmentLimitExceeded) return SvgPathMetricError.SegmentLimitExceeded; if (error == SvgFlattenError.SubdivisionLimitExceeded) return SvgPathMetricError.SubdivisionLimitExceeded; if (error == SvgFlattenError.InvalidArgument) return SvgPathMetricError.InvalidArgument; return SvgPathMetricError.NonFiniteResult; }
}
