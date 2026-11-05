using System;

namespace geo;

internal static class VectorMath
{
    internal static Coordinate AddCoordinateVectors(Coordinate first, Coordinate second)
    {
        ValidateCoordinateValue(first, "First vector");
        ValidateCoordinateValue(second, "Second vector");
        Coordinate result = new Coordinate();
        result.X = RequireFiniteResult(first.X + second.X, "Vector addition X ordinate");
        result.Y = RequireFiniteResult(first.Y + second.Y, "Vector addition Y ordinate");
        result.Z = AddOptionalOrdinates(first.Z, second.Z);
        result.M = AddOptionalOrdinates(first.M, second.M);
        return result;
    }

    internal static Coordinate SubtractCoordinateVectors(Coordinate first, Coordinate second)
    {
        ValidateCoordinateValue(first, "First vector");
        ValidateCoordinateValue(second, "Second vector");
        Coordinate result = new Coordinate();
        result.X = RequireFiniteResult(first.X - second.X, "Vector subtraction X ordinate");
        result.Y = RequireFiniteResult(first.Y - second.Y, "Vector subtraction Y ordinate");
        result.Z = SubtractOptionalOrdinates(first.Z, second.Z);
        result.M = SubtractOptionalOrdinates(first.M, second.M);
        return result;
    }

    internal static Coordinate ScaleCoordinateVector(Coordinate vector, double factor)
    {
        ValidateCoordinateValue(vector, "Vector");
        ValidateFiniteValue(factor, "Vector scale factor");
        Coordinate result = new Coordinate();
        result.X = RequireFiniteResult(vector.X * factor, "Scaled vector X ordinate");
        result.Y = RequireFiniteResult(vector.Y * factor, "Scaled vector Y ordinate");
        result.Z = ScaleOptionalOrdinate(vector.Z, factor);
        result.M = ScaleOptionalOrdinate(vector.M, factor);
        return result;
    }

    internal static Coordinate NegateCoordinateVector(Coordinate vector)
    {
        return ScaleCoordinateVector(vector, -1.0);
    }

    internal static double DotProduct(Coordinate first, Coordinate second)
    {
        ValidateCoordinateValue(first, "First vector");
        ValidateCoordinateValue(second, "Second vector");
        return first.X * second.X + first.Y * second.Y;
    }

    internal static double CrossProduct(Coordinate first, Coordinate second)
    {
        ValidateCoordinateValue(first, "First vector");
        ValidateCoordinateValue(second, "Second vector");
        return first.X * second.Y - first.Y * second.X;
    }

    internal static double GetVectorLength(Coordinate vector)
    {
        ValidateCoordinateValue(vector, "Vector");
        double absoluteX = Math.Abs(vector.X);
        double absoluteY = Math.Abs(vector.Y);
        double scale = absoluteX > absoluteY ? absoluteX : absoluteY;
        if (scale == 0.0) return 0.0;
        double scaledX = vector.X / scale;
        double scaledY = vector.Y / scale;
        return scale * Math.Sqrt(scaledX * scaledX + scaledY * scaledY);
    }

    internal static Coordinate NormalizeCoordinateVector(Coordinate vector)
    {
        ValidateCoordinateValue(vector, "Vector");
        double absoluteX = Math.Abs(vector.X);
        double absoluteY = Math.Abs(vector.Y);
        double scale = absoluteX > absoluteY ? absoluteX : absoluteY;
        if (scale == 0.0)
        {
            throw new InvalidGeometryException("A zero-length vector cannot be normalized.");
        }
        double scaledX = vector.X / scale;
        double scaledY = vector.Y / scale;
        double scaledLength = Math.Sqrt(scaledX * scaledX + scaledY * scaledY);
        Coordinate result = new Coordinate();
        result.X = scaledX / scaledLength;
        result.Y = scaledY / scaledLength;
        return result;
    }

    internal static int GetQuadrant(double x, double y)
    {
        ValidateFiniteValue(x, "Vector X ordinate");
        ValidateFiniteValue(y, "Vector Y ordinate");
        if (x == 0.0 && y == 0.0)
        {
            throw new InvalidGeometryException("A zero-length vector has no quadrant.");
        }
        if (x >= 0.0) return y >= 0.0 ? 0 : 3;
        return y >= 0.0 ? 1 : 2;
    }

    internal static bool AreOppositeQuadrants(int first, int second)
    {
        ValidateQuadrant(first);
        ValidateQuadrant(second);
        int difference = first - second;
        if (difference < 0) difference = -difference;
        return difference == 2;
    }

    internal static bool IsNorthernQuadrant(int quadrant)
    {
        ValidateQuadrant(quadrant);
        return quadrant == 0 || quadrant == 1;
    }

    internal static Coordinate InterpolateCoordinate(
        Coordinate start, Coordinate end, double factor)
    {
        ValidateCoordinateValue(start, "Interpolation start coordinate");
        ValidateCoordinateValue(end, "Interpolation end coordinate");
        ValidateFiniteValue(factor, "Interpolation factor");
        Coordinate result = new Coordinate();
        result.X = InterpolateFiniteOrdinate(start.X, end.X, factor);
        result.Y = InterpolateFiniteOrdinate(start.Y, end.Y, factor);
        result.Z = InterpolateOptionalOrdinate(start.Z, end.Z, factor);
        result.M = InterpolateOptionalOrdinate(start.M, end.M, factor);
        return result;
    }

    internal static double GetProjectionFactor(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd)
    {
        ValidateCoordinateValue(point, "Projection coordinate");
        ValidateCoordinateValue(segmentStart, "Segment start coordinate");
        ValidateCoordinateValue(segmentEnd, "Segment end coordinate");
        double scale = MaximumAbsolutePlanarOrdinate(point, segmentStart, segmentEnd);
        if (scale == 0.0) return double.NaN;
        double startX = segmentStart.X / scale;
        double startY = segmentStart.Y / scale;
        double deltaX = segmentEnd.X / scale - startX;
        double deltaY = segmentEnd.Y / scale - startY;
        double denominator = deltaX * deltaX + deltaY * deltaY;
        if (denominator == 0.0) return double.NaN;
        double pointX = point.X / scale - startX;
        double pointY = point.Y / scale - startY;
        return (pointX * deltaX + pointY * deltaY) / denominator;
    }

    internal static double GetAngleBetween(Coordinate first, Coordinate second)
    {
        Coordinate firstUnit = NormalizeCoordinateVector(first);
        Coordinate secondUnit = NormalizeCoordinateVector(second);
        double dot = firstUnit.X * secondUnit.X + firstUnit.Y * secondUnit.Y;
        double cross = firstUnit.X * secondUnit.Y - firstUnit.Y * secondUnit.X;
        return Math.Atan2(Math.Abs(cross), dot);
    }

    internal static double GetOrientedAngle(Coordinate first, Coordinate second)
    {
        Coordinate firstUnit = NormalizeCoordinateVector(first);
        Coordinate secondUnit = NormalizeCoordinateVector(second);
        double dot = firstUnit.X * secondUnit.X + firstUnit.Y * secondUnit.Y;
        double cross = firstUnit.X * secondUnit.Y - firstUnit.Y * secondUnit.X;
        return Math.Atan2(cross, dot);
    }

    internal static int CompareCoordinateValues(Coordinate first, Coordinate second)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException("Coordinates being compared cannot be null.");
        }
        int comparison = CompareOrdinateValues(first.X, second.X);
        if (comparison != 0) return comparison;
        comparison = CompareOrdinateValues(first.Y, second.Y);
        if (comparison != 0) return comparison;
        comparison = CompareOrdinateValues(first.Z, second.Z);
        if (comparison != 0) return comparison;
        return CompareOrdinateValues(first.M, second.M);
    }

    internal static int CompareOrdinateValues(double first, double second)
    {
        bool firstMissing = double.IsNaN(first);
        bool secondMissing = double.IsNaN(second);
        if (firstMissing && secondMissing) return 0;
        if (firstMissing) return -1;
        if (secondMissing) return 1;
        if (first < second) return -1;
        if (first > second) return 1;
        return 0;
    }

    internal static double InterpolateFiniteOrdinate(double start, double end, double factor)
    {
        if (factor == 0.0) return start;
        if (factor == 1.0) return end;
        double scale = Math.Abs(start);
        double absoluteEnd = Math.Abs(end);
        if (absoluteEnd > scale) scale = absoluteEnd;
        if (scale == 0.0) return 0.0;
        double result = ((1.0 - factor) * (start / scale) + factor * (end / scale)) * scale;
        return RequireFiniteResult(result, "Interpolated ordinate");
    }

    internal static double InterpolateOptionalOrdinate(double start, double end, double factor)
    {
        if (factor == 0.0) return start;
        if (factor == 1.0) return end;
        if (double.IsNaN(start) || double.IsNaN(end)) return double.NaN;
        return InterpolateFiniteOrdinate(start, end, factor);
    }

    internal static double AddOptionalOrdinates(double first, double second)
    {
        if (double.IsNaN(first) || double.IsNaN(second)) return double.NaN;
        return RequireFiniteResult(first + second, "Added optional ordinate");
    }

    internal static double SubtractOptionalOrdinates(double first, double second)
    {
        if (double.IsNaN(first) || double.IsNaN(second)) return double.NaN;
        return RequireFiniteResult(first - second, "Subtracted optional ordinate");
    }

    internal static double ScaleOptionalOrdinate(double value, double factor)
    {
        if (double.IsNaN(value)) return double.NaN;
        return RequireFiniteResult(value * factor, "Scaled optional ordinate");
    }

    internal static double MaximumAbsolutePlanarOrdinate(
        Coordinate first, Coordinate second, Coordinate third)
    {
        double maximum = Math.Abs(first.X);
        double candidate = Math.Abs(first.Y);
        if (candidate > maximum) maximum = candidate;
        candidate = Math.Abs(second.X);
        if (candidate > maximum) maximum = candidate;
        candidate = Math.Abs(second.Y);
        if (candidate > maximum) maximum = candidate;
        candidate = Math.Abs(third.X);
        if (candidate > maximum) maximum = candidate;
        candidate = Math.Abs(third.Y);
        if (candidate > maximum) maximum = candidate;
        return maximum;
    }

    internal static double RequireFiniteResult(double value, string description)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidGeometryException(description + " is outside the finite double range.");
        }
        return value;
    }

    internal static void ValidateCoordinateValue(Coordinate coordinate, string description)
    {
        if (coordinate == null)
        {
            throw new InvalidGeometryException(description + " is null.");
        }
        ValidateFiniteValue(coordinate.X, description + " X ordinate");
        ValidateFiniteValue(coordinate.Y, description + " Y ordinate");
        if (!double.IsNaN(coordinate.Z)) ValidateFiniteValue(coordinate.Z, description + " Z ordinate");
        if (!double.IsNaN(coordinate.M)) ValidateFiniteValue(coordinate.M, description + " M ordinate");
    }

    internal static void ValidateFiniteValue(double value, string description)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidGeometryException(description + " must be finite.");
        }
    }

    internal static void ValidateQuadrant(int quadrant)
    {
        if (quadrant < 0 || quadrant > 3)
        {
            throw new InvalidGeometryException("A quadrant must be between zero and three.");
        }
    }
}
