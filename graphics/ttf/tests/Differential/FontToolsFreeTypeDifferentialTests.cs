namespace ttf.Tests;

internal class FontToolsFreeTypeDifferentialTests
{
    const string OracleFontBase64 =
        "AAEAAAAKAIAAAwAgT1MvMk0SdxAAAAEoAAAAYGNtYXAAVSE2AAABmAAAAERnbHlmq4nS+AAAAegAAAAyaGVhZC7ALewAAACsAAAANmhoZWEFmAIrAAAA5AAAACRobXR4CAIAWgAAAYgAAAAQbG9jYQAZAAwAAAHcAAAACm1heHAABgAGAAABCAAAACBuYW1leoVSSAAAAhwAAAJocG9zdHYZc5sAAASEAAAALwABAAAAAQAAWYXUnl8PPPUAAQPoAAAAAOap9SgAAAAA5qn1KAAyAAACJgK8AAAAAwACAAAAAAAAAAEAAAMg/zgAKAK8ACgAMgImAAEAAAAAAAAAAAAAAAAAAAAEAAEAAAAEAAQAAQAAAAAAAgAAAAAAAAAAAAAAAAAAAAAABAIBAZAABQEEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAgAAAAAAAAAAT1JDTADAACAgrALu/wYAFAOEASwAAAAAAAAAAAHCArwAAAAgAAAB9AAAAPoAAAJYADICvAAoAAAAAgAAAAMAAAAUAAMAAQAAABQABAAwAAAACAAIAAIAAAAgAEEgrP//AAAAIABBIKz////h/8HfVwABAAAAAAAAAAAAAAAAAAAADAAZAAAAAQAyAAACJgK8AAIAADMTEzL6+gK8/UQAAQAyAAAB9AK8AAMAADMhESEyAcL+PgK8AAAAAAAAEgDeAAEAAAAAAAEACwAAAAEAAAAAAAIACAALAAEAAAAAAAMAFgATAAEAAAAAAAQAEwApAAEAAAAAAAUADQA8AAEAAAAAAAYAEgBJAAEAAAABAAEADgBbAAEAAAABAAIABgBpAAEAAAABAAQAFQBvAAMAAQQJAAEAFgCEAAMAAQQJAAIADgCaAAMAAQQJAAMALACoAAMAAQQJAAQAJgDUAAMAAQQJAAUAGgD6AAMAAQQJAAYAJAEUAAMAAQQMAAEAHAE4AAMAAQQMAAIADAFUAAMAAQQMAAQAKgFgQ2FmjiBPcmFjbGVSjmd1bGllck9yYWNsZVNhbnMtUmVndWxhci0xLjBPcmFjbGUgU2FucyBSZWd1bGFyVmVyc2lvbiAxLjAwME9yYWNsZVNhbnMtUmVndWxhck9yYWNsZSBTYW5zIEZSTm9ybWFsT3JhY2xlIFNhbnMgRlIgTm9ybWFsAE8AcgBhAGMAbABlACAAUwBhAG4AcwBSAGUAZwB1AGwAYQByAE8AcgBhAGMAbABlAFMAYQBuAHMALQBSAGUAZwB1AGwAYQByAC0AMQAuADAATwByAGEAYwBsAGUAIABTAGEAbgBzACAAUgBlAGcAdQBsAGEAcgBWAGUAcgBzAGkAbwBuACAAMQAuADAAMAAwAE8AcgBhAGMAbABlAFMAYQBuAHMALQBSAGUAZwB1AGwAYQByAE8AcgBhAGMAbABlACAAUwBhAG4AcwAgAEYAUgBOAG8AcgBtAGEAbABPAHIAYQBjAGwAZQAgAFMAYQBuAHMAIABGAFIAIABOAG8AcgBtAGEAbAACAAAAAAAA/5wAMgAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAADACQBAgRFdXJvAA==";

    [Test]
    public void MatchesPinnedFontToolsTableVersionsAndRejectsTruncation()
    {
        byte[] bytes = GetOracleFont();
        FontFace face = Load(bytes).GetFace(0);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetHeadTable()!.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(face.GetHeadTable()!.GetMinorVersion(), Is.Zero);
            Assert.That(face.GetMaxpTable()!.GetKind(), Is.EqualTo(SfntMaxpKind.TrueType));
            Assert.That(face.GetOs2Table()!.GetVersion(), Is.EqualTo(4));
            Assert.That(face.GetPostTable()!.GetVersion(), Is.EqualTo(2.0));
            Assert.That(face.GetNameTable()!.GetVersion(), Is.Zero);
            Assert.That(face.GetCmapTable()!.GetEncodingRecordCount(), Is.EqualTo(2));
            Assert.Throws<FontFormatException>(() => Load(Array.Empty<byte>()));
            Assert.Throws<FontFormatException>(() => Load(bytes.AsSpan(0, 11).ToArray()));
            Assert.Throws<FontFormatException>(() => Load(bytes.AsSpan(0, bytes.Length - 1).ToArray()));
        });
    }

    [Test]
    public void MatchesPinnedFontToolsLegacyNamesLocalizedSelectionAndCmap()
    {
        FontFace face = Load(GetOracleFont()).GetFace(0);
        SfntNameTable? name = face.GetNameTable();
        SfntCmapMapping? cmap = face.GetCmapMapping();
        SfntDecodedName macFamily = FindDecodedName(name, 1, 1, 0, 0);
        FontNames english = face.GetNames();
        FontNames french = face.GetLocalizedNames("fr-FR");

        Assert.Multiple(() =>
        {
            Assert.That(macFamily.GetValue(), Is.EqualTo("Café Oracle"));
            Assert.That(macFamily.HasReplacementCharacters(), Is.False);
            Assert.That(english.GetFamilyName()!.GetValue(), Is.EqualTo("Oracle Sans"));
            Assert.That(english.GetFamilyName()!.GetLanguageTag(), Is.EqualTo("en-US"));
            Assert.That(french.GetFamilyName()!.GetValue(), Is.EqualTo("Oracle Sans FR"));
            Assert.That(french.GetFamilyName()!.GetLanguageTag(), Is.EqualTo("fr-FR"));
            Assert.That(french.GetFamilyName()!.GetMatchKind(), Is.EqualTo(FontNameMatchKind.ExactLanguage));
            Assert.That(face.GetLocalizedNames("fr-CA").GetFamilyName()!.GetValue(), Is.EqualTo("Oracle Sans FR"));
            Assert.That(cmap!.MapUnicode(0x20), Is.EqualTo(1));
            Assert.That(cmap.MapUnicode(0x41), Is.EqualTo(2));
            Assert.That(cmap.MapUnicode(0x20AC), Is.EqualTo(3));
            Assert.That(cmap.MapUnicode(0x42), Is.Zero);
            Assert.That(face.GetCoverage().GetRangeCount(), Is.EqualTo(3));
            Assert.That(face.GetCoverage().GetCoveredValueCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void MatchesPinnedFontToolsRightsAndFreeTypeGlobalMetrics()
    {
        FontFace face = Load(GetOracleFont()).GetFace(0);
        FontEmbeddingRights? rights = face.GetEmbeddingRights();
        FontDesignMetrics metrics = face.GetDesignMetrics(
            FontMetricSelectionPolicy.OpenTypeRecommended);
        FontScaledMetrics scaled = metrics.Scale(20);

        Assert.Multiple(() =>
        {
            Assert.That(rights!.GetRawFlags(), Is.EqualTo(0x0104));
            Assert.That(rights.GetPermission(), Is.EqualTo(FontEmbeddingPermission.PreviewAndPrint));
            Assert.That(rights.AllowsEmbedding(), Is.True);
            Assert.That(rights.AllowsSubsetting(), Is.False);
            Assert.That(rights.GetRecommendedAction(), Is.EqualTo(FontEmbeddingAction.EmbedFullFont));
            Assert.That(metrics.GetLineMetricSource(), Is.EqualTo(FontLineMetricSource.Typographic));
            Assert.That(metrics.GetUnitsPerEm(), Is.EqualTo(1000));
            Assert.That(metrics.GetAscender(), Is.EqualTo(750));
            Assert.That(metrics.GetDescender(), Is.EqualTo(-250));
            Assert.That(metrics.GetLineGap(), Is.EqualTo(20));
            Assert.That(metrics.GetLineHeight(), Is.EqualTo(1020));
            Assert.That(face.GetMetrics().GetGlyphCount(), Is.EqualTo(4));
            Assert.That(face.GetMetrics().GetMaximumAdvanceWidth(), Is.EqualTo(700));
            Assert.That(scaled.GetAscender(), Is.EqualTo(15));
            Assert.That(scaled.GetDescender(), Is.EqualTo(-5));
            Assert.That(scaled.GetLineHeight(), Is.EqualTo(20.4));
        });
    }

    [Test]
    public void MatchesPinnedFontToolsAndFreeTypePerGlyphMetrics()
    {
        FontFace face = Load(GetOracleFont()).GetFace(0);
        GlyphMetrics space = face.GetDesignGlyphMetrics(1, 0);
        GlyphMetrics letterA = face.GetDesignGlyphMetrics(2, 700);
        GlyphMetrics euro = face.GetDesignGlyphMetrics(3, 700);

        Assert.Multiple(() =>
        {
            Assert.That(space.GetAdvanceWidth(), Is.EqualTo(250));
            Assert.That(space.GetLeftSideBearing(), Is.Zero);
            Assert.That(letterA.GetAdvanceWidth(), Is.EqualTo(600));
            Assert.That(letterA.GetLeftSideBearing(), Is.EqualTo(50));
            Assert.That(euro.GetAdvanceWidth(), Is.EqualTo(700));
            Assert.That(euro.GetLeftSideBearing(), Is.EqualTo(40));
            Assert.That(letterA.GetAdvanceHeight(), Is.EqualTo(1000));
            Assert.That(letterA.GetVerticalOriginY(), Is.EqualTo(700));
            Assert.That(letterA.GetTopSideBearing(), Is.Zero);
            Assert.That(letterA.UsesSynthesizedVerticalAdvance(), Is.True);
            Assert.That(face.GetScaledGlyphMetrics(2, 700, 20).GetAdvanceWidth(), Is.EqualTo(12));
        });
    }

    [Test]
    public void KeepsUnsupportedHeadMajorVersionAsRawForInspection()
    {
        byte[] bytes = GetOracleFont();
        int headOffset = FindTableOffset(bytes, FontTags.Head);
        bytes[headOffset] = 0;
        bytes[headOffset + 1] = 2;
        FontPackage package = FontLoader.Load(bytes, FontLoadOptions.CreateInspectOnly());
        FontFace face = package.GetFace(0);
        SfntParsedTableView? view = face.GetHeadTableView();

        Assert.Multiple(() =>
        {
            Assert.That(view!.GetVersion().GetMajorVersion(), Is.EqualTo(2));
            Assert.That(view.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(view.HasParsedData(), Is.False);
            Assert.That(face.CopyTable(FontTags.Head)[0], Is.Zero);
            Assert.That(face.CopyTable(FontTags.Head)[1], Is.EqualTo(2));
        });
    }

    static FontPackage Load(byte[] bytes)
    {
        return FontLoader.Load(bytes, FontLoadOptions.CreateStrict());
    }

    internal static byte[] GetOracleFont()
    {
        return System.Convert.FromBase64String(OracleFontBase64);
    }

    static SfntDecodedName FindDecodedName(
        SfntNameTable? table,
        int nameId,
        int platformId,
        int encodingId,
        int languageId)
    {
        int index = 0;
        while (index < table!.GetRecordCount())
        {
            SfntNameRecord record = table.GetRecord(index);
            if (record.GetNameId() == nameId &&
                record.GetPlatformId() == platformId &&
                record.GetEncodingId() == encodingId &&
                record.GetLanguageId() == languageId)
            {
                return table.GetDecodedName(index);
            }
            index += 1;
        }
        throw new AssertionException("The pinned name record is absent.");
    }

    static int FindTableOffset(byte[] bytes, int tag)
    {
        int tableCount = bytes[4] << 8 | bytes[5];
        int index = 0;
        while (index < tableCount)
        {
            int recordOffset = 12 + index * 16;
            int recordTag = bytes[recordOffset] << 24 |
                bytes[recordOffset + 1] << 16 |
                bytes[recordOffset + 2] << 8 |
                bytes[recordOffset + 3];
            if (recordTag == tag)
            {
                return bytes[recordOffset + 8] << 24 |
                    bytes[recordOffset + 9] << 16 |
                    bytes[recordOffset + 10] << 8 |
                    bytes[recordOffset + 11];
            }
            index += 1;
        }
        throw new AssertionException("The pinned table is absent.");
    }
}
