using System.Collections.Generic;

namespace geo;

internal class SegmentNodeList
{
    internal SegmentString Owner;
    internal List<SegmentNode> Nodes = new List<SegmentNode>();

    internal SegmentNodeList(SegmentString owner)
    {
        if (owner == null)
        {
            throw new InvalidGeometryException("Segment node list owner is null.");
        }
        Owner = owner;
        AddNodeValue(CreateNode(Owner.Coordinates[0], 0, 0.0));
        int lastSegment = Owner.Coordinates.Length - 2;
        AddNodeValue(CreateNode(
            Owner.Coordinates[Owner.Coordinates.Length - 1], lastSegment, 1.0));
    }

    internal void AddNodeValue(SegmentNode node)
    {
        if (node == null || node.Coordinate == null)
        {
            throw new InvalidGeometryException("Segment node or coordinate is null.");
        }
        int index = 0;
        while (index < Nodes.Count)
        {
            SegmentNode existing = Nodes[index];
            if (existing.SegmentIndex == node.SegmentIndex &&
                existing.SegmentFraction == node.SegmentFraction)
            {
                return;
            }
            index++;
        }
        Nodes.Add(node);
    }

    internal SegmentNode[] GetSortedNodes()
    {
        Nodes.Sort((first, second) => CompareNodePositions(first, second));
        return Nodes.ToArray();
    }

    internal static SegmentNode CreateNode(
        Coordinate coordinate, int segmentIndex, double segmentFraction)
    {
        SegmentNode node = new SegmentNode();
        node.Coordinate = RobustLineIntersection.CopyCoordinate(coordinate);
        node.SegmentIndex = segmentIndex;
        node.SegmentFraction = segmentFraction;
        return node;
    }

    internal static int CompareNodePositions(SegmentNode first, SegmentNode second)
    {
        if (first.SegmentIndex < second.SegmentIndex) return -1;
        if (first.SegmentIndex > second.SegmentIndex) return 1;
        if (first.SegmentFraction < second.SegmentFraction) return -1;
        if (first.SegmentFraction > second.SegmentFraction) return 1;
        return RobustLineIntersection.ComparePlanarCoordinates(
            first.Coordinate, second.Coordinate);
    }
}
