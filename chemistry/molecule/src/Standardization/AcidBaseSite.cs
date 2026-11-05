using System.Collections.Generic;

namespace molecule;


internal class AcidBaseSite
{
    internal int AtomId;
    internal AcidBaseRule Rule;
    internal bool Found;

    internal AcidBaseSite()
    {
        AtomId = -1;
        Rule = new AcidBaseRule();
        Found = false;
    }
}

