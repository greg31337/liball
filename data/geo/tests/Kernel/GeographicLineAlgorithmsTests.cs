using geo;

namespace geo.Tests;

public class GeographicLineAlgorithmsTests
{
    [Test]
    public void GreatCircleCreatesEvenAngularSegmentsAcrossDateline()
    {
        Geometry equator = GeographicLineAlgorithms.CreateGreatCircle(
            CoordinateValue(0.0, 0.0),
            CoordinateValue(90.0, 0.0), 4);
        Geometry dateline = GeographicLineAlgorithms.CreateGreatCircle(
            CoordinateValue(170.0, 0.0),
            CoordinateValue(-170.0, 0.0), 4);

        Coordinate[] equatorCoordinates =
            equator.LineString!.Coordinates;
        Coordinate[] datelineCoordinates =
            dateline.LineString!.Coordinates;
        Assert.Multiple(() =>
        {
            Assert.That(equatorCoordinates, Has.Length.EqualTo(5));
            AssertCoordinate(equatorCoordinates[0], 0.0, 0.0, 1.0e-12);
            AssertCoordinate(equatorCoordinates[1], 22.5, 0.0, 1.0e-12);
            AssertCoordinate(equatorCoordinates[2], 45.0, 0.0, 1.0e-12);
            AssertCoordinate(equatorCoordinates[4], 90.0, 0.0, 1.0e-12);
            Assert.That(equator.SpatialReferenceId,
                Is.EqualTo(GeometryConstants.Crs84SpatialReferenceId));
            Assert.That(datelineCoordinates, Has.Length.EqualTo(5));
            AssertCoordinate(datelineCoordinates[0], 170.0, 0.0, 1.0e-12);
            AssertCoordinate(datelineCoordinates[1], 175.0, 0.0, 1.0e-12);
            AssertCoordinate(datelineCoordinates[2], -180.0, 0.0, 1.0e-12);
            AssertCoordinate(datelineCoordinates[3], -175.0, 0.0, 1.0e-12);
            AssertCoordinate(datelineCoordinates[4], -170.0, 0.0, 1.0e-12);
        });
    }

    [Test]
    public void GreatCircleInterpolationBowsTowardPoleAndInterpolatesData()
    {
        Coordinate start = new Coordinate
        {
            X = 0.0,
            Y = 60.0,
            Z = 10.0,
            M = 20.0
        };
        Coordinate end = new Coordinate
        {
            X = 90.0,
            Y = 60.0,
            Z = 30.0,
            M = 60.0
        };

        Coordinate midpoint =
            GeographicLineAlgorithms.InterpolateGreatCircle(
                start, end, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(midpoint.X, Is.EqualTo(45.0)
                .Within(1.0e-12));
            Assert.That(midpoint.Y, Is.EqualTo(67.7923457014035)
                .Within(1.0e-12));
            Assert.That(midpoint.Z, Is.EqualTo(20.0));
            Assert.That(midpoint.M, Is.EqualTo(40.0));
            AssertCoordinate(start, 0.0, 60.0, 0.0);
        });
    }

    [Test]
    public void RhumbDistanceAndBearingUseShortestLongitudeDelta()
    {
        Coordinate origin = CoordinateValue(0.0, 0.0);
        Coordinate east = CoordinateValue(90.0, 0.0);
        Coordinate north = CoordinateValue(0.0, 45.0);
        Coordinate westDateline = CoordinateValue(179.0, 0.0);
        Coordinate eastDateline = CoordinateValue(-179.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(GeographicLineAlgorithms.GetRhumbDistance(
                origin, east, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            Assert.That(GeographicLineAlgorithms.GetRhumbBearing(
                origin, east, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            Assert.That(GeographicLineAlgorithms.GetRhumbDistance(
                origin, north, SpatialUnit.Degrees),
                Is.EqualTo(45.0).Within(1.0e-12));
            Assert.That(GeographicLineAlgorithms.GetRhumbBearing(
                origin, north, SpatialUnit.Degrees),
                Is.EqualTo(0.0).Within(1.0e-12));
            Assert.That(GeographicLineAlgorithms.GetRhumbDistance(
                westDateline, eastDateline, SpatialUnit.Degrees),
                Is.EqualTo(2.0).Within(1.0e-12));
            Assert.That(GeographicLineAlgorithms.GetRhumbBearing(
                westDateline, eastDateline, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
        });
    }

    [Test]
    public void RhumbDestinationRoundTripsDistanceAndBearing()
    {
        Coordinate origin = CoordinateValue(179.0, 0.0);
        Coordinate destination =
            GeographicLineAlgorithms.GetRhumbDestination(
                origin, 2.0, SpatialUnit.Degrees,
                Math.PI / 2.0, SpatialUnit.Radians);
        Coordinate north = GeographicLineAlgorithms.GetRhumbDestination(
            CoordinateValue(0.0, 0.0), 45.0, SpatialUnit.Degrees,
            0.0, SpatialUnit.Degrees);
        Coordinate poleCrossing =
            GeographicLineAlgorithms.GetRhumbDestination(
                CoordinateValue(0.0, 80.0),
                20.0, SpatialUnit.Degrees,
                0.0, SpatialUnit.Degrees);

        Assert.Multiple(() =>
        {
            AssertCoordinate(destination, -179.0, 0.0, 1.0e-12);
            Assert.That(GeographicLineAlgorithms.GetRhumbDistance(
                origin, destination, SpatialUnit.Degrees),
                Is.EqualTo(2.0).Within(1.0e-12));
            Assert.That(GeographicLineAlgorithms.GetRhumbBearing(
                origin, destination, SpatialUnit.Degrees),
                Is.EqualTo(90.0).Within(1.0e-12));
            AssertCoordinate(north, 0.0, 45.0, 1.0e-12);
            Assert.That(double.IsFinite(poleCrossing.X), Is.True);
            Assert.That(double.IsFinite(poleCrossing.Y), Is.True);
            Assert.That(poleCrossing.Y,
                Is.GreaterThanOrEqualTo(-90.0).And.LessThanOrEqualTo(90.0));
        });
    }

    [Test]
    public void GeographicLengthSumsSegmentsWithoutComponentGaps()
    {
        Geometry line = Read(
            "LINESTRING (170 0, -170 0, -170 10)");
        Geometry multiLine = Read(
            "MULTILINESTRING ((0 0, 10 0), (30 0, 40 0))");
        Geometry empty = Read("LINESTRING EMPTY");

        Assert.Multiple(() =>
        {
            Assert.That(GeographicLineAlgorithms.GetGeographicLength(
                line, SpatialUnit.Degrees),
                Is.EqualTo(30.0).Within(1.0e-12));
            Assert.That(GeographicLineAlgorithms.GetGeographicLength(
                multiLine, SpatialUnit.Degrees),
                Is.EqualTo(20.0).Within(1.0e-12));
            Assert.That(GeographicLineAlgorithms.GetGeographicLength(
                empty, SpatialUnit.Kilometers), Is.EqualTo(0.0));
        });
    }

    [Test]
    public void AlongClampsAndInterpolatesAcrossDatelineWithData()
    {
        Geometry line = Read(
            "LINESTRING ZM (170 0 0 0, -170 0 20 40, " +
            "-170 10 30 60)");

        Coordinate? before =
            GeographicLineAlgorithms.GetCoordinateAlongLine(
                line, -5.0, SpatialUnit.Degrees);
        Coordinate? dateline =
            GeographicLineAlgorithms.GetCoordinateAlongLine(
                line, 10.0, SpatialUnit.Degrees);
        Coordinate? vertical =
            GeographicLineAlgorithms.GetCoordinateAlongLine(
                line, 25.0, SpatialUnit.Degrees);
        Coordinate? after =
            GeographicLineAlgorithms.GetCoordinateAlongLine(
                line, 100.0, SpatialUnit.Degrees);

        Assert.Multiple(() =>
        {
            AssertCoordinate(before!, 170.0, 0.0, 1.0e-12);
            AssertCoordinate(dateline!, -180.0, 0.0, 1.0e-12);
            Assert.That(dateline!.Z, Is.EqualTo(10.0).Within(1.0e-12));
            Assert.That(dateline.M, Is.EqualTo(20.0).Within(1.0e-12));
            AssertCoordinate(vertical!, -170.0, 5.0, 1.0e-12);
            Assert.That(vertical!.Z, Is.EqualTo(25.0).Within(1.0e-12));
            Assert.That(vertical.M, Is.EqualTo(50.0).Within(1.0e-12));
            AssertCoordinate(after!, -170.0, 10.0, 1.0e-12);
        });
    }

    [Test]
    public void SliceInterpolatesEndpointsAndPreservesDirectionMetadata()
    {
        Geometry source = ReadWithSrid(
            "SRID=4326;LINESTRING (170 0, -170 0, -170 10)");

        Geometry forward = GeographicLineAlgorithms.SliceGeographicLine(
            source, 5.0, 25.0, SpatialUnit.Degrees);
        Geometry reverse = GeographicLineAlgorithms.SliceGeographicLine(
            source, 25.0, 5.0, SpatialUnit.Degrees);

        Coordinate[] forwardCoordinates =
            forward.LineString!.Coordinates;
        Coordinate[] reverseCoordinates =
            reverse.LineString!.Coordinates;
        Assert.Multiple(() =>
        {
            Assert.That(forward.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(forwardCoordinates, Has.Length.EqualTo(3));
            AssertCoordinate(forwardCoordinates[0], 175.0, 0.0, 1.0e-12);
            AssertCoordinate(forwardCoordinates[1], -170.0, 0.0, 1.0e-12);
            AssertCoordinate(forwardCoordinates[2], -170.0, 5.0, 1.0e-12);
            Assert.That(reverseCoordinates, Has.Length.EqualTo(3));
            AssertCoordinate(reverseCoordinates[0], -170.0, 5.0, 1.0e-12);
            AssertCoordinate(reverseCoordinates[2], 175.0, 0.0, 1.0e-12);
            Assert.That(GeographicLineAlgorithms.GetGeographicLength(
                forward, SpatialUnit.Degrees),
                Is.EqualTo(20.0).Within(1.0e-12));
        });
    }

    [Test]
    public void SliceRetainsSeparateMultiLineComponents()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 10 0), (30 0, 40 0))");

        Geometry result = GeographicLineAlgorithms.SliceGeographicLine(
            source, 5.0, 15.0, SpatialUnit.Degrees);

        LineString[] lines = result.MultiLineString!.LineStrings;
        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(lines, Has.Length.EqualTo(2));
            AssertCoordinate(lines[0].Coordinates[0], 5.0, 0.0, 1.0e-12);
            AssertCoordinate(lines[0].Coordinates[1], 10.0, 0.0, 1.0e-12);
            AssertCoordinate(lines[1].Coordinates[0], 30.0, 0.0, 1.0e-12);
            AssertCoordinate(lines[1].Coordinates[1], 35.0, 0.0, 1.0e-12);
            Assert.That(GeographicLineAlgorithms.GetGeographicLength(
                result, SpatialUnit.Degrees),
                Is.EqualTo(10.0).Within(1.0e-12));
        });
    }

    [Test]
    public void NearestPointUsesSphericalSegmentProjection()
    {
        Geometry equator = Read("LINESTRING (-10 0, 10 0)");
        Geometry dateline = Read("LINESTRING (170 0, -170 0)");

        DistanceResult first =
            GeographicLineAlgorithms.GetNearestPointOnLine(
                equator, CoordinateValue(0.0, 5.0),
                SpatialUnit.Degrees);
        DistanceResult second =
            GeographicLineAlgorithms.GetNearestPointOnLine(
                dateline, CoordinateValue(180.0, 5.0),
                SpatialUnit.Degrees);

        Assert.Multiple(() =>
        {
            Assert.That(first.HasResult, Is.True);
            Assert.That(first.Distance,
                Is.EqualTo(5.0).Within(1.0e-12));
            AssertCoordinate(first.FirstCoordinate!, 0.0, 0.0, 1.0e-12);
            Assert.That(first.FirstLocation!.ComponentIndex, Is.Zero);
            Assert.That(first.FirstLocation.SegmentIndex, Is.Zero);
            Assert.That(first.FirstLocation.SegmentFraction,
                Is.EqualTo(0.5).Within(1.0e-12));
            Assert.That(second.Distance,
                Is.EqualTo(5.0).Within(1.0e-12));
            AssertCoordinate(second.FirstCoordinate!, -180.0, 0.0, 1.0e-12);
        });
    }

    [Test]
    public void NearestPointClampsToEndpointAndUsesFirstTie()
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 10 0), (0 0, -10 0))");

        DistanceResult endpoint =
            GeographicLineAlgorithms.GetNearestPointOnLine(
                source, CoordinateValue(20.0, 0.0),
                SpatialUnit.Degrees);
        DistanceResult tie =
            GeographicLineAlgorithms.GetNearestPointOnLine(
                source, CoordinateValue(0.0, 5.0),
                SpatialUnit.Degrees);

        Assert.Multiple(() =>
        {
            AssertCoordinate(endpoint.FirstCoordinate!,
                10.0, 0.0, 1.0e-12);
            Assert.That(endpoint.FirstLocation!.ComponentIndex, Is.Zero);
            Assert.That(endpoint.FirstLocation.SegmentIndex,
                Is.EqualTo(1));
            Assert.That(tie.FirstLocation!.ComponentIndex, Is.Zero);
            AssertCoordinate(tie.FirstCoordinate!, 0.0, 0.0, 1.0e-12);
        });
    }

    [Test]
    public void InvalidRoutesParametersAndLineworkAreRejected()
    {
        Coordinate origin = CoordinateValue(0.0, 0.0);
        Geometry line = Read("LINESTRING (0 0, 10 0)");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicLineAlgorithms.CreateGreatCircle(
                    origin, CoordinateValue(10.0, 0.0), 0));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicLineAlgorithms.InterpolateGreatCircle(
                    origin, CoordinateValue(10.0, 0.0), 1.1));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicLineAlgorithms.GetRhumbDestination(
                    CoordinateValue(0.0, 90.0),
                    1.0, SpatialUnit.Degrees,
                    90.0, SpatialUnit.Degrees));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicLineAlgorithms.GetGeographicLength(
                    Read("POINT (0 0)"), SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicLineAlgorithms.GetGeographicLength(
                    Read("LINESTRING (0 91, 1 1)"),
                    SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicLineAlgorithms.GetCoordinateAlongLine(
                    line, double.NaN, SpatialUnit.Meters));
            Assert.Throws<InvalidGeometryException>(() =>
                GeographicLineAlgorithms.SliceGeographicLine(
                    line, 0.0, 1.0, (SpatialUnit)99));
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

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Geometry ReadWithSrid(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
