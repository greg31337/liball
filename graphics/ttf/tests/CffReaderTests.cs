using ttf;

namespace ttf.Tests;

internal class CffReaderTests
{
    [Test]
    public void ParsesHeaderAndAllFixedLocationIndexes()
    {
        byte[] cff = BuildCff(
            1,
            0,
            1,
            Array.Empty<byte>(),
            BuildIndex(1, new byte[] { (byte)'T', (byte)'e', (byte)'s', (byte)'t' }),
            BuildIndex(1, new byte[] { 139, 17 }),
            BuildIndex(
                1,
                new byte[] { (byte)'A' },
                new byte[] { (byte)'B', (byte)'C' }),
            BuildIndex(1, new byte[] { 11 }),
            new byte[] { 9, 8, 7 });
        SfntCffTable? table = Parse(cff);
        CffHeader header = table!.GetHeader();
        CffIndex names = table.GetNameIndex();
        CffIndex top = table.GetTopDictionaryIndex();
        CffIndex strings = table.GetStringIndex();
        CffIndex globalSubroutines = table.GetGlobalSubroutineIndex();

        Assert.Multiple(() =>
        {
            Assert.That(header.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(header.GetMinorVersion(), Is.Zero);
            Assert.That(header.GetHeaderSize(), Is.EqualTo(4));
            Assert.That(header.GetAbsoluteOffsetSize(), Is.EqualTo(1));
            Assert.That(header.GetExtensionLength(), Is.Zero);
            Assert.That(names.GetStartOffset(), Is.EqualTo(4));
            Assert.That(names.GetCount(), Is.EqualTo(1));
            Assert.That(names.GetOffsetSize(), Is.EqualTo(1));
            Assert.That(names.GetEncodedOffset(0), Is.EqualTo(1));
            Assert.That(names.GetEncodedOffset(1), Is.EqualTo(5));
            Assert.That(names.GetObjectLength(0), Is.EqualTo(4));
            Assert.That(names.CopyObject(0), Is.EqualTo(new byte[]
            {
                (byte)'T', (byte)'e', (byte)'s', (byte)'t'
            }));
            Assert.That(names.IsCanonical(), Is.True);
            Assert.That(top.CopyObject(0), Is.EqualTo(new byte[] { 139, 17 }));
            Assert.That(strings.GetCount(), Is.EqualTo(2));
            Assert.That(strings.CopyObject(0), Is.EqualTo(new byte[] { (byte)'A' }));
            Assert.That(strings.CopyObject(1), Is.EqualTo(new byte[] { (byte)'B', (byte)'C' }));
            Assert.That(globalSubroutines.CopyObject(0), Is.EqualTo(new byte[] { 11 }));
            Assert.That(table.GetRemainingDataLength(), Is.EqualTo(3));
            Assert.That(table.GetRemainingData().CopyBytes(), Is.EqualTo(new byte[] { 9, 8, 7 }));
        });
    }

    [Test]
    public void EmptyIndexConsumesOnlyItsCountField()
    {
        byte[] bytes = new byte[] { 4, 5, 0, 0, 99 };
        CffIndex index = CffIndexParser.Parse(CreateSlice(bytes), 2);

        Assert.Multiple(() =>
        {
            Assert.That(index.IsEmpty(), Is.True);
            Assert.That(index.GetCount(), Is.Zero);
            Assert.That(index.GetLength(), Is.EqualTo(2));
            Assert.That(index.GetNextOffset(), Is.EqualTo(4));
            Assert.That(index.GetDataOffset(), Is.EqualTo(4));
            Assert.That(index.GetOffsetSize(), Is.Zero);
            Assert.That(index.IsCanonical(), Is.True);
            Assert.That(index.HasEmptyObjects(), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => index.GetEncodedOffset(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => index.GetObject(0));
        });
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void AcceptsWideNonCanonicalOffsetsAndZeroLengthObjects(int offsetSize)
    {
        byte[] encoded = BuildIndex(
            offsetSize,
            new byte[] { 1 },
            Array.Empty<byte>(),
            new byte[] { 2, 3 });
        byte[] wrapped = Join(new byte[] { 7, 8 }, encoded, new byte[] { 9 });
        CffIndex index = CffIndexParser.Parse(CreateSlice(wrapped), 2);

        Assert.Multiple(() =>
        {
            Assert.That(index.GetCount(), Is.EqualTo(3));
            Assert.That(index.GetOffsetSize(), Is.EqualTo(offsetSize));
            Assert.That(index.UsesMinimalOffsetSize(), Is.False);
            Assert.That(index.IsCanonical(), Is.False);
            Assert.That(index.HasEmptyObjects(), Is.True);
            Assert.That(index.GetObjectLength(1), Is.Zero);
            Assert.That(index.CopyObject(0), Is.EqualTo(new byte[] { 1 }));
            Assert.That(index.CopyObject(1), Is.Empty);
            Assert.That(index.CopyObject(2), Is.EqualTo(new byte[] { 2, 3 }));
            Assert.That(index.GetNextOffset(), Is.EqualTo(2 + encoded.Length));
        });
    }

    [Test]
    public void ParsesHeaderExtensionsUsingDeclaredHeaderSize()
    {
        byte[] cff = BuildCff(
            1,
            1,
            2,
            new byte[] { 0xAA, 0xBB },
            BuildIndex(2, new byte[] { (byte)'X' }),
            BuildIndex(2, new byte[] { 17 }),
            BuildIndex(2),
            BuildIndex(2),
            Array.Empty<byte>());
        SfntTableCache compatible = CreateCache(
            cff,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache strict = CreateCache(
            cff,
            FontLoadMode.Strict,
            FontLimits.CreateStrict());
        SfntCffTable? table = SfntCffTableParser.FindTable(compatible);
        byte[] extension = table!.GetHeader().CopyExtensionData();
        extension[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(table.GetHeader().GetMinorVersion(), Is.EqualTo(1));
            Assert.That(table.GetHeader().GetHeaderSize(), Is.EqualTo(6));
            Assert.That(table.GetHeader().GetAbsoluteOffsetSize(), Is.EqualTo(2));
            Assert.That(table.GetHeader().CopyExtensionData(),
                Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(table.GetNameIndex().GetStartOffset(), Is.EqualTo(6));
            Assert.That(SfntCffTableParser.FindView(compatible)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(SfntCffTableParser.FindTable(strict), Is.Null);
            Assert.That(SfntCffTableParser.FindView(strict)!.HasParsedData(), Is.False);
        });
    }

    [Test]
    public void KeepsUnknownMajorVersionRaw()
    {
        byte[] cff = new byte[] { 2, 0, 5, 0, 0 };
        SfntTableCache cache = CreateCache(
            cff,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(SfntCffTableParser.FindTable(cache), Is.Null);
            Assert.That(SfntCffTableParser.FindView(cache)!.HasParsedData(), Is.False);
            Assert.That(SfntCffTableParser.FindView(cache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.RawOnly));
        });
    }

    [Test]
    public void RejectsMalformedHeadersAndEveryHeaderTruncation()
    {
        byte[] complete = BuildMinimalCff();

        for (int length = 0; length < 4; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(
                () => Parse(truncated),
                "Length {0}",
                length);
        }

        byte[] smallHeader = complete.ToArray();
        smallHeader[2] = 3;
        byte[] oversizedHeader = complete.ToArray();
        oversizedHeader[2] = (byte)(complete.Length + 1);
        byte[] zeroOffsetSize = complete.ToArray();
        zeroOffsetSize[3] = 0;
        byte[] largeOffsetSize = complete.ToArray();
        largeOffsetSize[3] = 5;

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(smallHeader));
            Assert.Throws<FontFormatException>(() => Parse(oversizedHeader));
            Assert.Throws<FontFormatException>(() => Parse(zeroOffsetSize));
            Assert.Throws<FontFormatException>(() => Parse(largeOffsetSize));
        });
    }

    [Test]
    public void RejectsMalformedIndexOffsetStructures()
    {
        byte[][] invalid = new byte[][]
        {
            new byte[] { 0, 1 },
            new byte[] { 0, 1, 0, 1, 1 },
            new byte[] { 0, 1, 5, 1, 1 },
            new byte[] { 0, 1, 1, 0, 1 },
            new byte[] { 0, 1, 1, 2, 2 },
            new byte[] { 0, 2, 1, 1, 3, 2, 7 },
            new byte[] { 0, 1, 1, 1, 4, 7 },
            new byte[] { 0, 1, 4, 0x80, 0, 0, 0, 0x80, 0, 0, 1 }
        };

        Assert.Multiple(() =>
        {
            foreach (byte[] bytes in invalid)
            {
                Assert.Throws<FontFormatException>(() =>
                    CffIndexParser.Parse(CreateSlice(bytes), 0));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CffIndexParser.Parse(CreateSlice(new byte[] { 0, 0 }), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CffIndexParser.Parse(CreateSlice(new byte[] { 0, 0 }), 3));
            Assert.Throws<ArgumentNullException>(() =>
                CffIndexParser.Parse(null!, 0));
        });
    }

    [Test]
    public void RejectsEveryTruncationOfNonemptyIndex()
    {
        byte[] complete = BuildIndex(
            2,
            new byte[] { 1, 2 },
            new byte[] { 3, 4, 5 });

        for (int length = 0; length < complete.Length; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(
                () => CffIndexParser.Parse(CreateSlice(truncated), 0),
                "Length {0}",
                length);
        }
    }

    [Test]
    public void EnforcesOpenTypeFontSetCountsAndStringLimits()
    {
        byte[] noNames = BuildCffWithIndexes(
            BuildIndex(1),
            BuildIndex(1, new byte[] { 17 }),
            BuildIndex(1),
            BuildIndex(1));
        byte[] twoNames = BuildCffWithIndexes(
            BuildIndex(1, new byte[] { 65 }, new byte[] { 66 }),
            BuildIndex(1, new byte[] { 17 }, new byte[] { 17 }),
            BuildIndex(1),
            BuildIndex(1));
        byte[] noTopDictionary = BuildCffWithIndexes(
            BuildIndex(1, new byte[] { 65 }),
            BuildIndex(1),
            BuildIndex(1),
            BuildIndex(1));
        byte[] longString = BuildCffWithIndexes(
            BuildIndex(1, new byte[] { 65 }),
            BuildIndex(1, new byte[] { 17 }),
            BuildIndex(1, new byte[] { 1, 2, 3, 4 }),
            BuildIndex(1));
        byte[] tooManyStrings = BuildCffWithIndexes(
            BuildIndex(1, new byte[] { 65 }),
            BuildIndex(1, new byte[] { 17 }),
            BuildIndex(1, new byte[] { 1 }, new byte[] { 2 }),
            BuildIndex(1));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(noNames));
            Assert.Throws<FontFormatException>(() => Parse(twoNames));
            Assert.Throws<FontFormatException>(() => Parse(noTopDictionary));
            Assert.Throws<FontLimitException>(() =>
                SfntCffTableParser.FindTable(CreateCache(
                    longString,
                    FontLoadMode.Compatible,
                    CreateLimits(3))));
            Assert.Throws<FontLimitException>(() =>
                SfntCffTableParser.FindTable(CreateCache(
                    tooManyStrings,
                    FontLoadMode.Compatible,
                    CreateLimits(10, 1))));
        });
    }

    [Test]
    public void AccessorsRejectInvalidIndexesAndFaceCachesTable()
    {
        byte[] cff = BuildMinimalCff();
        SfntTableCache cache = CreateCache(
            cff,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(
                FontOutlineKind.Cff1,
                Array.Empty<FontDiagnostic>()),
            cache);
        SfntCffTable? table = face.GetCffTable();
        CffIndex names = table!.GetNameIndex();
        SfntTableCache missing = CreateMissingCache();

        Assert.Multiple(() =>
        {
            Assert.That(face.GetCffTable(), Is.SameAs(table));
            Assert.That(face.GetCffTableView()!.GetParsedData(), Is.SameAs(table));
            Assert.Throws<ArgumentOutOfRangeException>(() => names.GetEncodedOffset(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => names.GetEncodedOffset(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => names.GetObject(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => names.GetObject(1));
            Assert.That(SfntCffTableParser.FindTable(missing), Is.Null);
            Assert.That(SfntCffTableParser.FindView(missing), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntCffTableParser.FindView(null!));
        });
    }

    static SfntCffTable? Parse(byte[] bytes)
    {
        return SfntCffTableParser.FindTable(CreateCache(
            bytes,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static byte[] BuildMinimalCff()
    {
        return BuildCffWithIndexes(
            BuildIndex(1, new byte[] { (byte)'X' }),
            BuildIndex(1, new byte[] { 17 }),
            BuildIndex(1),
            BuildIndex(1));
    }

    static byte[] BuildCffWithIndexes(
        byte[] names,
        byte[] topDictionary,
        byte[] strings,
        byte[] globalSubroutines)
    {
        return BuildCff(
            1,
            0,
            1,
            Array.Empty<byte>(),
            names,
            topDictionary,
            strings,
            globalSubroutines,
            Array.Empty<byte>());
    }

    static byte[] BuildCff(
        int majorVersion,
        int minorVersion,
        int absoluteOffsetSize,
        byte[] headerExtension,
        byte[] names,
        byte[] topDictionary,
        byte[] strings,
        byte[] globalSubroutines,
        byte[] remaining)
    {
        return new TestBinaryBuilder()
            .WriteUInt8((byte)majorVersion)
            .WriteUInt8((byte)minorVersion)
            .WriteUInt8((byte)(4 + headerExtension.Length))
            .WriteUInt8((byte)absoluteOffsetSize)
            .WriteBytes(headerExtension)
            .WriteBytes(names)
            .WriteBytes(topDictionary)
            .WriteBytes(strings)
            .WriteBytes(globalSubroutines)
            .WriteBytes(remaining)
            .CopyBytes();
    }

    static byte[] BuildIndex(int offsetSize, params byte[][] objects)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)objects.Length);

        if (objects.Length == 0)
        {
            return builder.CopyBytes();
        }

        builder.WriteUInt8((byte)offsetSize);
        uint offset = 1;
        WriteOffset(builder, offsetSize, offset);

        foreach (byte[] value in objects)
        {
            offset += (uint)value.Length;
            WriteOffset(builder, offsetSize, offset);
        }

        foreach (byte[] value in objects)
        {
            builder.WriteBytes(value);
        }

        return builder.CopyBytes();
    }

    static void WriteOffset(
        TestBinaryBuilder builder,
        int offsetSize,
        uint value)
    {
        if (offsetSize == 1)
        {
            builder.WriteUInt8((byte)value);
        }
        else if (offsetSize == 2)
        {
            builder.WriteUInt16((ushort)value);
        }
        else if (offsetSize == 3)
        {
            builder.WriteUInt24(value);
        }
        else
        {
            builder.WriteUInt32(value);
        }
    }

    static FontDataSlice CreateSlice(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return data.GetSlice(0, data.GetLength());
    }

    static SfntTableCache CreateCache(
        byte[] cff,
        FontLoadMode mode,
        FontLimits limits)
    {
        FontData data = FontData.Create(cff);
        SfntTableData table = new SfntTableData(
            FontTags.Cff,
            FontChecksum.CalculateTable(cff),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        return SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            mode,
            limits);
    }

    static SfntTableCache CreateMissingCache()
    {
        return SfntTableCache.Create(
            new SfntTableStore(
                Array.Empty<SfntTableData>(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
    }

    static FontLimits CreateLimits(int maxStringBytes)
    {
        return CreateLimits(maxStringBytes, 100000);
    }

    static FontLimits CreateLimits(
        int maxStringBytes,
        int maxCoverageEntries)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            65535,
            1000,
            1000,
            10,
            10,
            10,
            100,
            100000,
            maxCoverageEntries,
            maxStringBytes,
            1000000,
            100,
            1000,
            100,
            100);
    }

    static byte[] Join(params byte[][] values)
    {
        int length = 0;

        foreach (byte[] value in values)
        {
            length += value.Length;
        }

        byte[] result = new byte[length];
        int offset = 0;

        foreach (byte[] value in values)
        {
            value.CopyTo(result, offset);
            offset += value.Length;
        }

        return result;
    }
}
