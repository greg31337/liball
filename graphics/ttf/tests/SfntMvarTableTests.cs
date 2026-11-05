using ttf;

namespace ttf.Tests;

internal class SfntMvarTableTests
{
    [Test]
    public void ParsesAndEvaluatesEveryRecognizedValueTag()
    {
        int[] tags = GetRecognizedTags();
        SfntMvarTable? table = SfntMvarTableParser.FindTable(
            CreateCache(BuildMvar(tags, 8, 0), true, true));
        SfntMvarLocation location = table!.CreateLocation(
            new double[] { 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetValueRecordSize(), Is.EqualTo(8));
            Assert.That(table.GetValueRecordCount(), Is.EqualTo(38));
            Assert.That(table.GetItemVariationStoreOffset(),
                Is.EqualTo(12 + tags.Length * 8));
            Assert.That(table.GetVariationStore(), Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetValueRecord(tags.Length));
        });

        int index = 0;

        while (index < tags.Length)
        {
            SfntMvarValueRecord record = table.GetValueRecord(index);
            Assert.Multiple(() =>
            {
                Assert.That(record.GetValueTag(), Is.EqualTo(tags[index]));
                Assert.That(record.GetDeltaSetIndex().GetOuterIndex(), Is.Zero);
                Assert.That(record.GetDeltaSetIndex().GetInnerIndex(),
                    Is.EqualTo(index));
                Assert.That(record.IsRecognized(), Is.True);
                Assert.That(table.FindValueRecord(tags[index]), Is.SameAs(record));
                Assert.That(location.HasValue(tags[index]), Is.True);
                Assert.That(location.CalculateDelta(tags[index]),
                    Is.EqualTo((index + 1) * 5));
            });
            index += 1;
        }
    }

    [Test]
    public void PreservesUnknownPrivateRecordsAndIgnoresMissingTags()
    {
        int privateTag = (int)TestSfntEncoding.EncodeTag("ZZZZ");
        int[] tags = new int[]
        {
            privateTag,
            SfntMvarValueTags.HorizontalAscender
        };
        SfntMvarTable? table = SfntMvarTableParser.FindTable(
            CreateCache(BuildMvar(tags, 8, 0), true, true));
        SfntMvarLocation location = table!.CreateLocation(
            new double[] { 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(table.GetValueRecord(0).IsRecognized(), Is.False);
            Assert.That(location.CalculateDelta(privateTag), Is.EqualTo(5));
            Assert.That(location.HasValue(FontTags.Head), Is.False);
            Assert.That(location.CalculateDelta(FontTags.Head), Is.Zero);
        });
    }

    [Test]
    public void SupportsEmptyTablesAndCompatibleRecordExtensions()
    {
        SfntMvarTable? empty = SfntMvarTableParser.FindTable(
            CreateCache(BuildMvar(Array.Empty<int>(), 8, 0), true, true));
        byte[] compatibleBytes = BuildMvar(
            new int[] { SfntMvarValueTags.HorizontalAscender },
            10,
            3);
        SfntParsedTableView? compatibleView = SfntMvarTableParser.FindView(
            CreateCache(compatibleBytes, true, true));
        SfntMvarTable compatible = (SfntMvarTable)
            compatibleView!.GetParsedData();

        Assert.Multiple(() =>
        {
            Assert.That(empty!.GetValueRecordCount(), Is.Zero);
            Assert.That(empty.GetVariationStore(), Is.Null);
            Assert.That(empty.CreateLocation(new double[] { 0.5 })
                .CalculateDelta(SfntMvarValueTags.HorizontalAscender), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                empty.CreateLocation(null!));
            Assert.Throws<ArgumentException>(() =>
                empty.CreateLocation(Array.Empty<double>()));
            Assert.That(compatibleView.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(compatible.GetMinorVersion(), Is.EqualTo(3));
            Assert.That(compatible.GetValueRecordSize(), Is.EqualTo(10));
            Assert.That(compatible.CreateLocation(new double[] { 0.5 })
                .CalculateDelta(SfntMvarValueTags.HorizontalAscender),
                Is.EqualTo(5));
        });
    }

    [Test]
    public void PreservesUnknownMajorAsRawData()
    {
        byte[] bytes = BuildMvar(Array.Empty<int>(), 8, 0);
        PatchUInt16(bytes, 0, 2);
        SfntParsedTableView? view = SfntMvarTableParser.FindView(
            CreateCache(bytes, true, true));

        Assert.Multiple(() =>
        {
            Assert.That(view!.HasParsedData(), Is.False);
            Assert.That(view.GetVersion().GetMajorVersion(), Is.EqualTo(2));
        });
    }

    [TestCaseSource(nameof(InvalidTables))]
    public void RejectsMalformedMvarTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() =>
            SfntMvarTableParser.FindTable(CreateCache(bytes, true, true)));
    }

    [Test]
    public void RequiresFvarAndTablesReferencedByRecognizedTags()
    {
        byte[] horizontal = BuildMvar(
            new int[] { SfntMvarValueTags.HorizontalCaretRise },
            8,
            0);
        byte[] vertical = BuildMvar(
            new int[] { SfntMvarValueTags.VerticalAscender },
            8,
            0);
        byte[] underline = BuildMvar(
            new int[] { SfntMvarValueTags.UnderlineSize },
            8,
            0);
        byte[] gasp = BuildMvar(
            new int[] { SfntMvarValueTags.GaspRange0 },
            8,
            0);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntMvarTableParser.FindTable(
                    CreateCache(horizontal, false, true)));
            Assert.Throws<FontFormatException>(() =>
                SfntMvarTableParser.FindTable(
                    CreateCache(horizontal, true, false)));
            Assert.Throws<FontFormatException>(() =>
                SfntMvarTableParser.FindTable(
                    CreateCacheWithout(vertical, FontTags.Vhea)));
            Assert.Throws<FontFormatException>(() =>
                SfntMvarTableParser.FindTable(
                    CreateCacheWithout(underline, FontTags.Post)));
            Assert.Throws<FontFormatException>(() =>
                SfntMvarTableParser.FindTable(
                    CreateCacheWithout(gasp, FontTags.Gasp)));
        });
    }

    static IEnumerable<TestCaseData> InvalidTables()
    {
        byte[] valid = BuildMvar(
            new int[] { SfntMvarValueTags.HorizontalAscender },
            8,
            0);
        yield return InvalidCase(valid[..11], "TruncatedHeader");

        byte[] reserved = valid.ToArray();
        PatchUInt16(reserved, 4, 1);
        yield return InvalidCase(reserved, "ReservedField");

        byte[] shortRecord = valid.ToArray();
        PatchUInt16(shortRecord, 6, 7);
        yield return InvalidCase(shortRecord, "ShortRecord");

        byte[] extendedCurrentRecord = valid.ToArray();
        PatchUInt16(extendedCurrentRecord, 6, 9);
        yield return InvalidCase(extendedCurrentRecord, "ExtendedCurrentRecord");

        byte[] zeroStore = valid.ToArray();
        PatchUInt16(zeroStore, 10, 0);
        yield return InvalidCase(zeroStore, "MissingStore");

        byte[] storeInRecords = valid.ToArray();
        PatchUInt16(storeInRecords, 10, 12);
        yield return InvalidCase(storeInRecords, "StoreInRecords");

        byte[] unordered = BuildMvar(
            new int[]
            {
                SfntMvarValueTags.HorizontalAscender,
                SfntMvarValueTags.CapHeight
            },
            8,
            0);
        yield return InvalidCase(unordered, "UnorderedRecords");

        byte[] duplicate = BuildMvar(
            new int[]
            {
                SfntMvarValueTags.HorizontalAscender,
                SfntMvarValueTags.HorizontalAscender
            },
            8,
            0);
        yield return InvalidCase(duplicate, "DuplicateRecords");

        byte[] missingDelta = valid.ToArray();
        PatchUInt16(missingDelta, 18, 5);
        yield return InvalidCase(missingDelta, "MissingDeltaSet");

        byte[] nonzeroEmptyStore = BuildMvar(Array.Empty<int>(), 8, 0);
        PatchUInt16(nonzeroEmptyStore, 10, 12);
        yield return InvalidCase(nonzeroEmptyStore, "NonzeroEmptyStore");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static int[] GetRecognizedTags()
    {
        int[] tags = new int[]
        {
            SfntMvarValueTags.CapHeight,
            SfntMvarValueTags.GaspRange0,
            SfntMvarValueTags.GaspRange1,
            SfntMvarValueTags.GaspRange2,
            SfntMvarValueTags.GaspRange3,
            SfntMvarValueTags.GaspRange4,
            SfntMvarValueTags.GaspRange5,
            SfntMvarValueTags.GaspRange6,
            SfntMvarValueTags.GaspRange7,
            SfntMvarValueTags.GaspRange8,
            SfntMvarValueTags.GaspRange9,
            SfntMvarValueTags.HorizontalAscender,
            SfntMvarValueTags.HorizontalClippingAscent,
            SfntMvarValueTags.HorizontalClippingDescent,
            SfntMvarValueTags.HorizontalCaretOffset,
            SfntMvarValueTags.HorizontalCaretRun,
            SfntMvarValueTags.HorizontalCaretRise,
            SfntMvarValueTags.HorizontalDescender,
            SfntMvarValueTags.HorizontalLineGap,
            SfntMvarValueTags.SubscriptXOffset,
            SfntMvarValueTags.SubscriptXSize,
            SfntMvarValueTags.SubscriptYOffset,
            SfntMvarValueTags.SubscriptYSize,
            SfntMvarValueTags.SuperscriptXOffset,
            SfntMvarValueTags.SuperscriptXSize,
            SfntMvarValueTags.SuperscriptYOffset,
            SfntMvarValueTags.SuperscriptYSize,
            SfntMvarValueTags.StrikeoutPosition,
            SfntMvarValueTags.StrikeoutSize,
            SfntMvarValueTags.UnderlinePosition,
            SfntMvarValueTags.UnderlineSize,
            SfntMvarValueTags.VerticalAscender,
            SfntMvarValueTags.VerticalCaretOffset,
            SfntMvarValueTags.VerticalCaretRun,
            SfntMvarValueTags.VerticalCaretRise,
            SfntMvarValueTags.VerticalDescender,
            SfntMvarValueTags.VerticalLineGap,
            SfntMvarValueTags.XHeight
        };
        Array.Sort(tags);
        return tags;
    }

    static byte[] BuildMvar(int[] tags, int recordSize, int minorVersion)
    {
        int storeOffset = tags.Length == 0 ? 0 :
            12 + tags.Length * recordSize;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)minorVersion)
            .WriteUInt16(0)
            .WriteUInt16((ushort)recordSize)
            .WriteUInt16((ushort)tags.Length)
            .WriteUInt16((ushort)storeOffset);
        int index = 0;

        while (index < tags.Length)
        {
            builder
                .WriteUInt32((uint)tags[index])
                .WriteUInt16(0)
                .WriteUInt16((ushort)index)
                .WriteZeros(recordSize - 8);
            index += 1;
        }

        if (tags.Length > 0)
        {
            WriteStore(builder, tags.Length);
        }

        return builder.CopyBytes();
    }

    static void WriteStore(TestBinaryBuilder builder, int itemCount)
    {
        builder
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(22)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0x4000)
            .WriteInt16(0x4000)
            .WriteUInt16((ushort)itemCount)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(0);
        int index = 0;

        while (index < itemCount)
        {
            builder.WriteInt16((short)((index + 1) * 10));
            index += 1;
        }
    }

    static SfntTableCache CreateCache(
        byte[] mvar,
        bool includeFvar,
        bool includeHhea)
    {
        return CreateCacheWithout(
            mvar,
            includeFvar ? 0 : FontTags.Fvar,
            includeHhea ? 0 : FontTags.Hhea);
    }

    static SfntTableCache CreateCacheWithout(byte[] mvar, int omittedTag)
    {
        return CreateCacheWithout(mvar, omittedTag, 0);
    }

    static SfntTableCache CreateCacheWithout(
        byte[] mvar,
        int firstOmittedTag,
        int secondOmittedTag)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Mvar, mvar);
        AddUnlessOmitted(tables, FontTags.Name, BuildName(),
            firstOmittedTag, secondOmittedTag);
        AddUnlessOmitted(tables, FontTags.Fvar, BuildFvar(),
            firstOmittedTag, secondOmittedTag);
        AddUnlessOmitted(tables, FontTags.Maxp, BuildMaxp(),
            firstOmittedTag, secondOmittedTag);
        AddUnlessOmitted(tables, FontTags.Hhea, BuildHhea(),
            firstOmittedTag, secondOmittedTag);
        AddUnlessOmitted(tables, FontTags.Vhea, BuildVhea(),
            firstOmittedTag, secondOmittedTag);
        AddUnlessOmitted(tables, FontTags.Os2, BuildOs2(),
            firstOmittedTag, secondOmittedTag);
        AddUnlessOmitted(tables, FontTags.Post, BuildPost(),
            firstOmittedTag, secondOmittedTag);
        AddUnlessOmitted(tables, FontTags.Gasp, BuildGasp(),
            firstOmittedTag, secondOmittedTag);
        return SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
    }

    static void AddUnlessOmitted(
        List<SfntTableData> tables,
        int tag,
        byte[] bytes,
        int firstOmittedTag,
        int secondOmittedTag)
    {
        if (tag != firstOmittedTag && tag != secondOmittedTag)
        {
            AddTable(tables, tag, bytes);
        }
    }

    static byte[] BuildName()
    {
        byte[] value = System.Text.Encoding.BigEndianUnicode.GetBytes("Weight");
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(18)
            .WriteUInt16(3).WriteUInt16(1).WriteUInt16(0x0409)
            .WriteUInt16(256).WriteUInt16((ushort)value.Length)
            .WriteUInt16(0).WriteBytes(value)
            .CopyBytes();
    }

    static byte[] BuildFvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(16).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(20).WriteUInt16(0).WriteUInt16(8)
            .WriteTag("wght")
            .WriteInt32(0).WriteInt32(0).WriteInt32(0x00010000)
            .WriteUInt16(0).WriteUInt16(256)
            .CopyBytes();
    }

    static byte[] BuildHhea()
    {
        return BuildMetricHeader(1, 0, 0);
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000)
            .WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildVhea()
    {
        return BuildMetricHeader(-2, 1, 3);
    }

    static byte[] BuildMetricHeader(
        int caretRise,
        int caretRun,
        int caretOffset)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(20)
            .WriteUInt16(1000).WriteInt16(0).WriteInt16(0).WriteInt16(1000)
            .WriteInt16((short)caretRise).WriteInt16((short)caretRun)
            .WriteInt16((short)caretOffset)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildOs2()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(2).WriteInt16(500).WriteUInt16(400).WriteUInt16(5)
            .WriteUInt16(0)
            .WriteInt16(650).WriteInt16(600).WriteInt16(10).WriteInt16(140)
            .WriteInt16(700).WriteInt16(650).WriteInt16(20).WriteInt16(480)
            .WriteInt16(50).WriteInt16(300).WriteInt16(0);
        builder.WriteZeros(10);
        return builder
            .WriteUInt32(0).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .WriteUInt32(0x54455354).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0xFFFF)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(20)
            .WriteUInt16(900).WriteUInt16(250)
            .WriteUInt32(0).WriteUInt32(0)
            .WriteInt16(450).WriteInt16(700)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0)
            .CopyBytes();
    }

    static byte[] BuildPost()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00030000).WriteInt32(0)
            .WriteInt16(-100).WriteInt16(50).WriteUInt32(0)
            .WriteUInt32(0).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .CopyBytes();
    }

    static byte[] BuildGasp()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(11);
        int maximum = 10;

        while (maximum <= 100)
        {
            builder.WriteUInt16((ushort)maximum).WriteUInt16(0x000F);
            maximum += 10;
        }

        return builder.WriteUInt16(0xFFFF).WriteUInt16(0x000F).CopyBytes();
    }

    static void AddTable(
        List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }
}
