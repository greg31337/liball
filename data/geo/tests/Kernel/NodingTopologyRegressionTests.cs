using geo;

namespace geo.Tests;

public class NodingTopologyRegressionTests
{
    [Test]
    public void IndexedNodingIsPermutationStableAndFullyNoded()
    {
        int[][] orders =
        [
            [0, 1, 2], [0, 2, 1], [1, 0, 2],
            [1, 2, 0], [2, 0, 1], [2, 1, 0]
        ];
        string[]? expected = null;
        int orderIndex = 0;
        while (orderIndex < orders.Length)
        {
            SegmentString[] sources = ThreeWayCrossing();
            SegmentString[] ordered =
            [
                sources[orders[orderIndex][0]],
                sources[orders[orderIndex][1]],
                sources[orders[orderIndex][2]]
            ];
            NodingResult result = IndexedNoder.NodeSelf(ordered, false);
            string[] signature = SortedEdgeSignatures(result.First);
            expected ??= signature;

            Assert.Multiple(() =>
            {
                Assert.That(result.IntersectionCount, Is.EqualTo(3));
                Assert.That(result.First, Has.Length.EqualTo(6));
                Assert.That(FullyNodedValidator.ValidateOutput(result.First).IsValid,
                    Is.True);
                Assert.That(signature, Is.EqualTo(expected));
            });
            orderIndex++;
        }
    }

    [Test]
    public void NodedSourceOrdinatesReconcileAtTheSharedPlanarGraphNode()
    {
        SegmentString horizontal = SegmentStringValue([
            CoordinateValue(0.0, 0.0, 0.0, 10.0),
            CoordinateValue(10.0, 0.0, 10.0, 20.0)
        ], 0);
        SegmentString vertical = SegmentStringValue([
            CoordinateValue(5.0, -5.0, 100.0, 200.0),
            CoordinateValue(5.0, 5.0, 200.0, 400.0)
        ], 1);
        NodingResult noded = IndexedNoder.NodeMutual(
            [horizontal], [vertical], false);
        PlanarGraph graph = new PlanarGraph();
        AddNodedEdges(graph, noded.First);
        AddNodedEdges(graph, noded.Second);
        TopologyNode crossing = graph.GetNodesInPlanarOrder().Single(node =>
            node.Coordinate.X == 5.0 && node.Coordinate.Y == 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(crossing.Coordinate.Z), Is.True);
            Assert.That(double.IsNaN(crossing.Coordinate.M), Is.True);
            Assert.That(crossing.Star.Edges, Has.Count.EqualTo(4));
            Assert.That(crossing.Star.Edges.All(edge =>
            {
                Coordinate endpoint = edge.IsForward ? edge.Edge.Coordinates[0] :
                    edge.Edge.Coordinates[edge.Edge.Coordinates.Length - 1];
                return double.IsNaN(endpoint.Z) && double.IsNaN(endpoint.M);
            }), Is.True);
            Assert.That(graph.Nodes.Single(node => node.Coordinate.X == 0.0).
                Coordinate.Z, Is.Zero);
            Assert.That(graph.Nodes.Single(node => node.Coordinate.Y == 5.0).
                Coordinate.Z, Is.EqualTo(200.0));
        });
    }

    [Test]
    public void SnapRoundingIsPermutationStableForAnOffGridCrossing()
    {
        int[][] orders = [[0, 1], [1, 0]];
        string[]? expectedEdges = null;
        string[]? expectedPixels = null;
        int orderIndex = 0;
        while (orderIndex < orders.Length)
        {
            SegmentString[] sources =
            [
                SegmentStringValue([
                    CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 1.0)
                ], 0),
                SegmentStringValue([
                    CoordinateValue(0.0, 1.0), CoordinateValue(2.0, 0.0)
                ], 1)
            ];
            SnapRoundingResult result = SnapRoundingNoder.NodeToGrid(
                [sources[orders[orderIndex][0]], sources[orders[orderIndex][1]]],
                FixedPrecision());
            string[] edges = SortedEdgeSignatures(result.Output);
            string[] pixels = result.HotPixels.Select(pixel =>
                CoordinateText(pixel.Center)).OrderBy(value => value).ToArray();
            expectedEdges ??= edges;
            expectedPixels ??= pixels;

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(edges, Is.EqualTo(expectedEdges));
                Assert.That(pixels, Is.EqualTo(expectedPixels));
                Assert.That(FullyNodedValidator.ValidateOutput(result.Output).IsValid,
                    Is.True);
            });
            orderIndex++;
        }
    }

    [Test]
    public void BranchingNodeLinksTouchingCyclesIntoSeparateRings()
    {
        PlanarGraph graph = new PlanarGraph();
        AddResultCycle(graph, [
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0),
            CoordinateValue(2.0, 2.0), CoordinateValue(0.0, 2.0),
            CoordinateValue(0.0, 0.0)
        ]);
        AddResultCycle(graph, [
            CoordinateValue(0.0, 0.0), CoordinateValue(-2.0, 0.0),
            CoordinateValue(-2.0, -2.0), CoordinateValue(0.0, -2.0),
            CoordinateValue(0.0, 0.0)
        ]);

        TopologyRing[] maximal = TopologyRingBuilder.BuildMaximalRings(graph);
        TopologyRing[] minimal = TopologyRingBuilder.BuildMinimalRings(maximal);
        Polygon[] polygons = TopologyPolygonBuilder.BuildPolygons(minimal);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes.Single(node =>
                node.Coordinate.X == 0.0 && node.Coordinate.Y == 0.0).
                Star.Edges, Has.Count.EqualTo(4));
            Assert.That(maximal, Has.Length.EqualTo(2));
            Assert.That(minimal, Has.Length.EqualTo(2));
            Assert.That(polygons, Has.Length.EqualTo(2));
            Assert.That(polygons.All(polygon =>
                System.Math.Abs(RingMath.GetSignedArea(
                    polygon.Shell.Coordinates)) == 4.0), Is.True);
        });
    }

    [Test]
    public void PolygonAssemblyIsCanonicalAcrossRingInputOrders()
    {
        int[][] orders =
        [
            [0, 1, 2, 3], [3, 2, 1, 0], [1, 3, 0, 2],
            [2, 0, 3, 1], [3, 0, 2, 1], [1, 2, 0, 3]
        ];
        string[]? expected = null;
        int orderIndex = 0;
        while (orderIndex < orders.Length)
        {
            TopologyRing[] rings = PolygonRings();
            TopologyRing[] ordered =
            [
                rings[orders[orderIndex][0]], rings[orders[orderIndex][1]],
                rings[orders[orderIndex][2]], rings[orders[orderIndex][3]]
            ];
            Polygon[] polygons = TopologyPolygonBuilder.BuildPolygons(ordered);
            string[] signature = polygons.Select(PolygonText).ToArray();
            expected ??= signature;

            Assert.Multiple(() =>
            {
                Assert.That(polygons, Has.Length.EqualTo(2));
                Assert.That(signature, Is.EqualTo(expected));
                Assert.That(polygons[0].Holes.Select(ring =>
                    CoordinateText(ring.Coordinates[0])), Is.Ordered);
            });
            orderIndex++;
        }
    }

    [Test]
    public void DuplicateEdgeMergingIsStableAcrossEveryContributionOrder()
    {
        int[][] orders =
        [
            [0, 1, 2], [0, 2, 1], [1, 0, 2],
            [1, 2, 0], [2, 0, 1], [2, 1, 0]
        ];
        string? expected = null;
        int orderIndex = 0;
        while (orderIndex < orders.Length)
        {
            PlanarGraph graph = new PlanarGraph();
            int contributionIndex = 0;
            while (contributionIndex < orders[orderIndex].Length)
            {
                AddMergeContribution(graph, orders[orderIndex][contributionIndex]);
                contributionIndex++;
            }
            TopologyEdge edge = graph.Edges[0];
            string signature = MergedEdgeText(edge);
            expected ??= signature;

            Assert.Multiple(() =>
            {
                Assert.That(graph.Edges, Has.Count.EqualTo(1));
                Assert.That(edge.MergeCount, Is.EqualTo(3));
                Assert.That(edge.Label.GetDepthDelta(0, true), Is.Zero);
                Assert.That(edge.Label.GetDepthDelta(1, true), Is.EqualTo(1));
                Assert.That(signature, Is.EqualTo(expected));
            });
            orderIndex++;
        }
    }

    [Test]
    public void FullyNodedValidationRejectsTJunctionsAndPartialOverlaps()
    {
        SegmentString horizontal = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0)
        ], 0);
        SegmentString tee = SegmentStringValue([
            CoordinateValue(5.0, 0.0), CoordinateValue(5.0, 3.0)
        ], 1);
        SegmentString overlap = SegmentStringValue([
            CoordinateValue(2.0, 0.0), CoordinateValue(8.0, 0.0)
        ], 2);

        NodingResult teeValidation = FullyNodedValidator.ValidateOutput(
            [horizontal, tee]);
        NodingResult overlapValidation = FullyNodedValidator.ValidateOutput(
            [horizontal, overlap]);

        Assert.Multiple(() =>
        {
            Assert.That(teeValidation.IsValid, Is.False);
            Assert.That(teeValidation.FirstNonNodedIntersection!.Type,
                Is.EqualTo(SegmentIntersectionType.Endpoint));
            Assert.That(overlapValidation.IsValid, Is.False);
            Assert.That(overlapValidation.HasCollinearOverlap, Is.True);
            Assert.That(overlapValidation.FirstNonNodedIntersection!.Type,
                Is.EqualTo(SegmentIntersectionType.CollinearOverlap));
        });
    }

    [Test]
    public void ThreeRepeatedNodeLobesSplitIntoThreeMinimalRings()
    {
        TopologyRing maximal = TopologyRingBuilder.CreateCoordinateRing([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0),
            CoordinateValue(1.0, 1.0), CoordinateValue(0.0, 0.0),
            CoordinateValue(-1.0, 1.0), CoordinateValue(-2.0, 0.0),
            CoordinateValue(0.0, 0.0), CoordinateValue(-1.0, -1.0),
            CoordinateValue(1.0, -1.0), CoordinateValue(0.0, 0.0)
        ], true);

        TopologyRing[] minimal = TopologyRingBuilder.BuildMinimalRings([maximal]);

        Assert.Multiple(() =>
        {
            Assert.That(minimal, Has.Length.EqualTo(3));
            Assert.That(minimal.All(ring => ring.Coordinates.Length >= 4), Is.True);
            Assert.That(minimal.All(ring => ring.AbsoluteArea > 0.0), Is.True);
        });
    }

    private static SegmentString[] ThreeWayCrossing()
    {
        return
        [
            SegmentStringValue([
                CoordinateValue(0.0, 0.0), CoordinateValue(4.0, 0.0)
            ], 0),
            SegmentStringValue([
                CoordinateValue(2.0, -2.0), CoordinateValue(2.0, 2.0)
            ], 1),
            SegmentStringValue([
                CoordinateValue(0.0, -2.0), CoordinateValue(4.0, 2.0)
            ], 2)
        ];
    }

    private static void AddNodedEdges(
        PlanarGraph graph, SegmentString[] segmentStrings)
    {
        int index = 0;
        while (index < segmentStrings.Length)
        {
            graph.AddMergedEdge(
                segmentStrings[index].Coordinates,
                LineLabel(segmentStrings[index].SourceIndex));
            index++;
        }
    }

    private static void AddResultCycle(
        PlanarGraph graph, Coordinate[] coordinates)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            TopologyEdge edge = graph.AddEdge(
                [coordinates[index], coordinates[index + 1]], AreaLabel(0, true));
            graph.SetResultDirection(edge, true, true);
            index++;
        }
    }

    private static TopologyRing[] PolygonRings()
    {
        return
        [
            CoordinateRing([
                CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0),
                CoordinateValue(10.0, 10.0), CoordinateValue(0.0, 10.0),
                CoordinateValue(0.0, 0.0)
            ]),
            CoordinateRing([
                CoordinateValue(1.0, 1.0), CoordinateValue(3.0, 1.0),
                CoordinateValue(3.0, 3.0), CoordinateValue(1.0, 3.0),
                CoordinateValue(1.0, 1.0)
            ]),
            CoordinateRing([
                CoordinateValue(6.0, 6.0), CoordinateValue(9.0, 6.0),
                CoordinateValue(9.0, 9.0), CoordinateValue(6.0, 9.0),
                CoordinateValue(6.0, 6.0)
            ]),
            CoordinateRing([
                CoordinateValue(20.0, 0.0), CoordinateValue(24.0, 0.0),
                CoordinateValue(24.0, 4.0), CoordinateValue(20.0, 4.0),
                CoordinateValue(20.0, 0.0)
            ])
        ];
    }

    private static void AddMergeContribution(PlanarGraph graph, int contribution)
    {
        switch (contribution)
        {
            case 0:
                graph.AddMergedEdge([
                    CoordinateValue(0.0, 0.0, 1.0, 10.0),
                    CoordinateValue(4.0, 0.0, 2.0, 20.0)
                ], AreaLabel(0, true));
                break;
            case 1:
                graph.AddMergedEdge([
                    CoordinateValue(4.0, 0.0, 2.0, 25.0),
                    CoordinateValue(0.0, 0.0, 1.0, 15.0)
                ], AreaLabel(0, true));
                break;
            default:
                graph.AddMergedEdge([
                    CoordinateValue(0.0, 0.0, 3.0, 10.0),
                    CoordinateValue(4.0, 0.0, 4.0, 20.0)
                ], AreaLabel(1, true));
                break;
        }
    }

    private static string MergedEdgeText(TopologyEdge edge)
    {
        return EdgeText(edge) + ":" + edge.Label.GetDepthDelta(0, true) + ":" +
            edge.Label.GetDepthDelta(1, true) + ":" +
            OrdinateText(edge.Coordinates[0].Z) + ":" +
            OrdinateText(edge.Coordinates[0].M) + ":" +
            OrdinateText(edge.Coordinates[1].Z) + ":" +
            OrdinateText(edge.Coordinates[1].M);
    }

    private static string[] SortedEdgeSignatures(SegmentString[] edges)
    {
        return edges.Select(edge => edge.SourceIndex + ":" + EdgeText(edge)).
            OrderBy(value => value).ToArray();
    }

    private static string EdgeText(SegmentString edge)
    {
        return string.Join(";", edge.Coordinates.Select(CoordinateText));
    }

    private static string EdgeText(TopologyEdge edge)
    {
        return string.Join(";", edge.Coordinates.Select(CoordinateText));
    }

    private static string PolygonText(Polygon polygon)
    {
        return string.Join(";", polygon.Shell.Coordinates.Select(CoordinateText)) +
            "|" + string.Join("|", polygon.Holes.Select(hole =>
                string.Join(";", hole.Coordinates.Select(CoordinateText))));
    }

    private static string CoordinateText(Coordinate coordinate)
    {
        return coordinate.X + "," + coordinate.Y;
    }

    private static string OrdinateText(double ordinate)
    {
        return double.IsNaN(ordinate) ? "NaN" : ordinate.ToString();
    }

    private static TopologyRing CoordinateRing(Coordinate[] coordinates)
    {
        return TopologyRingBuilder.CreateCoordinateRing(coordinates, false);
    }

    private static PrecisionModel FixedPrecision()
    {
        return new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 1.0
        };
    }

    private static TopologyLabel AreaLabel(int geometryIndex, bool interiorLeft)
    {
        TopologyLabel label = new TopologyLabel();
        label.SetAreaLocations(
            geometryIndex, TopologicalLocation.Boundary,
            interiorLeft ? TopologicalLocation.Interior : TopologicalLocation.Exterior,
            interiorLeft ? TopologicalLocation.Exterior : TopologicalLocation.Interior);
        return label;
    }

    private static TopologyLabel LineLabel(int geometryIndex)
    {
        TopologyLabel label = new TopologyLabel();
        label.SetLineLocation(geometryIndex, TopologicalLocation.Interior);
        return label;
    }

    private static SegmentString SegmentStringValue(
        Coordinate[] coordinates, int sourceIndex)
    {
        return SegmentNoding.CreateSegmentString(coordinates, null, sourceIndex);
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
