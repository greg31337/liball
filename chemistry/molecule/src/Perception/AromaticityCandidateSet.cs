using System.Collections.Generic;

namespace molecule;


internal class AromaticityCandidateSet
{
    internal List<AromaticRingCandidate> RingCandidates;
    internal List<int> CandidateRingIds;
    internal List<int> CandidateAtomIds;
    internal List<int> CandidateBondIds;

    internal AromaticityCandidateSet()
    {
        RingCandidates = new List<AromaticRingCandidate>();
        CandidateRingIds = new List<int>();
        CandidateAtomIds = new List<int>();
        CandidateBondIds = new List<int>();
    }

    internal bool RingIsCandidate(int ringId)
    {
        return ContainsInt(CandidateRingIds, ringId);
    }

    internal bool AtomIsCandidate(int atomId)
    {
        return ContainsInt(CandidateAtomIds, atomId);
    }

    internal bool BondIsCandidate(int bondId)
    {
        return ContainsInt(CandidateBondIds, bondId);
    }

    internal bool ContainsInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddUniqueInt(List<int> values, int value)
    {
        if (!ContainsInt(values, value))
        {
            values.Add(value);
        }
    }

    internal void AddCandidateRing(AromaticRingCandidate ring)
    {
        RingCandidates.Add(ring);

        if (ring.IsCandidate)
        {
            AddUniqueInt(CandidateRingIds, ring.RingId);

            int atomIndex = 0;
            while (atomIndex < ring.AtomIds.Count)
            {
                AddUniqueInt(CandidateAtomIds, ring.AtomIds[atomIndex]);
                atomIndex = atomIndex + 1;
            }

            int bondIndex = 0;
            while (bondIndex < ring.BondIds.Count)
            {
                AddUniqueInt(CandidateBondIds, ring.BondIds[bondIndex]);
                bondIndex = bondIndex + 1;
            }
        }
    }
}

