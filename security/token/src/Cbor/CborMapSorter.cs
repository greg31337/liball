using System;
namespace token;

internal class CborEncodedMapEntry
{
    internal CborMapEntry? Entry;
    internal byte[]? EncodedKey;
    internal int OriginalIndex;
}
