using ttf;

namespace ttf.Tests;

internal class FontLayoutInspectionPublicTests
{
    static int DefaultScript = Tag("DFLT");
    static int English = Tag("ENG ");
    static int Latin = Tag("latn");
    static int Ligatures = Tag("liga");
    static int RequiredLigatures = Tag("rlig");

    [Test]
    public void ExposesEveryGsubAndGposLookupFamilyIncludingExtensions()
    {
        int[] gsubTypes = new int[] { 1, 2, 3, 4, 5, 6, 8 };
        int[] gposTypes = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        int typeIndex = 0;

        while (typeIndex < gsubTypes.Length)
        {
            int lookupType = gsubTypes[typeIndex];
            FontLayoutLookup gsub = CreateLookup(
                lookupType,
                lookupType,
                typeIndex);

            Assert.Multiple(() =>
            {
                Assert.That(gsub.GetLookupType(), Is.EqualTo(lookupType));
                Assert.That(gsub.GetEffectiveType(), Is.EqualTo(lookupType));
                Assert.That(gsub.GetIndex(), Is.EqualTo(typeIndex));
                Assert.That(gsub.GetSubtableCount(), Is.EqualTo(1));
            });
            typeIndex += 1;
        }

        typeIndex = 0;
        while (typeIndex < gposTypes.Length)
        {
            int lookupType = gposTypes[typeIndex];
            FontLayoutLookup gpos = CreateLookup(
                lookupType,
                lookupType,
                typeIndex);

            Assert.Multiple(() =>
            {
                Assert.That(gpos.GetLookupType(), Is.EqualTo(lookupType));
                Assert.That(gpos.GetEffectiveType(), Is.EqualTo(lookupType));
                Assert.That(gpos.GetIndex(), Is.EqualTo(typeIndex));
                Assert.That(gpos.GetSubtableCount(), Is.EqualTo(1));
            });
            typeIndex += 1;
        }

        typeIndex = 0;
        while (typeIndex < gposTypes.Length)
        {
            int gposType = gposTypes[typeIndex];
            int gsubType = typeIndex < gsubTypes.Length ?
                gsubTypes[typeIndex] : gsubTypes[0];
            FontLayoutLookup gsubExtension = CreateLookup(7, gsubType, 0);
            FontLayoutLookup gposExtension = CreateLookup(9, gposType, 0);

            Assert.Multiple(() =>
            {
                Assert.That(gsubExtension.GetLookupType(), Is.EqualTo(7));
                Assert.That(gsubExtension.GetEffectiveType(),
                    Is.EqualTo(gsubType));
                Assert.That(gposExtension.GetLookupType(), Is.EqualTo(9));
                Assert.That(gposExtension.GetEffectiveType(),
                    Is.EqualTo(gposType));
            });
            typeIndex += 1;
        }
    }

    [Test]
    public void DiscoversGsubAndGposScriptsLanguagesFeaturesAndLookups()
    {
        FontFace face = CreateFace();
        FontLayoutInspection inspection = face.GetLayoutInspection();
        FontLayoutTable gsub = inspection.GetSubstitution()!;
        FontLayoutTable gpos = inspection.GetPositioning()!;
        FontLayoutScript latin = gsub.FindScript(Latin)!;
        FontLayoutLanguageSystem defaults =
            latin.GetDefaultLanguageSystem()!;
        FontLayoutLanguageSystem english = latin.FindLanguageSystem(English)!;
        FontLayoutFeature liga = defaults.GetOptionalFeature(0);
        FontLayoutLookup first = gsub.GetLookup(0);
        FontLayoutLookup second = gsub.GetLookup(1);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetLayoutInspection(), Is.SameAs(inspection));
            Assert.That(gsub.IsSubstitution(), Is.True);
            Assert.That(gsub.IsPositioning(), Is.False);
            Assert.That(gpos.IsPositioning(), Is.True);
            Assert.That(gpos.IsSubstitution(), Is.False);
            Assert.That(gsub.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(gsub.GetMinorVersion(), Is.EqualTo(1));
            Assert.That(gsub.GetScriptCount(), Is.EqualTo(2));
            Assert.That(gsub.GetScript(0).GetTag(), Is.EqualTo(DefaultScript));
            Assert.That(latin.GetTag(), Is.EqualTo(Latin));
            Assert.That(latin.GetLanguageSystemCount(), Is.EqualTo(1));
            Assert.That(latin.GetLanguageSystem(0).GetTag(), Is.EqualTo(English));
            Assert.That(english.GetTag(), Is.EqualTo(English));
            Assert.That(english.HasRequiredFeature(), Is.False);
            Assert.That(defaults.HasRequiredFeature(), Is.True);
            Assert.That(defaults.GetRequiredFeatureIndex(), Is.EqualTo(1));
            Assert.That(defaults.GetRequiredFeature()!.GetTag(),
                Is.EqualTo(RequiredLigatures));
            Assert.That(defaults.GetOptionalFeatureCount(), Is.EqualTo(1));
            Assert.That(defaults.GetOptionalFeatureIndex(0), Is.Zero);
            Assert.That(liga.GetTag(), Is.EqualTo(Ligatures));
            Assert.That(liga.HasParameters(), Is.True);
            Assert.That(liga.GetParameterOffset(), Is.GreaterThan(0));
            Assert.That(liga.CopyParameterBytes(2),
                Is.EqualTo(new byte[] { 0xAB, 0xCD }));
            Assert.That(liga.GetLookupCount(), Is.EqualTo(1));
            Assert.That(liga.GetLookupIndex(0), Is.EqualTo(1));
            Assert.That(gsub.FindFeature(RequiredLigatures)!.GetIndex(),
                Is.EqualTo(1));
            Assert.That(gsub.GetLookupCount(), Is.EqualTo(2));
            Assert.That(first.GetIndex(), Is.Zero);
            Assert.That(first.GetLookupType(), Is.EqualTo(1));
            Assert.That(first.GetEffectiveType(), Is.EqualTo(1));
            Assert.That(first.GetFlags(), Is.EqualTo(0x010B));
            Assert.That(first.IsRightToLeft(), Is.True);
            Assert.That(first.IgnoresBaseGlyphs(), Is.True);
            Assert.That(first.IgnoresLigatures(), Is.False);
            Assert.That(first.IgnoresMarks(), Is.True);
            Assert.That(first.GetMarkAttachmentType(), Is.EqualTo(1));
            Assert.That(first.UsesMarkFilteringSet(), Is.False);
            Assert.That(first.GetMarkFilteringSet(), Is.Zero);
            Assert.That(second.UsesMarkFilteringSet(), Is.True);
            Assert.That(second.GetMarkFilteringSet(), Is.EqualTo(3));
            Assert.That(first.GetSubtableCount(), Is.EqualTo(1));
            Assert.That(gpos.GetFeatureCount(), Is.EqualTo(2));
            Assert.Throws<InvalidOperationException>(() =>
                gsub.GetFeature(1).CopyParameterBytes(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                gsub.GetScript(2));
        });
    }

    [Test]
    public void SelectsCallerFeatureValuesOverUtf16Ranges()
    {
        FontLayoutTable gsub = CreateFace()
            .GetLayoutInspection()
            .GetSubstitution()!;
        FontLayoutSelection selection = gsub.Select(
            Latin,
            0,
            new FontLayoutFeatureSetting[]
            {
                new FontLayoutFeatureSetting(RequiredLigatures, 0, 0, 10),
                new FontLayoutFeatureSetting(Ligatures, 1, 0, 10),
                new FontLayoutFeatureSetting(Ligatures, 3, 3, 7),
                new FontLayoutFeatureSetting(Ligatures, 0, 5, 6)
            },
            10);
        FontLayoutSelectedLookup required = selection.GetLookup(0);
        FontLayoutSelectedLookup optional = selection.GetLookup(1);

        Assert.Multiple(() =>
        {
            Assert.That(selection.HasScript(), Is.True);
            Assert.That(selection.GetScriptTag(), Is.EqualTo(Latin));
            Assert.That(selection.UsedDefaultScript(), Is.False);
            Assert.That(selection.HasLanguageSystem(), Is.True);
            Assert.That(selection.GetLanguageTag(), Is.Zero);
            Assert.That(selection.UsedDefaultLanguageSystem(), Is.True);
            Assert.That(selection.GetAvailableFeatureCount(), Is.EqualTo(2));
            Assert.That(selection.GetAvailableFeature(0).GetTag(),
                Is.EqualTo(Ligatures));
            Assert.That(selection.HasFeature(RequiredLigatures), Is.True);
            Assert.That(selection.HasFeature(Ligatures), Is.True);
            Assert.That(selection.GetLookupCount(), Is.EqualTo(2));
            Assert.That(required.GetLookupIndex(), Is.Zero);
            Assert.That(required.GetLookup().GetIndex(), Is.Zero);
            Assert.That(required.GetRangeCount(), Is.EqualTo(1));
            Assert.That(required.GetValueAt(5), Is.EqualTo(1));
            Assert.That(optional.GetLookupIndex(), Is.EqualTo(1));
            Assert.That(optional.GetRangeCount(), Is.EqualTo(4));
            Assert.That(optional.GetRange(1).GetStart(), Is.EqualTo(3));
            Assert.That(optional.GetRange(1).GetEnd(), Is.EqualTo(5));
            Assert.That(optional.GetRange(1).GetValue(), Is.EqualTo(3));
            Assert.That(optional.GetValueAt(2), Is.EqualTo(1));
            Assert.That(optional.GetValueAt(4), Is.EqualTo(3));
            Assert.That(optional.GetValueAt(5), Is.Zero);
            Assert.That(optional.GetValueAt(6), Is.EqualTo(3));
            Assert.That(optional.GetValueAt(8), Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FontLayoutFeatureSetting(Ligatures, -1, 0, 1));
            Assert.Throws<ArgumentNullException>(() =>
                gsub.Select(Latin, 0, null!, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                optional.GetValueAt(10));
        });
    }

    [Test]
    public void DiscoversAndMatchesFeatureVariations()
    {
        FontLayoutTable gsub = CreateFace()
            .GetLayoutInspection()
            .GetSubstitution()!;
        FontLayoutFeatureVariations variations =
            gsub.GetFeatureVariations()!;
        NormalizedFontCoordinates matching = Coordinates(0.5);
        NormalizedFontCoordinates outside = Coordinates(0);
        FontLayoutFeature alternate = variations.GetAlternateFeature(0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(variations.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(variations.GetMinorVersion(), Is.Zero);
            Assert.That(variations.GetRecordCount(), Is.EqualTo(1));
            Assert.That(variations.GetConditionCount(0), Is.EqualTo(1));
            Assert.That(variations.GetConditionFormat(0, 0), Is.EqualTo(1));
            Assert.That(variations.GetConditionAxisIndex(0, 0), Is.Zero);
            Assert.That(variations.GetConditionMinimum(0, 0), Is.EqualTo(0.25));
            Assert.That(variations.GetConditionMaximum(0, 0), Is.EqualTo(0.75));
            Assert.That(variations.GetSubstitutionCount(0), Is.EqualTo(1));
            Assert.That(variations.GetSubstitutedFeatureIndex(0, 0), Is.Zero);
            Assert.That(alternate.GetTag(), Is.EqualTo(Ligatures));
            Assert.That(alternate.GetLookupCount(), Is.EqualTo(1));
            Assert.That(alternate.GetLookupIndex(0), Is.Zero);
            Assert.That(variations.Matches(0, matching), Is.True);
            Assert.That(variations.Matches(0, outside), Is.False);
            Assert.That(variations.FindMatchingRecord(matching), Is.Zero);
            Assert.That(variations.FindMatchingRecord(outside), Is.EqualTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                variations.GetConditionCount(1));
        });
    }

    static FontFace CreateFace()
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(10)
            .CopyBytes());
        AddTable(tables, FontTags.Gsub, BuildLayout(false));
        AddTable(tables, FontTags.Gpos, BuildLayout(true));
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static FontLayoutLookup CreateLookup(
        int declaredType,
        int effectiveType,
        int index)
    {
        SfntLayoutLookup lookup = new SfntLayoutLookup(
            declaredType,
            0,
            new int[] { 0 },
            new int[] { 0 },
            0);
        return new FontLayoutLookup(index, lookup, effectiveType);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] BuildLayout(bool positioning)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt32(0);
        int scriptList = builder.GetLength();
        builder.PatchUInt16(4, (ushort)scriptList)
            .WriteUInt16(2)
            .WriteTag("DFLT").WriteUInt16(0)
            .WriteTag("latn").WriteUInt16(0);
        int defaultScript = builder.GetLength();
        builder.PatchUInt16(scriptList + 6,
            (ushort)(defaultScript - scriptList))
            .WriteUInt16(4).WriteUInt16(0);
        WriteLanguageSystem(builder, 0xFFFF, new int[] { 0 });
        int latinScript = builder.GetLength();
        builder.PatchUInt16(scriptList + 12,
            (ushort)(latinScript - scriptList))
            .WriteUInt16(10).WriteUInt16(1)
            .WriteTag("ENG ").WriteUInt16(0);
        int latinDefault = builder.GetLength();
        WriteLanguageSystem(builder, 1, new int[] { 0 });
        int english = builder.GetLength();
        builder.PatchUInt16(latinScript + 8,
            (ushort)(english - latinScript));
        WriteLanguageSystem(builder, 0xFFFF, new int[] { 0 });

        int featureList = builder.GetLength();
        builder.PatchUInt16(6, (ushort)featureList)
            .WriteUInt16(2)
            .WriteTag("liga").WriteUInt16(0)
            .WriteTag("rlig").WriteUInt16(0);
        int liga = builder.GetLength();
        builder.PatchUInt16(featureList + 6,
            (ushort)(liga - featureList))
            .WriteUInt16(6).WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(0xABCD);
        int rlig = builder.GetLength();
        builder.PatchUInt16(featureList + 12,
            (ushort)(rlig - featureList))
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);

        int lookupList = builder.GetLength();
        builder.PatchUInt16(8, (ushort)lookupList)
            .WriteUInt16(2).WriteUInt16(0).WriteUInt16(0);
        int firstLookup = builder.GetLength();
        builder.PatchUInt16(lookupList + 2,
            (ushort)(firstLookup - lookupList))
            .WriteUInt16(1).WriteUInt16(0x010B)
            .WriteUInt16(1).WriteUInt16(8);
        WriteSingleSubtable(builder, positioning, 1, positioning ? 20 : 1);
        int secondLookup = builder.GetLength();
        builder.PatchUInt16(lookupList + 4,
            (ushort)(secondLookup - lookupList))
            .WriteUInt16(1).WriteUInt16(0x0010)
            .WriteUInt16(1).WriteUInt16(10).WriteUInt16(3);
        WriteSingleSubtable(builder, positioning, 2, positioning ? 30 : 1);

        int featureVariations = builder.GetLength();
        builder.PatchUInt32(10, (uint)featureVariations)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt32(1)
            .WriteUInt32(16).WriteUInt32(30)
            .WriteUInt16(1).WriteUInt32(6)
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(0x1000).WriteInt16(0x3000)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(1)
            .WriteUInt16(0).WriteUInt32(12)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        return builder.CopyBytes();
    }

    static void WriteLanguageSystem(
        TestBinaryBuilder builder,
        int requiredFeature,
        int[] optionalFeatures)
    {
        builder.WriteUInt16(0)
            .WriteUInt16((ushort)requiredFeature)
            .WriteUInt16((ushort)optionalFeatures.Length);
        int index = 0;
        while (index < optionalFeatures.Length)
        {
            builder.WriteUInt16((ushort)optionalFeatures[index]);
            index += 1;
        }
    }

    static void WriteSingleSubtable(
        TestBinaryBuilder builder,
        bool positioning,
        int glyphId,
        int value)
    {
        int subtable = builder.GetLength();
        if (positioning)
        {
            builder.WriteUInt16(1).WriteUInt16(8)
                .WriteUInt16(0x0004).WriteInt16((short)-value);
        }
        else
        {
            builder.WriteUInt16(1).WriteUInt16(6).WriteInt16((short)value);
        }
        builder.WriteUInt16(1).WriteUInt16(1).WriteUInt16((ushort)glyphId);
    }

    static NormalizedFontCoordinates Coordinates(double normalized)
    {
        return new NormalizedFontCoordinates(
            new int[] { Tag("wght") },
            new double[] { normalized },
            new double[] { normalized },
            new int[] { (int)System.Math.Round(normalized * 16384) },
            "test",
            -1,
            normalized == 0);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
