using ttf;

namespace ttf.Tests;

internal class CffCharsetEncodingTests
{
    [Test]
    public void ParsesAndTruncatesAllPredefinedCharsets()
    {
        CffStringTable strings = CreateStringTable();
        CffCharset isoAdobe = CffCharsetParser.Parse(
            CreateSlice(Array.Empty<byte>()), 0, 5, false, strings);
        CffCharset expert = CffCharsetParser.Parse(
            CreateSlice(Array.Empty<byte>()), 1, 4, false, strings);
        CffCharset subset = CffCharsetParser.Parse(
            CreateSlice(Array.Empty<byte>()), 2, 4, false, strings);

        Assert.Multiple(() =>
        {
            Assert.That(isoAdobe.IsPredefined(), Is.True);
            Assert.That(isoAdobe.GetPredefinedValue(), Is.Zero);
            Assert.That(isoAdobe.GetFormat(), Is.EqualTo(-1));
            Assert.That(isoAdobe.GetEncodedLength(), Is.Zero);
            Assert.That(isoAdobe.GetIdentifier(4), Is.EqualTo(4));
            Assert.That(isoAdobe.GetName(4), Is.EqualTo("numbersign"));
            Assert.That(expert.GetIdentifier(2), Is.EqualTo(229));
            Assert.That(expert.GetName(2), Is.EqualTo("exclamsmall"));
            Assert.That(subset.GetIdentifier(2), Is.EqualTo(231));
            Assert.That(subset.GetName(2), Is.EqualTo("dollaroldstyle"));
            Assert.That(subset.FindGlyphIndex(231), Is.EqualTo(2));
            Assert.That(subset.FindGlyphIndex(390), Is.EqualTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => subset.GetIdentifier(4));
        });
    }

    [Test]
    public void ParsesCustomCharsetFormatsZeroOneAndTwo()
    {
        CffStringTable strings = CreateStringTable(Bytes("CustomA"), Bytes("CustomB"));
        byte[] formatZeroBytes = Join(
            new byte[] { 9, 9, 9, 0 },
            UInt16(1), UInt16(391), UInt16(392));
        CffCharset formatZero = CffCharsetParser.Parse(
            CreateSlice(formatZeroBytes), 3, 4, false, strings);
        byte[] formatOneBytes = Join(
            new byte[] { 9, 9, 9, 1 },
            UInt16(5), new byte[] { 2 },
            UInt16(391), new byte[] { 1 });
        CffCharset formatOne = CffCharsetParser.Parse(
            CreateSlice(formatOneBytes), 3, 6, false, strings);
        byte[] formatTwoBytes = Join(
            new byte[] { 9, 9, 9, 2 },
            UInt16(1000), UInt16(256));
        CffCharset formatTwo = CffCharsetParser.Parse(
            CreateSlice(formatTwoBytes), 3, 258, true, strings);

        Assert.Multiple(() =>
        {
            Assert.That(formatZero.IsPredefined(), Is.False);
            Assert.That(formatZero.GetFormat(), Is.Zero);
            Assert.That(formatZero.GetEncodedLength(), Is.EqualTo(7));
            Assert.That(formatZero.GetName(1), Is.EqualTo("space"));
            Assert.That(formatZero.GetName(2), Is.EqualTo("CustomA"));
            Assert.That(formatZero.GetName(3), Is.EqualTo("CustomB"));
            Assert.That(formatOne.GetFormat(), Is.EqualTo(1));
            Assert.That(CopyIdentifiers(formatOne), Is.EqualTo(new int[] { 0, 5, 6, 7, 391, 392 }));
            Assert.That(formatTwo.IsCidKeyed(), Is.True);
            Assert.That(formatTwo.GetFormat(), Is.EqualTo(2));
            Assert.That(formatTwo.GetIdentifier(1), Is.EqualTo(1000));
            Assert.That(formatTwo.GetIdentifier(257), Is.EqualTo(1256));
            Assert.Throws<FontFormatException>(() => formatTwo.GetName(1));
        });
    }

    [Test]
    public void RejectsMalformedCharsets()
    {
        CffStringTable strings = CreateStringTable();
        byte[][] malformed = new byte[][]
        {
            new byte[] { 9, 9, 9, 3 },
            Join(new byte[] { 9, 9, 9, 0 }, UInt16(1)),
            Join(new byte[] { 9, 9, 9, 0 }, UInt16(1), UInt16(1)),
            Join(new byte[] { 9, 9, 9, 0 }, UInt16(65000), UInt16(1)),
            Join(new byte[] { 9, 9, 9, 1 }, UInt16(5), new byte[] { 3 }),
            Join(new byte[] { 9, 9, 9, 1 }, UInt16(65535), new byte[] { 1 })
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(malformed[0]), 3, 2, false, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(malformed[1]), 3, 3, false, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(malformed[2]), 3, 3, false, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(malformed[3]), 3, 3, false, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(malformed[4]), 3, 3, false, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(malformed[5]), 3, 3, true, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(Array.Empty<byte>()), 0, 230, false, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(Array.Empty<byte>()), 0, 1, true, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(Array.Empty<byte>()), 3, 1, false, strings));
            Assert.Throws<FontFormatException>(() => CffCharsetParser.Parse(CreateSlice(Array.Empty<byte>()), 0, 0, false, strings));
            Assert.Throws<ArgumentNullException>(() => CffCharsetParser.Parse(null!, 0, 1, false, strings));
        });
    }

    [Test]
    public void ResolvesStandardAndExpertEncodingsThroughCharset()
    {
        CffStringTable strings = CreateStringTable();
        CffCharset standardCharset = CffCharsetParser.Parse(
            CreateSlice(Array.Empty<byte>()), 0, 150, false, strings);
        CffCharset expertCharset = CffCharsetParser.Parse(
            CreateSlice(Array.Empty<byte>()), 1, 166, false, strings);
        CffEncoding standard = CffEncodingParser.Parse(
            CreateSlice(Array.Empty<byte>()), 0, standardCharset);
        CffEncoding expert = CffEncodingParser.Parse(
            CreateSlice(Array.Empty<byte>()), 1, expertCharset);

        Assert.Multiple(() =>
        {
            Assert.That(standard.IsPredefined(), Is.True);
            Assert.That(standard.GetPredefinedValue(), Is.Zero);
            Assert.That(standard.HasCode(32), Is.True);
            Assert.That(standard.GetGlyphIndex(32), Is.EqualTo(1));
            Assert.That(standard.GetIdentifier(65), Is.EqualTo(34));
            Assert.That(standard.GetGlyphIndex(65), Is.EqualTo(34));
            Assert.That(standard.HasCode(0), Is.False);
            Assert.That(standard.GetGlyphIndex(0), Is.Zero);
            Assert.That(expert.GetPredefinedValue(), Is.EqualTo(1));
            Assert.That(expert.GetIdentifier(33), Is.EqualTo(229));
            Assert.That(expert.GetGlyphIndex(33), Is.EqualTo(2));
            Assert.That(expert.HasCode(35), Is.False);
            Assert.That(CffPredefinedData.GetEncodingSid(0, 255), Is.Zero);
            Assert.That(CffPredefinedData.GetEncodingSid(1, 255), Is.EqualTo(378));
            Assert.That(CffPredefinedData.GetCharsetIdentifier(1, 165), Is.EqualTo(378));
            Assert.Throws<ArgumentOutOfRangeException>(() => standard.HasCode(256));
        });
    }

    [Test]
    public void ParsesCustomEncodingFormatsAndSupplements()
    {
        CffStringTable strings = CreateStringTable();
        CffCharset charset = ParseCidStyleCharsetAsSid(
            strings,
            new int[] { 0, 10, 11, 12, 13 });
        byte[] formatZeroBytes = Join(
            new byte[] { 9, 9, 0x80, 2, 65, 66, 1, 67 },
            UInt16(12));
        CffEncoding formatZero = CffEncodingParser.Parse(
            CreateSlice(formatZeroBytes), 2, charset);
        byte[] formatOneBytes = new byte[]
        {
            9, 9, 1, 2, 30, 1, 40, 1
        };
        CffEncoding formatOne = CffEncodingParser.Parse(
            CreateSlice(formatOneBytes), 2, charset);

        Assert.Multiple(() =>
        {
            Assert.That(formatZero.IsPredefined(), Is.False);
            Assert.That(formatZero.GetFormat(), Is.Zero);
            Assert.That(formatZero.GetEncodedLength(), Is.EqualTo(8));
            Assert.That(formatZero.GetSupplementCount(), Is.EqualTo(1));
            Assert.That(formatZero.GetGlyphIndex(65), Is.EqualTo(1));
            Assert.That(formatZero.GetIdentifier(66), Is.EqualTo(11));
            Assert.That(formatZero.GetGlyphIndex(67), Is.EqualTo(3));
            Assert.That(formatZero.IsSupplementalCode(67), Is.True);
            Assert.That(formatZero.IsSupplementalCode(65), Is.False);
            Assert.That(formatOne.GetFormat(), Is.EqualTo(1));
            Assert.That(formatOne.GetGlyphIndex(30), Is.EqualTo(1));
            Assert.That(formatOne.GetGlyphIndex(31), Is.EqualTo(2));
            Assert.That(formatOne.GetGlyphIndex(40), Is.EqualTo(3));
            Assert.That(formatOne.GetGlyphIndex(41), Is.EqualTo(4));
        });
    }

    [Test]
    public void RejectsMalformedCustomEncodings()
    {
        CffStringTable strings = CreateStringTable();
        CffCharset charset = ParseCidStyleCharsetAsSid(
            strings,
            new int[] { 0, 10, 11, 12 });
        CffCharset cidCharset = CffCharsetParser.Parse(
            CreateSlice(Join(new byte[] { 9, 9, 9, 0 }, UInt16(10))),
            3,
            2,
            true,
            strings);
        byte[][] malformed = new byte[][]
        {
            new byte[] { 9, 9, 2 },
            new byte[] { 9, 9, 0, 4, 1, 2, 3, 4 },
            new byte[] { 9, 9, 0, 2, 65, 65 },
            new byte[] { 9, 9, 1, 1, 255, 1 },
            new byte[] { 9, 9, 1, 2, 30, 1, 31, 0 },
            Join(new byte[] { 9, 9, 0x80, 1, 65, 1, 65 }, UInt16(11)),
            Join(new byte[] { 9, 9, 0x80, 1, 65, 1, 66 }, UInt16(99)),
            new byte[] { 9, 9, 0x80, 0, 1, 66, 0 }
        };

        Assert.Multiple(() =>
        {
            foreach (byte[] value in malformed)
            {
                Assert.Throws<FontFormatException>(() => CffEncodingParser.Parse(CreateSlice(value), 2, charset));
            }

            Assert.Throws<FontFormatException>(() => CffEncodingParser.Parse(CreateSlice(Array.Empty<byte>()), 2, charset));
            Assert.Throws<FontFormatException>(() => CffEncodingParser.Parse(CreateSlice(Array.Empty<byte>()), 0, cidCharset));
            Assert.Throws<ArgumentNullException>(() => CffEncodingParser.Parse(null!, 0, charset));
            Assert.Throws<ArgumentNullException>(() => CffEncodingParser.Parse(CreateSlice(Array.Empty<byte>()), 0, null!));
        });
    }

    [Test]
    public void ParsesFdSelectFormatsZeroAndThree()
    {
        CffFontDictionarySelect formatZero = CffFontDictionarySelectParser.Parse(
            CreateSlice(new byte[] { 0, 0, 1, 1, 0 }),
            0,
            4,
            2);
        byte[] formatThreeBytes = new TestBinaryBuilder()
            .WriteUInt8(3)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt8(1)
            .WriteUInt16(2)
            .WriteUInt8(0)
            .WriteUInt16(5)
            .CopyBytes();
        CffFontDictionarySelect formatThree = CffFontDictionarySelectParser.Parse(
            CreateSlice(formatThreeBytes),
            0,
            5,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(formatZero.GetFormat(), Is.Zero);
            Assert.That(formatZero.GetEncodedLength(), Is.EqualTo(5));
            Assert.That(CopyFdSelect(formatZero), Is.EqualTo(new int[] { 0, 1, 1, 0 }));
            Assert.That(formatThree.GetFormat(), Is.EqualTo(3));
            Assert.That(formatThree.GetEncodedLength(), Is.EqualTo(11));
            Assert.That(CopyFdSelect(formatThree), Is.EqualTo(new int[] { 1, 1, 0, 0, 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => formatThree.GetFontDictionaryIndex(5));
        });
    }

    [Test]
    public void RejectsMalformedFdSelectStructures()
    {
        byte[][] malformed = new byte[][]
        {
            new byte[] { 1 },
            new byte[] { 0, 0, 2 },
            new TestBinaryBuilder().WriteUInt8(3).WriteUInt16(0).CopyBytes(),
            BuildFdSelectThree(new int[] { 1 }, new int[] { 0 }, 3),
            BuildFdSelectThree(new int[] { 0, 0 }, new int[] { 0, 1 }, 3),
            BuildFdSelectThree(new int[] { 0 }, new int[] { 2 }, 3),
            BuildFdSelectThree(new int[] { 0 }, new int[] { 0 }, 2)
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CffFontDictionarySelectParser.Parse(CreateSlice(malformed[0]), 0, 2, 2));
            Assert.Throws<FontFormatException>(() => CffFontDictionarySelectParser.Parse(CreateSlice(malformed[1]), 0, 2, 2));

            for (int index = 2; index < malformed.Length; index += 1)
            {
                Assert.Throws<FontFormatException>(() => CffFontDictionarySelectParser.Parse(CreateSlice(malformed[index]), 0, 3, 2));
            }

            Assert.Throws<FontFormatException>(() => CffFontDictionarySelectParser.Parse(CreateSlice(Array.Empty<byte>()), 0, 1, 1));
            Assert.Throws<FontFormatException>(() => CffFontDictionarySelectParser.Parse(CreateSlice(new byte[] { 0 }), 0, 0, 1));
            Assert.Throws<ArgumentNullException>(() => CffFontDictionarySelectParser.Parse(null!, 0, 1, 1));
        });
    }

    [Test]
    public void ParsesFdArrayAndItsBoundedPrivateDictionaries()
    {
        CffStringTable strings = CreateStringTable(Bytes("FD0"), Bytes("FD1"));
        byte[] privateZero = Join(EncodeInteger(500), Operator(CffDictOperator.DefaultWidthX));
        byte[] privateOne = Join(EncodeInteger(600), Operator(CffDictOperator.DefaultWidthX));
        int privateOneOffset = privateZero.Length;
        byte[] fontZero = Join(
            EncodeInteger(391), Operator(CffDictOperator.FontName),
            EncodeInteger(privateZero.Length), EncodeInteger(0), Operator(CffDictOperator.Private));
        byte[] fontOne = Join(
            EncodeInteger(392), Operator(CffDictOperator.FontName),
            EncodeInteger(privateOne.Length), EncodeInteger(privateOneOffset), Operator(CffDictOperator.Private));
        byte[] fdArrayIndex = BuildIndex(fontZero, fontOne);
        byte[] data = Join(privateZero, privateOne, fdArrayIndex);
        CffFontDictionaryArray array = CffFontDictionaryArrayParser.Parse(
            CreateSlice(data),
            privateZero.Length + privateOne.Length,
            strings);

        Assert.Multiple(() =>
        {
            Assert.That(array.GetCount(), Is.EqualTo(2));
            Assert.That(array.GetIndex().GetCount(), Is.EqualTo(2));
            Assert.That(array.GetRecord(0).GetDictionary().GetFontName(), Is.EqualTo("FD0"));
            Assert.That(array.GetRecord(1).GetDictionary().GetFontName(), Is.EqualTo("FD1"));
            Assert.That(array.GetRecord(0).GetPrivateDictionary().GetDefaultWidthX(), Is.EqualTo(500));
            Assert.That(array.GetRecord(1).GetPrivateDictionary().GetDefaultWidthX(), Is.EqualTo(600));
            Assert.Throws<ArgumentOutOfRangeException>(() => array.GetRecord(2));
        });
    }

    [Test]
    public void RejectsMalformedFdArraysAndPrivateBounds()
    {
        CffStringTable strings = CreateStringTable();
        byte[] noPrivate = BuildIndex(Array.Empty<byte>());
        byte[] outsidePrivate = BuildIndex(Join(
            EncodeInteger(10), EncodeInteger(100), Operator(CffDictOperator.Private)));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CffFontDictionaryArrayParser.Parse(CreateSlice(BuildIndex()), 0, strings));
            Assert.Throws<FontFormatException>(() => CffFontDictionaryArrayParser.Parse(CreateSlice(noPrivate), 0, strings));
            Assert.Throws<FontFormatException>(() => CffFontDictionaryArrayParser.Parse(CreateSlice(outsidePrivate), 0, strings));
            Assert.Throws<FontFormatException>(() => CffFontDictionaryArrayParser.Parse(CreateSlice(new byte[] { 0, 0 }), 2, strings));
            Assert.Throws<ArgumentNullException>(() => CffFontDictionaryArrayParser.Parse(null!, 0, strings));
        });
    }

    [Test]
    public void ParsesNonCidTableCharsetEncodingAndCharStringsEndToEnd()
    {
        SfntCffTable? table = ParseTable(
            BuildNonCidCff(),
            FontLimits.CreateCompatible());
        CffIndex charStrings = table!.GetCharStringsIndex();
        CffCharset charset = table.GetCharset();
        CffEncoding? encoding = table.GetEncoding();

        Assert.Multiple(() =>
        {
            Assert.That(charStrings.GetCount(), Is.EqualTo(4));
            Assert.That(table.GetCharStringsIndex(), Is.SameAs(charStrings));
            Assert.That(charset.GetName(1), Is.EqualTo("space"));
            Assert.That(charset.GetName(2), Is.EqualTo("CustomA"));
            Assert.That(charset.GetName(3), Is.EqualTo("CustomB"));
            Assert.That(table.GetCharset(), Is.SameAs(charset));
            Assert.That(encoding!.GetGlyphIndex(65), Is.EqualTo(1));
            Assert.That(encoding.GetGlyphIndex(66), Is.EqualTo(2));
            Assert.That(encoding.GetGlyphIndex(67), Is.EqualTo(3));
            Assert.That(table.GetEncoding(), Is.SameAs(encoding));
            Assert.That(table.GetCidMetadata(), Is.Null);
            Assert.That(table.GetFontDictionaryArray(), Is.Null);
            Assert.That(table.GetFontDictionarySelect(), Is.Null);
            Assert.That(table.ExecuteCharString(0).GetWidth(), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => table.ExecuteCharString(4));
        });
    }

    [Test]
    public void ParsesCidMetadataFdArrayAndSelectionEndToEnd()
    {
        SfntCffTable? table = ParseTable(
            BuildCidCff(false),
            FontLimits.CreateCompatible());
        CffCidMetadata? metadata = table!.GetCidMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(metadata!.GetRegistry(), Is.EqualTo("Adobe"));
            Assert.That(metadata.GetOrdering(), Is.EqualTo("Identity"));
            Assert.That(metadata.GetSupplement(), Is.Zero);
            Assert.That(metadata.GetFontVersion(), Is.EqualTo(1));
            Assert.That(metadata.GetFontRevision(), Is.EqualTo(2));
            Assert.That(metadata.GetFontType(), Is.Zero);
            Assert.That(metadata.GetCidCount(), Is.EqualTo(100));
            Assert.That(metadata.HasUniqueIdentifierBase(), Is.True);
            Assert.That(metadata.GetUniqueIdentifierBase(), Is.EqualTo(50));
            Assert.That(metadata.GetCharset().GetIdentifier(3), Is.EqualTo(20));
            Assert.That(metadata.GetFontDictionaries().GetCount(), Is.EqualTo(2));
            Assert.That(metadata.GetFontDictionarySelect().GetFormat(), Is.Zero);
            Assert.That(metadata.GetFontDictionaryForGlyph(0).GetDictionary().GetFontName(), Is.EqualTo("FD0"));
            Assert.That(metadata.GetFontDictionaryForGlyph(3).GetDictionary().GetFontName(), Is.EqualTo("FD1"));
            Assert.That(metadata.GetFontDictionaryForGlyph(3).GetPrivateDictionary().GetDefaultWidthX(), Is.EqualTo(600));
            Assert.That(table.GetEncoding(), Is.Null);
            Assert.That(table.GetCidMetadata(), Is.SameAs(metadata));
            Assert.That(table.ExecuteCharString(0).GetWidth(), Is.EqualTo(500));
            Assert.That(table.ExecuteCharString(3).GetWidth(), Is.EqualTo(600));
        });
    }

    [Test]
    public void ExecutesTableBackedLocalAndGlobalSubroutines()
    {
        SfntCffTable? table = ParseTable(
            BuildSubroutineCff(),
            FontLimits.CreateCompatible());
        Type2CharStringResult result = table!.ExecuteCharString(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetWidth(), Is.EqualTo(500));
            Assert.That(result.GetCommandCount(), Is.EqualTo(3));
            Assert.That(result.GetCommand(1).GetEndX(), Is.EqualTo(1));
            Assert.That(result.GetCommand(1).GetEndY(), Is.EqualTo(2));
            Assert.That(result.GetCommand(2).GetEndX(), Is.EqualTo(4));
            Assert.That(result.GetCommand(2).GetEndY(), Is.EqualTo(6));
        });
    }

    [Test]
    public void RejectsInconsistentCidMetadataAndConfiguredGlyphLimit()
    {
        SfntCffTable? cidWithEncoding = ParseTable(
            BuildCidCff(true),
            FontLimits.CreateCompatible());
        SfntCffTable? limited = ParseTable(
            BuildNonCidCff(),
            CreateLimits(3));
        byte[] nonCidWithFdArray = Join(
            EncodeInteger(1), Operator(CffDictOperator.CharStrings),
            EncodeInteger(0), EncodeInteger(10), Operator(CffDictOperator.Private),
            EncodeInteger(20), Operator(CffDictOperator.FontDictionaryArray));
        CffStringTable strings = CreateStringTable();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => cidWithEncoding!.GetEncoding());
            Assert.Throws<FontFormatException>(() => cidWithEncoding!.GetCidMetadata());
            Assert.Throws<FontLimitException>(() => limited!.GetCharStringsIndex());
            Assert.Throws<FontLimitException>(() => limited!.GetCharStringsIndex());
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(
                CreateSlice(nonCidWithFdArray), strings));
        });
    }

    static CffCharset ParseCidStyleCharsetAsSid(
        CffStringTable strings,
        int[] identifiers)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt8(9)
            .WriteUInt8(9)
            .WriteUInt8(9)
            .WriteUInt8(0);

        for (int index = 1; index < identifiers.Length; index += 1)
        {
            builder.WriteUInt16((ushort)identifiers[index]);
        }

        return CffCharsetParser.Parse(
            CreateSlice(builder.CopyBytes()),
            3,
            identifiers.Length,
            false,
            strings);
    }

    static int[] CopyIdentifiers(CffCharset charset)
    {
        int[] values = new int[charset.GetGlyphCount()];

        for (int index = 0; index < values.Length; index += 1)
        {
            values[index] = charset.GetIdentifier(index);
        }

        return values;
    }

    static int[] CopyFdSelect(CffFontDictionarySelect select)
    {
        int[] values = new int[select.GetGlyphCount()];

        for (int index = 0; index < values.Length; index += 1)
        {
            values[index] = select.GetFontDictionaryIndex(index);
        }

        return values;
    }

    static byte[] BuildFdSelectThree(
        int[] firstGlyphs,
        int[] indices,
        int sentinel)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt8(3)
            .WriteUInt16((ushort)firstGlyphs.Length);

        for (int index = 0; index < firstGlyphs.Length; index += 1)
        {
            builder.WriteUInt16((ushort)firstGlyphs[index])
                .WriteUInt8((byte)indices[index]);
        }

        return builder.WriteUInt16((ushort)sentinel).CopyBytes();
    }

    static byte[] BuildNonCidCff()
    {
        byte[] strings = BuildIndex(Bytes("CustomA"), Bytes("CustomB"));
        byte[] charStrings = BuildIndex(
            new byte[] { 14 },
            new byte[] { 14 },
            new byte[] { 14 },
            new byte[] { 14 });
        byte[] charset = Join(
            new byte[] { 0 },
            UInt16(1), UInt16(391), UInt16(392));
        byte[] encoding = Join(
            new byte[] { 0x80, 2, 65, 66, 1, 67 },
            UInt16(392));
        byte[] placeholderTop = BuildNonCidTop(0, 0, 0, 0);
        int prefixLength = BuildCffPrefix(placeholderTop, strings).Length;
        int charStringsOffset = prefixLength;
        int charsetOffset = charStringsOffset + charStrings.Length;
        int encodingOffset = charsetOffset + charset.Length;
        byte[] top = BuildNonCidTop(
            charStringsOffset,
            charsetOffset,
            encodingOffset,
            prefixLength);
        byte[] prefix = BuildCffPrefix(top, strings);

        if (prefix.Length != prefixLength)
        {
            throw new InvalidOperationException("The test CFF prefix changed encoded width.");
        }

        return Join(prefix, charStrings, charset, encoding);
    }

    static byte[] BuildNonCidTop(
        int charStringsOffset,
        int charsetOffset,
        int encodingOffset,
        int privateOffset)
    {
        return Join(
            EncodeInteger(charsetOffset), Operator(CffDictOperator.Charset),
            EncodeInteger(encodingOffset), Operator(CffDictOperator.Encoding),
            EncodeInteger(charStringsOffset), Operator(CffDictOperator.CharStrings),
            EncodeInteger(0), EncodeInteger(privateOffset), Operator(CffDictOperator.Private));
    }

    internal static byte[] BuildCidCff(bool includeEncoding)
    {
        byte[] strings = BuildIndex(
            Bytes("Adobe"),
            Bytes("Identity"),
            Bytes("FD0"),
            Bytes("FD1"));
        byte[] charStrings = BuildIndex(
            new byte[] { 14 },
            new byte[] { 14 },
            new byte[] { 14 },
            new byte[] { 14 });
        byte[] charset = Join(
            new byte[] { 0 },
            UInt16(5), UInt16(10), UInt16(20));
        byte[] fdSelect = new byte[] { 0, 0, 0, 1, 1 };
        byte[] privateZero = Join(EncodeInteger(500), Operator(CffDictOperator.DefaultWidthX));
        byte[] privateOne = Join(EncodeInteger(600), Operator(CffDictOperator.DefaultWidthX));
        byte[] placeholderTop = BuildCidTop(0, 0, 0, 0, includeEncoding);
        int prefixLength = BuildCffPrefix(placeholderTop, strings).Length;

        for (int attempt = 0; attempt < 4; attempt += 1)
        {
            int charStringsOffset = prefixLength;
            int charsetOffset = charStringsOffset + charStrings.Length;
            int fdSelectOffset = charsetOffset + charset.Length;
            int privateZeroOffset = fdSelectOffset + fdSelect.Length;
            int privateOneOffset = privateZeroOffset + privateZero.Length;
            byte[] fontZero = Join(
                EncodeInteger(393), Operator(CffDictOperator.FontName),
                EncodeInteger(privateZero.Length), EncodeInteger(privateZeroOffset),
                Operator(CffDictOperator.Private));
            byte[] fontOne = Join(
                EncodeInteger(394), Operator(CffDictOperator.FontName),
                EncodeInteger(privateOne.Length), EncodeInteger(privateOneOffset),
                Operator(CffDictOperator.Private));
            byte[] fdArray = BuildIndex(fontZero, fontOne);
            int fdArrayOffset = privateOneOffset + privateOne.Length;
            byte[] top = BuildCidTop(
                charStringsOffset,
                charsetOffset,
                fdArrayOffset,
                fdSelectOffset,
                includeEncoding);
            byte[] prefix = BuildCffPrefix(top, strings);

            if (prefix.Length == prefixLength)
            {
                return Join(
                    prefix,
                    charStrings,
                    charset,
                    fdSelect,
                    privateZero,
                    privateOne,
                    fdArray);
            }

            prefixLength = prefix.Length;
        }

        throw new InvalidOperationException("The test CID offsets did not stabilize.");
    }

    static byte[] BuildSubroutineCff()
    {
        byte[] strings = BuildIndex();
        byte[] globalSubroutines = BuildIndex(new byte[] { 142, 143, 11 });
        byte[] charStrings = BuildIndex(new byte[]
        {
            139, 139, 21,
            32, 10, 5,
            32, 29, 5,
            14
        });
        byte[] localSubroutines = BuildIndex(new byte[] { 140, 141, 11 });
        byte[] privateValues = BuildSubroutinePrivate(0);
        privateValues = BuildSubroutinePrivate(privateValues.Length);
        int prefixLength = BuildCffPrefix(
            BuildSubroutineTop(0, privateValues.Length, 0),
            strings,
            globalSubroutines).Length;

        for (int attempt = 0; attempt < 4; attempt += 1)
        {
            int charStringsOffset = prefixLength;
            int privateOffset = charStringsOffset + charStrings.Length;
            byte[] top = BuildSubroutineTop(
                charStringsOffset,
                privateValues.Length,
                privateOffset);
            byte[] prefix = BuildCffPrefix(
                top,
                strings,
                globalSubroutines);

            if (prefix.Length == prefixLength)
            {
                return Join(
                    prefix,
                    charStrings,
                    privateValues,
                    localSubroutines);
            }

            prefixLength = prefix.Length;
        }

        throw new InvalidOperationException(
            "The test subroutine CFF offsets did not stabilize.");
    }

    static byte[] BuildSubroutineTop(
        int charStringsOffset,
        int privateSize,
        int privateOffset)
    {
        return Join(
            EncodeInteger(charStringsOffset),
            Operator(CffDictOperator.CharStrings),
            EncodeInteger(privateSize),
            EncodeInteger(privateOffset),
            Operator(CffDictOperator.Private));
    }

    static byte[] BuildSubroutinePrivate(int localSubroutinesOffset)
    {
        return Join(
            EncodeInteger(localSubroutinesOffset),
            Operator(CffDictOperator.LocalSubroutines),
            EncodeInteger(500),
            Operator(CffDictOperator.DefaultWidthX));
    }

    static byte[] BuildCidTop(
        int charStringsOffset,
        int charsetOffset,
        int fdArrayOffset,
        int fdSelectOffset,
        bool includeEncoding)
    {
        byte[] top = Join(
            EncodeInteger(391), EncodeInteger(392), EncodeInteger(0),
            Operator(CffDictOperator.RegistryOrderingSupplement),
            EncodeInteger(1), Operator(CffDictOperator.CidFontVersion),
            EncodeInteger(2), Operator(CffDictOperator.CidFontRevision),
            EncodeInteger(100), Operator(CffDictOperator.CidCount),
            EncodeInteger(50), Operator(CffDictOperator.UniqueIdentifierBase),
            EncodeInteger(charsetOffset), Operator(CffDictOperator.Charset),
            EncodeInteger(charStringsOffset), Operator(CffDictOperator.CharStrings),
            EncodeInteger(fdArrayOffset), Operator(CffDictOperator.FontDictionaryArray),
            EncodeInteger(fdSelectOffset), Operator(CffDictOperator.FontDictionarySelect));

        if (includeEncoding)
        {
            top = Join(
                top,
                EncodeInteger(0), Operator(CffDictOperator.Encoding));
        }

        return top;
    }

    static byte[] BuildCffPrefix(byte[] top, byte[] strings)
    {
        return BuildCffPrefix(top, strings, BuildIndex());
    }

    static byte[] BuildCffPrefix(
        byte[] top,
        byte[] strings,
        byte[] globalSubroutines)
    {
        return Join(
            new byte[] { 1, 0, 4, 1 },
            BuildIndex(new byte[] { (byte)'X' }),
            BuildIndex(top),
            strings,
            globalSubroutines);
    }

    static SfntCffTable? ParseTable(byte[] bytes, FontLimits limits)
    {
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            FontTags.Cff,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            limits);
        return SfntCffTableParser.FindTable(cache);
    }

    static CffStringTable CreateStringTable(params byte[][] values)
    {
        byte[] indexBytes = BuildIndex(values);
        return new CffStringTable(CffIndexParser.Parse(CreateSlice(indexBytes), 0));
    }

    static FontLimits CreateLimits(int maxGlyphs)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            maxGlyphs,
            1000,
            1000,
            10,
            10,
            10,
            100,
            100000,
            100000,
            1000000,
            1000000,
            100,
            1000,
            100,
            100);
    }

    static byte[] BuildIndex(params byte[][] values)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)values.Length);

        if (values.Length == 0)
        {
            return builder.CopyBytes();
        }

        int dataLength = 0;

        foreach (byte[] value in values)
        {
            dataLength += value.Length;
        }

        int offsetSize = dataLength + 1 <= 255 ? 1 : 2;
        builder.WriteUInt8((byte)offsetSize);
        int offset = 1;
        WriteIndexOffset(builder, offsetSize, offset);

        foreach (byte[] value in values)
        {
            offset += value.Length;
            WriteIndexOffset(builder, offsetSize, offset);
        }

        foreach (byte[] value in values)
        {
            builder.WriteBytes(value);
        }

        return builder.CopyBytes();
    }

    static void WriteIndexOffset(
        TestBinaryBuilder builder,
        int offsetSize,
        int offset)
    {
        if (offsetSize == 1)
        {
            builder.WriteUInt8((byte)offset);
        }
        else
        {
            builder.WriteUInt16((ushort)offset);
        }
    }

    static byte[] EncodeInteger(int value)
    {
        if (value >= -107 && value <= 107)
        {
            return new byte[] { (byte)(value + 139) };
        }

        if (value >= 108 && value <= 1131)
        {
            int adjusted = value - 108;
            return new byte[]
            {
                (byte)(247 + adjusted / 256),
                (byte)(adjusted % 256)
            };
        }

        return new TestBinaryBuilder()
            .WriteUInt8(28)
            .WriteInt16((short)value)
            .CopyBytes();
    }

    static byte[] Operator(int operatorCode)
    {
        if (operatorCode >= 0x0C00)
        {
            return new byte[] { 12, (byte)(operatorCode & 255) };
        }

        return new byte[] { (byte)operatorCode };
    }

    static byte[] UInt16(int value)
    {
        return new TestBinaryBuilder().WriteUInt16((ushort)value).CopyBytes();
    }

    static byte[] Bytes(string value)
    {
        byte[] bytes = new byte[value.Length];

        for (int index = 0; index < value.Length; index += 1)
        {
            bytes[index] = (byte)value[index];
        }

        return bytes;
    }

    static FontDataSlice CreateSlice(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return data.GetSlice(0, data.GetLength());
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
