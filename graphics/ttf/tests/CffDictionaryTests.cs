using ttf;

namespace ttf.Tests;

internal class CffDictionaryTests
{
    [Test]
    public void DecodesEveryDictIntegerFormAndPreservesOperators()
    {
        byte[] bytes = Join(
            new byte[] { 32, 246, 247, 0, 250, 255, 251, 0, 254, 255 },
            new byte[] { 28, 0x80, 0x00 },
            new byte[] { 29, 0x7F, 0xFF, 0xFF, 0xFF },
            new byte[] { 12, 99 },
            new byte[] { 139, 21 });
        CffDictionary dictionary = ParseDictionary(bytes);
        CffDictEntry escaped = dictionary.GetEntry(0);
        CffDictEntry ordinary = dictionary.GetEntry(1);

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.GetCount(), Is.EqualTo(2));
            Assert.That(escaped.GetOperator(), Is.EqualTo(CffDictOperator.CreateEscaped(99)));
            Assert.That(escaped.IsEscapedOperator(), Is.True);
            Assert.That(escaped.GetOperandCount(), Is.EqualTo(8));
            Assert.That(escaped.CopyNumbers(), Is.EqualTo(new double[]
            {
                -107, 107, 108, 1131, -108, -1131, -32768, int.MaxValue
            }));
            Assert.That(ordinary.GetOperator(), Is.EqualTo(21));
            Assert.That(ordinary.IsEscapedOperator(), Is.False);
            Assert.That(ordinary.GetOperand(0).GetInteger(), Is.Zero);
            Assert.That(dictionary.FindEntry(123), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetEntry(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => escaped.GetOperand(8));
            Assert.Throws<ArgumentOutOfRangeException>(() => CffDictOperator.CreateEscaped(256));
        });
    }

    [Test]
    public void DecodesRealNibblesIncludingSignsDecimalsAndExponents()
    {
        CffDictionary dictionary = ParseDictionary(new byte[]
        {
            30, 0xE1, 0x2A, 0x5C, 0x2F,
            30, 0x0B, 0x3F,
            30, 0x12, 0xFF,
            0
        });
        CffDictEntry entry = dictionary.GetEntry(0);

        Assert.Multiple(() =>
        {
            Assert.That(entry.GetOperand(0).IsInteger(), Is.False);
            Assert.That(entry.GetOperand(0).GetNumber(), Is.EqualTo(-0.125).Within(0.0000001));
            Assert.That(entry.GetOperand(1).GetNumber(), Is.Zero);
            Assert.That(entry.GetOperand(2).GetNumber(), Is.EqualTo(12));
            Assert.Throws<FontFormatException>(() => entry.GetOperand(0).GetInteger());
        });
    }

    [Test]
    public void DecodesDeltaArraysWithoutChangingRawOperands()
    {
        CffDictEntry entry = ParseDictionary(new byte[]
        {
            129, 149, 247, 36, 159, 6
        }).GetEntry(0);

        Assert.Multiple(() =>
        {
            Assert.That(entry.CopyNumbers(), Is.EqualTo(new double[] { -10, 10, 144, 20 }));
            Assert.That(entry.CopyDeltaValues(), Is.EqualTo(new double[] { -10, 0, 144, 164 }));
        });
    }

    [Test]
    public void RejectsReservedTruncatedDanglingAndOversizedDictData()
    {
        byte[][] invalid = new byte[][]
        {
            new byte[] { 22 },
            new byte[] { 27 },
            new byte[] { 31 },
            new byte[] { 255 },
            new byte[] { 12 },
            new byte[] { 28, 1 },
            new byte[] { 29, 1, 2, 3 },
            new byte[] { 247 },
            new byte[] { 251 },
            new byte[] { 139 }
        };
        byte[] tooManyOperands = new byte[50];

        for (int index = 0; index < 49; index += 1)
        {
            tooManyOperands[index] = 139;
        }

        tooManyOperands[49] = 0;

        Assert.Multiple(() =>
        {
            foreach (byte[] value in invalid)
            {
                Assert.Throws<FontFormatException>(() => ParseDictionary(value));
            }

            Assert.Throws<FontLimitException>(() => ParseDictionary(tooManyOperands));
            Assert.Throws<ArgumentNullException>(() => CffDictParser.Parse(null!));
            Assert.That(ParseDictionary(Array.Empty<byte>()).GetCount(), Is.Zero);
        });
    }

    [Test]
    public void RejectsMalformedRealEncodings()
    {
        byte[][] invalid = new byte[][]
        {
            new byte[] { 30 },
            new byte[] { 30, 0xDF },
            new byte[] { 30, 0xBF },
            new byte[] { 30, 0x1B, 0xFF },
            new byte[] { 30, 0x1A, 0x2A, 0x3F },
            new byte[] { 30, 0x1E, 0x2F },
            new byte[] { 30, 0xFA },
            new byte[] { 30, 0x1B, 0x99, 0x99, 0xFF }
        };

        Assert.Multiple(() =>
        {
            foreach (byte[] value in invalid)
            {
                Assert.Throws<FontFormatException>(() => ParseDictionary(value));
            }
        });
    }

    [Test]
    public void ResolvesAllStandardAndCustomStringSidRanges()
    {
        CffStringTable strings = CreateStringTable(
            new byte[] { (byte)'C', (byte)'u', (byte)'s', (byte)'t', (byte)'o', (byte)'m' },
            new byte[] { 0xE9 });

        Assert.Multiple(() =>
        {
            Assert.That(strings.GetStandardStringCount(), Is.EqualTo(391));
            Assert.That(strings.GetCustomStringCount(), Is.EqualTo(2));
            Assert.That(strings.GetString(0), Is.EqualTo(".notdef"));
            Assert.That(strings.GetString(1), Is.EqualTo("space"));
            Assert.That(strings.GetString(390), Is.EqualTo("Semibold"));
            Assert.That(strings.GetString(391), Is.EqualTo("Custom"));
            Assert.That(strings.GetString(392), Is.EqualTo("é"));
            Assert.That(strings.IsStandardSid(390), Is.True);
            Assert.That(strings.IsCustomSid(391), Is.True);
            Assert.That(strings.IsCustomSid(390), Is.False);
            Assert.Throws<FontFormatException>(() => strings.GetString(-1));
            Assert.Throws<FontFormatException>(() => strings.GetString(393));
            Assert.Throws<FontFormatException>(() => strings.GetString(65000));
        });
    }

    [Test]
    public void AppliesTopDictionaryDefaultsAndResolvesCustomStrings()
    {
        CffStringTable strings = CreateStringTable(
            Bytes("Custom Full Name"));
        byte[] bytes = Join(
            EncodeInteger(391), Operator(CffDictOperator.FullName),
            EncodeInteger(25), Operator(CffDictOperator.CharStrings),
            EncodeInteger(0), EncodeInteger(40), Operator(CffDictOperator.Private));
        CffTopDictionary top = CffTopDictionary.Parse(CreateSlice(bytes), strings);

        Assert.Multiple(() =>
        {
            Assert.That(top.GetFullName(), Is.EqualTo("Custom Full Name"));
            Assert.That(top.GetVersion(), Is.Null);
            Assert.That(top.IsFixedPitch(), Is.False);
            Assert.That(top.GetItalicAngle(), Is.Zero);
            Assert.That(top.GetUnderlinePosition(), Is.EqualTo(-100));
            Assert.That(top.GetUnderlineThickness(), Is.EqualTo(50));
            Assert.That(top.GetPaintType(), Is.Zero);
            Assert.That(top.GetCharstringType(), Is.EqualTo(2));
            Assert.That(top.CopyFontMatrix(), Is.EqualTo(new double[] { 0.001, 0, 0, 0.001, 0, 0 }));
            Assert.That(top.CopyFontBoundingBox(), Is.EqualTo(new double[] { 0, 0, 0, 0 }));
            Assert.That(top.GetStrokeWidth(), Is.Zero);
            Assert.That(top.GetCharsetOffset(), Is.Zero);
            Assert.That(top.GetEncodingOffset(), Is.Zero);
            Assert.That(top.GetCharStringsOffset(), Is.EqualTo(25));
            Assert.That(top.GetPrivateDictionarySize(), Is.Zero);
            Assert.That(top.GetPrivateDictionaryOffset(), Is.EqualTo(40));
            Assert.That(top.IsCidKeyed(), Is.False);
        });
    }

    [Test]
    public void DecodesExplicitTopAndCidDictionaryValues()
    {
        CffStringTable strings = CreateStringTable(Bytes("Adobe"), Bytes("Identity"));
        byte[] ordinaryBytes = Join(
            EncodeInteger(42), Operator(CffDictOperator.UniqueIdentifier),
            EncodeInteger(1), Operator(CffDictOperator.IsFixedPitch),
            EncodeReal(0xE1, 0x2A, 0x5F), Operator(CffDictOperator.ItalicAngle),
            EncodeInteger(-10), EncodeInteger(-20), EncodeInteger(600), EncodeInteger(700),
            Operator(CffDictOperator.FontBoundingBox),
            EncodeInteger(1), EncodeInteger(0), EncodeInteger(0), EncodeReal(0x0A, 0x0F),
            EncodeInteger(5), EncodeInteger(6), Operator(CffDictOperator.FontMatrix),
            EncodeInteger(3), Operator(CffDictOperator.SyntheticBase),
            EncodeInteger(391), Operator(CffDictOperator.PostScript),
            EncodeInteger(392), Operator(CffDictOperator.BaseFontName),
            EncodeInteger(10), EncodeInteger(2), Operator(CffDictOperator.BaseFontBlend),
            EncodeInteger(2), Operator(CffDictOperator.CharstringType),
            EncodeInteger(70), Operator(CffDictOperator.CharStrings),
            EncodeInteger(10), EncodeInteger(80), Operator(CffDictOperator.Private));
        CffTopDictionary ordinary = CffTopDictionary.Parse(CreateSlice(ordinaryBytes), strings);
        byte[] cidBytes = Join(
            EncodeInteger(391), EncodeInteger(392), EncodeInteger(3),
            Operator(CffDictOperator.RegistryOrderingSupplement),
            EncodeInteger(99), Operator(CffDictOperator.CharStrings),
            EncodeInteger(120), Operator(CffDictOperator.FontDictionaryArray),
            EncodeInteger(140), Operator(CffDictOperator.FontDictionarySelect));
        CffTopDictionary cid = CffTopDictionary.Parse(CreateSlice(cidBytes), strings);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.IsFixedPitch(), Is.True);
            Assert.That(ordinary.GetItalicAngle(), Is.EqualTo(-12.5));
            Assert.That(ordinary.CopyFontBoundingBox(), Is.EqualTo(new double[] { -10, -20, 600, 700 }));
            Assert.That(ordinary.CopyFontMatrix(), Is.EqualTo(new double[] { 1, 0, 0, 0.0, 5, 6 }));
            Assert.That(ordinary.HasUniqueIdentifier(), Is.True);
            Assert.That(ordinary.GetUniqueIdentifier(), Is.EqualTo(42));
            Assert.That(ordinary.HasSyntheticBase(), Is.True);
            Assert.That(ordinary.GetSyntheticBase(), Is.EqualTo(3));
            Assert.That(ordinary.GetPostScript(), Is.EqualTo("Adobe"));
            Assert.That(ordinary.GetBaseFontName(), Is.EqualTo("Identity"));
            Assert.That(ordinary.CopyBaseFontBlend(), Is.EqualTo(new double[] { 10, 12 }));
            Assert.That(cid.IsCidKeyed(), Is.True);
            Assert.That(cid.GetRegistry(), Is.EqualTo("Adobe"));
            Assert.That(cid.GetOrdering(), Is.EqualTo("Identity"));
            Assert.That(cid.GetSupplement(), Is.EqualTo(3));
            Assert.That(cid.GetCidFontVersion(), Is.Zero);
            Assert.That(cid.GetCidFontRevision(), Is.Zero);
            Assert.That(cid.GetCidFontType(), Is.Zero);
            Assert.That(cid.GetCidCount(), Is.EqualTo(8720));
            Assert.That(cid.GetFontDictionaryArrayOffset(), Is.EqualTo(120));
            Assert.That(cid.GetFontDictionarySelectOffset(), Is.EqualTo(140));
            Assert.That(cid.HasPrivateDictionary(), Is.False);
        });
    }

    [Test]
    public void DecodesPrivateDictionaryDeltasValuesAndDefaults()
    {
        byte[] bytes = Join(
            EncodeInteger(-20), EncodeInteger(10), EncodeInteger(400), EncodeInteger(20),
            Operator(CffDictOperator.BlueValues),
            EncodeReal(0x0A, 0x05, 0xFF), Operator(CffDictOperator.BlueScale),
            EncodeInteger(50), EncodeInteger(20), Operator(CffDictOperator.StemSnapHorizontal),
            EncodeInteger(1), Operator(CffDictOperator.ForceBold),
            EncodeInteger(9), Operator(CffDictOperator.ForceBoldThreshold),
            EncodeInteger(-1), Operator(CffDictOperator.LenIv),
            EncodeInteger(12), Operator(CffDictOperator.LocalSubroutines),
            EncodeInteger(500), Operator(CffDictOperator.DefaultWidthX),
            EncodeInteger(40), Operator(CffDictOperator.NominalWidthX));
        CffPrivateDictionary dictionary = CffPrivateDictionary.Parse(CreateSlice(bytes));
        CffPrivateDictionary defaults = CffPrivateDictionary.Parse(CreateSlice(Array.Empty<byte>()));

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.CopyBlueValues(), Is.EqualTo(new double[] { -20, -10, 390, 410 }));
            Assert.That(dictionary.GetBlueScale(), Is.EqualTo(0.05).Within(0.0000001));
            Assert.That(dictionary.CopyStemSnapHorizontal(), Is.EqualTo(new double[] { 50, 70 }));
            Assert.That(dictionary.IsForceBold(), Is.True);
            Assert.That(dictionary.HasForceBoldThreshold(), Is.True);
            Assert.That(dictionary.GetForceBoldThreshold(), Is.EqualTo(9));
            Assert.That(dictionary.HasLenIv(), Is.True);
            Assert.That(dictionary.GetLenIv(), Is.EqualTo(-1));
            Assert.That(dictionary.HasLocalSubroutines(), Is.True);
            Assert.That(dictionary.GetLocalSubroutinesOffset(), Is.EqualTo(12));
            Assert.That(dictionary.GetDefaultWidthX(), Is.EqualTo(500));
            Assert.That(dictionary.GetNominalWidthX(), Is.EqualTo(40));
            Assert.That(defaults.CopyBlueValues(), Is.Empty);
            Assert.That(defaults.GetBlueScale(), Is.EqualTo(0.039625));
            Assert.That(defaults.GetBlueShift(), Is.EqualTo(7));
            Assert.That(defaults.GetBlueFuzz(), Is.EqualTo(1));
            Assert.That(defaults.GetLanguageGroup(), Is.Zero);
            Assert.That(defaults.GetExpansionFactor(), Is.EqualTo(0.06));
            Assert.That(defaults.GetInitialRandomSeed(), Is.Zero);
            Assert.That(defaults.GetDefaultWidthX(), Is.Zero);
            Assert.That(defaults.GetNominalWidthX(), Is.Zero);
        });
    }

    [Test]
    public void DecodesFontDictionaryNameMatrixAndPrivateReference()
    {
        CffStringTable strings = CreateStringTable(Bytes("FD Regular"));
        byte[] bytes = Join(
            EncodeInteger(391), Operator(CffDictOperator.FontName),
            EncodeInteger(1), EncodeInteger(0), EncodeInteger(0), EncodeInteger(1),
            EncodeInteger(7), EncodeInteger(8), Operator(CffDictOperator.FontMatrix),
            EncodeInteger(14), EncodeInteger(200), Operator(CffDictOperator.Private));
        CffFontDictionary dictionary = CffFontDictionary.Parse(CreateSlice(bytes), strings);

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.GetFontName(), Is.EqualTo("FD Regular"));
            Assert.That(dictionary.HasFontMatrix(), Is.True);
            Assert.That(dictionary.CopyFontMatrix(), Is.EqualTo(new double[] { 1, 0, 0, 1, 7, 8 }));
            Assert.That(dictionary.GetPrivateDictionarySize(), Is.EqualTo(14));
            Assert.That(dictionary.GetPrivateDictionaryOffset(), Is.EqualTo(200));
        });
    }

    [Test]
    public void RejectsMalformedTypedDictionaries()
    {
        CffStringTable strings = CreateStringTable(Bytes("Only Custom"));
        byte[] validRequired = RequiredTopEntries();
        byte[] missingCharStrings = Join(
            EncodeInteger(0), EncodeInteger(10), Operator(CffDictOperator.Private));
        byte[] realCharStrings = Join(
            EncodeReal(0x1F), Operator(CffDictOperator.CharStrings),
            EncodeInteger(0), EncodeInteger(10), Operator(CffDictOperator.Private));
        byte[] duplicateCharStrings = Join(
            EncodeInteger(1), Operator(CffDictOperator.CharStrings),
            validRequired);
        byte[] invalidSid = Join(
            EncodeInteger(392), Operator(CffDictOperator.FullName),
            validRequired);
        byte[] invalidBoolean = Join(
            EncodeInteger(2), Operator(CffDictOperator.IsFixedPitch),
            validRequired);
        byte[] invalidCharstringType = Join(
            EncodeInteger(1), Operator(CffDictOperator.CharstringType),
            validRequired);
        byte[] shortPrivate = Join(
            EncodeInteger(1), Operator(CffDictOperator.Private),
            EncodeInteger(1), Operator(CffDictOperator.CharStrings));
        byte[] incompleteCid = Join(
            EncodeInteger(0), EncodeInteger(0), EncodeInteger(0),
            Operator(CffDictOperator.RegistryOrderingSupplement),
            EncodeInteger(1), Operator(CffDictOperator.CharStrings));
        byte[] oddBlueValues = Join(
            EncodeInteger(1), EncodeInteger(2), EncodeInteger(3),
            Operator(CffDictOperator.BlueValues));
        byte[] invalidForceBold = Join(
            EncodeInteger(2), Operator(CffDictOperator.ForceBold));
        byte[] noFontPrivate = Join(
            EncodeInteger(391), Operator(CffDictOperator.FontName));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(CreateSlice(missingCharStrings), strings));
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(CreateSlice(realCharStrings), strings));
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(CreateSlice(duplicateCharStrings), strings));
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(CreateSlice(invalidSid), strings));
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(CreateSlice(invalidBoolean), strings));
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(CreateSlice(invalidCharstringType), strings));
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(CreateSlice(shortPrivate), strings));
            Assert.Throws<FontFormatException>(() => CffTopDictionary.Parse(CreateSlice(incompleteCid), strings));
            Assert.Throws<FontFormatException>(() => CffPrivateDictionary.Parse(CreateSlice(oddBlueValues)));
            Assert.Throws<FontFormatException>(() => CffPrivateDictionary.Parse(CreateSlice(invalidForceBold)));
            Assert.Throws<FontFormatException>(() => CffFontDictionary.Parse(CreateSlice(noFontPrivate), strings));
            Assert.Throws<ArgumentNullException>(() => new CffTopDictionary(ParseDictionary(validRequired), null!));
            Assert.Throws<ArgumentNullException>(() => new CffFontDictionary(ParseDictionary(noFontPrivate), null!));
        });
    }

    [Test]
    public void TableLazilyCachesTypedDictionariesAndBoundsPrivateData()
    {
        byte[] privateBytes = Join(
            EncodeInteger(600), Operator(CffDictOperator.DefaultWidthX));
        SfntCffTable? table = ParseTable(BuildCffWithPrivate(privateBytes));
        CffTopDictionary top = table!.GetTopDictionary();
        CffPrivateDictionary? privateDictionary = table.GetPrivateDictionary();
        byte[] invalidCff = BuildCff(
            Join(
                EncodeInteger(1), Operator(CffDictOperator.CharStrings),
                EncodeInteger(2), EncodeInteger(1000), Operator(CffDictOperator.Private)),
            Array.Empty<byte>());
        SfntCffTable? invalid = ParseTable(invalidCff);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetStringTable().GetStandardStringCount(), Is.EqualTo(391));
            Assert.That(table.GetTopDictionary(), Is.SameAs(top));
            Assert.That(table.GetPrivateDictionary(), Is.SameAs(privateDictionary));
            Assert.That(privateDictionary!.GetDefaultWidthX(), Is.EqualTo(600));
            Assert.Throws<FontFormatException>(() => invalid!.GetPrivateDictionary());
        });
    }

    static CffDictionary ParseDictionary(byte[] bytes)
    {
        return CffDictParser.Parse(CreateSlice(bytes));
    }

    static CffStringTable CreateStringTable(params byte[][] values)
    {
        byte[] indexBytes = BuildIndex(values);
        return new CffStringTable(CffIndexParser.Parse(CreateSlice(indexBytes), 0));
    }

    static byte[] RequiredTopEntries()
    {
        return Join(
            EncodeInteger(1), Operator(CffDictOperator.CharStrings),
            EncodeInteger(0), EncodeInteger(10), Operator(CffDictOperator.Private));
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

        if (value >= -1131 && value <= -108)
        {
            int adjusted = -value - 108;
            return new byte[]
            {
                (byte)(251 + adjusted / 256),
                (byte)(adjusted % 256)
            };
        }

        if (value >= short.MinValue && value <= short.MaxValue)
        {
            return new TestBinaryBuilder()
                .WriteUInt8(28)
                .WriteInt16((short)value)
                .CopyBytes();
        }

        return new TestBinaryBuilder()
            .WriteUInt8(29)
            .WriteInt32(value)
            .CopyBytes();
    }

    static byte[] EncodeReal(params byte[] packedNibbles)
    {
        return Join(new byte[] { 30 }, packedNibbles);
    }

    static byte[] Operator(int operatorCode)
    {
        if (operatorCode >= 0x0C00)
        {
            return new byte[] { 12, (byte)(operatorCode & 255) };
        }

        return new byte[] { (byte)operatorCode };
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

    static byte[] BuildIndex(params byte[][] values)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)values.Length);

        if (values.Length == 0)
        {
            return builder.CopyBytes();
        }

        builder.WriteUInt8(1);
        int offset = 1;
        builder.WriteUInt8((byte)offset);

        foreach (byte[] value in values)
        {
            offset += value.Length;
            builder.WriteUInt8((byte)offset);
        }

        foreach (byte[] value in values)
        {
            builder.WriteBytes(value);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildCffWithPrivate(byte[] privateBytes)
    {
        byte[] placeholderTop = Join(
            EncodeInteger(1), Operator(CffDictOperator.CharStrings),
            EncodeInteger(privateBytes.Length), EncodeInteger(0),
            Operator(CffDictOperator.Private));
        byte[] placeholder = BuildCff(placeholderTop, privateBytes);
        int privateOffset = placeholder.Length - privateBytes.Length;
        byte[] top = Join(
            EncodeInteger(1), Operator(CffDictOperator.CharStrings),
            EncodeInteger(privateBytes.Length), EncodeInteger(privateOffset),
            Operator(CffDictOperator.Private));
        return BuildCff(top, privateBytes);
    }

    static byte[] BuildCff(byte[] top, byte[] remaining)
    {
        return Join(
            new byte[] { 1, 0, 4, 1 },
            BuildIndex(new byte[] { (byte)'X' }),
            BuildIndex(top),
            BuildIndex(),
            BuildIndex(),
            remaining);
    }

    static SfntCffTable? ParseTable(byte[] bytes)
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
            FontLimits.CreateCompatible());
        return SfntCffTableParser.FindTable(cache);
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
