using System.Collections.Generic;

namespace geo;

internal class TopologyConnectedSubgraph
{
    internal int Id;
    internal TopologyNode[] Nodes = [];
    internal TopologyEdge[] Edges = [];
    internal TopologyDirectedEdge[] DirectedEdges = [];
    internal TopologyNode? RightmostNode;
    internal int OutsideDepth;
}

internal static class ConnectedSubgraphBuilder
{
    internal static TopologyConnectedSubgraph[] BuildSubgraphs(PlanarGraph graph)
    {
        if (graph == null)
        {
            throw new InvalidGeometryException("Planar graph is null.");
        }
        graph.ClearTraversalState();
        List<TopologyConnectedSubgraph> subgraphs =
            new List<TopologyConnectedSubgraph>();
        int nodeIndex = 0;
        while (nodeIndex < graph.Nodes.Count)
        {
            TopologyNode node = graph.Nodes[nodeIndex];
            if (!node.Visited)
            {
                subgraphs.Add(BuildNodeSubgraph(node, subgraphs.Count));
            }
            nodeIndex++;
        }
        return subgraphs.ToArray();
    }

    internal static TopologyConnectedSubgraph BuildNodeSubgraph(
        TopologyNode start, int identifier)
    {
        if (start == null || identifier < 0)
        {
            throw new InvalidGeometryException("Connected-subgraph start is invalid.");
        }
        List<TopologyNode> nodes = new List<TopologyNode>();
        List<TopologyEdge> edges = new List<TopologyEdge>();
        List<TopologyDirectedEdge> directedEdges =
            new List<TopologyDirectedEdge>();
        Queue<TopologyNode> pending = new Queue<TopologyNode>();
        start.Visited = true;
        pending.Enqueue(start);
        TopologyNode rightmost = start;
        while (pending.Count > 0)
        {
            TopologyNode node = pending.Dequeue();
            nodes.Add(node);
            if (IsFartherRight(node, rightmost)) rightmost = node;
            int edgeIndex = 0;
            while (edgeIndex < node.Star.Edges.Count)
            {
                TopologyDirectedEdge directed = node.Star.Edges[edgeIndex];
                directedEdges.Add(directed);
                if (!ContainsEdge(edges, directed.Edge)) edges.Add(directed.Edge);
                if (!directed.ToNode.Visited)
                {
                    directed.ToNode.Visited = true;
                    pending.Enqueue(directed.ToNode);
                }
                edgeIndex++;
            }
        }
        TopologyConnectedSubgraph subgraph = new TopologyConnectedSubgraph();
        subgraph.Id = identifier;
        subgraph.Nodes = nodes.ToArray();
        subgraph.Edges = edges.ToArray();
        subgraph.DirectedEdges = directedEdges.ToArray();
        subgraph.RightmostNode = rightmost;
        return subgraph;
    }

    internal static bool ContainsEdge(
        List<TopologyEdge> edges, TopologyEdge target)
    {
        int index = 0;
        while (index < edges.Count)
        {
            if (edges[index] == target) return true;
            index++;
        }
        return false;
    }

    internal static bool IsFartherRight(
        TopologyNode candidate, TopologyNode current)
    {
        if (candidate.Coordinate.X > current.Coordinate.X) return true;
        return candidate.Coordinate.X == current.Coordinate.X &&
            candidate.Coordinate.Y < current.Coordinate.Y;
    }
}

internal static class TopologyDepthPropagator
{
    internal static void PropagateStarDepths(
        DirectedEdgeStar star, int geometryIndex, int startDepth)
    {
        if (star == null || startDepth < 0)
        {
            throw new TopologyException("Depth propagation input is invalid.");
        }
        TopologyLabel.ValidateGeometryIndex(geometryIndex);
        star.SortEdges();
        int cumulative = 0;
        int minimumCumulative = 0;
        int index = 0;
        while (index < star.Edges.Count)
        {
            cumulative += star.Edges[index].GetDirectedDepthDelta(geometryIndex);
            if (cumulative < minimumCumulative) minimumCumulative = cumulative;
            index++;
        }
        if (cumulative != 0)
        {
            throw new TopologyException(
                "Topology depth changes do not close around a node.");
        }
        int initialDepth = startDepth - minimumCumulative;
        int depth = initialDepth;
        index = 0;
        while (index < star.Edges.Count)
        {
            TopologyDirectedEdge edge = star.Edges[index];
            edge.DepthRight = depth;
            depth += edge.GetDirectedDepthDelta(geometryIndex);
            if (depth < 0)
            {
                throw new TopologyException("Topology depth became negative at a node.");
            }
            edge.DepthLeft = depth;
            index++;
        }
        if (depth != initialDepth)
        {
            throw new TopologyException(
                "Topology depth changes do not close around a node.");
        }
    }

    internal static void PropagateSubgraphDepths(
        TopologyConnectedSubgraph subgraph,
        int geometryIndex, int outsideDepth)
    {
        if (subgraph == null || outsideDepth < 0)
        {
            throw new TopologyException("Connected-subgraph depth input is invalid.");
        }
        subgraph.OutsideDepth = outsideDepth;
        int resetIndex = 0;
        while (resetIndex < subgraph.DirectedEdges.Length)
        {
            subgraph.DirectedEdges[resetIndex].DepthLeft = -1;
            subgraph.DirectedEdges[resetIndex].DepthRight = -1;
            resetIndex++;
        }
        TopologyNode? rightmost = subgraph.RightmostNode;
        if (rightmost == null || rightmost.Star.Edges.Count == 0) return;
        PropagateStarDepths(rightmost.Star, geometryIndex, outsideDepth);
        Queue<TopologyNode> pending = new Queue<TopologyNode>();
        List<TopologyNode> processed = new List<TopologyNode>();
        pending.Enqueue(rightmost);
        processed.Add(rightmost);
        while (pending.Count > 0)
        {
            TopologyNode node = pending.Dequeue();
            int edgeIndex = 0;
            while (edgeIndex < node.Star.Edges.Count)
            {
                TopologyDirectedEdge edge = node.Star.Edges[edgeIndex];
                TopologyDirectedEdge? symmetric = edge.Symmetric;
                if (symmetric == null)
                {
                    throw new TopologyException(
                        "Depth propagation requires symmetric directed edges.");
                }
                SetOrValidateDepths(
                    symmetric, edge.DepthRight, edge.DepthLeft);
                TopologyNode neighbor = symmetric.FromNode;
                if (!ContainsNode(processed, neighbor))
                {
                    PropagateStarFromKnownEdge(
                        neighbor.Star, symmetric, geometryIndex);
                    processed.Add(neighbor);
                    pending.Enqueue(neighbor);
                }
                edgeIndex++;
            }
        }
    }

    internal static void PropagateStarFromKnownEdge(
        DirectedEdgeStar star, TopologyDirectedEdge known,
        int geometryIndex)
    {
        star.SortEdges();
        int knownIndex = -1;
        int index = 0;
        while (index < star.Edges.Count)
        {
            if (star.Edges[index] == known) knownIndex = index;
            index++;
        }
        if (knownIndex < 0 || known.DepthLeft < 0 || known.DepthRight < 0)
        {
            throw new TopologyException("Known directed-edge depth is unavailable.");
        }
        int expectedLeft = known.DepthRight +
            known.GetDirectedDepthDelta(geometryIndex);
        if (known.DepthLeft != expectedLeft)
        {
            throw new TopologyException("Known directed-edge depths conflict with its label.");
        }
        int depth = known.DepthLeft;
        int step = 1;
        while (step < star.Edges.Count)
        {
            int edgeIndex = knownIndex + step;
            if (edgeIndex >= star.Edges.Count) edgeIndex -= star.Edges.Count;
            TopologyDirectedEdge edge = star.Edges[edgeIndex];
            int right = depth;
            depth += edge.GetDirectedDepthDelta(geometryIndex);
            if (depth < 0)
            {
                throw new TopologyException("Topology depth became negative at a node.");
            }
            SetOrValidateDepths(edge, depth, right);
            step++;
        }
        if (depth != known.DepthRight)
        {
            throw new TopologyException(
                "Topology depth changes do not close around a connected node.");
        }
    }

    internal static void SetOrValidateDepths(
        TopologyDirectedEdge edge, int left, int right)
    {
        if (edge.DepthLeft >= 0 && edge.DepthLeft != left ||
            edge.DepthRight >= 0 && edge.DepthRight != right)
        {
            throw new TopologyException(
                "Connected directed-edge depths are inconsistent.");
        }
        edge.DepthLeft = left;
        edge.DepthRight = right;
    }

    internal static bool ContainsNode(
        List<TopologyNode> nodes, TopologyNode target)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == target) return true;
            index++;
        }
        return false;
    }
}
