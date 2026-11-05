using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


internal class V3000CollectionRecord
{
    internal string Type;
    internal string Label;
    internal List<int> SourceAtomIds;

    internal V3000CollectionRecord()
    {
        Type = "";
        Label = "";
        SourceAtomIds = new List<int>();
    }
}

