using geo;

namespace geo.Tests;

public class TriangulationAlgorithmsTests
{
    [Test]
    public void DelaunayTriangulatesUniqueSitesIntoCounterClockwiseTin()
    {
        Geometry sites = Read(
            "MULTIPOINT Z ((0 0 1), (10 0 2), (10 10 3), " +
            "(0 10 4), (5 5 5))");
        sites.SpatialReferenceId = 3857;

        Geometry result = DelaunayBuilder.GetDelaunayTriangulation(sites);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType, Is.EqualTo(GeometryType.Tin));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(result.Tin!.Triangles, Has.Length.EqualTo(4));
            Assert.That(GetTinArea(result), Is.EqualTo(100.0).Within(1e-12));
            Assert.That(AllTrianglesCounterClockwise(result), Is.True);
            Assert.That(AllTriangleVerticesBelongTo(sites, result), Is.True);
        });
    }

    [Test]
    public void DelaunayHandlesDuplicateCollinearAndEmptySites()
    {
        Geometry collinear = Read(
            "LINESTRING (0 0, 1 0, 2 0, 1 0)");
        Geometry empty = Read("GEOMETRYCOLLECTION EMPTY");
        Geometry square = Read(
            "MULTIPOINT ((0 0), (1 0), (1 1), (0 1), (0 0))");

        Geometry collinearResult =
            DelaunayBuilder.GetDelaunayTriangulation(collinear);
        Geometry emptyResult =
            DelaunayBuilder.GetDelaunayTriangulation(empty);
        Geometry squareResult =
            DelaunayBuilder.GetDelaunayTriangulation(square);

        Assert.Multiple(() =>
        {
            Assert.That(collinearResult.GeometryType,
                Is.EqualTo(GeometryType.Tin));
            Assert.That(collinearResult.Tin!.Triangles, Is.Empty);
            Assert.That(emptyResult.Tin!.Triangles, Is.Empty);
            Assert.That(squareResult.Tin!.Triangles, Has.Length.EqualTo(2));
            Assert.That(GetTinArea(squareResult),
                Is.EqualTo(1.0).Within(1e-12));
        });
    }

    [Test]
    public void PolygonTriangulationCoversAConcavePolygon()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 2 2, 0 4, 0 0))");

        Geometry result =
            PolygonTriangulator.GetPolygonTriangulation(polygon);

        Assert.Multiple(() =>
        {
            Assert.That(result.Tin!.Triangles, Has.Length.EqualTo(3));
            Assert.That(GetTinArea(result), Is.EqualTo(12.0).Within(1e-12));
            Assert.That(AllTriangleCentroidsInside(result, polygon), Is.True);
            Assert.That(AllTrianglesCounterClockwise(result), Is.True);
        });
    }

    [Test]
    public void PolygonAndConstrainedTriangulationRespectAHole()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
            "(3 3, 3 7, 7 7, 7 3, 3 3))");

        Geometry ordinary =
            PolygonTriangulator.GetPolygonTriangulation(polygon);
        Geometry constrained =
            ConstrainedDelaunayBuilder.GetConstrainedDelaunayTriangulation(
                polygon);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.Tin!.Triangles, Has.Length.EqualTo(8));
            Assert.That(constrained.Tin!.Triangles, Has.Length.EqualTo(8));
            Assert.That(GetTinArea(ordinary), Is.EqualTo(84.0).Within(1e-12));
            Assert.That(GetTinArea(constrained),
                Is.EqualTo(84.0).Within(1e-12));
            Assert.That(AllTriangleCentroidsInside(ordinary, polygon), Is.True);
            Assert.That(AllTriangleCentroidsInside(constrained, polygon),
                Is.True);
        });
    }

    [Test]
    public void VoronoiCellsPartitionTheRequestedClipEnvelope()
    {
        Geometry sites = Read("MULTIPOINT ((2 5), (8 5))");
        Envelope clip = new Envelope
        {
            MinX = 0.0,
            MinY = 0.0,
            MaxX = 10.0,
            MaxY = 10.0
        };

        Geometry result = VoronoiBuilder.GetVoronoiDiagram(sites, clip);
        Geometry[] cells = result.GeometryCollection!.Geometries;
        Envelope firstEnvelope =
            EnvelopeAlgorithms.CreateGeometryEnvelope(cells[0]);
        Envelope secondEnvelope =
            EnvelopeAlgorithms.CreateGeometryEnvelope(cells[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(cells, Has.Length.EqualTo(2));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(100.0).Within(1e-12));
            Assert.That(firstEnvelope.MinX, Is.EqualTo(0.0));
            Assert.That(firstEnvelope.MaxX, Is.EqualTo(5.0));
            Assert.That(secondEnvelope.MinX, Is.EqualTo(5.0));
            Assert.That(secondEnvelope.MaxX, Is.EqualTo(10.0));
        });
    }

    [Test]
    public void MinimumSpanningTreeIsDeterministicAndOwnsItsCoordinates()
    {
        Geometry sites = Read(
            "MULTIPOINT ((0 0), (1 0), (1 1), (0 1), (0 0))");

        Geometry result =
            MinimumSpanningTreeBuilder.GetMinimumSpanningTree(sites);
        LineString[] edges = result.MultiLineString!.LineStrings;
        sites.MultiPoint!.Points[0].Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(edges, Has.Length.EqualTo(3));
            Assert.That(GeometryMeasureAlgorithms.GetLength(result),
                Is.EqualTo(3.0).Within(1e-12));
            AssertCoordinate(edges[0].Coordinates[0], 0.0, 0.0);
            AssertCoordinate(edges[0].Coordinates[1], 0.0, 1.0);
            AssertCoordinate(edges[1].Coordinates[0], 0.0, 0.0);
            AssertCoordinate(edges[1].Coordinates[1], 1.0, 0.0);
        });
    }

    [Test]
    public void TriangulationAndVoronoiRejectInvalidDomains()
    {
        Geometry line = Read("LINESTRING (0 0, 1 1)");
        Envelope invalid = new Envelope
        {
            MinX = 1.0,
            MinY = 0.0,
            MaxX = 0.0,
            MaxY = 1.0
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                PolygonTriangulator.GetPolygonTriangulation(line));
            Assert.Throws<InvalidGeometryException>(() =>
                ConstrainedDelaunayBuilder.GetConstrainedDelaunayTriangulation(
                    line));
            Assert.Throws<InvalidGeometryException>(() =>
                VoronoiBuilder.GetVoronoiDiagram(line, invalid));
        });
    }

    private static double GetTinArea(Geometry tin)
    {
        double area = 0.0;
        foreach (Triangle triangle in tin.Tin!.Triangles)
        {
            area += Math.Abs(RingMath.GetSignedArea(
                triangle.Shell.Coordinates));
        }
        return area;
    }

    private static bool AllTrianglesCounterClockwise(Geometry tin)
    {
        foreach (Triangle triangle in tin.Tin!.Triangles)
        {
            if (RingMath.GetRingOrientation(triangle.Shell.Coordinates) !=
                RingOrientation.CounterClockwise) return false;
        }
        return true;
    }

    private static bool AllTriangleVerticesBelongTo(
        Geometry source, Geometry tin)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(source);
        foreach (Triangle triangle in tin.Tin!.Triangles)
        {
            for (int index = 0; index < 3; index++)
            {
                bool found = false;
                foreach (Coordinate coordinate in coordinates)
                {
                    if (coordinate.X == triangle.Shell.Coordinates[index].X &&
                        coordinate.Y == triangle.Shell.Coordinates[index].Y &&
                        coordinate.Z == triangle.Shell.Coordinates[index].Z)
                    {
                        found = true;
                    }
                }
                if (!found) return false;
            }
        }
        return true;
    }

    private static bool AllTriangleCentroidsInside(
        Geometry tin, Geometry polygon)
    {
        foreach (Triangle triangle in tin.Tin!.Triangles)
        {
            Coordinate[] ring = triangle.Shell.Coordinates;
            Coordinate midpoint =
                RobustLineIntersection.InterpolateLineCoordinate(
                    ring[0], ring[1], 0.5);
            Coordinate centroid =
                RobustLineIntersection.InterpolateLineCoordinate(
                    midpoint, ring[2], 1.0 / 3.0);
            if (GeometryPointLocation.LocatePointInPolygon(
                centroid, polygon.Polygon!) != TopologicalLocation.Interior)
            {
                return false;
            }
        }
        return true;
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
}
