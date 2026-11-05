namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="HyphenationMatch"/>.</summary>
public interface IHyphenationMatch
{
    /// <summary>Returns the UTF-16 length of the matched word.</summary>
    /// <returns>The source length.</returns>
    int GetSourceLength();

    /// <summary>Returns the number of hyphenation opportunities.</summary>
    /// <returns>The opportunity count.</returns>
    int GetPointCount();

    /// <summary>Returns a hyphenation boundary as a Unicode scalar index.</summary>
    /// <param name="index">The opportunity index.</param>
    /// <returns>The scalar boundary before which a hyphen may be inserted.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    int GetRuneIndex(int index);

    /// <summary>Returns a hyphenation boundary as an original UTF-16 index.</summary>
    /// <param name="index">The opportunity index.</param>
    /// <returns>The UTF-16 boundary before which a hyphen may be inserted.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    int GetUtf16Index(int index);
}
