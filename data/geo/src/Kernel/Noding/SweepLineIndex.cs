using System.Collections.Generic;

namespace geo;

internal class SweepLineEvent
{
    internal double X;
    internal bool IsInsert;
    internal MonotoneChain Chain;

    internal SweepLineEvent(double x, bool isInsert, MonotoneChain chain)
    {
        X = x;
        IsInsert = isInsert;
        Chain = chain;
    }
}

internal class SegmentPairCandidate
{
    internal SegmentString First;
    internal int FirstSegmentIndex;
    internal SegmentString Second;
    internal int SecondSegmentIndex;
    internal int FirstChainId;
    internal int SecondChainId;

    internal SegmentPairCandidate(
        SegmentString first, int firstSegmentIndex,
        SegmentString second, int secondSegmentIndex,
        int firstChainId, int secondChainId)
    {
        First = first;
        FirstSegmentIndex = firstSegmentIndex;
        Second = second;
        SecondSegmentIndex = secondSegmentIndex;
        FirstChainId = firstChainId;
        SecondChainId = secondChainId;
    }
}

internal class SweepLineIndex
{
    internal MonotoneChain[] Chains;
    internal List<SweepLineEvent> Events = new List<SweepLineEvent>();

    internal SweepLineIndex(SegmentString[] segmentStrings)
    {
        Chains = BuildChains(segmentStrings);
        BuildEvents();
    }

    internal SegmentPairCandidate[] GetCandidatePairs(bool includeSelfCandidates)
    {
        List<MonotoneChain> active = new List<MonotoneChain>();
        List<SegmentPairCandidate> candidates = new List<SegmentPairCandidate>();
        int eventIndex = 0;
        while (eventIndex < Events.Count)
        {
            SweepLineEvent sweepEvent = Events[eventIndex];
            if (sweepEvent.IsInsert)
            {
                int activeIndex = 0;
                while (activeIndex < active.Count)
                {
                    MonotoneChain activeChain = active[activeIndex];
                    bool sameSource = activeChain.Source == sweepEvent.Chain.Source;
                    if ((!sameSource || includeSelfCandidates) &&
                        RangesOverlap(activeChain.Bounds.MinY, activeChain.Bounds.MaxY,
                            sweepEvent.Chain.Bounds.MinY, sweepEvent.Chain.Bounds.MaxY))
                    {
                        AddChainCandidates(activeChain, sweepEvent.Chain, candidates);
                    }
                    activeIndex++;
                }
                active.Add(sweepEvent.Chain);
                active.Sort((first, second) => first.Id.CompareTo(second.Id));
            }
            else
            {
                active.Remove(sweepEvent.Chain);
            }
            eventIndex++;
        }
        candidates.Sort((first, second) => CompareCandidates(first, second));
        return candidates.ToArray();
    }

    internal static MonotoneChain[] BuildChains(SegmentString[] segmentStrings)
    {
        if (segmentStrings == null)
        {
            throw new InvalidGeometryException("Sweep-line segment string array is null.");
        }
        List<MonotoneChain> chains = new List<MonotoneChain>();
        int identifier = 0;
        int index = 0;
        while (index < segmentStrings.Length)
        {
            SegmentString segmentString = segmentStrings[index];
            if (segmentString == null)
            {
                throw new InvalidGeometryException("Sweep-line segment string is null.");
            }
            MonotoneChain[] sourceChains = MonotoneChain.CreateChains(
                segmentString, identifier);
            int chainIndex = 0;
            while (chainIndex < sourceChains.Length)
            {
                chains.Add(sourceChains[chainIndex]);
                chainIndex++;
            }
            identifier += sourceChains.Length;
            index++;
        }
        return chains.ToArray();
    }

    internal void BuildEvents()
    {
        int index = 0;
        while (index < Chains.Length)
        {
            MonotoneChain chain = Chains[index];
            SweepLineEvent insertion = new SweepLineEvent(
                chain.Bounds.MinX, true, chain);
            Events.Add(insertion);
            SweepLineEvent removal = new SweepLineEvent(
                chain.Bounds.MaxX, false, chain);
            Events.Add(removal);
            index++;
        }
        Events.Sort((first, second) => CompareEvents(first, second));
    }

    internal static void AddChainCandidates(
        MonotoneChain first, MonotoneChain second,
        List<SegmentPairCandidate> candidates)
    {
        MonotoneChain orderedFirst = first.Id <= second.Id ? first : second;
        MonotoneChain orderedSecond = orderedFirst == first ? second : first;
        int firstSegment = orderedFirst.StartIndex;
        while (firstSegment < orderedFirst.EndIndex)
        {
            Envelope firstEnvelope = orderedFirst.GetSegmentEnvelope(firstSegment);
            int secondSegment = orderedSecond.StartIndex;
            while (secondSegment < orderedSecond.EndIndex)
            {
                Envelope secondEnvelope = orderedSecond.GetSegmentEnvelope(secondSegment);
                if (EnvelopeAlgorithms.IntersectsEnvelopeValue(firstEnvelope, secondEnvelope))
                {
                    SegmentPairCandidate candidate = new SegmentPairCandidate(
                        orderedFirst.Source, firstSegment,
                        orderedSecond.Source, secondSegment,
                        orderedFirst.Id, orderedSecond.Id);
                    candidates.Add(candidate);
                }
                secondSegment++;
            }
            firstSegment++;
        }
    }

    internal static bool RangesOverlap(
        double firstMinimum, double firstMaximum,
        double secondMinimum, double secondMaximum)
    {
        return firstMinimum <= secondMaximum && firstMaximum >= secondMinimum;
    }

    internal static int CompareEvents(SweepLineEvent first, SweepLineEvent second)
    {
        if (first.X < second.X) return -1;
        if (first.X > second.X) return 1;
        if (first.IsInsert != second.IsInsert) return first.IsInsert ? -1 : 1;
        return first.Chain.Id.CompareTo(second.Chain.Id);
    }

    internal static int CompareCandidates(
        SegmentPairCandidate first, SegmentPairCandidate second)
    {
        if (first.FirstChainId != second.FirstChainId)
        {
            return first.FirstChainId.CompareTo(second.FirstChainId);
        }
        if (first.FirstSegmentIndex != second.FirstSegmentIndex)
        {
            return first.FirstSegmentIndex.CompareTo(second.FirstSegmentIndex);
        }
        if (first.SecondChainId != second.SecondChainId)
        {
            return first.SecondChainId.CompareTo(second.SecondChainId);
        }
        return first.SecondSegmentIndex.CompareTo(second.SecondSegmentIndex);
    }
}
