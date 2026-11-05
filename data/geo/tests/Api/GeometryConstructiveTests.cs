using geo;

namespace geo.Tests;

public class GeometryConstructiveTests
{
    [Test]
    public void HullAndMinimumShapeMethodsExposeOwningKernelResults()
    {
        IGeometryConstructive constructive = new GeometryConstructive();
        Geometry source = Read(
            "MULTIPOINT ((0 0), (4 0), (4 4), (0 4), (2 1))");

        Geometry convex = constructive.ConvexHull(source);
        Geometry concave = constructive.ConcaveHull(source, 1.0, false);
        Geometry rectangle = constructive.MinimumRotatedRectangle(source);
        CircleResult circle = constructive.MinimumBoundingCircle(source);
        DistanceResult width = constructive.MinimumWidth(source);
        source.MultiPoint!.Points[0].Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(convex),
                Is.EqualTo(16.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(concave),
                Is.EqualTo(16.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(rectangle),
                Is.EqualTo(16.0));
            Assert.That(circle.Radius,
                Is.EqualTo(Math.Sqrt(8.0)).Within(1e-12));
            Assert.That(width.Distance, Is.EqualTo(4.0));
            Assert.That(GeometryInspection.ExtractValidatedCoordinates(
                convex).Any(coordinate => coordinate.X == 100.0), Is.False);
        });
    }

    [Test]
    public void BufferAndOffsetMethodsApplyOrdinaryAndSingleSidedPolicies()
    {
        IGeometryConstructive constructive = new GeometryConstructive();
        Geometry line = Read("LINESTRING (0 0, 10 0)");
        BufferParameters parameters = new BufferParameters
        {
            CapStyle = BufferCapStyle.Flat,
            JoinStyle = BufferJoinStyle.Mitre,
            QuadrantSegments = 4,
            MitreLimit = 5.0
        };

        Geometry buffer = constructive.Buffer(line, 1.0, parameters);
        Geometry single = constructive.SingleSidedBuffer(
            line, 1.0, parameters);
        Geometry offset = constructive.OffsetCurve(
            line, 1.0, parameters);
        Envelope singleEnvelope =
            EnvelopeAlgorithms.CreateGeometryEnvelope(single);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(buffer),
                Is.EqualTo(20.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(single),
                Is.EqualTo(10.0));
            Assert.That(singleEnvelope.MinY, Is.EqualTo(0.0));
            Assert.That(singleEnvelope.MaxY, Is.EqualTo(1.0));
            Assert.That(offset.LineString!.Coordinates[0].Y,
                Is.EqualTo(1.0));
            Assert.That(parameters.SingleSided, Is.False);
        });
    }

    [Test]
    public void CircleAndTangentMethodsExposeCentersWitnessesAndSegments()
    {
        IGeometryConstructive constructive = new GeometryConstructive();
        Geometry polygon = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
        Geometry emptyObstacles = Read("GEOMETRYCOLLECTION EMPTY");

        CircleResult inscribed = constructive.MaximumInscribedCircle(
            polygon, 0.01);
        CircleResult empty = constructive.LargestEmptyCircle(
            emptyObstacles, polygon, 0.01);
        Geometry tangents = constructive.PolygonTangents(
            polygon, new Coordinate { X = -2.0, Y = 5.0 });

        Assert.Multiple(() =>
        {
            AssertCoordinate(inscribed.Center!, 5.0, 5.0);
            Assert.That(inscribed.Radius, Is.EqualTo(5.0).Within(0.01));
            Assert.That(inscribed.Witnesses, Has.Length.EqualTo(1));
            AssertCoordinate(empty.Center!, 5.0, 5.0);
            Assert.That(empty.Radius, Is.EqualTo(5.0).Within(0.01));
            Assert.That(tangents.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(tangents.MultiLineString!.LineStrings,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void TriangulationVoronoiAndTreeMethodsAreAvailableThroughContract()
    {
        IGeometryConstructive constructive = new GeometryConstructive();
        Geometry sites = Read(
            "MULTIPOINT ((0 0), (10 0), (10 10), (0 10), (5 5))");
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 2 2, 0 4, 0 0))");
        Envelope clip = new Envelope
        {
            MinX = 0.0,
            MinY = 0.0,
            MaxX = 10.0,
            MaxY = 10.0
        };

        Geometry delaunay = constructive.Delaunay(sites);
        Geometry constrained = constructive.ConstrainedDelaunay(polygon);
        Geometry triangulated = constructive.TriangulatePolygon(polygon);
        Geometry voronoi = constructive.Voronoi(
            Read("MULTIPOINT ((2 5), (8 5))"), clip);
        Geometry tree = constructive.MinimumSpanningTree(
            Read("MULTIPOINT ((0 0), (1 0), (1 1), (0 1))"));

        Assert.Multiple(() =>
        {
            Assert.That(delaunay.Tin!.Triangles, Has.Length.EqualTo(4));
            Assert.That(constrained.Tin!.Triangles, Has.Length.EqualTo(3));
            Assert.That(triangulated.Tin!.Triangles, Has.Length.EqualTo(3));
            Assert.That(voronoi.GeometryCollection!.Geometries,
                Has.Length.EqualTo(2));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(voronoi),
                Is.EqualTo(100.0).Within(1e-12));
            Assert.That(tree.MultiLineString!.LineStrings,
                Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void ConstructiveContractPreservesSpecificValidationFailures()
    {
        IGeometryConstructive constructive = new GeometryConstructive();
        Geometry line = Read("LINESTRING (0 0, 1 1)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");
        Envelope invalidEnvelope = new Envelope
        {
            MinX = 1.0,
            MinY = 0.0,
            MaxX = 0.0,
            MaxY = 1.0
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.ConcaveHull(line, -0.1, false));
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.Buffer(line, double.NaN,
                    new BufferParameters()));
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.SingleSidedBuffer(
                    polygon, 1.0, new BufferParameters()));
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.OffsetCurve(
                    polygon, 1.0, new BufferParameters()));
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.MaximumInscribedCircle(polygon, 0.0));
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.PolygonTangents(
                    polygon, new Coordinate { X = 2.0, Y = 2.0 }));
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.ConstrainedDelaunay(line));
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.TriangulatePolygon(line));
            Assert.Throws<InvalidGeometryException>(() =>
                constructive.Voronoi(line, invalidEnvelope));
        });
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y)
    {
        Assert.That(coordinate.X, Is.EqualTo(x).Within(0.01));
        Assert.That(coordinate.Y, Is.EqualTo(y).Within(0.01));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
