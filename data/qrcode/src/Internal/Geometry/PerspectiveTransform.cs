namespace qrcode;

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

    internal static PerspectiveTransform Create(
        QrQuadrilateral source,
        QrQuadrilateral destination)
    {
        QrPoint[] sourcePoints = ReadQuadrilateral(source, "source");
        QrPoint[] destinationPoints = ReadQuadrilateral(destination, "destination");
        double sourceCenterX = GetCenterX(sourcePoints);
        double sourceCenterY = GetCenterY(sourcePoints);
        double destinationCenterX = GetCenterX(destinationPoints);
        double destinationCenterY = GetCenterY(destinationPoints);
        double sourceScale = GetScale(sourcePoints);
        double destinationScale = GetScale(destinationPoints);
        QrPoint[] normalizedSource = NormalizePoints(
            sourcePoints,
            sourceCenterX,
            sourceCenterY,
            sourceScale);
        QrPoint[] normalizedDestination = NormalizePoints(
            destinationPoints,
            destinationCenterX,
            destinationCenterY,
            destinationScale);
        PerspectiveTransform sourceSquare = CreateSquareToQuadrilateral(
            normalizedSource);
        PerspectiveTransform destinationSquare = CreateSquareToQuadrilateral(
            normalizedDestination);
        PerspectiveTransform normalizedMapping = Multiply(
            destinationSquare,
            sourceSquare.Invert());
        PerspectiveTransform sourceNormalization = CreateNormalization(
            sourceCenterX,
            sourceCenterY,
            sourceScale);
        PerspectiveTransform destinationDenormalization = CreateDenormalization(
            destinationCenterX,
            destinationCenterY,
            destinationScale);
        PerspectiveTransform mapping = Multiply(
            destinationDenormalization,
            Multiply(normalizedMapping, sourceNormalization));
        NormalizeCoefficients(mapping);
        ValidateCoefficients(mapping);
        return mapping;
    }

    internal QrPoint MapPoint(QrPoint point)
    {
        if (point == null || !IsFinite(point.X) || !IsFinite(point.Y))
        {
            throw new QrInputException(
                "The point coordinates must be finite.",
                "point");
        }

        QrPoint mapped = new QrPoint();
        if (!TryMapPoint(point.X, point.Y, mapped))
        {
            throw new QrInputException(
                "The point maps to an undefined or non-finite projective coordinate.",
                "point");
        }

        return mapped;
    }

    // Reuses caller-owned storage for sampling loops. Failure leaves it unchanged.
    internal bool TryMapPoint(double x, double y, QrPoint mapped)
    {
        if (mapped == null)
        {
            throw new QrInputException("The mapped point storage is required.", "mapped");
        }

        if (!IsFinite(x) || !IsFinite(y))
        {
            return false;
        }

        double denominator = M20 * x + M21 * y + M22;
        double denominatorScale = global::System.Math.Abs(M20 * x)
            + global::System.Math.Abs(M21 * y)
            + global::System.Math.Abs(M22);
        if (!IsFinite(denominator)
            || denominatorScale == 0.0
            || global::System.Math.Abs(denominator) <= denominatorScale * 1.0E-14)
        {
            return false;
        }

        double mappedX = (M00 * x + M01 * y + M02) / denominator;
        double mappedY = (M10 * x + M11 * y + M12) / denominator;
        if (!IsFinite(mappedX) || !IsFinite(mappedY))
        {
            return false;
        }

        mapped.X = mappedX;
        mapped.Y = mappedY;
        return true;
    }

    internal PerspectiveTransform Invert()
    {
        ValidateCoefficients(this);
        // Homogeneous coefficients have arbitrary scale. Work on a normalized
        // copy so equivalent transforms neither overflow nor mutate the caller.
        PerspectiveTransform normalized = new PerspectiveTransform
        {
            M00 = M00, M01 = M01, M02 = M02,
            M10 = M10, M11 = M11, M12 = M12,
            M20 = M20, M21 = M21, M22 = M22
        };
        NormalizeCoefficients(normalized);
        double cofactor00 = normalized.M11 * normalized.M22 - normalized.M12 * normalized.M21;
        double cofactor01 = normalized.M12 * normalized.M20 - normalized.M10 * normalized.M22;
        double cofactor02 = normalized.M10 * normalized.M21 - normalized.M11 * normalized.M20;
        double determinant = normalized.M00 * cofactor00
            + normalized.M01 * cofactor01
            + normalized.M02 * cofactor02;
        double determinantScale = global::System.Math.Abs(normalized.M00 * cofactor00)
            + global::System.Math.Abs(normalized.M01 * cofactor01)
            + global::System.Math.Abs(normalized.M02 * cofactor02);
        if (!IsFinite(determinant)
            || !IsFinite(determinantScale)
            || determinantScale == 0.0
            || global::System.Math.Abs(determinant) <= determinantScale * 1.0E-14)
        {
            throw new QrInputException(
                "The perspective transform is singular.",
                "transform");
        }

        // The adjugate already represents the inverse projective mapping;
        // division by the determinant would only introduce another common scale.
        PerspectiveTransform inverse = new PerspectiveTransform
        {
            M00 = cofactor00,
            M01 = normalized.M02 * normalized.M21 - normalized.M01 * normalized.M22,
            M02 = normalized.M01 * normalized.M12 - normalized.M02 * normalized.M11,
            M10 = cofactor01,
            M11 = normalized.M00 * normalized.M22 - normalized.M02 * normalized.M20,
            M12 = normalized.M02 * normalized.M10 - normalized.M00 * normalized.M12,
            M20 = cofactor02,
            M21 = normalized.M01 * normalized.M20 - normalized.M00 * normalized.M21,
            M22 = normalized.M00 * normalized.M11 - normalized.M01 * normalized.M10
        };
        NormalizeCoefficients(inverse);
        ValidateCoefficients(inverse);
        return inverse;
    }

    static PerspectiveTransform CreateSquareToQuadrilateral(QrPoint[] points)
    {
        QrPoint topLeft = points[0];
        QrPoint topRight = points[1];
        QrPoint bottomRight = points[2];
        QrPoint bottomLeft = points[3];
        double diagonalX = topLeft.X - topRight.X
            + bottomRight.X - bottomLeft.X;
        double diagonalY = topLeft.Y - topRight.Y
            + bottomRight.Y - bottomLeft.Y;
        if (global::System.Math.Abs(diagonalX) <= 1.0E-14
            && global::System.Math.Abs(diagonalY) <= 1.0E-14)
        {
            return new PerspectiveTransform
            {
                M00 = topRight.X - topLeft.X,
                M01 = bottomLeft.X - topLeft.X,
                M02 = topLeft.X,
                M10 = topRight.Y - topLeft.Y,
                M11 = bottomLeft.Y - topLeft.Y,
                M12 = topLeft.Y,
                M22 = 1.0
            };
        }

        double rightToBottomX = topRight.X - bottomRight.X;
        double leftToBottomX = bottomLeft.X - bottomRight.X;
        double rightToBottomY = topRight.Y - bottomRight.Y;
        double leftToBottomY = bottomLeft.Y - bottomRight.Y;
        double denominator = rightToBottomX * leftToBottomY
            - leftToBottomX * rightToBottomY;
        double denominatorScale = global::System.Math.Abs(
            rightToBottomX * leftToBottomY)
            + global::System.Math.Abs(leftToBottomX * rightToBottomY);
        if (denominatorScale == 0.0
            || global::System.Math.Abs(denominator) <= denominatorScale * 1.0E-14)
        {
            throw new QrInputException(
                "The quadrilateral does not define a stable perspective transform.",
                "quadrilateral");
        }

        double perspectiveX = (diagonalX * leftToBottomY
            - leftToBottomX * diagonalY) / denominator;
        double perspectiveY = (rightToBottomX * diagonalY
            - diagonalX * rightToBottomY) / denominator;
        return new PerspectiveTransform
        {
            M00 = topRight.X - topLeft.X + perspectiveX * topRight.X,
            M01 = bottomLeft.X - topLeft.X + perspectiveY * bottomLeft.X,
            M02 = topLeft.X,
            M10 = topRight.Y - topLeft.Y + perspectiveX * topRight.Y,
            M11 = bottomLeft.Y - topLeft.Y + perspectiveY * bottomLeft.Y,
            M12 = topLeft.Y,
            M20 = perspectiveX,
            M21 = perspectiveY,
            M22 = 1.0
        };
    }

    static PerspectiveTransform Multiply(
        PerspectiveTransform left,
        PerspectiveTransform right)
    {
        return new PerspectiveTransform
        {
            M00 = left.M00 * right.M00 + left.M01 * right.M10 + left.M02 * right.M20,
            M01 = left.M00 * right.M01 + left.M01 * right.M11 + left.M02 * right.M21,
            M02 = left.M00 * right.M02 + left.M01 * right.M12 + left.M02 * right.M22,
            M10 = left.M10 * right.M00 + left.M11 * right.M10 + left.M12 * right.M20,
            M11 = left.M10 * right.M01 + left.M11 * right.M11 + left.M12 * right.M21,
            M12 = left.M10 * right.M02 + left.M11 * right.M12 + left.M12 * right.M22,
            M20 = left.M20 * right.M00 + left.M21 * right.M10 + left.M22 * right.M20,
            M21 = left.M20 * right.M01 + left.M21 * right.M11 + left.M22 * right.M21,
            M22 = left.M20 * right.M02 + left.M21 * right.M12 + left.M22 * right.M22
        };
    }

    static PerspectiveTransform CreateNormalization(
        double centerX,
        double centerY,
        double scale)
    {
        return new PerspectiveTransform
        {
            M00 = 1.0 / scale,
            M02 = -centerX / scale,
            M11 = 1.0 / scale,
            M12 = -centerY / scale,
            M22 = 1.0
        };
    }

    static PerspectiveTransform CreateDenormalization(
        double centerX,
        double centerY,
        double scale)
    {
        return new PerspectiveTransform
        {
            M00 = scale,
            M02 = centerX,
            M11 = scale,
            M12 = centerY,
            M22 = 1.0
        };
    }

    static QrPoint[] NormalizePoints(
        QrPoint[] points,
        double centerX,
        double centerY,
        double scale)
    {
        QrPoint[] normalized = new QrPoint[4];
        for (int index = 0; index < normalized.Length; index++)
        {
            normalized[index] = new QrPoint
            {
                X = (points[index].X - centerX) / scale,
                Y = (points[index].Y - centerY) / scale
            };
        }

        return normalized;
    }

    static QrPoint[] ReadQuadrilateral(
        QrQuadrilateral quadrilateral,
        string parameterName)
    {
        if (quadrilateral == null)
        {
            throw new QrInputException(
                "The quadrilateral is required.",
                parameterName);
        }

        QrPoint? topLeftValue = quadrilateral.TopLeft;
        QrPoint? topRightValue = quadrilateral.TopRight;
        QrPoint? bottomRightValue = quadrilateral.BottomRight;
        QrPoint? bottomLeftValue = quadrilateral.BottomLeft;
        if (topLeftValue == null
            || topRightValue == null
            || bottomRightValue == null
            || bottomLeftValue == null)
        {
            throw new QrInputException(
                "The quadrilateral requires four corners.",
                parameterName);
        }

        QrPoint[] points = new QrPoint[]
        {
            topLeftValue,
            topRightValue,
            bottomRightValue,
            bottomLeftValue
        };
        for (int index = 0; index < points.Length; index++)
        {
            if (!IsFinite(points[index].X) || !IsFinite(points[index].Y))
            {
                throw new QrInputException(
                    "The quadrilateral coordinates must be finite.",
                    parameterName);
            }
        }

        ValidateGeometry(points, parameterName);
        return points;
    }

    static void ValidateGeometry(QrPoint[] points, string parameterName)
    {
        double minimumX = points[0].X;
        double maximumX = points[0].X;
        double minimumY = points[0].Y;
        double maximumY = points[0].Y;
        for (int index = 1; index < points.Length; index++)
        {
            if (points[index].X < minimumX)
            {
                minimumX = points[index].X;
            }

            if (points[index].X > maximumX)
            {
                maximumX = points[index].X;
            }

            if (points[index].Y < minimumY)
            {
                minimumY = points[index].Y;
            }

            if (points[index].Y > maximumY)
            {
                maximumY = points[index].Y;
            }
        }

        double scale = maximumX - minimumX;
        if (maximumY - minimumY > scale)
        {
            scale = maximumY - minimumY;
        }

        if (!IsFinite(scale) || scale <= 0.0)
        {
            throw new QrInputException(
                "The quadrilateral has no measurable extent.",
                parameterName);
        }

        double firstCross = 0.0;
        for (int index = 0; index < points.Length; index++)
        {
            QrPoint first = points[index];
            QrPoint second = points[(index + 1) % points.Length];
            QrPoint third = points[(index + 2) % points.Length];
            // Test shape independently of coordinate units, without squaring
            // the original extent (which can overflow or underflow).
            double firstEdgeX = (second.X - first.X) / scale;
            double firstEdgeY = (second.Y - first.Y) / scale;
            double secondEdgeX = (third.X - second.X) / scale;
            double secondEdgeY = (third.Y - second.Y) / scale;
            double edgeLengthSquared = firstEdgeX * firstEdgeX
                + firstEdgeY * firstEdgeY;
            double cross = firstEdgeX * secondEdgeY - firstEdgeY * secondEdgeX;
            if (!IsFinite(edgeLengthSquared)
                || !IsFinite(cross)
                || edgeLengthSquared <= 1.0E-24
                || global::System.Math.Abs(cross) <= 1.0E-12
                || index > 0 && (cross > 0.0) != (firstCross > 0.0))
            {
                throw new QrInputException(
                    "The quadrilateral must be non-degenerate and convex.",
                    parameterName);
            }

            if (index == 0)
            {
                firstCross = cross;
            }
        }
    }

    static double GetCenterX(QrPoint[] points)
    {
        double minimum = points[0].X;
        double maximum = points[0].X;
        for (int index = 1; index < points.Length; index++)
        {
            if (points[index].X < minimum)
            {
                minimum = points[index].X;
            }

            if (points[index].X > maximum)
            {
                maximum = points[index].X;
            }
        }

        return minimum + (maximum - minimum) / 2.0;
    }

    static double GetCenterY(QrPoint[] points)
    {
        double minimum = points[0].Y;
        double maximum = points[0].Y;
        for (int index = 1; index < points.Length; index++)
        {
            if (points[index].Y < minimum)
            {
                minimum = points[index].Y;
            }

            if (points[index].Y > maximum)
            {
                maximum = points[index].Y;
            }
        }

        return minimum + (maximum - minimum) / 2.0;
    }

    static double GetScale(QrPoint[] points)
    {
        double minimumX = points[0].X;
        double maximumX = points[0].X;
        double minimumY = points[0].Y;
        double maximumY = points[0].Y;
        for (int index = 1; index < points.Length; index++)
        {
            if (points[index].X < minimumX)
            {
                minimumX = points[index].X;
            }

            if (points[index].X > maximumX)
            {
                maximumX = points[index].X;
            }

            if (points[index].Y < minimumY)
            {
                minimumY = points[index].Y;
            }

            if (points[index].Y > maximumY)
            {
                maximumY = points[index].Y;
            }
        }

        double scale = maximumX - minimumX;
        if (maximumY - minimumY > scale)
        {
            scale = maximumY - minimumY;
        }

        return scale;
    }

    static void NormalizeCoefficients(PerspectiveTransform transform)
    {
        double scale = global::System.Math.Abs(transform.M00);
        scale = SelectMaximum(scale, global::System.Math.Abs(transform.M01));
        scale = SelectMaximum(scale, global::System.Math.Abs(transform.M02));
        scale = SelectMaximum(scale, global::System.Math.Abs(transform.M10));
        scale = SelectMaximum(scale, global::System.Math.Abs(transform.M11));
        scale = SelectMaximum(scale, global::System.Math.Abs(transform.M12));
        scale = SelectMaximum(scale, global::System.Math.Abs(transform.M20));
        scale = SelectMaximum(scale, global::System.Math.Abs(transform.M21));
        scale = SelectMaximum(scale, global::System.Math.Abs(transform.M22));
        if (!IsFinite(scale) || scale == 0.0)
        {
            throw new QrInputException(
                "The perspective transform has invalid coefficients.",
                "transform");
        }

        transform.M00 /= scale;
        transform.M01 /= scale;
        transform.M02 /= scale;
        transform.M10 /= scale;
        transform.M11 /= scale;
        transform.M12 /= scale;
        transform.M20 /= scale;
        transform.M21 /= scale;
        transform.M22 /= scale;
    }

    static void ValidateCoefficients(PerspectiveTransform transform)
    {
        if (transform == null
            || !IsFinite(transform.M00)
            || !IsFinite(transform.M01)
            || !IsFinite(transform.M02)
            || !IsFinite(transform.M10)
            || !IsFinite(transform.M11)
            || !IsFinite(transform.M12)
            || !IsFinite(transform.M20)
            || !IsFinite(transform.M21)
            || !IsFinite(transform.M22))
        {
            throw new QrInputException(
                "The perspective transform coefficients must be finite.",
                "transform");
        }
    }

    static double SelectMaximum(double first, double second)
    {
        return first > second ? first : second;
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
