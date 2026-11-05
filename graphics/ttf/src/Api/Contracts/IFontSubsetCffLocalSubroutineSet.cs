namespace ttf;

/// <summary>Defines retained local CFF subroutines for one font dictionary.</summary>
public interface IFontSubsetCffLocalSubroutineSet
{
    /// <summary>Gets the source font-dictionary index.</summary>
    /// <returns>The font-dictionary index.</returns>
    int GetFontDictionaryIndex();

    /// <summary>Copies the retained local-subroutine indices.</summary>
    /// <returns>A new sorted array.</returns>
    int[] GetSubroutineIndices();
}
