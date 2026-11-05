using System;

namespace geo;

internal static class GeographicCircleAlgorithm
{
    internal static Geometry CreateGeographicCircle(
        Coordinate center, double radius,
        SpatialUnit radiusUnit, int segmentCount)
    {
        GeographicMath.ValidateGeographicCoordinate(center);
        if (!double.IsFinite(radius) || radius <= 0.0)
        {
            throw new InvalidGeometryException(
                "Geographic circle radius must be finite and positive.");
        }
        if (segmentCount < 3 ||
            segmentCount >= GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Geographic circle segment count must be at least three and within the coordinate limit.");
        }

        double angularRadius = GeographicMath.ConvertUnits(
            radius, radiusUnit, SpatialUnit.Radians);
        if (angularRadius <= 0.0 || angularRadius >= Math.PI)
        {
            throw new InvalidGeometryException(
                "Geographic circle radius must be less than half the earth circumference.");
        }

        Coordinate[] coordinates = new Coordinate[segmentCount + 1];
        int index = 0;
        while (index < segmentCount)
        {
            double bearing = -2.0 * Math.PI * index / segmentCount;
            coordinates[index] = GeographicMath.GetDestination(
                center, angularRadius, SpatialUnit.Radians,
                bearing, SpatialUnit.Radians);
            index++;
        }
        coordinates[segmentCount] =
            CoordinateSequences.CopyCoordinateValue(coordinates[0]);

        LinearRing shell = new LinearRing();
        shell.Coordinates = coordinates;
        GeometryFactoryOptions options = new GeometryFactoryOptions();
        options.SpatialReferenceId =
            GeometryConstants.Crs84SpatialReferenceId;
        return GeometryBuilder.CreatePolygonValue(shell, [], options);
    }
}
