using System.Reflection;
using ttf;

namespace ttf.Tests;

internal class MalformedBinaryPropertyTests
{
    const int MaximumGeneratedSize = 512;
    static int[] seeds = new int[]
    {
        0x13579BDF,
        0x02468ACE,
        0x10203040,
        0x6D2B79F5
    };

    [TestCaseSource(nameof(GetContainerCases))]
    public void ContainerReadersHandleBoundedMalformedData(
        string family,
        byte[] signature,
        int seed)
    {
        DeterministicByteGenerator generator =
            new DeterministicByteGenerator(seed);
        byte[] bytes = generator.CreateBytes(4, MaximumGeneratedSize);
        Array.Copy(signature, bytes, signature.Length);

        AssertDocumentedOutcome(
            () => FontLoader.Load(bytes, FontLoadOptions.CreateStrict()),
            family,
            seed);
    }

    [TestCaseSource(nameof(GetTableParserCases))]
    public void RegisteredTableReadersHandleBoundedMalformedData(
        string family,
        SfntTableParser parser,
        int seed)
    {
        DeterministicByteGenerator generator =
            new DeterministicByteGenerator(seed);
        byte[] bytes = generator.CreateBytes(4, MaximumGeneratedSize);
        WriteSupportedVersion(parser, bytes);
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            parser.GetTag(),
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Strict);

        AssertDocumentedOutcome(
            () => cache.FindParsedTable(parser),
            family,
            seed);
    }

    [TestCaseSource(nameof(GetSeeds))]
    public void BigEndianReaderRejectsEveryGeneratedOverread(int seed)
    {
        DeterministicByteGenerator generator =
            new DeterministicByteGenerator(seed);
        byte[] bytes = generator.CreateBytes(0, MaximumGeneratedSize);
        FontData data = FontData.Create(bytes);
        BigEndianReader reader = new BigEndianReader(
            data.GetSlice(0, data.GetLength()));

        while (reader.GetRemainingLength() != 0)
        {
            reader.ReadUInt8();
        }

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetPosition(), Is.EqualTo(bytes.Length));
            Assert.Throws<FontFormatException>(() => reader.ReadUInt8());
        });
    }

    [TestCaseSource(nameof(GetSeeds))]
    public void TrueTypeGlyphReadersHandleBoundedMalformedData(int seed)
    {
        DeterministicByteGenerator generator =
            new DeterministicByteGenerator(seed);
        byte[] simpleBytes = generator.CreateBytes(10, MaximumGeneratedSize);
        simpleBytes[0] = 0;
        simpleBytes[1] = 1;
        byte[] compositeBytes = generator.CreateBytes(10, MaximumGeneratedSize);
        compositeBytes[0] = 0xFF;
        compositeBytes[1] = 0xFF;
        SfntMaxpTable maxp = CreateBoundedMaxp();
        FontLimits limits = FontLimits.CreateStrict();

        AssertDocumentedOutcome(
            () => SfntSimpleGlyphParser.Parse(
                CreateGlyphRecord(simpleBytes),
                maxp,
                limits),
            "TrueType simple glyph",
            seed);
        AssertDocumentedOutcome(
            () => SfntCompositeGlyphParser.Parse(
                CreateGlyphRecord(compositeBytes),
                maxp,
                limits),
            "TrueType composite glyph",
            seed);
    }

    [TestCaseSource(nameof(GetSeeds))]
    public void CffReadersHandleBoundedMalformedData(int seed)
    {
        DeterministicByteGenerator generator =
            new DeterministicByteGenerator(seed);
        byte[] cffIndexBytes = generator.CreateBytes(2, MaximumGeneratedSize);
        int cffCount = generator.NextInt(33);
        cffIndexBytes[0] = (byte)(cffCount >> 8);
        cffIndexBytes[1] = (byte)cffCount;
        byte[] cff2IndexBytes = generator.CreateBytes(4, MaximumGeneratedSize);
        int cff2Count = generator.NextInt(33);
        cff2IndexBytes[0] = 0;
        cff2IndexBytes[1] = 0;
        cff2IndexBytes[2] = 0;
        cff2IndexBytes[3] = (byte)cff2Count;
        byte[] dictionaryBytes = generator.CreateBytes(0, MaximumGeneratedSize);
        byte[] charStringBytes = generator.CreateBytes(0, MaximumGeneratedSize);

        AssertDocumentedOutcome(
            () => CffIndexParser.Parse(CreateSlice(cffIndexBytes), 0),
            "CFF INDEX",
            seed);
        AssertDocumentedOutcome(
            () => Cff2IndexParser.Parse(CreateSlice(cff2IndexBytes), 0, 32),
            "CFF2 INDEX",
            seed);
        AssertDocumentedOutcome(
            () => CffDictParser.Parse(CreateSlice(dictionaryBytes)),
            "CFF DICT",
            seed);
        AssertDocumentedOutcome(
            () => CffDictParser.ParseCff2(CreateSlice(dictionaryBytes)),
            "CFF2 DICT",
            seed);
        AssertDocumentedOutcome(
            () => Type2CharStringExecutor.Execute(
                CreateSlice(charStringBytes),
                0,
                0,
                seed,
                null,
                null,
                10,
                1024),
            "Type 2 charstring",
            seed);
    }

    [TestCaseSource(nameof(GetSeeds))]
    public void UnicodeDataReaderHandlesBoundedMalformedData(int seed)
    {
        DeterministicByteGenerator generator =
            new DeterministicByteGenerator(seed);
        byte[] bytes = generator.CreateBytes(48, MaximumGeneratedSize);
        byte[] signature = new byte[]
        {
            0x54, 0x54, 0x46, 0x55, 0x43, 0x44, 0, 0
        };
        Array.Copy(signature, bytes, signature.Length);
        bytes[8] = 0;
        bytes[9] = 1;
        bytes[13] = 0;
        int sectionCount = generator.NextInt(17);
        bytes[46] = 0;
        bytes[47] = (byte)sectionCount;

        AssertDocumentedOutcome(
            () => new UnicodeData(bytes),
            "Unicode data artifact",
            seed);
    }

    static IEnumerable<TestCaseData> GetContainerCases()
    {
        object[][] families = new object[][]
        {
            new object[] { "SFNT", new byte[] { 0, 1, 0, 0 } },
            new object[] { "TTC", new byte[] { 0x74, 0x74, 0x63, 0x66 } },
            new object[] { "WOFF1", new byte[] { 0x77, 0x4F, 0x46, 0x46 } },
            new object[] { "WOFF2", new byte[] { 0x77, 0x4F, 0x46, 0x32 } }
        };

        foreach (object[] family in families)
        {
            foreach (int seed in seeds)
            {
                yield return new TestCaseData(family[0], family[1], seed)
                    .SetName(
                        "ContainerReadersHandleBoundedMalformedData(" +
                        family[0] + ",0x" + seed.ToString("X8") + ")");
            }
        }
    }

    static IEnumerable<TestCaseData> GetTableParserCases()
    {
        Assembly assembly = typeof(SfntTableParser).Assembly;
        Type[] types = assembly.GetTypes();
        Array.Sort(types, (first, second) => string.CompareOrdinal(
            first.FullName,
            second.FullName));

        foreach (Type type in types)
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType != typeof(SfntTableParser))
                {
                    continue;
                }

                SfntTableParser? parser = (SfntTableParser)field.GetValue(null)!;
                string family = FormatTag(parser!.GetTag()) + " table";

                foreach (int seed in seeds)
                {
                    yield return new TestCaseData(family, parser, seed)
                        .SetName(
                            "RegisteredTableReadersHandleBoundedMalformedData(" +
                            FormatTag(parser.GetTag()) + ",0x" +
                            seed.ToString("X8") + ")");
                }
            }
        }
    }

    static IEnumerable<TestCaseData> GetSeeds()
    {
        foreach (int seed in seeds)
        {
            yield return new TestCaseData(seed)
                .SetName("{m}(0x" + seed.ToString("X8") + ")");
        }
    }

    static void AssertDocumentedOutcome(
        TestDelegate operation,
        string family,
        int seed)
    {
        Exception? error = null;

        try
        {
            operation();
        }
        catch (Exception caught)
        {
            error = caught;
        }

        if (error != null)
        {
            Assert.That(
                error,
                Is.TypeOf<FontFormatException>()
                    .Or.TypeOf<FontLimitException>(),
                family + " leaked an unexpected exception for seed 0x" +
                seed.ToString("X8"));
        }
    }

    static void WriteSupportedVersion(SfntTableParser parser, byte[] bytes)
    {
        FieldInfo? field = typeof(SfntTableParser).GetField(
            "minimumMajorVersion",
            BindingFlags.Instance | BindingFlags.NonPublic);
        int majorVersion = (int)field!.GetValue(parser)!;

        if (parser.GetTag() == FontTags.Cff ||
            parser.GetTag() == FontTags.Cff2)
        {
            bytes[0] = (byte)majorVersion;
            bytes[1] = 0;
        }
        else
        {
            bytes[0] = (byte)(majorVersion >> 8);
            bytes[1] = (byte)majorVersion;
            bytes[2] = 0;
            bytes[3] = 0;
        }
    }

    static SfntGlyfRecord CreateGlyphRecord(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntGlyfRecord(
            0,
            0,
            data.GetSlice(0, data.GetLength()));
    }

    static SfntMaxpTable CreateBoundedMaxp()
    {
        return new SfntMaxpTable(
            0x00010000U,
            SfntMaxpKind.TrueType,
            256,
            512,
            16,
            2048,
            64,
            2,
            64,
            64,
            64,
            64,
            128,
            512,
            16,
            16);
    }

    static FontDataSlice CreateSlice(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return data.GetSlice(0, data.GetLength());
    }

    static string FormatTag(int tag)
    {
        return new string(new char[]
        {
            (char)((tag >> 24) & 0xFF),
            (char)((tag >> 16) & 0xFF),
            (char)((tag >> 8) & 0xFF),
            (char)(tag & 0xFF)
        });
    }
}

internal class DeterministicByteGenerator
{
    uint state;

    internal DeterministicByteGenerator(int seed)
    {
        state = (uint)seed;

        if (state == 0)
        {
            state = 0xA341316CU;
        }
    }

    internal byte[] CreateBytes(int minimumLength, int maximumLength)
    {
        if (minimumLength < 0 || maximumLength < minimumLength)
        {
            throw new ArgumentOutOfRangeException("minimumLength");
        }

        int length = minimumLength +
            NextInt(maximumLength - minimumLength + 1);
        byte[] bytes = new byte[length];

        for (int index = 0; index < bytes.Length; index += 1)
        {
            bytes[index] = (byte)NextUInt32();
        }

        return bytes;
    }

    internal int NextInt(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
        {
            throw new ArgumentOutOfRangeException("exclusiveMaximum");
        }

        return (int)(NextUInt32() % (uint)exclusiveMaximum);
    }

    uint NextUInt32()
    {
        uint value = state;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        state = value;
        return value;
    }
}
