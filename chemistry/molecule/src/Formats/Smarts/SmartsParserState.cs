using System.Collections.Generic;

namespace molecule;


internal class SmartsParserState
{
    internal string Text;
    internal int Index;
    internal QueryMolecule Query;
    internal int CurrentAtomId;
    internal QueryBond PendingBond;
    internal bool HasPendingBond;
    internal List<int> BranchAtoms;
    internal List<int> ComponentGroupStack;
    internal int CurrentComponentGroupId;
    internal int NextComponentGroupId;
    internal List<SmartsRingState> RingStates;
    internal SmartsParseResult Result;

    internal SmartsParserState()
    {
        Text = "";
        Index = 0;
        Query = new QueryMolecule();
        CurrentAtomId = -1;
        PendingBond = new QueryBond();
        HasPendingBond = false;
        BranchAtoms = new List<int>();
        ComponentGroupStack = new List<int>();
        CurrentComponentGroupId = -1;
        NextComponentGroupId = 0;
        RingStates = new List<SmartsRingState>();
        Result = new SmartsParseResult();
    }
}
