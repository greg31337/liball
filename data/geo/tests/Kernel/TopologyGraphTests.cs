using geo;

namespace geo.Tests;

public class TopologyGraphTests
{
    [Test]
    public void TopologyLabelReversesAreaSidesAndDepthDeltaByDirection()
    {
        TopologyLabel label = AreaLabel();
        label.SetLineLocation(1, TopologicalLocation.Boundary);
        TopologyLabel copy = label.CopyLabel();
        label.SetLineLocation(1, TopologicalLocation.Interior);

        Assert.Multiple(() =>
        {
            Assert.That(label.GetOnLocation(0), Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(label.GetLeftLocation(0, true),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(label.GetRightLocation(0, false),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(label.GetDepthDelta(0, true), Is.EqualTo(1));
            Assert.That(label.GetDepthDelta(0, false), Is.EqualTo(-1));
            Assert.That(copy.GetOnLocation(1), Is.EqualTo(TopologicalLocation.Boundary));
            Assert.Throws<InvalidGeometryException>(() => label.GetOnLocation(2));
        });
    }

    [Test]
    public void PlanarGraphCreatesSharedNodesSymmetricEdgesAndAngularStars()
    {
        PlanarGraph graph = new PlanarGraph();
        TopologyLabel label = AreaLabel();
        TopologyEdge east = graph.AddEdge([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0)
        ], label);
        TopologyEdge north = graph.AddEdge([
            CoordinateValue(0.0, 0.0), CoordinateValue(0.0, 2.0)
        ], label);
        label.SetLineLocation(1, TopologicalLocation.Interior);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes, Has.Count.EqualTo(3));
            Assert.That(graph.Edges, Has.Count.EqualTo(2));
            Assert.That(graph.DirectedEdges, Has.Count.EqualTo(4));
            Assert.That(east.Forward!.Symmetric, Is.SameAs(east.Reverse));
            Assert.That(east.Reverse!.Symmetric, Is.SameAs(east.Forward));
            Assert.That(east.Forward.FromNode, Is.SameAs(north.Forward!.FromNode));
            Assert.That(east.Label.GetOnLocation(1),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(east.Forward.FromNode.Star.Edges[0], Is.SameAs(east.Forward));
            Assert.That(east.Forward.FromNode.Star.Edges[1], Is.SameAs(north.Forward));
            Assert.That(east.Reverse.GetDirectedCoordinates()[0].X, Is.EqualTo(2.0));
        });
    }

    [Test]
    public void ConnectedSubgraphsAndRightmostNodesAreDeterministic()
    {
        PlanarGraph graph = new PlanarGraph();
        graph.AddEdge([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0)
        ], AreaLabel());
        graph.AddEdge([
            CoordinateValue(10.0, 2.0), CoordinateValue(12.0, 2.0)
        ], AreaLabel());
        graph.AddEdge([
            CoordinateValue(12.0, 2.0), CoordinateValue(12.0, 1.0)
        ], AreaLabel());

        TopologyConnectedSubgraph[] subgraphs =
            ConnectedSubgraphBuilder.BuildSubgraphs(graph);

        Assert.Multiple(() =>
        {
            Assert.That(subgraphs, Has.Length.EqualTo(2));
            Assert.That(subgraphs[0].Id, Is.Zero);
            Assert.That(subgraphs[0].Nodes, Has.Length.EqualTo(2));
            Assert.That(subgraphs[0].Edges, Has.Length.EqualTo(1));
            Assert.That(subgraphs[1].Nodes, Has.Length.EqualTo(3));
            Assert.That(subgraphs[1].Edges, Has.Length.EqualTo(2));
            Assert.That(subgraphs[1].RightmostNode!.Coordinate.X, Is.EqualTo(12.0));
            Assert.That(subgraphs[1].RightmostNode!.Coordinate.Y, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void DepthPropagationClosesAroundEveryNodeOfAnAreaRing()
    {
        PlanarGraph graph = CreateSquareGraph(0.0, 0.0, 4.0, true);
        TopologyConnectedSubgraph subgraph =
            ConnectedSubgraphBuilder.BuildSubgraphs(graph)[0];

        TopologyDepthPropagator.PropagateSubgraphDepths(subgraph, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(subgraph.OutsideDepth, Is.Zero);
            Assert.That(subgraph.DirectedEdges.All(edge =>
                edge.DepthLeft >= 0 && edge.DepthRight >= 0), Is.True);
            Assert.That(graph.Edges.All(edge =>
                edge.Forward!.DepthLeft == 1 && edge.Forward.DepthRight == 0),
                Is.True);
            Assert.That(graph.Edges.All(edge =>
                edge.Reverse!.DepthLeft == 0 && edge.Reverse.DepthRight == 1),
                Is.True);
        });
    }

    [Test]
    public void ResultEdgeLinkingBuildsAClosedMaximalAndMinimalRing()
    {
        PlanarGraph graph = CreateSquareGraph(0.0, 0.0, 4.0, true);

        TopologyRing[] maximal = TopologyRingBuilder.BuildMaximalRings(graph);
        TopologyRing[] minimal = TopologyRingBuilder.BuildMinimalRings(maximal);

        Assert.Multiple(() =>
        {
            Assert.That(maximal, Has.Length.EqualTo(1));
            Assert.That(maximal[0].IsMaximal, Is.True);
            Assert.That(maximal[0].DirectedEdges, Has.Length.EqualTo(4));
            Assert.That(maximal[0].Coordinates, Has.Length.EqualTo(5));
            Assert.That(maximal[0].AbsoluteArea, Is.EqualTo(16.0));
            Assert.That(minimal, Has.Length.EqualTo(1));
            Assert.That(minimal[0].IsMaximal, Is.False);
            Assert.That(minimal[0].AbsoluteArea, Is.EqualTo(16.0));
            Assert.That(graph.DirectedEdges.Count(edge => edge.InResult && edge.Visited),
                Is.EqualTo(4));
        });
    }

    [Test]
    public void RepeatedNodeMaximalRingSplitsIntoMinimalCycles()
    {
        TopologyRing maximal = TopologyRingBuilder.CreateCoordinateRing([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0),
            CoordinateValue(1.0, 1.0), CoordinateValue(0.0, 0.0),
            CoordinateValue(-1.0, 1.0), CoordinateValue(-2.0, 0.0),
            CoordinateValue(0.0, 0.0)
        ], true);

        TopologyRing[] minimal = TopologyRingBuilder.BuildMinimalRings([maximal]);

        Assert.Multiple(() =>
        {
            Assert.That(minimal, Has.Length.EqualTo(2));
            Assert.That(minimal[0].Coordinates, Has.Length.EqualTo(4));
            Assert.That(minimal[1].Coordinates, Has.Length.EqualTo(4));
            Assert.That(minimal[0].AbsoluteArea, Is.EqualTo(1.0));
            Assert.That(minimal[1].AbsoluteArea, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void PolygonBuilderAssignsNestedHolesAndIslandsByContainment()
    {
        TopologyRing outer = CoordinateRing([
            CoordinateValue(0.0, 0.0), CoordinateValue(0.0, 10.0),
            CoordinateValue(10.0, 10.0), CoordinateValue(10.0, 0.0),
            CoordinateValue(0.0, 0.0)
        ]);
        TopologyRing hole = CoordinateRing([
            CoordinateValue(2.0, 2.0), CoordinateValue(8.0, 2.0),
            CoordinateValue(8.0, 8.0), CoordinateValue(2.0, 8.0),
            CoordinateValue(2.0, 2.0)
        ]);
        TopologyRing island = CoordinateRing([
            CoordinateValue(4.0, 4.0), CoordinateValue(4.0, 6.0),
            CoordinateValue(6.0, 6.0), CoordinateValue(6.0, 4.0),
            CoordinateValue(4.0, 4.0)
        ]);
        TopologyRing disjoint = CoordinateRing([
            CoordinateValue(20.0, 0.0), CoordinateValue(22.0, 0.0),
            CoordinateValue(22.0, 2.0), CoordinateValue(20.0, 2.0),
            CoordinateValue(20.0, 0.0)
        ]);

        Polygon[] polygons = TopologyPolygonBuilder.BuildPolygons(
            [hole, disjoint, island, outer]);
        Polygon polygonWithHole = polygons.Single(value => value.Holes.Length == 1);

        Assert.Multiple(() =>
        {
            Assert.That(polygons, Has.Length.EqualTo(3));
            Assert.That(polygonWithHole.Holes, Has.Length.EqualTo(1));
            Assert.That(System.Math.Abs(RingMath.GetSignedArea(
                polygonWithHole.Shell.Coordinates)), Is.EqualTo(100.0));
            Assert.That(RingMath.GetRingOrientation(
                polygonWithHole.Shell.Coordinates),
                Is.EqualTo(RingOrientation.CounterClockwise));
            Assert.That(RingMath.GetRingOrientation(
                polygonWithHole.Holes[0].Coordinates),
                Is.EqualTo(RingOrientation.Clockwise));
            Assert.That(polygons.Count(value => value.Holes.Length == 0), Is.EqualTo(2));
        });
    }

    [Test]
    public void RingBuildingRejectsDanglingResultEdgesAndInvalidRings()
    {
        PlanarGraph graph = new PlanarGraph();
        TopologyEdge edge = graph.AddEdge([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0)
        ], AreaLabel());
        graph.SetResultDirection(edge, true, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<TopologyException>(() =>
                TopologyRingBuilder.BuildMaximalRings(graph));
            Assert.Throws<TopologyException>(() =>
                TopologyRingBuilder.CreateCoordinateRing([
                    CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0),
                    CoordinateValue(0.0, 0.0)
                ], false));
            Assert.Throws<InvalidGeometryException>(() =>
                ConnectedSubgraphBuilder.BuildSubgraphs(null!));
        });
    }

    private static PlanarGraph CreateSquareGraph(
        double minimumX, double minimumY, double size, bool markResult)
    {
        Coordinate a = CoordinateValue(minimumX, minimumY);
        Coordinate b = CoordinateValue(minimumX + size, minimumY);
        Coordinate c = CoordinateValue(minimumX + size, minimumY + size);
        Coordinate d = CoordinateValue(minimumX, minimumY + size);
        PlanarGraph graph = new PlanarGraph();
        TopologyEdge[] edges =
        [
            graph.AddEdge([a, b], AreaLabel()),
            graph.AddEdge([b, c], AreaLabel()),
            graph.AddEdge([c, d], AreaLabel()),
            graph.AddEdge([d, a], AreaLabel())
        ];
        if (markResult)
        {
            int index = 0;
            while (index < edges.Length)
            {
                graph.SetResultDirection(edges[index], true, true);
                index++;
            }
        }
        return graph;
    }

    private static TopologyLabel AreaLabel()
    {
        TopologyLabel label = new TopologyLabel();
        label.SetAreaLocations(
            0, TopologicalLocation.Boundary,
            TopologicalLocation.Interior, TopologicalLocation.Exterior);
        return label;
    }

    private static TopologyRing CoordinateRing(Coordinate[] coordinates)
    {
        return TopologyRingBuilder.CreateCoordinateRing(coordinates, false);
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }
}
