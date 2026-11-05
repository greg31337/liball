using System;

namespace geo;

internal static class SegmentDistanceAlgorithms
{
    internal static bool IsPointOnLine(
        Coordinate point, Coordinate lineStart, Coordinate lineEnd)
    {
        ValidatePointAndSegment(point, lineStart, lineEnd);
        if (RobustLineIntersection.CoordinatesEqualPlanar(lineStart, lineEnd))
        {
            return RobustLineIntersection.CoordinatesEqualPlanar(point, lineStart);
        }
        return RobustOrientation.GetOrientationIndex(lineStart, lineEnd, point) == 0;
    }

    internal static TopologicalLocation LocatePointOnSegment(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd)
    {
        ValidatePointAndSegment(point, segmentStart, segmentEnd);
        if (!IsPointInSegmentEnvelope(point, segmentStart, segmentEnd))
        {
            return TopologicalLocation.Exterior;
        }
        if (RobustOrientation.GetOrientationIndex(segmentStart, segmentEnd, point) != 0)
        {
            return TopologicalLocation.Exterior;
        }
        if (RobustLineIntersection.CoordinatesEqualPlanar(point, segmentStart) ||
            RobustLineIntersection.CoordinatesEqualPlanar(point, segmentEnd))
        {
            return TopologicalLocation.Boundary;
        }
        return TopologicalLocation.Interior;
    }

    internal static bool IsPointOnSegment(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd)
    {
        return LocatePointOnSegment(point, segmentStart, segmentEnd) !=
            TopologicalLocation.Exterior;
    }

    internal static bool IsPointInSegmentEnvelope(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd)
    {
        ValidatePointAndSegment(point, segmentStart, segmentEnd);
        return RobustLineIntersection.PointIsInEnvelope(point, segmentStart, segmentEnd);
    }

    internal static bool DoSegmentEnvelopesIntersect(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        RobustLineIntersection.ValidateInputs(
            firstStart, firstEnd, secondStart, secondEnd);
        return RobustLineIntersection.EnvelopesIntersect(
            firstStart, firstEnd, secondStart, secondEnd);
    }

    internal static bool CanSegmentIntersectEnvelope(
        Coordinate segmentStart, Coordinate segmentEnd, Envelope envelope)
    {
        VectorMath.ValidateCoordinateValue(segmentStart, "Segment start");
        VectorMath.ValidateCoordinateValue(segmentEnd, "Segment end");
        bool empty = EnvelopeAlgorithms.ValidateAndCheckEmpty(envelope);
        if (empty) return false;
        return Math.Min(segmentStart.X, segmentEnd.X) <= envelope.MaxX &&
            Math.Max(segmentStart.X, segmentEnd.X) >= envelope.MinX &&
            Math.Min(segmentStart.Y, segmentEnd.Y) <= envelope.MaxY &&
            Math.Max(segmentStart.Y, segmentEnd.Y) >= envelope.MinY;
    }

    internal static DistanceResult GetNearestPointPair(
        Coordinate first, Coordinate second)
    {
        VectorMath.ValidateCoordinateValue(first, "First point");
        VectorMath.ValidateCoordinateValue(second, "Second point");
        return CreateWitnessResult(
            RobustLineIntersection.CopyCoordinate(first),
            RobustLineIntersection.CopyCoordinate(second),
            false, 0.0, false, 0.0);
    }

    internal static DistanceResult GetNearestPointSegment(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd)
    {
        ValidatePointAndSegment(point, segmentStart, segmentEnd);
        double factor = GetExactProjectionFactor(point, segmentStart, segmentEnd);
        if (double.IsNaN(factor)) factor = 0.0;
        factor = ClampSegmentFactor(factor);
        Coordinate nearest = CreatePointAtSegmentFactor(segmentStart, segmentEnd, factor);
        return CreateWitnessResult(
            RobustLineIntersection.CopyCoordinate(point), nearest,
            false, 0.0, true, factor);
    }

    internal static DistanceResult GetNearestSegmentPair(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        RobustLineIntersection.ValidateInputs(
            firstStart, firstEnd, secondStart, secondEnd);
        SegmentIntersectionResult intersection =
            RobustLineIntersection.GetSegmentIntersection(
                firstStart, firstEnd, secondStart, secondEnd);
        if (intersection.Type != SegmentIntersectionType.None)
        {
            return CreateIntersectionWitnessResult(
                intersection, firstStart, firstEnd, secondStart, secondEnd);
        }

        DistanceResult best = CreateEndpointToSegmentCandidate(
            firstStart, 0.0, secondStart, secondEnd, true);
        DistanceResult candidate = CreateEndpointToSegmentCandidate(
            firstEnd, 1.0, secondStart, secondEnd, true);
        best = SelectCloserResult(best, candidate);
        candidate = CreateEndpointToSegmentCandidate(
            secondStart, 0.0, firstStart, firstEnd, false);
        best = SelectCloserResult(best, candidate);
        candidate = CreateEndpointToSegmentCandidate(
            secondEnd, 1.0, firstStart, firstEnd, false);
        return SelectCloserResult(best, candidate);
    }

    internal static double GetExactProjectionFactor(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd)
    {
        ValidatePointAndSegment(point, segmentStart, segmentEnd);
        ExactDyadicValue denominator = CreateSquaredSegmentLength(
            segmentStart, segmentEnd);
        if (denominator.Sign == 0) return double.NaN;
        ExactDyadicValue numerator = CreateProjectionNumerator(
            point, segmentStart, segmentEnd);
        return ExactDyadicArithmetic.DivideValues(numerator, denominator);
    }

    internal static ExactDyadicValue CreateProjectionNumerator(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd)
    {
        BinaryProduct[] products =
        [
            RobustDeterminant.CreateBinaryProduct(point.X, segmentEnd.X),
            RobustDeterminant.CreateBinaryProduct(point.X, segmentStart.X),
            RobustDeterminant.CreateBinaryProduct(segmentStart.X, segmentEnd.X),
            RobustDeterminant.CreateBinaryProduct(segmentStart.X, segmentStart.X),
            RobustDeterminant.CreateBinaryProduct(point.Y, segmentEnd.Y),
            RobustDeterminant.CreateBinaryProduct(point.Y, segmentStart.Y),
            RobustDeterminant.CreateBinaryProduct(segmentStart.Y, segmentEnd.Y),
            RobustDeterminant.CreateBinaryProduct(segmentStart.Y, segmentStart.Y)
        ];
        int[] coefficients = [1, -1, -1, 1, 1, -1, -1, 1];
        return ExactDyadicArithmetic.CreateProductSum(products, coefficients);
    }

    internal static ExactDyadicValue CreateSquaredSegmentLength(
        Coordinate segmentStart, Coordinate segmentEnd)
    {
        BinaryProduct xCross = RobustDeterminant.CreateBinaryProduct(
            segmentStart.X, segmentEnd.X);
        BinaryProduct yCross = RobustDeterminant.CreateBinaryProduct(
            segmentStart.Y, segmentEnd.Y);
        BinaryProduct[] products =
        [
            RobustDeterminant.CreateBinaryProduct(segmentEnd.X, segmentEnd.X),
            xCross,
            xCross,
            RobustDeterminant.CreateBinaryProduct(segmentStart.X, segmentStart.X),
            RobustDeterminant.CreateBinaryProduct(segmentEnd.Y, segmentEnd.Y),
            yCross,
            yCross,
            RobustDeterminant.CreateBinaryProduct(segmentStart.Y, segmentStart.Y)
        ];
        int[] coefficients = [1, -1, -1, 1, 1, -1, -1, 1];
        return ExactDyadicArithmetic.CreateProductSum(products, coefficients);
    }

    internal static DistanceResult CreateIntersectionWitnessResult(
        SegmentIntersectionResult intersection,
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        Coordinate? witness = intersection.FirstIntersection;
        if (witness == null)
        {
            throw new InvalidGeometryException(
                "An intersecting segment result requires a witness coordinate.");
        }
        double firstFactor = GetExactProjectionFactor(witness, firstStart, firstEnd);
        double secondFactor = GetExactProjectionFactor(witness, secondStart, secondEnd);
        if (double.IsNaN(firstFactor)) firstFactor = 0.0;
        if (double.IsNaN(secondFactor)) secondFactor = 0.0;
        firstFactor = ClampSegmentFactor(firstFactor);
        secondFactor = ClampSegmentFactor(secondFactor);
        Coordinate firstWitness = CreatePointAtSegmentFactor(
            firstStart, firstEnd, firstFactor);
        Coordinate secondWitness = CreatePointAtSegmentFactor(
            secondStart, secondEnd, secondFactor);
        DistanceResult result = CreateWitnessResult(
            firstWitness, secondWitness, true, firstFactor, true, secondFactor);
        result.Distance = 0.0;
        return result;
    }

    internal static DistanceResult CreateEndpointToSegmentCandidate(
        Coordinate endpoint, double endpointFactor,
        Coordinate segmentStart, Coordinate segmentEnd,
        bool endpointBelongsToFirst)
    {
        DistanceResult pointResult = GetNearestPointSegment(
            endpoint, segmentStart, segmentEnd);
        if (endpointBelongsToFirst)
        {
            pointResult.FirstLocation = CreateLinearLocation(endpointFactor);
            return pointResult;
        }
        Coordinate? firstCoordinate = pointResult.SecondCoordinate;
        Coordinate? secondCoordinate = pointResult.FirstCoordinate;
        LinearLocation? firstLocation = pointResult.SecondLocation;
        if (firstCoordinate == null || secondCoordinate == null || firstLocation == null)
        {
            throw new InvalidGeometryException(
                "A point-segment distance requires coordinates and a segment location.");
        }
        pointResult.FirstCoordinate = firstCoordinate;
        pointResult.SecondCoordinate = secondCoordinate;
        pointResult.FirstLocation = firstLocation;
        pointResult.SecondLocation = CreateLinearLocation(endpointFactor);
        return pointResult;
    }

    internal static DistanceResult SelectCloserResult(
        DistanceResult current, DistanceResult candidate)
    {
        Coordinate? currentFirst = current.FirstCoordinate;
        Coordinate? currentSecond = current.SecondCoordinate;
        Coordinate? candidateFirst = candidate.FirstCoordinate;
        Coordinate? candidateSecond = candidate.SecondCoordinate;
        if (currentFirst == null || currentSecond == null ||
            candidateFirst == null || candidateSecond == null)
        {
            throw new InvalidGeometryException(
                "Nearest-distance candidates require two witness coordinates.");
        }
        ExactDyadicValue currentSquared = CreateSquaredSegmentLength(
            currentFirst, currentSecond);
        ExactDyadicValue candidateSquared = CreateSquaredSegmentLength(
            candidateFirst, candidateSecond);
        if (ExactDyadicArithmetic.CompareAbsoluteValues(
            candidateSquared, currentSquared) < 0) return candidate;
        return current;
    }

    internal static DistanceResult CreateWitnessResult(
        Coordinate first, Coordinate second,
        bool firstHasLocation, double firstFactor,
        bool secondHasLocation, double secondFactor)
    {
        DistanceResult result = new DistanceResult();
        result.HasResult = true;
        result.Distance = GetPlanarDistance(first, second);
        result.FirstCoordinate = first;
        result.SecondCoordinate = second;
        if (firstHasLocation) result.FirstLocation = CreateLinearLocation(firstFactor);
        if (secondHasLocation) result.SecondLocation = CreateLinearLocation(secondFactor);
        return result;
    }

    internal static LinearLocation CreateLinearLocation(double factor)
    {
        LinearLocation location = new LinearLocation();
        location.SegmentFraction = factor;
        return location;
    }

    internal static Coordinate CreatePointAtSegmentFactor(
        Coordinate segmentStart, Coordinate segmentEnd, double factor)
    {
        if (factor <= 0.0) return RobustLineIntersection.CopyCoordinate(segmentStart);
        if (factor >= 1.0) return RobustLineIntersection.CopyCoordinate(segmentEnd);
        return VectorMath.InterpolateCoordinate(segmentStart, segmentEnd, factor);
    }

    internal static double ClampSegmentFactor(double factor)
    {
        if (factor <= 0.0) return 0.0;
        if (factor >= 1.0) return 1.0;
        return factor;
    }

    internal static double GetPlanarDistance(Coordinate first, Coordinate second)
    {
        double horizontal = first.X - second.X;
        double vertical = first.Y - second.Y;
        if (double.IsFinite(horizontal) && double.IsFinite(vertical))
        {
            return EnvelopeAlgorithms.ScaledDistance(
                Math.Abs(horizontal), Math.Abs(vertical));
        }
        double scale = Math.Abs(first.X);
        double candidate = Math.Abs(first.Y);
        if (candidate > scale) scale = candidate;
        candidate = Math.Abs(second.X);
        if (candidate > scale) scale = candidate;
        candidate = Math.Abs(second.Y);
        if (candidate > scale) scale = candidate;
        if (scale == 0.0) return 0.0;
        double deltaX = first.X / scale - second.X / scale;
        double deltaY = first.Y / scale - second.Y / scale;
        return scale * Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    internal static void ValidatePointAndSegment(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd)
    {
        VectorMath.ValidateCoordinateValue(point, "Point");
        VectorMath.ValidateCoordinateValue(segmentStart, "Segment start");
        VectorMath.ValidateCoordinateValue(segmentEnd, "Segment end");
    }
}
