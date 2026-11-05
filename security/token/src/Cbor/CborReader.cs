using System;
using System.Collections.Generic;
using System.Text;

namespace token;

internal class CborArgumentResult
{
    internal bool Success;
    internal bool Indefinite;
    internal ulong Value;
    internal int NextPosition;
    internal TokenError? Error;
}
