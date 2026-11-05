using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


internal class V3000AtomStereoHint
{
    internal int SourceAtomId;
    internal int Cfg;

    internal V3000AtomStereoHint()
    {
        SourceAtomId = -1;
        Cfg = 0;
    }
}

