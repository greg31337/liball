namespace svg;

internal class SvgPointCalculation
{
    internal bool Success;
    internal SvgPoint? Point;

    internal SvgPointCalculation(bool success, SvgPoint? point)
    {
        Success = success;
        Point = point;
    }
}

internal class SvgBoundsAccumulator
{
    internal bool HasPoint;
    internal double MinimumX;
    internal double MinimumY;
    internal double MaximumX;
    internal double MaximumY;

    internal SvgBoundsAccumulator()
    {
        HasPoint = false;
        MinimumX = 0;
        MinimumY = 0;
        MaximumX = 0;
        MaximumY = 0;
    }

    internal void Add(SvgPoint? point)
    {
        if (!HasPoint)
        {
            MinimumX = (point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
            MinimumY = point.Y;
            MaximumX = point.X;
            MaximumY = point.Y;
            HasPoint = true;
            return;
        }
        if ((point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X < MinimumX) MinimumX = point.X;
        if (point.Y < MinimumY) MinimumY = point.Y;
        if (point.X > MaximumX) MaximumX = point.X;
        if (point.Y > MaximumY) MaximumY = point.Y;
    }

    internal SvgRectOperationResult Finish()
    {
        if (!HasPoint) return new SvgRectOperationResult(true, SvgGeometryError.None, SvgRect.Create(0, 0, 0, 0, true));
        SvgFiniteResult width = SvgFiniteMath.Subtract(MaximumX, MinimumX);
        SvgFiniteResult height = SvgFiniteMath.Subtract(MaximumY, MinimumY);
        if (!width.Success || !height.Success) return RectFailure(SvgGeometryError.NonFiniteResult);
        return new SvgRectOperationResult(true, SvgGeometryError.None, new SvgRect(MinimumX, MinimumY, width.Value, height.Value));
    }

    internal static SvgRectOperationResult RectFailure(SvgGeometryError error)
    {
        return new SvgRectOperationResult(false, error, null);
    }
}

internal static class SvgCurveMath
{
    internal static SvgPointOperationResult Evaluate(SvgLineSegment line, double amount)
    {
        if (!ValidLine(line) || !ValidAmount(amount)) return PointFailure(SvgGeometryError.InvalidArgument);
        SvgPointCalculation point = Interpolate(line.Start, line.End, amount);
        return PointResult(point);
    }

    internal static SvgPointOperationResult Evaluate(SvgQuadraticBezier quadratic, double amount)
    {
        if (!ValidQuadratic(quadratic) || !ValidAmount(amount)) return PointFailure(SvgGeometryError.InvalidArgument);
        SvgPointCalculation first = Interpolate(quadratic.Start, quadratic.Control, amount);
        SvgPointCalculation second = Interpolate(quadratic.Control, quadratic.End, amount);
        if (!first.Success || !second.Success) return PointFailure(SvgGeometryError.NonFiniteResult);
        return PointResult(Interpolate(first.Point, second.Point, amount));
    }

    internal static SvgPointOperationResult Evaluate(SvgCubicBezier cubic, double amount)
    {
        if (!ValidCubic(cubic) || !ValidAmount(amount)) return PointFailure(SvgGeometryError.InvalidArgument);
        SvgPointCalculation first = Interpolate(cubic.Start, cubic.Control1, amount);
        SvgPointCalculation second = Interpolate(cubic.Control1, cubic.Control2, amount);
        SvgPointCalculation third = Interpolate(cubic.Control2, cubic.End, amount);
        if (!first.Success || !second.Success || !third.Success) return PointFailure(SvgGeometryError.NonFiniteResult);
        SvgPointCalculation fourth = Interpolate(first.Point, second.Point, amount);
        SvgPointCalculation fifth = Interpolate(second.Point, third.Point, amount);
        if (!fourth.Success || !fifth.Success) return PointFailure(SvgGeometryError.NonFiniteResult);
        return PointResult(Interpolate(fourth.Point, fifth.Point, amount));
    }

    internal static SvgVectorOperationResult Derivative(SvgLineSegment line, double amount)
    {
        if (!ValidLine(line) || !ValidAmount(amount)) return VectorFailure(SvgGeometryError.InvalidArgument);
        return Difference(line.End, line.Start, 1);
    }

    internal static SvgVectorOperationResult Derivative(SvgQuadraticBezier quadratic, double amount)
    {
        if (!ValidQuadratic(quadratic) || !ValidAmount(amount)) return VectorFailure(SvgGeometryError.InvalidArgument);
        SvgVectorOperationResult first = Difference(quadratic.Control, quadratic.Start, 1);
        SvgVectorOperationResult second = Difference(quadratic.End, quadratic.Control, 1);
        if (!first.Success || !second.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        SvgFiniteResult x = SvgFiniteMath.Lerp((first.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (second.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, amount);
        SvgFiniteResult y = SvgFiniteMath.Lerp(first.Vector.Y, second.Vector.Y, amount);
        if (!x.Success || !y.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return Vector(x.Value, y.Value, 2);
    }

    internal static SvgVectorOperationResult Derivative(SvgCubicBezier cubic, double amount)
    {
        if (!ValidCubic(cubic) || !ValidAmount(amount)) return VectorFailure(SvgGeometryError.InvalidArgument);
        SvgVectorOperationResult first = Difference(cubic.Control1, cubic.Start, 1);
        SvgVectorOperationResult second = Difference(cubic.Control2, cubic.Control1, 1);
        SvgVectorOperationResult third = Difference(cubic.End, cubic.Control2, 1);
        if (!first.Success || !second.Success || !third.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        SvgFiniteResult firstX = SvgFiniteMath.Lerp((first.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (second.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, amount);
        SvgFiniteResult firstY = SvgFiniteMath.Lerp(first.Vector.Y, second.Vector.Y, amount);
        SvgFiniteResult secondX = SvgFiniteMath.Lerp(second.Vector.X, (third.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, amount);
        SvgFiniteResult secondY = SvgFiniteMath.Lerp(second.Vector.Y, third.Vector.Y, amount);
        if (!firstX.Success || !firstY.Success || !secondX.Success || !secondY.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        SvgFiniteResult x = SvgFiniteMath.Lerp(firstX.Value, secondX.Value, amount);
        SvgFiniteResult y = SvgFiniteMath.Lerp(firstY.Value, secondY.Value, amount);
        if (!x.Success || !y.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return Vector(x.Value, y.Value, 3);
    }

    internal static SvgVectorOperationResult Derivative(SvgArc arc, double amount)
    {
        if (!SvgArcMath.ValidArc(arc) || !ValidAmount(amount)) return VectorFailure(SvgGeometryError.InvalidArgument);
        if (arc.IsPoint) return new SvgVectorOperationResult(true, SvgGeometryError.None, new SvgVector(0, 0));
        if (arc.IsLine) return Difference(arc.End, arc.Start, 1);
        double angle = arc.StartAngle + arc.SweepAngle * amount;
        double cosineAngle = System.Math.Cos(angle);
        double sineAngle = System.Math.Sin(angle);
        double cosineRotation = System.Math.Cos(arc.Rotation);
        double sineRotation = System.Math.Sin(arc.Rotation);
        double x = (-arc.RadiusX * cosineRotation * sineAngle - arc.RadiusY * sineRotation * cosineAngle) * arc.SweepAngle;
        double y = (-arc.RadiusX * sineRotation * sineAngle + arc.RadiusY * cosineRotation * cosineAngle) * arc.SweepAngle;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return new SvgVectorOperationResult(true, SvgGeometryError.None, new SvgVector(x, y));
    }

    internal static SvgLineSplitResult Split(SvgLineSegment line, double amount)
    {
        if (!ValidLine(line) || !ValidAmount(amount)) return new SvgLineSplitResult(false, SvgGeometryError.InvalidArgument, null, null);
        SvgPointCalculation middle = Interpolate(line.Start, line.End, amount);
        if (!middle.Success) return new SvgLineSplitResult(false, SvgGeometryError.NonFiniteResult, null, null);
        return new SvgLineSplitResult(true, SvgGeometryError.None, new SvgLineSegment(line.Start, (middle.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))), new SvgLineSegment(middle.Point, line.End));
    }

    internal static SvgQuadraticSplitResult Split(SvgQuadraticBezier quadratic, double amount)
    {
        if (!ValidQuadratic(quadratic) || !ValidAmount(amount)) return new SvgQuadraticSplitResult(false, SvgGeometryError.InvalidArgument, null, null);
        SvgPointCalculation first = Interpolate(quadratic.Start, quadratic.Control, amount);
        SvgPointCalculation second = Interpolate(quadratic.Control, quadratic.End, amount);
        if (!first.Success || !second.Success) return new SvgQuadraticSplitResult(false, SvgGeometryError.NonFiniteResult, null, null);
        SvgPointCalculation middle = Interpolate(first.Point, second.Point, amount);
        if (!middle.Success) return new SvgQuadraticSplitResult(false, SvgGeometryError.NonFiniteResult, null, null);
        return new SvgQuadraticSplitResult(true, SvgGeometryError.None, new SvgQuadraticBezier(quadratic.Start, (first.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (middle.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))), new SvgQuadraticBezier(middle.Point, (second.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), quadratic.End));
    }

    internal static SvgCubicSplitResult Split(SvgCubicBezier cubic, double amount)
    {
        if (!ValidCubic(cubic) || !ValidAmount(amount)) return new SvgCubicSplitResult(false, SvgGeometryError.InvalidArgument, null, null);
        SvgPointCalculation first = Interpolate(cubic.Start, cubic.Control1, amount);
        SvgPointCalculation second = Interpolate(cubic.Control1, cubic.Control2, amount);
        SvgPointCalculation third = Interpolate(cubic.Control2, cubic.End, amount);
        if (!first.Success || !second.Success || !third.Success) return new SvgCubicSplitResult(false, SvgGeometryError.NonFiniteResult, null, null);
        SvgPointCalculation fourth = Interpolate(first.Point, second.Point, amount);
        SvgPointCalculation fifth = Interpolate(second.Point, third.Point, amount);
        if (!fourth.Success || !fifth.Success) return new SvgCubicSplitResult(false, SvgGeometryError.NonFiniteResult, null, null);
        SvgPointCalculation middle = Interpolate(fourth.Point, fifth.Point, amount);
        if (!middle.Success) return new SvgCubicSplitResult(false, SvgGeometryError.NonFiniteResult, null, null);
        SvgCubicBezier left = new SvgCubicBezier(cubic.Start, (first.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (fourth.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (middle.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        SvgCubicBezier right = new SvgCubicBezier(middle.Point, (fifth.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (third.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), cubic.End);
        return new SvgCubicSplitResult(true, SvgGeometryError.None, left, right);
    }

    internal static SvgLineOperationResult Reverse(SvgLineSegment line)
    {
        if (!ValidLine(line)) return new SvgLineOperationResult(false, SvgGeometryError.InvalidArgument, null);
        return new SvgLineOperationResult(true, SvgGeometryError.None, new SvgLineSegment(line.End, line.Start));
    }

    internal static SvgQuadraticOperationResult Reverse(SvgQuadraticBezier quadratic)
    {
        if (!ValidQuadratic(quadratic)) return new SvgQuadraticOperationResult(false, SvgGeometryError.InvalidArgument, null);
        return new SvgQuadraticOperationResult(true, SvgGeometryError.None, new SvgQuadraticBezier(quadratic.End, quadratic.Control, quadratic.Start));
    }

    internal static SvgCubicOperationResult Reverse(SvgCubicBezier cubic)
    {
        if (!ValidCubic(cubic)) return new SvgCubicOperationResult(false, SvgGeometryError.InvalidArgument, null);
        return new SvgCubicOperationResult(true, SvgGeometryError.None, new SvgCubicBezier(cubic.End, cubic.Control2, cubic.Control1, cubic.Start));
    }

    internal static SvgArcOperationResult Reverse(SvgArc arc)
    {
        if (!SvgArcMath.ValidArc(arc)) return SvgArcMath.ArcFailure(SvgGeometryError.InvalidArgument);
        double startAngle = arc.StartAngle + arc.SweepAngle;
        double sweepAngle = -arc.SweepAngle;
        if (!SvgFiniteMath.IsFinite(startAngle) || !SvgFiniteMath.IsFinite(sweepAngle)) return SvgArcMath.ArcFailure(SvgGeometryError.NonFiniteResult);
        SvgArc reversed = new SvgArc(new SvgPoint(arc.End.X, arc.End.Y), new SvgPoint(arc.Start.X, arc.Start.Y), arc.CenterX, arc.CenterY, arc.RadiusX, arc.RadiusY, arc.Rotation, SvgFiniteMath.NormalizeZero(startAngle), SvgFiniteMath.NormalizeZero(sweepAngle), arc.IsLine, arc.IsPoint);
        return new SvgArcOperationResult(true, SvgGeometryError.None, reversed);
    }

    internal static SvgPolynomialRootResult SolveLinear(double a, double b)
    {
        if (!SvgFiniteMath.IsFinite(a) || !SvgFiniteMath.IsFinite(b)) return RootFailure(SvgGeometryError.InvalidArgument);
        System.Collections.Generic.List<double> roots = new System.Collections.Generic.List<double>();
        if (a == 0) return RootSuccess(roots);
        double root = -b / a;
        if (!SvgFiniteMath.IsFinite(root)) return RootFailure(SvgGeometryError.NonFiniteResult);
        roots.Add(SvgFiniteMath.NormalizeZero(root));
        return RootSuccess(roots);
    }

    internal static SvgPolynomialRootResult SolveQuadratic(double a, double b, double c)
    {
        if (!SvgFiniteMath.IsFinite(a) || !SvgFiniteMath.IsFinite(b) || !SvgFiniteMath.IsFinite(c)) return RootFailure(SvgGeometryError.InvalidArgument);
        if (a == 0) return SolveLinear(b, c);
        double scale = System.Math.Max(System.Math.Abs(a), System.Math.Max(System.Math.Abs(b), System.Math.Abs(c)));
        if (scale == 0) return RootSuccess(new System.Collections.Generic.List<double>());
        double scaledA = a / scale;
        double scaledB = b / scale;
        double scaledC = c / scale;
        double discriminant = scaledB * scaledB - 4 * scaledA * scaledC;
        System.Collections.Generic.List<double> roots = new System.Collections.Generic.List<double>();
        if (discriminant < 0) return RootSuccess(roots);
        double squareRoot = System.Math.Sqrt(discriminant);
        if (!SvgFiniteMath.IsFinite(squareRoot)) return RootFailure(SvgGeometryError.NonFiniteResult);
        if (squareRoot == 0)
        {
            double repeated = -scaledB / (2 * scaledA);
            if (!SvgFiniteMath.IsFinite(repeated)) return RootFailure(SvgGeometryError.NonFiniteResult);
            roots.Add(SvgFiniteMath.NormalizeZero(repeated));
            return RootSuccess(roots);
        }
        double signedSquareRoot = scaledB >= 0 ? squareRoot : -squareRoot;
        double q = -0.5 * (scaledB + signedSquareRoot);
        double first = q / scaledA;
        double second = scaledC / q;
        if (!SvgFiniteMath.IsFinite(first) || !SvgFiniteMath.IsFinite(second)) return RootFailure(SvgGeometryError.NonFiniteResult);
        first = SvgFiniteMath.NormalizeZero(first);
        second = SvgFiniteMath.NormalizeZero(second);
        if (first > second)
        {
            double temporary = first;
            first = second;
            second = temporary;
        }
        roots.Add(first);
        if (!NearlyEqual(first, second)) roots.Add(second);
        return RootSuccess(roots);
    }

    internal static SvgRectOperationResult GetBounds(SvgLineSegment line)
    {
        if (!ValidLine(line)) return SvgBoundsAccumulator.RectFailure(SvgGeometryError.InvalidArgument);
        SvgBoundsAccumulator bounds = new SvgBoundsAccumulator();
        bounds.Add(line.Start);
        bounds.Add(line.End);
        return bounds.Finish();
    }

    internal static SvgRectOperationResult GetBounds(SvgQuadraticBezier quadratic)
    {
        if (!ValidQuadratic(quadratic)) return SvgBoundsAccumulator.RectFailure(SvgGeometryError.InvalidArgument);
        SvgBoundsAccumulator bounds = new SvgBoundsAccumulator();
        bounds.Add(quadratic.Start);
        bounds.Add(quadratic.End);
        SvgPolynomialRootResult xRoots = QuadraticExtrema(quadratic.Start.X, quadratic.Control.X, quadratic.End.X);
        SvgPolynomialRootResult yRoots = QuadraticExtrema(quadratic.Start.Y, quadratic.Control.Y, quadratic.End.Y);
        if (!xRoots.Success || !yRoots.Success) return SvgBoundsAccumulator.RectFailure(SvgGeometryError.NonFiniteResult);
        SvgGeometryError added = AddParameters(bounds, quadratic, xRoots.Roots);
        if (added == SvgGeometryError.None) added = AddParameters(bounds, quadratic, yRoots.Roots);
        if (added != SvgGeometryError.None) return SvgBoundsAccumulator.RectFailure(added);
        return bounds.Finish();
    }

    internal static SvgRectOperationResult GetBounds(SvgCubicBezier cubic)
    {
        if (!ValidCubic(cubic)) return SvgBoundsAccumulator.RectFailure(SvgGeometryError.InvalidArgument);
        SvgBoundsAccumulator bounds = new SvgBoundsAccumulator();
        bounds.Add(cubic.Start);
        bounds.Add(cubic.End);
        SvgPolynomialRootResult xRoots = CubicExtrema(cubic.Start.X, cubic.Control1.X, cubic.Control2.X, cubic.End.X);
        SvgPolynomialRootResult yRoots = CubicExtrema(cubic.Start.Y, cubic.Control1.Y, cubic.Control2.Y, cubic.End.Y);
        if (!xRoots.Success || !yRoots.Success) return SvgBoundsAccumulator.RectFailure(SvgGeometryError.NonFiniteResult);
        SvgGeometryError added = AddParameters(bounds, cubic, xRoots.Roots);
        if (added == SvgGeometryError.None) added = AddParameters(bounds, cubic, yRoots.Roots);
        if (added != SvgGeometryError.None) return SvgBoundsAccumulator.RectFailure(added);
        return bounds.Finish();
    }

    internal static SvgRectOperationResult GetBounds(SvgArc arc)
    {
        if (!SvgArcMath.ValidArc(arc)) return SvgBoundsAccumulator.RectFailure(SvgGeometryError.InvalidArgument);
        SvgBoundsAccumulator bounds = new SvgBoundsAccumulator();
        bounds.Add(arc.Start);
        bounds.Add(arc.End);
        if (arc.IsPoint || arc.IsLine || arc.SweepAngle == 0) return bounds.Finish();
        double cosine = System.Math.Cos(arc.Rotation);
        double sine = System.Math.Sin(arc.Rotation);
        double xAngle = System.Math.Atan2(-arc.RadiusY * sine, arc.RadiusX * cosine);
        double yAngle = System.Math.Atan2(arc.RadiusY * cosine, arc.RadiusX * sine);
        SvgGeometryError error = AddArcAngle(bounds, arc, xAngle);
        if (error == SvgGeometryError.None) error = AddArcAngle(bounds, arc, xAngle + System.Math.PI);
        if (error == SvgGeometryError.None) error = AddArcAngle(bounds, arc, yAngle);
        if (error == SvgGeometryError.None) error = AddArcAngle(bounds, arc, yAngle + System.Math.PI);
        if (error != SvgGeometryError.None) return SvgBoundsAccumulator.RectFailure(error);
        return bounds.Finish();
    }

    internal static bool ValidPoint(SvgPoint? point)
    {
        return point != null && SvgFiniteMath.IsFinite(point.X) && SvgFiniteMath.IsFinite(point.Y);
    }

    static bool ValidLine(SvgLineSegment line) { return line != null && ValidPoint(line.Start) && ValidPoint(line.End); }
    static bool ValidQuadratic(SvgQuadraticBezier quadratic) { return quadratic != null && ValidPoint(quadratic.Start) && ValidPoint(quadratic.Control) && ValidPoint(quadratic.End); }
    static bool ValidCubic(SvgCubicBezier cubic) { return cubic != null && ValidPoint(cubic.Start) && ValidPoint(cubic.Control1) && ValidPoint(cubic.Control2) && ValidPoint(cubic.End); }
    static bool ValidAmount(double amount) { return SvgFiniteMath.IsFinite(amount) && amount >= 0 && amount <= 1; }

    static SvgPointCalculation Interpolate(SvgPoint? start, SvgPoint? end, double amount)
    {
        SvgFiniteResult x = SvgFiniteMath.Lerp((start ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (end ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, amount);
        SvgFiniteResult y = SvgFiniteMath.Lerp(start.Y, end.Y, amount);
        if (!x.Success || !y.Success) return new SvgPointCalculation(false, null);
        return new SvgPointCalculation(true, new SvgPoint(x.Value, y.Value));
    }

    static SvgPointOperationResult PointResult(SvgPointCalculation point)
    {
        if (!point.Success) return PointFailure(SvgGeometryError.NonFiniteResult);
        return new SvgPointOperationResult(true, SvgGeometryError.None, point.Point);
    }

    static SvgVectorOperationResult Difference(SvgPoint left, SvgPoint right, double factor)
    {
        SvgFiniteResult x = SvgFiniteMath.Subtract(left.X, right.X);
        SvgFiniteResult y = SvgFiniteMath.Subtract(left.Y, right.Y);
        if (!x.Success || !y.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return Vector(x.Value, y.Value, factor);
    }

    static SvgVectorOperationResult Vector(double x, double y, double factor)
    {
        SvgFiniteResult scaledX = SvgFiniteMath.Multiply(x, factor);
        SvgFiniteResult scaledY = SvgFiniteMath.Multiply(y, factor);
        if (!scaledX.Success || !scaledY.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return new SvgVectorOperationResult(true, SvgGeometryError.None, new SvgVector(scaledX.Value, scaledY.Value));
    }

    static SvgPolynomialRootResult QuadraticExtrema(double start, double control, double end)
    {
        double scale = System.Math.Max(System.Math.Abs(start), System.Math.Max(System.Math.Abs(control), System.Math.Abs(end)));
        if (scale == 0) return RootSuccess(new System.Collections.Generic.List<double>());
        double normalizedStart = start / scale;
        double normalizedControl = control / scale;
        double normalizedEnd = end / scale;
        double a = normalizedStart - 2 * normalizedControl + normalizedEnd;
        double b = normalizedControl - normalizedStart;
        return SolveLinear(a, b);
    }

    static SvgPolynomialRootResult CubicExtrema(double start, double control1, double control2, double end)
    {
        double scale = System.Math.Max(System.Math.Abs(start), System.Math.Max(System.Math.Abs(control1), System.Math.Max(System.Math.Abs(control2), System.Math.Abs(end))));
        if (scale == 0) return RootSuccess(new System.Collections.Generic.List<double>());
        double p0 = start / scale;
        double p1 = control1 / scale;
        double p2 = control2 / scale;
        double p3 = end / scale;
        double a = -p0 + 3 * p1 - 3 * p2 + p3;
        double b = 2 * (p0 - 2 * p1 + p2);
        double c = p1 - p0;
        return SolveQuadratic(a, b, c);
    }

    static SvgGeometryError AddParameters(SvgBoundsAccumulator bounds, SvgQuadraticBezier quadratic, System.Collections.Generic.List<double>? roots)
    {
        int index = 0;
        while (index < (roots ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            double amount = roots[index];
            if (amount > 0 && amount < 1)
            {
                SvgPointOperationResult point = Evaluate(quadratic, amount);
                if (!point.Success) return point.Error;
                bounds.Add(point.Point);
            }
            index = index + 1;
        }
        return SvgGeometryError.None;
    }

    static SvgGeometryError AddParameters(SvgBoundsAccumulator bounds, SvgCubicBezier cubic, System.Collections.Generic.List<double>? roots)
    {
        int index = 0;
        while (index < (roots ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            double amount = roots[index];
            if (amount > 0 && amount < 1)
            {
                SvgPointOperationResult point = Evaluate(cubic, amount);
                if (!point.Success) return point.Error;
                bounds.Add(point.Point);
            }
            index = index + 1;
        }
        return SvgGeometryError.None;
    }

    static SvgGeometryError AddArcAngle(SvgBoundsAccumulator bounds, SvgArc arc, double angle)
    {
        double fullTurn = System.Math.PI * 2;
        double distance;
        if (arc.SweepAngle >= 0)
        {
            distance = (angle - arc.StartAngle) % fullTurn;
        }
        else
        {
            distance = (arc.StartAngle - angle) % fullTurn;
        }
        if (distance < 0) distance = distance + fullTurn;
        double sweep = System.Math.Abs(arc.SweepAngle);
        double tolerance = 1e-12;
        if (distance > sweep + tolerance) return SvgGeometryError.None;
        double amount = sweep == 0 ? 0 : distance / sweep;
        if (amount < 0) amount = 0;
        if (amount > 1) amount = 1;
        SvgPointOperationResult point = SvgArcMath.Evaluate(arc, amount);
        if (!point.Success) return point.Error;
        bounds.Add(point.Point);
        return SvgGeometryError.None;
    }

    static bool NearlyEqual(double left, double right)
    {
        double scale = System.Math.Max(1, System.Math.Max(System.Math.Abs(left), System.Math.Abs(right)));
        return System.Math.Abs(left - right) <= scale * 1e-14;
    }

    static SvgPolynomialRootResult RootSuccess(System.Collections.Generic.List<double> roots) { return new SvgPolynomialRootResult(true, SvgGeometryError.None, roots); }
    static SvgPolynomialRootResult RootFailure(SvgGeometryError error) { return new SvgPolynomialRootResult(false, error, null); }
    static SvgPointOperationResult PointFailure(SvgGeometryError error) { return new SvgPointOperationResult(false, error, null); }
    static SvgVectorOperationResult VectorFailure(SvgGeometryError error) { return new SvgVectorOperationResult(false, error, null); }
}
