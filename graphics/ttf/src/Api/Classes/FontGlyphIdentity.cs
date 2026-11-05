namespace ttf;

/// <summary>Describes one glyph's name provenance and Unicode sequences.</summary>
public class FontGlyphIdentity : IFontGlyphIdentity
{
    FontGlyphCatalog catalog;
    int glyphId;
    string name;
    FontGlyphNameSource nameSource;
    FontPostTableFormat postTableFormat;
    int postNameIndex;
    bool hasPostNameIndex;
    bool validPostNameIndex;
    bool validPostName;
    FontCharacterMapping[]? unicodeSequences;

    internal FontGlyphIdentity(
        FontGlyphCatalog catalog,
        int glyphId,
        string name,
        FontGlyphNameSource nameSource,
        FontPostTableFormat postTableFormat,
        int postNameIndex,
        bool hasPostNameIndex,
        bool validPostNameIndex,
        bool validPostName)
    {
        this.catalog = catalog;
        this.glyphId = glyphId;
        this.name = name;
        this.nameSource = nameSource;
        this.postTableFormat = postTableFormat;
        this.postNameIndex = postNameIndex;
        this.hasPostNameIndex = hasPostNameIndex;
        this.validPostNameIndex = validPostNameIndex;
        this.validPostName = validPostName;
    }

    /// <summary>Gets the glyph identifier.</summary>
    /// <returns>The zero-based glyph identifier.</returns>
    public int GetGlyphId() { return glyphId; }

    /// <summary>Gets the parsed or stable synthetic glyph name.</summary>
    /// <returns>The glyph name.</returns>
    public string GetName() { return name; }

    /// <summary>Gets the glyph-name provenance.</summary>
    /// <returns>The name source.</returns>
    public FontGlyphNameSource GetNameSource() { return nameSource; }

    /// <summary>Reports whether the public name was synthesized.</summary>
    /// <returns>True for a stable <c>gidNNNN</c> fallback.</returns>
    public bool IsSyntheticName()
    {
        return nameSource != FontGlyphNameSource.PostStandard &&
            nameSource != FontGlyphNameSource.PostCustom &&
            nameSource != FontGlyphNameSource.PostVersion4CharacterCode;
    }

    /// <summary>Gets the supported <c>post</c> format used for inspection.</summary>
    /// <returns>The format, or unavailable.</returns>
    public FontPostTableFormat GetPostTableFormat() { return postTableFormat; }

    /// <summary>Reports whether a numeric <c>post</c> name value is available.</summary>
    /// <returns>True when a name index, offset result, or character code exists.</returns>
    public bool HasPostNameIndex() { return hasPostNameIndex; }

    /// <summary>Gets the numeric <c>post</c> name value.</summary>
    /// <returns>The name index, offset result, or character code.</returns>
    /// <exception cref="System.InvalidOperationException">No numeric value is available.</exception>
    public int GetPostNameIndex()
    {
        if (!hasPostNameIndex)
        {
            throw new System.InvalidOperationException(
                "The glyph identity has no numeric post name value.");
        }

        return postNameIndex;
    }

    /// <summary>Reports whether the numeric <c>post</c> name value is valid.</summary>
    /// <returns>True when the name value is structurally valid.</returns>
    public bool HasValidPostNameIndex() { return validPostNameIndex; }

    /// <summary>Reports whether <c>post</c> supplied a valid usable name.</summary>
    /// <returns>True for a valid provided name.</returns>
    public bool HasValidPostName() { return validPostName; }

    /// <summary>Gets the number of nominal and variation Unicode sequences for this glyph.</summary>
    /// <returns>The sequence count.</returns>
    public int GetUnicodeSequenceCount()
    {
        return GetUnicodeSequences().Length;
    }

    /// <summary>Gets one Unicode sequence with cmap provenance.</summary>
    /// <param name="index">The zero-based sequence index.</param>
    /// <returns>The resolved mapping that represents the sequence.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public FontCharacterMapping GetUnicodeSequence(int index)
    {
        FontCharacterMapping[] sequences = GetUnicodeSequences();

        if (index < 0 || index >= sequences.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return sequences[index];
    }

    FontCharacterMapping[] GetUnicodeSequences()
    {
        lock (this)
        {
            if (unicodeSequences == null)
            {
                unicodeSequences = catalog.CreateUnicodeSequences(glyphId);
            }

            return unicodeSequences;
        }
    }
}
