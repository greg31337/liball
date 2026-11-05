using System;
using System.Collections.Generic;

namespace token;

internal class CoseHeaderIntegerValue
{
    internal bool Present;
    internal int Value;
}

internal class CoseHeaderRemovalResult
{
    internal CborMapEntry[]? Parameters;
    internal bool Removed;
}
