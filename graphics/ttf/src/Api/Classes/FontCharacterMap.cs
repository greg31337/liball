namespace ttf;

/// <summary>Provides ranked Unicode cmap operations with source provenance.</summary>
public class FontCharacterMap : IFontCharacterMap
{
    FontFace face;
    SfntCmapMapping? mapping;
    FontCharacterMappingSource[] sources;
    int nominalSourceCount;
    FontCharacterMappingSource? variationSource;
    int glyphCount;

    internal FontCharacterMap(FontFace face)
    {
        this.face = face;
        SfntCmapTable? table = face.GetCmapTable();
        SfntMaxpTable? maxp = face.GetMaxpTable();
        glyphCount = maxp == null ? 0 : maxp.GetGlyphCount();
        mapping = table == null ? null : table.GetMapping();
        nominalSourceCount = mapping == null ? 0 : mapping.GetSourceCount();
        SfntCmapEncodingRecord? variationRecord = FindVariationRecord(table);
        int sourceCount = nominalSourceCount + (variationRecord == null ? 0 : 1);
        sources = new FontCharacterMappingSource[sourceCount];
        int index = 0;

        while (index < nominalSourceCount)
        {
            System.ArgumentNullException.ThrowIfNull(mapping);
            SfntCmapEncodingRecord record = mapping.GetSource(index);
            sources[index] = new FontCharacterMappingSource(
                index,
                FindEncodingRecordIndex(table, record),
                mapping.GetSourceRank(index),
                false,
                record);
            index += 1;
        }

        if (variationRecord != null)
        {
            variationSource = new FontCharacterMappingSource(
                nominalSourceCount,
                FindEncodingRecordIndex(table, variationRecord),
                -1,
                true,
                variationRecord);
            sources[nominalSourceCount] = variationSource;
        }
    }

    /// <summary>Gets compact merged Unicode coverage.</summary>
    /// <returns>The stable range-based coverage.</returns>
    public FontCoverage GetCoverage() { return face.GetCoverage(); }

    /// <summary>Reports whether Windows symbol remapping semantics are selected.</summary>
    /// <returns>True for a selected Windows symbol cmap.</returns>
    public bool IsSymbolMapping()
    {
        return mapping != null && mapping.IsSymbolMapping();
    }

    /// <summary>Gets the number of selected nominal mapping sources.</summary>
    /// <returns>The nominal source count.</returns>
    public int GetNominalSourceCount() { return nominalSourceCount; }

    /// <summary>Gets the total number of selected nominal and variation sources.</summary>
    /// <returns>The source count.</returns>
    public int GetSourceCount() { return sources.Length; }

    /// <summary>Gets one selected mapping source.</summary>
    /// <param name="index">The zero-based source index.</param>
    /// <returns>The mapping source.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public FontCharacterMappingSource GetSource(int index)
    {
        if (index < 0 || index >= sources.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return sources[index];
    }

    /// <summary>Gets the supported format 14 source.</summary>
    /// <returns>The variation source, or null when none is selected.</returns>
    public FontCharacterMappingSource? GetVariationSource()
    {
        return variationSource;
    }

    /// <summary>Maps one Unicode scalar through ranked nominal cmap sources.</summary>
    /// <param name="unicodeValue">The Unicode scalar value.</param>
    /// <returns>The resolved or missing mapping.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>unicodeValue</c> is not a Unicode scalar.
    /// </exception>
    public FontCharacterMapping MapUnicode(int unicodeValue)
    {
        ValidateUnicodeScalar(unicodeValue, "unicodeValue");

        if (mapping == null)
        {
            return CreateMissing(unicodeValue, -1, null);
        }

        int glyphId = mapping.MapUnicode(unicodeValue);

        if (glyphId == 0)
        {
            return CreateMissing(unicodeValue, -1, null);
        }

        int sourceIndex = mapping.FindMappingSourceIndex(unicodeValue, glyphId);
        FontCharacterMappingKind kind = mapping.IsSymbolRemapped(
            sourceIndex,
            unicodeValue) ?
            FontCharacterMappingKind.SymbolRemapped :
            FontCharacterMappingKind.Direct;
        return new FontCharacterMapping(
            unicodeValue,
            -1,
            glyphId,
            kind,
            sources[sourceIndex],
            null);
    }

    /// <summary>Maps one standardized Unicode variation sequence.</summary>
    /// <param name="unicodeValue">The base Unicode scalar value.</param>
    /// <param name="variationSelector">The standardized variation selector.</param>
    /// <returns>The resolved or missing mapping.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Either value is invalid for a Unicode variation sequence.
    /// </exception>
    public FontCharacterMapping MapVariation(
        int unicodeValue,
        int variationSelector)
    {
        ValidateUnicodeScalar(unicodeValue, "unicodeValue");

        if (!IsVariationSelector(variationSelector))
        {
            throw new System.ArgumentOutOfRangeException("variationSelector");
        }

        if (mapping == null)
        {
            return CreateMissing(unicodeValue, variationSelector, variationSource);
        }

        SfntCmapFormat14Data? variations = mapping.GetVariations();
        SfntCmapVariationSelectorRecord? selector = variations == null ?
            null : variations.FindSelectorRecord(variationSelector);

        if (selector == null)
        {
            return CreateMissing(unicodeValue, variationSelector, variationSource);
        }

        SfntCmapVariationMapping? nonDefault =
            selector.FindNonDefaultMapping(unicodeValue);

        if (nonDefault != null && nonDefault.GetGlyphId() != 0)
        {
            return new FontCharacterMapping(
                unicodeValue,
                variationSelector,
                nonDefault.GetGlyphId(),
                FontCharacterMappingKind.NonDefaultVariation,
                variationSource,
                variationSource);
        }

        if (selector.IsDefaultUnicodeValue(unicodeValue))
        {
            FontCharacterMapping nominal = MapUnicode(unicodeValue);

            if (nominal.IsMapped())
            {
                return new FontCharacterMapping(
                    unicodeValue,
                    variationSelector,
                    nominal.GetGlyphId(),
                    FontCharacterMappingKind.DefaultVariation,
                    nominal.GetSource(),
                    variationSource);
            }
        }

        return CreateMissing(unicodeValue, variationSelector, variationSource);
    }

    /// <summary>Gets nominal Unicode mappings that resolve to one glyph.</summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>Mappings in effective source-preference order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is invalid.</exception>
    public FontCharacterMapping[] GetUnicodeCandidates(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        if (mapping == null)
        {
            return new FontCharacterMapping[0];
        }

        int[] unicodeValues = mapping.GetUnicodeCandidates(glyphId);
        FontCharacterMapping[] candidates =
            new FontCharacterMapping[unicodeValues.Length];
        int index = 0;

        while (index < unicodeValues.Length)
        {
            candidates[index] = MapUnicode(unicodeValues[index]);
            index += 1;
        }

        return candidates;
    }

    FontCharacterMapping CreateMissing(
        int unicodeValue,
        int variationSelector,
        FontCharacterMappingSource? checkedVariationSource)
    {
        return new FontCharacterMapping(
            unicodeValue,
            variationSelector,
            0,
            FontCharacterMappingKind.Missing,
            null,
            checkedVariationSource);
    }

    static SfntCmapEncodingRecord? FindVariationRecord(SfntCmapTable? table)
    {
        if (table == null)
        {
            return null;
        }

        int index = 0;

        while (index < table.GetEncodingRecordCount())
        {
            SfntCmapEncodingRecord record = table.GetEncodingRecord(index);

            if (record.GetPlatformId() == 0 && record.GetEncodingId() == 5 &&
                record.GetSubtable().GetFormat14() != null)
            {
                return record;
            }

            index += 1;
        }

        return null;
    }

    static int FindEncodingRecordIndex(
        SfntCmapTable? table,
        SfntCmapEncodingRecord record)
    {
        System.ArgumentNullException.ThrowIfNull(table);
        int index = 0;

        while (index < table.GetEncodingRecordCount())
        {
            if (table.GetEncodingRecord(index) == record)
            {
                return index;
            }

            index += 1;
        }

        throw new System.InvalidOperationException(
            "A selected cmap source is absent from its source table.");
    }

    static void ValidateUnicodeScalar(int value, string parameterName)
    {
        if (value < 0 || value > 0x10FFFF ||
            value >= 0xD800 && value <= 0xDFFF)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static bool IsVariationSelector(int value)
    {
        return value >= 0x180B && value <= 0x180D ||
            value == 0x180F ||
            value >= 0xFE00 && value <= 0xFE0F ||
            value >= 0xE0100 && value <= 0xE01EF;
    }
}
