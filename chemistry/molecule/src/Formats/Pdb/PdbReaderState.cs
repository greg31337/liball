using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


internal class PdbReaderState
{
    internal string Text;
    internal List<string> Lines;
    internal MoleculeBuilder Builder;
    internal PdbReadResult Result;
    internal int CurrentModelId;
    internal List<int> Serials;
    internal List<int> AtomIds;
    internal List<int> SerialModelIds;

    internal PdbReaderState()
    {
        Text = "";
        Lines = new List<string>();
        Builder = new MoleculeBuilder();
        Result = new PdbReadResult();
        CurrentModelId = -1;
        Serials = new List<int>();
        AtomIds = new List<int>();
        SerialModelIds = new List<int>();
    }
}
