using System.Collections.Generic;

namespace geo;

internal class PolygonValidityRing
{
    internal Coordinate[] Coordinates = [];
    internal string Path = "";
    internal int Index;
}

internal class PolygonValidityTouch
{
    internal int FirstRing;
    internal int SecondRing;
    internal Coordinate Coordinate = new Coordinate();
    internal string Path = "";
}

internal class PolygonValidityState
{
    internal List<PolygonValidityTouch> Touches =
        new List<PolygonValidityTouch>();
    internal ValidationResult? Failure;
}

internal static class GeometryValidityChecker
{
    internal static ValidationResult ValidateGeometry(Geometry? geometry)
    {
        ValidationResult structure =
            StructuralValidator.ValidateGeometry(geometry);
        if (!structure.IsValid || geometry == null) return structure;
        return ValidateGeometryPayload(
            geometry, geometry.GeometryType.ToString());
    }

    internal static ValidationResult ValidateGeometryPayload(
        Geometry geometry, string path)
    {
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return new ValidationResult();
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)
                    GeometryDispatch.GetSelectedPayload(geometry);
                return ValidateLinearRing(ring, path);
            case GeometryType.Polygon:
                Polygon polygon = (Polygon)
                    GeometryDispatch.GetSelectedPayload(geometry);
                return ValidatePolygon(polygon, path);
            case GeometryType.MultiPolygon:
                return ExtendedGeometryValidityChecker.ValidateMultiPolygon(
                    (MultiPolygon)GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            case GeometryType.GeometryCollection:
                return ExtendedGeometryValidityChecker.ValidateCollection(
                    (GeometryCollection)GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            case GeometryType.Triangle:
                return ExtendedGeometryValidityChecker.ValidateTriangle(
                    (Triangle)GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            case GeometryType.Tin:
                return ExtendedGeometryValidityChecker.ValidateTin(
                    (Tin)GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            case GeometryType.PolyhedralSurface:
                return ExtendedGeometryValidityChecker.ValidatePolyhedralSurface(
                    (PolyhedralSurface)
                        GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            case GeometryType.CircularString:
                return new ValidationResult();
            case GeometryType.CompoundCurve:
                return ExtendedGeometryValidityChecker.ValidateCompoundCurve(
                    (CompoundCurve)GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            case GeometryType.CurvePolygon:
                return ExtendedGeometryValidityChecker.ValidateCurvePolygon(
                    (CurvePolygon)GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            case GeometryType.MultiCurve:
                return ExtendedGeometryValidityChecker.ValidateMultiCurve(
                    (MultiCurve)GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            case GeometryType.MultiSurface:
                return ExtendedGeometryValidityChecker.ValidateMultiSurface(
                    (MultiSurface)GeometryDispatch.GetSelectedPayload(geometry),
                    path);
            default:
                return StructuralValidator.CreateFailureResult(
                    ValidationError.InvalidGeometryType,
                    "Topological validity checking does not yet support this geometry type.",
                    null, geometry.GeometryType.ToString());
        }
    }

    internal static ValidationResult ValidateLinearRing(
        LinearRing ring, string path)
    {
        if (ring == null)
        {
            return StructuralValidator.CreateFailureResult(
                ValidationError.NullGeometry,
                "LinearRing payload is null.", null, path);
        }
        PolygonValidityRing validityRing = CreateRing(
            ring.Coordinates, path, 0);
        ValidationResult? failure = CheckRingSelfTopology(validityRing);
        return failure == null ? new ValidationResult() : failure;
    }

    internal static ValidationResult ValidatePolygon(
        Polygon polygon, string path)
    {
        if (polygon == null)
        {
            return StructuralValidator.CreateFailureResult(
                ValidationError.NullGeometry,
                "Polygon payload is null.", null, path);
        }
        PolygonValidityRing[] rings = CreatePolygonRings(polygon, path);
        int index = 0;
        while (index < rings.Length)
        {
            ValidationResult? selfFailure =
                CheckRingSelfTopology(rings[index]);
            if (selfFailure != null) return selfFailure;
            index++;
        }
        ValidationResult? duplicateFailure = CheckDuplicateRings(rings);
        if (duplicateFailure != null) return duplicateFailure;
        PolygonValidityState topology = AnalyzeMutualTopology(rings);
        if (topology.Failure != null) return topology.Failure;
        ValidationResult? shellFailure = CheckHolesInShell(rings);
        if (shellFailure != null) return shellFailure;
        ValidationResult? nestingFailure = CheckHolesNotNested(rings);
        if (nestingFailure != null) return nestingFailure;
        ValidationResult? connectionFailure =
            CheckInteriorConnected(rings, topology.Touches);
        return connectionFailure == null ?
            new ValidationResult() : connectionFailure;
    }

    internal static PolygonValidityRing[] CreatePolygonRings(
        Polygon polygon, string path)
    {
        PolygonValidityRing[] rings =
            new PolygonValidityRing[polygon.Holes.Length + 1];
        rings[0] = CreateRing(
            polygon.Shell.Coordinates, path + "/Shell", 0);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            rings[index + 1] = CreateRing(
                polygon.Holes[index].Coordinates,
                path + "/Holes[" + index + "]", index + 1);
            index++;
        }
        return rings;
    }

    internal static PolygonValidityRing CreateRing(
        Coordinate[] coordinates, string path, int index)
    {
        PolygonValidityRing ring = new PolygonValidityRing();
        ring.Coordinates = DouglasPeucker.RemoveAdjacentDuplicates(
            coordinates, true);
        ring.Path = path;
        ring.Index = index;
        return ring;
    }

    internal static ValidationResult? CheckRingSelfTopology(
        PolygonValidityRing ring)
    {
        if (ring.Coordinates.Length == 0) return null;
        SegmentString segmentString = SegmentNoding.CreateSegmentString(
            ring.Coordinates, ring, ring.Index);
        SweepLineIndex sweep = new SweepLineIndex([segmentString]);
        SegmentPairCandidate[] candidates =
            sweep.GetCandidatePairs(true);
        int index = 0;
        while (index < candidates.Length)
        {
            SegmentPairCandidate candidate = candidates[index];
            SegmentIntersectionResult intersection =
                GetCandidateIntersection(candidate);
            if (intersection.Type != SegmentIntersectionType.None &&
                !AdjacentRingIntersectionIsAllowed(
                    candidate, intersection))
            {
                ValidationError error = ClassifyRingIntersection(
                    candidate, intersection);
                Coordinate? witness = intersection.FirstIntersection;
                return CreateTopologyFailure(
                    error, RingFailureMessage(error), witness,
                    ring.Path, candidate.SecondSegmentIndex);
            }
            index++;
        }
        return null;
    }

    internal static bool AdjacentRingIntersectionIsAllowed(
        SegmentPairCandidate candidate,
        SegmentIntersectionResult intersection)
    {
        if (intersection.Type ==
            SegmentIntersectionType.CollinearOverlap)
        {
            return false;
        }
        int first = candidate.FirstSegmentIndex;
        int second = candidate.SecondSegmentIndex;
        int difference = first - second;
        if (difference < 0) difference = -difference;
        if (difference == 1) return true;
        int lastSegment = candidate.First.Coordinates.Length - 2;
        return first == 0 && second == lastSegment ||
            second == 0 && first == lastSegment;
    }

    internal static ValidationError ClassifyRingIntersection(
        SegmentPairCandidate candidate,
        SegmentIntersectionResult intersection)
    {
        if (intersection.Type == SegmentIntersectionType.ProperPoint)
        {
            return ValidationError.SelfIntersection;
        }
        if (intersection.Type ==
            SegmentIntersectionType.CollinearOverlap)
        {
            return ValidationError.SelfIntersection;
        }
        Coordinate? witness = intersection.FirstIntersection;
        if (witness != null &&
            IsCandidateNode(witness, candidate))
        {
            return ValidationError.RingSelfTouch;
        }
        return ValidationError.NonNodedIntersection;
    }

    internal static ValidationResult? CheckDuplicateRings(
        PolygonValidityRing[] rings)
    {
        int first = 0;
        while (first < rings.Length)
        {
            int second = first + 1;
            while (second < rings.Length)
            {
                if (RingsAreEqual(
                    rings[first].Coordinates,
                    rings[second].Coordinates))
                {
                    Coordinate? witness = rings[second].Coordinates.Length == 0 ?
                        null : rings[second].Coordinates[0];
                    return StructuralValidator.CreateFailureResult(
                        ValidationError.DuplicateRings,
                        "Polygon contains duplicate rings.",
                        witness, rings[second].Path);
                }
                second++;
            }
            first++;
        }
        return null;
    }

    internal static bool RingsAreEqual(
        Coordinate[] first, Coordinate[] second)
    {
        if (first.Length == 0 || second.Length == 0) return false;
        int firstCount = first.Length - 1;
        int secondCount = second.Length - 1;
        if (firstCount != secondCount) return false;
        int start = 0;
        while (start < secondCount)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                first[0], second[start]))
            {
                if (RingSequenceMatches(
                    first, second, firstCount, start, 1)) return true;
                if (RingSequenceMatches(
                    first, second, firstCount, start, -1)) return true;
            }
            start++;
        }
        return false;
    }

    internal static bool RingSequenceMatches(
        Coordinate[] first, Coordinate[] second,
        int count, int start, int direction)
    {
        int index = 0;
        while (index < count)
        {
            int secondIndex = start + direction * index;
            while (secondIndex < 0) secondIndex += count;
            while (secondIndex >= count) secondIndex -= count;
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                first[index], second[secondIndex])) return false;
            index++;
        }
        return true;
    }

    internal static PolygonValidityState AnalyzeMutualTopology(
        PolygonValidityRing[] rings)
    {
        PolygonValidityState state = new PolygonValidityState();
        List<SegmentString> strings = new List<SegmentString>();
        int index = 0;
        while (index < rings.Length)
        {
            if (rings[index].Coordinates.Length >= 2)
            {
                strings.Add(SegmentNoding.CreateSegmentString(
                    rings[index].Coordinates, rings[index],
                    rings[index].Index));
            }
            index++;
        }
        if (strings.Count < 2) return state;
        SweepLineIndex sweep = new SweepLineIndex(strings.ToArray());
        SegmentPairCandidate[] candidates =
            sweep.GetCandidatePairs(false);
        index = 0;
        while (index < candidates.Length)
        {
            SegmentPairCandidate candidate = candidates[index];
            SegmentIntersectionResult intersection =
                GetCandidateIntersection(candidate);
            if (intersection.Type != SegmentIntersectionType.None)
            {
                AnalyzeMutualIntersection(
                    candidate, intersection, state);
                if (state.Failure != null) return state;
            }
            index++;
        }
        return state;
    }

    internal static void AnalyzeMutualIntersection(
        SegmentPairCandidate candidate,
        SegmentIntersectionResult intersection,
        PolygonValidityState state)
    {
        PolygonValidityRing first = GetValidityRing(candidate.First);
        PolygonValidityRing second = GetValidityRing(candidate.Second);
        if (intersection.Type ==
            SegmentIntersectionType.CollinearOverlap)
        {
            state.Failure = CreateTopologyFailure(
                ValidationError.NonNodedIntersection,
                "Polygon rings overlap along a segment.",
                intersection.FirstIntersection,
                second.Path, candidate.SecondSegmentIndex);
            return;
        }
        if (intersection.Type == SegmentIntersectionType.ProperPoint)
        {
            state.Failure = CreateTopologyFailure(
                ValidationError.NonNodedIntersection,
                "Polygon rings contain a proper crossing.",
                intersection.FirstIntersection,
                second.Path, candidate.SecondSegmentIndex);
            return;
        }
        AnalyzeMutualWitness(
            intersection.FirstIntersection, candidate,
            first, second, state);
        if (state.Failure != null) return;
        AnalyzeMutualWitness(
            intersection.SecondIntersection, candidate,
            first, second, state);
    }

    internal static void AnalyzeMutualWitness(
        Coordinate? witness, SegmentPairCandidate candidate,
        PolygonValidityRing first, PolygonValidityRing second,
        PolygonValidityState state)
    {
        if (witness == null) return;
        if (RingsCrossAtWitness(witness, first, second))
        {
            state.Failure = CreateTopologyFailure(
                ValidationError.NonNodedIntersection,
                "Polygon rings cross at a shared node.",
                witness, second.Path,
                candidate.SecondSegmentIndex);
            return;
        }
        AddRingTouch(first, second, witness, state);
    }

    internal static bool RingsCrossAtWitness(
        Coordinate witness, PolygonValidityRing first,
        PolygonValidityRing second)
    {
        if (RingPassesAcross(
            witness, first.Coordinates, second.Coordinates))
        {
            return true;
        }
        return RingPassesAcross(
            witness, second.Coordinates, first.Coordinates);
    }

    internal static bool RingPassesAcross(
        Coordinate witness, Coordinate[] test,
        Coordinate[] target)
    {
        int count = test.Length - 1;
        int index = 0;
        while (index < count)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                witness, test[index]))
            {
                int previous = index == 0 ? count - 1 : index - 1;
                int next = index + 1 == count ? 0 : index + 1;
                TopologicalLocation previousLocation =
                    RingPointLocation.LocatePointByRayCrossing(
                        test[previous], target);
                TopologicalLocation nextLocation =
                    RingPointLocation.LocatePointByRayCrossing(
                        test[next], target);
                if (previousLocation == TopologicalLocation.Interior &&
                    nextLocation == TopologicalLocation.Exterior ||
                    previousLocation == TopologicalLocation.Exterior &&
                    nextLocation == TopologicalLocation.Interior)
                {
                    return true;
                }
            }
            index++;
        }
        return false;
    }

    internal static void AddRingTouch(
        PolygonValidityRing first, PolygonValidityRing second,
        Coordinate witness, PolygonValidityState state)
    {
        int low = first.Index < second.Index ?
            first.Index : second.Index;
        int high = first.Index < second.Index ?
            second.Index : first.Index;
        int index = 0;
        while (index < state.Touches.Count)
        {
            PolygonValidityTouch existing = state.Touches[index];
            if (existing.FirstRing == low &&
                existing.SecondRing == high)
            {
                if (RobustLineIntersection.CoordinatesEqualPlanar(
                    existing.Coordinate, witness)) return;
            }
            index++;
        }
        PolygonValidityTouch touch = new PolygonValidityTouch();
        touch.FirstRing = low;
        touch.SecondRing = high;
        touch.Coordinate = RobustLineIntersection.CopyCoordinate(witness);
        touch.Path = second.Path;
        state.Touches.Add(touch);
    }

    internal static ValidationResult? CheckHolesInShell(
        PolygonValidityRing[] rings)
    {
        PolygonValidityRing shell = rings[0];
        int index = 1;
        while (index < rings.Length)
        {
            PolygonValidityRing hole = rings[index];
            if (hole.Coordinates.Length != 0)
            {
                Coordinate? witness = FindNonBoundaryCoordinate(
                    hole.Coordinates, shell.Coordinates);
                if (witness == null ||
                    RingPointLocation.LocatePointByRayCrossing(
                        witness, shell.Coordinates) ==
                    TopologicalLocation.Exterior)
                {
                    Coordinate? failureCoordinate = witness == null ?
                        hole.Coordinates[0] : witness;
                    return StructuralValidator.CreateFailureResult(
                        ValidationError.HoleOutsideShell,
                        "Polygon hole lies outside its shell.",
                        failureCoordinate, hole.Path);
                }
            }
            index++;
        }
        return null;
    }

    internal static ValidationResult? CheckHolesNotNested(
        PolygonValidityRing[] rings)
    {
        int first = 1;
        while (first < rings.Length)
        {
            int second = first + 1;
            while (second < rings.Length)
            {
                ValidationResult? nested = CheckHolePairNested(
                    rings[first], rings[second]);
                if (nested != null) return nested;
                second++;
            }
            first++;
        }
        return null;
    }

    internal static ValidationResult? CheckHolePairNested(
        PolygonValidityRing first, PolygonValidityRing second)
    {
        if (first.Coordinates.Length == 0 ||
            second.Coordinates.Length == 0) return null;
        Coordinate? firstWitness = FindNonBoundaryCoordinate(
            first.Coordinates, second.Coordinates);
        if (firstWitness != null &&
            RingPointLocation.LocatePointByRayCrossing(
                firstWitness, second.Coordinates) ==
            TopologicalLocation.Interior)
        {
            return StructuralValidator.CreateFailureResult(
                ValidationError.NestedHoles,
                "Polygon hole is nested inside another hole.",
                firstWitness, first.Path);
        }
        Coordinate? secondWitness = FindNonBoundaryCoordinate(
            second.Coordinates, first.Coordinates);
        if (secondWitness != null &&
            RingPointLocation.LocatePointByRayCrossing(
                secondWitness, first.Coordinates) ==
            TopologicalLocation.Interior)
        {
            return StructuralValidator.CreateFailureResult(
                ValidationError.NestedHoles,
                "Polygon hole is nested inside another hole.",
                secondWitness, second.Path);
        }
        return null;
    }

    internal static Coordinate? FindNonBoundaryCoordinate(
        Coordinate[] test, Coordinate[] target)
    {
        int count = test.Length;
        if (count > 1 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                test[0], test[count - 1])) count--;
        int index = 0;
        while (index < count)
        {
            TopologicalLocation location =
                RingPointLocation.LocatePointByRayCrossing(
                    test[index], target);
            if (location != TopologicalLocation.Boundary)
            {
                return test[index];
            }
            index++;
        }
        return null;
    }

    internal static ValidationResult? CheckInteriorConnected(
        PolygonValidityRing[] rings,
        List<PolygonValidityTouch> touches)
    {
        int[] parents = new int[rings.Length];
        int index = 0;
        while (index < parents.Length)
        {
            parents[index] = index;
            index++;
        }
        index = 0;
        while (index < touches.Count)
        {
            PolygonValidityTouch touch = touches[index];
            int firstRoot = FindTouchRoot(parents, touch.FirstRing);
            int secondRoot = FindTouchRoot(parents, touch.SecondRing);
            if (firstRoot == secondRoot)
            {
                return StructuralValidator.CreateFailureResult(
                    ValidationError.DisconnectedInterior,
                    "A cycle of touching rings disconnects the polygon interior.",
                    touch.Coordinate, touch.Path);
            }
            parents[secondRoot] = firstRoot;
            index++;
        }
        return null;
    }

    internal static int FindTouchRoot(int[] parents, int index)
    {
        int root = index;
        while (parents[root] != root) root = parents[root];
        int current = index;
        while (parents[current] != current)
        {
            int next = parents[current];
            parents[current] = root;
            current = next;
        }
        return root;
    }

    internal static SegmentIntersectionResult GetCandidateIntersection(
        SegmentPairCandidate candidate)
    {
        return RobustLineIntersection.GetSegmentIntersection(
            candidate.First.Coordinates[
                candidate.FirstSegmentIndex],
            candidate.First.Coordinates[
                candidate.FirstSegmentIndex + 1],
            candidate.Second.Coordinates[
                candidate.SecondSegmentIndex],
            candidate.Second.Coordinates[
                candidate.SecondSegmentIndex + 1]);
    }

    internal static bool IsCandidateNode(
        Coordinate witness, SegmentPairCandidate candidate)
    {
        bool firstNode = IsSegmentEndpoint(
            witness, candidate.First.Coordinates,
            candidate.FirstSegmentIndex);
        bool secondNode = IsSegmentEndpoint(
            witness, candidate.Second.Coordinates,
            candidate.SecondSegmentIndex);
        return firstNode && secondNode;
    }

    internal static bool IsSegmentEndpoint(
        Coordinate witness, Coordinate[] coordinates,
        int segmentIndex)
    {
        return RobustLineIntersection.CoordinatesEqualPlanar(
            witness, coordinates[segmentIndex]) ||
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, coordinates[segmentIndex + 1]);
    }

    internal static PolygonValidityRing GetValidityRing(
        SegmentString segmentString)
    {
        object? metadata = segmentString.SourceMetadata;
        if (metadata == null)
        {
            throw new InvalidGeometryException(
                "Polygon validity segment metadata is null.");
        }
        return (PolygonValidityRing)metadata;
    }

    internal static ValidationResult CreateTopologyFailure(
        ValidationError error, string message,
        Coordinate? coordinate, string path, int segmentIndex)
    {
        return StructuralValidator.CreateFailureResult(
            error, message, coordinate,
            path + "/Segments[" + segmentIndex + "]");
    }

    internal static string RingFailureMessage(ValidationError error)
    {
        if (error == ValidationError.RingSelfTouch)
        {
            return "Ring touches itself at a non-adjacent vertex.";
        }
        if (error == ValidationError.NonNodedIntersection)
        {
            return "Ring contains a segment-interior vertex intersection.";
        }
        return "Ring contains a self-intersection.";
    }
}
