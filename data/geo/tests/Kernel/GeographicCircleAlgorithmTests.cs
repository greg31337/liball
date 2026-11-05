using geo;

namespace geo.Tests;

public class GeographicCircleAlgorithmTests
{
    [Test]
    public void EquatorialCircleUsesEqualCounterClockwiseBearings()
    {
        Coordinate center = CoordinateValue(0.0, 0.0);

        Geometry circle =
            GeographicCircleAlgorithm.CreateGeographicCircle(
                center, 10.0, SpatialUnit.Degrees, 4);

        Coordinate[] coordinates = circle.Polygon!.Shell.Coordinates;
        Assert.Multiple(() =>
        {
            Assert.That(circle.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(circle.SpatialReferenceId,
                Is.EqualTo(GeometryConstants.Crs84SpatialReferenceId));
            Assert.That(circle.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XY));
            Assert.That(coordinates, Has.Length.EqualTo(5));
            AssertCoordinate(coordinates[0], 0.0, 10.0, 1.0e-12);
            AssertCoordinate(coordinates[1], -10.0, 0.0, 1.0e-12);
            AssertCoordinate(coordinates[2], 0.0, -10.0, 1.0e-12);
            AssertCoordinate(coordinates[3], 10.0, 0.0, 1.0e-12);
            AssertCoordinate(coordinates[4], 0.0, 10.0, 1.0e-12);
            Assert.That(RingMath.GetSignedArea(coordinates),
                Is.GreaterThan(0.0));
            AssertCoordinate(center, 0.0, 0.0, 0.0);
        });
    }

    [Test]
    public void EveryBoundaryVertexIsAtTheRequestedRadius()
    {
        Coordinate center = CoordinateValue(179.0, 82.0);
        Geometry circle =
            GeographicCircleAlgorithm.CreateGeographicCircle(
                center, 750.0, SpatialUnit.Kilometers, 32);
        Coordinate[] coordinates = circle.Polygon!.Shell.Coordinates;
        bool hasWesternLongitude = false;
        bool hasEasternLongitude = false;

        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            double distance = GeographicMath.GetHaversineDistance(
                center, coordinates[index], SpatialUnit.Kilometers);
            Assert.That(distance,
                Is.EqualTo(750.0).Within(1.0e-9));
            Assert.That(coordinates[index].X,
                Is.GreaterThanOrEqualTo(-180.0).And.LessThan(180.0));
            Assert.That(coordinates[index].Y,
                Is.GreaterThanOrEqualTo(-90.0).And.LessThanOrEqualTo(90.0));
            if (coordinates[index].X < 0.0) hasWesternLongitude = true;
            if (coordinates[index].X > 0.0) hasEasternLongitude = true;
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(hasWesternLongitude, Is.True);
            Assert.That(hasEasternLongitude, Is.True);
            AssertCoordinate(coordinates[coordinates.Length - 1],
                coordinates[0].X, coordinates[0].Y, 0.0);
            Assert.That(coordinates[coordinates.Length - 1],
                Is.Not.SameAs(coordinates[0]));
        });
    }

    [Test]
    public void EquivalentRadiusUnitsProduceEquivalentCircles()
    {
        Coordinate center = CoordinateValue(-73.0, 40.0);
        Geometry kilometers =
            GeographicCircleAlgorithm.CreateGeographicCircle(
                center, 1.0, SpatialUnit.Kilometers, 12);
        Geometry meters =
            GeographicCircleAlgorithm.CreateGeographicCircle(
                center, 1000.0, SpatialUnit.Meters, 12);
        Coordinate[] first = kilometers.Polygon!.Shell.Coordinates;
        Coordinate[] second = meters.Polygon!.Shell.Coordinates;

        int index = 0;
        while (index < first.Length)
        {
            AssertCoordinate(first[index],
                second[index].X, second[index].Y, 1.0e-13);
            index++;
        }
    }

    [Test]
    public void CircleAreaConvergesTowardTheSphericalCapArea()
    {
        const double angularRadius = 20.0;
        Geometry circle =
            GeographicCircleAlgorithm.CreateGeographicCircle(
                CoordinateValue(20.0, 30.0), angularRadius,
                SpatialUnit.Degrees, 720);
        double actual = SphericalAreaAlgorithm.GetSphericalArea(
            circle, SpatialUnit.Radians);
        double radiusRadians = angularRadius * Math.PI / 180.0;
        double expected = 2.0 * Math.PI *
            (1.0 - Math.Cos(radiusRadians));

        Assert.That(actual,
            Is.EqualTo(expected).Within(expected * 2.0e-5));
    }

    [Test]
    public void InvalidCenterRadiusUnitAndSegmentCountAreRejected()
    {
        Coordinate center = CoordinateValue(0.0, 0.0);
        double halfCircumference = Math.PI *
            GeographicMath.EarthRadiusMeters;

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicCircleAlgorithm.CreateGeographicCircle(
                    CoordinateValue(0.0, 91.0), 1.0,
                    SpatialUnit.Kilometers, 8));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicCircleAlgorithm.CreateGeographicCircle(
                    center, 0.0, SpatialUnit.Kilometers, 8));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicCircleAlgorithm.CreateGeographicCircle(
                    center, double.PositiveInfinity,
                    SpatialUnit.Kilometers, 8));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicCircleAlgorithm.CreateGeographicCircle(
                    center, 1.0, (SpatialUnit)99, 8));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicCircleAlgorithm.CreateGeographicCircle(
                    center, 1.0, SpatialUnit.Kilometers, 2));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicCircleAlgorithm.CreateGeographicCircle(
                    center, halfCircumference,
                    SpatialUnit.Meters, 8));
        });
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y, double tolerance)
    {
        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(x).Within(tolerance));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(tolerance));
        });
    }
}
