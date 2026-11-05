using ttf;

namespace ttf.Tests;

internal class FontGlyphCatalogPublicTests
{
    [Test]
    public void ExposesEverySupportedPostGlyphNameFormat()
    {
        FontGlyphCatalog version1 = LoadCatalog(
            BuildPostHeader(0x00010000U).CopyBytes(),
            258,
            null);
        TestBinaryBuilder version2Builder = BuildPostHeader(0x00020000U)
            .WriteUInt16(4)
            .WriteUInt16(0)
            .WriteUInt16(36)
            .WriteUInt16(258)
            .WriteUInt16(259);
        WritePascalString(version2Builder, "custom_name");
        WritePascalString(version2Builder, "A.alt");
        FontGlyphCatalog version2 = LoadCatalog(
            version2Builder.CopyBytes(),
            4,
            null);
        FontGlyphCatalog version25 = LoadCatalog(
            BuildPostHeader(0x00025000U)
                .WriteUInt16(3)
                .WriteInt8(36)
                .WriteInt8(36)
                .WriteInt8(-3)
                .CopyBytes(),
            3,
            null);
        FontGlyphCatalog version3 = LoadCatalog(
            BuildPostHeader(0x00030000U).CopyBytes(),
            3,
            null);
        FontGlyphCatalog version4 = LoadCatalog(
            BuildPostHeader(0x00040000U)
                .WriteUInt16(0x0041)
                .WriteUInt16(0x4E2D)
                .WriteUInt16(0xFFFF)
                .CopyBytes(),
            3,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(version1.GetPostTableFormat(),
                Is.EqualTo(FontPostTableFormat.Version1));
            Assert.That(version1.GetGlyphCount(), Is.EqualTo(258));
            Assert.That(version1.GetGlyph(36).GetName(), Is.EqualTo("A"));
            Assert.That(version1.GetGlyph(36).GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.PostStandard));
            Assert.That(version1.GetGlyph(36).GetPostNameIndex(), Is.EqualTo(36));
            Assert.That(version2.GetPostTableFormat(),
                Is.EqualTo(FontPostTableFormat.Version2));
            Assert.That(version2.GetGlyph(0).GetName(), Is.EqualTo(".notdef"));
            Assert.That(version2.GetGlyph(1).GetName(), Is.EqualTo("A"));
            Assert.That(version2.GetGlyph(2).GetName(), Is.EqualTo("custom_name"));
            Assert.That(version2.GetGlyph(2).GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.PostCustom));
            Assert.That(version2.GetGlyph(2).GetPostNameIndex(), Is.EqualTo(258));
            Assert.That(version2.GetGlyph(2).HasValidPostNameIndex(), Is.True);
            Assert.That(version2.GetGlyph(2).HasValidPostName(), Is.True);
            Assert.That(version2.GetGlyph(2).IsSyntheticName(), Is.False);
            Assert.That(version25.GetPostTableFormat(),
                Is.EqualTo(FontPostTableFormat.Version25));
            Assert.That(version25.GetGlyph(0).GetName(), Is.EqualTo("A"));
            Assert.That(version25.GetGlyph(1).GetName(), Is.EqualTo("B"));
            Assert.That(version25.GetGlyph(0).GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.PostStandard));
            Assert.That(version25.GetGlyph(2).GetName(), Is.EqualTo("gid0002"));
            Assert.That(version25.GetGlyph(2).GetPostNameIndex(), Is.EqualTo(-1));
            Assert.That(version25.GetGlyph(2).HasValidPostNameIndex(), Is.False);
            Assert.That(version25.GetGlyph(2).GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.SyntheticInvalidPostNameIndex));
            Assert.That(version3.GetPostTableFormat(),
                Is.EqualTo(FontPostTableFormat.Version3));
            Assert.That(version3.GetGlyph(2).GetName(), Is.EqualTo("gid0002"));
            Assert.That(version3.GetGlyph(2).GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.SyntheticPostVersion3));
            Assert.That(version3.GetGlyph(2).HasPostNameIndex(), Is.False);
            Assert.That(version3.GetGlyph(2).IsSyntheticName(), Is.True);
            Assert.That(version4.GetPostTableFormat(),
                Is.EqualTo(FontPostTableFormat.Version4));
            Assert.That(version4.GetGlyph(0).GetName(), Is.EqualTo("a0041"));
            Assert.That(version4.GetGlyph(0).GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.PostVersion4CharacterCode));
            Assert.That(version4.GetGlyph(0).GetPostNameIndex(), Is.EqualTo(0x41));
            Assert.That(version4.GetGlyph(1).GetName(), Is.EqualTo("a4E2D"));
            Assert.That(version4.GetGlyph(2).GetName(), Is.EqualTo("gid0002"));
            Assert.That(version4.GetGlyph(2).GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.SyntheticMissingPostVersion4Name));
        });
    }

    [Test]
    public void ReportsEverySyntheticNameReason()
    {
        FontGlyphIdentity absent = CreateCatalogWithoutPost().GetGlyph(0);
        TestBinaryBuilder invalidIndexBuilder = BuildPostHeader(0x00020000U)
            .WriteUInt16(1)
            .WriteUInt16(260);
        WritePascalString(invalidIndexBuilder, "first");
        WritePascalString(invalidIndexBuilder, "second");
        FontGlyphIdentity invalidIndex = LoadCatalog(
            invalidIndexBuilder.CopyBytes(),
            1,
            null).GetGlyph(0);
        TestBinaryBuilder invalidNameBuilder = BuildPostHeader(0x00020000U)
            .WriteUInt16(1)
            .WriteUInt16(258);
        WritePascalString(invalidNameBuilder, "bad-name");
        FontGlyphIdentity invalidName = LoadCatalog(
            invalidNameBuilder.CopyBytes(),
            1,
            null).GetGlyph(0);
        FontGlyphIdentity unsupported = LoadCatalog(
            BuildPostHeader(0x00050000U).CopyBytes(),
            1,
            null).GetGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(absent.GetName(), Is.EqualTo("gid0000"));
            Assert.That(absent.GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.SyntheticNoPostTable));
            Assert.That(absent.GetPostTableFormat(),
                Is.EqualTo(FontPostTableFormat.Unavailable));
            Assert.That(absent.HasPostNameIndex(), Is.False);
            Assert.Throws<InvalidOperationException>(() => absent.GetPostNameIndex());
            Assert.That(invalidIndex.GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.SyntheticInvalidPostNameIndex));
            Assert.That(invalidIndex.GetPostNameIndex(), Is.EqualTo(260));
            Assert.That(invalidIndex.HasValidPostNameIndex(), Is.False);
            Assert.That(invalidIndex.HasValidPostName(), Is.False);
            Assert.That(invalidName.GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.SyntheticInvalidPostName));
            Assert.That(invalidName.HasValidPostNameIndex(), Is.True);
            Assert.That(invalidName.HasValidPostName(), Is.False);
            Assert.That(unsupported.GetNameSource(),
                Is.EqualTo(FontGlyphNameSource.SyntheticUnsupportedPostTable));
            Assert.That(unsupported.GetPostTableFormat(),
                Is.EqualTo(FontPostTableFormat.Unavailable));
        });
    }

    [Test]
    public void FindsDuplicateNamesAndEnumeratesAllUnicodeSequencesPerGlyph()
    {
        TestBinaryBuilder postBuilder = BuildPostHeader(0x00020000U)
            .WriteUInt16(4)
            .WriteUInt16(0)
            .WriteUInt16(258)
            .WriteUInt16(258)
            .WriteUInt16(36);
        WritePascalString(postBuilder, "shared");
        byte[] nominal = BuildFormat6(0x41, new int[] { 1, 1, 2 });
        byte[] variations = BuildFormat14(0xFE0F, 0x41, 0x43, 1);
        byte[] cmap = BuildCmap(
            new int[] { 0, 0 },
            new int[] { 4, 5 },
            new byte[][] { nominal, variations });
        FontGlyphCatalog catalog = LoadCatalog(
            postBuilder.CopyBytes(),
            4,
            cmap);
        FontGlyphIdentity glyph = catalog.GetGlyph(1);
        FontGlyphIdentity[] matches = catalog.FindGlyphsByName("shared");
        FontGlyphIdentity[] secondMatches = catalog.FindGlyphsByName("shared");
        FontCharacterMapping nominalA = glyph.GetUnicodeSequence(0);
        FontCharacterMapping nominalB = glyph.GetUnicodeSequence(1);
        FontCharacterMapping defaultVariation = glyph.GetUnicodeSequence(2);
        FontCharacterMapping nonDefaultVariation = glyph.GetUnicodeSequence(3);
        int[] copiedSequence = nonDefaultVariation.CopyUnicodeSequence();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.GetGlyph(1), Is.SameAs(glyph));
            Assert.That(matches.Length, Is.EqualTo(2));
            Assert.That(matches[0].GetGlyphId(), Is.EqualTo(1));
            Assert.That(matches[1].GetGlyphId(), Is.EqualTo(2));
            Assert.That(matches, Is.Not.SameAs(secondMatches));
            Assert.That(catalog.FindGlyphsByName("Shared"), Is.Empty);
            Assert.That(catalog.FindGlyphsByName("missing"), Is.Empty);
            Assert.That(glyph.GetUnicodeSequenceCount(), Is.EqualTo(4));
            Assert.That(nominalA.CopyUnicodeSequence(), Is.EqualTo(new int[] { 0x41 }));
            Assert.That(nominalA.GetUnicodeSequenceLength(), Is.EqualTo(1));
            Assert.That(nominalA.GetUnicodeSequenceScalar(0), Is.EqualTo(0x41));
            Assert.That(nominalA.GetKind(),
                Is.EqualTo(FontCharacterMappingKind.Direct));
            Assert.That(nominalB.GetUnicodeValue(), Is.EqualTo(0x42));
            Assert.That(defaultVariation.CopyUnicodeSequence(),
                Is.EqualTo(new int[] { 0x41, 0xFE0F }));
            Assert.That(defaultVariation.GetKind(),
                Is.EqualTo(FontCharacterMappingKind.DefaultVariation));
            Assert.That(defaultVariation.GetUnicodeSequenceLength(), Is.EqualTo(2));
            Assert.That(defaultVariation.GetUnicodeSequenceScalar(1),
                Is.EqualTo(0xFE0F));
            Assert.That(nonDefaultVariation.CopyUnicodeSequence(),
                Is.EqualTo(new int[] { 0x43, 0xFE0F }));
            Assert.That(nonDefaultVariation.GetKind(),
                Is.EqualTo(FontCharacterMappingKind.NonDefaultVariation));
            Assert.That(catalog.GetGlyph(2).GetUnicodeSequenceCount(), Is.EqualTo(1));
            Assert.That(catalog.GetGlyph(2).GetUnicodeSequence(0).GetUnicodeValue(),
                Is.EqualTo(0x43));
            Assert.That(catalog.GetGlyph(0).GetUnicodeSequenceCount(), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.GetGlyph(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.GetGlyph(4));
            Assert.Throws<ArgumentNullException>(() => catalog.FindGlyphsByName(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                glyph.GetUnicodeSequence(4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                nominalA.GetUnicodeSequenceScalar(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                defaultVariation.GetUnicodeSequenceScalar(2));
        });

        copiedSequence[0] = 0;
        matches[0] = catalog.GetGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(nonDefaultVariation.CopyUnicodeSequence(),
                Is.EqualTo(new int[] { 0x43, 0xFE0F }));
            Assert.That(catalog.FindGlyphsByName("shared")[0].GetGlyphId(),
                Is.EqualTo(1));
        });
    }

    static FontGlyphCatalog LoadCatalog(
        byte[]? post,
        int glyphCount,
        byte[]? cmap)
    {
        TinySfntBuilder builder = new TinySfntBuilder()
            .AddTable("cmap", cmap == null ? new byte[4] : cmap)
            .AddTable("glyf", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("loca", new byte[4])
            .AddTable("maxp", BuildMaxp(glyphCount))
            .AddTable("name", new byte[6])
            .AddTable("OS/2", new byte[4]);

        if (post != null)
        {
            builder.AddTable("post", post);
        }

        FontFace face = new FontLoader()
            .LoadFromBytes(
                builder.Build(),
                FontLoadOptions.CreateCompatible())
            .GetFace(0);
        FontGlyphCatalog first = face.GetGlyphCatalog();
        FontGlyphCatalog second = face.GetGlyphCatalog();
        Assert.That(first, Is.SameAs(second));
        return first;
    }

    static FontGlyphCatalog CreateCatalogWithoutPost()
    {
        byte[] maxp = BuildMaxp(1);
        FontData data = FontData.Create(maxp);
        SfntTableData table = new SfntTableData(
            FontTags.Maxp,
            FontChecksum.CalculateTable(maxp),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                Array.Empty<FontDiagnostic>()),
            cache);
        return face.GetGlyphCatalog();
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount);
        int index = 0;

        while (index < 13)
        {
            builder.WriteUInt16((ushort)(index == 4 ? 1 : 0));
            index += 1;
        }

        return builder.CopyBytes();
    }

    static TestBinaryBuilder BuildPostHeader(uint version)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(version)
            .WriteInt32(0)
            .WriteInt16(-100)
            .WriteInt16(50)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0);
    }

    static void WritePascalString(TestBinaryBuilder builder, string value)
    {
        builder.WriteUInt8((byte)value.Length);
        int index = 0;

        while (index < value.Length)
        {
            builder.WriteUInt8((byte)value[index]);
            index += 1;
        }
    }

    static byte[] BuildFormat6(int firstCode, int[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(6)
            .WriteUInt16((ushort)(10 + glyphIds.Length * 2))
            .WriteUInt16(0)
            .WriteUInt16((ushort)firstCode)
            .WriteUInt16((ushort)glyphIds.Length);
        int index = 0;

        while (index < glyphIds.Length)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildFormat14(
        int variationSelector,
        int defaultUnicodeValue,
        int nonDefaultUnicodeValue,
        int nonDefaultGlyphId)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(14)
            .WriteUInt32(38)
            .WriteUInt32(1)
            .WriteUInt24((uint)variationSelector)
            .WriteUInt32(21)
            .WriteUInt32(29)
            .WriteUInt32(1)
            .WriteUInt24((uint)defaultUnicodeValue)
            .WriteUInt8(0)
            .WriteUInt32(1)
            .WriteUInt24((uint)nonDefaultUnicodeValue)
            .WriteUInt16((ushort)nonDefaultGlyphId)
            .CopyBytes();
    }

    static byte[] BuildCmap(
        int[] platformIds,
        int[] encodingIds,
        byte[][] subtables)
    {
        int offset = 4 + subtables.Length * 8;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)subtables.Length);
        int index = 0;

        while (index < subtables.Length)
        {
            builder
                .WriteUInt16((ushort)platformIds[index])
                .WriteUInt16((ushort)encodingIds[index])
                .WriteUInt32((uint)offset);
            offset += subtables[index].Length;
            index += 1;
        }

        index = 0;
        while (index < subtables.Length)
        {
            builder.WriteBytes(subtables[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }
}
