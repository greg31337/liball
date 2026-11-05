using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class SfntOs2TableTests
{
    [Test]
    public void ParsesEveryCommonField()
    {
        SfntOs2Table? table = Parse(BuildOs2(5));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.EqualTo(5));
            Assert.That(table.GetAverageCharacterWidth(), Is.EqualTo(-123));
            Assert.That(table.GetWeightClass(), Is.EqualTo(650));
            Assert.That(table.GetWidthClass(), Is.EqualTo(7));
            Assert.That(table.GetEmbeddingFlags(), Is.Zero);
            Assert.That(table.GetSubscriptXSize(), Is.EqualTo(601));
            Assert.That(table.GetSubscriptYSize(), Is.EqualTo(602));
            Assert.That(table.GetSubscriptXOffset(), Is.EqualTo(-3));
            Assert.That(table.GetSubscriptYOffset(), Is.EqualTo(4));
            Assert.That(table.GetSuperscriptXSize(), Is.EqualTo(701));
            Assert.That(table.GetSuperscriptYSize(), Is.EqualTo(702));
            Assert.That(table.GetSuperscriptXOffset(), Is.EqualTo(-5));
            Assert.That(table.GetSuperscriptYOffset(), Is.EqualTo(6));
            Assert.That(table.GetStrikeoutSize(), Is.EqualTo(45));
            Assert.That(table.GetStrikeoutPosition(), Is.EqualTo(250));
            Assert.That(table.GetFamilyClassValue(), Is.EqualTo(0x0802));
            Assert.That(table.GetFamilyClass(), Is.EqualTo(8));
            Assert.That(table.GetFamilySubclass(), Is.EqualTo(2));
            Assert.That(table.GetVendorId(), Is.EqualTo(0x54455354U));
            Assert.That(table.GetSelectionFlags(), Is.Zero);
            Assert.That(table.GetFirstCharacterIndex(), Is.EqualTo(0x20));
            Assert.That(table.GetLastCharacterIndex(), Is.EqualTo(0xFFFD));
            Assert.That(table.GetExtensionLength(), Is.Zero);
        });

        for (int index = 0; index < 10; index += 1)
        {
            Assert.That(table!.GetPanoseValue(index), Is.EqualTo(index + 1));
        }

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetUnicodeRange(0), Is.EqualTo(0x80000001U));
            Assert.That(table.GetUnicodeRange(1), Is.EqualTo(0x40000002U));
            Assert.That(table.GetUnicodeRange(2), Is.EqualTo(0x20000004U));
            Assert.That(table.GetUnicodeRange(3), Is.EqualTo(0x04000008U));
            Assert.That(table.HasUnicodeRange(0), Is.True);
            Assert.That(table.HasUnicodeRange(63), Is.False);
            Assert.That(table.HasUnicodeRange(122), Is.True);
        });
    }

    [TestCase(0, 78, true, false, false, false)]
    [TestCase(1, 86, true, true, false, false)]
    [TestCase(2, 96, true, true, true, false)]
    [TestCase(3, 96, true, true, true, false)]
    [TestCase(4, 96, true, true, true, false)]
    [TestCase(5, 100, true, true, true, true)]
    public void ParsesEveryPublishedVersionAndItsDefinedLength(
        int version,
        int expectedLength,
        bool hasTypographicMetrics,
        bool hasCodePages,
        bool hasVersion2Metrics,
        bool hasOpticalSizes)
    {
        byte[] bytes = BuildOs2(version);
        SfntOs2Table? table = Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Length, Is.EqualTo(expectedLength));
            Assert.That(table!.GetVersion(), Is.EqualTo(version));
            Assert.That(table.HasTypographicMetrics(), Is.EqualTo(hasTypographicMetrics));
            Assert.That(table.HasCodePageRanges(), Is.EqualTo(hasCodePages));
            Assert.That(table.HasVersion2Metrics(), Is.EqualTo(hasVersion2Metrics));
            Assert.That(table.HasOpticalPointSizes(), Is.EqualTo(hasOpticalSizes));
        });
    }

    [Test]
    public void ParsesTypographicCodePageAndVersion2Metrics()
    {
        SfntOs2Table? version1 = Parse(BuildOs2(1));
        SfntOs2Table? version2 = Parse(BuildOs2(2));

        Assert.Multiple(() =>
        {
            Assert.That(version1!.GetTypographicAscender(), Is.EqualTo(900));
            Assert.That(version1.GetTypographicDescender(), Is.EqualTo(-300));
            Assert.That(version1.GetTypographicLineGap(), Is.EqualTo(20));
            Assert.That(version1.GetWindowsAscent(), Is.EqualTo(1000));
            Assert.That(version1.GetWindowsDescent(), Is.EqualTo(350));
            Assert.That(version1.GetCodePageRange(0), Is.EqualTo(0x00010001U));
            Assert.That(version1.GetCodePageRange(1), Is.EqualTo(0x80010000U));
            Assert.That(version1.HasCodePageRange(0), Is.True);
            Assert.That(version1.HasCodePageRange(48), Is.True);
            Assert.That(version1.HasCodePageRange(63), Is.True);
            Assert.That(version2!.GetXHeight(), Is.EqualTo(450));
            Assert.That(version2.GetCapHeight(), Is.EqualTo(700));
            Assert.That(version2.GetDefaultCharacter(), Is.Zero);
            Assert.That(version2.GetBreakCharacter(), Is.EqualTo(0x20));
            Assert.That(version2.GetMaximumContext(), Is.EqualTo(3));
        });
    }

    [Test]
    public void ParsesOpticalSizesAndVariationSelectionFlag()
    {
        byte[] finite = BuildOs2(5);
        WriteUInt16(finite, 62, 1 << 7);
        SfntOs2Table? finiteTable = Parse(finite);
        byte[] unbounded = BuildOs2(5);
        WriteUInt16(unbounded, 96, 0);
        WriteUInt16(unbounded, 98, 0xFFFF);
        SfntOs2Table? unboundedTable = Parse(unbounded);

        Assert.Multiple(() =>
        {
            Assert.That(finiteTable!.UsesTypographicMetrics(), Is.True);
            Assert.That(finiteTable.HasSelectionFlag(7), Is.True);
            Assert.That(finiteTable.GetLowerOpticalPointSize(), Is.EqualTo(160));
            Assert.That(finiteTable.GetUpperOpticalPointSize(), Is.EqualTo(720));
            Assert.That(finiteTable.GetLowerOpticalPointSizeInPoints(), Is.EqualTo(8.0));
            Assert.That(finiteTable.GetUpperOpticalPointSizeInPoints(), Is.EqualTo(36.0));
            Assert.That(unboundedTable!.GetLowerOpticalPointSizeInPoints(), Is.Zero);
            Assert.That(unboundedTable.GetUpperOpticalPointSizeInPoints(),
                Is.EqualTo(double.PositiveInfinity));
        });
    }

    [Test]
    public void ParsesLegacyShortVersion0WithoutUndefinedSuffix()
    {
        byte[] shortVersion = BuildOs2(0).AsSpan(0, 68).ToArray();
        SfntOs2Table? table = Parse(shortVersion);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.Zero);
            Assert.That(table.HasTypographicMetrics(), Is.False);
            Assert.That(table.HasCodePageRanges(), Is.False);
            Assert.That(table.GetExtensionLength(), Is.Zero);
            Assert.Throws<InvalidOperationException>(() => table.GetTypographicAscender());
            Assert.Throws<InvalidOperationException>(() => table.GetWindowsDescent());
            Assert.Throws<InvalidOperationException>(() => table.GetCodePageRange(0));
            Assert.Throws<InvalidOperationException>(() => table.HasCodePageRange(0));
        });
    }

    [Test]
    public void UndefinedVersionFieldsRejectAccess()
    {
        SfntOs2Table? version0 = Parse(BuildOs2(0));
        SfntOs2Table? version1 = Parse(BuildOs2(1));
        SfntOs2Table? version4 = Parse(BuildOs2(4));

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => version0!.GetCodePageRange(0));
            Assert.Throws<InvalidOperationException>(() => version0!.GetXHeight());
            Assert.Throws<InvalidOperationException>(() => version1!.GetCapHeight());
            Assert.Throws<InvalidOperationException>(() => version1!.GetMaximumContext());
            Assert.Throws<InvalidOperationException>(() => version4!.GetLowerOpticalPointSize());
            Assert.Throws<InvalidOperationException>(() => version4!.GetUpperOpticalPointSize());
        });
    }

    [TestCase(0, 67)]
    [TestCase(1, 85)]
    [TestCase(2, 95)]
    [TestCase(3, 95)]
    [TestCase(4, 95)]
    [TestCase(5, 99)]
    public void RejectsTruncationForEveryPublishedVersion(int version, int length)
    {
        byte[] bytes = BuildOs2(version);
        Assert.Throws<FontFormatException>(
            () => Parse(bytes.AsSpan(0, length).ToArray()));
    }

    [Test]
    public void RejectsEveryPartialLegacyVersion0Suffix()
    {
        byte[] bytes = BuildOs2(0);

        for (int length = 69; length < 78; length += 1)
        {
            Assert.Throws<FontFormatException>(
                () => Parse(bytes.AsSpan(0, length).ToArray()),
                "Length {0}",
                length);
        }
    }

    [Test]
    public void PreservesAllowedTrailingExtensionsAndGatesFutureVersions()
    {
        SfntOs2Table? version0 = Parse(Append(BuildOs2(0), new byte[] { 1, 2 }));
        SfntOs2Table? version4 = Parse(Append(BuildOs2(4), new byte[] { 3, 4, 5, 6 }));
        byte[] future = Append(BuildOs2(5), new byte[] { 7, 8 });
        WriteUInt16(future, 0, 6);
        SfntTableCache compatibleCache = CreateCache(
            future,
            null,
            FontLoadMode.Compatible);
        SfntTableCache strictCache = CreateCache(
            future,
            null,
            FontLoadMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(version0!.GetExtensionLength(), Is.EqualTo(2));
            Assert.That(version4!.GetExtensionLength(), Is.EqualTo(4));
            Assert.That(SfntOs2TableParser.FindTable(compatibleCache)!.GetVersion(),
                Is.EqualTo(6));
            Assert.That(SfntOs2TableParser.FindTable(compatibleCache)!.GetExtensionLength(),
                Is.EqualTo(2));
            Assert.That(SfntOs2TableParser.FindView(compatibleCache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(SfntOs2TableParser.FindTable(strictCache), Is.Null);
            Assert.That(SfntOs2TableParser.FindView(strictCache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.RawOnly));
        });
    }

    [TestCase(0)]
    [TestCase(1001)]
    public void RejectsInvalidWeightClasses(int weightClass)
    {
        byte[] bytes = BuildOs2(5);
        WriteUInt16(bytes, 4, weightClass);
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(0)]
    [TestCase(10)]
    public void RejectsInvalidWidthClasses(int widthClass)
    {
        byte[] bytes = BuildOs2(5);
        WriteUInt16(bytes, 6, widthClass);
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void ValidatesEmbeddingFlagsByTableVersion()
    {
        byte[] bitZero = BuildOs2(2);
        WriteUInt16(bitZero, 8, 1);
        byte[] reserved = BuildOs2(2);
        WriteUInt16(reserved, 8, 1 << 4);
        byte[] multipleVersion2 = BuildOs2(2);
        WriteUInt16(multipleVersion2, 8, 4 | 8);
        byte[] multipleVersion3 = BuildOs2(3);
        WriteUInt16(multipleVersion3, 8, 4 | 8);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(bitZero));
            Assert.Throws<FontFormatException>(() => Parse(reserved));
            Assert.That(Parse(multipleVersion2)!.GetEmbeddingFlags(), Is.EqualTo(12));
            Assert.Throws<FontFormatException>(() => Parse(multipleVersion3));
        });
    }

    [Test]
    public void ValidatesSelectionFlagsByTableVersion()
    {
        byte[] version3UseTypo = BuildOs2(3);
        WriteUInt16(version3UseTypo, 62, 1 << 7);
        byte[] version4Reserved = BuildOs2(4);
        WriteUInt16(version4Reserved, 62, 1 << 10);
        byte[] regularItalic = BuildOs2(5);
        WriteUInt16(regularItalic, 62, (1 << 6) | 1);
        byte[] regularBold = BuildOs2(5);
        WriteUInt16(regularBold, 62, (1 << 6) | (1 << 5));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(version3UseTypo));
            Assert.Throws<FontFormatException>(() => Parse(version4Reserved));
            Assert.Throws<FontFormatException>(() => Parse(regularItalic));
            Assert.Throws<FontFormatException>(() => Parse(regularBold));
        });
    }

    [Test]
    public void RejectsReservedUnicodeAndCodePageRangeBits()
    {
        byte[] unicode = BuildOs2(5);
        WriteUInt32(unicode, 54, 1U << 27);
        byte[] codePage1 = BuildOs2(2);
        WriteUInt32(codePage1, 78, 1U << 9);
        byte[] codePage2 = BuildOs2(2);
        WriteUInt32(codePage2, 82, 1U);
        byte[] version1Unassigned = BuildOs2(1);
        WriteUInt32(version1Unassigned, 78, 1U << 8);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(unicode));
            Assert.Throws<FontFormatException>(() => Parse(codePage1));
            Assert.Throws<FontFormatException>(() => Parse(codePage2));
            Assert.Throws<FontFormatException>(() => Parse(version1Unassigned));
        });
    }

    [Test]
    public void RejectsReversedCharacterAndOpticalSizeRanges()
    {
        byte[] characters = BuildOs2(5);
        WriteUInt16(characters, 64, 100);
        WriteUInt16(characters, 66, 99);
        byte[] equalOptical = BuildOs2(5);
        WriteUInt16(equalOptical, 96, 100);
        WriteUInt16(equalOptical, 98, 100);
        byte[] upperTooSmall = BuildOs2(5);
        WriteUInt16(upperTooSmall, 96, 0);
        WriteUInt16(upperTooSmall, 98, 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(characters));
            Assert.Throws<FontFormatException>(() => Parse(equalOptical));
            Assert.Throws<FontFormatException>(() => Parse(upperTooSmall));
        });
    }

    [Test]
    public void FlagAndArrayAccessRejectsInvalidIndexes()
    {
        SfntOs2Table? table = Parse(BuildOs2(5));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.HasEmbeddingFlag(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.HasEmbeddingFlag(16));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.HasSelectionFlag(16));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetPanoseValue(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetUnicodeRange(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.HasUnicodeRange(128));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetCodePageRange(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.HasCodePageRange(64));
        });
    }

    [Test]
    public void FaceCachesTableAndCrossChecksHeadStyleBits()
    {
        byte[] matching = BuildOs2(5);
        WriteUInt16(matching, 62, 1 | 1 << 5);
        FontFace face = CreateFace(matching, BuildHead(3));
        SfntOs2Table? first = face.GetOs2Table();

        Assert.Multiple(() =>
        {
            Assert.That(face.GetOs2Table(), Is.SameAs(first));
            Assert.That(face.GetOs2TableView()!.GetParsedData(), Is.SameAs(first));
            Assert.That(first!.HasSelectionFlag(0), Is.True);
            Assert.That(first.HasSelectionFlag(5), Is.True);
        });

        FontFace italicMismatch = CreateFace(matching, BuildHead(1));
        FontFace boldMismatch = CreateFace(matching, BuildHead(2));
        FontFace missingHead = CreateFace(matching, null);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => italicMismatch.GetOs2Table());
            Assert.Throws<FontFormatException>(() => boldMismatch.GetOs2Table());
            Assert.Throws<FontFormatException>(() => missingHead.GetOs2Table());
        });
    }

    [Test]
    public void MissingTableReturnsNoView()
    {
        SfntTableStore store = new SfntTableStore(
            Array.Empty<SfntTableData>(),
            Array.Empty<FontDiagnostic>());
        SfntTableCache cache = SfntTableCache.Create(store, FontLoadMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(SfntOs2TableParser.FindView(cache), Is.Null);
            Assert.That(SfntOs2TableParser.FindTable(cache), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntOs2TableParser.FindView(null!));
        });
    }

    static SfntOs2Table? Parse(byte[] bytes)
    {
        return SfntOs2TableParser.FindTable(CreateCache(
            bytes,
            null,
            FontLoadMode.Compatible));
    }

    static FontFace CreateFace(byte[] os2, byte[]? head)
    {
        SfntValidationResult validation = new SfntValidationResult(
            FontOutlineKind.Cff1,
            Array.Empty<FontDiagnostic>());
        return new FontFace(
            0,
            0x4F54544FU,
            validation,
            CreateCache(os2, head, FontLoadMode.Compatible));
    }

    static SfntTableCache CreateCache(
        byte[] os2,
        byte[]? head,
        FontLoadMode mode)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Os2, os2);
        AddTable(tables, FontTags.Head, head);
        SfntTableStore store = new SfntTableStore(
            tables.ToArray(),
            Array.Empty<FontDiagnostic>());
        return SfntTableCache.Create(store, mode);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[]? bytes)
    {
        if (bytes == null)
        {
            return;
        }

        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] BuildOs2(int version)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)version)
            .WriteInt16(-123)
            .WriteUInt16(650)
            .WriteUInt16(7)
            .WriteUInt16(0)
            .WriteInt16(601)
            .WriteInt16(602)
            .WriteInt16(-3)
            .WriteInt16(4)
            .WriteInt16(701)
            .WriteInt16(702)
            .WriteInt16(-5)
            .WriteInt16(6)
            .WriteInt16(45)
            .WriteInt16(250)
            .WriteInt16(0x0802);

        for (int index = 1; index <= 10; index += 1)
        {
            builder.WriteUInt8((byte)index);
        }

        builder
            .WriteUInt32(0x80000001U)
            .WriteUInt32(0x40000002U)
            .WriteUInt32(0x20000004U)
            .WriteUInt32(0x04000008U)
            .WriteUInt32(0x54455354U)
            .WriteUInt16(0)
            .WriteUInt16(0x20)
            .WriteUInt16(0xFFFD)
            .WriteInt16(900)
            .WriteInt16(-300)
            .WriteInt16(20)
            .WriteUInt16(1000)
            .WriteUInt16(350);

        if (version >= 1)
        {
            builder
                .WriteUInt32(0x00010001U)
                .WriteUInt32(0x80010000U);
        }

        if (version >= 2)
        {
            builder
                .WriteInt16(450)
                .WriteInt16(700)
                .WriteUInt16(0)
                .WriteUInt16(0x20)
                .WriteUInt16(3);
        }

        if (version >= 5)
        {
            builder
                .WriteUInt16(160)
                .WriteUInt16(720);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildHead(int macStyle)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U)
            .WriteUInt16(0)
            .WriteUInt16(1000)
            .WriteInt64(0)
            .WriteInt64(0)
            .WriteInt16(-20)
            .WriteInt16(-30)
            .WriteInt16(1000)
            .WriteInt16(900)
            .WriteUInt16((ushort)macStyle)
            .WriteUInt16(9)
            .WriteInt16(2)
            .WriteInt16(0)
            .WriteInt16(0)
            .CopyBytes();
    }

    static byte[] Append(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, 2), (ushort)value);
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value);
    }
}
