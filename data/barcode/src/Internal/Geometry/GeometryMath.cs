namespace barcode;

internal static class GeometryMath
{
    internal const double Epsilon = 0.000000001;

    internal static BarcodePoint CreatePoint(double x, double y)
    {
        ValidateCoordinate(x);
        ValidateCoordinate(y);
        BarcodePoint point = new BarcodePoint();
        point.X = x;
        point.Y = y;
        return point;
    }

    internal static BarcodePoint CopyPointValue(BarcodePoint point)
    {
        ValidatePoint(point);
        return CreatePoint(point.X, point.Y);
    }

    internal static double DistanceBetweenPoints(BarcodePoint first, BarcodePoint second)
    {
        ValidatePoint(first);
        ValidatePoint(second);
        double deltaX = second.X - first.X;
        double deltaY = second.Y - first.Y;
        return global::System.Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    internal static BarcodePoint InterpolatePoint(BarcodePoint start, BarcodePoint end, double fraction)
    {
        ValidatePoint(start);
        ValidatePoint(end);
        ValidateCoordinate(fraction);
        double x = start.X + ((end.X - start.X) * fraction);
        double y = start.Y + ((end.Y - start.Y) * fraction);
        return CreatePoint(x, y);
    }

    internal static BarcodeRectangle CreateRectangle(double x, double y, double width, double height)
    {
        ValidateCoordinate(x);
        ValidateCoordinate(y);
        ValidateCoordinate(width);
        ValidateCoordinate(height);
        if (width < 0.0 || height < 0.0)
        {
            throw new BarcodeValidationException("Rectangle dimensions must not be negative.");
        }

        BarcodeRectangle rectangle = new BarcodeRectangle();
        rectangle.X = x;
        rectangle.Y = y;
        rectangle.Width = width;
        rectangle.Height = height;
        return rectangle;
    }

    internal static BarcodeRectangle CopyRectangleValue(BarcodeRectangle rectangle)
    {
        ValidateRectangle(rectangle);
        return CreateRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    internal static bool IsPointInsideRectangle(BarcodePoint point, BarcodeRectangle rectangle)
    {
        ValidatePoint(point);
        ValidateRectangle(rectangle);
        double right = rectangle.X + rectangle.Width;
        double bottom = rectangle.Y + rectangle.Height;
        return point.X >= rectangle.X && point.X <= right && point.Y >= rectangle.Y && point.Y <= bottom;
    }

    internal static BarcodeRectangle? IntersectRectangles(BarcodeRectangle first, BarcodeRectangle second)
    {
        ValidateRectangle(first);
        ValidateRectangle(second);
        double left = global::System.Math.Max(first.X, second.X);
        double top = global::System.Math.Max(first.Y, second.Y);
        double right = global::System.Math.Min(first.X + first.Width, second.X + second.Width);
        double bottom = global::System.Math.Min(first.Y + first.Height, second.Y + second.Height);
        if (right < left || bottom < top)
        {
            return null;
        }

        return CreateRectangle(left, top, right - left, bottom - top);
    }

    internal static BarcodeQuadrilateral CreateQuadrilateral(
        BarcodePoint topLeft,
        BarcodePoint topRight,
        BarcodePoint bottomRight,
        BarcodePoint bottomLeft)
    {
        BarcodeQuadrilateral quadrilateral = new BarcodeQuadrilateral();
        quadrilateral.TopLeft = CopyPointValue(topLeft);
        quadrilateral.TopRight = CopyPointValue(topRight);
        quadrilateral.BottomRight = CopyPointValue(bottomRight);
        quadrilateral.BottomLeft = CopyPointValue(bottomLeft);
        quadrilateral.Bounds = CalculateQuadrilateralBounds(quadrilateral);
        return quadrilateral;
    }

    internal static BarcodeRectangle CalculateQuadrilateralBounds(BarcodeQuadrilateral quadrilateral)
    {
        ValidateQuadrilateral(quadrilateral);
        BarcodePoint topLeft = RequirePoint(quadrilateral.TopLeft);
        BarcodePoint topRight = RequirePoint(quadrilateral.TopRight);
        BarcodePoint bottomRight = RequirePoint(quadrilateral.BottomRight);
        BarcodePoint bottomLeft = RequirePoint(quadrilateral.BottomLeft);
        double left = global::System.Math.Min(global::System.Math.Min(topLeft.X, topRight.X), global::System.Math.Min(bottomRight.X, bottomLeft.X));
        double top = global::System.Math.Min(global::System.Math.Min(topLeft.Y, topRight.Y), global::System.Math.Min(bottomRight.Y, bottomLeft.Y));
        double right = global::System.Math.Max(global::System.Math.Max(topLeft.X, topRight.X), global::System.Math.Max(bottomRight.X, bottomLeft.X));
        double bottom = global::System.Math.Max(global::System.Math.Max(topLeft.Y, topRight.Y), global::System.Math.Max(bottomRight.Y, bottomLeft.Y));
        return CreateRectangle(left, top, right - left, bottom - top);
    }

    internal static double CalculateQuadrilateralSignedArea(BarcodeQuadrilateral quadrilateral)
    {
        ValidateQuadrilateral(quadrilateral);
        BarcodePoint topLeft = RequirePoint(quadrilateral.TopLeft);
        BarcodePoint topRight = RequirePoint(quadrilateral.TopRight);
        BarcodePoint bottomRight = RequirePoint(quadrilateral.BottomRight);
        BarcodePoint bottomLeft = RequirePoint(quadrilateral.BottomLeft);
        double twiceArea = (topLeft.X * topRight.Y) - (topLeft.Y * topRight.X);
        twiceArea = twiceArea + ((topRight.X * bottomRight.Y) - (topRight.Y * bottomRight.X));
        twiceArea = twiceArea + ((bottomRight.X * bottomLeft.Y) - (bottomRight.Y * bottomLeft.X));
        twiceArea = twiceArea + ((bottomLeft.X * topLeft.Y) - (bottomLeft.Y * topLeft.X));
        return twiceArea * 0.5;
    }

    internal static bool IsPointInsideQuadrilateral(BarcodePoint point, BarcodeQuadrilateral quadrilateral)
    {
        ValidatePoint(point);
        ValidateQuadrilateral(quadrilateral);
        BarcodePoint topLeft = RequirePoint(quadrilateral.TopLeft);
        BarcodePoint topRight = RequirePoint(quadrilateral.TopRight);
        BarcodePoint bottomRight = RequirePoint(quadrilateral.BottomRight);
        BarcodePoint bottomLeft = RequirePoint(quadrilateral.BottomLeft);
        double first = CalculateCrossProduct(topLeft, topRight, point);
        double second = CalculateCrossProduct(topRight, bottomRight, point);
        double third = CalculateCrossProduct(bottomRight, bottomLeft, point);
        double fourth = CalculateCrossProduct(bottomLeft, topLeft, point);
        bool hasNegative = first < -Epsilon || second < -Epsilon || third < -Epsilon || fourth < -Epsilon;
        bool hasPositive = first > Epsilon || second > Epsilon || third > Epsilon || fourth > Epsilon;
        return !(hasNegative && hasPositive);
    }

    internal static double CalculateCrossProduct(BarcodePoint origin, BarcodePoint first, BarcodePoint second)
    {
        ValidatePoint(origin);
        ValidatePoint(first);
        ValidatePoint(second);
        return ((first.X - origin.X) * (second.Y - origin.Y)) - ((first.Y - origin.Y) * (second.X - origin.X));
    }

    internal static BarcodePoint? IntersectLineSegments(LineSegment first, LineSegment second)
    {
        if (first == null || second == null)
        {
            throw new BarcodeValidationException("Line segments must not be null.");
        }

        double firstX = first.End.X - first.Start.X;
        double firstY = first.End.Y - first.Start.Y;
        double secondX = second.End.X - second.Start.X;
        double secondY = second.End.Y - second.Start.Y;
        double denominator = (firstX * secondY) - (firstY * secondX);
        if (global::System.Math.Abs(denominator) <= Epsilon)
        {
            return null;
        }

        double offsetX = second.Start.X - first.Start.X;
        double offsetY = second.Start.Y - first.Start.Y;
        double firstFraction = ((offsetX * secondY) - (offsetY * secondX)) / denominator;
        double secondFraction = ((offsetX * firstY) - (offsetY * firstX)) / denominator;
        if (firstFraction < -Epsilon || firstFraction > 1.0 + Epsilon || secondFraction < -Epsilon || secondFraction > 1.0 + Epsilon)
        {
            return null;
        }

        return CreatePoint(first.Start.X + (firstX * firstFraction), first.Start.Y + (firstY * firstFraction));
    }

    internal static LineSegment? ClipLineToRectangle(LineSegment line, BarcodeRectangle rectangle)
    {
        if (line == null)
        {
            throw new BarcodeValidationException("Line segment must not be null.");
        }

        ValidateRectangle(rectangle);
        double deltaX = line.End.X - line.Start.X;
        double deltaY = line.End.Y - line.Start.Y;
        double[] direction = new double[] { -deltaX, deltaX, -deltaY, deltaY };
        double[] distance = new double[]
        {
            line.Start.X - rectangle.X,
            rectangle.X + rectangle.Width - line.Start.X,
            line.Start.Y - rectangle.Y,
            rectangle.Y + rectangle.Height - line.Start.Y
        };
        double entering = 0.0;
        double leaving = 1.0;
        int index = 0;
        while (index < 4)
        {
            double p = direction[index];
            double q = distance[index];
            if (global::System.Math.Abs(p) <= Epsilon)
            {
                if (q < 0.0)
                {
                    return null;
                }
            }
            else
            {
                double ratio = q / p;
                if (p < 0.0)
                {
                    if (ratio > leaving)
                    {
                        return null;
                    }

                    if (ratio > entering)
                    {
                        entering = ratio;
                    }
                }
                else
                {
                    if (ratio < entering)
                    {
                        return null;
                    }

                    if (ratio < leaving)
                    {
                        leaving = ratio;
                    }
                }
            }

            index = index + 1;
        }

        BarcodePoint clippedStart = InterpolatePoint(line.Start, line.End, entering);
        BarcodePoint clippedEnd = InterpolatePoint(line.Start, line.End, leaving);
        return new LineSegment(clippedStart, clippedEnd);
    }

    internal static bool IsFiniteValue(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal static void ValidateCoordinate(double value)
    {
        if (!IsFiniteValue(value))
        {
            throw new BarcodeValidationException("Coordinate must be finite.");
        }
    }

    internal static BarcodePoint RequirePoint(BarcodePoint? point)
    {
        if (point == null)
        {
            throw new BarcodeValidationException("Point must not be null.");
        }

        ValidateCoordinate(point.X);
        ValidateCoordinate(point.Y);
        return point;
    }

    internal static void ValidatePoint(BarcodePoint? point)
    {
        RequirePoint(point);
    }

    internal static void ValidateRectangle(BarcodeRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new BarcodeValidationException("Rectangle must not be null.");
        }

        CreateRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    internal static void ValidateQuadrilateral(BarcodeQuadrilateral quadrilateral)
    {
        if (quadrilateral == null)
        {
            throw new BarcodeValidationException("Quadrilateral must not be null.");
        }

        RequirePoint(quadrilateral.TopLeft);
        RequirePoint(quadrilateral.TopRight);
        RequirePoint(quadrilateral.BottomRight);
        RequirePoint(quadrilateral.BottomLeft);
    }
}
