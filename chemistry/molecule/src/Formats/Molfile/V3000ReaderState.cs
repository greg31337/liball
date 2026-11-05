using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


internal class V3000ReaderState
{
    internal string Text;
    internal List<string> Lines;
    internal MoleculeBuilder Builder;
    internal MolfileReadResult Result;
    internal int AtomCount;
    internal int BondCount;
    internal bool SawCounts;
    internal List<int> SourceAtomIds;
    internal List<int> MoleculeAtomIds;
    internal List<int> SourceBondIds;
    internal List<int> MoleculeBondIds;
    internal List<V3000AtomStereoHint> AtomStereoHints;
    internal List<V3000CollectionRecord> Collections;
    internal MolfileReadOptions Options;

    internal V3000ReaderState()
    {
        Text = "";
        Lines = new List<string>();
        Builder = new MoleculeBuilder();
        Result = new MolfileReadResult();
        AtomCount = 0;
        BondCount = 0;
        SawCounts = false;
        SourceAtomIds = new List<int>();
        MoleculeAtomIds = new List<int>();
        SourceBondIds = new List<int>();
        MoleculeBondIds = new List<int>();
        AtomStereoHints = new List<V3000AtomStereoHint>();
        Collections = new List<V3000CollectionRecord>();
        Options = new MolfileReadOptions();
    }
}
