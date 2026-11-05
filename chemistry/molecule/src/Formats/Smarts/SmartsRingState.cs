using System.Collections.Generic;

namespace molecule;


internal class SmartsRingState
{
    internal int RingNumber;
    internal int AtomId;
    internal QueryBond Bond;
    internal bool HasBond;
    internal bool IsOpen;

    internal SmartsRingState()
    {
        RingNumber = 0;
        AtomId = -1;
        Bond = new QueryBond();
        HasBond = false;
        IsOpen = false;
    }
}

