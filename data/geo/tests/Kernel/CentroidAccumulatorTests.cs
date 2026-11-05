using System;
using geo;

namespace geo.Tests;

public class CentroidAccumulatorTests
{
    [Test]
    public void PointCentroidAveragesCoordinatesAndLeavesOptionalOrdinatesUnknown()
    {
        Geometry points = Read(
            "MULTIPOINT Z ((0 0 1), (2 4 9), (4 2 20))");

        Coordinate? centroid = CentroidAccumulator.GetCentroid(points);

        Assert.Multiple(() =>
        {
            AssertCoordinate(centroid, 2.0, 2.0);
            Assert.That(double.IsNaN(centroid!.Z), Is.True);
            Assert.That(double.IsNaN(centroid.M), Is.True);
            Assert.That(points.MultiPoint!.Points[1].Coordinate!.Z,
                Is.EqualTo(9.0));
        });
    }

    [Test]
    public void LineCentroidWeightsSegmentMidpointsByPlanarLength()
    {
        Geometry lines = Read(
            "MULTILINESTRING ((0 0, 2 0), (2 0, 2 6))");

        Coordinate? centroid = CentroidAccumulator.GetCentroid(lines);

        AssertCoordinate(centroid, 1.75, 2.25);
    }

    [Test]
    public void PolygonCentroidSubtractsHolesIndependentOfOrientation()
    {
        Geometry counterClockwise = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (1 1, 3 1, 3 5, 1 5, 1 1))");
        Geometry clockwise = Read(
            "POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0), (1 1, 1 5, 3 5, 3 1, 1 1))");

        Coordinate? first = CentroidAccumulator.GetCentroid(counterClockwise);
        Coordinate? second = CentroidAccumulator.GetCentroid(clockwise);

        Assert.Multiple(() =>
        {
            AssertCoordinate(first, 121.0 / 23.0, 119.0 / 23.0);
            AssertCoordinate(second, 121.0 / 23.0, 119.0 / 23.0);
        });
    }

    [Test]
    public void MixedCollectionUsesOnlyHighestDimensionWithNonzeroMeasure()
    {
        Geometry geometry = Read(
            "GEOMETRYCOLLECTION (POINT (1000 1000), LINESTRING (100 100, 200 100), POLYGON ((0 0, 8 0, 8 4, 0 4, 0 0)))");

        Coordinate? centroid = CentroidAccumulator.GetCentroid(geometry);

        AssertCoordinate(centroid, 4.0, 2.0);
    }

    [Test]
    public void DegenerateSurfaceFallsBackToLinesAndZeroLengthLinesFallBackToPoints()
    {
        Geometry collapsedSurface = Read(
            "GEOMETRYCOLLECTION (POLYGON ((0 0, 2 0, 4 0, 0 0)), POINT (100 100))");
        Geometry collapsedLine = Read(
            "GEOMETRYCOLLECTION (LINESTRING (2 3, 2 3), POINT (4 5))");

        Coordinate? surfaceCentroid = CentroidAccumulator.GetCentroid(
            collapsedSurface);
        Coordinate? lineCentroid = CentroidAccumulator.GetCentroid(
            collapsedLine);

        Assert.Multiple(() =>
        {
            AssertCoordinate(surfaceCentroid, 2.0, 0.0);
            AssertCoordinate(lineCentroid, 3.0, 4.0);
        });
    }

    [Test]
    public void SurfaceContainersAggregateProjectedPatchCentroids()
    {
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 6 0, 0 3, 0 0))");
        Geometry tin = Read(
            "TIN (((0 0, 6 0, 0 3, 0 0)), ((6 0, 6 3, 0 3, 6 0)))");
        Geometry polyhedral = Read(
            "POLYHEDRALSURFACE (((10 10, 14 10, 14 14, 10 14, 10 10)))");
        Geometry multiSurface = Read(
            "MULTISURFACE (((0 0, 4 0, 4 4, 0 4, 0 0)), TRIANGLE ((10 0, 14 0, 10 4, 10 0)))");

        Assert.Multiple(() =>
        {
            AssertCoordinate(CentroidAccumulator.GetCentroid(triangle),
                2.0, 1.0);
            AssertCoordinate(CentroidAccumulator.GetCentroid(tin),
                3.0, 1.5);
            AssertCoordinate(CentroidAccumulator.GetCentroid(polyhedral),
                12.0, 12.0);
            AssertCoordinate(CentroidAccumulator.GetCentroid(multiSurface),
                46.0 / 9.0, 16.0 / 9.0);
        });
    }

    [Test]
    public void CurvesAreLinearizedBeforeLineAndAreaAccumulation()
    {
        CurveLinearizationParameters parameters = new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 2.0,
            MaximumSegments = 16
        };
        Geometry circular = Read(
            "CIRCULARSTRING (1 0, 0 1, -1 0)");
        Geometry compound = Read(
            "COMPOUNDCURVE ((0 0, 1 0), (1 0, 3 0))");
        Geometry curvePolygon = Read(
            "CURVEPOLYGON (CIRCULARSTRING (2 0, 0 2, -2 0, 0 -2, 2 0))");

        Assert.Multiple(() =>
        {
            AssertCoordinate(CentroidAccumulator.GetCentroidWithCurveParameters(
                circular, parameters), 0.0, 0.5);
            AssertCoordinate(CentroidAccumulator.GetCentroidWithCurveParameters(
                compound, parameters), 1.5, 0.0);
            AssertCoordinate(CentroidAccumulator.GetCentroidWithCurveParameters(
                curvePolygon, parameters), 0.0, 0.0);
        });
    }

    [Test]
    public void LargeTranslationsDoNotDestabilizeCentroid()
    {
        Geometry translated = Read(
            "POLYGON ((1000000000000000 1000000000000000, 1000000000000003 1000000000000000, 1000000000000003 1000000000000004, 1000000000000000 1000000000000004, 1000000000000000 1000000000000000))");

        Coordinate? centroid = CentroidAccumulator.GetCentroid(translated);

        AssertCoordinate(centroid,
            1000000000000001.5, 1000000000000002.0);
    }

    [Test]
    public void EmptyGeometryReturnsNullAndMalformedLineIsRejected()
    {
        Geometry empty = Read(
            "GEOMETRYCOLLECTION (POINT EMPTY, LINESTRING EMPTY, POLYGON EMPTY, CURVEPOLYGON EMPTY)");
        Geometry malformed = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [new Coordinate { X = 1.0, Y = 2.0 }]
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(CentroidAccumulator.GetCentroid(empty), Is.Null);
            Assert.Throws<InvalidGeometryException>(() =>
                CentroidAccumulator.GetCentroid(malformed));
            Assert.Throws<InvalidGeometryException>(() =>
                CentroidAccumulator.GetCentroidWithCurveParameters(
                    Read("POINT (0 0)"), null!));
        });
    }

    private static void AssertCoordinate(
        Coordinate? coordinate, double expectedX, double expectedY)
    {
        Assert.That(coordinate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(coordinate!.X, Is.EqualTo(expectedX).Within(1.0e-12));
            Assert.That(coordinate.Y, Is.EqualTo(expectedY).Within(1.0e-12));
        });
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
