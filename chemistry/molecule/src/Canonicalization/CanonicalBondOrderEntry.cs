using System.Collections.Generic;

namespace molecule;


internal class CanonicalBondOrderEntry
{
    internal int BondId;
    internal int BeginCanonicalAtomIndex;
    internal int EndCanonicalAtomIndex;
    internal int OrderValue;
    internal int IsAromaticValue;
    internal int IsDativeValue;
    internal int StereoValue;
    internal int DirectionValue;
    internal int QueryOperationValue;
    internal string QueryValue;
    internal string SourceFlags;

    internal CanonicalBondOrderEntry()
    {
        BondId = -1;
        BeginCanonicalAtomIndex = -1;
        EndCanonicalAtomIndex = -1;
        OrderValue = 0;
        IsAromaticValue = 0;
        IsDativeValue = 0;
        StereoValue = 0;
        DirectionValue = 0;
        QueryOperationValue = 0;
        QueryValue = "";
        SourceFlags = "";
    }
}

