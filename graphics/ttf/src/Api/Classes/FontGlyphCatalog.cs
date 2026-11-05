namespace ttf;

/// <summary>Provides safe glyph lookup with name and Unicode provenance.</summary>
public class FontGlyphCatalog : IFontGlyphCatalog
{
    FontFace face;
    SfntPostTable? post;
    FontPostTableFormat postTableFormat;
    FontGlyphIdentity[] glyphs;
    int maximumSequenceCount;

    internal FontGlyphCatalog(FontFace face)
    {
        this.face = face;
        SfntMaxpTable? maxp = face.GetMaxpTable();

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "Glyph identity inspection requires a supported maxp table.");
        }

        post = face.GetPostTable();
        postTableFormat = GetPublicPostFormat(post);
        glyphs = new FontGlyphIdentity[maxp.GetGlyphCount()];
        maximumSequenceCount = face.GetTableCache()
            .GetLimits()
            .GetMaxCoverageEntries();
    }

    /// <summary>Gets the number of glyph identities.</summary>
    /// <returns>The glyph count from <c>maxp</c>.</returns>
    public int GetGlyphCount() { return glyphs.Length; }

    /// <summary>Gets the supported <c>post</c> format.</summary>
    /// <returns>The format, or unavailable.</returns>
    public FontPostTableFormat GetPostTableFormat() { return postTableFormat; }

    /// <summary>Gets one glyph identity by identifier.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The stable identity.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is invalid.</exception>
    public FontGlyphIdentity GetGlyph(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphs.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        lock (glyphs)
        {
            if (glyphs[glyphId] == null)
            {
                glyphs[glyphId] = CreateGlyph(glyphId);
            }

            return glyphs[glyphId];
        }
    }

    /// <summary>Finds every glyph whose name exactly matches an ordinal string.</summary>
    /// <param name="name">The glyph name to find.</param>
    /// <returns>All matches in ascending glyph-identifier order.</returns>
    /// <exception cref="System.ArgumentNullException"><c>name</c> is null.</exception>
    public FontGlyphIdentity[] FindGlyphsByName(string name)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        System.Collections.Generic.List<FontGlyphIdentity> matches =
            new System.Collections.Generic.List<FontGlyphIdentity>();
        int glyphId = 0;

        while (glyphId < glyphs.Length)
        {
            FontGlyphIdentity glyph = GetGlyph(glyphId);

            if (string.Equals(
                glyph.GetName(),
                name,
                System.StringComparison.Ordinal))
            {
                matches.Add(glyph);
            }

            glyphId += 1;
        }

        return matches.ToArray();
    }

    internal FontCharacterMapping[] CreateUnicodeSequences(int glyphId)
    {
        FontCharacterMap characterMap = face.GetCharacterMap();
        FontCharacterMapping[] nominal = characterMap.GetUnicodeCandidates(
            glyphId);
        System.Collections.Generic.List<FontCharacterMapping> sequences =
            new System.Collections.Generic.List<FontCharacterMapping>();
        int index = 0;

        while (index < nominal.Length)
        {
            AddSequence(sequences, nominal[index]);
            index += 1;
        }

        SfntCmapMapping? cmap = face.GetCmapMapping();
        SfntCmapFormat14Data? variations = cmap == null ?
            null : cmap.GetVariations();

        if (variations == null)
        {
            return sequences.ToArray();
        }

        int selectorIndex = 0;

        while (selectorIndex < variations.GetSelectorRecordCount())
        {
            SfntCmapVariationSelectorRecord selector =
                variations.GetSelectorRecord(selectorIndex);
            int variationSelector = selector.GetVariationSelector();
            index = 0;

            while (index < nominal.Length)
            {
                int unicodeValue = nominal[index].GetUnicodeValue();

                if (selector.IsDefaultUnicodeValue(unicodeValue))
                {
                    AddSequence(
                        sequences,
                        characterMap.MapVariation(
                            unicodeValue,
                            variationSelector));
                }

                index += 1;
            }

            index = 0;
            while (index < selector.GetNonDefaultMappingCount())
            {
                SfntCmapVariationMapping mapping =
                    selector.GetNonDefaultMapping(index);

                if (mapping.GetGlyphId() == glyphId && glyphId != 0)
                {
                    AddSequence(
                        sequences,
                        characterMap.MapVariation(
                            mapping.GetUnicodeValue(),
                            variationSelector));
                }

                index += 1;
            }

            selectorIndex += 1;
        }

        return sequences.ToArray();
    }

    void AddSequence(
        System.Collections.Generic.List<FontCharacterMapping> sequences,
        FontCharacterMapping mapping)
    {
        if (!mapping.IsMapped())
        {
            return;
        }

        if (sequences.Count >= maximumSequenceCount)
        {
            throw FontLimitException.Create(
                "The glyph Unicode sequence count exceeds the configured coverage limit.");
        }

        sequences.Add(mapping);
    }

    FontGlyphIdentity CreateGlyph(int glyphId)
    {
        if (post == null)
        {
            FontGlyphNameSource source = face.HasTable(FontTags.Post) ?
                FontGlyphNameSource.SyntheticUnsupportedPostTable :
                FontGlyphNameSource.SyntheticNoPostTable;
            return new FontGlyphIdentity(
                this,
                glyphId,
                CreateSyntheticName(glyphId),
                source,
                FontPostTableFormat.Unavailable,
                -1,
                false,
                false,
                false);
        }

        SfntPostGlyphName record = post.GetGlyphNameRecord(glyphId);
        bool hasNameIndex = post.GetFormat() != SfntPostFormat.Version3;
        return new FontGlyphIdentity(
            this,
            glyphId,
            record.GetName(),
            GetNameSource(post, record),
            postTableFormat,
            record.GetNameIndex(),
            hasNameIndex,
            record.HasValidNameIndex(),
            record.HasValidName());
    }

    static FontGlyphNameSource GetNameSource(
        SfntPostTable table,
        SfntPostGlyphName record)
    {
        if (record.IsProvided())
        {
            if (record.IsStandardName())
            {
                return FontGlyphNameSource.PostStandard;
            }

            if (record.IsCustomName())
            {
                return FontGlyphNameSource.PostCustom;
            }

            return FontGlyphNameSource.PostVersion4CharacterCode;
        }

        if (table.GetFormat() == SfntPostFormat.Version3)
        {
            return FontGlyphNameSource.SyntheticPostVersion3;
        }

        if (!record.HasValidNameIndex())
        {
            return FontGlyphNameSource.SyntheticInvalidPostNameIndex;
        }

        if (table.GetFormat() == SfntPostFormat.Version4)
        {
            return FontGlyphNameSource.SyntheticMissingPostVersion4Name;
        }

        return FontGlyphNameSource.SyntheticInvalidPostName;
    }

    static FontPostTableFormat GetPublicPostFormat(SfntPostTable? table)
    {
        if (table == null)
        {
            return FontPostTableFormat.Unavailable;
        }

        if (table.GetFormat() == SfntPostFormat.Version1)
        {
            return FontPostTableFormat.Version1;
        }

        if (table.GetFormat() == SfntPostFormat.Version2)
        {
            return FontPostTableFormat.Version2;
        }

        if (table.GetFormat() == SfntPostFormat.Version25)
        {
            return FontPostTableFormat.Version25;
        }

        if (table.GetFormat() == SfntPostFormat.Version3)
        {
            return FontPostTableFormat.Version3;
        }

        return FontPostTableFormat.Version4;
    }

    static string CreateSyntheticName(int glyphId)
    {
        return "gid" + glyphId.ToString(
            "D4",
            System.Globalization.CultureInfo.InvariantCulture);
    }
}
