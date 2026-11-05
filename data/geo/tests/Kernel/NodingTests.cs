using geo;

namespace geo.Tests;

public class NodingTests
{
    [Test]
    public void SegmentStringCreationCleansCoordinatesOwnsValuesAndCarriesMetadata()
    {
        object metadata = new object();
        Coordinate first = CoordinateValue(0.0, 0.0, 1.0, 2.0);
        Coordinate repeated = CoordinateValue(0.0, 0.0, 9.0, 10.0);
        Coordinate end = CoordinateValue(5.0, 0.0, 3.0, 4.0);
        SegmentString segmentString = SegmentNoding.CreateSegmentString(
            [first, repeated, end], metadata, 17);

        first.X = 100.0;
        end.Y = 200.0;
        SegmentNode[] nodes = segmentString.Nodes.GetSortedNodes();

        Assert.Multiple(() =>
        {
            Assert.That(segmentString.Coordinates, Has.Length.EqualTo(2));
            Assert.That(segmentString.Coordinates[0].X, Is.Zero);
            Assert.That(segmentString.Coordinates[0].Z, Is.EqualTo(1.0));
            Assert.That(segmentString.Coordinates[1].Y, Is.Zero);
            Assert.That(segmentString.SourceMetadata, Is.SameAs(metadata));
            Assert.That(segmentString.SourceIndex, Is.EqualTo(17));
            Assert.That(segmentString.SourceStartSegment, Is.Zero);
            Assert.That(segmentString.SourceEndSegment, Is.Zero);
            Assert.That(nodes, Has.Length.EqualTo(2));
            Assert.That(nodes[0].SegmentIndex, Is.Zero);
            Assert.That(nodes[0].SegmentFraction, Is.Zero);
            Assert.That(nodes[1].SegmentFraction, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void IntersectionNodesAreCanonicalSortedAndDeduplicatedAtVertices()
    {
        SegmentString segmentString = SegmentNoding.CreateSegmentString(
            [CoordinateValue(0.0, 0.0), CoordinateValue(5.0, 0.0),
                CoordinateValue(10.0, 0.0)], null, 0);

        SegmentNode interior = SegmentNoding.AddIntersectionNode(
            segmentString, CoordinateValue(2.5, 0.0), 0);
        SegmentNode vertexFromLeft = SegmentNoding.AddIntersectionNode(
            segmentString, CoordinateValue(5.0, 0.0), 0);
        SegmentNode vertexFromRight = SegmentNoding.AddIntersectionNode(
            segmentString, CoordinateValue(5.0, 0.0), 1);
        SegmentNode[] nodes = segmentString.Nodes.GetSortedNodes();

        Assert.Multiple(() =>
        {
            Assert.That(interior.SegmentIndex, Is.Zero);
            Assert.That(interior.SegmentFraction, Is.EqualTo(0.5));
            Assert.That(vertexFromLeft, Is.SameAs(vertexFromRight));
            Assert.That(vertexFromLeft.SegmentIndex, Is.EqualTo(1));
            Assert.That(vertexFromLeft.SegmentFraction, Is.Zero);
            Assert.That(nodes, Has.Length.EqualTo(4));
            Assert.That(nodes[0].Coordinate.X, Is.Zero);
            Assert.That(nodes[1].Coordinate.X, Is.EqualTo(2.5));
            Assert.That(nodes[2].Coordinate.X, Is.EqualTo(5.0));
            Assert.That(nodes[3].Coordinate.X, Is.EqualTo(10.0));
        });
    }

    [Test]
    public void EdgeSplittingRetainsOriginalVerticesAndSourceRanges()
    {
        object metadata = new object();
        SegmentString segmentString = SegmentNoding.CreateSegmentString(
            [CoordinateValue(0.0, 0.0), CoordinateValue(5.0, 0.0),
                CoordinateValue(10.0, 0.0), CoordinateValue(15.0, 0.0)], metadata, 8);
        SegmentNoding.AddIntersectionNode(segmentString, CoordinateValue(2.5, 0.0), 0);
        SegmentNoding.AddIntersectionNode(segmentString, CoordinateValue(12.5, 0.0), 2);

        SegmentString[] splits = SegmentNoding.SplitEdges(segmentString);

        Assert.Multiple(() =>
        {
            Assert.That(splits, Has.Length.EqualTo(3));
            AssertCoordinates(splits[0].Coordinates, [0.0, 2.5]);
            AssertCoordinates(splits[1].Coordinates, [2.5, 5.0, 10.0, 12.5]);
            AssertCoordinates(splits[2].Coordinates, [12.5, 15.0]);
            Assert.That(splits[0].SourceMetadata, Is.SameAs(metadata));
            Assert.That(splits[1].SourceIndex, Is.EqualTo(8));
            Assert.That(splits[1].SourceStartSegment, Is.Zero);
            Assert.That(splits[1].SourceEndSegment, Is.EqualTo(2));
            Assert.That(splits[2].SourceStartSegment, Is.EqualTo(2));
            Assert.That(splits[0].Coordinates[0], Is.Not.SameAs(segmentString.Coordinates[0]));
        });
    }

    [Test]
    public void ProperIntersectionInsertionSplitsBothSourcesWithLocalOrdinates()
    {
        object firstMetadata = new object();
        object secondMetadata = new object();
        SegmentString first = SegmentNoding.CreateSegmentString(
            [CoordinateValue(0.0, 0.0, 0.0, 10.0),
                CoordinateValue(10.0, 10.0, 10.0, 20.0)], firstMetadata, 1);
        SegmentString second = SegmentNoding.CreateSegmentString(
            [CoordinateValue(0.0, 10.0, 100.0, 200.0),
                CoordinateValue(10.0, 0.0, 200.0, 400.0)], secondMetadata, 2);

        SegmentIntersectionResult result = SegmentNoding.InsertSegmentIntersections(
            first, 0, second, 0);
        SegmentString[] firstSplits = SegmentNoding.SplitEdges(first);
        SegmentString[] secondSplits = SegmentNoding.SplitEdges(second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(firstSplits, Has.Length.EqualTo(2));
            Assert.That(secondSplits, Has.Length.EqualTo(2));
            Assert.That(firstSplits[0].Coordinates[1].X, Is.EqualTo(5.0));
            Assert.That(firstSplits[0].Coordinates[1].Z, Is.EqualTo(5.0));
            Assert.That(firstSplits[0].Coordinates[1].M, Is.EqualTo(15.0));
            Assert.That(secondSplits[0].Coordinates[1].X, Is.EqualTo(5.0));
            Assert.That(secondSplits[0].Coordinates[1].Z, Is.EqualTo(150.0));
            Assert.That(secondSplits[0].Coordinates[1].M, Is.EqualTo(300.0));
            Assert.That(firstSplits[1].SourceMetadata, Is.SameAs(firstMetadata));
            Assert.That(secondSplits[1].SourceMetadata, Is.SameAs(secondMetadata));
        });
    }

    [Test]
    public void CollinearOverlapInsertsBothEndpointsWithoutDuplicatePieces()
    {
        SegmentString first = SegmentNoding.CreateSegmentString(
            [CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0)], null, 0);
        SegmentString second = SegmentNoding.CreateSegmentString(
            [CoordinateValue(3.0, 0.0), CoordinateValue(7.0, 0.0)], null, 1);

        SegmentIntersectionResult result = SegmentNoding.InsertSegmentIntersections(
            first, 0, second, 0);
        SegmentString[] firstSplits = SegmentNoding.SplitEdges(first);
        SegmentString[] secondSplits = SegmentNoding.SplitEdges(second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.CollinearOverlap));
            Assert.That(first.Nodes.GetSortedNodes(), Has.Length.EqualTo(4));
            Assert.That(second.Nodes.GetSortedNodes(), Has.Length.EqualTo(2));
            Assert.That(firstSplits, Has.Length.EqualTo(3));
            AssertCoordinates(firstSplits[0].Coordinates, [0.0, 3.0]);
            AssertCoordinates(firstSplits[1].Coordinates, [3.0, 7.0]);
            AssertCoordinates(firstSplits[2].Coordinates, [7.0, 10.0]);
            Assert.That(secondSplits, Has.Length.EqualTo(1));
            AssertCoordinates(secondSplits[0].Coordinates, [3.0, 7.0]);
        });
    }

    [Test]
    public void RoundedRationalIntersectionWitnessIsAcceptedAndProjectedPerSource()
    {
        SegmentString first = SegmentNoding.CreateSegmentString(
            [CoordinateValue(-1.0, -1.0), CoordinateValue(0.0, 0.0)], null, 0);
        SegmentString second = SegmentNoding.CreateSegmentString(
            [CoordinateValue(-1.0, 0.0), CoordinateValue(1.0, -1.0)], null, 1);

        SegmentIntersectionResult result = SegmentNoding.InsertSegmentIntersections(
            first, 0, second, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(first.Nodes.GetSortedNodes(), Has.Length.EqualTo(3));
            Assert.That(second.Nodes.GetSortedNodes(), Has.Length.EqualTo(3));
            Assert.That(first.Nodes.GetSortedNodes()[1].Coordinate.X,
                Is.EqualTo(-1.0 / 3.0).Within(1.0e-15));
            Assert.That(second.Nodes.GetSortedNodes()[1].Coordinate.X,
                Is.EqualTo(-1.0 / 3.0).Within(1.0e-15));
        });
    }

    [Test]
    public void DuplicateRemovalRetainsNonConsecutiveCoordinatesAndFirstOrdinates()
    {
        Coordinate[] cleaned = SegmentNoding.RemoveRepeatedCoordinateValues(
            [CoordinateValue(0.0, 0.0, 1.0, 2.0),
                CoordinateValue(0.0, 0.0, 9.0, 10.0),
                CoordinateValue(1.0, 0.0), CoordinateValue(0.0, 0.0)]);

        Assert.Multiple(() =>
        {
            Assert.That(cleaned, Has.Length.EqualTo(3));
            Assert.That(cleaned[0].X, Is.Zero);
            Assert.That(cleaned[0].Z, Is.EqualTo(1.0));
            Assert.That(cleaned[1].X, Is.EqualTo(1.0));
            Assert.That(cleaned[2].X, Is.Zero);
        });
    }

    [Test]
    public void NodingRejectsCollapsedNonFiniteOffSegmentAndInvalidIndexInputs()
    {
        SegmentString valid = SegmentNoding.CreateSegmentString(
            [CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0)], null, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentNoding.CreateSegmentString(null!, null, 0));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentNoding.CreateSegmentString(
                    [CoordinateValue(1.0, 1.0), CoordinateValue(1.0, 1.0)], null, 0));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentNoding.CreateSegmentString(
                    [CoordinateValue(0.0, 0.0), CoordinateValue(double.NaN, 1.0)], null, 0));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentNoding.CreateSegmentString(
                    [CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0)], null, -1));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentNoding.AddIntersectionNode(valid, CoordinateValue(5.0, 2.0), 0));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentNoding.AddIntersectionNode(valid, CoordinateValue(5.0, 0.0), 1));
        });
    }

    private static void AssertCoordinates(Coordinate[] coordinates, double[] expectedX)
    {
        Assert.That(coordinates, Has.Length.EqualTo(expectedX.Length));
        int index = 0;
        while (index < expectedX.Length)
        {
            Assert.That(coordinates[index].X, Is.EqualTo(expectedX[index]));
            Assert.That(coordinates[index].Y, Is.Zero);
            index++;
        }
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
