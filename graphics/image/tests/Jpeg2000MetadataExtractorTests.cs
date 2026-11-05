using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class Jpeg2000MetadataExtractorTests
{
    [Test]
    public void Parse_ExtractsImageHeaderEnumeratedColorAndResolution()
    {
        byte[] ihdr = CreateImageHeader(1920, 1080, 3, 7, 7, 0, 1);
        byte[] colr = CreateBox("colr", Join(new byte[] { 1, 0, 1 }, Unsigned32(16)));
        byte[] resc = CreateBox("resc", new byte[] { 0, 3, 0, 1, 0, 6, 0, 1, 2, 2 });
        byte[] jp2h = CreateBox("jp2h", Join(ihdr, colr, CreateBox("res ", resc)));

        ImageMetadataDocument document = Parse(Join(CreateSignatureBox(), jp2h), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(1920));
        Assert.That(document.Frames[0].Height, Is.EqualTo(1080));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Jpeg2000));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(Find(document, "Jpeg2000.ihdr.IntellectualProperty").Value.BooleanValue, Is.True);
        Assert.That(Find(document, "Jpeg2000.colr.EnumeratedColorSpace").DisplayValue, Is.EqualTo("sRGB"));
        Assert.That(Find(document, "Jpeg2000.resc.XResolution").Value.DoubleValue, Is.EqualTo(600));
        Assert.That(Find(document, "Jpeg2000.resc.YResolution").Value.DoubleValue, Is.EqualTo(300));
        Assert.That(document.Summary.Width, Is.EqualTo(1920));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Summary.ResolutionText, Is.EqualTo("600 x 300 pixels/m"));
    }

    [Test]
    public void Parse_ExtractsIccProfileAndRawColorBox()
    {
        byte[] icc = IccProfileParserTests.CreateProfile(false);
        byte[] colr = CreateBox("colr", Join(new byte[] { 2, 0, 0 }, icc));

        ImageMetadataDocument document = Parse(colr, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Icc.JPEG2000.colr"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(icc));
        Assert.That(document.Profiles[0].Offset, Is.EqualTo(11));
        Assert.That(Find(document, "Jpeg2000.colr.IccProfileByteLength").Value.IntegerValue, Is.EqualTo(icc.Length));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("colr"));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Summary.ColorProfileName, Is.EqualTo("Studio Display"));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
    }

    [Test]
    public void Parse_ExtractsXmlAndKnownUuidMetadataProfiles()
    {
        byte[] xmp = Encoding.UTF8.GetBytes("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"urn:rdf\"/></x:xmpmeta>");
        byte[] tiff = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };
        byte[] xml = CreateBox("xml ", Encoding.UTF8.GetBytes("<gml>map</gml>"));
        byte[] boxes = Join(
            xml,
            CreateBox("jp2i", new byte[] { 1, 2, 3 }),
            CreateUuidBox(Hex(Jpeg2000MetadataExtractor.XmpUuidHex), xmp),
            CreateUuidBox(Encoding.ASCII.GetBytes(Jpeg2000MetadataExtractor.ExifUuidText), tiff),
            CreateUuidBox(Hex(Jpeg2000MetadataExtractor.IptcUuidHex), new byte[] { 28, 2, 5, 0, 3, 74, 80, 50 }),
            CreateUuidBox(Hex(Jpeg2000MetadataExtractor.GeoJp2UuidHex), tiff),
            CreateUuidBox(Hex(Jpeg2000MetadataExtractor.PhotoshopUuidHex), new byte[] { 56, 66, 73, 77 }));

        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;
        ImageMetadataDocument document = Parse(boxes, options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(5));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Xmp && profile.Text.Contains("xmpmeta")));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Exif.JPEG2000.uuid" && profile.Data.SequenceEqual(tiff)));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Iptc));
        Assert.That(Find(document, "Iptc.Application2.ObjectName").Value.Text, Is.EqualTo("JP2"));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Exif.JPEG2000.GeoJP2"));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.PhotoshopImageResource));
        Assert.That(Find(document, "Jpeg2000.XML.Text").Value.Text, Is.EqualTo("<gml>map</gml>"));
        Assert.That(Find(document, "Jpeg2000.jp2i.PayloadByteLength").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(7));
        Assert.That(document.RawBlocks, Has.Some.Matches<MetadataRawBlock>(block => block.Name == "JPEG 2000 UUID XMP"));
        Assert.That(document.RawBlocks, Has.All.Matches<MetadataRawBlock>(block => block.Data.Length == block.Length));
    }

    [Test]
    public void Parse_RespectsProfileRawAndXmpOptions()
    {
        byte[] xmp = Encoding.UTF8.GetBytes("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/>");
        byte[] icc = new byte[] { 1, 2, 3, 4 };
        byte[] boxes = Join(
            CreateBox("xml ", xmp),
            CreateUuidBox(Hex(Jpeg2000MetadataExtractor.XmpUuidHex), xmp),
            CreateBox("colr", Join(new byte[] { 2, 0, 0 }, icc)));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseXmp = false;
        options.ParseIcc = false;
        options.CaptureRawBlocks = false;

        ImageMetadataDocument document = Parse(boxes, options);

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(Find(document, "Jpeg2000.XML.Text").Value.Text, Does.Contain("xmpmeta"));
        Assert.That(Find(document, "Jpeg2000.colr.IccProfileByteLength").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(document.Summary.HasColorProfile, Is.True);
    }

    [Test]
    public void Parse_EnforcesXmlProfileRawAndFrameLimits()
    {
        byte[] xml = Encoding.UTF8.GetBytes("<metadata>too long</metadata>");
        byte[] icc = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] boxes = Join(
            CreateImageHeader(10, 10, 3, 7, 7, 0, 0),
            CreateImageHeader(20, 20, 3, 7, 7, 0, 0),
            CreateBox("xml ", xml),
            CreateBox("colr", Join(new byte[] { 2, 0, 0 }, icc)));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFrameCount = 1;
        options.MaxXmlBytes = 8;
        options.MaxProfileBytes = 4;
        options.CaptureRawBlockData = true;
        options.MaxRawBlockBytes = 10;

        ImageMetadataDocument document = Parse(boxes, options);

        AssertDiagnostic(document, "jp2.frame.count.limit", MetadataSeverity.LimitExceeded);
        AssertDiagnostic(document, "jp2.xml.limit", MetadataSeverity.LimitExceeded);
        AssertDiagnostic(document, "jp2.icc.profile.limit", MetadataSeverity.LimitExceeded);
        AssertDiagnostic(document, "jp2.raw.block.limit", MetadataSeverity.LimitExceeded);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Jpeg2000.XML.Text"), Is.Null);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(2));
        Assert.That(document.RawBlocks, Has.All.Matches<MetadataRawBlock>(block => block.Data.Length == 0));
    }

    [Test]
    public void Parse_ReportsTruncatedAndInvalidSemanticPayloads()
    {
        byte[] boxes = Join(
            CreateBox("ihdr", new byte[13]),
            CreateBox("colr", new byte[] { 1, 0, 0 }),
            CreateBox("resc", new byte[9]),
            CreateBox("resd", new byte[] { 0, 1, 0, 0, 0, 1, 0, 1, 0, 0 }));

        ImageMetadataDocument document = Parse(boxes, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "jp2.ihdr.truncated", MetadataSeverity.Truncated);
        AssertDiagnostic(document, "jp2.colr.enumerated.truncated", MetadataSeverity.Truncated);
        AssertDiagnostic(document, "jp2.resc.truncated", MetadataSeverity.Truncated);
        AssertDiagnostic(document, "jp2.resolution.denominator.invalid", MetadataSeverity.RecoverableError);
        Assert.That(document.Frames, Is.Empty);
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageMetadataReaderOptions options)
    {
        MemoryStream stream = new MemoryStream(bytes);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return Jpeg2000MetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg2000), options);
    }

    public static MetadataTag Find(ImageMetadataDocument document, string key)
    {
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, key);
        Assert.That(tag, Is.Not.Null, key);
        return tag;
    }

    public static byte[] CreateSignatureBox()
    {
        return new byte[] { 0, 0, 0, 12, 106, 80, 32, 32, 13, 10, 135, 10 };
    }

    public static byte[] CreateImageHeader(uint width, uint height, int components, int depth, int compression, int unknownColor, int intellectualProperty)
    {
        List<byte> data = new List<byte>();
        AddUnsigned32(data, height);
        AddUnsigned32(data, width);
        data.Add((byte)(components >> 8));
        data.Add((byte)components);
        data.Add((byte)depth);
        data.Add((byte)compression);
        data.Add((byte)unknownColor);
        data.Add((byte)intellectualProperty);
        return CreateBox("ihdr", data.ToArray());
    }

    public static byte[] CreateUuidBox(byte[] uuid, byte[] payload)
    {
        return CreateBox("uuid", Join(uuid, payload));
    }

    public static byte[] CreateBox(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)(payload.Length + 8));
        AddAscii(bytes, type);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] Unsigned32(uint value)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, value);
        return bytes.ToArray();
    }

    public static byte[] Hex(string text)
    {
        byte[] bytes = new byte[text.Length / 2];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = System.Convert.ToByte(text.Substring(index * 2, 2), 16);
            index++;
        }

        return bytes;
    }

    public static byte[] Join(params byte[][] arrays)
    {
        List<byte> bytes = new List<byte>();
        foreach (byte[] array in arrays)
        {
            AddBytes(bytes, array);
        }

        return bytes.ToArray();
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddAscii(List<byte> bytes, string text)
    {
        foreach (char value in text)
        {
            bytes.Add((byte)value);
        }
    }

    public static void AddBytes(List<byte> bytes, byte[] values)
    {
        foreach (byte value in values)
        {
            bytes.Add(value);
        }
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }
}
