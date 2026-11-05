using raw;
using System.Text;
using System.Xml;

namespace raw.Tests;

public class XmpCodecTests
{
    const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    const string Dc = "http://purl.org/dc/elements/1.1/";
    const string Xmp = "http://ns.adobe.com/xap/1.0/";

    [Test]
    public void WrappedPacketRetainsBytesPaddingAndExtractsRdfForms()
    {
        string xml = CreateXml("Original title", "Outil", true);
        string packetText = "<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n" + xml + "\n   \n    <?xpacket end=\"w\"?>";
        byte[] bytes = new UTF8Encoding(false).GetBytes(packetText);

        RawXmpPacket packet = RawXmpPacket.Parse(bytes, new RawLimits());
        byte[] copy = packet.CopyBytes();
        copy[0] ^= 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(packet.Encoding, Is.EqualTo(RawXmpEncoding.Utf8));
            Assert.That(packet.HasByteOrderMark, Is.False);
            Assert.That(packet.XmlText, Is.EqualTo(xml));
            Assert.That((packet.PrefixBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, Is.GreaterThan(0));
            Assert.That(Encoding.UTF8.GetString((packet.PaddingBytes ?? throw new System.InvalidOperationException("A required value is unavailable."))), Is.EqualTo("\n   \n    "));
            Assert.That(Encoding.UTF8.GetString((packet.SuffixBytes ?? throw new System.InvalidOperationException("A required value is unavailable."))), Is.EqualTo("<?xpacket end=\"w\"?>"));
            Assert.That(packet.ReadOnly, Is.False);
            Assert.That((packet.GetField(Xmp, "CreatorTool", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("Outil"));
            Assert.That((packet.GetField(Xmp, "Rating", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("5"));
            Assert.That((packet.GetField(Dc, "title", "x-default") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("Original title"));
            Assert.That((packet.GetField(Dc, "title", "fr-FR") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("Titre"));
            Assert.That(packet.GetFields(Dc, "subject").Select(field => field.Value), Is.EqualTo(new string[] { "raw", "photo" }));
            Assert.That((packet.GetField("http://ns.adobe.com/xap/1.0/mm/", "DocumentID", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("xmp.did:123"));
            Assert.That(packet.RawBytes, Is.EqualTo(bytes));
            Assert.That(packet.RawBytes, Is.Not.EqualTo(copy));
            Assert.That(packet.ToTagValue().GetByteValues(), Is.EqualTo(bytes));
        });
    }

    [TestCase(RawXmpEncoding.Utf8, false)]
    [TestCase(RawXmpEncoding.Utf8, true)]
    [TestCase(RawXmpEncoding.Utf16BigEndian, false)]
    [TestCase(RawXmpEncoding.Utf16LittleEndian, true)]
    [TestCase(RawXmpEncoding.Utf32BigEndian, true)]
    [TestCase(RawXmpEncoding.Utf32LittleEndian, false)]
    public void PacketEncodingDetectionSupportsUtfFamilies(RawXmpEncoding encoding, bool bom)
    {
        string xml = CreateXml("東京", "Éditeur", false);
        byte[] bytes = Encode(xml, encoding, bom);

        RawXmpPacket packet = RawXmpPacket.Parse(bytes, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(packet.Encoding, Is.EqualTo(encoding));
            Assert.That(packet.HasByteOrderMark, Is.EqualTo(bom));
            Assert.That((packet.GetField(Dc, "title", "x-default") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("東京"));
            Assert.That((packet.GetField(Xmp, "CreatorTool", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("Éditeur"));
            Assert.That(packet.XmlBytes, Is.EqualTo(bytes.Skip(PreambleLength(encoding, bom)).ToArray()));
        });
    }

    [Test]
    public void ReplacementPreservesEncodingWrapperPaddingAndOptionalFixedLength()
    {
        string originalXml = CreateXml("A much longer original title", "Camera", false);
        string prefix = "<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>\n";
        string padding = new string(' ', 300);
        string suffix = "<?xpacket end='r'?>";
        byte[] originalBytes = Encode(prefix + originalXml + padding + suffix, RawXmpEncoding.Utf16LittleEndian, true);
        RawXmpPacket packet = RawXmpPacket.Parse(originalBytes, new RawLimits());
        string replacementXml = CreateXml("New", "Editor", false);

        RawXmpPacket variable = packet.ReplaceXml(replacementXml, new RawLimits());
        RawXmpPacket fixedPacket = packet.ReplaceXmlFixedSize(replacementXml, new RawLimits());
        string oversized = CreateXml(new string('x', 1000), "Editor", false);

        Assert.Multiple(() =>
        {
            Assert.That(variable.Encoding, Is.EqualTo(RawXmpEncoding.Utf16LittleEndian));
            Assert.That(variable.HasByteOrderMark, Is.True);
            Assert.That(variable.PrefixBytes, Is.EqualTo(packet.PrefixBytes));
            Assert.That(variable.PaddingBytes, Is.EqualTo(packet.PaddingBytes));
            Assert.That(variable.SuffixBytes, Is.EqualTo(packet.SuffixBytes));
            Assert.That(variable.ReadOnly, Is.True);
            Assert.That((variable.GetField(Dc, "title", "x-default") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("New"));
            Assert.That((fixedPacket.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, Is.EqualTo((packet.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length));
            Assert.That((fixedPacket.PaddingBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, Is.GreaterThan((packet.PaddingBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length));
            Assert.That((fixedPacket.GetField(Xmp, "CreatorTool", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("Editor"));
            Assert.Throws<RawFormatException>(() => packet.ReplaceXmlFixedSize(oversized, new RawLimits()));
            Assert.Throws<RawFormatException>(() => packet.ReplaceXml("<?xml version='1.0'?>" + replacementXml, new RawLimits()));
        });
    }

    [Test]
    public void SafeReaderRejectsDtdMalformedXmlDepthAndSizeLimit()
    {
        string dtd = "<!DOCTYPE x:xmpmeta [<!ENTITY leak SYSTEM 'file:///etc/passwd'>]>" + CreateXml("&leak;", "Tool", false);
        string malformed = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'></x:xmpmeta>";
        string deep = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description><a:a xmlns:a='urn:a'><a:b><a:c>v</a:c></a:b></a:a></rdf:Description></rdf:RDF></x:xmpmeta>";
        RawLimits depthLimits = new RawLimits();
        depthLimits.MaxRecursionDepth = 3;
        RawLimits sizeLimits = new RawLimits();
        sizeLimits.MaxXmpBytes = 32;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawXmpPacket.Parse(Encoding.UTF8.GetBytes(dtd), new RawLimits()));
            Assert.Throws<XmlException>(() => RawXmpPacket.Parse(Encoding.UTF8.GetBytes(malformed), new RawLimits()));
            Assert.Throws<RawLimitException>(() => RawXmpPacket.Parse(Encoding.UTF8.GetBytes(deep), depthLimits));
            Assert.Throws<RawLimitException>(() => RawXmpPacket.Parse(Encoding.UTF8.GetBytes(CreateXml("title", "tool", false)), sizeLimits));
        });
    }

    [Test]
    public void DecodedTextAndDepthTrackingHonorTheSingleAllocationLimit()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateXml("bounded", "reader", false));
        RawLimits textLimits = new RawLimits();
        textLimits.MaxAllocationBytes = bytes.Length;
        RawLimits depthLimits = new RawLimits();
        depthLimits.MaxAllocationBytes = bytes.Length * 2;
        depthLimits.MaxRecursionDepth = bytes.Length;

        RawLimitException? text = Assert.Throws<RawLimitException>(() => RawXmpPacket.Parse(bytes, textLimits));
        RawLimitException? depth = Assert.Throws<RawLimitException>(() => RawXmpPacket.Parse(bytes, depthLimits));

        Assert.Multiple(() =>
        {
            Assert.That((text ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((depth ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void MetadataOpenDiscoversTiffXmpAndMapsExactPayload(RawByteOrder byteOrder)
    {
        string xml = CreateXml("TIFF title", "Raw camera", false);
        string packetText = "<?xpacket begin='' id='W5M0MpCehiHzreSzNTczkc9d'?>" + xml + new string(' ', 48) + "<?xpacket end='w'?>";
        byte[] packetBytes = Encoding.UTF8.GetBytes(packetText);
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, false);
        SyntheticTiffIfd root = builder.AddIfd();
        builder.AddField(root, 700, TiffFieldType.Byte, (ulong)packetBytes.Length, packetBytes);
        byte[] tiff = builder.Build();
        RawOpenOptions options = new RawOpenOptions();

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(tiff), null, options);
        RawXmpPacket packet = document.Metadata.XmpPackets.Single();
        RawPayloadBlock payload = document.PayloadMap.Find(RawPayloadKind.Xmp).Single();

        Assert.Multiple(() =>
        {
            Assert.That(packet.IfdPath, Is.EqualTo("IFD0"));
            Assert.That(packet.Offset, Is.EqualTo(payload.Offset));
            Assert.That((packet.GetField(Dc, "title", "x-default") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("TIFF title"));
            Assert.That(document.ExtractPayload(payload), Is.EqualTo(packetBytes));
            Assert.That((document.Metadata.FindTag("XMP") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes(), Is.EqualTo(packetBytes));
        });

        RawDocument previewOnly = RawCodec.OpenPreviews(new ReadOnlyMemory<byte>(tiff), null, options);
        Assert.Multiple(() =>
        {
            Assert.That(previewOnly.Metadata.XmpPackets, Is.Empty);
            Assert.That(previewOnly.PayloadMap.Find(RawPayloadKind.Xmp), Is.Empty);
        });
    }

    static string CreateXml(string title, string tool, bool includeCollections)
    {
        string collections = includeCollections
            ? "<dc:title><rdf:Alt><rdf:li xml:lang='x-default'>" + title + "</rdf:li><rdf:li xml:lang='fr-FR'>Titre</rdf:li></rdf:Alt></dc:title><dc:subject><rdf:Bag><rdf:li>raw</rdf:li><rdf:li>photo</rdf:li></rdf:Bag></dc:subject><xmpMM:DocumentID rdf:resource='xmp.did:123'/>"
            : "<dc:title><rdf:Alt><rdf:li xml:lang='x-default'>" + title + "</rdf:li></rdf:Alt></dc:title>";
        return "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about='' xmlns:dc='" + Dc + "' xmlns:xmp='" + Xmp + "' xmlns:xmpMM='http://ns.adobe.com/xap/1.0/mm/' xmp:Rating='5'><xmp:CreatorTool>" + tool + "</xmp:CreatorTool>" + collections + "</rdf:Description></rdf:RDF></x:xmpmeta>";
    }

    static byte[] Encode(string text, RawXmpEncoding encoding, bool bom)
    {
        Encoding codec = EncodingFor(encoding, bom);
        byte[] content = codec.GetBytes(text);
        byte[] preamble = bom ? codec.GetPreamble() : Array.Empty<byte>();
        return preamble.Concat(content).ToArray();
    }

    static Encoding EncodingFor(RawXmpEncoding encoding, bool bom)
    {
        return encoding switch
        {
            RawXmpEncoding.Utf8 => new UTF8Encoding(bom),
            RawXmpEncoding.Utf16BigEndian => new UnicodeEncoding(true, bom),
            RawXmpEncoding.Utf16LittleEndian => new UnicodeEncoding(false, bom),
            RawXmpEncoding.Utf32BigEndian => new UTF32Encoding(true, bom),
            RawXmpEncoding.Utf32LittleEndian => new UTF32Encoding(false, bom),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
    }

    static int PreambleLength(RawXmpEncoding encoding, bool bom)
    {
        return bom ? EncodingFor(encoding, true).GetPreamble().Length : 0;
    }
}
