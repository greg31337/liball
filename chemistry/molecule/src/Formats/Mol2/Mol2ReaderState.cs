using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


internal class Mol2ReaderState
{
    internal string Text;
    internal List<string> Lines;
    internal MoleculeBuilder Builder;
    internal Mol2ReadResult Result;
    internal bool SawMolecule;
    internal int AtomCount;
    internal int BondCount;
    internal int SubstructureCount;
    internal List<int> SourceAtomIds;
    internal List<int> MoleculeAtomIds;
    internal List<int> AtomResidueIds;

    internal Mol2ReaderState()
    {
        Text = "";
        Lines = new List<string>();
        Builder = new MoleculeBuilder();
        Result = new Mol2ReadResult();
        SawMolecule = false;
        AtomCount = 0;
        BondCount = 0;
        SubstructureCount = 0;
        SourceAtomIds = new List<int>();
        MoleculeAtomIds = new List<int>();
        AtomResidueIds = new List<int>();
    }
}

