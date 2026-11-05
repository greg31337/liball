namespace asn1;

internal class PermittedAlphabetResult
{
    internal IntegerConstraintSet? Domain;
    internal IntegerConstraintSet? Root;
    internal IntegerConstraintSet? Extension;
    internal IntegerConstraintSet? KnownScalars;
    internal AlphabetIndexRange[] RootIndexRanges = [];
    internal int RootAlphabetSize;
    internal bool OpenExtension;

    internal int GetRootIndex(int scalar)
    {
        int index = 0;
        while (index < this.RootIndexRanges.Length)
        {
            AlphabetIndexRange range = this.RootIndexRanges[index];
            if (scalar >= range.LowerScalar && scalar <= range.UpperScalar)
            {
                return range.FirstIndex + scalar - range.LowerScalar;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int GetRootScalar(int index)
    {
        if (index < 0 || index >= this.RootAlphabetSize)
        {
            return -1;
        }

        int rangeIndex = 0;
        while (rangeIndex < this.RootIndexRanges.Length)
        {
            AlphabetIndexRange range = this.RootIndexRanges[rangeIndex];
            int count = range.UpperScalar - range.LowerScalar + 1;
            if (index >= range.FirstIndex && index < range.FirstIndex + count)
            {
                return range.LowerScalar + index - range.FirstIndex;
            }

            rangeIndex = rangeIndex + 1;
        }

        return -1;
    }
}
