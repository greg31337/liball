using System.Collections.Generic;

namespace geo;

internal class NodingResult
{
    internal bool ValidationOnly;
    internal bool IsValid = true;
    internal bool HasProperIntersection;
    internal bool HasCollinearOverlap;
    internal int IntersectionCount;
    internal SegmentIntersectionResult? FirstIntersection;
    internal SegmentIntersectionResult? FirstNonNodedIntersection;
    internal SegmentString[] First = [];
    internal SegmentString[] Second = [];
}

internal static class IndexedNoder
{
    internal static NodingResult NodeMutual(
        SegmentString[] first, SegmentString[] second, bool validationOnly)
    {
        SegmentString[] firstWorking = CloneSegmentStrings(first);
        SegmentString[] secondWorking = CloneSegmentStrings(second);
        SegmentString[] combined = CombineGroups(firstWorking, secondWorking);
        NodingResult result = new NodingResult();
        result.ValidationOnly = validationOnly;
        ProcessCandidates(
            combined, firstWorking.Length, true, validationOnly, result);
        result.First = validationOnly ? firstWorking :
            SplitGroup(firstWorking, 0, firstWorking.Length);
        result.Second = validationOnly ? secondWorking :
            SplitGroup(secondWorking, 0, secondWorking.Length);
        if (!validationOnly) result.IsValid = true;
        return result;
    }

    internal static NodingResult NodeSelf(
        SegmentString[] segmentStrings, bool validationOnly)
    {
        SegmentString[] working = CloneSegmentStrings(segmentStrings);
        NodingResult result = new NodingResult();
        result.ValidationOnly = validationOnly;
        ProcessCandidates(working, working.Length, false, validationOnly, result);
        result.First = validationOnly ? working : SplitGroup(working, 0, working.Length);
        if (!validationOnly) result.IsValid = true;
        return result;
    }

    internal static void ProcessCandidates(
        SegmentString[] working, int firstGroupLength,
        bool mutual, bool validationOnly, NodingResult result)
    {
        SweepLineIndex index = new SweepLineIndex(working);
        SegmentPairCandidate[] candidates = index.GetCandidatePairs(!mutual);
        int candidateIndex = 0;
        while (candidateIndex < candidates.Length)
        {
            SegmentPairCandidate candidate = candidates[candidateIndex];
            bool process = true;
            if (mutual)
            {
                bool firstInFirstGroup = IsInPrefix(
                    working, candidate.First, firstGroupLength);
                bool secondInFirstGroup = IsInPrefix(
                    working, candidate.Second, firstGroupLength);
                if (firstInFirstGroup == secondInFirstGroup) process = false;
            }
            else if (ShouldSkipSelfPair(candidate))
            {
                process = false;
            }
            if (process)
            {
                ProcessCandidate(candidate, validationOnly, result);
            }
            candidateIndex++;
        }
    }

    internal static void ProcessCandidate(
        SegmentPairCandidate candidate, bool validationOnly, NodingResult nodingResult)
    {
        Coordinate firstStart = candidate.First.Coordinates[candidate.FirstSegmentIndex];
        Coordinate firstEnd = candidate.First.Coordinates[candidate.FirstSegmentIndex + 1];
        Coordinate secondStart = candidate.Second.Coordinates[candidate.SecondSegmentIndex];
        Coordinate secondEnd = candidate.Second.Coordinates[candidate.SecondSegmentIndex + 1];
        SegmentIntersectionResult intersection = RobustLineIntersection.GetSegmentIntersection(
            firstStart, firstEnd, secondStart, secondEnd);
        if (intersection.Type == SegmentIntersectionType.None) return;
        nodingResult.IntersectionCount++;
        if (nodingResult.FirstIntersection == null)
        {
            nodingResult.FirstIntersection = intersection;
        }
        if (intersection.Type == SegmentIntersectionType.ProperPoint)
        {
            nodingResult.HasProperIntersection = true;
        }
        if (intersection.Type == SegmentIntersectionType.CollinearOverlap)
        {
            nodingResult.HasCollinearOverlap = true;
        }
        bool requiresNoding = IntersectionRequiresNoding(
            intersection, firstStart, firstEnd, secondStart, secondEnd);
        if (requiresNoding)
        {
            nodingResult.IsValid = false;
            if (nodingResult.FirstNonNodedIntersection == null)
            {
                nodingResult.FirstNonNodedIntersection = intersection;
            }
        }
        if (!validationOnly)
        {
            InsertIntersectionResult(candidate, intersection);
        }
    }

    internal static void InsertIntersectionResult(
        SegmentPairCandidate candidate, SegmentIntersectionResult intersection)
    {
        if (intersection.FirstIntersection != null)
        {
            SegmentNoding.AddIntersectionNode(
                candidate.First, intersection.FirstIntersection,
                candidate.FirstSegmentIndex);
            SegmentNoding.AddIntersectionNode(
                candidate.Second, intersection.FirstIntersection,
                candidate.SecondSegmentIndex);
        }
        if (intersection.SecondIntersection != null)
        {
            SegmentNoding.AddIntersectionNode(
                candidate.First, intersection.SecondIntersection,
                candidate.FirstSegmentIndex);
            SegmentNoding.AddIntersectionNode(
                candidate.Second, intersection.SecondIntersection,
                candidate.SecondSegmentIndex);
        }
    }

    internal static bool IntersectionRequiresNoding(
        SegmentIntersectionResult intersection,
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        if (intersection.Type == SegmentIntersectionType.ProperPoint) return true;
        if (WitnessRequiresNoding(
            intersection.FirstIntersection, firstStart, firstEnd,
            secondStart, secondEnd)) return true;
        return WitnessRequiresNoding(
            intersection.SecondIntersection, firstStart, firstEnd,
            secondStart, secondEnd);
    }

    internal static bool WitnessRequiresNoding(
        Coordinate? witness,
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        if (witness == null) return false;
        bool firstEndpoint = RobustLineIntersection.CoordinatesEqualPlanar(
            witness, firstStart) || RobustLineIntersection.CoordinatesEqualPlanar(
            witness, firstEnd);
        bool secondEndpoint = RobustLineIntersection.CoordinatesEqualPlanar(
            witness, secondStart) || RobustLineIntersection.CoordinatesEqualPlanar(
            witness, secondEnd);
        return !firstEndpoint || !secondEndpoint;
    }

    internal static bool ShouldSkipSelfPair(SegmentPairCandidate candidate)
    {
        if (candidate.First != candidate.Second) return false;
        int difference = candidate.FirstSegmentIndex - candidate.SecondSegmentIndex;
        if (difference < 0) difference = -difference;
        SegmentString source = candidate.First;
        bool adjacent = difference <= 1;
        if (!adjacent)
        {
            bool closed = RobustLineIntersection.CoordinatesEqualPlanar(
                source.Coordinates[0], source.Coordinates[source.Coordinates.Length - 1]);
            int lastSegment = source.Coordinates.Length - 2;
            adjacent = closed && (candidate.FirstSegmentIndex == 0 &&
                candidate.SecondSegmentIndex == lastSegment ||
                candidate.SecondSegmentIndex == 0 &&
                candidate.FirstSegmentIndex == lastSegment);
        }
        if (!adjacent) return false;
        SegmentIntersectionResult intersection = RobustLineIntersection.GetSegmentIntersection(
            source.Coordinates[candidate.FirstSegmentIndex],
            source.Coordinates[candidate.FirstSegmentIndex + 1],
            source.Coordinates[candidate.SecondSegmentIndex],
            source.Coordinates[candidate.SecondSegmentIndex + 1]);
        return intersection.Type != SegmentIntersectionType.CollinearOverlap;
    }

    internal static bool IsInPrefix(
        SegmentString[] segmentStrings, SegmentString target, int prefixLength)
    {
        int index = 0;
        while (index < prefixLength)
        {
            if (segmentStrings[index] == target) return true;
            index++;
        }
        return false;
    }

    internal static SegmentString[] CloneSegmentStrings(SegmentString[] segmentStrings)
    {
        if (segmentStrings == null)
        {
            throw new InvalidGeometryException("Noder segment string array is null.");
        }
        SegmentString[] copy = new SegmentString[segmentStrings.Length];
        int index = 0;
        while (index < segmentStrings.Length)
        {
            SegmentString source = segmentStrings[index];
            if (source == null)
            {
                throw new InvalidGeometryException("Noder segment string is null.");
            }
            SegmentString item = new SegmentString(
                source.Coordinates, source.SourceMetadata, source.SourceIndex);
            item.SourceStartSegment = source.SourceStartSegment;
            item.SourceEndSegment = source.SourceEndSegment;
            copy[index] = item;
            index++;
        }
        return copy;
    }

    internal static SegmentString[] CombineGroups(
        SegmentString[] first, SegmentString[] second)
    {
        SegmentString[] combined = new SegmentString[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    internal static SegmentString[] SplitGroup(
        SegmentString[] segmentStrings, int startIndex, int count)
    {
        List<SegmentString> output = new List<SegmentString>();
        int index = startIndex;
        while (index < startIndex + count)
        {
            SegmentString[] splits = SegmentNoding.SplitEdges(segmentStrings[index]);
            int splitIndex = 0;
            while (splitIndex < splits.Length)
            {
                output.Add(splits[splitIndex]);
                splitIndex++;
            }
            index++;
        }
        return output.ToArray();
    }
}
