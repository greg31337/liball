using System;

namespace geo;

internal static class RobustLineIntersection
{
    internal static Coordinate? GetInfiniteLineIntersection(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        ValidateInputs(firstStart, firstEnd, secondStart, secondEnd);
        if (CoordinatesEqualPlanar(firstStart, firstEnd) ||
            CoordinatesEqualPlanar(secondStart, secondEnd))
        {
            return null;
        }
        ExactDyadicValue denominator = CreateDirectionDeterminant(
            firstStart, firstEnd, secondStart, secondEnd);
        if (denominator.Sign == 0) return null;
        ExactDyadicValue numerator = CreateLineParameterNumerator(
            firstStart, secondStart, secondEnd);
        double parameter = ExactDyadicArithmetic.DivideValues(numerator, denominator);
        if (!double.IsFinite(parameter))
        {
            throw new InvalidGeometryException(
                "The infinite-line intersection is outside the finite double range.");
        }
        return InterpolateLineCoordinate(firstStart, firstEnd, parameter);
    }

    internal static SegmentIntersectionResult GetSegmentIntersection(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        ValidateInputs(firstStart, firstEnd, secondStart, secondEnd);
        SegmentIntersectionResult result = CreateEmptyResult();
        if (!EnvelopesIntersect(firstStart, firstEnd, secondStart, secondEnd)) return result;

        int firstStartSide = RobustOrientation.GetOrientationIndex(
            firstStart, firstEnd, secondStart);
        int firstEndSide = RobustOrientation.GetOrientationIndex(
            firstStart, firstEnd, secondEnd);
        int secondStartSide = RobustOrientation.GetOrientationIndex(
            secondStart, secondEnd, firstStart);
        int secondEndSide = RobustOrientation.GetOrientationIndex(
            secondStart, secondEnd, firstEnd);

        if (firstStartSide == 0 && firstEndSide == 0 &&
            secondStartSide == 0 && secondEndSide == 0)
        {
            return CreateCollinearResult(
                firstStart, firstEnd, secondStart, secondEnd);
        }

        Coordinate? endpoint = FindEndpointIntersection(
            firstStart, firstEnd, secondStart, secondEnd,
            firstStartSide, firstEndSide, secondStartSide, secondEndSide);
        if (endpoint != null)
        {
            result.Type = SegmentIntersectionType.Endpoint;
            result.Location = TopologicalLocation.Boundary;
            result.FirstIntersection = endpoint;
            return result;
        }

        if (SignsAreOpposite(firstStartSide, firstEndSide) &&
            SignsAreOpposite(secondStartSide, secondEndSide))
        {
            Coordinate? intersection = GetInfiniteLineIntersection(
                firstStart, firstEnd, secondStart, secondEnd);
            if (intersection == null)
            {
                throw new InvalidGeometryException(
                    "Properly crossing segments must have a finite line intersection.");
            }
            ClampToIntersectionEnvelope(
                intersection, firstStart, firstEnd, secondStart, secondEnd);
            result.Type = SegmentIntersectionType.ProperPoint;
            result.Location = TopologicalLocation.Interior;
            result.FirstIntersection = intersection;
            result.IsProper = true;
        }
        return result;
    }

    internal static ExactDyadicValue CreateDirectionDeterminant(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        BinaryProduct[] products =
        [
            RobustDeterminant.CreateBinaryProduct(firstEnd.X, secondEnd.Y),
            RobustDeterminant.CreateBinaryProduct(firstEnd.X, secondStart.Y),
            RobustDeterminant.CreateBinaryProduct(firstStart.X, secondEnd.Y),
            RobustDeterminant.CreateBinaryProduct(firstStart.X, secondStart.Y),
            RobustDeterminant.CreateBinaryProduct(firstEnd.Y, secondEnd.X),
            RobustDeterminant.CreateBinaryProduct(firstEnd.Y, secondStart.X),
            RobustDeterminant.CreateBinaryProduct(firstStart.Y, secondEnd.X),
            RobustDeterminant.CreateBinaryProduct(firstStart.Y, secondStart.X)
        ];
        int[] coefficients = [1, -1, -1, 1, -1, 1, 1, -1];
        return ExactDyadicArithmetic.CreateProductSum(products, coefficients);
    }

    internal static ExactDyadicValue CreateLineParameterNumerator(
        Coordinate lineStart, Coordinate otherStart, Coordinate otherEnd)
    {
        BinaryProduct[] products = RobustOrientation.CreateOrientationProducts(
            lineStart.X, lineStart.Y, otherStart.X, otherStart.Y,
            otherEnd.X, otherEnd.Y);
        int[] coefficients = [1, -1, -1, 1, 1, -1];
        return ExactDyadicArithmetic.CreateProductSum(products, coefficients);
    }

    internal static SegmentIntersectionResult CreateCollinearResult(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        Coordinate firstLow = ComparePlanarCoordinates(firstStart, firstEnd) <= 0 ?
            firstStart : firstEnd;
        Coordinate firstHigh = firstLow == firstStart ? firstEnd : firstStart;
        Coordinate secondLow = ComparePlanarCoordinates(secondStart, secondEnd) <= 0 ?
            secondStart : secondEnd;
        Coordinate secondHigh = secondLow == secondStart ? secondEnd : secondStart;
        Coordinate overlapLow = ComparePlanarCoordinates(firstLow, secondLow) >= 0 ?
            firstLow : secondLow;
        Coordinate overlapHigh = ComparePlanarCoordinates(firstHigh, secondHigh) <= 0 ?
            firstHigh : secondHigh;
        SegmentIntersectionResult result = CreateEmptyResult();
        int comparison = ComparePlanarCoordinates(overlapLow, overlapHigh);
        if (comparison > 0) return result;
        result.Location = TopologicalLocation.Boundary;
        result.FirstIntersection = CopyCoordinate(overlapLow);
        if (comparison == 0)
        {
            result.Type = SegmentIntersectionType.CollinearPoint;
            return result;
        }
        result.Type = SegmentIntersectionType.CollinearOverlap;
        result.SecondIntersection = CopyCoordinate(overlapHigh);
        return result;
    }

    internal static Coordinate? FindEndpointIntersection(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd,
        int firstStartSide, int firstEndSide,
        int secondStartSide, int secondEndSide)
    {
        if (firstStartSide == 0 && PointIsInEnvelope(secondStart, firstStart, firstEnd))
        {
            return CopyCoordinate(secondStart);
        }
        if (firstEndSide == 0 && PointIsInEnvelope(secondEnd, firstStart, firstEnd))
        {
            return CopyCoordinate(secondEnd);
        }
        if (secondStartSide == 0 && PointIsInEnvelope(firstStart, secondStart, secondEnd))
        {
            return CopyCoordinate(firstStart);
        }
        if (secondEndSide == 0 && PointIsInEnvelope(firstEnd, secondStart, secondEnd))
        {
            return CopyCoordinate(firstEnd);
        }
        return null;
    }

    internal static Coordinate InterpolateLineCoordinate(
        Coordinate start, Coordinate end, double parameter)
    {
        Coordinate result = new Coordinate();
        result.X = InterpolateLineOrdinate(start.X, end.X, parameter);
        result.Y = InterpolateLineOrdinate(start.Y, end.Y, parameter);
        result.Z = InterpolateOptionalLineOrdinate(start.Z, end.Z, parameter);
        result.M = InterpolateOptionalLineOrdinate(start.M, end.M, parameter);
        return result;
    }

    internal static double InterpolateLineOrdinate(double start, double end, double parameter)
    {
        if (parameter == 0.0) return start;
        if (parameter == 1.0) return end;
        double scale = Math.Abs(start);
        double absoluteEnd = Math.Abs(end);
        if (absoluteEnd > scale) scale = absoluteEnd;
        if (scale == 0.0) return 0.0;
        double scaledStart = start / scale;
        double scaledDelta = end / scale - scaledStart;
        double value = Math.FusedMultiplyAdd(parameter, scaledDelta, scaledStart) * scale;
        return VectorMath.RequireFiniteResult(value, "Line intersection ordinate");
    }

    internal static double InterpolateOptionalLineOrdinate(
        double start, double end, double parameter)
    {
        if (parameter == 0.0) return start;
        if (parameter == 1.0) return end;
        if (double.IsNaN(start) || double.IsNaN(end)) return double.NaN;
        return InterpolateLineOrdinate(start, end, parameter);
    }

    internal static void ClampToIntersectionEnvelope(
        Coordinate point, Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        double minimumX = Math.Max(
            Math.Min(firstStart.X, firstEnd.X), Math.Min(secondStart.X, secondEnd.X));
        double maximumX = Math.Min(
            Math.Max(firstStart.X, firstEnd.X), Math.Max(secondStart.X, secondEnd.X));
        double minimumY = Math.Max(
            Math.Min(firstStart.Y, firstEnd.Y), Math.Min(secondStart.Y, secondEnd.Y));
        double maximumY = Math.Min(
            Math.Max(firstStart.Y, firstEnd.Y), Math.Max(secondStart.Y, secondEnd.Y));
        if (point.X < minimumX) point.X = minimumX;
        if (point.X > maximumX) point.X = maximumX;
        if (point.Y < minimumY) point.Y = minimumY;
        if (point.Y > maximumY) point.Y = maximumY;
    }

    internal static SegmentIntersectionResult CreateEmptyResult()
    {
        SegmentIntersectionResult result = new SegmentIntersectionResult();
        result.Location = TopologicalLocation.Exterior;
        return result;
    }

    internal static Coordinate CopyCoordinate(Coordinate source)
    {
        Coordinate copy = new Coordinate();
        copy.X = source.X;
        copy.Y = source.Y;
        copy.Z = source.Z;
        copy.M = source.M;
        return copy;
    }

    internal static bool CoordinatesEqualPlanar(Coordinate first, Coordinate second)
    {
        return first.X == second.X && first.Y == second.Y;
    }

    internal static int ComparePlanarCoordinates(Coordinate first, Coordinate second)
    {
        if (first.X < second.X) return -1;
        if (first.X > second.X) return 1;
        if (first.Y < second.Y) return -1;
        if (first.Y > second.Y) return 1;
        return 0;
    }

    internal static bool SignsAreOpposite(int first, int second)
    {
        return first < 0 && second > 0 || first > 0 && second < 0;
    }

    internal static bool PointIsInEnvelope(
        Coordinate point, Coordinate start, Coordinate end)
    {
        return point.X >= Math.Min(start.X, end.X) &&
            point.X <= Math.Max(start.X, end.X) &&
            point.Y >= Math.Min(start.Y, end.Y) &&
            point.Y <= Math.Max(start.Y, end.Y);
    }

    internal static bool EnvelopesIntersect(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        return Math.Min(firstStart.X, firstEnd.X) <= Math.Max(secondStart.X, secondEnd.X) &&
            Math.Max(firstStart.X, firstEnd.X) >= Math.Min(secondStart.X, secondEnd.X) &&
            Math.Min(firstStart.Y, firstEnd.Y) <= Math.Max(secondStart.Y, secondEnd.Y) &&
            Math.Max(firstStart.Y, firstEnd.Y) >= Math.Min(secondStart.Y, secondEnd.Y);
    }

    internal static void ValidateInputs(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        VectorMath.ValidateCoordinateValue(firstStart, "First line start");
        VectorMath.ValidateCoordinateValue(firstEnd, "First line end");
        VectorMath.ValidateCoordinateValue(secondStart, "Second line start");
        VectorMath.ValidateCoordinateValue(secondEnd, "Second line end");
    }
}
