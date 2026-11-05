using System;
namespace token;

internal class CwtConfirmationMapResult
{
    internal CborMapEntry[]? Entries;
    internal TokenError? Error;

    internal CwtConfirmationMapResult()
    {
        Entries = null;
        Error = null;
    }
}
