using geo;

namespace geo.Tests;

public class EnvelopeAlgorithmsTests
{
    [Test]
    public void CreationSupportsEmptyCoordinateBoundsAndNestedGeometryInputs()
    {
        Envelope empty = EnvelopeAlgorithms.CreateEmptyEnvelope();
        Coordinate coordinate = new Coordinate { X = 2.0, Y = 3.0, Z = 4.0, M = 5.0 };
        Envelope pointEnvelope = EnvelopeAlgorithms.CreateCoordinateEnvelope(coordinate);
        Envelope bounds = EnvelopeAlgorithms.CreateBoundsEnvelope(-4.0, -3.0, 8.0, 9.0);
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry point = factory.CreatePoint(new Coordinate { X = -2.0, Y = 7.0 });
        Geometry polygon = factory.CreateEnvelopePolygon(new Envelope { MinX = 1.0, MinY = -5.0, MaxX = 10.0, MaxY = 4.0 });
        Geometry collection = factory.CreateGeometryCollection([point, polygon, factory.CreatePoint(null)]);
        Envelope geometryEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(collection);
        Envelope emptyGeometryEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(factory.CreateEmptyGeometry(GeometryType.MultiPolygon));

        Assert.Multiple(() =>
        {
            Assert.That(EnvelopeAlgorithms.ValidateAndCheckEmpty(empty), Is.True);
            Assert.That(pointEnvelope.MinX, Is.EqualTo(2.0));
            Assert.That(pointEnvelope.MinY, Is.EqualTo(3.0));
            Assert.That(pointEnvelope.MaxX, Is.EqualTo(2.0));
            Assert.That(pointEnvelope.MaxY, Is.EqualTo(3.0));
            Assert.That(bounds.MinX, Is.EqualTo(-4.0));
            Assert.That(bounds.MaxY, Is.EqualTo(9.0));
            Assert.That(geometryEnvelope.MinX, Is.EqualTo(-2.0));
            Assert.That(geometryEnvelope.MinY, Is.EqualTo(-5.0));
            Assert.That(geometryEnvelope.MaxX, Is.EqualTo(10.0));
            Assert.That(geometryEnvelope.MaxY, Is.EqualTo(7.0));
            Assert.That(EnvelopeAlgorithms.ValidateAndCheckEmpty(emptyGeometryEnvelope), Is.True);
        });
    }

    [Test]
    public void CreationAndValidationRejectMalformedBoundsCoordinatesAndGraphs()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry cyclic = factory.CreateEmptyGeometry(GeometryType.GeometryCollection);
        cyclic.GeometryCollection!.Geometries = [cyclic];

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.CreateBoundsEnvelope(2.0, 0.0, 1.0, 1.0));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.CreateBoundsEnvelope(0.0, double.NaN, 1.0, 1.0));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.CreateCoordinateEnvelope(null!));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.CreateCoordinateEnvelope(new Coordinate { X = double.PositiveInfinity, Y = 0.0 }));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.ValidateAndCheckEmpty(null!));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.ValidateAndCheckEmpty(new Envelope { MinX = 0.0 }));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.ValidateAndCheckEmpty(new Envelope { MinX = 2.0, MinY = 0.0, MaxX = 1.0, MaxY = 1.0 }));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.CreateGeometryEnvelope(cyclic));
        });
    }

    [Test]
    public void ExpansionReturnsIndependentEnvelopesAndHandlesEmptyValues()
    {
        Envelope source = EnvelopeAlgorithms.CreateBoundsEnvelope(0.0, 1.0, 2.0, 3.0);
        Envelope coordinateExpansion = EnvelopeAlgorithms.ExpandEnvelopeByCoordinate(source, new Coordinate { X = -4.0, Y = 5.0 });
        Envelope envelopeExpansion = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(source, EnvelopeAlgorithms.CreateBoundsEnvelope(-2.0, -3.0, 8.0, 2.0));
        Envelope distanceExpansion = EnvelopeAlgorithms.ExpandEnvelopeByDistance(source, 2.0);
        Envelope empty = EnvelopeAlgorithms.CreateEmptyEnvelope();
        Envelope emptyWithCoordinate = EnvelopeAlgorithms.ExpandEnvelopeByCoordinate(empty, new Coordinate { X = 6.0, Y = 7.0 });
        Envelope emptyWithEnvelope = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(empty, source);
        Envelope sourceWithEmpty = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(source, empty);
        Envelope expandedEmpty = EnvelopeAlgorithms.ExpandEnvelopeByDistance(empty, 2.0);

        Assert.Multiple(() =>
        {
            Assert.That(source.MinX, Is.EqualTo(0.0));
            Assert.That(source.MaxY, Is.EqualTo(3.0));
            Assert.That(coordinateExpansion.MinX, Is.EqualTo(-4.0));
            Assert.That(coordinateExpansion.MaxY, Is.EqualTo(5.0));
            Assert.That(envelopeExpansion.MinY, Is.EqualTo(-3.0));
            Assert.That(envelopeExpansion.MaxX, Is.EqualTo(8.0));
            Assert.That(distanceExpansion.MinX, Is.EqualTo(-2.0));
            Assert.That(distanceExpansion.MinY, Is.EqualTo(-1.0));
            Assert.That(distanceExpansion.MaxX, Is.EqualTo(4.0));
            Assert.That(distanceExpansion.MaxY, Is.EqualTo(5.0));
            Assert.That(emptyWithCoordinate.MinX, Is.EqualTo(6.0));
            Assert.That(emptyWithEnvelope.MinX, Is.EqualTo(source.MinX));
            Assert.That(emptyWithEnvelope, Is.Not.SameAs(source));
            Assert.That(sourceWithEmpty.MaxX, Is.EqualTo(source.MaxX));
            Assert.That(sourceWithEmpty, Is.Not.SameAs(source));
            Assert.That(EnvelopeAlgorithms.ValidateAndCheckEmpty(expandedEmpty), Is.True);
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.ExpandEnvelopeByDistance(source, -1.0));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.ExpandEnvelopeByDistance(source, double.PositiveInfinity));
        });
    }

    [Test]
    public void ContainmentAndCoverageAreInclusiveAndEmptySafe()
    {
        Envelope outer = EnvelopeAlgorithms.CreateBoundsEnvelope(0.0, 0.0, 10.0, 10.0);
        Envelope inner = EnvelopeAlgorithms.CreateBoundsEnvelope(2.0, 3.0, 8.0, 9.0);
        Envelope touching = EnvelopeAlgorithms.CreateBoundsEnvelope(0.0, 4.0, 4.0, 6.0);
        Envelope outside = EnvelopeAlgorithms.CreateBoundsEnvelope(-1.0, 4.0, 4.0, 6.0);
        Envelope empty = EnvelopeAlgorithms.CreateEmptyEnvelope();
        Coordinate interior = new Coordinate { X = 5.0, Y = 5.0 };
        Coordinate boundary = new Coordinate { X = 0.0, Y = 5.0 };
        Coordinate exterior = new Coordinate { X = -1.0, Y = 5.0 };

        Assert.Multiple(() =>
        {
            Assert.That(EnvelopeAlgorithms.ContainsEnvelopeCoordinate(outer, interior), Is.True);
            Assert.That(EnvelopeAlgorithms.ContainsEnvelopeCoordinate(outer, boundary), Is.True);
            Assert.That(EnvelopeAlgorithms.CoversEnvelopeCoordinate(outer, boundary), Is.True);
            Assert.That(EnvelopeAlgorithms.CoversEnvelopeCoordinate(outer, exterior), Is.False);
            Assert.That(EnvelopeAlgorithms.ContainsEnvelopeValue(outer, inner), Is.True);
            Assert.That(EnvelopeAlgorithms.ContainsEnvelopeValue(outer, touching), Is.True);
            Assert.That(EnvelopeAlgorithms.CoversEnvelopeValue(outer, outer), Is.True);
            Assert.That(EnvelopeAlgorithms.CoversEnvelopeValue(outer, outside), Is.False);
            Assert.That(EnvelopeAlgorithms.CoversEnvelopeValue(empty, inner), Is.False);
            Assert.That(EnvelopeAlgorithms.CoversEnvelopeValue(outer, empty), Is.False);
        });
    }

    [Test]
    public void IntersectionUnionAndDisjointHandleOverlapTouchAndEmptyValues()
    {
        Envelope first = EnvelopeAlgorithms.CreateBoundsEnvelope(0.0, 0.0, 4.0, 4.0);
        Envelope overlap = EnvelopeAlgorithms.CreateBoundsEnvelope(2.0, -2.0, 6.0, 3.0);
        Envelope touch = EnvelopeAlgorithms.CreateBoundsEnvelope(4.0, 1.0, 8.0, 2.0);
        Envelope separate = EnvelopeAlgorithms.CreateBoundsEnvelope(5.0, 5.0, 6.0, 6.0);
        Envelope empty = EnvelopeAlgorithms.CreateEmptyEnvelope();
        Envelope intersection = EnvelopeAlgorithms.IntersectEnvelopeValues(first, overlap);
        Envelope touchIntersection = EnvelopeAlgorithms.IntersectEnvelopeValues(first, touch);
        Envelope missingIntersection = EnvelopeAlgorithms.IntersectEnvelopeValues(first, separate);
        Envelope union = EnvelopeAlgorithms.UnionEnvelopeValues(first, overlap);
        Envelope emptyUnion = EnvelopeAlgorithms.UnionEnvelopeValues(empty, first);

        Assert.Multiple(() =>
        {
            Assert.That(EnvelopeAlgorithms.IntersectsEnvelopeValue(first, overlap), Is.True);
            Assert.That(EnvelopeAlgorithms.IntersectsEnvelopeValue(first, touch), Is.True);
            Assert.That(EnvelopeAlgorithms.IntersectsEnvelopeValue(first, separate), Is.False);
            Assert.That(EnvelopeAlgorithms.IntersectsEnvelopeValue(first, empty), Is.False);
            Assert.That(EnvelopeAlgorithms.IsDisjointEnvelopeValue(first, separate), Is.True);
            Assert.That(EnvelopeAlgorithms.IsDisjointEnvelopeValue(first, touch), Is.False);
            Assert.That(intersection.MinX, Is.EqualTo(2.0));
            Assert.That(intersection.MinY, Is.EqualTo(0.0));
            Assert.That(intersection.MaxX, Is.EqualTo(4.0));
            Assert.That(intersection.MaxY, Is.EqualTo(3.0));
            Assert.That(touchIntersection.MinX, Is.EqualTo(4.0));
            Assert.That(touchIntersection.MaxX, Is.EqualTo(4.0));
            Assert.That(EnvelopeAlgorithms.ValidateAndCheckEmpty(missingIntersection), Is.True);
            Assert.That(union.MinY, Is.EqualTo(-2.0));
            Assert.That(union.MaxX, Is.EqualTo(6.0));
            Assert.That(emptyUnion.MinX, Is.EqualTo(first.MinX));
            Assert.That(emptyUnion, Is.Not.SameAs(first));
        });
    }

    [Test]
    public void DistanceIsZeroForIntersectionAndEuclideanForSeparatedAxes()
    {
        Envelope origin = EnvelopeAlgorithms.CreateBoundsEnvelope(0.0, 0.0, 2.0, 2.0);
        Envelope horizontal = EnvelopeAlgorithms.CreateBoundsEnvelope(5.0, 1.0, 7.0, 3.0);
        Envelope vertical = EnvelopeAlgorithms.CreateBoundsEnvelope(1.0, 6.0, 3.0, 8.0);
        Envelope diagonal = EnvelopeAlgorithms.CreateBoundsEnvelope(5.0, 6.0, 7.0, 8.0);
        Envelope touching = EnvelopeAlgorithms.CreateBoundsEnvelope(2.0, 2.0, 4.0, 4.0);

        Assert.Multiple(() =>
        {
            Assert.That(EnvelopeAlgorithms.DistanceBetweenEnvelopes(origin, horizontal), Is.EqualTo(3.0));
            Assert.That(EnvelopeAlgorithms.DistanceBetweenEnvelopes(origin, vertical), Is.EqualTo(4.0));
            Assert.That(EnvelopeAlgorithms.DistanceBetweenEnvelopes(origin, diagonal), Is.EqualTo(5.0));
            Assert.That(EnvelopeAlgorithms.DistanceBetweenEnvelopes(diagonal, origin), Is.EqualTo(5.0));
            Assert.That(EnvelopeAlgorithms.DistanceBetweenEnvelopes(origin, touching), Is.EqualTo(0.0));
            Assert.That(double.IsNaN(EnvelopeAlgorithms.DistanceBetweenEnvelopes(origin, EnvelopeAlgorithms.CreateEmptyEnvelope())), Is.True);
        });
    }

    [Test]
    public void CenterDimensionsAndAreaHaveDefinedDegenerateAndEmptyBehavior()
    {
        Envelope envelope = EnvelopeAlgorithms.CreateBoundsEnvelope(-2.0, 1.0, 6.0, 5.0);
        Envelope line = EnvelopeAlgorithms.CreateBoundsEnvelope(3.0, 1.0, 3.0, 5.0);
        Envelope empty = EnvelopeAlgorithms.CreateEmptyEnvelope();
        Coordinate center = EnvelopeAlgorithms.GetEnvelopeCenter(envelope)!;
        Coordinate extremeCenter = EnvelopeAlgorithms.GetEnvelopeCenter(
            EnvelopeAlgorithms.CreateBoundsEnvelope(-double.MaxValue, -double.MaxValue, double.MaxValue, double.MaxValue))!;

        Assert.Multiple(() =>
        {
            Assert.That(center.X, Is.EqualTo(2.0));
            Assert.That(center.Y, Is.EqualTo(3.0));
            Assert.That(double.IsNaN(center.Z), Is.True);
            Assert.That(double.IsNaN(center.M), Is.True);
            Assert.That(extremeCenter.X, Is.EqualTo(0.0));
            Assert.That(extremeCenter.Y, Is.EqualTo(0.0));
            Assert.That(EnvelopeAlgorithms.GetEnvelopeWidth(envelope), Is.EqualTo(8.0));
            Assert.That(EnvelopeAlgorithms.GetEnvelopeHeight(envelope), Is.EqualTo(4.0));
            Assert.That(EnvelopeAlgorithms.GetEnvelopeArea(envelope), Is.EqualTo(32.0));
            Assert.That(EnvelopeAlgorithms.GetEnvelopeWidth(line), Is.EqualTo(0.0));
            Assert.That(EnvelopeAlgorithms.GetEnvelopeArea(line), Is.EqualTo(0.0));
            Assert.That(EnvelopeAlgorithms.GetEnvelopeCenter(empty), Is.Null);
            Assert.That(EnvelopeAlgorithms.GetEnvelopeWidth(empty), Is.EqualTo(0.0));
            Assert.That(EnvelopeAlgorithms.GetEnvelopeHeight(empty), Is.EqualTo(0.0));
            Assert.That(EnvelopeAlgorithms.GetEnvelopeArea(empty), Is.EqualTo(0.0));
        });
    }

    [Test]
    public void PolygonConversionPreservesBoundsMetadataPrecisionAndEmptyValues()
    {
        GeometryFactoryOptions options = new GeometryFactoryOptions
        {
            SpatialReferenceId = 4326,
            PrecisionModelType = PrecisionModelType.Fixed,
            PrecisionScale = 2.0,
            RingOrientation = RingOrientation.Clockwise
        };
        Envelope envelope = EnvelopeAlgorithms.CreateBoundsEnvelope(0.24, 1.24, 4.26, 5.26);

        Geometry polygon = EnvelopeAlgorithms.ConvertEnvelopeToPolygon(envelope, options);
        Geometry emptyPolygon = EnvelopeAlgorithms.ConvertEnvelopeToPolygon(EnvelopeAlgorithms.CreateEmptyEnvelope(), options);
        Coordinate[] coordinates = polygon.Polygon!.Shell.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(polygon.GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(polygon.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(coordinates[0].Y, Is.EqualTo(1.0));
            Assert.That(coordinates[2].X, Is.EqualTo(4.5));
            Assert.That(coordinates[2].Y, Is.EqualTo(5.5));
            Assert.That(emptyPolygon.GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(emptyPolygon.Polygon!.Shell.Coordinates, Is.Empty);
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.ConvertEnvelopeToPolygon(
                EnvelopeAlgorithms.CreateBoundsEnvelope(1.0, 1.0, 1.0, 2.0), options));
            Assert.Throws<InvalidGeometryException>(() => EnvelopeAlgorithms.ConvertEnvelopeToPolygon(
                envelope,
                new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYZ }));
            Assert.Throws<PrecisionException>(() => EnvelopeAlgorithms.ConvertEnvelopeToPolygon(envelope, null!));
        });
    }
}
