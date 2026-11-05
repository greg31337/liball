using raw;
using System.Text;

namespace raw.Tests;

public class PublicStandaloneMetadataTests
{
    private const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private const string Dc = "http://purl.org/dc/elements/1.1/";

    [Test]
    public void XmpOperationsPreserveEncodingWrapperPaddingAndUnknownFields()
    {
        IRawCodec codec = new RawCodecService();
        string originalXml = CreateXmpXml("Original long title", "opaque");
        string prefix = "<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>\n";
        string padding = new string(' ', 240);
        string suffix = "<?xpacket end='r'?>";
        UnicodeEncoding encoding = new UnicodeEncoding(false, true);
        byte[] packetBytes = encoding.GetPreamble().Concat(encoding.GetBytes(prefix + originalXml + padding + suffix)).ToArray();

        RawXmpPacket packet = codec.ParseXmpPacket(packetBytes, new RawLimits());
        byte[] encoded = codec.EncodeXmpPacketBytes(packet, new RawLimits());
        encoded[0] ^= 0xFF;
        string replacementXml = CreateXmpXml("New", "opaque");
        RawXmpPacket replaced = codec.ReplaceXmpPacketXml(packet, replacementXml, new RawLimits());
        RawXmpPacket fixedSize = codec.ReplaceXmpPacketXmlFixedSize(packet, replacementXml, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(packet.Encoding, Is.EqualTo(RawXmpEncoding.Utf16LittleEndian));
            Assert.That(packet.HasByteOrderMark, Is.True);
            Assert.That(codec.FindXmpField(packet, Dc, "title", "x-default")?.Value, Is.EqualTo("Original long title"));
            Assert.That(codec.FindXmpFields(packet, Dc, "subject").Select(field => field.Value), Is.EqualTo(new string[] { "known", "opaque" }));
            Assert.That(codec.FindXmpField(replaced, "urn:unknown", "Private", null)?.Value, Is.EqualTo("opaque"));
            Assert.That(replaced.PrefixBytes, Is.EqualTo(packet.PrefixBytes));
            Assert.That(replaced.PaddingBytes, Is.EqualTo(packet.PaddingBytes));
            Assert.That(replaced.SuffixBytes, Is.EqualTo(packet.SuffixBytes));
            Assert.That(replaced.Encoding, Is.EqualTo(packet.Encoding));
            Assert.That(fixedSize.RawBytes, Has.Length.EqualTo(packetBytes.Length));
            Assert.That(fixedSize.PaddingBytes, Has.Length.GreaterThan(packet.PaddingBytes?.Length ?? 0));
            Assert.That(codec.EncodeXmpPacketBytes(packet, new RawLimits()), Is.EqualTo(packetBytes));
            Assert.That(codec.CreateXmpTagValue(packet, new RawLimits()).RawBytes, Is.EqualTo(packetBytes));
            Assert.That(packet.RawBytes, Is.Not.EqualTo(encoded));
        });
    }

    [Test]
    public void XmpPublicParserRejectsUnsafeXmlAndHonorsBounds()
    {
        IRawCodec codec = new RawCodecService();
        byte[] unsafeXml = Encoding.UTF8.GetBytes("<!DOCTYPE x:xmpmeta [<!ENTITY leak SYSTEM 'file:///etc/passwd'>]>" + CreateXmpXml("&leak;", "value"));
        byte[] valid = Encoding.UTF8.GetBytes(CreateXmpXml("Title", "value"));
        RawLimits limits = new RawLimits();
        limits.MaxXmpBytes = valid.Length - 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => codec.ParseXmpPacket(unsafeXml, new RawLimits()));
            Assert.Throws<RawLimitException>(() => codec.ParseXmpPacket(valid, limits));
            Assert.Throws<RawFormatException>(() => codec.ReplaceXmpPacketXml(codec.ParseXmpPacket(valid, new RawLimits()), "<?xml version='1.0'?>" + CreateXmpXml("Title", "value"), new RawLimits()));
        });
    }

    [Test]
    public void IptcOperationsPreserveUnknownDatasetsEncodingOrderAndPadding()
    {
        IRawCodec codec = new RawCodecService();
        RawLimits limits = new RawLimits();
        RawIptcDataset charset = codec.CreateBinaryIptcDataset(1, 90, new byte[] { 0x1B, 0x25, 0x47 });
        RawIptcDataset version = codec.CreateUnsignedIptcDataset(2, 0, 4);
        RawIptcDataset firstKeyword = codec.CreateTextIptcDataset(2, 25, "café", true);
        RawIptcDataset secondKeyword = codec.CreateTextIptcDataset(2, 25, "東京", true);
        RawIptcDataset date = codec.CreateDateIptcDataset(2, 55, new DateTime(2026, 8, 26));
        RawIptcDataset time = codec.CreateTimeIptcDataset(2, 60, 9, 15, 30, true, -420);
        RawIptcDataset unknown = codec.CreateBinaryIptcDataset(8, 201, new byte[] { 9, 8, 7, 6 });
        RawIptcRecord created = codec.CreateIptcRecord(new RawIptcDataset[] { charset, version, firstKeyword, secondKeyword, date, time, unknown }, limits);
        byte[] canonical = codec.EncodeIptcRecordBytes(created, limits);
        byte[] padded = canonical.Concat(new byte[] { 0, 0, 0, 0 }).ToArray();
        RawIptcRecord parsed = codec.ParseIptcRecord(padded, limits);
        RawIptcDataset replacement = codec.CreateTextIptcDataset(2, 25, "édition", true);
        RawIptcRecord replaced = codec.ReplaceIptcDatasetAtIndex(parsed, 2, replacement, limits);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Utf8, Is.True);
            Assert.That(codec.CopyIptcRecordBytes(parsed), Is.EqualTo(padded));
            Assert.That(codec.FindIptcDatasets(parsed, 2, 25).Select(dataset => dataset.Text), Is.EqualTo(new string[] { "café", "東京" }));
            Assert.That(codec.FindIptcDataset(parsed, 2, 55)?.Year, Is.EqualTo(2026));
            Assert.That(codec.FindIptcDataset(parsed, 2, 60)?.UtcOffsetMinutes, Is.EqualTo(-420));
            Assert.That(codec.CopyIptcDatasetBytes(codec.FindIptcDataset(replaced, 8, 201) ?? throw new InvalidOperationException()), Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
            Assert.That(codec.FindIptcDatasets(replaced, 2, 25).Select(dataset => dataset.Text), Is.EqualTo(new string[] { "édition", "東京" }));
            Assert.That(replaced.PaddingBytes, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
            Assert.That(codec.CopyIptcRecordBytes(replaced).TakeLast(4), Is.All.Zero);
            Assert.That(codec.EncodeIptcRecordBytes(replaced, limits), Has.Length.EqualTo((replaced.RawBytes?.Length ?? 0) - 4));
            Assert.That(codec.CreateIptcTagValue(replaced, limits).Type, Is.EqualTo(RawTagType.Undefined));
            Assert.Throws<RawFormatException>(() => codec.ReplaceIptcDatasetAtIndex(parsed, 99, replacement, limits));
        });
    }

    [Test]
    public void IccOperationsPreserveUnknownBytesAndSupportFixedSizeTagReplacement()
    {
        IRawCodec codec = new RawCodecService();
        RawLimits limits = new RawLimits();
        byte[] originalBytes = IccCodecTests.CreateProfile();
        RawIccProfile profile = codec.ParseIccProfile(originalBytes, limits);
        RawIccTag description = codec.FindIccTag(profile, "desc") ?? throw new InvalidOperationException();
        RawIccTag redMatrix = codec.FindIccTag(profile, "rXYZ") ?? throw new InvalidOperationException();
        byte[] replacement = codec.CopyIccTagBytes(description);
        Encoding.BigEndianUnicode.GetBytes("Alternate RGB").CopyTo(replacement, 28);
        RawIccProfile replaced = codec.ReplaceIccTagDataFixedSize(profile, "desc", replacement, limits);
        byte[] encoded = codec.EncodeIccProfileBytes(profile, limits);
        encoded[0] ^= 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidateIccProfile(profile).IsValid(), Is.True);
            Assert.That(codec.IccProfileHasMatrixTrc(profile), Is.True);
            Assert.That(description.TypeSignature, Is.EqualTo("mluc"));
            Assert.That(codec.CopyIccTagBytes(redMatrix), Is.EqualTo(redMatrix.RawBytes));
            Assert.That(codec.EncodeIccProfileBytes(profile, limits), Is.EqualTo(originalBytes));
            Assert.That(profile.RawBytes, Is.Not.EqualTo(encoded));
            Assert.That(codec.CreateIccTagValue(profile, limits).Type, Is.EqualTo(RawTagType.Undefined));
            Assert.That(replaced.Description, Is.EqualTo("Alternate RGB"));
            Assert.That(replaced.ProfileIdMatches, Is.True);
            Assert.That(codec.CopyIccTagBytes(codec.FindIccTag(replaced, "rXYZ") ?? throw new InvalidOperationException()), Is.EqualTo(redMatrix.RawBytes));
            Assert.Throws<RawFormatException>(() => codec.ReplaceIccTagDataFixedSize(profile, "desc", replacement.Take(replacement.Length - 1).ToArray(), limits));
            Assert.Throws<RawFormatException>(() => codec.ReplaceIccTagDataFixedSize(profile, "none", replacement, limits));
        });
    }

    [Test]
    public void StandaloneIptcAndIccParsingEnforceConfiguredLimits()
    {
        IRawCodec codec = new RawCodecService();
        byte[] iptc = codec.EncodeIptcRecordBytes(codec.CreateIptcRecord(new RawIptcDataset[] { codec.CreateTextIptcDataset(2, 105, "Headline", false) }, new RawLimits()), new RawLimits());
        byte[] icc = IccCodecTests.CreateProfile();
        RawLimits iptcLimits = new RawLimits();
        iptcLimits.MaxMetadataBytes = iptc.Length - 1;
        RawLimits iccLimits = new RawLimits();
        iccLimits.MaxTagValueBytes = icc.Length - 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawLimitException>(() => codec.ParseIptcRecord(iptc, iptcLimits));
            Assert.Throws<RawLimitException>(() => codec.ParseIccProfile(icc, iccLimits));
        });
    }

    private static string CreateXmpXml(string title, string unknownValue)
    {
        return "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about='' xmlns:dc='" + Dc + "' xmlns:u='urn:unknown'><dc:title><rdf:Alt><rdf:li xml:lang='x-default'>" + title + "</rdf:li></rdf:Alt></dc:title><dc:subject><rdf:Bag><rdf:li>known</rdf:li><rdf:li>opaque</rdf:li></rdf:Bag></dc:subject><u:Private>" + unknownValue + "</u:Private></rdf:Description></rdf:RDF></x:xmpmeta>";
    }
}
