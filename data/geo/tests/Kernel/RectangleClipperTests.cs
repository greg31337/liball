using geo;

namespace geo.Tests;

public class RectangleClipperTests
{
    [Test]
    public void PointClippingPreservesPointDimensionAtEdgesAndEmpties()
    {
        Envelope bounds = Bounds(0.0, 0.0, 4.0, 4.0);

        Geometry inside = RectangleClipper.ClipToEnvelope(
            Read("POINT (2 2)"), bounds);
        Geometry boundary = RectangleClipper.ClipToEnvelope(
            Read("POINT (0 4)"), bounds);
        Geometry outside = RectangleClipper.ClipToEnvelope(
            Read("POINT (5 2)"), bounds);
        Geometry emptyBounds = RectangleClipper.ClipToEnvelope(
            Read("POINT (2 2)"), EnvelopeAlgorithms.CreateEmptyEnvelope());

        Assert.Multiple(() =>
        {
            AssertCanonicalEqual(inside, Read("POINT (2 2)"));
            AssertCanonicalEqual(boundary, Read("POINT (0 4)"));
            AssertEmptyType(outside, GeometryType.Point);
            AssertEmptyType(emptyBounds, GeometryType.Point);
        });
    }

    [Test]
    public void LineClippingInterpolatesOwningZAndMOrdinates()
    {
        Geometry source = Read(
            "LINESTRING ZM (-2 0 0 0, 2 0 4 8)");

        Geometry result = RectangleClipper.ClipToEnvelope(
            source, Bounds(-1.0, -1.0, 1.0, 1.0));
        LineString line = (LineString)
            GeometryDispatch.GetSelectedPayload(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(line.Coordinates[0].X, Is.EqualTo(-1.0));
            Assert.That(line.Coordinates[0].Z, Is.EqualTo(1.0));
            Assert.That(line.Coordinates[0].M, Is.EqualTo(2.0));
            Assert.That(line.Coordinates[1].X, Is.EqualTo(1.0));
            Assert.That(line.Coordinates[1].Z, Is.EqualTo(3.0));
            Assert.That(line.Coordinates[1].M, Is.EqualTo(6.0));
            Assert.That(source.LineString!.Coordinates[0].X,
                Is.EqualTo(-2.0));
        });
    }

    [Test]
    public void PolygonClippingRetainsHolesAndCanonicalAreaOutput()
    {
        Geometry source = Read(
            "POLYGON ((-2 -2, 6 -2, 6 6, -2 6, -2 -2), (1 1, 3 1, 3 3, 1 3, 1 1))");

        Geometry result = RectangleClipper.ClipToEnvelope(
            source, Bounds(0.0, 0.0, 4.0, 4.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Polygon!.Holes.Length, Is.EqualTo(1));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(12.0));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result,
                Read("POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0), (1 1, 1 3, 3 3, 3 1, 1 1))")),
                Is.True);
        });
    }

    [Test]
    public void DegenerateRectanglesProduceLineAndPointCollapses()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");

        Geometry line = RectangleClipper.ClipToEnvelope(
            polygon, Bounds(2.0, 1.0, 2.0, 3.0));
        Geometry point = RectangleClipper.ClipToEnvelope(
            polygon, Bounds(0.0, 0.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            AssertCanonicalEqual(line, Read("LINESTRING (2 1, 2 3)"));
            AssertCanonicalEqual(point, Read("POINT (0 0)"));
        });
    }

    [Test]
    public void GeneralClippingUsesIntersectionForArbitraryCoreMasks()
    {
        Geometry source = Read("LINESTRING (-2 2, 6 2)");
        Geometry polygonMask = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0), (1 1, 3 1, 3 3, 1 3, 1 1))");

        Geometry result = RectangleClipper.ClipThroughIntersection(
            source, polygonMask);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(GeometryEquality.AreTopologicallyEqual(
                result,
                Read("MULTILINESTRING ((0 2, 1 2), (3 2, 4 2))")),
                Is.True);
        });
    }

    [Test]
    public void GridSubdivisionProducesRowMajorAreaPieces()
    {
        Geometry polygon = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))");

        Geometry[] pieces = RectangleClipper.SubdivideToGrid(
            polygon, 2, 2);
        double area = 0.0;
        int index = 0;
        while (index < pieces.Length)
        {
            area += GeometryMeasureAlgorithms.GetProjectedArea(pieces[index]);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(pieces, Has.Length.EqualTo(4));
            Assert.That(pieces.All(piece =>
                piece.GeometryType == GeometryType.Polygon), Is.True);
            Assert.That(area, Is.EqualTo(16.0));
            Assert.That(EnvelopeAlgorithms.CreateGeometryEnvelope(
                pieces[0]).MinX, Is.EqualTo(0.0));
            Assert.That(EnvelopeAlgorithms.CreateGeometryEnvelope(
                pieces[0]).MinY, Is.EqualTo(0.0));
            Assert.That(EnvelopeAlgorithms.CreateGeometryEnvelope(
                pieces[3]).MinX, Is.EqualTo(2.0));
            Assert.That(EnvelopeAlgorithms.CreateGeometryEnvelope(
                pieces[3]).MinY, Is.EqualTo(2.0));
        });
    }

    [Test]
    public void GridSubdivisionSupportsLinesPointsAndEmptyValues()
    {
        Geometry[] linePieces = RectangleClipper.SubdivideToGrid(
            Read("LINESTRING (0 0, 0 4)"), 5, 2);
        Geometry[] pointPieces = RectangleClipper.SubdivideToGrid(
            Read("POINT (3 4)"), 3, 4);
        Geometry[] emptyPieces = RectangleClipper.SubdivideToGrid(
            Read("POLYGON EMPTY"), 2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(linePieces, Has.Length.EqualTo(2));
            Assert.That(linePieces.All(piece =>
                piece.GeometryType == GeometryType.LineString), Is.True);
            Assert.That(linePieces.Sum(piece =>
                GeometryMeasureAlgorithms.GetLength(piece)),
                Is.EqualTo(4.0));
            Assert.That(pointPieces, Has.Length.EqualTo(1));
            Assert.That(pointPieces[0].GeometryType,
                Is.EqualTo(GeometryType.Point));
            Assert.That(emptyPieces, Is.Empty);
        });
    }

    [Test]
    public void ClippingValidatesEnvelopesGridCountsTypesAndMetadata()
    {
        Geometry point = Read("POINT (0 0)");
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        Geometry firstSrid = ReadWithSrid(
            "SRID=4326;LINESTRING (0 0, 2 0)");
        Geometry secondSrid = ReadWithSrid(
            "SRID=3857;POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                RectangleClipper.ClipToEnvelope(point, null!));
            Assert.Throws<InvalidGeometryException>(() =>
                RectangleClipper.ClipToEnvelope(
                    point, new Envelope { MinX = 0.0 }));
            Assert.Throws<InvalidGeometryException>(() =>
                RectangleClipper.SubdivideToGrid(point, 0, 1));
            Assert.Throws<InvalidGeometryException>(() =>
                RectangleClipper.SubdivideToGrid(point, 1, 0));
            Assert.Throws<InvalidGeometryException>(() =>
                RectangleClipper.ClipToEnvelope(
                    triangle, Bounds(0.0, 0.0, 1.0, 1.0)));
            Assert.Throws<InvalidGeometryException>(() =>
                RectangleClipper.ClipThroughIntersection(
                    firstSrid, secondSrid));
        });
    }

    private static Envelope Bounds(
        double minimumX, double minimumY,
        double maximumX, double maximumY)
    {
        return EnvelopeAlgorithms.CreateBoundsEnvelope(
            minimumX, minimumY, maximumX, maximumY);
    }

    private static void AssertEmptyType(
        Geometry geometry, GeometryType geometryType)
    {
        Assert.That(geometry.GeometryType, Is.EqualTo(geometryType));
        Assert.That(GeometryInspection.IsEmptyValue(geometry), Is.True);
    }

    private static void AssertCanonicalEqual(
        Geometry actual, Geometry expected)
    {
        Geometry normalizedExpected =
            GeometryNormalization.NormalizeGeometryValue(expected);
        Assert.That(GeometryEquality.AreExactlyEqual(
            actual, normalizedExpected), Is.True);
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
