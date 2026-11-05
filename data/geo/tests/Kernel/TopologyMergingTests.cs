using geo;

namespace geo.Tests;

public class TopologyMergingTests
{
    [Test]
    public void PlanarNodeKeysIgnoreOptionalOrdinatesAndNormalizeSignedZero()
    {
        PlanarGraph graph = new PlanarGraph();
        TopologyNode first = graph.FindOrAddNode(
            CoordinateValue(0.0, -0.0, 5.0, 9.0));
        TopologyNode same = graph.FindOrAddNode(
            CoordinateValue(-0.0, 0.0, 5.0, 9.0));
        TopologyNode distinct = graph.FindOrAddNode(
            CoordinateValue(double.Epsilon, 0.0, 5.0, 9.0));

        Assert.Multiple(() =>
        {
            Assert.That(same, Is.SameAs(first));
            Assert.That(distinct, Is.Not.SameAs(first));
            Assert.That(graph.Nodes, Has.Count.EqualTo(2));
            Assert.That(first.Key.XBits, Is.Zero);
            Assert.That(first.Key.YBits, Is.Zero);
            Assert.That(System.BitConverter.DoubleToInt64Bits(first.Coordinate.X), Is.Zero);
            Assert.That(System.BitConverter.DoubleToInt64Bits(first.Coordinate.Y), Is.Zero);
            Assert.That(first.Coordinate.Z, Is.EqualTo(5.0));
            Assert.That(first.Coordinate.M, Is.EqualTo(9.0));
        });
    }

    [Test]
    public void ConflictingNodeOrdinatesBecomeMissingOnEveryIncidentEndpoint()
    {
        PlanarGraph graph = new PlanarGraph();
        TopologyEdge east = graph.AddEdge([
            CoordinateValue(0.0, 0.0, 1.0, 10.0),
            CoordinateValue(2.0, 0.0, 2.0, 20.0)
        ], LineLabel(0));
        TopologyEdge north = graph.AddEdge([
            CoordinateValue(0.0, 0.0, 3.0, 10.0),
            CoordinateValue(0.0, 2.0, 4.0, 40.0)
        ], LineLabel(1));
        TopologyNode origin = east.Forward!.FromNode;

        Assert.Multiple(() =>
        {
            Assert.That(origin, Is.SameAs(north.Forward!.FromNode));
            Assert.That(double.IsNaN(origin.Coordinate.Z), Is.True);
            Assert.That(origin.Coordinate.M, Is.EqualTo(10.0));
            Assert.That(double.IsNaN(east.Coordinates[0].Z), Is.True);
            Assert.That(double.IsNaN(north.Coordinates[0].Z), Is.True);
            Assert.That(east.Coordinates[0].M, Is.EqualTo(10.0));
            Assert.That(north.Coordinates[0].M, Is.EqualTo(10.0));
        });
    }

    [Test]
    public void MissingAndFiniteContributionsReconcileToMissing()
    {
        Coordinate finite = CoordinateValue(1.0, 2.0, 7.0, 8.0);
        Coordinate missing = CoordinateValue(1.0, 2.0, double.NaN, 8.0);

        Coordinate reconciled = OrdinateReconciler.ReconcileCoordinates(
            finite, missing);

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(reconciled.Z), Is.True);
            Assert.That(reconciled.M, Is.EqualTo(8.0));
            Assert.That(finite.Z, Is.EqualTo(7.0));
            Assert.Throws<InvalidGeometryException>(() =>
                OrdinateReconciler.ReconcileCoordinates(
                    finite, CoordinateValue(2.0, 2.0, 7.0, 8.0)));
        });
    }

    [Test]
    public void ForwardDuplicateEdgesMergeLabelsAndReconcileEveryOrdinate()
    {
        PlanarGraph graph = new PlanarGraph();
        Coordinate[] firstCoordinates =
        [
            CoordinateValue(0.0, 0.0, 1.0, 10.0),
            CoordinateValue(1.0, 1.0, 2.0, 20.0),
            CoordinateValue(2.0, 0.0, 3.0, 30.0)
        ];
        Coordinate[] secondCoordinates =
        [
            CoordinateValue(0.0, 0.0, 1.0, 10.0),
            CoordinateValue(1.0, 1.0, 2.0, 25.0),
            CoordinateValue(2.0, 0.0, 3.0, 30.0)
        ];
        TopologyEdge first = graph.AddMergedEdge(
            firstCoordinates, AreaLabel(0, true));
        TopologyEdge second = graph.AddMergedEdge(
            secondCoordinates, LineLabel(1));

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(graph.Edges, Has.Count.EqualTo(1));
            Assert.That(graph.DirectedEdges, Has.Count.EqualTo(2));
            Assert.That(first.MergeCount, Is.EqualTo(2));
            Assert.That(first.Label.IsSet[0], Is.True);
            Assert.That(first.Label.IsSet[1], Is.True);
            Assert.That(first.Label.GetDepthDelta(0, true), Is.EqualTo(1));
            Assert.That(first.Coordinates[1].Z, Is.EqualTo(2.0));
            Assert.That(double.IsNaN(first.Coordinates[1].M), Is.True);
            Assert.That(secondCoordinates[1].M, Is.EqualTo(25.0));
        });
    }

    [Test]
    public void ReversedDuplicateEdgesUseCanonicalDirectionAndCancelDepthDeltas()
    {
        PlanarGraph graph = new PlanarGraph();
        TopologyEdge first = graph.AddMergedEdge([
            CoordinateValue(0.0, 0.0, 1.0, 10.0),
            CoordinateValue(2.0, 0.0, 2.0, 20.0)
        ], AreaLabel(0, true));
        TopologyEdge reversed = graph.AddMergedEdge([
            CoordinateValue(2.0, 0.0, 2.0, 20.0),
            CoordinateValue(0.0, 0.0, 1.0, 10.0)
        ], AreaLabel(0, true));

        Assert.Multiple(() =>
        {
            Assert.That(reversed, Is.SameAs(first));
            Assert.That(first.Coordinates[0].X, Is.Zero);
            Assert.That(first.Coordinates[1].X, Is.EqualTo(2.0));
            Assert.That(first.Label.GetDepthDelta(0, true), Is.Zero);
            Assert.That(first.Label.GetLeftLocation(0, true),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(first.Label.GetRightLocation(0, true),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(first.Coordinates[0].Z, Is.EqualTo(1.0));
            Assert.That(first.Coordinates[1].Z, Is.EqualTo(2.0));
        });
    }

    [Test]
    public void MergedEdgeStateIsIndependentOfContributionOrder()
    {
        Coordinate[] low =
        [
            CoordinateValue(4.0, 0.0, 2.0, 10.0),
            CoordinateValue(0.0, 0.0, 1.0, 5.0)
        ];
        Coordinate[] high =
        [
            CoordinateValue(0.0, 0.0, 1.0, 7.0),
            CoordinateValue(4.0, 0.0, 2.0, 10.0)
        ];
        PlanarGraph firstGraph = new PlanarGraph();
        firstGraph.AddMergedEdge(low, AreaLabel(0, true));
        firstGraph.AddMergedEdge(high, LineLabel(1));
        PlanarGraph secondGraph = new PlanarGraph();
        secondGraph.AddMergedEdge(high, LineLabel(1));
        secondGraph.AddMergedEdge(low, AreaLabel(0, true));
        TopologyEdge first = firstGraph.Edges[0];
        TopologyEdge second = secondGraph.Edges[0];

        Assert.Multiple(() =>
        {
            Assert.That(first.Coordinates[0].X, Is.EqualTo(second.Coordinates[0].X));
            Assert.That(first.Coordinates[1].X, Is.EqualTo(second.Coordinates[1].X));
            Assert.That(first.Coordinates[0].Z, Is.EqualTo(second.Coordinates[0].Z));
            Assert.That(double.IsNaN(first.Coordinates[0].M), Is.True);
            Assert.That(double.IsNaN(second.Coordinates[0].M), Is.True);
            Assert.That(first.Label.GetDepthDelta(0, true),
                Is.EqualTo(second.Label.GetDepthDelta(0, true)));
            Assert.That(first.Label.GetOnLocation(1),
                Is.EqualTo(second.Label.GetOnLocation(1)));
        });
    }

    [Test]
    public void PlanarOrderingIsStableAcrossInsertionOrders()
    {
        PlanarGraph first = OrderedGraph(false);
        PlanarGraph second = OrderedGraph(true);
        TopologyNode[] firstNodes = first.GetNodesInPlanarOrder();
        TopologyNode[] secondNodes = second.GetNodesInPlanarOrder();
        TopologyEdge[] firstEdges = first.GetEdgesInPlanarOrder();
        TopologyEdge[] secondEdges = second.GetEdgesInPlanarOrder();

        Assert.Multiple(() =>
        {
            Assert.That(firstNodes, Has.Length.EqualTo(secondNodes.Length));
            Assert.That(firstEdges, Has.Length.EqualTo(secondEdges.Length));
            Assert.That(firstNodes.Select(NodeText), Is.EqualTo(secondNodes.Select(NodeText)));
            Assert.That(firstEdges.Select(EdgeText), Is.EqualTo(secondEdges.Select(EdgeText)));
        });
    }

    [Test]
    public void EdgesWithEqualEndpointsButDifferentPathsDoNotMerge()
    {
        PlanarGraph graph = new PlanarGraph();
        graph.AddMergedEdge([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 0.0)
        ], LineLabel(0));
        graph.AddMergedEdge([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, -1.0),
            CoordinateValue(2.0, 0.0)
        ], LineLabel(1));

        Assert.That(graph.Edges, Has.Count.EqualTo(2));
    }

    [Test]
    public void ReconciledNodeOrdinatesFlowIntoBuiltRings()
    {
        PlanarGraph graph = new PlanarGraph();
        TopologyEdge first = graph.AddEdge([
            CoordinateValue(0.0, 0.0, 1.0, 10.0),
            CoordinateValue(2.0, 0.0, 2.0, 20.0)
        ], AreaLabel(0, true));
        TopologyEdge second = graph.AddEdge([
            CoordinateValue(2.0, 0.0, 9.0, 20.0),
            CoordinateValue(2.0, 2.0, 3.0, 30.0)
        ], AreaLabel(0, true));
        TopologyEdge third = graph.AddEdge([
            CoordinateValue(2.0, 2.0, 3.0, 30.0),
            CoordinateValue(0.0, 2.0, 4.0, 40.0)
        ], AreaLabel(0, true));
        TopologyEdge fourth = graph.AddEdge([
            CoordinateValue(0.0, 2.0, 4.0, 40.0),
            CoordinateValue(0.0, 0.0, 1.0, 10.0)
        ], AreaLabel(0, true));
        MarkForwardResult(graph, [first, second, third, fourth]);

        TopologyRing ring = TopologyRingBuilder.BuildMaximalRings(graph)[0];
        Coordinate conflict = ring.Coordinates.Single(coordinate =>
            coordinate.X == 2.0 && coordinate.Y == 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(conflict.Z), Is.True);
            Assert.That(conflict.M, Is.EqualTo(20.0));
            Assert.That(double.IsNaN(first.Coordinates[1].Z), Is.True);
            Assert.That(double.IsNaN(second.Coordinates[0].Z), Is.True);
        });
    }

    private static PlanarGraph OrderedGraph(bool reverseOrder)
    {
        Coordinate[] low =
        [CoordinateValue(-2.0, 0.0), CoordinateValue(0.0, 0.0)];
        Coordinate[] high =
        [CoordinateValue(0.0, 0.0), CoordinateValue(3.0, 2.0)];
        PlanarGraph graph = new PlanarGraph();
        if (reverseOrder)
        {
            graph.AddMergedEdge(high, LineLabel(1));
            graph.AddMergedEdge(low, LineLabel(0));
        }
        else
        {
            graph.AddMergedEdge(low, LineLabel(0));
            graph.AddMergedEdge(high, LineLabel(1));
        }
        return graph;
    }

    private static string NodeText(TopologyNode node)
    {
        return node.Coordinate.X + ":" + node.Coordinate.Y;
    }

    private static string EdgeText(TopologyEdge edge)
    {
        return edge.Coordinates[0].X + ":" + edge.Coordinates[0].Y + "-" +
            edge.Coordinates[edge.Coordinates.Length - 1].X + ":" +
            edge.Coordinates[edge.Coordinates.Length - 1].Y;
    }

    private static void MarkForwardResult(
        PlanarGraph graph, TopologyEdge[] edges)
    {
        int index = 0;
        while (index < edges.Length)
        {
            graph.SetResultDirection(edges[index], true, true);
            index++;
        }
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

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
