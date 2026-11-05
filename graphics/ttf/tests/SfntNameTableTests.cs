using System.Buffers.Binary;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class SfntNameTableTests
{
    [Test]
    public void ClassifiesShiftJisAndRejectsUnsupportedJohabNames()
    {
        int[][] records = new int[][]
        {
            Record(3, 2, 0x0411, 1, 0),
            Record(3, 6, 0x0412, 1, 1)
        };
        byte[][] strings = new byte[][]
        {
            new byte[] { 0x82, 0xA0 },
            new byte[] { 0x84, 0x41 }
        };
        SfntNameTable? table = Parse(BuildNameTable(
            0,
            records,
            strings,
            Array.Empty<string>()));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetRecord(0).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.WindowsCodePage932));
            Assert.That(table.GetDecodedName(0).GetValue(), Is.EqualTo("\u3042"));
            Assert.That(table.GetRecord(1).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.Unknown));
            Assert.That(table.GetRecord(1).HasSupportedEncoding(), Is.False);
        });
    }

    [Test]
    public void ParsesFormatZeroRecordsAndClassifiesEncodings()
    {
        int[][] records = new int[][]
        {
            Record(0, 3, 0, 1, 0),
            Record(1, 0, 0, 1, 1),
            Record(1, 7, 0, 1, 2),
            Record(2, 0, 0, 1, 3),
            Record(3, 1, 0x0409, 1, 4),
            Record(3, 3, 0x0804, 1, 5),
            Record(3, 4, 0x0404, 1, 6),
            Record(3, 5, 0x0412, 1, 7),
            Record(3, 7, 0x0409, 1, 8)
        };
        byte[][] strings = new byte[][]
        {
            Utf16("Unicode"),
            new byte[] { 0x8E, 0x41 },
            new byte[] { 0x81, 0x40 },
            new byte[] { 1, 2, 3 },
            Utf16("Windows"),
            new byte[] { 0xD6, 0xD0 },
            new byte[] { 0xA4, 0x40 },
            new byte[] { 0xB0, 0xA1 },
            new byte[] { 0xFF }
        };
        SfntNameTable? table = Parse(BuildNameTable(
            0,
            records,
            strings,
            Array.Empty<string>()));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.Zero);
            Assert.That(table.GetRecordCount(), Is.EqualTo(9));
            Assert.That(table.GetLanguageTagCount(), Is.Zero);
            Assert.That(table.GetDuplicateRecordCount(), Is.Zero);
            Assert.That(table.GetInvalidRecordCount(), Is.Zero);
            Assert.That(table.GetInvalidLanguageTagCount(), Is.Zero);
            Assert.That(table.GetStorageOffset(), Is.EqualTo(6 + 9 * 12));
            Assert.That(table.GetRecord(0).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.Utf16BigEndian));
            Assert.That(table.GetRecord(1).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.MacintoshRoman));
            Assert.That(table.GetRecord(2).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.MacintoshLegacy));
            Assert.That(table.GetRecord(3).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.Unknown));
            Assert.That(table.GetRecord(4).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.Utf16BigEndian));
            Assert.That(table.GetRecord(5).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.WindowsCodePage936));
            Assert.That(table.GetRecord(6).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.WindowsCodePage950));
            Assert.That(table.GetRecord(7).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.WindowsCodePage949));
            Assert.That(table.GetRecord(8).GetEncodingKind(),
                Is.EqualTo(SfntNameEncodingKind.Unknown));
            Assert.That(table.GetRecord(0).IsUnicodeEncoding(), Is.True);
            Assert.That(table.GetRecord(1).IsUnicodeEncoding(), Is.False);
            Assert.That(table.GetRecord(0).HasSupportedEncoding(), Is.True);
            Assert.That(table.GetRecord(3).HasSupportedEncoding(), Is.False);
            Assert.That(table.GetRecord(5).CopyStringBytes(), Is.EqualTo(strings[5]));
            Assert.That(table.GetStorageLength(),
                Is.EqualTo(strings.Sum(value => value.Length)));
        });
    }

    [Test]
    public void ParsesFormatOneLanguageTagsAndReferences()
    {
        int[][] records = new int[][]
        {
            Record(3, 1, 0x0409, 1, 0),
            Record(3, 1, 0x8000, 1, 1),
            Record(3, 1, 0x8001, 1, 2)
        };
        byte[][] strings = new byte[][]
        {
            Utf16("Fallback"),
            Utf16("English"),
            Utf16("Traditional")
        };
        SfntNameTable? table = Parse(BuildNameTable(
            1,
            records,
            strings,
            new string[] { "en", "zh-Hant-HK" }));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.EqualTo(1));
            Assert.That(table.GetLanguageTagCount(), Is.EqualTo(2));
            Assert.That(table.GetLanguageTagRecord(0).GetLanguageId(), Is.EqualTo(0x8000));
            Assert.That(table.GetLanguageTagRecord(0).GetLanguageTag(), Is.EqualTo("en"));
            Assert.That(table.GetLanguageTagRecord(1).GetLanguageId(), Is.EqualTo(0x8001));
            Assert.That(table.GetLanguageTagRecord(1).GetLanguageTag(),
                Is.EqualTo("zh-Hant-HK"));
            Assert.That(table.GetLanguageTagRecord(1).CopyStringBytes(),
                Is.EqualTo(Utf16("zh-Hant-HK")));
            Assert.That(table.GetRecord(0).UsesLanguageTag(), Is.False);
            Assert.That(table.GetRecord(1).UsesLanguageTag(), Is.True);
            Assert.That(table.GetRecord(1).GetLanguageTagIndex(), Is.Zero);
            Assert.That(table.GetRecord(2).GetLanguageTagIndex(), Is.EqualTo(1));
            Assert.That(table.GetLanguageTagForRecord(0), Is.Null);
            Assert.That(table.GetLanguageTagForRecord(1), Is.EqualTo("en"));
            Assert.That(table.GetLanguageTagForRecord(2), Is.EqualTo("zh-Hant-HK"));
        });
    }

    [Test]
    public void PreservesAndMarksDuplicateRecordKeys()
    {
        int[][] records = new int[][]
        {
            Record(3, 1, 0x0409, 1, 0),
            Record(3, 1, 0x0409, 1, 1),
            Record(3, 1, 0x0409, 1, 2),
            Record(3, 1, 0x0409, 2, 3)
        };
        SfntNameTable? table = Parse(BuildNameTable(
            0,
            records,
            new byte[][]
            {
                Utf16("First"),
                Utf16("Second"),
                Utf16("Third"),
                Utf16("Regular")
            },
            Array.Empty<string>()));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetRecordCount(), Is.EqualTo(4));
            Assert.That(table.GetDuplicateRecordCount(), Is.EqualTo(2));
            Assert.That(table.GetRecord(0).IsDuplicateKey(), Is.False);
            Assert.That(table.GetRecord(1).IsDuplicateKey(), Is.True);
            Assert.That(table.GetRecord(2).IsDuplicateKey(), Is.True);
            Assert.That(table.GetRecord(3).IsDuplicateKey(), Is.False);
            Assert.That(table.GetRecord(0).CopyStringBytes(), Is.EqualTo(Utf16("First")));
            Assert.That(table.GetRecord(1).CopyStringBytes(), Is.EqualTo(Utf16("Second")));
        });
    }

    [Test]
    public void RejectsUnsortedRecordKeys()
    {
        int[][] records = new int[][]
        {
            Record(3, 1, 0x0409, 2, 0),
            Record(3, 1, 0x0409, 1, 1)
        };
        byte[] bytes = BuildNameTable(
            0,
            records,
            new byte[][] { Utf16("Regular"), Utf16("Family") },
            Array.Empty<string>());

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void ValidatesVersionSpecificLanguageIds()
    {
        byte[] invalidVersion0 = BuildNameTable(
            0,
            new int[][] { Record(3, 1, 0x8000, 1, 0) },
            new byte[][] { Utf16("Name") },
            Array.Empty<string>());
        byte[] userDefinedVersion0 = BuildNameTable(
            0,
            new int[][] { Record(240, 0, 0x8000, 1, 0) },
            new byte[][] { new byte[] { 1, 2 } },
            Array.Empty<string>());
        byte[] unavailableTag = BuildNameTable(
            1,
            new int[][] { Record(3, 1, 0x8001, 1, 0) },
            new byte[][] { Utf16("Name") },
            new string[] { "en" });

        SfntNameTable? invalid = Parse(invalidVersion0);
        SfntNameTable? userDefined = Parse(userDefinedVersion0);
        SfntNameTable? unavailable = Parse(unavailableTag);

        Assert.Multiple(() =>
        {
            Assert.That(invalid!.GetRecord(0).HasValidLanguage(), Is.False);
            Assert.That(invalid.GetInvalidRecordCount(), Is.EqualTo(1));
            Assert.That(userDefined!.GetRecord(0).UsesLanguageTag(), Is.False);
            Assert.That(userDefined.GetRecord(0).HasValidLanguage(), Is.True);
            Assert.That(userDefined.GetLanguageTagForRecord(0), Is.Null);
            Assert.Throws<InvalidOperationException>(
                () => userDefined.GetRecord(0).GetLanguageTagIndex());
            Assert.That(unavailable!.GetRecord(0).HasValidLanguage(), Is.False);
            Assert.That(unavailable.GetLanguageTagForRecord(0), Is.Null);
        });
    }

    [Test]
    public void RejectsEveryTruncationOfFixedRecordData()
    {
        byte[] format0 = BuildNameTable(
            0,
            new int[][] { Record(3, 1, 0x0409, 1, 0) },
            new byte[][] { Utf16("A") },
            Array.Empty<string>());
        int format0FixedLength = 18;

        for (int length = 0; length < format0FixedLength; length += 1)
        {
            Assert.Throws<FontFormatException>(
                () => Parse(format0.AsSpan(0, length).ToArray()),
                "Format 0 length {0}",
                length);
        }

        byte[] format1 = BuildNameTable(
            1,
            Array.Empty<int[]>(),
            Array.Empty<byte[]>(),
            new string[] { "en" });
        int format1FixedLength = 12;

        for (int length = 0; length < format1FixedLength; length += 1)
        {
            Assert.Throws<FontFormatException>(
                () => Parse(format1.AsSpan(0, length).ToArray()),
                "Format 1 length {0}",
                length);
        }
    }

    [Test]
    public void RejectsInvalidStorageAndStringRanges()
    {
        byte[] storageBeforeRecords = BuildNameTable(
            0,
            new int[][] { Record(3, 1, 0x0409, 1, 0) },
            new byte[][] { Utf16("A") },
            Array.Empty<string>());
        WriteUInt16(storageBeforeRecords, 4, 17);
        byte[] storagePastTable = BuildNameTable(
            0,
            Array.Empty<int[]>(),
            Array.Empty<byte[]>(),
            Array.Empty<string>());
        WriteUInt16(storagePastTable, 4, 7);
        byte[] stringOffsetPastStorage = BuildNameTable(
            0,
            new int[][] { Record(3, 1, 0x0409, 1, 0) },
            new byte[][] { Utf16("A") },
            Array.Empty<string>());
        WriteUInt16(stringOffsetPastStorage, 16, 3);
        byte[] stringLengthPastStorage = BuildNameTable(
            0,
            new int[][] { Record(3, 1, 0x0409, 1, 0) },
            new byte[][] { Utf16("A") },
            Array.Empty<string>());
        WriteUInt16(stringLengthPastStorage, 14, 3);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(storageBeforeRecords));
            Assert.Throws<FontFormatException>(() => Parse(storagePastTable));
            Assert.Throws<FontFormatException>(() => Parse(stringOffsetPastStorage));
            Assert.Throws<FontFormatException>(() => Parse(stringLengthPastStorage));
        });
    }

    [TestCaseSource(nameof(InvalidUtf16Strings))]
    public void RejectsMalformedUtf16NameStrings(byte[] invalidString)
    {
        byte[] bytes = BuildNameTable(
            0,
            new int[][] { Record(3, 1, 0x0409, 1, 0) },
            new byte[][] { invalidString },
            Array.Empty<string>());

        SfntNameTable? table = Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetRecord(0).HasValidString(), Is.False);
            Assert.That(table.GetInvalidRecordCount(), Is.EqualTo(1));
            Assert.That(table.GetRecord(0).CopyStringBytes(), Is.EqualTo(invalidString));
        });
    }

    [Test]
    public void AcceptsValidUtf16SurrogatePairs()
    {
        byte[] pair = new byte[] { 0xD8, 0x3D, 0xDE, 0x00 };
        SfntNameTable? table = Parse(BuildNameTable(
            0,
            new int[][] { Record(3, 10, 0x0409, 1, 0) },
            new byte[][] { pair },
            Array.Empty<string>()));

        Assert.That(table!.GetRecord(0).CopyStringBytes(), Is.EqualTo(pair));
    }

    [Test]
    public void PreservesButMarksEmptyNameStringsInvalid()
    {
        SfntNameTable? table = Parse(BuildNameTable(
            0,
            new int[][] { Record(3, 1, 0x0409, 1, 0) },
            new byte[][] { Array.Empty<byte>() },
            Array.Empty<string>()));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetRecord(0).HasValidString(), Is.False);
            Assert.That(table.GetRecord(0).CopyStringBytes(), Is.Empty);
            Assert.That(table.GetInvalidRecordCount(), Is.EqualTo(1));
        });
    }

    [TestCase("")]
    [TestCase("en-")]
    [TestCase("en--US")]
    [TestCase("en_US")]
    [TestCase("abcdefghi")]
    public void RejectsInvalidLanguageTagStrings(string languageTag)
    {
        byte[] bytes = BuildNameTable(
            1,
            Array.Empty<int[]>(),
            Array.Empty<byte[]>(),
            new string[] { languageTag });

        SfntNameTable? table = Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetLanguageTagRecord(0).IsValid(), Is.False);
            Assert.That(table.GetLanguageTagRecord(0).GetLanguageTag(), Is.Null);
            Assert.That(table.GetInvalidLanguageTagCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsOddAndSurrogateLanguageTagStrings()
    {
        byte[] odd = BuildNameTable(
            1,
            Array.Empty<int[]>(),
            Array.Empty<byte[]>(),
            new string[] { "en" });
        WriteUInt16(odd, 8, 3);
        byte[] surrogate = BuildNameTable(
            1,
            Array.Empty<int[]>(),
            Array.Empty<byte[]>(),
            new string[] { "en" });
        int storageOffset = ReadUInt16(surrogate, 4);
        surrogate[storageOffset] = 0xD8;
        surrogate[storageOffset + 1] = 0;

        SfntNameTable? oddTable = Parse(odd);
        SfntNameTable? surrogateTable = Parse(surrogate);

        Assert.Multiple(() =>
        {
            Assert.That(oddTable!.GetLanguageTagRecord(0).IsValid(), Is.False);
            Assert.That(surrogateTable!.GetLanguageTagRecord(0).IsValid(), Is.False);
            Assert.That(oddTable.GetInvalidLanguageTagCount(), Is.EqualTo(1));
            Assert.That(surrogateTable.GetInvalidLanguageTagCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void AppliesEncodedStringByteLimit()
    {
        byte[] bytes = BuildNameTable(
            0,
            new int[][] { Record(3, 1, 0x0409, 1, 0) },
            new byte[][] { Utf16("AB") },
            Array.Empty<string>());
        SfntTableCache cache = CreateCache(
            bytes,
            FontLoadMode.Compatible,
            CreateLimits(3));

        Assert.Throws<FontLimitException>(() => SfntNameTableParser.FindTable(cache));
    }

    [Test]
    public void AppliesCoverageLimitBeforeAllocatingRecordArrays()
    {
        byte[] tooManyRecords = BuildNameTable(
            0,
            new int[][]
            {
                Record(3, 1, 0x0409, 1, 0),
                Record(3, 1, 0x0409, 2, 1)
            },
            new byte[][] { Utf16("A"), Utf16("B") },
            Array.Empty<string>());
        byte[] recordAndLanguageTag = BuildNameTable(
            1,
            new int[][] { Record(3, 1, 0x8000, 1, 0) },
            new byte[][] { Utf16("A") },
            new string[] { "en" });
        FontLimits limits = CreateLimits(100, 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() =>
                SfntNameTableParser.FindTable(CreateCache(
                    tooManyRecords,
                    FontLoadMode.Compatible,
                    limits)));
            Assert.Throws<FontLimitException>(() =>
                SfntNameTableParser.FindTable(CreateCache(
                    recordAndLanguageTag,
                    FontLoadMode.Compatible,
                    limits)));
        });
    }

    [Test]
    public void AllowsSharedAndOverlappingStringStorage()
    {
        byte[] bytes = BuildNameTable(
            0,
            new int[][]
            {
                Record(3, 1, 0x0409, 1, 0),
                Record(3, 1, 0x0409, 2, 1)
            },
            new byte[][] { Utf16("Shared"), Utf16("Other") },
            Array.Empty<string>());
        WriteUInt16(bytes, 28, 0);
        WriteUInt16(bytes, 26, Utf16("Shared").Length);
        SfntNameTable? table = Parse(bytes);

        Assert.That(table!.GetRecord(1).CopyStringBytes(),
            Is.EqualTo(table.GetRecord(0).CopyStringBytes()));
    }

    [Test]
    public void AppliesFutureVersionGatingWithFormatOnePrefix()
    {
        byte[] future = BuildNameTable(
            1,
            Array.Empty<int[]>(),
            Array.Empty<byte[]>(),
            Array.Empty<string>());
        WriteUInt16(future, 0, 2);
        SfntTableCache compatible = CreateCache(
            future,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache strict = CreateCache(
            future,
            FontLoadMode.Strict,
            FontLimits.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(SfntNameTableParser.FindTable(compatible)!.GetVersion(), Is.EqualTo(2));
            Assert.That(SfntNameTableParser.FindView(compatible)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(SfntNameTableParser.FindTable(strict), Is.Null);
            Assert.That(SfntNameTableParser.FindView(strict)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.RawOnly));
        });
    }

    [Test]
    public void AccessorsRejectInvalidIndexes()
    {
        SfntNameTable? table = Parse(BuildNameTable(
            1,
            new int[][] { Record(3, 1, 0x8000, 1, 0) },
            new byte[][] { Utf16("Name") },
            new string[] { "en" }));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetRecord(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetRecord(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetLanguageTagRecord(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetLanguageTagRecord(1));
        });
    }

    [Test]
    public void FaceCachesNameTableAndMissingTableReturnsNoView()
    {
        byte[] bytes = BuildNameTable(
            0,
            Array.Empty<int[]>(),
            Array.Empty<byte[]>(),
            Array.Empty<string>());
        SfntTableCache cache = CreateCache(
            bytes,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntValidationResult validation = new SfntValidationResult(
            FontOutlineKind.Cff1,
            Array.Empty<FontDiagnostic>());
        FontFace face = new FontFace(0, 0x4F54544FU, validation, cache);
        SfntNameTable? first = face.GetNameTable();
        SfntTableStore emptyStore = new SfntTableStore(
            Array.Empty<SfntTableData>(),
            Array.Empty<FontDiagnostic>());
        SfntTableCache emptyCache = SfntTableCache.Create(
            emptyStore,
            FontLoadMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetNameTable(), Is.SameAs(first));
            Assert.That(face.GetNameTableView()!.GetParsedData(), Is.SameAs(first));
            Assert.That(SfntNameTableParser.FindTable(emptyCache), Is.Null);
            Assert.That(SfntNameTableParser.FindView(emptyCache), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntNameTableParser.FindView(null!));
        });
    }

    static IEnumerable<byte[]> InvalidUtf16Strings()
    {
        yield return new byte[] { 0 };
        yield return new byte[] { 0xD8, 0x00 };
        yield return new byte[] { 0xDC, 0x00 };
        yield return new byte[] { 0xD8, 0x00, 0, 0x41 };
    }

    static SfntNameTable? Parse(byte[] bytes)
    {
        return SfntNameTableParser.FindTable(CreateCache(
            bytes,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[] bytes,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (bytes != null)
        {
            FontData data = FontData.Create(bytes);
            tables.Add(new SfntTableData(
                FontTags.Name,
                FontChecksum.CalculateTable(bytes),
                data.GetSlice(0, data.GetLength()),
                SfntTableClassification.Known,
                true));
        }

        SfntTableStore store = new SfntTableStore(
            tables.ToArray(),
            Array.Empty<FontDiagnostic>());
        return SfntTableCache.Create(store, mode, limits);
    }

    static int[] Record(
        int platformId,
        int encodingId,
        int languageId,
        int nameId,
        int stringIndex)
    {
        return new int[]
        {
            platformId,
            encodingId,
            languageId,
            nameId,
            stringIndex
        };
    }

    static byte[] BuildNameTable(
        int version,
        int[][] records,
        byte[][] strings,
        string[] languageTags)
    {
        int storageOffset = 6 + records.Length * 12;

        if (version >= 1)
        {
            storageOffset += 2 + languageTags.Length * 4;
        }

        int[] stringOffsets = new int[strings.Length];
        int nextOffset = 0;

        for (int index = 0; index < strings.Length; index += 1)
        {
            stringOffsets[index] = nextOffset;
            nextOffset += strings[index].Length;
        }

        int[] languageTagOffsets = new int[languageTags.Length];

        for (int index = 0; index < languageTags.Length; index += 1)
        {
            languageTagOffsets[index] = nextOffset;
            nextOffset += Utf16(languageTags[index]).Length;
        }

        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)version)
            .WriteUInt16((ushort)records.Length)
            .WriteUInt16((ushort)storageOffset);

        for (int index = 0; index < records.Length; index += 1)
        {
            int[] record = records[index];
            byte[] value = strings[record[4]];
            builder
                .WriteUInt16((ushort)record[0])
                .WriteUInt16((ushort)record[1])
                .WriteUInt16((ushort)record[2])
                .WriteUInt16((ushort)record[3])
                .WriteUInt16((ushort)value.Length)
                .WriteUInt16((ushort)stringOffsets[record[4]]);
        }

        if (version >= 1)
        {
            builder.WriteUInt16((ushort)languageTags.Length);

            for (int index = 0; index < languageTags.Length; index += 1)
            {
                byte[] encoded = Utf16(languageTags[index]);
                builder
                    .WriteUInt16((ushort)encoded.Length)
                    .WriteUInt16((ushort)languageTagOffsets[index]);
            }
        }

        for (int index = 0; index < strings.Length; index += 1)
        {
            builder.WriteBytes(strings[index]);
        }

        for (int index = 0; index < languageTags.Length; index += 1)
        {
            builder.WriteBytes(Utf16(languageTags[index]));
        }

        return builder.CopyBytes();
    }

    static byte[] Utf16(string value)
    {
        return Encoding.BigEndianUnicode.GetBytes(value);
    }

    static FontLimits CreateLimits(int maximumStringBytes)
    {
        return CreateLimits(maximumStringBytes, 2000000);
    }

    static FontLimits CreateLimits(
        int maximumStringBytes,
        int maximumCoverageEntries)
    {
        return FontLimits.Create(
            268435456,
            536870912,
            256,
            1024,
            65535,
            1048576,
            32767,
            64,
            10,
            32,
            16384,
            5000000,
            maximumCoverageEntries,
            maximumStringBytes,
            536870912,
            250,
            65536,
            128,
            4096);
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, 2), (ushort)value);
    }
}
