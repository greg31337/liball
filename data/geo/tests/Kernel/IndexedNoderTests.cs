using geo;

namespace geo.Tests;

public class IndexedNoderTests
{
    [Test]
    public void MutualNodingSplitsProperCrossingsAndRetainsSourceMetadata()
    {
        object firstMetadata = new object();
        object secondMetadata = new object();
        SegmentString first = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0)
        ], firstMetadata, 4);
        SegmentString second = SegmentStringValue([
            CoordinateValue(5.0, -5.0), CoordinateValue(5.0, 5.0)
        ], secondMetadata, 9);

        NodingResult result = IndexedNoder.NodeMutual([first], [second], false);

        Assert.Multiple(() =>
        {
            Assert.That(result.ValidationOnly, Is.False);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasProperIntersection, Is.True);
            Assert.That(result.HasCollinearOverlap, Is.False);
            Assert.That(result.IntersectionCount, Is.EqualTo(1));
            Assert.That(result.FirstIntersection!.Type,
                Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(result.First, Has.Length.EqualTo(2));
            Assert.That(result.Second, Has.Length.EqualTo(2));
            AssertPiece(result.First[0], 0.0, 0.0, 5.0, 0.0);
            AssertPiece(result.First[1], 5.0, 0.0, 10.0, 0.0);
            AssertPiece(result.Second[0], 5.0, -5.0, 5.0, 0.0);
            AssertPiece(result.Second[1], 5.0, 0.0, 5.0, 5.0);
            Assert.That(result.First[0].SourceMetadata, Is.SameAs(firstMetadata));
            Assert.That(result.Second[1].SourceMetadata, Is.SameAs(secondMetadata));
            Assert.That(result.First[1].SourceIndex, Is.EqualTo(4));
            Assert.That(result.Second[0].SourceIndex, Is.EqualTo(9));
            Assert.That(first.Nodes.GetSortedNodes(), Has.Length.EqualTo(2));
            Assert.That(second.Nodes.GetSortedNodes(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void MutualNodingSplitsOnlyTheInteriorSideOfATJunction()
    {
        SegmentString horizontal = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0)
        ], null, 0);
        SegmentString vertical = SegmentStringValue([
            CoordinateValue(5.0, 0.0), CoordinateValue(5.0, 5.0)
        ], null, 1);

        NodingResult result = IndexedNoder.NodeMutual(
            [horizontal], [vertical], false);

        Assert.Multiple(() =>
        {
            Assert.That(result.IntersectionCount, Is.EqualTo(1));
            Assert.That(result.FirstIntersection!.Type,
                Is.EqualTo(SegmentIntersectionType.Endpoint));
            Assert.That(result.First, Has.Length.EqualTo(2));
            Assert.That(result.Second, Has.Length.EqualTo(1));
            AssertPiece(result.First[0], 0.0, 0.0, 5.0, 0.0);
            AssertPiece(result.First[1], 5.0, 0.0, 10.0, 0.0);
            AssertPiece(result.Second[0], 5.0, 0.0, 5.0, 5.0);
        });
    }

    [Test]
    public void MutualNodingSplitsAtBothEndsOfACollinearOverlap()
    {
        SegmentString first = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0)
        ], null, 0);
        SegmentString second = SegmentStringValue([
            CoordinateValue(3.0, 0.0), CoordinateValue(7.0, 0.0)
        ], null, 1);

        NodingResult result = IndexedNoder.NodeMutual([first], [second], false);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasProperIntersection, Is.False);
            Assert.That(result.HasCollinearOverlap, Is.True);
            Assert.That(result.IntersectionCount, Is.EqualTo(1));
            Assert.That(result.FirstIntersection!.Type,
                Is.EqualTo(SegmentIntersectionType.CollinearOverlap));
            Assert.That(result.First, Has.Length.EqualTo(3));
            Assert.That(result.Second, Has.Length.EqualTo(1));
            AssertPiece(result.First[0], 0.0, 0.0, 3.0, 0.0);
            AssertPiece(result.First[1], 3.0, 0.0, 7.0, 0.0);
            AssertPiece(result.First[2], 7.0, 0.0, 10.0, 0.0);
            AssertPiece(result.Second[0], 3.0, 0.0, 7.0, 0.0);
        });
    }

    [Test]
    public void ValidationOnlyReportsNonNodedIntersectionsWithoutSplittingOrMutation()
    {
        SegmentString first = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0)
        ], null, 0);
        SegmentString second = SegmentStringValue([
            CoordinateValue(5.0, -5.0), CoordinateValue(5.0, 5.0)
        ], null, 1);

        NodingResult result = IndexedNoder.NodeMutual([first], [second], true);

        Assert.Multiple(() =>
        {
            Assert.That(result.ValidationOnly, Is.True);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.HasProperIntersection, Is.True);
            Assert.That(result.IntersectionCount, Is.EqualTo(1));
            Assert.That(result.First, Has.Length.EqualTo(1));
            Assert.That(result.Second, Has.Length.EqualTo(1));
            Assert.That(result.First[0], Is.Not.SameAs(first));
            Assert.That(result.Second[0], Is.Not.SameAs(second));
            Assert.That(result.First[0].Nodes.GetSortedNodes(), Has.Length.EqualTo(2));
            Assert.That(result.Second[0].Nodes.GetSortedNodes(), Has.Length.EqualTo(2));
            Assert.That(first.Nodes.GetSortedNodes(), Has.Length.EqualTo(2));
            Assert.That(second.Nodes.GetSortedNodes(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ValidationOnlyAcceptsAnIntersectionThatIsAnEndpointOfBothSegments()
    {
        SegmentString first = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(5.0, 0.0)
        ], null, 0);
        SegmentString second = SegmentStringValue([
            CoordinateValue(5.0, 0.0), CoordinateValue(10.0, 0.0)
        ], null, 1);

        NodingResult result = IndexedNoder.NodeMutual([first], [second], true);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IntersectionCount, Is.EqualTo(1));
            Assert.That(result.FirstIntersection!.Type,
                Is.EqualTo(SegmentIntersectionType.CollinearPoint));
        });
    }

    [Test]
    public void MutualNodingIgnoresPairsWithinTheSameInputGroup()
    {
        SegmentString first = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 10.0)
        ], null, 0);
        SegmentString crossingFirst = SegmentStringValue([
            CoordinateValue(0.0, 10.0), CoordinateValue(10.0, 0.0)
        ], null, 1);
        SegmentString remoteSecond = SegmentStringValue([
            CoordinateValue(20.0, 0.0), CoordinateValue(30.0, 0.0)
        ], null, 2);

        NodingResult result = IndexedNoder.NodeMutual(
            [first, crossingFirst], [remoteSecond], true);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IntersectionCount, Is.Zero);
            Assert.That(result.First, Has.Length.EqualTo(2));
            Assert.That(result.Second, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void SelfNodingSplitsABowTieAndPreservesTheClosedMiddleFragment()
    {
        object metadata = new object();
        SegmentString bowTie = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 2.0),
            CoordinateValue(0.0, 2.0), CoordinateValue(2.0, 0.0)
        ], metadata, 3);

        NodingResult result = IndexedNoder.NodeSelf([bowTie], false);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasProperIntersection, Is.True);
            Assert.That(result.IntersectionCount, Is.EqualTo(1));
            Assert.That(result.Second, Is.Empty);
            Assert.That(result.First, Has.Length.EqualTo(3));
            AssertPiece(result.First[0], 0.0, 0.0, 1.0, 1.0);
            Assert.That(result.First[1].Coordinates, Has.Length.EqualTo(4));
            Assert.That(result.First[1].Coordinates[0].X, Is.EqualTo(1.0));
            Assert.That(result.First[1].Coordinates[0].Y, Is.EqualTo(1.0));
            Assert.That(result.First[1].Coordinates[3].X, Is.EqualTo(1.0));
            Assert.That(result.First[1].Coordinates[3].Y, Is.EqualTo(1.0));
            AssertPiece(result.First[2], 1.0, 1.0, 2.0, 0.0);
            Assert.That(result.First[1].SourceMetadata, Is.SameAs(metadata));
            Assert.That(result.First[1].SourceIndex, Is.EqualTo(3));
            Assert.That(result.First[1].SourceStartSegment, Is.Zero);
            Assert.That(result.First[1].SourceEndSegment, Is.EqualTo(2));
        });
    }

    [Test]
    public void SelfValidationIgnoresAdjacentAndClosingSegmentEndpoints()
    {
        SegmentString square = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0),
            CoordinateValue(2.0, 2.0), CoordinateValue(0.0, 2.0),
            CoordinateValue(0.0, 0.0)
        ], null, 0);

        NodingResult result = IndexedNoder.NodeSelf([square], true);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IntersectionCount, Is.Zero);
            Assert.That(result.HasProperIntersection, Is.False);
            Assert.That(result.HasCollinearOverlap, Is.False);
            Assert.That(result.First, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void SelfNodingDoesNotIgnoreAnAdjacentCollinearOverlap()
    {
        SegmentString backtrack = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(4.0, 0.0),
            CoordinateValue(1.0, 0.0)
        ], null, 0);

        NodingResult validation = IndexedNoder.NodeSelf([backtrack], true);
        NodingResult noded = IndexedNoder.NodeSelf([backtrack], false);

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.HasCollinearOverlap, Is.True);
            Assert.That(validation.IntersectionCount, Is.EqualTo(1));
            Assert.That(noded.First, Has.Length.EqualTo(3));
            AssertPiece(noded.First[0], 0.0, 0.0, 1.0, 0.0);
            AssertPiece(noded.First[1], 1.0, 0.0, 4.0, 0.0);
            AssertPiece(noded.First[2], 4.0, 0.0, 1.0, 0.0);
        });
    }

    [Test]
    public void IndexedNodingCarriesExistingSourceSegmentOffsetsIntoNewPieces()
    {
        SegmentString first = SegmentStringValue([
            CoordinateValue(10.0, 0.0), CoordinateValue(20.0, 0.0)
        ], null, 0);
        first.SourceStartSegment = 5;
        first.SourceEndSegment = 5;
        SegmentString second = SegmentStringValue([
            CoordinateValue(15.0, -1.0), CoordinateValue(15.0, 1.0)
        ], null, 1);
        second.SourceStartSegment = 8;
        second.SourceEndSegment = 8;

        NodingResult result = IndexedNoder.NodeMutual([first], [second], false);

        Assert.Multiple(() =>
        {
            Assert.That(result.First[0].SourceStartSegment, Is.EqualTo(5));
            Assert.That(result.First[0].SourceEndSegment, Is.EqualTo(5));
            Assert.That(result.First[1].SourceStartSegment, Is.EqualTo(5));
            Assert.That(result.First[1].SourceEndSegment, Is.EqualTo(5));
            Assert.That(result.Second[0].SourceStartSegment, Is.EqualTo(8));
            Assert.That(result.Second[1].SourceEndSegment, Is.EqualTo(8));
        });
    }

    [Test]
    public void IndexedNoderRejectsNullArraysAndEntries()
    {
        SegmentString source = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0)
        ], null, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                IndexedNoder.NodeMutual(null!, [source], false));
            Assert.Throws<InvalidGeometryException>(() =>
                IndexedNoder.NodeMutual([source], null!, false));
            Assert.Throws<InvalidGeometryException>(() =>
                IndexedNoder.NodeSelf(null!, false));
            Assert.Throws<InvalidGeometryException>(() =>
                IndexedNoder.NodeSelf([source, null!], false));
        });
    }

    private static void AssertPiece(
        SegmentString segmentString,
        double startX, double startY, double endX, double endY)
    {
        Coordinate[] coordinates = segmentString.Coordinates;
        Assert.That(coordinates[0].X, Is.EqualTo(startX));
        Assert.That(coordinates[0].Y, Is.EqualTo(startY));
        Assert.That(coordinates[coordinates.Length - 1].X, Is.EqualTo(endX));
        Assert.That(coordinates[coordinates.Length - 1].Y, Is.EqualTo(endY));
    }

    private static SegmentString SegmentStringValue(
        Coordinate[] coordinates, object? metadata, int sourceIndex)
    {
        return SegmentNoding.CreateSegmentString(coordinates, metadata, sourceIndex);
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }
}
