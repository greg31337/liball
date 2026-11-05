namespace svg;

internal class SvgLineIntersectionCandidate
{
    internal bool Found; internal bool Overlap; internal double First; internal double Second; internal double FirstEnd; internal double SecondEnd;
    internal SvgLineIntersectionCandidate() { Found = false; Overlap = false; First = 0; Second = 0; FirstEnd = 0; SecondEnd = 0; }
}

internal class SvgIntersectionState
{
    internal System.Collections.Generic.List<SvgIntersection> Results; internal int Comparisons; SvgIntersectionSegment first; SvgIntersectionSegment second; SvgFlattenResult firstFlat; SvgFlattenResult secondFlat; SvgIntersectionOptions options;
    internal SvgIntersectionState(SvgIntersectionSegment firstSegment, SvgIntersectionSegment secondSegment, SvgFlattenResult firstResult, SvgFlattenResult secondResult, SvgIntersectionOptions intersectionOptions) { first = firstSegment; second = secondSegment; firstFlat = firstResult; secondFlat = secondResult; options = intersectionOptions; Results = new System.Collections.Generic.List<SvgIntersection>(); Comparisons = 0; }
    internal SvgIntersectionError Run()
    {
        int i = 1; while (i < (firstFlat.Points ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count) { int j = 1; while (j < (secondFlat.Points ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count) { if (Comparisons >= options.MaximumComparisons) return SvgIntersectionError.ComparisonLimitExceeded; Comparisons = Comparisons + 1; SvgLineIntersectionCandidate candidate = Intersect(firstFlat.Points[i - 1].Point, firstFlat.Points[i].Point, secondFlat.Points[j - 1].Point, secondFlat.Points[j].Point); if (candidate.Found) { SvgIntersectionError error = Add(candidate, firstFlat.Points[i - 1], firstFlat.Points[i], secondFlat.Points[j - 1], secondFlat.Points[j]); if (error != SvgIntersectionError.None) return error; } j = j + 1; } i = i + 1; }
        return SvgIntersectionError.None;
    }

    SvgIntersectionError Add(SvgLineIntersectionCandidate candidate, SvgFlattenedPoint firstStart, SvgFlattenedPoint firstEnd, SvgFlattenedPoint secondStart, SvgFlattenedPoint secondEnd)
    {
        double firstAmount = firstStart.Amount + (firstEnd.Amount - firstStart.Amount) * candidate.First; double secondAmount = secondStart.Amount + (secondEnd.Amount - secondStart.Amount) * candidate.Second; SvgIntersectionError error = AddOne(firstAmount, secondAmount, candidate.Overlap ? SvgIntersectionKind.Overlap : SvgIntersectionKind.Crossing); if (error != SvgIntersectionError.None) return error;
        if (candidate.Overlap && (candidate.FirstEnd != candidate.First || candidate.SecondEnd != candidate.Second)) { double firstOverlapEnd = firstStart.Amount + (firstEnd.Amount - firstStart.Amount) * candidate.FirstEnd; double secondOverlapEnd = secondStart.Amount + (secondEnd.Amount - secondStart.Amount) * candidate.SecondEnd; return AddOne(firstOverlapEnd, secondOverlapEnd, SvgIntersectionKind.Overlap); }
        return SvgIntersectionError.None;
    }

    SvgIntersectionError AddOne(double firstAmount, double secondAmount, SvgIntersectionKind suggested)
    {
        SvgPointOperationResult firstPoint = new SvgIntersections().Evaluate(first, Clamp(firstAmount)); SvgPointOperationResult secondPoint = new SvgIntersections().Evaluate(second, Clamp(secondAmount)); if (!firstPoint.Success || !secondPoint.Success) return SvgIntersectionError.NonFiniteResult;
        SvgFiniteResult x = SvgFiniteMath.Lerp((firstPoint.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (secondPoint.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, 0.5); SvgFiniteResult y = SvgFiniteMath.Lerp(firstPoint.Point.Y, secondPoint.Point.Y, 0.5); if (!x.Success || !y.Success) return SvgIntersectionError.NonFiniteResult; SvgPoint point = new SvgPoint(x.Value, y.Value);
        int index = 0; while (index < Results.Count) { if (Distance(point, Results[index].Point) <= options.Tolerance) { Results[index].DuplicateCount = Results[index].DuplicateCount + 1; if (suggested == SvgIntersectionKind.Overlap) Results[index].Kind = SvgIntersectionKind.Overlap; else if (Tangent(firstAmount, secondAmount)) Results[index].Kind = SvgIntersectionKind.Tangent; return SvgIntersectionError.None; } index = index + 1; }
        if (Results.Count >= options.MaximumIntersections) return SvgIntersectionError.IntersectionLimitExceeded; SvgIntersectionKind kind = suggested == SvgIntersectionKind.Overlap ? suggested : Tangent(firstAmount, secondAmount) ? SvgIntersectionKind.Tangent : SvgIntersectionKind.Crossing; Results.Add(new SvgIntersection(point, Clamp(firstAmount), Clamp(secondAmount), kind, 0)); return SvgIntersectionError.None;
    }

    bool Tangent(double firstAmount, double secondAmount) { SvgVectorOperationResult a = new SvgIntersections().Derivative(first, Clamp(firstAmount)); SvgVectorOperationResult b = new SvgIntersections().Derivative(second, Clamp(secondAmount)); if (!a.Success || !b.Success) return false; double firstLength = Length((a.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, a.Vector.Y); double secondLength = Length((b.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, b.Vector.Y); if (firstLength == 0 || secondLength == 0 || !SvgFiniteMath.IsFinite(firstLength) || !SvgFiniteMath.IsFinite(secondLength)) return true; double cross = a.Vector.X / firstLength * (b.Vector.Y / secondLength) - a.Vector.Y / firstLength * (b.Vector.X / secondLength); return System.Math.Abs(cross) <= options.TangentTolerance; }

    SvgLineIntersectionCandidate Intersect(SvgPoint a, SvgPoint b, SvgPoint c, SvgPoint d)
    {
        SvgLineIntersectionCandidate result = new SvgLineIntersectionCandidate(); double rx = b.X - a.X; double ry = b.Y - a.Y; double sx = d.X - c.X; double sy = d.Y - c.Y; double qx = c.X - a.X; double qy = c.Y - a.Y; if (!Finite(rx, ry, sx, sy, qx, qy)) return result; double firstLength = Length(rx, ry); double secondLength = Length(sx, sy);
        if (firstLength == 0) { if (secondLength == 0) { if (Distance(a, c) <= options.Tolerance) result.Found = true; return result; } double secondAmount = Parameter(a, c, d); double secondTolerance = options.Tolerance / secondLength; if (secondAmount >= -secondTolerance && secondAmount <= 1 + secondTolerance && Distance(a, Point(c, d, Clamp(secondAmount))) <= options.Tolerance) { result.Found = true; result.Second = Clamp(secondAmount); } return result; }
        if (secondLength == 0) { double firstAmount = Parameter(c, a, b); double firstTolerance = options.Tolerance / firstLength; if (firstAmount >= -firstTolerance && firstAmount <= 1 + firstTolerance && Distance(c, Point(a, b, Clamp(firstAmount))) <= options.Tolerance) { result.Found = true; result.First = Clamp(firstAmount); } return result; }
        double denominator = Cross(rx, ry, sx, sy); double scale = Higher(firstLength, secondLength); double epsilon = options.Tolerance * scale;
        if (System.Math.Abs(denominator) > epsilon) { double t = Cross(qx, qy, sx, sy) / denominator; double u = Cross(qx, qy, rx, ry) / denominator; double parameterTolerance = scale == 0 ? 0 : options.Tolerance / scale; if (t >= -parameterTolerance && t <= 1 + parameterTolerance && u >= -parameterTolerance && u <= 1 + parameterTolerance) { result.Found = true; result.First = Clamp(t); result.Second = Clamp(u); } return result; }
        if (System.Math.Abs(Cross(qx, qy, rx, ry)) > epsilon) return result;
        double t0 = Parameter(c, a, b); double t1 = Parameter(d, a, b); double lower = Lower(t0, t1); double upper = Higher(t0, t1); double overlapStart = Higher(0, lower); double overlapEnd = Lower(1, upper); if (overlapEnd < overlapStart) return result; result.Found = true; result.Overlap = true; result.First = Clamp(overlapStart); result.FirstEnd = Clamp(overlapEnd); SvgPoint startPoint = Point(a, b, result.First); SvgPoint endPoint = Point(a, b, result.FirstEnd); result.Second = Parameter(startPoint, c, d); result.SecondEnd = Parameter(endPoint, c, d); return result;
    }

    static double Parameter(SvgPoint point, SvgPoint start, SvgPoint end) { double dx = end.X - start.X; double dy = end.Y - start.Y; if (System.Math.Abs(dx) >= System.Math.Abs(dy)) return dx == 0 ? 0 : (point.X - start.X) / dx; return dy == 0 ? 0 : (point.Y - start.Y) / dy; }
    static SvgPoint Point(SvgPoint start, SvgPoint end, double amount) { SvgFiniteResult x = SvgFiniteMath.Lerp(start.X, end.X, amount); SvgFiniteResult y = SvgFiniteMath.Lerp(start.Y, end.Y, amount); return x.Success && y.Success ? new SvgPoint(x.Value, y.Value) : start; }
    static double Distance(SvgPoint a, SvgPoint b) { return Length(b.X - a.X, b.Y - a.Y); }
    static double Length(double x, double y) { double ax = System.Math.Abs(x); double ay = System.Math.Abs(y); double high = Higher(ax, ay); if (high == 0 || !SvgFiniteMath.IsFinite(high)) return high; double low = Lower(ax, ay); double ratio = low / high; return high * System.Math.Sqrt(1 + ratio * ratio); }
    static double Cross(double ax, double ay, double bx, double by) { return ax * by - ay * bx; }
    static double Clamp(double value) { if (value < 0) return 0; if (value > 1) return 1; return SvgFiniteMath.NormalizeZero(value); }
    static double Lower(double a, double b) { return a < b ? a : b; }
    static double Higher(double a, double b) { return a > b ? a : b; }
    static bool Finite(double a, double b, double c, double d, double e, double f) { return SvgFiniteMath.IsFinite(a) && SvgFiniteMath.IsFinite(b) && SvgFiniteMath.IsFinite(c) && SvgFiniteMath.IsFinite(d) && SvgFiniteMath.IsFinite(e) && SvgFiniteMath.IsFinite(f); }
}
