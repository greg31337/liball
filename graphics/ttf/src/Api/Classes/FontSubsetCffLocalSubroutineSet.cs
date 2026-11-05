namespace ttf;

/// <summary>Identifies retained local CFF subroutines for one font dictionary.</summary>
public class FontSubsetCffLocalSubroutineSet : IFontSubsetCffLocalSubroutineSet
{
    int fontDictionaryIndex;
    int[] subroutineIndices;

    internal FontSubsetCffLocalSubroutineSet(
        int fontDictionaryIndex,
        int[] subroutineIndices)
    {
        this.fontDictionaryIndex = fontDictionaryIndex;
        this.subroutineIndices = CopyIntegers(subroutineIndices);
    }

    /// <summary>Gets the source font-dictionary index.</summary>
    /// <returns>The font-dictionary index.</returns>
    public int GetFontDictionaryIndex() { return fontDictionaryIndex; }

    /// <summary>Copies the retained local-subroutine indices.</summary>
    /// <returns>A new sorted array.</returns>
    public int[] GetSubroutineIndices()
    {
        return CopyIntegers(subroutineIndices);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
