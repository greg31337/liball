using System;
using geo;

namespace geo.Tests;

public class RepresentativePointAlgorithmTests
{
    [Test]
    public void LinealPointUsesLongestSegmentAndInterpolatesOptionalOrdinates()
    {
        Geometry line = Read(
            "LINESTRING ZM (0 0 0 0, 2 0 2 4, 2 6 8 10)");

        Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(line);

        Assert.Multiple(() =>
        {
            AssertCoordinate(point, 2.0, 3.0);
            Assert.That(point!.Z, Is.EqualTo(5.0));
            Assert.That(point.M, Is.EqualTo(7.0));
            Assert.That(line.LineString!.Coordinates[1].Z, Is.EqualTo(2.0));
            AssertLocation(point, line, TopologicalLocation.Interior, Parameters());
        });
    }

    [Test]
    public void CollapsedLineStillProducesAPointOnTheLine()
    {
        Geometry line = Read(
            "LINESTRING Z (4 5 7, 4 5 9)");

        Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(line);

        Assert.Multiple(() =>
        {
            AssertCoordinate(point, 4.0, 5.0);
            Assert.That(point!.Z, Is.EqualTo(8.0));
            AssertLocation(point, line, TopologicalLocation.Interior, Parameters());
        });
    }

    [Test]
    public void PolygonPointUsesWidestInteriorSectionAndAvoidsHoles()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (3 3, 7 3, 7 7, 3 7, 3 3))");

        Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(
            polygon);

        Assert.Multiple(() =>
        {
            AssertCoordinate(point, 5.0, 1.5);
            Assert.That(double.IsNaN(point!.Z), Is.True);
            Assert.That(double.IsNaN(point.M), Is.True);
            AssertLocation(point, polygon, TopologicalLocation.Interior,
                Parameters());
        });
    }

    [Test]
    public void ConcavePolygonPointRemainsInTheInterior()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 10 0, 10 2, 2 2, 2 8, 10 8, 10 10, 0 10, 0 0))");

        Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(
            polygon);

        Assert.Multiple(() =>
        {
            AssertCoordinate(point, 5.0, 1.0);
            AssertLocation(point, polygon, TopologicalLocation.Interior,
                Parameters());
        });
    }

    [Test]
    public void MultiPolygonChoosesTheComponentWithTheWidestSection()
    {
        Geometry polygons = Read(
            "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), ((10 0, 20 0, 20 4, 10 4, 10 0)))");

        Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(
            polygons);

        Assert.Multiple(() =>
        {
            AssertCoordinate(point, 15.0, 2.0);
            AssertLocation(point, polygons, TopologicalLocation.Interior,
                Parameters());
        });
    }

    [Test]
    public void MixedCollectionPrefersAPolygonalInteriorPoint()
    {
        Geometry geometry = Read(
            "GEOMETRYCOLLECTION (POINT (1000 1000), LINESTRING (100 100, 500 100), POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0)))");

        Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(
            geometry);

        Assert.Multiple(() =>
        {
            AssertCoordinate(point, 2.0, 2.0);
            AssertLocation(point, geometry, TopologicalLocation.Interior,
                Parameters());
        });
    }

    [Test]
    public void SurfaceContainersReturnInteriorPatchPoints()
    {
        Geometry[] surfaces =
        [
            Read("TRIANGLE ((0 0, 6 0, 0 4, 0 0))"),
            Read("TIN (((0 0, 6 0, 0 4, 0 0)), ((10 0, 18 0, 10 4, 10 0)))"),
            Read("POLYHEDRALSURFACE (((20 0, 26 0, 26 6, 20 6, 20 0)))"),
            Read("MULTISURFACE (((0 0, 4 0, 4 4, 0 4, 0 0)), TRIANGLE ((10 0, 16 0, 10 3, 10 0)))")
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < surfaces.Length)
            {
                Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(
                    surfaces[index]);
                Assert.That(point, Is.Not.Null);
                AssertLocation(point, surfaces[index],
                    TopologicalLocation.Interior, Parameters());
                index++;
            }
        });
    }

    [Test]
    public void CurvesAreLinearizedBeforeSelectingRepresentativePoints()
    {
        CurveLinearizationParameters parameters = Parameters();
        Geometry circular = Read(
            "CIRCULARSTRING (1 0, 0 1, -1 0)");
        Geometry compound = Read(
            "COMPOUNDCURVE ((0 0, 1 0), (1 0, 5 0))");
        Geometry curvePolygon = Read(
            "CURVEPOLYGON (CIRCULARSTRING (2 0, 0 2, -2 0, 0 -2, 2 0))");
        Geometry multiCurve = Read(
            "MULTICURVE ((0 0, 1 0), CIRCULARSTRING (10 0, 12 2, 14 0))");

        Coordinate? circularPoint =
            RepresentativePointAlgorithm.GetPointOnSurfaceWithCurveParameters(
                circular, parameters);
        Coordinate? compoundPoint =
            RepresentativePointAlgorithm.GetPointOnSurfaceWithCurveParameters(
                compound, parameters);
        Coordinate? surfacePoint =
            RepresentativePointAlgorithm.GetPointOnSurfaceWithCurveParameters(
                curvePolygon, parameters);
        Coordinate? multiPoint =
            RepresentativePointAlgorithm.GetPointOnSurfaceWithCurveParameters(
                multiCurve, parameters);

        Assert.Multiple(() =>
        {
            AssertCoordinate(circularPoint, 0.5, 0.5);
            AssertCoordinate(compoundPoint, 3.0, 0.0);
            AssertCoordinate(surfacePoint, 0.0, -1.0);
            Assert.That(multiPoint, Is.Not.Null);
            AssertLocation(circularPoint, circular,
                TopologicalLocation.Interior, parameters);
            AssertLocation(compoundPoint, compound,
                TopologicalLocation.Interior, parameters);
            AssertLocation(surfacePoint, curvePolygon,
                TopologicalLocation.Interior, parameters);
            AssertLocation(multiPoint, multiCurve,
                TopologicalLocation.Interior, parameters);
        });
    }

    [Test]
    public void UnrepresentableInteriorMidpointFallsBackToBoundary()
    {
        Geometry polygon = new Geometry
        {
            GeometryType = GeometryType.Polygon,
            Point = null,
            Polygon = new Polygon
            {
                Shell = new LinearRing
                {
                    Coordinates =
                    [
                        CoordinateValue(0.0, 0.0),
                        CoordinateValue(2.0, 0.0),
                        CoordinateValue(0.0, double.Epsilon),
                        CoordinateValue(0.0, 0.0)
                    ]
                }
            }
        };

        Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(
            polygon);

        Assert.Multiple(() =>
        {
            Assert.That(point, Is.Not.Null);
            AssertLocation(point, polygon, TopologicalLocation.Boundary,
                Parameters());
        });
    }

    [Test]
    public void PointAndEmptyInputsReturnCopiesOrNullAndInvalidInputsFail()
    {
        Geometry pointGeometry = Read("POINT ZM (2 3 7 11)");
        Geometry empty = Read(
            "GEOMETRYCOLLECTION (POINT EMPTY, LINESTRING EMPTY, POLYGON EMPTY)");
        Geometry malformed = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [CoordinateValue(1.0, 2.0)]
            }
        };

        Coordinate? point = RepresentativePointAlgorithm.GetPointOnSurface(
            pointGeometry);

        Assert.Multiple(() =>
        {
            AssertCoordinate(point, 2.0, 3.0);
            Assert.That(point!.Z, Is.EqualTo(7.0));
            Assert.That(point.M, Is.EqualTo(11.0));
            Assert.That(point, Is.Not.SameAs(
                pointGeometry.Point!.Coordinate));
            Assert.That(RepresentativePointAlgorithm.GetPointOnSurface(empty),
                Is.Null);
            Assert.Throws<InvalidGeometryException>(() =>
                RepresentativePointAlgorithm.GetPointOnSurface(malformed));
            Assert.Throws<InvalidGeometryException>(() =>
                RepresentativePointAlgorithm.GetPointOnSurfaceWithCurveParameters(
                    pointGeometry, null!));
        });
    }

    private static void AssertLocation(
        Coordinate? point, Geometry geometry, TopologicalLocation expected,
        CurveLinearizationParameters parameters)
    {
        Assert.That(point, Is.Not.Null);
        Assert.That(GeometryPointLocation.LocatePoint(
            point!, geometry, parameters), Is.EqualTo(expected));
    }

    private static void AssertCoordinate(
        Coordinate? coordinate, double expectedX, double expectedY)
    {
        Assert.That(coordinate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(coordinate!.X,
                Is.EqualTo(expectedX).Within(1.0e-12));
            Assert.That(coordinate.Y,
                Is.EqualTo(expectedY).Within(1.0e-12));
        });
    }

    private static CurveLinearizationParameters Parameters()
    {
        return new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 2.0,
            MaximumSegments = 32
        };
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
