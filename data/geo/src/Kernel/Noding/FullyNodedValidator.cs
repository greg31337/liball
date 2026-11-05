namespace geo;

internal static class FullyNodedValidator
{
    internal static NodingResult ValidateOutput(SegmentString[] segmentStrings)
    {
        return IndexedNoder.NodeSelf(segmentStrings, true);
    }

    internal static void EnsureFullyNoded(SegmentString[] segmentStrings)
    {
        NodingResult validation = ValidateOutput(segmentStrings);
        if (!validation.IsValid)
        {
            string message = "Noded output contains a segment-interior intersection.";
            Coordinate? coordinate = validation.FirstNonNodedIntersection == null ? null :
                validation.FirstNonNodedIntersection.FirstIntersection;
            if (coordinate != null)
            {
                message = $"Noded output contains a segment-interior intersection at " +
                    $"({coordinate.X}, {coordinate.Y}).";
            }
            throw new TopologyException(message);
        }
    }
}
