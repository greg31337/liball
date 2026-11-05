namespace geo;

internal static class RingPointLocation
{
    internal static TopologicalLocation LocatePointByRayCrossing(
        Coordinate point, Coordinate[] ring)
    {
        ValidateInputs(point, ring);
        if (ring.Length == 0) return TopologicalLocation.Exterior;
        bool inside = false;
        Coordinate previous = ring[ring.Length - 1];
        int index = 0;
        while (index < ring.Length)
        {
            Coordinate current = ring[index];
            TopologicalLocation edgeLocation =
                SegmentDistanceAlgorithms.LocatePointOnSegment(
                    point, previous, current);
            if (edgeLocation != TopologicalLocation.Exterior)
            {
                return TopologicalLocation.Boundary;
            }
            int orientation = RobustOrientation.GetOrientationIndex(
                previous, current, point);
            if (previous.Y <= point.Y)
            {
                if (current.Y > point.Y && orientation > 0) inside = !inside;
            }
            else
            {
                if (current.Y <= point.Y && orientation < 0) inside = !inside;
            }
            previous = current;
            index++;
        }
        return inside ? TopologicalLocation.Interior : TopologicalLocation.Exterior;
    }

    internal static TopologicalLocation LocatePointByWinding(
        Coordinate point, Coordinate[] ring)
    {
        ValidateInputs(point, ring);
        if (ring.Length == 0) return TopologicalLocation.Exterior;
        int winding = 0;
        Coordinate previous = ring[ring.Length - 1];
        int index = 0;
        while (index < ring.Length)
        {
            Coordinate current = ring[index];
            TopologicalLocation edgeLocation =
                SegmentDistanceAlgorithms.LocatePointOnSegment(
                    point, previous, current);
            if (edgeLocation != TopologicalLocation.Exterior)
            {
                return TopologicalLocation.Boundary;
            }
            int orientation = RobustOrientation.GetOrientationIndex(
                previous, current, point);
            if (previous.Y <= point.Y)
            {
                if (current.Y > point.Y && orientation > 0) winding++;
            }
            else
            {
                if (current.Y <= point.Y && orientation < 0) winding--;
            }
            previous = current;
            index++;
        }
        return winding == 0 ? TopologicalLocation.Exterior : TopologicalLocation.Interior;
    }

    internal static void ValidateInputs(Coordinate point, Coordinate[] ring)
    {
        VectorMath.ValidateCoordinateValue(point, "Point being located");
        RingMath.ValidateRingCoordinates(ring);
    }
}
