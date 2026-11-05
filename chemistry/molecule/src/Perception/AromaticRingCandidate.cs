using System.Collections.Generic;

namespace molecule;


internal class AromaticRingCandidate
{
    internal int RingId;
    internal int Size;
    internal List<int> AtomIds;
    internal List<int> BondIds;
    internal List<AromaticAtomCandidate> AtomCandidates;
    internal int ElectronCount;
    internal bool IsCandidate;
    internal string RejectionReason;

    internal AromaticRingCandidate()
    {
        RingId = -1;
        Size = 0;
        AtomIds = new List<int>();
        BondIds = new List<int>();
        AtomCandidates = new List<AromaticAtomCandidate>();
        ElectronCount = 0;
        IsCandidate = false;
        RejectionReason = "";
    }
}

