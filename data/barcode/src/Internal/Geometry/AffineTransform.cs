namespace barcode;

internal class AffineTransform
{
    internal double M00;
    internal double M01;
    internal double M02;
    internal double M10;
    internal double M11;
    internal double M12;

    internal AffineTransform(double m00, double m01, double m02, double m10, double m11, double m12)
    {
        GeometryMath.ValidateCoordinate(m00);
        GeometryMath.ValidateCoordinate(m01);
        GeometryMath.ValidateCoordinate(m02);
        GeometryMath.ValidateCoordinate(m10);
        GeometryMath.ValidateCoordinate(m11);
        GeometryMath.ValidateCoordinate(m12);
        M00 = m00;
        M01 = m01;
        M02 = m02;
        M10 = m10;
        M11 = m11;
        M12 = m12;
    }

    internal static AffineTransform CreateIdentity()
    {
        return new AffineTransform(1.0, 0.0, 0.0, 0.0, 1.0, 0.0);
    }

    internal static AffineTransform CreateTranslation(double x, double y)
    {
        return new AffineTransform(1.0, 0.0, x, 0.0, 1.0, y);
    }

    internal static AffineTransform CreateScaling(double x, double y)
    {
        return new AffineTransform(x, 0.0, 0.0, 0.0, y, 0.0);
    }

    internal static AffineTransform CreateRotationDegrees(double degrees)
    {
        GeometryMath.ValidateCoordinate(degrees);
        double radians = degrees * global::System.Math.PI / 180.0;
        double cosine = global::System.Math.Cos(radians);
        double sine = global::System.Math.Sin(radians);
        return new AffineTransform(cosine, -sine, 0.0, sine, cosine, 0.0);
    }

    internal static AffineTransform CreateFromPointPairs(
        BarcodePoint sourceFirst,
        BarcodePoint sourceSecond,
        BarcodePoint sourceThird,
        BarcodePoint destinationFirst,
        BarcodePoint destinationSecond,
        BarcodePoint destinationThird)
    {
        GeometryMath.ValidatePoint(sourceFirst);
        GeometryMath.ValidatePoint(sourceSecond);
        GeometryMath.ValidatePoint(sourceThird);
        GeometryMath.ValidatePoint(destinationFirst);
        GeometryMath.ValidatePoint(destinationSecond);
        GeometryMath.ValidatePoint(destinationThird);
        double source00 = sourceSecond.X - sourceFirst.X;
        double source01 = sourceThird.X - sourceFirst.X;
        double source10 = sourceSecond.Y - sourceFirst.Y;
        double source11 = sourceThird.Y - sourceFirst.Y;
        double determinant = (source00 * source11) - (source01 * source10);
        if (global::System.Math.Abs(determinant) <= GeometryMath.Epsilon)
        {
            throw new BarcodeValidationException("Affine source points must form a non-degenerate triangle.");
        }

        double inverse00 = source11 / determinant;
        double inverse01 = -source01 / determinant;
        double inverse10 = -source10 / determinant;
        double inverse11 = source00 / determinant;
        double destination00 = destinationSecond.X - destinationFirst.X;
        double destination01 = destinationThird.X - destinationFirst.X;
        double destination10 = destinationSecond.Y - destinationFirst.Y;
        double destination11 = destinationThird.Y - destinationFirst.Y;
        double m00 = (destination00 * inverse00) + (destination01 * inverse10);
        double m01 = (destination00 * inverse01) + (destination01 * inverse11);
        double m10 = (destination10 * inverse00) + (destination11 * inverse10);
        double m11 = (destination10 * inverse01) + (destination11 * inverse11);
        double m02 = destinationFirst.X - (m00 * sourceFirst.X) - (m01 * sourceFirst.Y);
        double m12 = destinationFirst.Y - (m10 * sourceFirst.X) - (m11 * sourceFirst.Y);
        return new AffineTransform(m00, m01, m02, m10, m11, m12);
    }

    internal BarcodePoint TransformPoint(BarcodePoint point)
    {
        GeometryMath.ValidatePoint(point);
        double x = (M00 * point.X) + (M01 * point.Y) + M02;
        double y = (M10 * point.X) + (M11 * point.Y) + M12;
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

    internal AffineTransform ComposeAfter(AffineTransform next)
    {
        if (next == null)
        {
            throw new BarcodeValidationException("Affine transform must not be null.");
        }

        double m00 = (next.M00 * M00) + (next.M01 * M10);
        double m01 = (next.M00 * M01) + (next.M01 * M11);
        double m02 = (next.M00 * M02) + (next.M01 * M12) + next.M02;
        double m10 = (next.M10 * M00) + (next.M11 * M10);
        double m11 = (next.M10 * M01) + (next.M11 * M11);
        double m12 = (next.M10 * M02) + (next.M11 * M12) + next.M12;
        return new AffineTransform(m00, m01, m02, m10, m11, m12);
    }

    internal AffineTransform InvertTransform()
    {
        double determinant = (M00 * M11) - (M01 * M10);
        if (global::System.Math.Abs(determinant) <= GeometryMath.Epsilon)
        {
            throw new BarcodeValidationException("Affine transform is not invertible.");
        }

        double inverse00 = M11 / determinant;
        double inverse01 = -M01 / determinant;
        double inverse10 = -M10 / determinant;
        double inverse11 = M00 / determinant;
        double inverse02 = -((inverse00 * M02) + (inverse01 * M12));
        double inverse12 = -((inverse10 * M02) + (inverse11 * M12));
        return new AffineTransform(inverse00, inverse01, inverse02, inverse10, inverse11, inverse12);
    }
}
