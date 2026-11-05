namespace ttf.Tests;

internal class SfntLayoutTableTests
{
    static int English = (int)TestSfntEncoding.EncodeTag("ENG ");
    static int Kern = (int)TestSfntEncoding.EncodeTag("kern");
    static int Latin = (int)TestSfntEncoding.EncodeTag("latn");
    static int Ligatures = (int)TestSfntEncoding.EncodeTag("liga");

    [Test]
    public void ParsesScriptsLanguagesFeaturesAndLookupsFromTheirOwnBases()
    {
        SfntLayoutTable table = Parse(BuildVersion11());
        SfntLayoutScriptList scripts = table.GetScriptList();
        SfntLayoutScript? latin = scripts.FindScript(Latin);
        SfntLayoutLanguageSystem? defaults =
            latin!.GetDefaultLanguageSystem();
        SfntLayoutLanguageSystem? english =
            latin.FindLanguageSystem(English);
        SfntLayoutFeatureList features = table.GetFeatureList();
        SfntLayoutFeature? kern = features.FindFeature(Kern);
        SfntLayoutFeature? ligatures = features.FindFeature(Ligatures);
        SfntLayoutLookup firstLookup = table.GetLookupList().GetLookup(0);
        SfntLayoutLookup secondLookup = table.GetLookupList().GetLookup(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.EqualTo(1));
            Assert.That(table.GetFeatureVariationsOffset(), Is.EqualTo(108));
            Assert.That(table.GetDataSlice().GetLength(), Is.EqualTo(116));
            Assert.That(scripts.GetScriptCount(), Is.EqualTo(1));
            Assert.That(scripts.GetScript(0), Is.SameAs(latin));
            Assert.That(scripts.FindScript(Kern), Is.Null);
            Assert.That(latin.GetLanguageSystemCount(), Is.EqualTo(1));
            Assert.That(latin.GetLanguageSystem(0), Is.SameAs(english));
            Assert.That(latin.FindLanguageSystem(Kern), Is.Null);
            Assert.That(defaults!.GetLanguageTag(), Is.Zero);
            Assert.That(defaults.HasRequiredFeature(), Is.True);
            Assert.That(defaults.GetRequiredFeatureIndex(), Is.Zero);
            Assert.That(defaults.GetFeatureIndexCount(), Is.EqualTo(1));
            Assert.That(defaults.GetFeatureIndex(0), Is.EqualTo(1));
            Assert.That(english!.HasRequiredFeature(), Is.False);
            Assert.That(english.GetRequiredFeatureIndex(), Is.EqualTo(0xFFFF));
            Assert.That(english.GetFeatureIndex(0), Is.Zero);
            Assert.That(features.GetFeatureCount(), Is.EqualTo(2));
            Assert.That(features.GetFeature(0), Is.SameAs(kern));
            Assert.That(kern!.HasFeatureParams(), Is.True);
            Assert.That(kern.GetFeatureParamsOffset(), Is.EqualTo(6));
            Assert.That(kern.GetFeatureParamsTableOffset(), Is.EqualTo(68));
            Assert.That(kern.GetLookupIndexCount(), Is.EqualTo(1));
            Assert.That(kern.GetLookupIndex(0), Is.Zero);
            Assert.That(ligatures!.HasFeatureParams(), Is.False);
            Assert.That(ligatures.GetLookupIndex(0), Is.EqualTo(1));
            Assert.That(table.GetLookupList().GetLookupCount(), Is.EqualTo(2));
            Assert.That(firstLookup.GetLookupType(), Is.EqualTo(2));
            Assert.That(firstLookup.GetLookupFlag(), Is.EqualTo(0x010B));
            Assert.That(firstLookup.IsRightToLeft(), Is.True);
            Assert.That(firstLookup.IgnoresBaseGlyphs(), Is.True);
            Assert.That(firstLookup.IgnoresLigatures(), Is.False);
            Assert.That(firstLookup.IgnoresMarks(), Is.True);
            Assert.That(firstLookup.GetMarkAttachmentType(), Is.EqualTo(1));
            Assert.That(firstLookup.UsesMarkFilteringSet(), Is.False);
            Assert.That(firstLookup.GetSubtableCount(), Is.EqualTo(1));
            Assert.That(firstLookup.GetSubtableOffset(0), Is.EqualTo(8));
            Assert.That(firstLookup.GetSubtableTableOffset(0), Is.EqualTo(90));
            Assert.That(secondLookup.GetLookupType(), Is.EqualTo(4));
            Assert.That(secondLookup.UsesMarkFilteringSet(), Is.True);
            Assert.That(secondLookup.GetMarkFilteringSet(), Is.EqualTo(3));
            Assert.That(secondLookup.GetSubtableCount(), Is.EqualTo(2));
            Assert.That(secondLookup.GetSubtableOffset(0), Is.EqualTo(12));
            Assert.That(secondLookup.GetSubtableTableOffset(1), Is.EqualTo(106));
        });
    }

    [Test]
    public void ParsesVersion10WithEmptyListsAndNoOptionalOffsets()
    {
        SfntLayoutTable table = Parse(BuildVersion10());

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetFeatureVariationsOffset(), Is.Zero);
            Assert.That(table.GetScriptList().GetScriptCount(), Is.Zero);
            Assert.That(table.GetFeatureList().GetFeatureCount(), Is.Zero);
            Assert.That(table.GetLookupList().GetLookupCount(), Is.Zero);
        });
    }

    [TestCaseSource(nameof(InvalidLayoutTables))]
    public void RejectsMalformedOffsetsReferencesTagsAndFlags(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void EnforcesLookupAndAggregateEntryLimits()
    {
        byte[] bytes = BuildVersion11();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Parse(
                bytes,
                CreateLimits(1, 100)));
            Assert.Throws<FontLimitException>(() => Parse(
                bytes,
                CreateLimits(10, 1)));
        });
    }

    [Test]
    public void RejectsNullParserArguments()
    {
        FontData data = FontData.Create(BuildVersion10());
        FontDataSlice slice = data.GetSlice(0, data.GetLength());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutTableParser.Parse(null!, FontLimits.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutTableParser.Parse(slice, null!));
        });
    }

    static IEnumerable<TestCaseData> InvalidLayoutTables()
    {
        byte[] truncatedHeader = BuildVersion11().AsSpan(0, 9).ToArray();
        yield return InvalidCase(truncatedHeader, "TruncatedHeader");

        byte[] unknownVersion = BuildVersion11();
        PatchUInt16(unknownVersion, 0, 2);
        yield return InvalidCase(unknownVersion, "UnknownVersion");

        byte[] topListIntoHeader = BuildVersion11();
        PatchUInt16(topListIntoHeader, 4, 2);
        yield return InvalidCase(topListIntoHeader, "TopListIntoHeader");

        byte[] scriptIntoRecordArray = BuildVersion11();
        PatchUInt16(scriptIntoRecordArray, 20, 2);
        yield return InvalidCase(scriptIntoRecordArray, "ScriptIntoRecordArray");

        byte[] defaultLanguageIntoHeader = BuildVersion11();
        PatchUInt16(defaultLanguageIntoHeader, 22, 2);
        yield return InvalidCase(defaultLanguageIntoHeader, "DefaultLanguageIntoHeader");

        byte[] nullLanguageOffset = BuildVersion11();
        PatchUInt16(nullLanguageOffset, 30, 0);
        yield return InvalidCase(nullLanguageOffset, "NullLanguageOffset");

        byte[] featureIntoRecordArray = BuildVersion11();
        PatchUInt16(featureIntoRecordArray, 54, 2);
        yield return InvalidCase(featureIntoRecordArray, "FeatureIntoRecordArray");

        byte[] paramsIntoFeatureHeader = BuildVersion11();
        PatchUInt16(paramsIntoFeatureHeader, 62, 2);
        yield return InvalidCase(paramsIntoFeatureHeader, "ParamsIntoFeatureHeader");

        byte[] lookupIntoOffsetArray = BuildVersion11();
        PatchUInt16(lookupIntoOffsetArray, 78, 2);
        yield return InvalidCase(lookupIntoOffsetArray, "LookupIntoOffsetArray");

        byte[] subtableIntoLookupHeader = BuildVersion11();
        PatchUInt16(subtableIntoLookupHeader, 88, 2);
        yield return InvalidCase(subtableIntoLookupHeader, "SubtableIntoLookupHeader");

        byte[] variationIntoHeader = BuildVersion11();
        PatchUInt32(variationIntoHeader, 10, 12);
        yield return InvalidCase(variationIntoHeader, "VariationIntoHeader");

        byte[] nonzeroLookupOrder = BuildVersion11();
        PatchUInt16(nonzeroLookupOrder, 32, 1);
        yield return InvalidCase(nonzeroLookupOrder, "NonzeroLookupOrder");

        byte[] invalidRequiredFeature = BuildVersion11();
        PatchUInt16(invalidRequiredFeature, 34, 2);
        yield return InvalidCase(invalidRequiredFeature, "InvalidRequiredFeature");

        byte[] invalidLanguageFeature = BuildVersion11();
        PatchUInt16(invalidLanguageFeature, 38, 2);
        yield return InvalidCase(invalidLanguageFeature, "InvalidLanguageFeature");

        byte[] invalidFeatureLookup = BuildVersion11();
        PatchUInt16(invalidFeatureLookup, 66, 2);
        yield return InvalidCase(invalidFeatureLookup, "InvalidFeatureLookup");

        byte[] reservedLookupFlag = BuildVersion11();
        PatchUInt16(reservedLookupFlag, 84, 0x0020);
        yield return InvalidCase(reservedLookupFlag, "ReservedLookupFlag");

        byte[] emptyLookup = BuildVersion11();
        PatchUInt16(emptyLookup, 86, 0);
        yield return InvalidCase(emptyLookup, "EmptyLookup");

        byte[] unorderedFeatures = BuildVersion11();
        PatchUInt32(unorderedFeatures, 56, TestSfntEncoding.EncodeTag("aaaa"));
        yield return InvalidCase(unorderedFeatures, "UnorderedFeatures");

        byte[] invalidScriptTag = BuildVersion11();
        invalidScriptTag[16] = 0;
        yield return InvalidCase(invalidScriptTag, "InvalidScriptTag");

        byte[] invalidDefaultScript = BuildVersion11();
        PatchUInt32(
            invalidDefaultScript,
            16,
            TestSfntEncoding.EncodeTag("DFLT"));
        yield return InvalidCase(invalidDefaultScript, "InvalidDefaultScript");

        byte[] zeroLookupType = BuildVersion11();
        PatchUInt16(zeroLookupType, 82, 0);
        yield return InvalidCase(zeroLookupType, "ZeroLookupType");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Invalid_" + name);
    }

    static byte[] BuildVersion10()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(10).WriteUInt16(12).WriteUInt16(14)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .CopyBytes();
    }

    static byte[] BuildVersion11()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt32(0);

        int scriptList = builder.GetLength();
        builder.PatchUInt16(4, (ushort)scriptList)
            .WriteUInt16(1)
            .WriteTag("latn")
            .WriteUInt16(0);
        int script = builder.GetLength();
        builder.PatchUInt16(scriptList + 6, (ushort)(script - scriptList))
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteTag("ENG ")
            .WriteUInt16(0);
        int defaultLanguage = builder.GetLength();
        builder.PatchUInt16(script, (ushort)(defaultLanguage - script));
        WriteLanguageSystem(builder, 0, new int[] { 1 });
        int english = builder.GetLength();
        builder.PatchUInt16(script + 8, (ushort)(english - script));
        WriteLanguageSystem(builder, 0xFFFF, new int[] { 0 });

        int featureList = builder.GetLength();
        builder.PatchUInt16(6, (ushort)featureList)
            .WriteUInt16(2)
            .WriteTag("kern")
            .WriteUInt16(0)
            .WriteTag("liga")
            .WriteUInt16(0);
        int kern = builder.GetLength();
        builder.PatchUInt16(
            featureList + 6,
            (ushort)(kern - featureList))
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0);
        int kernParams = builder.GetLength();
        builder.PatchUInt16(kern, (ushort)(kernParams - kern))
            .WriteUInt16(0x1234);
        int ligatures = builder.GetLength();
        builder.PatchUInt16(
            featureList + 12,
            (ushort)(ligatures - featureList))
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(1);

        int lookupList = builder.GetLength();
        builder.PatchUInt16(8, (ushort)lookupList)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(0);
        int firstLookup = builder.GetLength();
        builder.PatchUInt16(
            lookupList + 2,
            (ushort)(firstLookup - lookupList))
            .WriteUInt16(2)
            .WriteUInt16(0x010B)
            .WriteUInt16(1)
            .WriteUInt16(0);
        int firstSubtable = builder.GetLength();
        builder.PatchUInt16(
            firstLookup + 6,
            (ushort)(firstSubtable - firstLookup))
            .WriteUInt16(1);
        int secondLookup = builder.GetLength();
        builder.PatchUInt16(
            lookupList + 4,
            (ushort)(secondLookup - lookupList))
            .WriteUInt16(4)
            .WriteUInt16(0x0010)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(3);
        int secondSubtable = builder.GetLength();
        builder.PatchUInt16(
            secondLookup + 6,
            (ushort)(secondSubtable - secondLookup))
            .WriteUInt16(1);
        int thirdSubtable = builder.GetLength();
        builder.PatchUInt16(
            secondLookup + 8,
            (ushort)(thirdSubtable - secondLookup))
            .WriteUInt16(2);

        int featureVariations = builder.GetLength();
        builder.PatchUInt32(10, (uint)featureVariations)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(0);
        return builder.CopyBytes();
    }

    static void WriteLanguageSystem(
        TestBinaryBuilder builder,
        int requiredFeatureIndex,
        int[] featureIndices)
    {
        builder
            .WriteUInt16(0)
            .WriteUInt16((ushort)requiredFeatureIndex)
            .WriteUInt16((ushort)featureIndices.Length);
        int index = 0;

        while (index < featureIndices.Length)
        {
            builder.WriteUInt16((ushort)featureIndices[index]);
            index += 1;
        }
    }

    static SfntLayoutTable Parse(byte[] bytes)
    {
        return Parse(bytes, FontLimits.CreateStrict());
    }

    static SfntLayoutTable Parse(byte[] bytes, FontLimits limits)
    {
        FontData data = FontData.Create(bytes);
        return SfntLayoutTableParser.Parse(
            data.GetSlice(0, data.GetLength()),
            limits);
    }

    static FontLimits CreateLimits(int maxLookups, int maxEntries)
    {
        return FontLimits.Create(
            1000,
            1000,
            10,
            10,
            100,
            100,
            100,
            10,
            10,
            10,
            maxLookups,
            100,
            maxEntries,
            100,
            1000,
            10,
            100,
            10,
            10);
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
