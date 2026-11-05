using System.Collections.Generic;

namespace molecule;


internal class AromaticAtomCandidate
{
    internal int AtomId;
    internal int AtomicNumber;
    internal bool IsCandidate;
    internal int ElectronContribution;
    internal bool HasRingDoubleBond;
    internal bool HasRingAromaticBond;
    internal string Reason;

    internal AromaticAtomCandidate()
    {
        AtomId = -1;
        AtomicNumber = 0;
        IsCandidate = false;
        ElectronContribution = 0;
        HasRingDoubleBond = false;
        HasRingAromaticBond = false;
        Reason = "";
    }
}

