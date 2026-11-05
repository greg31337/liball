namespace geo;

internal class TopologyLabel
{
    internal TopologicalLocation[] On =
        [TopologicalLocation.Exterior, TopologicalLocation.Exterior];
    internal TopologicalLocation[] Left =
        [TopologicalLocation.Exterior, TopologicalLocation.Exterior];
    internal TopologicalLocation[] Right =
        [TopologicalLocation.Exterior, TopologicalLocation.Exterior];
    internal bool[] HasSides = [false, false];
    internal bool[] IsSet = [false, false];
    internal int[] DepthDelta = [0, 0];

    internal void SetLineLocation(
        int geometryIndex, TopologicalLocation onLocation)
    {
        ValidateGeometryIndex(geometryIndex);
        On[geometryIndex] = onLocation;
        HasSides[geometryIndex] = false;
        IsSet[geometryIndex] = true;
        DepthDelta[geometryIndex] = 0;
    }

    internal void SetAreaLocations(
        int geometryIndex, TopologicalLocation onLocation,
        TopologicalLocation leftLocation, TopologicalLocation rightLocation)
    {
        ValidateGeometryIndex(geometryIndex);
        On[geometryIndex] = onLocation;
        Left[geometryIndex] = leftLocation;
        Right[geometryIndex] = rightLocation;
        HasSides[geometryIndex] = true;
        IsSet[geometryIndex] = true;
        DepthDelta[geometryIndex] = CalculateDepthDelta(
            leftLocation, rightLocation);
    }

    internal TopologicalLocation GetOnLocation(int geometryIndex)
    {
        ValidateGeometryIndex(geometryIndex);
        return On[geometryIndex];
    }

    internal TopologicalLocation GetLeftLocation(
        int geometryIndex, bool forward)
    {
        ValidateGeometryIndex(geometryIndex);
        return forward ? Left[geometryIndex] : Right[geometryIndex];
    }

    internal TopologicalLocation GetRightLocation(
        int geometryIndex, bool forward)
    {
        ValidateGeometryIndex(geometryIndex);
        return forward ? Right[geometryIndex] : Left[geometryIndex];
    }

    internal int GetDepthDelta(int geometryIndex, bool forward)
    {
        ValidateGeometryIndex(geometryIndex);
        return forward ? DepthDelta[geometryIndex] : -DepthDelta[geometryIndex];
    }

    internal TopologyLabel CopyLabel()
    {
        TopologyLabel copy = new TopologyLabel();
        int index = 0;
        while (index < 2)
        {
            copy.On[index] = On[index];
            copy.Left[index] = Left[index];
            copy.Right[index] = Right[index];
            copy.HasSides[index] = HasSides[index];
            copy.IsSet[index] = IsSet[index];
            copy.DepthDelta[index] = DepthDelta[index];
            index++;
        }
        return copy;
    }

    internal TopologyLabel CopyForDirection(bool forward)
    {
        TopologyLabel copy = CopyLabel();
        if (forward) return copy;
        int index = 0;
        while (index < 2)
        {
            TopologicalLocation left = copy.Left[index];
            copy.Left[index] = copy.Right[index];
            copy.Right[index] = left;
            copy.DepthDelta[index] = -copy.DepthDelta[index];
            index++;
        }
        return copy;
    }

    internal void MergeLabel(TopologyLabel contribution)
    {
        if (contribution == null)
        {
            throw new InvalidGeometryException("Merged topology label is null.");
        }
        int index = 0;
        while (index < 2)
        {
            if (contribution.IsSet[index])
            {
                if (!IsSet[index])
                {
                    On[index] = contribution.On[index];
                    Left[index] = contribution.Left[index];
                    Right[index] = contribution.Right[index];
                    HasSides[index] = contribution.HasSides[index];
                    IsSet[index] = true;
                    DepthDelta[index] = contribution.DepthDelta[index];
                }
                else
                {
                    On[index] = MergeLocation(
                        On[index], contribution.On[index]);
                    if (contribution.HasSides[index])
                    {
                        if (!HasSides[index])
                        {
                            Left[index] = contribution.Left[index];
                            Right[index] = contribution.Right[index];
                            HasSides[index] = true;
                        }
                        else
                        {
                            Left[index] = MergeLocation(
                                Left[index], contribution.Left[index]);
                            Right[index] = MergeLocation(
                                Right[index], contribution.Right[index]);
                        }
                    }
                    DepthDelta[index] += contribution.DepthDelta[index];
                }
            }
            index++;
        }
    }

    internal static TopologicalLocation MergeLocation(
        TopologicalLocation first, TopologicalLocation second)
    {
        if (first == TopologicalLocation.Boundary ||
            second == TopologicalLocation.Boundary)
        {
            return TopologicalLocation.Boundary;
        }
        if (first == TopologicalLocation.Interior ||
            second == TopologicalLocation.Interior)
        {
            return TopologicalLocation.Interior;
        }
        return TopologicalLocation.Exterior;
    }

    internal static int CalculateDepthDelta(
        TopologicalLocation left, TopologicalLocation right)
    {
        if (left == TopologicalLocation.Interior &&
            right == TopologicalLocation.Exterior) return 1;
        if (left == TopologicalLocation.Exterior &&
            right == TopologicalLocation.Interior) return -1;
        return 0;
    }

    internal static void ValidateGeometryIndex(int geometryIndex)
    {
        if (geometryIndex < 0 || geometryIndex > 1)
        {
            throw new InvalidGeometryException(
                "Topology label geometry index must be zero or one.");
        }
    }
}
