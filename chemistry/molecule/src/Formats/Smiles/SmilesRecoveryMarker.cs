using System.Collections.Generic;

namespace molecule;


internal class SmilesRecoveryMarker
{
    internal int Number;
    internal int Position;
    internal int Length;

    internal SmilesRecoveryMarker()
    {
        Number = 0;
        Position = -1;
        Length = 0;
    }
}

