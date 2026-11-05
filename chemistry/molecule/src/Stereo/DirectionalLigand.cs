using System.Collections.Generic;

namespace molecule;


internal class DirectionalLigand
{
    internal bool Found;
    internal int AtomId;
    internal int BondId;
    internal BondDirection Direction;

    internal DirectionalLigand()
    {
        Found = false;
        AtomId = -1;
        BondId = -1;
        Direction = BondDirection.None;
    }
}

