using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class ByteDecoderTests
{
    [Test]
    public void ResolvesXmlAndIanaEncodingNamesCaseInsensitively()
    {
        object[][] cases =
        [
            ["UtF-8", 65001, "utf-8"],
            ["UTF8", 65001, "utf-8"],
            ["uTf-16", 1200, "utf-16"],
            ["UTF-16LE", 1200, "utf-16"],
            ["Unicode", 1200, "utf-16"],
            ["Utf-16BE", 1201, "utf-16BE"],
            ["UnicodeFFFE", 1201, "utf-16BE"],
            ["UTF-32", 12000, "utf-32"],
            ["utf32le", 12000, "utf-32"],
            ["UTF-32BE", 12001, "utf-32BE"],
            ["Us-AsCiI", 20127, "us-ascii"],
            ["LaTiN1", 28591, "iso-8859-1"],
            ["ISO-8859-1", 28591, "iso-8859-1"],
        ];

        foreach (object[] item in cases)
        {
            Encoding encoding = EncodingDetector.ResolveXmlEncodingName((string)item[0], EncodingPolicyKind.BuiltInText);
            Assert.That(encoding.CodePage, Is.EqualTo((int)item[1]), (string)item[0]);
            Assert.That(encoding.WebName, Is.EqualTo((string)item[2]).IgnoreCase, (string)item[0]);
            Assert.That(encoding.DecoderFallback, Is.TypeOf<DecoderExceptionFallback>(), (string)item[0]);
        }
    }

    [Test]
    public void ReconstructsCallerEncodingWithStrictFallbackAndPrefersTheInstance()
    {
        XmlInput input = new XmlInput();
        input.Encoding = new UTF8Encoding(false, false);
        input.EncodingName = "us-ascii";

        Encoding selected = EncodingDetector.ResolveCallerSelectedEncoding(input, EncodingPolicyKind.UnicodeOnly);

        Assert.That(selected.CodePage, Is.EqualTo(65001));
        Assert.That(selected, Is.Not.SameAs(input.Encoding));
        Assert.That(selected.DecoderFallback, Is.TypeOf<DecoderExceptionFallback>());
        Assert.That(() => selected.GetString(new byte[] { 0xC0, 0x80 }), Throws.TypeOf<DecoderFallbackException>());
    }

    [Test]
    public void EncodingPolicyRejectsLegacyUnknownAndMissingSelections()
    {
        Assert.That(() => EncodingDetector.ResolveXmlEncodingName("US-ASCII", EncodingPolicyKind.UnicodeOnly), Throws.TypeOf<NotSupportedException>());
        Assert.That(() => EncodingDetector.ResolveXmlEncodingName("ISO-8859-1", EncodingPolicyKind.UnicodeOnly), Throws.TypeOf<NotSupportedException>());
        Assert.That(() => EncodingDetector.CreateStrictBuiltInEncoding(Encoding.ASCII, EncodingPolicyKind.UnicodeOnly), Throws.TypeOf<NotSupportedException>());
        Assert.That(() => EncodingDetector.ResolveXmlEncodingName("windows-1252", EncodingPolicyKind.BuiltInText), Throws.TypeOf<NotSupportedException>());
        Assert.That(() => EncodingDetector.ResolveXmlEncodingName(" UTF-8 ", EncodingPolicyKind.BuiltInText), Throws.TypeOf<NotSupportedException>());
        Assert.That(() => EncodingDetector.ResolveXmlEncodingName("", EncodingPolicyKind.BuiltInText), Throws.ArgumentException);
        Assert.That(() => EncodingDetector.ResolveXmlEncodingName("UTF-8", (EncodingPolicyKind)2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EncodingDetector.ResolveCallerSelectedEncoding(new XmlInput(), EncodingPolicyKind.BuiltInText), Throws.ArgumentException);
    }

    [Test]
    public void DecodesEveryPermittedBuiltInEncoding()
    {
        Assert.That(DecodeMemory(new byte[] { 0xEF, 0xBB, 0xBF, 0xC2, 0xA2 }, "UTF-8", EncodingPolicyKind.UnicodeOnly), Is.EqualTo("\u00A2"));
        Assert.That(DecodeMemory(new byte[] { 0x41, 0x00, 0x3D, 0xD8, 0x00, 0xDE }, "UTF-16LE", EncodingPolicyKind.UnicodeOnly), Is.EqualTo("A" + char.ConvertFromUtf32(0x1F600)));
        Assert.That(DecodeMemory(new byte[] { 0x00, 0x41, 0xD8, 0x3D, 0xDE, 0x00 }, "UTF-16BE", EncodingPolicyKind.UnicodeOnly), Is.EqualTo("A" + char.ConvertFromUtf32(0x1F600)));
        Assert.That(DecodeMemory(new byte[] { 0x41, 0x00, 0x00, 0x00 }, "UTF-32LE", EncodingPolicyKind.UnicodeOnly), Is.EqualTo("A"));
        Assert.That(DecodeMemory(new byte[] { 0x00, 0x00, 0x00, 0x41 }, "UTF-32BE", EncodingPolicyKind.UnicodeOnly), Is.EqualTo("A"));
        Assert.That(DecodeMemory(new byte[] { 0x41, 0x53, 0x43, 0x49, 0x49 }, "US-ASCII", EncodingPolicyKind.BuiltInText), Is.EqualTo("ASCII"));
        Assert.That(DecodeMemory(new byte[] { 0x63, 0x61, 0x66, 0xE9 }, "ISO-8859-1", EncodingPolicyKind.BuiltInText), Is.EqualTo("caf\u00E9"));
    }

    [Test]
    public void StrictBuiltInDecodersReportMalformedByteSequences()
    {
        AssertDecodeFailure([0xC0, 0x80], "UTF-8", EncodingPolicyKind.UnicodeOnly);
        AssertDecodeFailure([0x41], "UTF-16LE", EncodingPolicyKind.UnicodeOnly);
        AssertDecodeFailure([0x41, 0x00, 0x00], "UTF-32LE", EncodingPolicyKind.UnicodeOnly);
        AssertDecodeFailure([0x80], "US-ASCII", EncodingPolicyKind.BuiltInText);
    }

    [Test]
    public void FactoryDecodesShortReadsAndClosesOwnedInputOnce()
    {
        TrackingStream stream = new TrackingStream(new byte[] { 0xEF, 0xBB, 0xBF, 0xE2, 0x82, 0xAC }, 1);
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.Stream;
        input.ByteStream = stream;
        input.EncodingName = "utf-8";
        input.OwnsResource = true;
        ByteDecoder decoder = ByteDecoderFactory.CreateXmlInputByteDecoder(input, EncodingPolicyKind.UnicodeOnly, 2);

        Assert.That(decoder.InspectByteDecoderEncodingName(), Is.EqualTo("utf-8"));
        Assert.That(decoder.DecodeByteText(), Is.EqualTo("\u20AC"));
        Assert.That(stream.ReadCalls, Is.GreaterThan(4));
        decoder.CloseByteDecoder();
        decoder.CloseByteDecoder();
        Assert.That(stream.DisposeCalls, Is.EqualTo(1));
    }

    [Test]
    public void PolicyFailureDoesNotReadOrDisposeOwnedInput()
    {
        TrackingStream stream = new TrackingStream(new byte[] { 0x41 }, 1);
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.Stream;
        input.ByteStream = stream;
        input.EncodingName = "windows-1252";
        input.OwnsResource = true;

        Assert.That(() => ByteDecoderFactory.CreateXmlInputByteDecoder(input, EncodingPolicyKind.BuiltInText, 4), Throws.TypeOf<NotSupportedException>());
        Assert.That(stream.ReadCalls, Is.Zero);
        Assert.That(stream.DisposeCalls, Is.Zero);
        Assert.That(stream.ReadByte(), Is.EqualTo(0x41));
    }

    private static string DecodeMemory(byte[] bytes, string encodingName, EncodingPolicyKind policy)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.ByteMemory;
        input.Bytes = bytes;
        input.EncodingName = encodingName;
        ByteDecoder decoder = ByteDecoderFactory.CreateXmlInputByteDecoder(input, policy, 3);
        return decoder.DecodeByteText();
    }

    private static void AssertDecodeFailure(byte[] bytes, string encodingName, EncodingPolicyKind policy)
    {
        Assert.That(() => DecodeMemory(bytes, encodingName, policy), Throws.TypeOf<DecoderFallbackException>());
    }

    private sealed class TrackingStream : MemoryStream
    {
        internal int MaximumRead;
        internal int ReadCalls;
        internal int DisposeCalls;

        internal TrackingStream(byte[] bytes, int maximumRead) : base(bytes)
        {
            MaximumRead = maximumRead;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls = ReadCalls + 1;
            int limited = count < MaximumRead ? count : MaximumRead;
            return base.Read(buffer, offset, limited);
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
