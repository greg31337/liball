using System.Collections.Generic;
using System.IO;

namespace molecule;


internal class DuplicateEntry
{
    internal Molecule Molecule;
    internal string Key;
    internal int InputIndex;
    internal int FirstIndex;
    internal bool IsDuplicate;

    internal DuplicateEntry()
    {
        Molecule = new Molecule();
        Key = "";
        InputIndex = -1;
        FirstIndex = -1;
        IsDuplicate = false;
    }
}

