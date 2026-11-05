namespace barcode;

internal class PerspectiveTransform
{
    internal double M00;
    internal double M01;
    internal double M02;
    internal double M10;
    internal double M11;
    internal double M12;
    internal double M20;
    internal double M21;
    internal double M22;

    internal PerspectiveTransform(
        double m00,
        double m01,
        double m02,
        double m10,
        double m11,
        double m12,
        double m20,
        double m21,
        double m22)
    {
        GeometryMath.ValidateCoordinate(m00);
        GeometryMath.ValidateCoordinate(m01);
        GeometryMath.ValidateCoordinate(m02);
        GeometryMath.ValidateCoordinate(m10);
        GeometryMath.ValidateCoordinate(m11);
        GeometryMath.ValidateCoordinate(m12);
        GeometryMath.ValidateCoordinate(m20);
        GeometryMath.ValidateCoordinate(m21);
        GeometryMath.ValidateCoordinate(m22);
        M00 = m00;
        M01 = m01;
        M02 = m02;
        M10 = m10;
        M11 = m11;
        M12 = m12;
        M20 = m20;
        M21 = m21;
        M22 = m22;
    }

    internal static PerspectiveTransform CreateIdentity()
    {
        return new PerspectiveTransform(1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0);
    }

    internal static PerspectiveTransform CreateSquareToQuadrilateral(BarcodeQuadrilateral quadrilateral)
    {
        GeometryMath.ValidateQuadrilateral(quadrilateral);
        BarcodePoint topLeft = GeometryMath.RequirePoint(quadrilateral.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(quadrilateral.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(quadrilateral.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(quadrilateral.BottomLeft);
        double diagonalX = topLeft.X - topRight.X + bottomRight.X - bottomLeft.X;
        double diagonalY = topLeft.Y - topRight.Y + bottomRight.Y - bottomLeft.Y;
        PerspectiveTransform transform;
        if (global::System.Math.Abs(diagonalX) <= GeometryMath.Epsilon && global::System.Math.Abs(diagonalY) <= GeometryMath.Epsilon)
        {
            transform = new PerspectiveTransform(
                topRight.X - topLeft.X,
                bottomLeft.X - topLeft.X,
                topLeft.X,
                topRight.Y - topLeft.Y,
                bottomLeft.Y - topLeft.Y,
                topLeft.Y,
                0.0,
                0.0,
                1.0);
        }
        else
        {
            double rightX = topRight.X - bottomRight.X;
            double leftX = bottomLeft.X - bottomRight.X;
            double rightY = topRight.Y - bottomRight.Y;
            double leftY = bottomLeft.Y - bottomRight.Y;
            double denominator = (rightX * leftY) - (leftX * rightY);
            if (global::System.Math.Abs(denominator) <= GeometryMath.Epsilon)
            {
                throw new BarcodeValidationException("Quadrilateral does not define an invertible perspective transform.");
            }

            double perspectiveX = ((diagonalX * leftY) - (leftX * diagonalY)) / denominator;
            double perspectiveY = ((rightX * diagonalY) - (diagonalX * rightY)) / denominator;
            transform = new PerspectiveTransform(
                topRight.X - topLeft.X + (perspectiveX * topRight.X),
                bottomLeft.X - topLeft.X + (perspectiveY * bottomLeft.X),
                topLeft.X,
                topRight.Y - topLeft.Y + (perspectiveX * topRight.Y),
                bottomLeft.Y - topLeft.Y + (perspectiveY * bottomLeft.Y),
                topLeft.Y,
                perspectiveX,
                perspectiveY,
                1.0);
        }

        if (global::System.Math.Abs(transform.CalculateDeterminant()) <= GeometryMath.Epsilon)
        {
            throw new BarcodeValidationException("Quadrilateral does not define an invertible perspective transform.");
        }

        return transform;
    }

    internal static PerspectiveTransform CreateQuadrilateralToSquare(BarcodeQuadrilateral quadrilateral)
    {
        PerspectiveTransform squareToQuadrilateral = CreateSquareToQuadrilateral(quadrilateral);
        return squareToQuadrilateral.InvertTransform();
    }

    internal static PerspectiveTransform CreateQuadrilateralMapping(
        BarcodeQuadrilateral source,
        BarcodeQuadrilateral destination)
    {
        PerspectiveTransform sourceToSquare = CreateQuadrilateralToSquare(source);
        PerspectiveTransform squareToDestination = CreateSquareToQuadrilateral(destination);
        return sourceToSquare.ComposeAfter(squareToDestination);
    }

    internal BarcodePoint TransformPoint(BarcodePoint point)
    {
        GeometryMath.ValidatePoint(point);
        double denominator = (M20 * point.X) + (M21 * point.Y) + M22;
        if (global::System.Math.Abs(denominator) <= GeometryMath.Epsilon)
        {
            throw new BarcodeValidationException("Perspective transform maps the point to infinity.");
        }

        double x = ((M00 * point.X) + (M01 * point.Y) + M02) / denominator;
        double y = ((M10 * point.X) + (M11 * point.Y) + M12) / denominator;
        return GeometryMath.CreatePoint(x, y);
    }

    internal BarcodePoint[] TransformPointArray(BarcodePoint[] points)
    {
        if (points == null)
        {
            throw new BarcodeValidationException("Point array must not be null.");
        }

        BarcodePoint[] transformed = new BarcodePoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            transformed[index] = TransformPoint(points[index]);
            index = index + 1;
        }

        return transformed;
    }

    internal PerspectiveTransform ComposeAfter(PerspectiveTransform next)
    {
        if (next == null)
        {
            throw new BarcodeValidationException("Perspective transform must not be null.");
        }

        double m00 = (next.M00 * M00) + (next.M01 * M10) + (next.M02 * M20);
        double m01 = (next.M00 * M01) + (next.M01 * M11) + (next.M02 * M21);
        double m02 = (next.M00 * M02) + (next.M01 * M12) + (next.M02 * M22);
        double m10 = (next.M10 * M00) + (next.M11 * M10) + (next.M12 * M20);
        double m11 = (next.M10 * M01) + (next.M11 * M11) + (next.M12 * M21);
        double m12 = (next.M10 * M02) + (next.M11 * M12) + (next.M12 * M22);
        double m20 = (next.M20 * M00) + (next.M21 * M10) + (next.M22 * M20);
        double m21 = (next.M20 * M01) + (next.M21 * M11) + (next.M22 * M21);
        double m22 = (next.M20 * M02) + (next.M21 * M12) + (next.M22 * M22);
        return new PerspectiveTransform(m00, m01, m02, m10, m11, m12, m20, m21, m22);
    }

    internal PerspectiveTransform InvertTransform()
    {
        double determinant = CalculateDeterminant();
        if (global::System.Math.Abs(determinant) <= GeometryMath.Epsilon)
        {
            throw new BarcodeValidationException("Perspective transform is not invertible.");
        }

        double m00 = ((M11 * M22) - (M12 * M21)) / determinant;
        double m01 = ((M02 * M21) - (M01 * M22)) / determinant;
        double m02 = ((M01 * M12) - (M02 * M11)) / determinant;
        double m10 = ((M12 * M20) - (M10 * M22)) / determinant;
        double m11 = ((M00 * M22) - (M02 * M20)) / determinant;
        double m12 = ((M02 * M10) - (M00 * M12)) / determinant;
        double m20 = ((M10 * M21) - (M11 * M20)) / determinant;
        double m21 = ((M01 * M20) - (M00 * M21)) / determinant;
        double m22 = ((M00 * M11) - (M01 * M10)) / determinant;
        return new PerspectiveTransform(m00, m01, m02, m10, m11, m12, m20, m21, m22);
    }

    internal double CalculateDeterminant()
    {
        double first = M00 * ((M11 * M22) - (M12 * M21));
        double second = M01 * ((M10 * M22) - (M12 * M20));
        double third = M02 * ((M10 * M21) - (M11 * M20));
        return first - second + third;
    }
}
