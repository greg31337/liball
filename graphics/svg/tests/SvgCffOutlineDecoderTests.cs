namespace svg.Tests;

public class SvgCffOutlineDecoderTests
{
    [Test]
    public void DecodesCff1PathsAndSharedLocalAndGlobalSubroutines()
    {
        byte[] empty = new byte[] { 14 };
        byte[] glyph = new byte[]
        {
            239, 239, 21,
            32, 29, 5,
            139, 239, 39, 139, 5,
            32, 10,
            14
        };
        byte[] global = new byte[] { 239, 139, 11 };
        byte[] local = new byte[] { 139, 89, 89, 89, 139, 89, 8, 11 };
        SvgFont font = CreateFont(BuildCff1(new byte[][] { empty, glyph }, new byte[][] { global }, new byte[][] { local }), false, 2);

        SvgCffGlyphResult result = font.DecodeCffGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgCffError.None));
            Assert.That(result.RequiresFallback, Is.False);
            Assert.That(result.Outline.Cff2, Is.False);
            Assert.That(result.Outline.Empty, Is.False);
            Assert.That(result.Outline.ContourCount, Is.EqualTo(1));
            Assert.That(result.Outline.SubroutineCalls, Is.EqualTo(2));
            Assert.That(result.Outline.Path.Commands.Select(command => command.Kind), Is.EqualTo(new SvgPathCommandKind[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.CubicCurveTo,
                SvgPathCommandKind.ClosePath
            }));
            Assert.That(result.Outline.Path.Commands[0].Values, Is.EqualTo(new double[] { 100, 100 }));
            Assert.That(result.Outline.Path.Commands[1].Values, Is.EqualTo(new double[] { 200, 100 }));
            Assert.That(result.Outline.Path.Commands[3].Values, Is.EqualTo(new double[] { 100, 200 }));
            Assert.That(result.Outline.Path.Commands[4].Values, Is.EqualTo(new double[] { 100, 150, 50, 100, 50, 50 }));
        });
    }

    [Test]
    public void DecodesCff2IndexesDictionariesAndExhaustedCharStrings()
    {
        byte[] empty = Array.Empty<byte>();
        byte[] glyph = new byte[] { 239, 239, 21, 189, 139, 189, 189, 139, 189, 8 };
        SvgFont font = CreateFont(BuildCff2(new byte[][] { empty, glyph }), true, 2);

        SvgCffGlyphResult result = new SvgCffOutlineDecoder().DecodeWithFontAndGlyphIndex(font, 1);
        SvgCffGlyphResult missing = font.DecodeCffGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Outline.Cff2, Is.True);
            Assert.That(result.Outline.Path.Commands.Select(command => command.Kind), Is.EqualTo(new SvgPathCommandKind[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.CubicCurveTo,
                SvgPathCommandKind.ClosePath
            }));
            Assert.That(result.Outline.Path.Commands[1].Values, Is.EqualTo(new double[] { 150, 100, 200, 150, 200, 200 }));
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.Outline.Empty, Is.True);
            Assert.That(missing.Outline.Path.Commands, Is.Empty);
        });
    }

    [Test]
    public void ClassifiesVariationAndUnsupportedOperatorsForCallerFallback()
    {
        SvgFont variationFont = CreateFont(BuildCff2(new byte[][] { new byte[] { 139, 15 } }), true, 1);
        SvgFont unsupportedFont = CreateFont(BuildCff1(new byte[][] { new byte[] { 12, 0, 14 } }, Array.Empty<byte[]>(), Array.Empty<byte[]>()), false, 1);

        SvgCffGlyphResult variation = variationFont.DecodeCffGlyph(0);
        SvgCffGlyphResult unsupported = unsupportedFont.DecodeCffGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(variation.Success, Is.False);
            Assert.That(variation.Error, Is.EqualTo(SvgCffError.UnsupportedOperator));
            Assert.That(variation.RequiresFallback, Is.True);
            Assert.That(variation.FallbackReason, Is.EqualTo(SvgCffFallbackReason.VariationData));
            Assert.That(variation.UnsupportedOperator, Is.EqualTo(15));
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Error, Is.EqualTo(SvgCffError.UnsupportedOperator));
            Assert.That(unsupported.RequiresFallback, Is.True);
            Assert.That(unsupported.FallbackReason, Is.EqualTo(SvgCffFallbackReason.UnsupportedOperator));
            Assert.That(unsupported.UnsupportedOperator, Is.EqualTo(3072));
        });
    }

    [Test]
    public void EnforcesOperandPointSubroutineAndRecursionLimits()
    {
        byte[] operandsGlyph = new byte[] { 139, 139, 21, 139, 139, 139, 5, 14 };
        SvgFont operandsFont = CreateFont(BuildCff1(new byte[][] { operandsGlyph }, Array.Empty<byte[]>(), Array.Empty<byte[]>()), false, 1);
        SvgCffOutlineOptions operandOptions = new SvgCffOutlineOptions();
        operandOptions.MaximumOperands = 2;
        SvgCffGlyphResult operands = operandsFont.DecodeCffGlyphWithOptions(0, operandOptions);

        byte[] formatOperandGlyph = new byte[50];
        int operandIndex = 0;
        while (operandIndex < 49)
        {
            formatOperandGlyph[operandIndex] = 139;
            operandIndex = operandIndex + 1;
        }
        formatOperandGlyph[49] = 14;
        SvgFont formatOperandFont = CreateFont(BuildCff1(new byte[][] { formatOperandGlyph }, Array.Empty<byte[]>(), Array.Empty<byte[]>()), false, 1);
        SvgCffGlyphResult formatOperands = formatOperandFont.DecodeCffGlyph(0);

        byte[] pointGlyph = new byte[] { 239, 239, 21, 189, 139, 189, 189, 139, 189, 8 };
        SvgFont pointFont = CreateFont(BuildCff2(new byte[][] { pointGlyph }), true, 1);
        SvgCffOutlineOptions pointOptions = new SvgCffOutlineOptions();
        pointOptions.Limits.MaxPointsPerGlyph = 3;
        SvgCffGlyphResult points = pointFont.DecodeCffGlyphWithOptions(0, pointOptions);

        byte[] recursiveGlyph = new byte[] { 32, 29, 14 };
        byte[] recursiveGlobal = new byte[] { 32, 29, 11 };
        SvgFont recursiveFont = CreateFont(BuildCff1(new byte[][] { recursiveGlyph }, new byte[][] { recursiveGlobal }, Array.Empty<byte[]>()), false, 1);
        SvgCffGlyphResult recursion = recursiveFont.DecodeCffGlyph(0);

        byte[] twoCallsGlyph = new byte[] { 32, 29, 32, 29, 14 };
        SvgFont callFont = CreateFont(BuildCff1(new byte[][] { twoCallsGlyph }, new byte[][] { new byte[] { 11 } }, Array.Empty<byte[]>()), false, 1);
        SvgCffOutlineOptions callOptions = new SvgCffOutlineOptions();
        callOptions.MaximumSubroutineCalls = 1;
        SvgCffGlyphResult calls = callFont.DecodeCffGlyphWithOptions(0, callOptions);

        Assert.Multiple(() =>
        {
            Assert.That(operands.Error, Is.EqualTo(SvgCffError.OperandLimitExceeded));
            Assert.That(formatOperands.Error, Is.EqualTo(SvgCffError.OperandLimitExceeded));
            Assert.That(points.Error, Is.EqualTo(SvgCffError.PointLimitExceeded));
            Assert.That(recursion.Error, Is.EqualTo(SvgCffError.SubroutineLimitExceeded));
            Assert.That(calls.Error, Is.EqualTo(SvgCffError.SubroutineLimitExceeded));
        });
    }

    [Test]
    public void RejectsMalformedIndexesAndInvalidArguments()
    {
        byte[] bytes = BuildCff1(new byte[][] { new byte[] { 14 } }, Array.Empty<byte[]>(), Array.Empty<byte[]>());
        bytes[7] = 0;
        SvgFont font = CreateFont(bytes, false, 1);

        SvgCffGlyphResult malformed = font.DecodeCffGlyph(0);
        SvgCffGlyphResult badGlyph = font.DecodeCffGlyph(1);
        SvgCffGlyphResult badOptions = font.DecodeCffGlyphWithOptions(0, null);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error, Is.EqualTo(SvgCffError.InvalidIndex));
            Assert.That(malformed.RequiresFallback, Is.False);
            Assert.That(badGlyph.Error, Is.EqualTo(SvgCffError.InvalidArgument));
            Assert.That(badOptions.Error, Is.EqualTo(SvgCffError.InvalidArgument));
        });
    }

    static SvgFont CreateFont(byte[] bytes, bool cff2, int glyphCount)
    {
        SvgFont font = new SvgFont(bytes, false);
        font.OutlineKind = cff2 ? SvgOpenTypeOutlineKind.Cff2 : SvgOpenTypeOutlineKind.Cff;
        font.Tables.Add(new SvgOpenTypeTable(cff2 ? "CFF2" : "CFF ", 0, 0, bytes.Length));
        font.MaximumProfile = new SvgOpenTypeMaximumProfileTable();
        font.MaximumProfile.GlyphCount = glyphCount;
        font.Success = true;
        return font;
    }

    static byte[] BuildCff1(byte[][] charStrings, byte[][] globalSubroutines, byte[][] localSubroutines)
    {
        byte[] nameIndex = BuildIndex(false, new byte[][] { new byte[] { (byte)'T' } });
        byte[] stringIndex = BuildIndex(false, Array.Empty<byte[]>());
        byte[] globalIndex = BuildIndex(false, globalSubroutines);
        byte[] charStringIndex = BuildIndex(false, charStrings);
        byte[] privateDictionary = localSubroutines.Length == 0 ? Array.Empty<byte>() : new byte[] { 141, 19 };
        byte[] localIndex = localSubroutines.Length == 0 ? Array.Empty<byte>() : BuildIndex(false, localSubroutines);
        int topDictionaryLength = 17;
        int topIndexLength = 2 + 1 + 2 + topDictionaryLength;
        int charStringsOffset = 4 + nameIndex.Length + topIndexLength + stringIndex.Length + globalIndex.Length;
        int privateOffset = charStringsOffset + charStringIndex.Length;
        List<byte> topDictionary = new List<byte>();
        AddDictionaryInteger(topDictionary, charStringsOffset);
        topDictionary.Add(17);
        AddDictionaryInteger(topDictionary, privateDictionary.Length);
        AddDictionaryInteger(topDictionary, privateOffset);
        topDictionary.Add(18);
        byte[] topIndex = BuildIndex(false, new byte[][] { topDictionary.ToArray() });
        List<byte> result = new List<byte>(privateOffset + privateDictionary.Length + localIndex.Length);
        result.AddRange(new byte[] { 1, 0, 4, 1 });
        result.AddRange(nameIndex);
        result.AddRange(topIndex);
        result.AddRange(stringIndex);
        result.AddRange(globalIndex);
        result.AddRange(charStringIndex);
        result.AddRange(privateDictionary);
        result.AddRange(localIndex);
        return result.ToArray();
    }

    static byte[] BuildCff2(byte[][] charStrings)
    {
        byte[] globalIndex = BuildIndex(true, Array.Empty<byte[]>());
        byte[] charStringIndex = BuildIndex(true, charStrings);
        byte[] fontDictionary = new byte[] { 139, 139, 18 };
        byte[] fontDictionaryIndex = BuildIndex(true, new byte[][] { fontDictionary });
        int topDictionaryLength = 13;
        int charStringsOffset = 5 + topDictionaryLength + globalIndex.Length;
        int fontDictionaryOffset = charStringsOffset + charStringIndex.Length;
        List<byte> topDictionary = new List<byte>();
        AddDictionaryInteger(topDictionary, charStringsOffset);
        topDictionary.Add(17);
        AddDictionaryInteger(topDictionary, fontDictionaryOffset);
        topDictionary.Add(12);
        topDictionary.Add(36);
        List<byte> result = new List<byte>();
        result.AddRange(new byte[] { 2, 0, 5, 0, (byte)topDictionaryLength });
        result.AddRange(topDictionary);
        result.AddRange(globalIndex);
        result.AddRange(charStringIndex);
        result.AddRange(fontDictionaryIndex);
        return result.ToArray();
    }

    static byte[] BuildIndex(bool cff2, byte[][] objects)
    {
        List<byte> result = new List<byte>();
        if (cff2) AddUInt32(result, (uint)objects.Length);
        else AddUInt16(result, objects.Length);
        if (objects.Length == 0) return result.ToArray();
        int dataLength = 0;
        int index = 0;
        while (index < objects.Length)
        {
            dataLength = dataLength + objects[index].Length;
            index = index + 1;
        }
        if (dataLength + 1 > 255) throw new InvalidOperationException("Test INDEX requires a wider offset size.");
        result.Add(1);
        int offset = 1;
        result.Add((byte)offset);
        index = 0;
        while (index < objects.Length)
        {
            offset = offset + objects[index].Length;
            result.Add((byte)offset);
            index = index + 1;
        }
        index = 0;
        while (index < objects.Length)
        {
            result.AddRange(objects[index]);
            index = index + 1;
        }
        return result.ToArray();
    }

    static void AddDictionaryInteger(List<byte> bytes, int value)
    {
        bytes.Add(29);
        AddUInt32(bytes, (uint)value);
    }

    static void AddUInt16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static void AddUInt32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }
}
