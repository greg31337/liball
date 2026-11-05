using geo;

namespace geo.Tests;

public class SnapRoundingTests
{
    [Test]
    public void HotPixelDetectsInteriorAndBoundaryHitsWithoutAcceptingMisses()
    {
        HotPixel pixel = new HotPixel(CoordinateValue(1.0, 1.0), 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(pixel.ContainsCoordinate(CoordinateValue(0.5, 1.5)), Is.True);
            Assert.That(pixel.IntersectsSegment(
                CoordinateValue(0.0, 1.0), CoordinateValue(2.0, 1.0)), Is.True);
            Assert.That(pixel.IntersectsSegment(
                CoordinateValue(0.0, 0.5), CoordinateValue(2.0, 0.5)), Is.True);
            Assert.That(pixel.IntersectsSegment(
                CoordinateValue(0.0, 0.49), CoordinateValue(2.0, 0.49)), Is.False);
            Assert.Throws<PrecisionException>(() =>
                new HotPixel(CoordinateValue(1.0, 1.0), 0.0));
        });
    }

    [Test]
    public void VertexSnappingUsesNearestTargetWithDeterministicTieBreaking()
    {
        SegmentString source = SegmentStringValue([
            CoordinateValue(1.0, 0.0, 3.0, 4.0),
            CoordinateValue(4.0, 0.0, 5.0, 6.0)
        ], null, 0);
        SegmentString highTarget = SegmentStringValue([
            CoordinateValue(1.0, 0.2), CoordinateValue(8.0, 8.0)
        ], null, 1);
        SegmentString lowTarget = SegmentStringValue([
            CoordinateValue(1.0, -0.2), CoordinateValue(9.0, 9.0)
        ], null, 2);

        SegmentString[] snapped = GeometrySnapper.SnapToTarget(
            [source], [highTarget, lowTarget], 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(snapped[0].Coordinates[0].X, Is.EqualTo(1.0));
            Assert.That(snapped[0].Coordinates[0].Y, Is.EqualTo(-0.2));
            Assert.That(snapped[0].Coordinates[0].Z, Is.EqualTo(3.0));
            Assert.That(snapped[0].Coordinates[0].M, Is.EqualTo(4.0));
            Assert.That(source.Coordinates[0].Y, Is.Zero);
        });
    }

    [Test]
    public void TargetVertexSnapsIntoNearbySourceSegmentAndSplitsIt()
    {
        object metadata = new object();
        SegmentString source = SegmentStringValue([
            CoordinateValue(0.0, 0.0, 0.0, 10.0),
            CoordinateValue(10.0, 0.0, 10.0, 20.0)
        ], metadata, 7);
        SegmentString target = SegmentStringValue([
            CoordinateValue(5.0, 0.2), CoordinateValue(5.0, 3.0)
        ], null, 8);

        SegmentString[] snapped = GeometrySnapper.SnapToTarget(
            [source], [target], 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(snapped, Has.Length.EqualTo(2));
            AssertPiece(snapped[0], 0.0, 0.0, 5.0, 0.2);
            AssertPiece(snapped[1], 5.0, 0.2, 10.0, 0.0);
            Assert.That(snapped[0].Coordinates[1].Z, Is.EqualTo(5.0));
            Assert.That(snapped[0].Coordinates[1].M, Is.EqualTo(15.0));
            Assert.That(snapped[1].SourceMetadata, Is.SameAs(metadata));
            Assert.That(snapped[1].SourceIndex, Is.EqualTo(7));
            Assert.That(source.Nodes.GetSortedNodes(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void VertexSnapCollapseAdvancesTheRetainedSourceSegmentRange()
    {
        SegmentString source = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(0.1, 0.0),
            CoordinateValue(2.0, 0.0)
        ], null, 3);
        source.SourceStartSegment = 5;
        source.SourceEndSegment = 6;
        SegmentString target = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(0.0, 2.0)
        ], null, 4);

        SegmentString[] snapped = GeometrySnapper.SnapToTarget(
            [source], [target], 0.2);

        Assert.Multiple(() =>
        {
            Assert.That(snapped, Has.Length.EqualTo(1));
            AssertPiece(snapped[0], 0.0, 0.0, 2.0, 0.0);
            Assert.That(snapped[0].SourceStartSegment, Is.EqualTo(6));
            Assert.That(snapped[0].SourceEndSegment, Is.EqualTo(6));
        });
    }

    [Test]
    public void MutualGeometrySnappingProducesSharedNodesOnBothInputs()
    {
        SegmentString horizontal = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0)
        ], null, 0);
        SegmentString nearVertical = SegmentStringValue([
            CoordinateValue(5.0, 0.1), CoordinateValue(5.0, 5.0)
        ], null, 1);

        NodingResult result = GeometrySnapper.SnapMutual(
            [horizontal], [nearVertical], 0.2);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.First, Has.Length.EqualTo(2));
            Assert.That(result.Second, Has.Length.EqualTo(1));
            AssertPiece(result.First[0], 0.0, 0.0, 5.0, 0.1);
            AssertPiece(result.First[1], 5.0, 0.1, 10.0, 0.0);
            AssertPiece(result.Second[0], 5.0, 0.1, 5.0, 5.0);
            Assert.That(FullyNodedValidator.ValidateOutput(
                Combine(result.First, result.Second)).IsValid, Is.True);
        });
    }

    [Test]
    public void SnapRoundingMovesOffGridIntersectionToAHotPixelAndFullyNodesOutput()
    {
        SegmentString rising = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 1.0)
        ], null, 0);
        SegmentString falling = SegmentStringValue([
            CoordinateValue(0.0, 1.0), CoordinateValue(2.0, 0.0)
        ], null, 1);
        PrecisionModel precision = FixedPrecision(1.0, 0.0, 0.0);

        SnapRoundingResult result = SnapRoundingNoder.NodeToGrid(
            [rising, falling], precision);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Output, Has.Length.EqualTo(4));
            Assert.That(result.HotPixels.Any(pixel =>
                pixel.Center.X == 1.0 && pixel.Center.Y == 1.0), Is.True);
            Assert.That(result.Output.Count(edge => EdgeHasEndpoint(edge, 1.0, 1.0)),
                Is.EqualTo(4));
            Assert.That(FullyNodedValidator.ValidateOutput(result.Output).IsValid, Is.True);
            Assert.That(result.Output.All(HasIntegralPlanarCoordinates), Is.True);
        });
    }

    [Test]
    public void SnapRoundingHonorsGridOffsetsAndPreservesLocalOrdinates()
    {
        SegmentString source = SegmentStringValue([
            CoordinateValue(0.2, 0.2, 2.0, 20.0),
            CoordinateValue(2.2, 1.2, 6.0, 40.0)
        ], null, 0);
        SegmentString crossing = SegmentStringValue([
            CoordinateValue(0.2, 1.2, 10.0, 100.0),
            CoordinateValue(2.2, 0.2, 20.0, 200.0)
        ], null, 1);
        PrecisionModel precision = FixedPrecision(1.0, 0.25, 0.25);

        SnapRoundingResult result = SnapRoundingNoder.NodeToGrid(
            [source, crossing], precision);

        SegmentString sourceFirst = result.Output.Single(edge =>
            edge.SourceIndex == 0 && edge.Coordinates[0].X == 0.25);
        Coordinate snappedIntersection = sourceFirst.Coordinates[
            sourceFirst.Coordinates.Length - 1];

        Assert.Multiple(() =>
        {
            Assert.That(snappedIntersection.X, Is.EqualTo(1.25));
            Assert.That(snappedIntersection.Y, Is.EqualTo(1.25));
            Assert.That(snappedIntersection.Z, Is.EqualTo(4.4).Within(1.0e-12));
            Assert.That(snappedIntersection.M, Is.EqualTo(32.0).Within(1.0e-12));
            Assert.That(source.Coordinates[0].X, Is.EqualTo(0.2));
        });
    }

    [Test]
    public void SnapRoundingRemovesEdgesCollapsedByTheGrid()
    {
        object metadata = new object();
        SegmentString collapsed = SegmentStringValue([
            CoordinateValue(0.1, 0.1), CoordinateValue(0.4, 0.4)
        ], metadata, 4);
        SegmentString retained = SegmentStringValue([
            CoordinateValue(0.1, 0.1), CoordinateValue(1.1, 0.1)
        ], metadata, 5);
        SegmentString leadingCollapse = SegmentStringValue([
            CoordinateValue(0.1, 0.1), CoordinateValue(0.4, 0.4),
            CoordinateValue(1.1, 0.1)
        ], metadata, 6);
        leadingCollapse.SourceStartSegment = 10;
        leadingCollapse.SourceEndSegment = 11;

        SnapRoundingResult result = SnapRoundingNoder.NodeToGrid(
            [collapsed, retained, leadingCollapse],
            FixedPrecision(1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.RemovedCollapsedEdgeCount, Is.EqualTo(1));
            Assert.That(result.Output, Has.Length.EqualTo(2));
            AssertPiece(result.Output[0], 0.0, 0.0, 1.0, 0.0);
            Assert.That(result.Output[0].SourceMetadata, Is.SameAs(metadata));
            Assert.That(result.Output[0].SourceIndex, Is.EqualTo(5));
            Assert.That(result.Output[1].SourceIndex, Is.EqualTo(6));
            Assert.That(result.Output[1].SourceStartSegment, Is.EqualTo(11));
            Assert.That(result.Output[1].SourceEndSegment, Is.EqualTo(11));
        });
    }

    [Test]
    public void FullyNodedValidatorReportsAndRejectsResidualCrossings()
    {
        SegmentString first = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 2.0)
        ], null, 0);
        SegmentString sharedEndpoint = SegmentStringValue([
            CoordinateValue(2.0, 2.0), CoordinateValue(3.0, 2.0)
        ], null, 1);
        SegmentString crossing = SegmentStringValue([
            CoordinateValue(0.0, 2.0), CoordinateValue(2.0, 0.0)
        ], null, 2);
        SegmentString[] inputs = [first, sharedEndpoint, crossing];
        NodingResult validation = FullyNodedValidator.ValidateOutput(inputs);

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.HasProperIntersection, Is.True);
            Assert.That(validation.FirstIntersection!.Type,
                Is.EqualTo(SegmentIntersectionType.Endpoint));
            Assert.That(validation.FirstNonNodedIntersection!.Type,
                Is.EqualTo(SegmentIntersectionType.ProperPoint));
            TopologyException? exception = Assert.Throws<TopologyException>(() =>
                FullyNodedValidator.EnsureFullyNoded(inputs));
            Assert.That(exception!.Message, Does.Contain("(1, 1)"));
        });
    }

    [Test]
    public void SnappingRejectsInvalidTolerancePrecisionAndInputs()
    {
        SegmentString source = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0)
        ], null, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<PrecisionException>(() =>
                GeometrySnapper.SnapToTarget([source], [source], -1.0));
            Assert.Throws<PrecisionException>(() =>
                SnapRoundingNoder.NodeToGrid([source], null!));
            Assert.Throws<PrecisionException>(() =>
                SnapRoundingNoder.NodeToGrid([source], new PrecisionModel()));
            Assert.Throws<PrecisionException>(() =>
                SnapRoundingNoder.NodeToGrid([source],
                    FixedPrecision(0.0, 0.0, 0.0)));
            Assert.Throws<InvalidGeometryException>(() =>
                SnapRoundingNoder.NodeToGrid([source, null!],
                    FixedPrecision(1.0, 0.0, 0.0)));
        });
    }

    private static SegmentString[] Combine(
        SegmentString[] first, SegmentString[] second)
    {
        SegmentString[] combined = new SegmentString[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    private static bool EdgeHasEndpoint(
        SegmentString edge, double x, double y)
    {
        Coordinate first = edge.Coordinates[0];
        Coordinate last = edge.Coordinates[edge.Coordinates.Length - 1];
        return first.X == x && first.Y == y || last.X == x && last.Y == y;
    }

    private static bool HasIntegralPlanarCoordinates(SegmentString edge)
    {
        return edge.Coordinates.All(coordinate =>
            coordinate.X == System.Math.Round(coordinate.X) &&
            coordinate.Y == System.Math.Round(coordinate.Y));
    }

    private static void AssertPiece(
        SegmentString edge,
        double startX, double startY, double endX, double endY)
    {
        Coordinate start = edge.Coordinates[0];
        Coordinate end = edge.Coordinates[edge.Coordinates.Length - 1];
        Assert.That(start.X, Is.EqualTo(startX));
        Assert.That(start.Y, Is.EqualTo(startY));
        Assert.That(end.X, Is.EqualTo(endX));
        Assert.That(end.Y, Is.EqualTo(endY));
    }

    private static PrecisionModel FixedPrecision(
        double scale, double offsetX, double offsetY)
    {
        return new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = scale,
            OffsetX = offsetX,
            OffsetY = offsetY
        };
    }

    private static SegmentString SegmentStringValue(
        Coordinate[] coordinates, object? metadata, int sourceIndex)
    {
        return SegmentNoding.CreateSegmentString(coordinates, metadata, sourceIndex);
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
