using System.Collections.Generic;
using System.IO;

namespace molecule;


internal class FormatCodecEntry
{
    internal FormatKind Kind;
    internal IMoleculeReader? Reader;
    internal IMoleculeWriter? Writer;

    internal FormatCodecEntry()
    {
        Kind = FormatKind.Unknown;
        Reader = null;
        Writer = null;
    }
}

