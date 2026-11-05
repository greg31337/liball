using System.Collections.Generic;

namespace molecule;


internal class SmilesParserState
{
    internal string Text;
    internal int Index;
    internal MoleculeBuilder Builder;
    internal int CurrentAtomId;
    internal BondSpec PendingBond;
    internal bool HasPendingBond;
    internal List<int> BranchAtoms;
    internal List<SmilesRingState> RingStates;
    internal SmilesParseResult Result;

    internal SmilesParserState()
    {
        Text = "";
        Index = 0;
        Builder = new MoleculeBuilder();
        CurrentAtomId = -1;
        PendingBond = new BondSpec();
        HasPendingBond = false;
        BranchAtoms = new List<int>();
        RingStates = new List<SmilesRingState>();
        Result = new SmilesParseResult();
    }
}

