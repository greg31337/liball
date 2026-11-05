namespace ttf;

/// <summary>Defines ranked Unicode character mapping and inspection operations.</summary>
public interface IFontCharacterMap
{
    /// <summary>Gets compact merged Unicode coverage.</summary>
    /// <returns>The stable range-based coverage.</returns>
    FontCoverage GetCoverage();

    /// <summary>Reports whether Windows symbol remapping semantics are selected.</summary>
    /// <returns>True for a selected Windows symbol cmap.</returns>
    bool IsSymbolMapping();

    /// <summary>Gets the number of selected nominal mapping sources.</summary>
    /// <returns>The nominal source count.</returns>
    int GetNominalSourceCount();

    /// <summary>Gets the total number of selected nominal and variation sources.</summary>
    /// <returns>The source count.</returns>
    int GetSourceCount();

    /// <summary>Gets one selected mapping source.</summary>
    /// <param name="index">The zero-based source index.</param>
    /// <returns>The mapping source.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    FontCharacterMappingSource GetSource(int index);

    /// <summary>Gets the supported format 14 source.</summary>
    /// <returns>The variation source, or null when none is selected.</returns>
    FontCharacterMappingSource? GetVariationSource();

    /// <summary>Maps one Unicode scalar through ranked nominal cmap sources.</summary>
    /// <param name="unicodeValue">The Unicode scalar value.</param>
    /// <returns>The resolved or missing mapping.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>unicodeValue</c> is not a Unicode scalar.
    /// </exception>
    FontCharacterMapping MapUnicode(int unicodeValue);

    /// <summary>Maps one standardized Unicode variation sequence.</summary>
    /// <param name="unicodeValue">The base Unicode scalar value.</param>
    /// <param name="variationSelector">The standardized variation selector.</param>
    /// <returns>The resolved or missing mapping.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Either value is invalid for a Unicode variation sequence.
    /// </exception>
    FontCharacterMapping MapVariation(int unicodeValue, int variationSelector);

    /// <summary>Gets nominal Unicode mappings that resolve to one glyph.</summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>Mappings in effective source-preference order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is invalid.</exception>
    FontCharacterMapping[] GetUnicodeCandidates(int glyphId);
}
