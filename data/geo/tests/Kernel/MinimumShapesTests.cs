using geo;

namespace geo.Tests;

public class MinimumShapesTests
{
    [Test]
    public void MinimumRotatedRectangleFindsSupportingEdgeBox()
    {
        Geometry source = Read(
            "MULTIPOINT ((0 0), (4 2), (3 4), (-1 2), " +
            "(1 2), (2 2))");

        Geometry rectangle = MinimumShapes.GetMinimumRotatedRectangle(source);
        Coordinate[] shell = rectangle.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(rectangle.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(shell, Has.Length.EqualTo(5));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(rectangle),
                Is.EqualTo(10.0).Within(1e-12));
            Assert.That(AllCoordinatesCovered(source, rectangle), Is.True);
            Assert.That(RingMath.GetExactSignedAreaSign(shell), Is.EqualTo(1));
            Assert.That(IsRectangle(shell), Is.True);
        });
    }

    [Test]
    public void MinimumRectangleUsesNarrowestDegenerateResultsAndOwnsOrdinates()
    {
        Geometry emptySource = ReadWithSrid(
            "SRID=4326;GEOMETRYCOLLECTION EMPTY");
        Geometry pointSource = Read("POINT ZM (2 3 4 5)");
        Geometry lineSource = Read(
            "MULTIPOINT Z ((5 5 9), (1 1 3), (3 3 6))");

        Geometry empty =
            MinimumShapes.GetMinimumRotatedRectangle(emptySource);
        Geometry point =
            MinimumShapes.GetMinimumRotatedRectangle(pointSource);
        Geometry line =
            MinimumShapes.GetMinimumRotatedRectangle(lineSource);
        pointSource.Point!.Coordinate!.X = 20.0;

        Assert.Multiple(() =>
        {
            Assert.That(empty.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(empty.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(GeometryInspection.IsEmptyValue(empty), Is.True);
            Assert.That(point.GeometryType, Is.EqualTo(GeometryType.Point));
            AssertCoordinate(point.Point!.Coordinate!, 2.0, 3.0);
            Assert.That(point.Point.Coordinate!.Z, Is.EqualTo(4.0));
            Assert.That(point.Point.Coordinate.M, Is.EqualTo(5.0));
            Assert.That(line.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            AssertCoordinate(line.LineString!.Coordinates[0], 1.0, 1.0);
            AssertCoordinate(line.LineString.Coordinates[1], 5.0, 5.0);
        });
    }

    [Test]
    public void MinimumWidthAndDiameterReturnSupportingWitnesses()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 4 0, 4 2, 0 2, 0 0))");

        DistanceResult width = MinimumShapes.GetMinimumWidth(source);
        DistanceResult diameter = MinimumShapes.GetMinimumDiameter(source);

        Assert.Multiple(() =>
        {
            Assert.That(width.HasResult, Is.True);
            Assert.That(width.Distance, Is.EqualTo(2.0));
            Assert.That(diameter.HasResult, Is.True);
            Assert.That(diameter.Distance, Is.EqualTo(width.Distance));
            Assert.That(SegmentDistanceAlgorithms.GetPlanarDistance(
                width.FirstCoordinate!, width.SecondCoordinate!),
                Is.EqualTo(width.Distance));
            Assert.That(SegmentDistanceAlgorithms.IsPointOnLine(
                width.FirstCoordinate!, Point(0.0, 0.0), Point(4.0, 0.0)) ||
                SegmentDistanceAlgorithms.IsPointOnLine(
                    width.FirstCoordinate!, Point(4.0, 2.0), Point(0.0, 2.0)),
                Is.True);
        });
    }

    [Test]
    public void MaximumDiameterUsesExactFarthestPairAndCarriesWitnessOrdinates()
    {
        Geometry source = Read(
            "MULTIPOINT ZM ((0 0 1 11), (4 0 2 12), " +
            "(4 2 3 13), (0 2 4 14), (2 1 9 19))");

        DistanceResult result = MinimumShapes.GetMaximumDiameter(source);
        source.MultiPoint!.Points[0].Coordinate!.Z = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.True);
            Assert.That(result.Distance, Is.EqualTo(Math.Sqrt(20.0)));
            AssertCoordinate(result.FirstCoordinate!, 0.0, 0.0);
            AssertCoordinate(result.SecondCoordinate!, 4.0, 2.0);
            Assert.That(result.FirstCoordinate!.Z, Is.EqualTo(1.0));
            Assert.That(result.FirstCoordinate.M, Is.EqualTo(11.0));
            Assert.That(result.SecondCoordinate!.Z, Is.EqualTo(3.0));
            Assert.That(result.SecondCoordinate.M, Is.EqualTo(13.0));
        });
    }

    [Test]
    public void MinimumBoundingCircleHandlesDiameterAndCircumcircleSupports()
    {
        Geometry rightTriangle = Read(
            "MULTIPOINT ((0 0), (4 0), (0 3), (1 1))");
        Geometry acuteTriangle = Read(
            "MULTIPOINT ((-2 0), (2 0), (0 3))");

        CircleResult diameterCircle =
            MinimumShapes.GetMinimumBoundingCircle(rightTriangle);
        CircleResult circumcircle =
            MinimumShapes.GetMinimumBoundingCircle(acuteTriangle);

        Assert.Multiple(() =>
        {
            AssertCoordinate(diameterCircle.Center!, 2.0, 1.5);
            Assert.That(diameterCircle.Radius, Is.EqualTo(2.5));
            Assert.That(diameterCircle.Witnesses, Has.Length.EqualTo(2));
            Assert.That(AllCoordinatesInCircle(
                rightTriangle, diameterCircle), Is.True);
            AssertCoordinate(circumcircle.Center!, 0.0, 5.0 / 6.0);
            Assert.That(circumcircle.Radius,
                Is.EqualTo(13.0 / 6.0).Within(1e-12));
            Assert.That(circumcircle.Witnesses, Has.Length.EqualTo(3));
            Assert.That(AllCoordinatesInCircle(
                acuteTriangle, circumcircle), Is.True);
            Assert.That(circumcircle.Geometry!.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(circumcircle.Unit, Is.EqualTo(SpatialUnit.Meters));
        });
    }

    [Test]
    public void BoundingCircleDegeneraciesAndEmptyMetricWitnessesAreDefined()
    {
        Geometry emptySource = Read("GEOMETRYCOLLECTION EMPTY");
        Geometry pointSource = ReadWithSrid("SRID=3857;POINT Z (3 4 8)");
        Geometry collinear = Read(
            "MULTIPOINT ((5 0), (0 0), (2 0), (3 0))");

        CircleResult empty =
            MinimumShapes.GetMinimumBoundingCircle(emptySource);
        CircleResult point =
            MinimumShapes.GetMinimumBoundingCircle(pointSource);
        CircleResult line =
            MinimumShapes.GetMinimumBoundingCircle(collinear);
        DistanceResult emptyWidth = MinimumShapes.GetMinimumWidth(emptySource);
        DistanceResult emptyDiameter =
            MinimumShapes.GetMaximumDiameter(emptySource);

        pointSource.Point!.Coordinate!.X = 30.0;

        Assert.Multiple(() =>
        {
            Assert.That(empty.Geometry, Is.Null);
            Assert.That(empty.Center, Is.Null);
            Assert.That(empty.Witnesses, Is.Empty);
            Assert.That(point.Radius, Is.Zero);
            AssertCoordinate(point.Center!, 3.0, 4.0);
            Assert.That(point.Center!.Z, Is.EqualTo(8.0));
            Assert.That(point.Geometry!.GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(point.Geometry.SpatialReferenceId, Is.EqualTo(3857));
            AssertCoordinate(line.Center!, 2.5, 0.0);
            Assert.That(line.Radius, Is.EqualTo(2.5));
            Assert.That(line.Witnesses, Has.Length.EqualTo(2));
            Assert.That(emptyWidth.HasResult, Is.False);
            Assert.That(emptyWidth.Distance, Is.EqualTo(double.PositiveInfinity));
            Assert.That(emptyDiameter.HasResult, Is.False);
        });
    }

    private static bool AllCoordinatesCovered(
        Geometry source, Geometry polygon)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(source);
        int index = 0;
        while (index < coordinates.Length)
        {
            if (GeometryPointLocation.LocatePoint(
                coordinates[index], polygon, CurveParameters()) ==
                    TopologicalLocation.Exterior)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    private static bool AllCoordinatesInCircle(
        Geometry source, CircleResult circle)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(source);
        int index = 0;
        while (index < coordinates.Length)
        {
            double distance = SegmentDistanceAlgorithms.GetPlanarDistance(
                circle.Center!, coordinates[index]);
            if (distance > circle.Radius + 1e-12) return false;
            index++;
        }
        return true;
    }

    private static bool IsRectangle(Coordinate[] ring)
    {
        int index = 0;
        while (index < 4)
        {
            Coordinate first = ring[index];
            Coordinate second = ring[index + 1];
            Coordinate third = ring[(index + 2) % 4];
            double firstX = second.X - first.X;
            double firstY = second.Y - first.Y;
            double secondX = third.X - second.X;
            double secondY = third.Y - second.Y;
            if (Math.Abs(firstX * secondX + firstY * secondY) > 1e-12)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    private static CurveLinearizationParameters CurveParameters()
    {
        return new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 2.0,
            MaximumSegments = 16
        };
    }

    private static Coordinate Point(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y)
    {
        Assert.That(coordinate.X, Is.EqualTo(x).Within(1e-12));
        Assert.That(coordinate.Y, Is.EqualTo(y).Within(1e-12));
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
