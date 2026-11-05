using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class DecodingDeclarationCoverageTests
{
    [TestCase(XmlInputKind.String)]
    [TestCase(XmlInputKind.CharacterMemory)]
    [TestCase(XmlInputKind.ByteMemory)]
    [TestCase(XmlInputKind.Stream)]
    [TestCase(XmlInputKind.TextReader)]
    public void DecoderAcceptsEverySupportedInputKind(XmlInputKind kind)
    {
        string text = "<?xml version='1.0' encoding='UTF-8'?><r>\u20AC</r>";
        byte[] bytes = new UTF8Encoding(false, true).GetBytes(text);
        XmlInput input = new XmlInput();
        input.Kind = kind;
        if (kind == XmlInputKind.String)
        {
            input.Text = text;
        }
        else if (kind == XmlInputKind.CharacterMemory)
        {
            input.Characters = text.AsMemory();
        }
        else if (kind == XmlInputKind.ByteMemory)
        {
            input.Bytes = bytes;
        }
        else if (kind == XmlInputKind.Stream)
        {
            input.ByteStream = new BoundaryStream(bytes, 1);
        }
        else
        {
            input.TextReader = new StringReader(text);
        }

        XmlInputDecoder decoder = new XmlInputDecoder(input, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, 3);
        XmlDecodedInput decoded = decoder.DecodeXmlInput();

        Assert.That(decoded.Text, Is.EqualTo(text));
        Assert.That(decoded.HasDeclaration, Is.True);
        Assert.That(decoded.DeclaredEncodingName, Is.EqualTo("UTF-8"));
        Assert.That(decoded.DeclarationCharacterCount, Is.EqualTo(text.IndexOf("?>", StringComparison.Ordinal) + 2));
        if (kind == XmlInputKind.ByteMemory || kind == XmlInputKind.Stream)
        {
            Assert.That(decoded.EffectiveEncodingName, Is.EqualTo("utf-8"));
            Assert.That(decoded.RawByteOffsets[0], Is.Zero);
            Assert.That(decoded.RawByteOffsets[^1], Is.EqualTo(bytes.Length));
        }
        else
        {
            Assert.That(decoded.EffectiveEncodingName, Is.Null);
            Assert.That(decoded.RawByteOffsets, Has.All.EqualTo(-1));
        }

        decoder.CloseXmlInputDecoder();
    }

    [Test]
    public void DecoderSupportsEveryBomAndUnicodeByteOrderCombination()
    {
        object[][] cases =
        [
            [new UTF8Encoding(true, true), "UTF-8", ByteOrderMarkKind.Utf8, InitialEncodingPatternKind.AsciiCompatible],
            [new UnicodeEncoding(false, true, true), "UTF-16", ByteOrderMarkKind.Utf16LittleEndian, InitialEncodingPatternKind.Utf16LittleEndian],
            [new UnicodeEncoding(true, true, true), "UTF-16", ByteOrderMarkKind.Utf16BigEndian, InitialEncodingPatternKind.Utf16BigEndian],
            [new UTF32Encoding(false, true, true), "UTF-32", ByteOrderMarkKind.Utf32LittleEndian, InitialEncodingPatternKind.Utf32LittleEndian],
            [new UTF32Encoding(true, true, true), "UTF-32", ByteOrderMarkKind.Utf32BigEndian, InitialEncodingPatternKind.Utf32BigEndian],
        ];

        foreach (object[] item in cases)
        {
            Encoding encoding = (Encoding)item[0];
            string declaredName = (string)item[1];
            string text = "<?xml version='1.0' encoding='" + declaredName + "'?><r>\U0001F600</r>";
            byte[] payload = encoding.GetBytes(text);
            byte[] bytes = JoinBytes(encoding.GetPreamble(), payload);

            XmlDecodedInput decoded = DecodeByteMemory(bytes, null, EncodingPolicyKind.UnicodeOnly, 2);

            Assert.That(decoded.Text, Is.EqualTo(text), declaredName + " " + item[2]);
            Assert.That(decoded.ByteOrderMark, Is.EqualTo((ByteOrderMarkKind)item[2]));
            Assert.That(decoded.InitialPattern, Is.EqualTo((InitialEncodingPatternKind)item[3]));
            Assert.That(decoded.DeclaredEncodingName, Is.EqualTo(declaredName));
            Assert.That(decoded.RawByteOffsets[0], Is.EqualTo(encoding.GetPreamble().Length));
            Assert.That(decoded.RawByteOffsets[^1], Is.EqualTo(bytes.Length));
        }
    }

    [Test]
    public void DecoderSupportsEveryBomlessBuiltInEncodingCombination()
    {
        object[][] cases =
        [
            [new UTF8Encoding(false, true), "UTF-8", EncodingPolicyKind.UnicodeOnly, "\u20AC"],
            [new UnicodeEncoding(false, false, true), "UTF-16LE", EncodingPolicyKind.UnicodeOnly, "\u20AC"],
            [new UnicodeEncoding(true, false, true), "UTF-16BE", EncodingPolicyKind.UnicodeOnly, "\u20AC"],
            [new UTF32Encoding(false, false, true), "UTF-32LE", EncodingPolicyKind.UnicodeOnly, "\U0001F600"],
            [new UTF32Encoding(true, false, true), "UTF-32BE", EncodingPolicyKind.UnicodeOnly, "\U0001F600"],
            [EncodingDetector.CreateStrictAsciiEncoding(), "US-ASCII", EncodingPolicyKind.BuiltInText, "ASCII"],
            [EncodingDetector.CreateStrictLatin1Encoding(), "ISO-8859-1", EncodingPolicyKind.BuiltInText, "caf\u00E9"],
        ];

        foreach (object[] item in cases)
        {
            Encoding encoding = (Encoding)item[0];
            string declaredName = (string)item[1];
            EncodingPolicyKind policy = (EncodingPolicyKind)item[2];
            string text = "<?xml version='1.0' encoding='" + declaredName + "'?><r>" + (string)item[3] + "</r>";
            byte[] bytes = encoding.GetBytes(text);

            XmlDecodedInput decoded = DecodeByteMemory(bytes, null, policy, 3);

            Assert.That(decoded.Text, Is.EqualTo(text), declaredName);
            Assert.That(decoded.ByteOrderMark, Is.EqualTo(ByteOrderMarkKind.None), declaredName);
            Assert.That(decoded.DeclaredEncodingName, Is.EqualTo(declaredName));
            Assert.That(decoded.RawByteOffsets[0], Is.Zero);
            Assert.That(decoded.RawByteOffsets[^1], Is.EqualTo(bytes.Length));
        }
    }

    [Test]
    public void StreamDecoderAcceptsEveryByteSplitInBomAndDeclarationInputs()
    {
        object[][] cases =
        [
            [new UTF8Encoding(true, true), "UTF-8"],
            [new UnicodeEncoding(false, true, true), "UTF-16"],
            [new UnicodeEncoding(true, true, true), "UTF-16"],
            [new UTF32Encoding(false, true, true), "UTF-32"],
            [new UTF32Encoding(true, true, true), "UTF-32"],
        ];

        foreach (object[] item in cases)
        {
            Encoding encoding = (Encoding)item[0];
            string text = "<?xml version='1.1' encoding='" + (string)item[1] + "'?><r>\u20AC\U0001F600</r>";
            byte[] bytes = JoinBytes(encoding.GetPreamble(), encoding.GetBytes(text));
            int split = 1;
            while (split < bytes.Length)
            {
                BoundaryStream stream = new BoundaryStream(bytes, split);
                XmlInput input = new XmlInput();
                input.Kind = XmlInputKind.Stream;
                input.ByteStream = stream;
                XmlInputDecoder decoder = new XmlInputDecoder(input, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, 3);

                XmlDecodedInput decoded = decoder.DecodeXmlInput();

                Assert.That(decoded.Text, Is.EqualTo(text), encoding.WebName + " split " + split);
                Assert.That(decoded.RawByteOffsets[^1], Is.EqualTo(bytes.Length), encoding.WebName + " split " + split);
                decoder.CloseXmlInputDecoder();
                split = split + 1;
            }
        }
    }

    [Test]
    public void InputDecoderRejectsMalformedSequencesForEveryStrictEncodingFamily()
    {
        object[][] cases =
        [
            [new byte[] { 0xC0, 0x80 }, "UTF-8", EncodingPolicyKind.UnicodeOnly],
            [new byte[] { 0x00, 0xD8 }, "UTF-16LE", EncodingPolicyKind.UnicodeOnly],
            [new byte[] { 0xD8, 0x00 }, "UTF-16BE", EncodingPolicyKind.UnicodeOnly],
            [new byte[] { 0x00, 0x00, 0x11, 0x00 }, "UTF-32LE", EncodingPolicyKind.UnicodeOnly],
            [new byte[] { 0x00, 0x11, 0x00, 0x00 }, "UTF-32BE", EncodingPolicyKind.UnicodeOnly],
            [new byte[] { 0x80 }, "US-ASCII", EncodingPolicyKind.BuiltInText],
        ];

        foreach (object[] item in cases)
        {
            byte[] bytes = (byte[])item[0];
            string encodingName = (string)item[1];
            EncodingPolicyKind policy = (EncodingPolicyKind)item[2];

            Assert.Throws<DecoderFallbackException>(() => DecodeByteMemory(bytes, encodingName, policy, 2), encodingName);
        }
    }

    [TestCase("1.0", XmlVersion.Xml10)]
    [TestCase("1.1", XmlVersion.Xml11)]
    [TestCase("1.9", XmlVersion.Xml10)]
    public void DecodedDeclarationsRetainAndClassifyXmlVersionNumbers(string number, XmlVersion expected)
    {
        string text = "<?xml version='" + number + "' encoding='UTF-8'?><r/>";
        byte[] bytes = new UTF8Encoding(false, true).GetBytes(text);
        XmlDecodedInput decoded = DecodeByteMemory(bytes, null, EncodingPolicyKind.UnicodeOnly, 4);
        XmlDeclarationParser parser = new XmlDeclarationParser(decoded.Text.AsMemory(), XmlEncodingEntityKind.Document, 0);

        XmlDeclarationInfo declaration = parser.ParseXmlOrTextDeclaration();

        Assert.That(declaration.VersionNumber, Is.EqualTo(number));
        Assert.That(declaration.Version, Is.EqualTo(expected));
        Assert.That(declaration.CharacterCount, Is.EqualTo(decoded.DeclarationCharacterCount));
    }

    [TestCase("\r", XmlVersion.Xml10, 5)]
    [TestCase("\n", XmlVersion.Xml10, 5)]
    [TestCase("\r\n", XmlVersion.Xml10, 6)]
    [TestCase("\r", XmlVersion.Xml11, 5)]
    [TestCase("\n", XmlVersion.Xml11, 5)]
    [TestCase("\r\n", XmlVersion.Xml11, 6)]
    [TestCase("\r\u0085", XmlVersion.Xml11, 7)]
    [TestCase("\u0085", XmlVersion.Xml11, 6)]
    [TestCase("\u2028", XmlVersion.Xml11, 7)]
    public void EveryVersionSpecificLineEndingPreservesRawByteLocation(string lineEnding, XmlVersion version, int expectedSecondLineByteOffset)
    {
        Encoding encoding = new UTF8Encoding(true, true);
        byte[] bytes = JoinBytes(encoding.GetPreamble(), encoding.GetBytes("A" + lineEnding + "B"));
        XmlDecodedInput decoded = DecodeByteMemory(bytes, null, EncodingPolicyKind.UnicodeOnly, 2);

        XmlLineNormalizationResult normalized = LineNormalizer.NormalizeXmlLineEndings(decoded, version, XmlEncodingEntityKind.Document);
        SourceLocation start = normalized.GetNormalizedSourceLocation(0, "urn:test");
        SourceLocation breakLocation = normalized.GetNormalizedSourceLocation(1, "urn:test");
        SourceLocation secondLine = normalized.GetNormalizedSourceLocation(2, "urn:test");
        SourceLocation end = normalized.GetNormalizedSourceLocation(3, "urn:test");

        Assert.That(normalized.Text, Is.EqualTo("A\nB"));
        Assert.That(start.ByteOffset, Is.EqualTo(3));
        Assert.That(start.Line, Is.EqualTo(1));
        Assert.That(start.Column, Is.EqualTo(1));
        Assert.That(breakLocation.ByteOffset, Is.EqualTo(4));
        Assert.That(breakLocation.Line, Is.EqualTo(1));
        Assert.That(breakLocation.Column, Is.EqualTo(2));
        Assert.That(secondLine.ByteOffset, Is.EqualTo(expectedSecondLineByteOffset));
        Assert.That(secondLine.Line, Is.EqualTo(2));
        Assert.That(secondLine.Column, Is.EqualTo(1));
        Assert.That(end.ByteOffset, Is.EqualTo(bytes.Length));
        Assert.That(end.Line, Is.EqualTo(2));
        Assert.That(end.Column, Is.EqualTo(2));
    }

    private static XmlDecodedInput DecodeByteMemory(byte[] bytes, string? encodingName, EncodingPolicyKind policy, int bufferSize)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.ByteMemory;
        input.Bytes = bytes;
        input.EncodingName = encodingName;
        XmlInputDecoder decoder = new XmlInputDecoder(input, XmlEncodingEntityKind.Document, policy, bufferSize);
        return decoder.DecodeXmlInput();
    }

    private static byte[] JoinBytes(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    private sealed class BoundaryStream : MemoryStream
    {
        internal int Boundary;

        internal BoundaryStream(byte[] bytes, int boundary) : base(bytes)
        {
            Boundary = boundary;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position < Boundary && Position + count > Boundary)
            {
                count = Boundary - (int)Position;
            }

            return base.Read(buffer, offset, count);
        }
    }
}
