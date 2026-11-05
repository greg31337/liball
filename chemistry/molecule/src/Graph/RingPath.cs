using System.Collections.Generic;
using System.Text;

namespace molecule;


internal class RingPath
{
    internal bool Found;
    internal List<int> AtomIds;
    internal List<int> BondIds;

    internal RingPath()
    {
        Found = false;
        AtomIds = new List<int>();
        BondIds = new List<int>();
    }
}

