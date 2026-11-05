using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlEncodingSnifferTests
{
    [Test]
    public void DetectsStandardXmlInitialBytePatterns()
    {
        Assert.That(EncodingDetector.DetectInitialEncodingPattern([0x00, 0x00, 0x00, 0x3C]), Is.EqualTo(InitialEncodingPatternKind.Utf32BigEndian));
        Assert.That(EncodingDetector.DetectInitialEncodingPattern([0x3C, 0x00, 0x00, 0x00]), Is.EqualTo(InitialEncodingPatternKind.Utf32LittleEndian));
        Assert.That(EncodingDetector.DetectInitialEncodingPattern([0x00, 0x3C, 0x00, 0x3F]), Is.EqualTo(InitialEncodingPatternKind.Utf16BigEndian));
        Assert.That(EncodingDetector.DetectInitialEncodingPattern([0x3C, 0x00, 0x3F, 0x00]), Is.EqualTo(InitialEncodingPatternKind.Utf16LittleEndian));
        Assert.That(EncodingDetector.DetectInitialEncodingPattern([0x3C, 0x3F, 0x78, 0x6D]), Is.EqualTo(InitialEncodingPatternKind.AsciiCompatible));
        Assert.That(EncodingDetector.DetectInitialEncodingPattern([0x4C, 0x6F, 0xA7, 0x94]), Is.EqualTo(InitialEncodingPatternKind.Ebcdic));
        Assert.That(EncodingDetector.DetectInitialEncodingPattern([0x3C, 0x72, 0x2F, 0x3E]), Is.EqualTo(InitialEncodingPatternKind.Unknown));
    }

    [Test]
    public void BomSelectsByteOrderAndAcceptsMatchingGenericDeclaration()
    {
        string text = "<?xml version='1.0' encoding='uTf-16'?><r>\u20AC</r>";
        Encoding encoding = new UnicodeEncoding(true, true, true);
        XmlDecodedInput decoded = DecodeBytes(JoinBytes(encoding.GetPreamble(), encoding.GetBytes(text)), XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null);

        Assert.That(decoded.Text, Is.EqualTo(text));
        Assert.That(decoded.EffectiveEncodingName, Is.EqualTo("utf-16BE").IgnoreCase);
        Assert.That(decoded.DeclaredEncodingName, Is.EqualTo("uTf-16"));
        Assert.That(decoded.ByteOrderMark, Is.EqualTo(ByteOrderMarkKind.Utf16BigEndian));
        Assert.That(decoded.InitialPattern, Is.EqualTo(InitialEncodingPatternKind.Utf16BigEndian));
        Assert.That(decoded.HasDeclaration, Is.True);
        Assert.That(decoded.DeclarationCharacterCount, Is.EqualTo(text.IndexOf("?>", StringComparison.Ordinal) + 2));
        Assert.That(decoded.IsCharacterInput, Is.False);
    }

    [Test]
    public void AsciiCompatibleDeclarationSelectsLegacyEncodingCaseInsensitively()
    {
        string text = "<?xml version='1.0' encoding='IsO-8859-1'?><r>caf\u00E9</r>";

        XmlDecodedInput decoded = DecodeBytes(Encoding.Latin1.GetBytes(text), XmlEncodingEntityKind.Document, EncodingPolicyKind.BuiltInText, null);

        Assert.That(decoded.Text, Is.EqualTo(text));
        Assert.That(decoded.EffectiveEncodingName, Is.EqualTo("iso-8859-1"));
        Assert.That(decoded.DeclaredEncodingName, Is.EqualTo("IsO-8859-1"));
        Assert.That(decoded.ByteOrderMark, Is.EqualTo(ByteOrderMarkKind.None));
        Assert.That(decoded.InitialPattern, Is.EqualTo(InitialEncodingPatternKind.AsciiCompatible));
    }

    [Test]
    public void BomlessUndeclaredInputDefaultsToStrictUtf8()
    {
        string text = "<r>\u20AC</r>";

        XmlDecodedInput decoded = DecodeBytes(new UTF8Encoding(false, true).GetBytes(text), XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null);

        Assert.That(decoded.Text, Is.EqualTo(text));
        Assert.That(decoded.EffectiveEncodingName, Is.EqualTo("utf-8"));
        Assert.That(decoded.HasDeclaration, Is.False);
        Assert.That(() => DecodeBytes([0x3C, 0x72, 0x3E, 0xC0, 0x80, 0x3C, 0x2F, 0x72, 0x3E], XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null), Throws.TypeOf<DecoderFallbackException>());
    }

    [Test]
    public void BomlessWidePatternsRequireAndReconcileEncodingDeclarations()
    {
        string utf16Text = "<?xml version='1.0' encoding='UTF-16LE'?><r/>";
        string utf32Text = "<?xml version='1.0' encoding='UTF-32BE'?><r/>";

        XmlDecodedInput utf16 = DecodeBytes(new UnicodeEncoding(false, false, true).GetBytes(utf16Text), XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null);
        XmlDecodedInput utf32 = DecodeBytes(new UTF32Encoding(true, false, true).GetBytes(utf32Text), XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null);

        Assert.That(utf16.Text, Is.EqualTo(utf16Text));
        Assert.That(utf16.EffectiveEncodingName, Is.EqualTo("utf-16"));
        Assert.That(utf16.InitialPattern, Is.EqualTo(InitialEncodingPatternKind.Utf16LittleEndian));
        Assert.That(utf32.Text, Is.EqualTo(utf32Text));
        Assert.That(utf32.EffectiveEncodingName, Is.EqualTo("utf-32BE").IgnoreCase);
        Assert.That(utf32.InitialPattern, Is.EqualTo(InitialEncodingPatternKind.Utf32BigEndian));

        Assert.That(() => DecodeBytes(new UnicodeEncoding(true, false, true).GetBytes("<?xml version='1.0'?><r/>"), XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null), Throws.TypeOf<FormatException>());
        Assert.That(() => DecodeBytes(new UnicodeEncoding(false, false, true).GetBytes("<?xml encoding='UTF-16'?><r/>"), XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void ConflictingBomPatternCallerAndDeclarationEvidenceIsRejected()
    {
        byte[] utf8Conflict = JoinBytes(new UTF8Encoding(true, true).GetPreamble(), Encoding.UTF8.GetBytes("<?xml encoding='UTF-16'?><r/>"));
        byte[] utf16Conflict = new UnicodeEncoding(false, false, true).GetBytes("<?xml encoding='UTF-16BE'?><r/>");
        byte[] utf16Bytes = new UnicodeEncoding(false, false, true).GetBytes("<?xml encoding='UTF-16LE'?><r/>");

        Assert.That(() => DecodeBytes(utf8Conflict, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null), Throws.TypeOf<FormatException>());
        Assert.That(() => DecodeBytes(utf16Conflict, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null), Throws.TypeOf<FormatException>());
        Assert.That(() => DecodeBytes(utf16Bytes, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, "UTF-8"), Throws.TypeOf<FormatException>());
        Assert.That(() => DecodeBytes(JoinBytes(new UTF8Encoding(true, true).GetPreamble(), Encoding.UTF8.GetBytes("<r/>")), XmlEncodingEntityKind.Document, EncodingPolicyKind.BuiltInText, "US-ASCII"), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void UnsupportedAutodetectedByteOrdersAndFamiliesAreRejected()
    {
        Assert.That(EncodingDetector.DetectByteOrderMark([0x00, 0x00, 0xFF, 0xFE]), Is.EqualTo(ByteOrderMarkKind.Utf32Unusual2143));
        Assert.That(EncodingDetector.DetectByteOrderMark([0xFE, 0xFF, 0x00, 0x00]), Is.EqualTo(ByteOrderMarkKind.Utf32Unusual3412));
        Assert.That(() => DecodeBytes([0x00, 0x00, 0xFF, 0xFE, 0x00], XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null), Throws.TypeOf<NotSupportedException>());
        Assert.That(() => DecodeBytes([0x4C, 0x6F, 0xA7, 0x94], XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, null), Throws.TypeOf<NotSupportedException>());
    }

    [Test]
    public void TextReaderBypassesByteEncodingAndHonorsExternalDeclarationRules()
    {
        string text = "<?xml version='1.0' encoding='x-reader-decoded'?><r/>";
        TrackingTextReader reader = new TrackingTextReader(text);
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.TextReader;
        input.TextReader = reader;
        input.EncodingName = "UTF-16BE";
        input.OwnsResource = true;
        XmlInputDecoder decoder = new XmlInputDecoder(input, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, 4);

        XmlDecodedInput decoded = decoder.DecodeXmlInput();

        Assert.That(decoded.Text, Is.EqualTo(text));
        Assert.That(decoded.EffectiveEncodingName, Is.Null);
        Assert.That(decoded.DeclaredEncodingName, Is.EqualTo("x-reader-decoded"));
        Assert.That(decoded.IsCharacterInput, Is.True);
        Assert.That(decoder.DecodeXmlInput(), Is.SameAs(decoded));
        decoder.CloseXmlInputDecoder();
        decoder.CloseXmlInputDecoder();
        Assert.That(reader.DisposeCalls, Is.EqualTo(1));
        Assert.That(() => decoder.DecodeXmlInput(), Throws.TypeOf<ObjectDisposedException>());

        Assert.That(() => DecodeCharacters("<?xml version='1.0'?>entity", XmlEncodingEntityKind.ExternalParsedEntity), Throws.TypeOf<FormatException>());
        Assert.That(DecodeCharacters("<?xml version='1.0'?>document", XmlEncodingEntityKind.Document).Text, Is.EqualTo("<?xml version='1.0'?>document"));
        Assert.That(DecodeCharacters("<?xml-stylesheet href='a'?>text", XmlEncodingEntityKind.ExternalParsedEntity).HasDeclaration, Is.False);
    }

    private static XmlDecodedInput DecodeBytes(byte[] bytes, XmlEncodingEntityKind entityKind, EncodingPolicyKind policy, string? encodingName)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.ByteMemory;
        input.Bytes = bytes;
        input.EncodingName = encodingName;
        XmlInputDecoder decoder = new XmlInputDecoder(input, entityKind, policy, 3);
        return decoder.DecodeXmlInput();
    }

    private static XmlDecodedInput DecodeCharacters(string text, XmlEncodingEntityKind entityKind)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        XmlInputDecoder decoder = new XmlInputDecoder(input, entityKind, EncodingPolicyKind.UnicodeOnly, 4);
        return decoder.DecodeXmlInput();
    }

    private static byte[] JoinBytes(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    private sealed class TrackingTextReader : StringReader
    {
        internal int DisposeCalls;

        internal TrackingTextReader(string text) : base(text)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCalls = DisposeCalls + 1;
            }

            base.Dispose(disposing);
        }
    }
}
