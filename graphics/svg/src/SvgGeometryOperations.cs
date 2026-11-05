namespace svg;

internal class SvgRectEdges
{
    internal double Right;
    internal double Bottom;

    internal SvgRectEdges(double right, double bottom)
    {
        Right = right;
        Bottom = bottom;
    }
}

/// <summary>Implements exact finite value comparison and deterministic elementary geometry.</summary>
internal static class SvgGeometryOperations
{
    internal static bool Equal(SvgPoint left, SvgPoint right)
    {
        return ValidPoint(left) && ValidPoint(right) && left.X == right.X && left.Y == right.Y;
    }

    internal static bool Equal(SvgVector left, SvgVector right)
    {
        return ValidVector(left) && ValidVector(right) && left.X == right.X && left.Y == right.Y;
    }

    internal static bool Equal(SvgSize left, SvgSize right)
    {
        return ValidSize(left) && ValidSize(right) && left.Width == right.Width && left.Height == right.Height;
    }

    internal static bool Equal(SvgRect left, SvgRect right)
    {
        if (!ValidRect(left) || !ValidRect(right)) return false;
        if (left.IsEmpty || right.IsEmpty) return left.IsEmpty && right.IsEmpty;
        return left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;
    }

    internal static bool Equal(SvgMatrix left, SvgMatrix right)
    {
        return ValidMatrix(left) && ValidMatrix(right) && left.A == right.A && left.B == right.B && left.C == right.C && left.D == right.D && left.E == right.E && left.F == right.F;
    }

    internal static SvgPointOperationResult Add(SvgPoint point, SvgVector vector)
    {
        if (!ValidPoint(point) || !ValidVector(vector)) return PointFailure(SvgGeometryError.InvalidArgument);
        SvgFiniteResult x = SvgFiniteMath.Add(point.X, vector.X);
        SvgFiniteResult y = SvgFiniteMath.Add(point.Y, vector.Y);
        if (!x.Success || !y.Success) return PointFailure(SvgGeometryError.NonFiniteResult);
        return new SvgPointOperationResult(true, SvgGeometryError.None, new SvgPoint(x.Value, y.Value));
    }

    internal static SvgVectorOperationResult Subtract(SvgPoint left, SvgPoint right)
    {
        if (!ValidPoint(left) || !ValidPoint(right)) return VectorFailure(SvgGeometryError.InvalidArgument);
        SvgFiniteResult x = SvgFiniteMath.Subtract(left.X, right.X);
        SvgFiniteResult y = SvgFiniteMath.Subtract(left.Y, right.Y);
        if (!x.Success || !y.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return new SvgVectorOperationResult(true, SvgGeometryError.None, new SvgVector(x.Value, y.Value));
    }

    internal static SvgVectorOperationResult Add(SvgVector left, SvgVector right)
    {
        if (!ValidVector(left) || !ValidVector(right)) return VectorFailure(SvgGeometryError.InvalidArgument);
        SvgFiniteResult x = SvgFiniteMath.Add(left.X, right.X);
        SvgFiniteResult y = SvgFiniteMath.Add(left.Y, right.Y);
        if (!x.Success || !y.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return new SvgVectorOperationResult(true, SvgGeometryError.None, new SvgVector(x.Value, y.Value));
    }

    internal static SvgVectorOperationResult Subtract(SvgVector left, SvgVector right)
    {
        if (!ValidVector(left) || !ValidVector(right)) return VectorFailure(SvgGeometryError.InvalidArgument);
        SvgFiniteResult x = SvgFiniteMath.Subtract(left.X, right.X);
        SvgFiniteResult y = SvgFiniteMath.Subtract(left.Y, right.Y);
        if (!x.Success || !y.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return new SvgVectorOperationResult(true, SvgGeometryError.None, new SvgVector(x.Value, y.Value));
    }

    internal static SvgVectorOperationResult Scale(SvgVector vector, double factor)
    {
        if (!ValidVector(vector) || !SvgFiniteMath.IsFinite(factor)) return VectorFailure(SvgGeometryError.InvalidArgument);
        SvgFiniteResult x = SvgFiniteMath.Multiply(vector.X, factor);
        SvgFiniteResult y = SvgFiniteMath.Multiply(vector.Y, factor);
        if (!x.Success || !y.Success) return VectorFailure(SvgGeometryError.NonFiniteResult);
        return new SvgVectorOperationResult(true, SvgGeometryError.None, new SvgVector(x.Value, y.Value));
    }

    internal static SvgScalarOperationResult Dot(SvgVector left, SvgVector right)
    {
        if (!ValidVector(left) || !ValidVector(right)) return ScalarFailure(SvgGeometryError.InvalidArgument);
        SvgFiniteResult x = SvgFiniteMath.Multiply(left.X, right.X);
        SvgFiniteResult y = SvgFiniteMath.Multiply(left.Y, right.Y);
        if (!x.Success || !y.Success) return ScalarFailure(SvgGeometryError.NonFiniteResult);
        SvgFiniteResult sum = SvgFiniteMath.Add(x.Value, y.Value);
        if (!sum.Success) return ScalarFailure(SvgGeometryError.NonFiniteResult);
        return new SvgScalarOperationResult(true, SvgGeometryError.None, sum.Value);
    }

    internal static SvgScalarOperationResult Cross(SvgVector left, SvgVector right)
    {
        if (!ValidVector(left) || !ValidVector(right)) return ScalarFailure(SvgGeometryError.InvalidArgument);
        SvgFiniteResult first = SvgFiniteMath.Multiply(left.X, right.Y);
        SvgFiniteResult second = SvgFiniteMath.Multiply(left.Y, right.X);
        if (!first.Success || !second.Success) return ScalarFailure(SvgGeometryError.NonFiniteResult);
        SvgFiniteResult difference = SvgFiniteMath.Subtract(first.Value, second.Value);
        if (!difference.Success) return ScalarFailure(SvgGeometryError.NonFiniteResult);
        return new SvgScalarOperationResult(true, SvgGeometryError.None, difference.Value);
    }

    internal static SvgScalarOperationResult Length(SvgVector vector)
    {
        if (!ValidVector(vector)) return ScalarFailure(SvgGeometryError.InvalidArgument);
        double absoluteX = System.Math.Abs(vector.X);
        double absoluteY = System.Math.Abs(vector.Y);
        double maximum = System.Math.Max(absoluteX, absoluteY);
        if (maximum == 0) return new SvgScalarOperationResult(true, SvgGeometryError.None, 0);
        double minimum = System.Math.Min(absoluteX, absoluteY);
        double ratio = minimum / maximum;
        double value = maximum * System.Math.Sqrt(1 + ratio * ratio);
        if (!SvgFiniteMath.IsFinite(value)) return ScalarFailure(SvgGeometryError.NonFiniteResult);
        return new SvgScalarOperationResult(true, SvgGeometryError.None, SvgFiniteMath.NormalizeZero(value));
    }

    internal static SvgRectOperationResult Union(SvgRect left, SvgRect? right)
    {
        if (!ValidRect(left) || !ValidRect((right ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")))) return RectFailure(SvgGeometryError.InvalidArgument);
        if (left.IsEmpty && right.IsEmpty) return RectSuccess(Empty());
        if (left.IsEmpty) return RectSuccess(Copy(right));
        if (right.IsEmpty) return RectSuccess(Copy(left));
        SvgRectEdges? leftEdges = Edges(left);
        SvgRectEdges? rightEdges = Edges(right);
        if (leftEdges == null || rightEdges == null) return RectFailure(SvgGeometryError.NonFiniteResult);
        double x = System.Math.Min(left.X, right.X);
        double y = System.Math.Min(left.Y, right.Y);
        double maximumX = System.Math.Max(leftEdges.Right, rightEdges.Right);
        double maximumY = System.Math.Max(leftEdges.Bottom, rightEdges.Bottom);
        SvgFiniteResult width = SvgFiniteMath.Subtract(maximumX, x);
        SvgFiniteResult height = SvgFiniteMath.Subtract(maximumY, y);
        if (!width.Success || !height.Success) return RectFailure(SvgGeometryError.NonFiniteResult);
        return RectSuccess(new SvgRect(x, y, width.Value, height.Value));
    }

    internal static SvgRectOperationResult Intersect(SvgRect left, SvgRect? right)
    {
        if (!ValidRect(left) || !ValidRect((right ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")))) return RectFailure(SvgGeometryError.InvalidArgument);
        if (left.IsEmpty || right.IsEmpty) return RectSuccess(Empty());
        SvgRectEdges? leftEdges = Edges(left);
        SvgRectEdges? rightEdges = Edges(right);
        if (leftEdges == null || rightEdges == null) return RectFailure(SvgGeometryError.NonFiniteResult);
        double x = System.Math.Max(left.X, right.X);
        double y = System.Math.Max(left.Y, right.Y);
        double maximumX = System.Math.Min(leftEdges.Right, rightEdges.Right);
        double maximumY = System.Math.Min(leftEdges.Bottom, rightEdges.Bottom);
        if (maximumX < x || maximumY < y) return RectSuccess(Empty());
        SvgFiniteResult width = SvgFiniteMath.Subtract(maximumX, x);
        SvgFiniteResult height = SvgFiniteMath.Subtract(maximumY, y);
        if (!width.Success || !height.Success) return RectFailure(SvgGeometryError.NonFiniteResult);
        return RectSuccess(new SvgRect(x, y, width.Value, height.Value));
    }

    internal static SvgRectOperationResult Expand(SvgRect rectangle, double horizontal, double vertical)
    {
        if (!ValidRect(rectangle) || !SvgFiniteMath.IsFinite(horizontal) || !SvgFiniteMath.IsFinite(vertical) || horizontal < 0 || vertical < 0) return RectFailure(SvgGeometryError.InvalidArgument);
        if (rectangle.IsEmpty) return RectSuccess(Empty());
        SvgFiniteResult x = SvgFiniteMath.Subtract(rectangle.X, horizontal);
        SvgFiniteResult y = SvgFiniteMath.Subtract(rectangle.Y, vertical);
        SvgFiniteResult widthOnce = SvgFiniteMath.Add(rectangle.Width, horizontal);
        SvgFiniteResult heightOnce = SvgFiniteMath.Add(rectangle.Height, vertical);
        if (!x.Success || !y.Success || !widthOnce.Success || !heightOnce.Success) return RectFailure(SvgGeometryError.NonFiniteResult);
        SvgFiniteResult width = SvgFiniteMath.Add(widthOnce.Value, horizontal);
        SvgFiniteResult height = SvgFiniteMath.Add(heightOnce.Value, vertical);
        if (!width.Success || !height.Success) return RectFailure(SvgGeometryError.NonFiniteResult);
        return RectSuccess(new SvgRect(x.Value, y.Value, width.Value, height.Value));
    }

    internal static bool Contains(SvgRect rectangle, SvgPoint point)
    {
        if (!ValidRect(rectangle) || !ValidPoint(point) || rectangle.IsEmpty) return false;
        SvgRectEdges? edges = Edges(rectangle);
        if (edges == null) return false;
        return point.X >= rectangle.X && point.X <= edges.Right && point.Y >= rectangle.Y && point.Y <= edges.Bottom;
    }

    static SvgRectEdges? Edges(SvgRect rectangle)
    {
        SvgFiniteResult right = SvgFiniteMath.Add(rectangle.X, rectangle.Width);
        SvgFiniteResult bottom = SvgFiniteMath.Add(rectangle.Y, rectangle.Height);
        if (!right.Success || !bottom.Success) return null;
        return new SvgRectEdges(right.Value, bottom.Value);
    }

    static SvgRect Copy(SvgRect rectangle)
    {
        return SvgRect.Create(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, rectangle.IsEmpty);
    }

    static SvgRect Empty()
    {
        return SvgRect.Create(0, 0, 0, 0, true);
    }

    static SvgPointOperationResult PointFailure(SvgGeometryError error)
    {
        return new SvgPointOperationResult(false, error, null);
    }

    static SvgVectorOperationResult VectorFailure(SvgGeometryError error)
    {
        return new SvgVectorOperationResult(false, error, null);
    }

    static SvgScalarOperationResult ScalarFailure(SvgGeometryError error)
    {
        return new SvgScalarOperationResult(false, error, 0);
    }

    static SvgRectOperationResult RectFailure(SvgGeometryError error)
    {
        return new SvgRectOperationResult(false, error, null);
    }

    static SvgRectOperationResult RectSuccess(SvgRect rectangle)
    {
        return new SvgRectOperationResult(true, SvgGeometryError.None, rectangle);
    }

    static bool ValidPoint(SvgPoint point)
    {
        return point != null && SvgFiniteMath.IsFinite(point.X) && SvgFiniteMath.IsFinite(point.Y);
    }

    static bool ValidVector(SvgVector vector)
    {
        return vector != null && SvgFiniteMath.IsFinite(vector.X) && SvgFiniteMath.IsFinite(vector.Y);
    }

    static bool ValidSize(SvgSize size)
    {
        return size != null && SvgFiniteMath.IsFinite(size.Width) && SvgFiniteMath.IsFinite(size.Height) && size.Width >= 0 && size.Height >= 0;
    }

    static bool ValidRect(SvgRect rectangle)
    {
        return rectangle != null && SvgFiniteMath.IsFinite(rectangle.X) && SvgFiniteMath.IsFinite(rectangle.Y) && SvgFiniteMath.IsFinite(rectangle.Width) && SvgFiniteMath.IsFinite(rectangle.Height) && rectangle.Width >= 0 && rectangle.Height >= 0;
    }

    static bool ValidMatrix(SvgMatrix matrix)
    {
        return matrix != null && SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }
}
