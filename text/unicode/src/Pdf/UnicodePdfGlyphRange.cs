namespace unicode;

internal class UnicodePdfGlyphRange
{
    internal int StartCharacterCode;
    internal int EndCharacterCode;
    internal int StartUnicodeCodePoint;

    internal UnicodePdfGlyphRange(
        int startCharacterCode,
        int endCharacterCode,
        int startUnicodeCodePoint)
    {
        if (startCharacterCode < 0)
        {
            throw new System.ArgumentOutOfRangeException("startCharacterCode");
        }

        if (endCharacterCode < startCharacterCode)
        {
            throw new System.ArgumentException("Range end must be greater than or equal to range start.");
        }

        if (!UnicodeScalar.Instance.IsValid(startUnicodeCodePoint))
        {
            throw new System.ArgumentOutOfRangeException("startUnicodeCodePoint");
        }

        StartCharacterCode = startCharacterCode;
        EndCharacterCode = endCharacterCode;
        StartUnicodeCodePoint = startUnicodeCodePoint;
    }

    internal int Count()
    {
        return EndCharacterCode - StartCharacterCode + 1;
    }

    internal bool ContainsCharacterCode(int characterCode)
    {
        if (characterCode < StartCharacterCode)
        {
            return false;
        }

        return characterCode <= EndCharacterCode;
    }
}
