namespace webp;

internal class WebpVp8LPrefixCodeEntry
{
    internal int Symbol;
    internal int CodeLength;
    internal int CanonicalCode;
    internal int ReversedCode;

    internal WebpVp8LPrefixCodeEntry()
    {
        Symbol = 0;
        CodeLength = 0;
        CanonicalCode = 0;
        ReversedCode = 0;
    }
}
