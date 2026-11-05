using System;

namespace geo;

internal class CircularArcDefinition
{
    internal Coordinate Start = new Coordinate();
    internal Coordinate Middle = new Coordinate();
    internal Coordinate End = new Coordinate();
    internal bool IsLinear;
    internal double CenterX;
    internal double CenterY;
    internal double Radius;
    internal double StartAngle;
    internal double FirstSweep;
    internal double SecondSweep;
    internal double Sweep;
}

internal static class CircularStringAlgorithms
{
    internal static void ValidateValue(CircularString circularString)
    {
        if (circularString == null || circularString.Coordinates == null)
        {
            throw new InvalidGeometryException(
                "CircularString payload or coordinates are null.");
        }
        Coordinate[] coordinates = circularString.Coordinates;
        if (coordinates.Length != 0 &&
            (coordinates.Length < 3 || coordinates.Length % 2 == 0))
        {
            throw new InvalidGeometryException(
                "A CircularString must be empty or contain an odd number of at least three coordinates.");
        }
        int index = 0;
        while (index < coordinates.Length)
        {
            VectorMath.ValidateCoordinateValue(
                coordinates[index], "CircularString coordinate");
            index++;
        }
    }

    internal static CircularArcDefinition CreateArc(
        Coordinate start, Coordinate middle, Coordinate end)
    {
        VectorMath.ValidateCoordinateValue(start, "Circular arc start");
        VectorMath.ValidateCoordinateValue(middle, "Circular arc control point");
        VectorMath.ValidateCoordinateValue(end, "Circular arc end");
        CircularArcDefinition arc = new CircularArcDefinition();
        arc.Start = start;
        arc.Middle = middle;
        arc.End = end;
        if (RobustOrientation.GetOrientationIndex(start, middle, end) == 0)
        {
            arc.IsLinear = true;
            return arc;
        }

        double firstX = middle.X - start.X;
        double firstY = middle.Y - start.Y;
        double secondX = end.X - start.X;
        double secondY = end.Y - start.Y;
        double scale = MaximumAbsolute(
            firstX, firstY, secondX, secondY);
        if (!double.IsFinite(scale))
        {
            return CreateScaledArc(start, middle, end, arc);
        }
        if (scale == 0.0)
        {
            arc.IsLinear = true;
            return arc;
        }
        firstX /= scale;
        firstY /= scale;
        secondX /= scale;
        secondY /= scale;
        return CompleteArc(
            start, middle, end, arc,
            firstX, firstY, secondX, secondY, scale);
    }

    internal static CircularArcDefinition CreateScaledArc(
        Coordinate start, Coordinate middle, Coordinate end,
        CircularArcDefinition arc)
    {
        double scale = MaximumAbsolute(
            start.X, start.Y, middle.X, middle.Y);
        double candidate = Math.Abs(end.X);
        if (candidate > scale) scale = candidate;
        candidate = Math.Abs(end.Y);
        if (candidate > scale) scale = candidate;
        if (scale == 0.0 || !double.IsFinite(scale))
        {
            throw new InvalidGeometryException(
                "Circular arc center cannot be represented at this coordinate scale.");
        }
        double startX = start.X / scale;
        double startY = start.Y / scale;
        return CompleteArc(
            start, middle, end, arc,
            middle.X / scale - startX,
            middle.Y / scale - startY,
            end.X / scale - startX,
            end.Y / scale - startY, scale);
    }

    internal static CircularArcDefinition CompleteArc(
        Coordinate start, Coordinate middle, Coordinate end,
        CircularArcDefinition arc, double firstX, double firstY,
        double secondX, double secondY, double scale)
    {
        double denominator =
            2.0 * (firstX * secondY - firstY * secondX);
        if (denominator == 0.0 || !double.IsFinite(denominator))
        {
            throw new InvalidGeometryException(
                "Circular arc center cannot be represented at this coordinate scale.");
        }
        double firstLength = firstX * firstX + firstY * firstY;
        double secondLength = secondX * secondX + secondY * secondY;
        double offsetX =
            (firstLength * secondY - secondLength * firstY) /
            denominator;
        double offsetY =
            (firstX * secondLength - secondX * firstLength) /
            denominator;
        double radiusScale = Math.Sqrt(
            offsetX * offsetX + offsetY * offsetY);
        arc.CenterX = start.X + offsetX * scale;
        arc.CenterY = start.Y + offsetY * scale;
        arc.Radius = radiusScale * scale;
        if (!double.IsFinite(arc.CenterX) ||
            !double.IsFinite(arc.CenterY) ||
            !double.IsFinite(arc.Radius) || arc.Radius == 0.0)
        {
            throw new InvalidGeometryException(
                "Circular arc has an invalid center or radius.");
        }

        arc.StartAngle = Math.Atan2(-offsetY, -offsetX);
        double middleAngle = Math.Atan2(
            firstY - offsetY, firstX - offsetX);
        double endAngle = Math.Atan2(
            secondY - offsetY, secondX - offsetX);
        double counterClockwiseEnd = NormalizePositiveAngle(
            endAngle - arc.StartAngle);
        double counterClockwiseMiddle = NormalizePositiveAngle(
            middleAngle - arc.StartAngle);
        if (counterClockwiseMiddle <= counterClockwiseEnd)
        {
            arc.FirstSweep = counterClockwiseMiddle;
            arc.Sweep = counterClockwiseEnd;
        }
        else
        {
            arc.FirstSweep = -NormalizePositiveAngle(
                arc.StartAngle - middleAngle);
            arc.Sweep = counterClockwiseEnd - Math.PI * 2.0;
        }
        arc.SecondSweep = arc.Sweep - arc.FirstSweep;
        return arc;
    }

    internal static double GetLength(CircularString circularString)
    {
        ValidateValue(circularString);
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        Coordinate[] coordinates = circularString.Coordinates;
        int index = 0;
        while (index + 2 < coordinates.Length)
        {
            CircularArcDefinition arc = CreateArc(
                coordinates[index], coordinates[index + 1],
                coordinates[index + 2]);
            if (arc.IsLinear)
            {
                sum.AddValue(SegmentDistanceAlgorithms.GetPlanarDistance(
                    arc.Start, arc.Middle));
                sum.AddValue(SegmentDistanceAlgorithms.GetPlanarDistance(
                    arc.Middle, arc.End));
            }
            else
            {
                sum.AddValue(arc.Radius * Math.Abs(arc.Sweep));
            }
            index += 2;
        }
        return sum.GetValue();
    }

    internal static double GetSignedAreaContribution(
        CircularString circularString, Coordinate origin)
    {
        ValidateValue(circularString);
        VectorMath.ValidateCoordinateValue(
            origin, "CircularString area origin");
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        Coordinate[] coordinates = circularString.Coordinates;
        int index = 0;
        while (index + 2 < coordinates.Length)
        {
            CircularArcDefinition arc = CreateArc(
                coordinates[index], coordinates[index + 1],
                coordinates[index + 2]);
            if (arc.IsLinear)
            {
                sum.AddValue(GetChordAreaRelativeToOrigin(
                    arc.Start, arc.Middle, origin));
                sum.AddValue(GetChordAreaRelativeToOrigin(
                    arc.Middle, arc.End, origin));
            }
            else
            {
                double chord = GetChordAreaRelativeToOrigin(
                    arc.Start, arc.End, origin);
                double segment = arc.Radius * arc.Radius *
                    (arc.Sweep - Math.Sin(arc.Sweep)) * 0.5;
                sum.AddValue(chord);
                sum.AddValue(segment);
            }
            index += 2;
        }
        return sum.GetValue();
    }

    internal static Envelope ExpandEnvelope(
        Envelope envelope, CircularString circularString)
    {
        ValidateValue(circularString);
        Coordinate[] coordinates = circularString.Coordinates;
        int index = 0;
        while (index + 2 < coordinates.Length)
        {
            CircularArcDefinition arc = CreateArc(
                coordinates[index], coordinates[index + 1],
                coordinates[index + 2]);
            if (!arc.IsLinear)
            {
                envelope = ExpandAtAngle(envelope, arc, 0.0);
                envelope = ExpandAtAngle(envelope, arc, Math.PI * 0.5);
                envelope = ExpandAtAngle(envelope, arc, Math.PI);
                envelope = ExpandAtAngle(envelope, arc, Math.PI * 1.5);
            }
            index += 2;
        }
        return envelope;
    }

    internal static Envelope ExpandAtAngle(
        Envelope envelope, CircularArcDefinition arc, double angle)
    {
        if (!ContainsAngle(arc, angle)) return envelope;
        Coordinate coordinate = new Coordinate();
        coordinate.X = VectorMath.RequireFiniteResult(
            arc.CenterX + arc.Radius * Math.Cos(angle),
            "Circular arc envelope X ordinate");
        coordinate.Y = VectorMath.RequireFiniteResult(
            arc.CenterY + arc.Radius * Math.Sin(angle),
            "Circular arc envelope Y ordinate");
        return EnvelopeAlgorithms.ExpandEnvelopeByCoordinate(
            envelope, coordinate);
    }

    internal static bool ContainsAngle(
        CircularArcDefinition arc, double angle)
    {
        double offset = arc.Sweep > 0.0 ?
            NormalizePositiveAngle(angle - arc.StartAngle) :
            NormalizePositiveAngle(arc.StartAngle - angle);
        return offset <= Math.Abs(arc.Sweep) + 1.0e-14;
    }

    internal static double GetChordAreaRelativeToOrigin(
        Coordinate start, Coordinate end, Coordinate origin)
    {
        double startX = start.X - origin.X;
        double startY = start.Y - origin.Y;
        double endX = end.X - origin.X;
        double endY = end.Y - origin.Y;
        return (startX * endY - endX * startY) * 0.5;
    }

    internal static double NormalizePositiveAngle(double angle)
    {
        double full = Math.PI * 2.0;
        while (angle < 0.0) angle += full;
        while (angle >= full) angle -= full;
        return angle;
    }

    internal static double MaximumAbsolute(
        double first, double second, double third, double fourth)
    {
        double maximum = Math.Abs(first);
        double candidate = Math.Abs(second);
        if (candidate > maximum) maximum = candidate;
        candidate = Math.Abs(third);
        if (candidate > maximum) maximum = candidate;
        candidate = Math.Abs(fourth);
        if (candidate > maximum) maximum = candidate;
        return maximum;
    }
}
