namespace svg;

internal static class SvgArcMath
{
    internal static SvgArcOperationResult FromEndpoint(SvgPoint start, SvgPoint end, double radiusX, double radiusY, double rotationDegrees, bool largeArc, bool sweep)
    {
        if (!ValidPoint(start) || !ValidPoint(end) || !SvgFiniteMath.IsFinite(radiusX) || !SvgFiniteMath.IsFinite(radiusY) || !SvgFiniteMath.IsFinite(rotationDegrees) || radiusX < 0 || radiusY < 0) return ArcFailure(SvgGeometryError.InvalidArgument);
        SvgPoint startCopy = new SvgPoint(start.X, start.Y);
        SvgPoint endCopy = new SvgPoint(end.X, end.Y);
        double reducedDegrees = rotationDegrees % 360;
        double rotation = reducedDegrees * System.Math.PI / 180;
        if (!SvgFiniteMath.IsFinite(rotation)) return ArcFailure(SvgGeometryError.NonFiniteResult);
        radiusX = SvgFiniteMath.NormalizeZero(radiusX);
        radiusY = SvgFiniteMath.NormalizeZero(radiusY);
        if (start.X == end.X && start.Y == end.Y) return ArcSuccess(PointArc(startCopy, radiusX, radiusY, rotation));
        if (radiusX == 0 || radiusY == 0) return ArcSuccess(LineArc(startCopy, endCopy, radiusX, radiusY, rotation));

        double cosine = System.Math.Cos(rotation);
        double sine = System.Math.Sin(rotation);
        SvgFiniteResult halfDifferenceX = HalfDifference(start.X, end.X);
        SvgFiniteResult halfDifferenceY = HalfDifference(start.Y, end.Y);
        if (!halfDifferenceX.Success || !halfDifferenceY.Success) return ArcFailure(SvgGeometryError.NonFiniteResult);
        double transformedX = cosine * halfDifferenceX.Value + sine * halfDifferenceY.Value;
        double transformedY = -sine * halfDifferenceX.Value + cosine * halfDifferenceY.Value;
        if (!SvgFiniteMath.IsFinite(transformedX) || !SvgFiniteMath.IsFinite(transformedY)) return ArcFailure(SvgGeometryError.NonFiniteResult);

        double ratioX = transformedX / radiusX;
        double ratioY = transformedY / radiusY;
        double magnitude = Hypotenuse(ratioX, ratioY);
        if (!SvgFiniteMath.IsFinite(magnitude)) return ArcFailure(SvgGeometryError.NonFiniteResult);
        if (magnitude > 1)
        {
            radiusX = radiusX * magnitude;
            radiusY = radiusY * magnitude;
            if (!SvgFiniteMath.IsFinite(radiusX) || !SvgFiniteMath.IsFinite(radiusY)) return ArcFailure(SvgGeometryError.NonFiniteResult);
            ratioX = transformedX / radiusX;
            ratioY = transformedY / radiusY;
        }

        double lambda = ratioX * ratioX + ratioY * ratioY;
        if (!SvgFiniteMath.IsFinite(lambda) || lambda <= 0) return ArcFailure(SvgGeometryError.NonFiniteResult);
        double numerator = 1 - lambda;
        if (numerator < 0) numerator = 0;
        double coefficient = System.Math.Sqrt(numerator / lambda);
        if (largeArc == sweep) coefficient = -coefficient;
        double centerPrimeX = coefficient * (transformedY / radiusY) * radiusX;
        double centerPrimeY = -coefficient * (transformedX / radiusX) * radiusY;
        SvgFiniteResult midpointX = HalfSum(start.X, end.X);
        SvgFiniteResult midpointY = HalfSum(start.Y, end.Y);
        if (!SvgFiniteMath.IsFinite(centerPrimeX) || !SvgFiniteMath.IsFinite(centerPrimeY) || !midpointX.Success || !midpointY.Success) return ArcFailure(SvgGeometryError.NonFiniteResult);
        double centerX = cosine * centerPrimeX - sine * centerPrimeY + midpointX.Value;
        double centerY = sine * centerPrimeX + cosine * centerPrimeY + midpointY.Value;
        if (!SvgFiniteMath.IsFinite(centerX) || !SvgFiniteMath.IsFinite(centerY)) return ArcFailure(SvgGeometryError.NonFiniteResult);

        double unitStartX = transformedX / radiusX - centerPrimeX / radiusX;
        double unitStartY = transformedY / radiusY - centerPrimeY / radiusY;
        double unitEndX = -transformedX / radiusX - centerPrimeX / radiusX;
        double unitEndY = -transformedY / radiusY - centerPrimeY / radiusY;
        double startAngle = System.Math.Atan2(unitStartY, unitStartX);
        double cross = unitStartX * unitEndY - unitStartY * unitEndX;
        double dot = unitStartX * unitEndX + unitStartY * unitEndY;
        double sweepAngle = System.Math.Atan2(cross, dot);
        double fullTurn = System.Math.PI * 2;
        if (!sweep && sweepAngle > 0) sweepAngle = sweepAngle - fullTurn;
        if (sweep && sweepAngle < 0) sweepAngle = sweepAngle + fullTurn;
        if (!SvgFiniteMath.IsFinite(startAngle) || !SvgFiniteMath.IsFinite(sweepAngle)) return ArcFailure(SvgGeometryError.NonFiniteResult);
        SvgArc arc = new SvgArc(startCopy, endCopy, SvgFiniteMath.NormalizeZero(centerX), SvgFiniteMath.NormalizeZero(centerY), SvgFiniteMath.NormalizeZero(radiusX), SvgFiniteMath.NormalizeZero(radiusY), SvgFiniteMath.NormalizeZero(rotation), SvgFiniteMath.NormalizeZero(startAngle), SvgFiniteMath.NormalizeZero(sweepAngle), false, false);
        return ArcSuccess(arc);
    }

    internal static SvgPointOperationResult Evaluate(SvgArc arc, double amount)
    {
        if (!ValidArc(arc) || !SvgFiniteMath.IsFinite(amount) || amount < 0 || amount > 1) return PointFailure(SvgGeometryError.InvalidArgument);
        if (amount == 0) return PointSuccess(arc.Start);
        if (amount == 1) return PointSuccess(arc.End);
        if (arc.IsPoint) return PointSuccess(arc.Start);
        if (arc.IsLine)
        {
            SvgFiniteResult x = SvgFiniteMath.Lerp(arc.Start.X, arc.End.X, amount);
            SvgFiniteResult y = SvgFiniteMath.Lerp(arc.Start.Y, arc.End.Y, amount);
            if (!x.Success || !y.Success) return PointFailure(SvgGeometryError.NonFiniteResult);
            return new SvgPointOperationResult(true, SvgGeometryError.None, new SvgPoint(x.Value, y.Value));
        }
        double angle = arc.StartAngle + arc.SweepAngle * amount;
        if (!SvgFiniteMath.IsFinite(angle)) return PointFailure(SvgGeometryError.NonFiniteResult);
        return EvaluateAngle(arc, angle);
    }

    internal static SvgArcSplitResult Split(SvgArc arc, double amount)
    {
        if (!ValidArc(arc) || !SvgFiniteMath.IsFinite(amount) || amount < 0 || amount > 1) return SplitFailure(SvgGeometryError.InvalidArgument);
        SvgPointOperationResult middle = Evaluate(arc, amount);
        if (!middle.Success) return SplitFailure(middle.Error);
        if (arc.IsPoint)
        {
            SvgArc firstPoint = PointArc(new SvgPoint(arc.Start.X, arc.Start.Y), arc.RadiusX, arc.RadiusY, arc.Rotation);
            SvgArc secondPoint = PointArc(new SvgPoint(arc.Start.X, arc.Start.Y), arc.RadiusX, arc.RadiusY, arc.Rotation);
            return new SvgArcSplitResult(true, SvgGeometryError.None, firstPoint, secondPoint);
        }
        if (arc.IsLine)
        {
            SvgArc firstLine = LineArc(new SvgPoint(arc.Start.X, arc.Start.Y), new SvgPoint((middle.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, middle.Point.Y), arc.RadiusX, arc.RadiusY, arc.Rotation);
            SvgArc secondLine = LineArc(new SvgPoint(middle.Point.X, middle.Point.Y), new SvgPoint(arc.End.X, arc.End.Y), arc.RadiusX, arc.RadiusY, arc.Rotation);
            return new SvgArcSplitResult(true, SvgGeometryError.None, firstLine, secondLine);
        }
        double firstSweep = arc.SweepAngle * amount;
        double secondStart = arc.StartAngle + firstSweep;
        double secondSweep = arc.SweepAngle - firstSweep;
        if (!SvgFiniteMath.IsFinite(firstSweep) || !SvgFiniteMath.IsFinite(secondStart) || !SvgFiniteMath.IsFinite(secondSweep)) return SplitFailure(SvgGeometryError.NonFiniteResult);
        SvgArc first = CenterArc(new SvgPoint(arc.Start.X, arc.Start.Y), new SvgPoint((middle.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, middle.Point.Y), arc, arc.StartAngle, firstSweep);
        SvgArc second = CenterArc(new SvgPoint(middle.Point.X, middle.Point.Y), new SvgPoint(arc.End.X, arc.End.Y), arc, secondStart, secondSweep);
        return new SvgArcSplitResult(true, SvgGeometryError.None, first, second);
    }

    internal static SvgArcCubicResult ToCubics(SvgArc arc)
    {
        if (!ValidArc(arc)) return CubicFailure(SvgGeometryError.InvalidArgument);
        System.Collections.Generic.List<SvgCubicBezier> curves = new System.Collections.Generic.List<SvgCubicBezier>();
        if (arc.IsPoint) return new SvgArcCubicResult(true, SvgGeometryError.None, curves);
        if (arc.IsLine)
        {
            SvgFiniteResult control1X = Weighted(arc.Start.X, arc.End.X, 1);
            SvgFiniteResult control1Y = Weighted(arc.Start.Y, arc.End.Y, 1);
            SvgFiniteResult control2X = Weighted(arc.Start.X, arc.End.X, 2);
            SvgFiniteResult control2Y = Weighted(arc.Start.Y, arc.End.Y, 2);
            if (!control1X.Success || !control1Y.Success || !control2X.Success || !control2Y.Success) return CubicFailure(SvgGeometryError.NonFiniteResult);
            curves.Add(new SvgCubicBezier(arc.Start, new SvgPoint(control1X.Value, control1Y.Value), new SvgPoint(control2X.Value, control2Y.Value), arc.End));
            return new SvgArcCubicResult(true, SvgGeometryError.None, curves);
        }
        if (arc.SweepAngle == 0) return new SvgArcCubicResult(true, SvgGeometryError.None, curves);
        int segmentCount = (int)System.Math.Ceiling(System.Math.Abs(arc.SweepAngle) / (System.Math.PI / 2));
        if (segmentCount < 1) segmentCount = 1;
        double segmentSweep = arc.SweepAngle / segmentCount;
        int index = 0;
        while (index < segmentCount)
        {
            double angle0 = arc.StartAngle + segmentSweep * index;
            double angle1 = angle0 + segmentSweep;
            SvgPointOperationResult calculatedStart = EvaluateAngle(arc, angle0);
            SvgPointOperationResult calculatedEnd = EvaluateAngle(arc, angle1);
            if (!calculatedStart.Success || !calculatedEnd.Success) return CubicFailure(SvgGeometryError.NonFiniteResult);
            SvgPoint? start = index == 0 ? new SvgPoint(arc.Start.X, arc.Start.Y) : calculatedStart.Point;
            SvgPoint? end = index == segmentCount - 1 ? new SvgPoint(arc.End.X, arc.End.Y) : calculatedEnd.Point;
            double alpha = 4.0 / 3.0 * System.Math.Tan(segmentSweep / 4);
            double cosineRotation = System.Math.Cos(arc.Rotation);
            double sineRotation = System.Math.Sin(arc.Rotation);
            double derivative0X = -arc.RadiusX * cosineRotation * System.Math.Sin(angle0) - arc.RadiusY * sineRotation * System.Math.Cos(angle0);
            double derivative0Y = -arc.RadiusX * sineRotation * System.Math.Sin(angle0) + arc.RadiusY * cosineRotation * System.Math.Cos(angle0);
            double derivative1X = -arc.RadiusX * cosineRotation * System.Math.Sin(angle1) - arc.RadiusY * sineRotation * System.Math.Cos(angle1);
            double derivative1Y = -arc.RadiusX * sineRotation * System.Math.Sin(angle1) + arc.RadiusY * cosineRotation * System.Math.Cos(angle1);
            double control1X = (start ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X + alpha * derivative0X;
            double control1Y = start.Y + alpha * derivative0Y;
            double control2X = (end ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X - alpha * derivative1X;
            double control2Y = end.Y - alpha * derivative1Y;
            if (!SvgFiniteMath.IsFinite(alpha) || !SvgFiniteMath.IsFinite(control1X) || !SvgFiniteMath.IsFinite(control1Y) || !SvgFiniteMath.IsFinite(control2X) || !SvgFiniteMath.IsFinite(control2Y)) return CubicFailure(SvgGeometryError.NonFiniteResult);
            curves.Add(new SvgCubicBezier(start, new SvgPoint(control1X, control1Y), new SvgPoint(control2X, control2Y), end));
            index = index + 1;
        }
        return new SvgArcCubicResult(true, SvgGeometryError.None, curves);
    }

    internal static bool ValidPoint(SvgPoint? point)
    {
        return point != null && SvgFiniteMath.IsFinite(point.X) && SvgFiniteMath.IsFinite(point.Y);
    }

    internal static SvgArcOperationResult ArcFailure(SvgGeometryError error)
    {
        return new SvgArcOperationResult(false, error, null);
    }

    internal static bool ValidArc(SvgArc? arc)
    {
        if (arc == null || !ValidPoint(arc.Start) || !ValidPoint(arc.End)) return false;
        if (!SvgFiniteMath.IsFinite(arc.CenterX) || !SvgFiniteMath.IsFinite(arc.CenterY) || !SvgFiniteMath.IsFinite(arc.RadiusX) || !SvgFiniteMath.IsFinite(arc.RadiusY) || !SvgFiniteMath.IsFinite(arc.Rotation) || !SvgFiniteMath.IsFinite(arc.StartAngle) || !SvgFiniteMath.IsFinite(arc.SweepAngle)) return false;
        if (arc.RadiusX < 0 || arc.RadiusY < 0 || arc.IsLine && arc.IsPoint) return false;
        if (!arc.IsLine && !arc.IsPoint && (arc.RadiusX == 0 || arc.RadiusY == 0)) return false;
        return true;
    }

    static SvgPointOperationResult EvaluateAngle(SvgArc arc, double angle)
    {
        double cosineAngle = System.Math.Cos(angle);
        double sineAngle = System.Math.Sin(angle);
        double cosineRotation = System.Math.Cos(arc.Rotation);
        double sineRotation = System.Math.Sin(arc.Rotation);
        double x = arc.CenterX + arc.RadiusX * cosineRotation * cosineAngle - arc.RadiusY * sineRotation * sineAngle;
        double y = arc.CenterY + arc.RadiusX * sineRotation * cosineAngle + arc.RadiusY * cosineRotation * sineAngle;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return PointFailure(SvgGeometryError.NonFiniteResult);
        return new SvgPointOperationResult(true, SvgGeometryError.None, new SvgPoint(x, y));
    }

    static SvgArc CenterArc(SvgPoint start, SvgPoint end, SvgArc source, double startAngle, double sweepAngle)
    {
        return new SvgArc(start, end, source.CenterX, source.CenterY, source.RadiusX, source.RadiusY, source.Rotation, SvgFiniteMath.NormalizeZero(startAngle), SvgFiniteMath.NormalizeZero(sweepAngle), false, false);
    }

    static SvgArc PointArc(SvgPoint point, double radiusX, double radiusY, double rotation)
    {
        SvgPoint start = new SvgPoint(point.X, point.Y);
        SvgPoint end = new SvgPoint(point.X, point.Y);
        return new SvgArc(start, end, point.X, point.Y, radiusX, radiusY, rotation, 0, 0, false, true);
    }

    static SvgArc LineArc(SvgPoint start, SvgPoint end, double radiusX, double radiusY, double rotation)
    {
        return new SvgArc(start, end, 0, 0, radiusX, radiusY, rotation, 0, 0, true, false);
    }

    static SvgArcOperationResult ArcSuccess(SvgArc arc)
    {
        return new SvgArcOperationResult(true, SvgGeometryError.None, arc);
    }

    static SvgPointOperationResult PointSuccess(SvgPoint point)
    {
        return new SvgPointOperationResult(true, SvgGeometryError.None, new SvgPoint(point.X, point.Y));
    }

    static SvgPointOperationResult PointFailure(SvgGeometryError error)
    {
        return new SvgPointOperationResult(false, error, null);
    }

    static SvgArcSplitResult SplitFailure(SvgGeometryError error)
    {
        return new SvgArcSplitResult(false, error, null, null);
    }

    static SvgArcCubicResult CubicFailure(SvgGeometryError error)
    {
        return new SvgArcCubicResult(false, error, null);
    }

    static SvgFiniteResult HalfDifference(double left, double right)
    {
        return SvgFiniteMath.Subtract(left / 2, right / 2);
    }

    static SvgFiniteResult HalfSum(double left, double right)
    {
        return SvgFiniteMath.Add(left / 2, right / 2);
    }

    static double Hypotenuse(double x, double y)
    {
        double absoluteX = System.Math.Abs(x);
        double absoluteY = System.Math.Abs(y);
        double maximum = System.Math.Max(absoluteX, absoluteY);
        if (!SvgFiniteMath.IsFinite(maximum)) return maximum;
        if (maximum == 0) return 0;
        double minimum = System.Math.Min(absoluteX, absoluteY);
        double ratio = minimum / maximum;
        return maximum * System.Math.Sqrt(1 + ratio * ratio);
    }

    static SvgFiniteResult Weighted(double start, double end, int endWeight)
    {
        double startWeight = 3 - endWeight;
        double value = start / 3 * startWeight + end / 3 * endWeight;
        if (!SvgFiniteMath.IsFinite(value)) return new SvgFiniteResult(false, 0, "Cubic line control is not finite.");
        return new SvgFiniteResult(true, SvgFiniteMath.NormalizeZero(value), "");
    }
}
