using raw;
using System.Text;

namespace raw.Tests;

public class IptcCodecTests
{
    const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    const string Dc = "http://purl.org/dc/elements/1.1/";
    const string Core = "http://iptc.org/std/Iptc4xmpCore/1.0/xmlns/";
    const string Extension = "http://iptc.org/std/Iptc4xmpExt/2008-02-29/";
    const string Photoshop = "http://ns.adobe.com/photoshop/1.0/";

    [Test]
    public void IimRoundTripPreservesTypedUtf8DateTimeAndRepeatableValues()
    {
        RawIptcDataset charset = RawIptcDataset.CreateBinary(1, 90, new byte[] { 0x1B, 0x25, 0x47 });
        RawIptcDataset version = RawIptcDataset.CreateUnsignedInteger(2, 0, 4);
        RawIptcDataset firstKeyword = RawIptcDataset.CreateText(2, 25, "café", true);
        RawIptcDataset secondKeyword = RawIptcDataset.CreateText(2, 25, "東京", true);
        RawIptcDataset date = RawIptcDataset.CreateDate(2, 55, new DateTime(2026, 8, 19));
        RawIptcDataset time = RawIptcDataset.CreateTime(2, 60, 15, 27, 4, true, -420);
        RawIptcDataset previewFormat = RawIptcDataset.CreateUnsignedInteger(2, 200, 11);
        RawIptcRecord created = RawIptcRecord.Create(new RawIptcDataset[] { charset, version, firstKeyword, secondKeyword, date, time, previewFormat }, new RawLimits());
        byte[] encoded = created.Encode(new RawLimits());
        RawIptcRecord decoded = RawIptcRecord.Parse(encoded, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Utf8, Is.True);
            Assert.That(decoded.CopyBytes(), Is.EqualTo(encoded));
            Assert.That((decoded.GetDataset(2, 0) ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedValue, Is.EqualTo(4));
            Assert.That((decoded.GetDataset(2, 0) ?? throw new System.InvalidOperationException("A required value is unavailable.")).ValueKind, Is.EqualTo(RawIptcValueKind.UnsignedInteger));
            Assert.That(decoded.GetDatasets(2, 25).Select(value => value.Text), Is.EqualTo(new string[] { "café", "東京" }));
            Assert.That((decoded.GetDataset(2, 25) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Encoding, Is.EqualTo(RawTextEncoding.Utf8));
            Assert.That((decoded.GetDataset(2, 55) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Name, Is.EqualTo("DateCreated"));
            Assert.That((decoded.GetDataset(2, 55) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Year, Is.EqualTo(2026));
            Assert.That((decoded.GetDataset(2, 55) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Month, Is.EqualTo(8));
            Assert.That((decoded.GetDataset(2, 55) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Day, Is.EqualTo(19));
            Assert.That((decoded.GetDataset(2, 60) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Hour, Is.EqualTo(15));
            Assert.That((decoded.GetDataset(2, 60) ?? throw new System.InvalidOperationException("A required value is unavailable.")).HasUtcOffset, Is.True);
            Assert.That((decoded.GetDataset(2, 60) ?? throw new System.InvalidOperationException("A required value is unavailable.")).UtcOffsetMinutes, Is.EqualTo(-420));
            Assert.That((decoded.GetDataset(2, 200) ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedValue, Is.EqualTo(11));
            Assert.That(decoded.ToTagValue(new RawLimits()).CopyBytes(), Is.EqualTo(encoded));
        });
    }

    [Test]
    public void ExtendedLengthAndResourceLimitsAreEnforced()
    {
        byte[] binary = Enumerable.Range(0, 40000).Select(index => (byte)index).ToArray();
        RawIptcRecord record = RawIptcRecord.Create(new RawIptcDataset[] { RawIptcDataset.CreateBinary(8, 10, binary) }, new RawLimits());
        byte[] encoded = record.Encode(new RawLimits());
        RawLimits valueLimits = new RawLimits();
        valueLimits.MaxTagValueBytes = 39999;
        RawLimits metadataLimits = new RawLimits();
        metadataLimits.MaxMetadataBytes = 100;
        metadataLimits.MaxTagValueBytes = 100;
        byte[] truncated = encoded.Take(encoded.Length - 1).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(encoded[3] & 0x80, Is.EqualTo(0x80));
            Assert.That((RawIptcRecord.Parse(encoded, new RawLimits()).GetDataset(8, 10) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo(binary));
            Assert.Throws<RawLimitException>(() => RawIptcRecord.Parse(encoded, valueLimits));
            Assert.Throws<RawLimitException>(() => RawIptcRecord.Parse(encoded, metadataLimits));
            Assert.Throws<RawFormatException>(() => RawIptcRecord.Parse(truncated, new RawLimits()));
            Assert.Throws<RawFormatException>(() => RawIptcRecord.Parse(new byte[] { 0x1C, 2, 25, 0, 2, (byte)'x' }, new RawLimits()));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void MetadataOpenDiscoversDirectTiffIptcAndMapsPayload(RawByteOrder byteOrder)
    {
        byte[] iim = CreateSimpleIim("Direct headline");
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, false);
        SyntheticTiffIfd root = builder.AddIfd();
        builder.AddField(root, 33723, TiffFieldType.Undefined, (ulong)iim.Length, iim);
        byte[] tiff = builder.Build();

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(tiff), null, new RawOpenOptions());
        RawIptcRecord record = document.Metadata.IptcRecords.Single();
        RawPayloadBlock payload = document.PayloadMap.Find(RawPayloadKind.Iptc).Single();

        Assert.Multiple(() =>
        {
            Assert.That(record.IfdPath, Is.EqualTo("IFD0"));
            Assert.That((record.GetDataset(2, 105) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Text, Is.EqualTo("Direct headline"));
            Assert.That(document.ExtractPayload(payload), Is.EqualTo(iim));
            Assert.That(record.Offset, Is.EqualTo(payload.Offset));
            Assert.That((document.Metadata.FindTag("RichTIFFIPTC") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes(), Is.EqualTo(iim));
        });
    }

    [Test]
    public void MetadataOpenDiscoversIptcInsidePhotoshopImageResource()
    {
        byte[] iim = CreateSimpleIim("Resource headline");
        byte[] resource = CreatePhotoshopResource(0x0404, iim);
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        builder.AddField(root, 34377, TiffFieldType.Undefined, (ulong)resource.Length, resource);
        byte[] tiff = builder.Build();

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(tiff), null, new RawOpenOptions());
        RawIptcRecord record = document.Metadata.IptcRecords.Single();
        RawPayloadBlock payload = document.PayloadMap.Find(RawPayloadKind.Iptc).Single();

        Assert.Multiple(() =>
        {
            Assert.That((record.GetDataset(2, 105) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Text, Is.EqualTo("Resource headline"));
            Assert.That(document.ExtractPayload(payload), Is.EqualTo(iim));
            Assert.That(payload.Offset, Is.EqualTo(record.Offset));
            Assert.That(document.Metadata.FindTag("PhotoshopImageResources"), Is.Not.Null);
        });
    }

    [Test]
    public void XmpFieldsExposeIptcCoreAndExtensionViews()
    {
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about='' xmlns:dc='" + Dc + "' xmlns:iptcCore='" + Core + "' xmlns:iptcExt='" + Extension + "' xmlns:photoshop='" + Photoshop + "' iptcCore:CountryCode='USA' iptcExt:DigitalSourceType='http://cv.iptc.org/newscodes/digitalsourcetype/digitalCapture' photoshop:Headline='Core headline'><dc:title><rdf:Alt><rdf:li xml:lang='x-default'>Core title</rdf:li><rdf:li xml:lang='fr-FR'>Titre</rdf:li></rdf:Alt></dc:title></rdf:Description></rdf:RDF></x:xmpmeta>";
        byte[] xmp = Encoding.UTF8.GetBytes(xml);
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        builder.AddField(root, 700, TiffFieldType.Byte, (ulong)xmp.Length, xmp);
        byte[] tiff = builder.Build();

        RawMetadata metadata = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(tiff), null, new RawOpenOptions()).Metadata;

        Assert.Multiple(() =>
        {
            Assert.That((metadata.GetIptcCoreField("CountryCode", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("USA"));
            Assert.That((metadata.GetIptcCoreField("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).NamespaceUri, Is.EqualTo(Photoshop));
            Assert.That((metadata.GetIptcCoreField("title", "fr-FR") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("Titre"));
            Assert.That((metadata.GetIptcExtensionField("DigitalSourceType", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Schema, Is.EqualTo(RawIptcSchema.Extension));
            Assert.That(metadata.IptcCoreFields.All(field => field.Schema == RawIptcSchema.Core), Is.True);
            Assert.That(metadata.IptcExtensionFields.All(field => field.Schema == RawIptcSchema.Extension), Is.True);
        });
    }

    static byte[] CreateSimpleIim(string headline)
    {
        return RawIptcRecord.Create(new RawIptcDataset[]
        {
            RawIptcDataset.CreateUnsignedInteger(2, 0, 4),
            RawIptcDataset.CreateText(2, 105, headline, false)
        }, new RawLimits()).Encode(new RawLimits());
    }

    static byte[] CreatePhotoshopResource(ushort resourceId, byte[] data)
    {
        int paddedLength = data.Length + (data.Length & 1);
        byte[] result = new byte[12 + paddedLength];
        result[0] = (byte)'8';
        result[1] = (byte)'B';
        result[2] = (byte)'I';
        result[3] = (byte)'M';
        result[4] = (byte)(resourceId >> 8);
        result[5] = (byte)resourceId;
        result[6] = 0;
        result[7] = 0;
        result[8] = (byte)(data.Length >> 24);
        result[9] = (byte)(data.Length >> 16);
        result[10] = (byte)(data.Length >> 8);
        result[11] = (byte)data.Length;
        data.CopyTo(result, 12);
        return result;
    }
}
