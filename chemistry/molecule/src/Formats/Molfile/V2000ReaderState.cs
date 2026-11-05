using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


internal class V2000ReaderState
{
    internal string Text;
    internal List<string> Lines;
    internal MoleculeBuilder Builder;
    internal MolfileReadResult Result;
    internal int AtomCount;
    internal int BondCount;
    internal MolfileReadOptions Options;

    internal V2000ReaderState()
    {
        Text = "";
        Lines = new List<string>();
        Builder = new MoleculeBuilder();
        Result = new MolfileReadResult();
        AtomCount = 0;
        BondCount = 0;
        Options = new MolfileReadOptions();
    }
}
