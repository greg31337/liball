using ttf;

namespace ttf.Tests;

internal class Cff2ReaderTests
{
    [Test]
    public void ParsesHeaderTopDictionaryIndexesAndCrossReferences()
    {
        byte[] bytes = BuildCompleteCff2(4);
        SfntCff2Table? table = Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetHeader().GetMajorVersion(), Is.EqualTo(2));
            Assert.That(table.GetHeader().GetMinorVersion(), Is.Zero);
            Assert.That(table.GetHeader().GetHeaderSize(), Is.EqualTo(5));
            Assert.That(table.GetHeader().GetTopDictionarySize(), Is.EqualTo(26));
            Assert.That(table.GetGlobalSubroutineIndex().GetCount(), Is.Zero);
            Assert.That(table.GetCharStringsIndex().GetCount(), Is.EqualTo(3));
            Assert.That(table.GetCharStringsIndex().CopyObject(1),
                Is.EqualTo(new byte[] { 14 }));
            Assert.That(table.GetFontDictionaryArray().GetCount(), Is.EqualTo(2));
            Assert.That(table.GetFontDictionarySelect()!.GetFormat(), Is.EqualTo(4));
            Assert.That(table.GetFontDictionarySelect()!.GetEncodedLength(), Is.EqualTo(21));
            Assert.That(table.GetFontDictionaryForGlyph(0),
                Is.SameAs(table.GetFontDictionaryArray().GetRecord(0)));
            Assert.That(table.GetFontDictionaryForGlyph(2),
                Is.SameAs(table.GetFontDictionaryArray().GetRecord(1)));
            Assert.That(table.GetVariationStore()!.GetLength(), Is.EqualTo(4));
            Assert.That(table.GetVariationStore()!.CopyData(),
                Is.EqualTo(new byte[] { 0, 1, 0, 0 }));
            Assert.That(table.GetTopDictionary().CopyFontMatrix(),
                Is.EqualTo(new double[] { 0.001, 0, 0, 0.001, 0, 0 }));
        });
    }

    [Test]
    public void ParsesCff2IndexWithWideCountAndEmptyObjects()
    {
        byte[] indexBytes = BuildIndex(
            2,
            new byte[] { 7 },
            Array.Empty<byte>(),
            new byte[] { 8, 9 });
        byte[] wrapped = Join(new byte[] { 99 }, indexBytes, new byte[] { 100 });
        Cff2Index index = Cff2IndexParser.Parse(Slice(wrapped), 1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(index.GetCount(), Is.EqualTo(3));
            Assert.That(index.GetStartOffset(), Is.EqualTo(1));
            Assert.That(index.GetOffsetSize(), Is.EqualTo(2));
            Assert.That(index.UsesMinimalOffsetSize(), Is.False);
            Assert.That(index.HasEmptyObjects(), Is.True);
            Assert.That(index.GetObjectLength(1), Is.Zero);
            Assert.That(index.CopyObject(2), Is.EqualTo(new byte[] { 8, 9 }));
            Assert.That(index.GetNextOffset(), Is.EqualTo(1 + indexBytes.Length));
        });
    }

    [Test]
    public void EmptyCff2IndexConsumesOnlyFourByteCount()
    {
        Cff2Index index = Cff2IndexParser.Parse(
            Slice(new byte[] { 0, 0, 0, 0, 55 }),
            0,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(index.IsEmpty(), Is.True);
            Assert.That(index.GetLength(), Is.EqualTo(4));
            Assert.That(index.GetNextOffset(), Is.EqualTo(4));
            Assert.That(index.IsCanonical(), Is.True);
        });
    }

    [Test]
    public void RejectsMalformedCff2Indexes()
    {
        byte[][] invalid = new byte[][]
        {
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 0, 1, 0, 1, 1 },
            new byte[] { 0, 0, 0, 1, 5, 1, 1 },
            new byte[] { 0, 0, 0, 1, 1, 0, 1 },
            new byte[] { 0, 0, 0, 1, 1, 1, 3, 7 },
            new byte[] { 0, 0, 0, 2, 1, 1, 3, 2, 7 }
        };

        Assert.Multiple(() =>
        {
            foreach (byte[] bytes in invalid)
            {
                Assert.Throws<FontFormatException>(() =>
                    Cff2IndexParser.Parse(Slice(bytes), 0, 10));
            }

            Assert.Throws<FontLimitException>(() =>
                Cff2IndexParser.Parse(
                    Slice(new byte[] { 0, 0, 0, 2, 1, 1, 1, 1 }),
                    0,
                    1));
        });
    }

    [Test]
    public void ParsesEveryCff2FdSelectFormat()
    {
        Cff2FontDictionarySelect format0 =
            Cff2FontDictionarySelectParser.Parse(
                Slice(new byte[] { 0, 0, 1, 1 }),
                0,
                3,
                2);
        Cff2FontDictionarySelect format3 =
            Cff2FontDictionarySelectParser.Parse(
                Slice(new byte[]
                {
                    3, 0, 2,
                    0, 0, 0,
                    0, 1, 1,
                    0, 3
                }),
                0,
                3,
                2);
        Cff2FontDictionarySelect format4 =
            Cff2FontDictionarySelectParser.Parse(
                Slice(BuildFdSelect4()),
                0,
                3,
                2);

        Assert.Multiple(() =>
        {
            Assert.That(format0.GetEncodedLength(), Is.EqualTo(4));
            Assert.That(format3.GetEncodedLength(), Is.EqualTo(11));
            Assert.That(format4.GetEncodedLength(), Is.EqualTo(21));
            Assert.That(format0.GetFontDictionaryIndex(0), Is.Zero);
            Assert.That(format3.GetFontDictionaryIndex(1), Is.EqualTo(1));
            Assert.That(format4.GetFontDictionaryIndex(2), Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsMalformedCff2FdSelectData()
    {
        byte[][] invalid = new byte[][]
        {
            new byte[] { 2 },
            new byte[] { 0, 0, 2, 0 },
            new byte[] { 3, 0, 0 },
            new byte[] { 3, 0, 1, 0, 1, 0, 0, 3 },
            new byte[] { 3, 0, 1, 0, 0, 0, 0, 2 },
            new byte[] { 4, 0, 0, 0, 1, 0, 0, 0, 0, 0, 2,
                0, 0, 0, 3 }
        };

        Assert.Multiple(() =>
        {
            foreach (byte[] bytes in invalid)
            {
                Assert.Throws<FontFormatException>(() =>
                    Cff2FontDictionarySelectParser.Parse(
                        Slice(bytes),
                        0,
                        3,
                        2));
            }
        });
    }

    [Test]
    public void ParsesTopFontAndPrivateDictionaryValues()
    {
        Cff2TopDictionary top = Cff2TopDictionary.Parse(Slice(Join(
            Integer(40), new byte[] { 17 },
            Integer(80), new byte[] { 24 },
            Integer(50), new byte[] { 12, 36 })));
        Cff2FontDictionary font = Cff2FontDictionary.Parse(Slice(Join(
            Integer(5), Integer(100), new byte[] { 18 })));
        Cff2PrivateDictionary privateDictionary =
            Cff2PrivateDictionary.Parse(Slice(new byte[]
            {
                144, 22,
                149, 19,
                30, 0x0A, 0x03, 0x96, 0x25, 0xFF, 12, 9
            }));

        Assert.Multiple(() =>
        {
            Assert.That(top.GetCharStringsOffset(), Is.EqualTo(40));
            Assert.That(top.GetVariationStoreOffset(), Is.EqualTo(80));
            Assert.That(top.GetFontDictionaryArrayOffset(), Is.EqualTo(50));
            Assert.That(font.GetPrivateDictionarySize(), Is.EqualTo(5));
            Assert.That(font.GetPrivateDictionaryOffset(), Is.EqualTo(100));
            Assert.That(privateDictionary.GetVariationStoreDataIndex(), Is.EqualTo(5));
            Assert.That(privateDictionary.GetLocalSubroutinesOffset(), Is.EqualTo(10));
            Assert.That(privateDictionary.GetBlueScale(), Is.EqualTo(0.039625).Within(0.000000001));
        });
    }

    [Test]
    public void PreservesBlendedPrivateDictionaryForLaterInstanceEvaluation()
    {
        byte[] bytes = new byte[] { 139, 140, 140, 23, 6 };
        Cff2PrivateDictionary dictionary =
            Cff2PrivateDictionary.Parse(Slice(bytes));

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.HasBlend(), Is.True);
            Assert.That(dictionary.GetData()!.CopyBytes(), Is.EqualTo(bytes));
            Assert.Throws<InvalidOperationException>(() =>
                dictionary.CopyBlueValues());
        });
    }

    [Test]
    public void RejectsMalformedHeaderAndDictionaryReferences()
    {
        byte[] complete = BuildCompleteCff2(4);

        Assert.Multiple(() =>
        {
            for (int length = 0; length < 5; length += 1)
            {
                Assert.Throws<FontFormatException>(() =>
                    Parse(complete.AsSpan(0, length).ToArray()));
            }

            Assert.Throws<FontFormatException>(() =>
                Parse(new byte[] { 2, 0, 4, 0, 1, 17, 0, 0, 0, 0 }));
            Assert.Throws<FontFormatException>(() =>
                Parse(new byte[] { 2, 0, 5, 0, 0 }));
            Assert.Throws<FontFormatException>(() =>
                Cff2TopDictionary.Parse(Slice(new byte[] { 139, 17 })));
            Assert.Throws<FontFormatException>(() =>
                Cff2FontDictionary.Parse(Slice(new byte[] { 139, 18 })));
            Assert.Throws<FontFormatException>(() =>
                Cff2PrivateDictionary.Parse(Slice(new byte[] { 138, 22 })));
        });
    }

    [Test]
    public void RejectsTruncatedVariationStoreAndInvalidFdSelectReference()
    {
        byte[] truncatedStore = BuildCompleteCff2(20);
        byte[] invalidSelection = BuildCompleteCff2(4);
        int fdSelectOffset = ReadTopOffset(invalidSelection, 24);
        invalidSelection[fdSelectOffset + 10] = 2;

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                Parse(truncatedStore)!.GetVariationStore());
            Assert.Throws<FontFormatException>(() =>
                Parse(invalidSelection)!.GetFontDictionarySelect());
        });
    }

    [Test]
    public void CompatibleMinorVersionParsesWhileStrictModeKeepsItRaw()
    {
        byte[] bytes = BuildCompleteCff2(4);
        bytes[1] = 1;
        SfntTableCache compatible = CreateCache(bytes, FontLoadMode.Compatible);
        SfntTableCache strict = CreateCache(bytes, FontLoadMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(SfntCff2TableParser.FindTable(compatible), Is.Not.Null);
            Assert.That(SfntCff2TableParser.FindView(compatible)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(SfntCff2TableParser.FindTable(strict), Is.Null);
            Assert.That(SfntCff2TableParser.FindView(strict)!.HasParsedData(), Is.False);
        });
    }

    static byte[] BuildCompleteCff2(int declaredVariationLength)
    {
        byte[] charStrings = BuildIndex(
            1,
            new byte[] { 14 },
            new byte[] { 14 },
            new byte[] { 14 });
        int topSize = 26;
        int fixedEnd = 5 + topSize + 4;
        int charStringsOffset = fixedEnd;
        int fdArrayOffset = charStringsOffset + charStrings.Length;
        int fdArrayLength = 4 + 1 + 3 + 22;
        int fdSelectOffset = fdArrayOffset + fdArrayLength;
        byte[] fdSelect = BuildFdSelect4();
        int variationOffset = fdSelectOffset + fdSelect.Length;
        int private0Offset = variationOffset + 6;
        int private1Offset = private0Offset + 2;
        byte[] fdArray = BuildIndex(
            1,
            Join(Integer(2), Integer(private0Offset), new byte[] { 18 }),
            Join(Integer(2), Integer(private1Offset), new byte[] { 18 }));
        byte[] top = Join(
            Integer(charStringsOffset), new byte[] { 17 },
            Integer(variationOffset), new byte[] { 24 },
            Integer(fdArrayOffset), new byte[] { 12, 36 },
            Integer(fdSelectOffset), new byte[] { 12, 37 });
        byte[] variation = new byte[]
        {
            (byte)(declaredVariationLength >> 8),
            (byte)declaredVariationLength,
            0, 1, 0, 0
        };
        return Join(
            new byte[] { 2, 0, 5, 0, (byte)top.Length },
            top,
            new byte[] { 0, 0, 0, 0 },
            charStrings,
            fdArray,
            fdSelect,
            variation,
            new byte[] { 139, 22 },
            new byte[] { 139, 22 });
    }

    static byte[] BuildFdSelect4()
    {
        return new byte[]
        {
            4,
            0, 0, 0, 2,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 1, 0, 1,
            0, 0, 0, 3
        };
    }

    static byte[] BuildIndex(int offsetSize, params byte[][] objects)
    {
        List<byte> bytes = new List<byte>();
        WriteUInt32(bytes, objects.Length);

        if (objects.Length == 0)
        {
            return bytes.ToArray();
        }

        bytes.Add((byte)offsetSize);
        int offset = 1;
        WriteOffset(bytes, offset, offsetSize);

        foreach (byte[] value in objects)
        {
            offset += value.Length;
            WriteOffset(bytes, offset, offsetSize);
        }

        foreach (byte[] value in objects)
        {
            bytes.AddRange(value);
        }

        return bytes.ToArray();
    }

    static byte[] Integer(int value)
    {
        return new byte[]
        {
            29,
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
    }

    static int ReadTopOffset(byte[] bytes, int operandOffset)
    {
        return (bytes[operandOffset + 1] << 24) |
            (bytes[operandOffset + 2] << 16) |
            (bytes[operandOffset + 3] << 8) |
            bytes[operandOffset + 4];
    }

    static void WriteOffset(List<byte> bytes, int value, int size)
    {
        for (int shift = (size - 1) * 8; shift >= 0; shift -= 8)
        {
            bytes.Add((byte)(value >> shift));
        }
    }

    static void WriteUInt32(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static byte[] Join(params byte[][] values)
    {
        return values.SelectMany(value => value).ToArray();
    }

    static FontDataSlice Slice(byte[] bytes)
    {
        return FontData.Create(bytes).GetSlice(0, bytes.Length);
    }

    static SfntCff2Table? Parse(byte[] bytes)
    {
        return SfntCff2TableParser.FindTable(
            CreateCache(bytes, FontLoadMode.Strict));
    }

    static SfntTableCache CreateCache(byte[] bytes, FontLoadMode mode)
    {
        FontData fontData = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            FontTags.Cff2,
            FontChecksum.CalculateTable(bytes),
            fontData.GetSlice(0, bytes.Length),
            SfntTableClassification.Known,
            true);
        return SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            mode,
            mode == FontLoadMode.Strict ?
                FontLimits.CreateStrict() : FontLimits.CreateCompatible());
    }
}
