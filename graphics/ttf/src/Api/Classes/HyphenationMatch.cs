namespace ttf;

/// <summary>Represents immutable hyphenation opportunities for one input word.</summary>
public class HyphenationMatch : IHyphenationMatch
{
    int sourceLength;
    int[] runeIndices;
    int[] utf16Indices;

    internal HyphenationMatch(
        int sourceLength,
        int[] runeIndices,
        int[] utf16Indices)
    {
        this.sourceLength = sourceLength;
        this.runeIndices = runeIndices;
        this.utf16Indices = utf16Indices;
    }

    /// <summary>Returns the UTF-16 length of the matched word.</summary>
    /// <returns>The source length.</returns>
    public int GetSourceLength()
    {
        return sourceLength;
    }

    /// <summary>Returns the number of hyphenation opportunities.</summary>
    /// <returns>The opportunity count.</returns>
    public int GetPointCount()
    {
        return utf16Indices.Length;
    }

    /// <summary>Returns a hyphenation boundary as a Unicode scalar index.</summary>
    /// <param name="index">The opportunity index.</param>
    /// <returns>The scalar boundary before which a hyphen may be inserted.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public int GetRuneIndex(int index)
    {
        ValidateIndex(index);
        return runeIndices[index];
    }

    /// <summary>Returns a hyphenation boundary as an original UTF-16 index.</summary>
    /// <param name="index">The opportunity index.</param>
    /// <returns>The UTF-16 boundary before which a hyphen may be inserted.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public int GetUtf16Index(int index)
    {
        ValidateIndex(index);
        return utf16Indices[index];
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= utf16Indices.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
