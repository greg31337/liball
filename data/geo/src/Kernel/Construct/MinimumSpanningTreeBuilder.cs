using System.Collections.Generic;

namespace geo;

internal class SpanningTreeEdge
{
    internal int First;
    internal int Second;
    internal double Length;
}

internal static class MinimumSpanningTreeBuilder
{
    internal static Geometry GetMinimumSpanningTree(Geometry geometry)
    {
        Coordinate[] sites = DelaunayBuilder.ExtractUniqueSites(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        if (sites.Length < 2)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.MultiLineString, options);
        }
        List<SpanningTreeEdge> candidates =
            CreateCandidateEdges(sites);
        candidates.Sort((first, second) =>
            CompareEdges(first, second));
        int[] parents = new int[sites.Length];
        int[] ranks = new int[sites.Length];
        int index = 0;
        while (index < parents.Length)
        {
            parents[index] = index;
            index++;
        }
        List<LineString> selected = new List<LineString>();
        index = 0;
        while (index < candidates.Count &&
            selected.Count + 1 < sites.Length)
        {
            SpanningTreeEdge edge = candidates[index];
            int firstRoot = FindRoot(parents, edge.First);
            int secondRoot = FindRoot(parents, edge.Second);
            if (firstRoot != secondRoot)
            {
                JoinRoots(parents, ranks, firstRoot, secondRoot);
                LineString line = new LineString();
                line.Coordinates =
                [
                    RobustLineIntersection.CopyCoordinate(sites[edge.First]),
                    RobustLineIntersection.CopyCoordinate(sites[edge.Second])
                ];
                selected.Add(line);
            }
            index++;
        }
        return GeometryBuilder.CreateMultiLineStringValue(
            selected.ToArray(), options);
    }

    internal static List<SpanningTreeEdge> CreateCandidateEdges(
        Coordinate[] sites)
    {
        List<SpanningTreeEdge> edges = new List<SpanningTreeEdge>();
        int first = 0;
        while (first < sites.Length)
        {
            int second = first + 1;
            while (second < sites.Length)
            {
                SpanningTreeEdge edge = new SpanningTreeEdge();
                edge.First = first;
                edge.Second = second;
                edge.Length = SegmentDistanceAlgorithms.GetPlanarDistance(
                    sites[first], sites[second]);
                edges.Add(edge);
                if (edges.Count > GeometryConstants.DefaultMaximumCoordinates)
                {
                    throw new InvalidGeometryException(
                        "Minimum spanning tree edge count exceeds the supported limit.");
                }
                second++;
            }
            first++;
        }
        return edges;
    }

    internal static int CompareEdges(
        SpanningTreeEdge first, SpanningTreeEdge second)
    {
        if (first.Length < second.Length) return -1;
        if (first.Length > second.Length) return 1;
        if (first.First < second.First) return -1;
        if (first.First > second.First) return 1;
        if (first.Second < second.Second) return -1;
        if (first.Second > second.Second) return 1;
        return 0;
    }

    internal static int FindRoot(int[] parents, int value)
    {
        int root = value;
        while (parents[root] != root)
        {
            root = parents[root];
        }
        int current = value;
        while (parents[current] != current)
        {
            int next = parents[current];
            parents[current] = root;
            current = next;
        }
        return root;
    }

    internal static void JoinRoots(
        int[] parents, int[] ranks, int first, int second)
    {
        if (ranks[first] < ranks[second])
        {
            parents[first] = second;
        }
        else if (ranks[first] > ranks[second])
        {
            parents[second] = first;
        }
        else
        {
            parents[second] = first;
            ranks[first]++;
        }
    }
}
