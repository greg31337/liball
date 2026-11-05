using System.Collections.Generic;

namespace molecule;


internal class SmilesRingState
{
    internal int RingNumber;
    internal int AtomId;
    internal BondSpec BondSpec;
    internal bool HasBond;
    internal bool IsOpen;

    internal SmilesRingState()
    {
        RingNumber = 0;
        AtomId = -1;
        BondSpec = new BondSpec();
        HasBond = false;
        IsOpen = false;
    }
}

