using System.Collections.Generic;

namespace molecule;


internal class TautomerCandidate
{
    internal Molecule Molecule;
    internal string Key;
    internal string RuleName;
    internal bool Reverse;
    internal int DonorAtomId;
    internal int PivotAtomId;
    internal int AcceptorAtomId;

    internal TautomerCandidate()
    {
        Molecule = new Molecule();
        Key = "";
        RuleName = "";
        Reverse = false;
        DonorAtomId = -1;
        PivotAtomId = -1;
        AcceptorAtomId = -1;
    }
}

