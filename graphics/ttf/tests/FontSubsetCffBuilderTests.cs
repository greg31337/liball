using ttf;

namespace ttf.Tests;

internal class FontSubsetCffBuilderTests
{
    [Test]
    public void RewritesCompactNonCidProgramsStringsEncodingAndSubroutines()
    {
        FontFace sourceFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            BuildNonCidCff(),
            4);
        FontSubset subset = BuildSubset(sourceFace, new int[] { 1, 3 });
        byte[] bytes = subset.CreateCffData().CopyData();
        FontFace outputFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            bytes,
            3);
        SfntCffTable? output = outputFace.GetCffTable();
        FontInstance source = sourceFace.CreateDefaultInstance();
        FontInstance instance = outputFace.CreateDefaultInstance();
        CffIndex? localSubroutines =
            output!.GetLocalSubroutineIndexForSubsetting(0);
        FontSubsetClosureReport closure = subset.GetClosureReport();

        Assert.Multiple(() =>
        {
            Assert.That(output.GetCharStringsIndex().GetCount(),
                Is.EqualTo(3));
            Assert.That(output.GetCharset().GetName(1), Is.EqualTo("A"));
            Assert.That(output.GetCharset().GetName(2), Is.EqualTo("C"));
            Assert.That(output.GetTopDictionary().GetFullName(),
                Is.EqualTo("Demo"));
            Assert.That(output.GetStringIndex().GetCount(), Is.EqualTo(3));
            Assert.That(output.GetEncoding()!.GetGlyphIndex(65),
                Is.EqualTo(1));
            Assert.That(output.GetEncoding()!.GetGlyphIndex(67),
                Is.EqualTo(2));
            Assert.That(output.GetEncoding()!.HasCode(66), Is.False);
            Assert.That(output.GetGlobalSubroutineIndex().GetCount(),
                Is.EqualTo(1));
            Assert.That(output.GetGlobalSubroutineIndex().CopyObject(0),
                Is.EqualTo(new byte[] { 149, 139, 5, 11 }));
            Assert.That(localSubroutines!.GetCount(), Is.EqualTo(2));
            Assert.That(localSubroutines.CopyObject(0),
                Is.EqualTo(new byte[] { 11 }));
            Assert.That(localSubroutines.CopyObject(1),
                Is.EqualTo(new byte[] { 139, 149, 5, 11 }));
            Assert.That(closure.IsCffApplicable(), Is.True);
            Assert.That(closure.GetCffGlobalSubroutineIndices(),
                Is.EqualTo(new int[] { 0 }));
            Assert.That(closure.GetCffLocalSubroutineSetCount(), Is.EqualTo(1));
            Assert.That(closure.GetCffLocalSubroutineSet(0)
                .GetFontDictionaryIndex(), Is.Zero);
            Assert.That(closure.GetCffLocalSubroutineSet(0)
                .GetSubroutineIndices(), Is.EqualTo(new int[] { 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                closure.GetCffLocalSubroutineSet(1));
            AssertOutlinesEqual(source.GetGlyphOutline(1),
                instance.GetGlyphOutline(1));
            AssertOutlinesEqual(source.GetGlyphOutline(3),
                instance.GetGlyphOutline(2));
        });
    }

    [Test]
    public void RewritesCidCharsetFontDictionariesAndPrivateData()
    {
        FontFace sourceFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);
        FontSubset subset = BuildSubset(sourceFace, new int[] { 1 });
        FontSubsetCffData data = subset.CreateCffData();
        byte[] first = data.CopyData();
        byte[] second = data.CopyData();
        first[0] = 0;
        FontFace outputFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            second,
            2);
        SfntCffTable? output = outputFace.GetCffTable();
        CffCidMetadata? cid = output!.GetCidMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(data.CopyData()[0], Is.EqualTo(1));
            Assert.That(output.GetCharStringsIndex().GetCount(),
                Is.EqualTo(2));
            Assert.That(cid!.GetCharset().GetIdentifier(0), Is.Zero);
            Assert.That(cid.GetCharset().GetIdentifier(1), Is.EqualTo(5));
            Assert.That(cid.GetCidCount(), Is.EqualTo(6));
            Assert.That(cid.GetFontDictionaries().GetCount(), Is.EqualTo(1));
            Assert.That(cid.GetFontDictionarySelect()
                .GetFontDictionaryIndex(0), Is.Zero);
            Assert.That(cid.GetFontDictionarySelect()
                .GetFontDictionaryIndex(1), Is.Zero);
            Assert.That(cid.GetFontDictionaries().GetRecord(0)
                .GetDictionary().GetFontName(), Is.EqualTo("FD0"));
            Assert.That(cid.GetFontDictionaries().GetRecord(0)
                .GetPrivateDictionary().GetDefaultWidthX(), Is.EqualTo(500));
            Assert.That(output.GetStringIndex().GetCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void PreserveGidWritesValidEmptyProgramsNamesAndEncoding()
    {
        FontFace sourceFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            BuildNonCidCff(),
            4);
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetGlyphRetentionMode(
            FontSubsetGlyphRetentionMode.Preserve);
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            sourceFace.CreateDefaultInstance(),
            options);
        builder.AddGlyph(3);
        byte[] bytes = builder.Build().CreateCffData().CopyData();
        FontFace outputFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            bytes,
            4);
        SfntCffTable? output = outputFace.GetCffTable();

        Assert.Multiple(() =>
        {
            Assert.That(output!.GetCharStringsIndex().GetCount(),
                Is.EqualTo(4));
            Assert.That(output.GetCharStringsIndex().CopyObject(1),
                Is.EqualTo(new byte[] { 14 }));
            Assert.That(output.GetCharStringsIndex().CopyObject(2),
                Is.EqualTo(new byte[] { 14 }));
            Assert.That(output.GetCharset().GetName(1),
                Is.EqualTo(".empty00001"));
            Assert.That(output.GetCharset().GetName(2),
                Is.EqualTo(".empty00002"));
            Assert.That(output.GetCharset().GetName(3), Is.EqualTo("C"));
            Assert.That(output.GetEncoding()!.GetGlyphIndex(67),
                Is.EqualTo(3));
            Assert.That(outputFace.CreateDefaultInstance()
                .GetGlyphOutline(1).IsEmpty(), Is.True);
            Assert.That(outputFace.CreateDefaultInstance()
                .GetGlyphOutline(2).IsEmpty(), Is.True);
        });
    }

    static FontSubset BuildSubset(FontFace face, int[] glyphIds)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            options);
        int index = 0;

        while (index < glyphIds.Length)
        {
            builder.AddGlyph(glyphIds[index]);
            index += 1;
        }

        return builder.Build();
    }

    static void AssertOutlinesEqual(
        GlyphOutline expected,
        GlyphOutline actual)
    {
        Assert.That(actual.GetCommandCount(),
            Is.EqualTo(expected.GetCommandCount()));
        int index = 0;

        while (index < expected.GetCommandCount())
        {
            GlyphPathCommand expectedCommand = expected.GetCommand(index);
            GlyphPathCommand actualCommand = actual.GetCommand(index);
            Assert.That(actualCommand.GetKind(),
                Is.EqualTo(expectedCommand.GetKind()));

            if (expectedCommand.GetKind() != GlyphPathCommandKind.Close)
            {
                Assert.That(actualCommand.GetEndX(),
                    Is.EqualTo(expectedCommand.GetEndX()));
                Assert.That(actualCommand.GetEndY(),
                    Is.EqualTo(expectedCommand.GetEndY()));
            }

            index += 1;
        }
    }

    internal static byte[] BuildNonCidCff()
    {
        byte[] strings = BuildIndex(
            Bytes("A"),
            Bytes("B"),
            Bytes("C"),
            Bytes("Unused"),
            Bytes("Demo"));
        byte[] globalSubroutines = BuildIndex(
            new byte[] { 149, 139, 5, 11 },
            new byte[] { 159, 139, 5, 11 });
        byte[] charStrings = BuildIndex(
            new byte[] { 14 },
            new byte[] { 139, 139, 21, 32, 29, 14 },
            new byte[] { 14 },
            new byte[] { 139, 139, 21, 33, 10, 14 });
        byte[] charset = Join(
            new byte[] { 0 },
            UInt16(391),
            UInt16(392),
            UInt16(393));
        byte[] encoding = new byte[] { 0, 3, 65, 66, 67 };
        byte[] localSubroutines = BuildIndex(
            new byte[] { 149, 149, 5, 11 },
            new byte[] { 139, 149, 5, 11 });
        byte[] privateDictionary = BuildPrivateDictionary(0);
        privateDictionary = BuildPrivateDictionary(privateDictionary.Length);
        int prefixLength = BuildPrefix(
            BuildTopDictionary(0, 0, 0, privateDictionary.Length, 0),
            strings,
            globalSubroutines).Length;
        int attempt = 0;

        while (attempt < 8)
        {
            int charStringsOffset = prefixLength;
            int charsetOffset = charStringsOffset + charStrings.Length;
            int encodingOffset = charsetOffset + charset.Length;
            int privateOffset = encodingOffset + encoding.Length;
            byte[] top = BuildTopDictionary(
                charStringsOffset,
                charsetOffset,
                encodingOffset,
                privateDictionary.Length,
                privateOffset);
            byte[] prefix = BuildPrefix(top, strings, globalSubroutines);

            if (prefix.Length == prefixLength)
            {
                return Join(
                    prefix,
                    charStrings,
                    charset,
                    encoding,
                    privateDictionary,
                    localSubroutines);
            }

            prefixLength = prefix.Length;
            attempt += 1;
        }

        throw new InvalidOperationException(
            "The CFF subset test offsets did not stabilize.");
    }

    static byte[] BuildTopDictionary(
        int charStringsOffset,
        int charsetOffset,
        int encodingOffset,
        int privateSize,
        int privateOffset)
    {
        return Join(
            EncodeInteger(395),
            Operator(CffDictOperator.FullName),
            EncodeInteger(charsetOffset),
            Operator(CffDictOperator.Charset),
            EncodeInteger(encodingOffset),
            Operator(CffDictOperator.Encoding),
            EncodeInteger(charStringsOffset),
            Operator(CffDictOperator.CharStrings),
            EncodeInteger(privateSize),
            EncodeInteger(privateOffset),
            Operator(CffDictOperator.Private));
    }

    static byte[] BuildPrivateDictionary(int subroutineOffset)
    {
        return Join(
            EncodeInteger(subroutineOffset),
            Operator(CffDictOperator.LocalSubroutines),
            EncodeInteger(500),
            Operator(CffDictOperator.DefaultWidthX));
    }

    static byte[] BuildPrefix(
        byte[] top,
        byte[] strings,
        byte[] globalSubroutines)
    {
        return Join(
            new byte[] { 1, 0, 4, 1 },
            BuildIndex(Bytes("SubsetTest")),
            BuildIndex(top),
            strings,
            globalSubroutines);
    }

    static byte[] BuildIndex(params byte[][] values)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)values.Length);

        if (values.Length == 0)
        {
            return builder.CopyBytes();
        }

        int length = 1;
        int index = 0;
        while (index < values.Length)
        {
            length += values[index].Length;
            index += 1;
        }

        int offsetSize = length <= 255 ? 1 : 2;
        builder.WriteUInt8((byte)offsetSize);
        int offset = 1;
        WriteOffset(builder, offsetSize, offset);
        index = 0;

        while (index < values.Length)
        {
            offset += values[index].Length;
            WriteOffset(builder, offsetSize, offset);
            index += 1;
        }

        index = 0;
        while (index < values.Length)
        {
            builder.WriteBytes(values[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static void WriteOffset(
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
        return new byte[]
        {
            29,
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
    }

    static byte[] Operator(int value)
    {
        return value >= 0x0C00 ?
            new byte[] { 12, (byte)value } :
            new byte[] { (byte)value };
    }

    static byte[] UInt16(int value)
    {
        return new byte[] { (byte)(value >> 8), (byte)value };
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.ASCII.GetBytes(value);
    }

    static byte[] Join(params byte[][] values)
    {
        int length = 0;
        int index = 0;
        while (index < values.Length)
        {
            length += values[index].Length;
            index += 1;
        }

        byte[] result = new byte[length];
        int offset = 0;
        index = 0;
        while (index < values.Length)
        {
            Array.Copy(values[index], 0, result, offset, values[index].Length);
            offset += values[index].Length;
            index += 1;
        }

        return result;
    }
}
