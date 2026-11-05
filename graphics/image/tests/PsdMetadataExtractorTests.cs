using System.Collections.Generic;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PsdMetadataExtractorTests
{
    [Test]
    public void Parse_ExtractsImageResourcesResolutionProfilesAndThumbnail()
    {
        byte[] exif = new byte[] { 73, 73, 42, 0 };
        byte[] iptc = new byte[] { 28, 2, 5, 0, 1, 65 };
        byte[] icc = new byte[] { 1, 2, 3, 4, 5 };
        byte[] xmp = Encoding.UTF8.GetBytes("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/>");
        byte[] jpeg = new byte[] { 255, 216, 255, 217 };
        byte[] resources = Join(
            BuildResource(1005, "dpi", BuildResolution(300, 1, 1, 72.5, 1, 1)),
            BuildResource(1058, "", exif),
            BuildResource(1060, "x", xmp),
            BuildResource(1028, "", iptc),
            BuildResource(1039, "icc", icc),
            BuildResource(1036, "preview", BuildThumbnail(64, 32, jpeg)));
        byte[] bytes = PsdMetadataParserTests.CreateDocument(1, 3, 100, 200, 8, 3, new byte[0], resources, new byte[0], 0, new byte[0]);

        ImageMetadataDocument document = PsdMetadataParserTests.Parse(bytes, ImageFormatKind.Psd, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ImageResources.ResourceCount").Value.IntegerValue, Is.EqualTo(6));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.LayerCount").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(new ImageMetadataQuery().Find(document, "Photoshop.IRB.ResourceId").Select(tag => tag.Value.IntegerValue), Is.EqualTo(new long[] { 1005, 1058, 1060, 1028, 1039, 1036 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Resolution.Horizontal").Value.DoubleValue, Is.EqualTo(300));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Resolution.Vertical").Value.DoubleValue, Is.EqualTo(72.5));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Resolution.HorizontalUnit").DisplayValue, Is.EqualTo("Pixels per inch"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Thumbnail.Width").Value.UnsignedIntegerValue, Is.EqualTo(64));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Thumbnail.Height").Value.UnsignedIntegerValue, Is.EqualTo(32));
        Assert.That(document.Profiles.Select(profile => profile.Kind), Is.EqualTo(new[]
        {
            MetadataProfileKind.PhotoshopImageResource,
            MetadataProfileKind.Exif,
            MetadataProfileKind.Xmp,
            MetadataProfileKind.Iptc,
            MetadataProfileKind.Icc,
            MetadataProfileKind.Thumbnail
        }));
        Assert.That(document.Profiles.Single(profile => profile.Kind == MetadataProfileKind.Exif).Data, Is.EqualTo(exif));
        Assert.That(document.Profiles.Single(profile => profile.Kind == MetadataProfileKind.Xmp).Text, Is.EqualTo(Encoding.UTF8.GetString(xmp)));
        Assert.That(document.Profiles.Single(profile => profile.Kind == MetadataProfileKind.Iptc).Data, Is.EqualTo(iptc));
        Assert.That(document.Profiles.Single(profile => profile.Kind == MetadataProfileKind.Icc).Data, Is.EqualTo(icc));
        Assert.That(document.Profiles.Single(profile => profile.Kind == MetadataProfileKind.Thumbnail).Data, Is.EqualTo(jpeg));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Iptc.Application2.ObjectName").Value.Text, Is.EqualTo("A"));
        Assert.That(document.Directories, Has.Count.EqualTo(6));
        Assert.That(document.Directories[2].Children, Has.Count.EqualTo(6));
    }

    [Test]
    public void Parse_HonorsXmpIccAndProfileLimitsWithoutLosingResourceMetadata()
    {
        byte[] resources = Join(
            BuildResource(1058, "", new byte[] { 1, 2, 3, 4 }),
            BuildResource(1060, "", Encoding.UTF8.GetBytes("<x/>")),
            BuildResource(1039, "", new byte[] { 5, 6, 7, 8 }));
        byte[] bytes = PsdMetadataParserTests.CreateDocument(1, 3, 10, 10, 8, 3, new byte[0], resources, new byte[0], 0, new byte[0]);
        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.ParseXmp = false;
        disabled.ParseIcc = false;

        ImageMetadataDocument disabledDocument = PsdMetadataParserTests.Parse(bytes, ImageFormatKind.Psd, disabled);

        Assert.That(disabledDocument.Diagnostics, Is.Empty);
        Assert.That(disabledDocument.Profiles.Select(profile => profile.Kind), Is.EqualTo(new[] { MetadataProfileKind.PhotoshopImageResource, MetadataProfileKind.Exif }));
        Assert.That(RequiredMetadataQuery.FindFirst(disabledDocument, "Psd.ImageResources.ResourceCount").Value.IntegerValue, Is.EqualTo(3));

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.MaxProfileBytes = 2;
        limited.MaxXmlBytes = 2;
        ImageMetadataDocument limitedDocument = PsdMetadataParserTests.Parse(bytes, ImageFormatKind.Psd, limited);

        Assert.That(limitedDocument.Profiles, Has.Count.EqualTo(4));
        Assert.That(limitedDocument.Profiles, Has.All.Matches<MetadataProfile>(profile => profile.Data.Length == 0));
        Assert.That(limitedDocument.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("psd.resources.profile.limit"));
        Assert.That(limitedDocument.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("psd.exif.profile.limit"));
        Assert.That(limitedDocument.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("psd.xmp.profile.limit"));
        Assert.That(limitedDocument.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("psd.icc.profile.limit"));
    }

    [Test]
    public void Parse_PreservesIndexedAndDuotoneColorModeDataAsRawBlocks()
    {
        byte[] palette = new byte[768];
        palette[0] = 10;
        palette[256] = 20;
        palette[512] = 30;
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;
        byte[] indexedBytes = PsdMetadataParserTests.CreateDocument(1, 1, 10, 10, 8, 2, palette, new byte[0], new byte[0], 0, new byte[0]);

        ImageMetadataDocument indexed = PsdMetadataParserTests.Parse(indexedBytes, ImageFormatKind.Psd, options);

        Assert.That(indexed.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(indexed, "Psd.ColorModeData.Kind").Value.Text, Is.EqualTo("Indexed color table"));
        Assert.That(RequiredMetadataQuery.FindFirst(indexed, "Psd.ColorModeData.PaletteEntryCount").Value.IntegerValue, Is.EqualTo(256));
        Assert.That(RequiredMetadataQuery.FindFirst(indexed, "Psd.ColorModeData.StandardPaletteLength").Value.BooleanValue, Is.True);
        Assert.That(indexed.RawBlocks.Single(block => block.BlockType == "ColorModeData").Data, Is.EqualTo(palette));

        byte[] duotoneData = new byte[] { 9, 8, 7 };
        byte[] duotoneBytes = PsdMetadataParserTests.CreateDocument(1, 1, 10, 10, 8, 8, duotoneData, new byte[0], new byte[0], 0, new byte[0]);
        ImageMetadataDocument duotone = PsdMetadataParserTests.Parse(duotoneBytes, ImageFormatKind.Psd, options);

        Assert.That(RequiredMetadataQuery.FindFirst(duotone, "Psd.ColorModeData.Kind").Value.Text, Is.EqualTo("Duotone specification"));
        Assert.That(RequiredMetadataQuery.FindFirst(duotone, "Psd.ColorModeData.DuotonePreserved").Value.BooleanValue, Is.True);
        Assert.That(duotone.RawBlocks.Single(block => block.BlockType == "ColorModeData").Data, Is.EqualTo(duotoneData));
    }

    [Test]
    public void Parse_ExtractsPsdLayerRecordSummaryAndMergedTransparency()
    {
        byte[] record = BuildLayerRecord(1, -5, 7, 25, 47, new ulong[] { 12 }, "mul ", 128, 1, 2, "Shadow");
        byte[] layers = BuildLayerSection(1, -1, new byte[][] { record });
        byte[] bytes = PsdMetadataParserTests.CreateDocument(1, 4, 100, 100, 8, 3, new byte[0], new byte[0], layers, 0, new byte[0]);

        ImageMetadataDocument document = PsdMetadataParserTests.Parse(bytes, ImageFormatKind.Psd, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.LayerCount").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.ParsedLayerCount").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.MergedTransparency").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.Name").Value.Text, Is.EqualTo("Shadow"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.Top").Value.IntegerValue, Is.EqualTo(-5));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.Width").Value.IntegerValue, Is.EqualTo(40));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.Height").Value.IntegerValue, Is.EqualTo(30));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.ChannelDataLength").Value.UnsignedIntegerValue, Is.EqualTo(12));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.BlendMode").DisplayValue, Is.EqualTo("Multiply"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.Opacity").Value.IntegerValue, Is.EqualTo(128));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.Clipping").DisplayValue, Is.EqualTo("Non-base"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.Visible").Value.BooleanValue, Is.False);
        Assert.That(document.Directories[3].Children, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_UsesPsbEightByteLayerAndChannelLengths()
    {
        ulong channelLength = 4294967297;
        byte[] record = BuildLayerRecord(2, 0, 0, 1, 2, new ulong[] { channelLength }, "norm", 255, 0, 0, "Large");
        byte[] layers = BuildLayerSection(2, 1, new byte[][] { record });
        byte[] bytes = PsdMetadataParserTests.CreateDocument(2, 3, 10, 10, 8, 3, new byte[0], new byte[0], layers, 0, new byte[0]);

        ImageMetadataDocument document = PsdMetadataParserTests.Parse(bytes, ImageFormatKind.Psb, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.LayerInfoLength").Value.UnsignedIntegerValue, Is.EqualTo((ulong)(layers.Length - 8)));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.ChannelDataLength").Value.UnsignedIntegerValue, Is.EqualTo(channelLength));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Layer.Name").Value.Text, Is.EqualTo("Large"));
    }

    [Test]
    public void Parse_ReportsTruncatedRecognizedResourceAndLayerMetadata()
    {
        byte[] resource = BuildResource(1058, "", new byte[] { 1, 2, 3 });
        byte[] truncatedResource = resource.Take(resource.Length - 2).ToArray();
        byte[] resourceBytes = PsdMetadataParserTests.CreateDocument(1, 3, 10, 10, 8, 3, new byte[0], truncatedResource, new byte[0], 0, new byte[0]);

        ImageMetadataDocument resourceDocument = PsdMetadataParserTests.Parse(resourceBytes, ImageFormatKind.Psd, new ImageMetadataReaderOptions());

        Assert.That(resourceDocument.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("psd.resource.data.truncated"));
        Assert.That(RequiredMetadataQuery.FindFirst(resourceDocument, "Psd.ImageResources.ResourceCount").Value.IntegerValue, Is.EqualTo(1));

        byte[] invalidLayerInfo = Join(Unsigned32(20), new byte[] { 0, 1 });
        byte[] layerBytes = PsdMetadataParserTests.CreateDocument(1, 3, 10, 10, 8, 3, new byte[0], new byte[0], invalidLayerInfo, 0, new byte[0]);
        ImageMetadataDocument layerDocument = PsdMetadataParserTests.Parse(layerBytes, ImageFormatKind.Psd, new ImageMetadataReaderOptions());

        Assert.That(layerDocument.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("psd.layer-info.truncated"));
    }

    public static byte[] BuildResource(int resourceId, string name, byte[] data)
    {
        List<byte> bytes = new List<byte>();
        AddAscii(bytes, "8BIM");
        AddUnsigned16(bytes, (uint)resourceId);
        bytes.Add((byte)name.Length);
        AddAscii(bytes, name);
        if (((1 + name.Length) & 1) != 0)
        {
            bytes.Add(0);
        }

        AddUnsigned32(bytes, (uint)data.Length);
        bytes.AddRange(data);
        if ((data.Length & 1) != 0)
        {
            bytes.Add(0);
        }

        return bytes.ToArray();
    }

    public static byte[] BuildResolution(double horizontal, int horizontalUnit, int widthUnit, double vertical, int verticalUnit, int heightUnit)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)(horizontal * 65536));
        AddUnsigned16(bytes, (uint)horizontalUnit);
        AddUnsigned16(bytes, (uint)widthUnit);
        AddUnsigned32(bytes, (uint)(vertical * 65536));
        AddUnsigned16(bytes, (uint)verticalUnit);
        AddUnsigned16(bytes, (uint)heightUnit);
        return bytes.ToArray();
    }

    public static byte[] BuildThumbnail(uint width, uint height, byte[] jpeg)
    {
        List<byte> bytes = new List<byte>();
        uint rowBytes = (width * 24 + 31) / 32 * 4;
        AddUnsigned32(bytes, 1);
        AddUnsigned32(bytes, width);
        AddUnsigned32(bytes, height);
        AddUnsigned32(bytes, rowBytes);
        AddUnsigned32(bytes, rowBytes * height);
        AddUnsigned32(bytes, (uint)jpeg.Length);
        AddUnsigned16(bytes, 24);
        AddUnsigned16(bytes, 1);
        bytes.AddRange(jpeg);
        return bytes.ToArray();
    }

    public static byte[] BuildLayerSection(int version, short count, byte[][] records)
    {
        List<byte> info = new List<byte>();
        AddUnsigned16(info, (ushort)count);
        foreach (byte[] record in records)
        {
            info.AddRange(record);
        }

        List<byte> bytes = new List<byte>();
        if (version == 2)
        {
            AddUnsigned64(bytes, (ulong)info.Count);
        }
        else
        {
            AddUnsigned32(bytes, (uint)info.Count);
        }

        bytes.AddRange(info);
        return bytes.ToArray();
    }

    public static byte[] BuildLayerRecord(int version, int top, int left, int bottom, int right, ulong[] channelLengths, string blendMode, int opacity, int clipping, int flags, string name)
    {
        List<byte> bytes = new List<byte>();
        AddSigned32(bytes, top);
        AddSigned32(bytes, left);
        AddSigned32(bytes, bottom);
        AddSigned32(bytes, right);
        AddUnsigned16(bytes, (uint)channelLengths.Length);
        for (int index = 0; index < channelLengths.Length; index++)
        {
            AddUnsigned16(bytes, (uint)index);
            if (version == 2)
            {
                AddUnsigned64(bytes, channelLengths[index]);
            }
            else
            {
                AddUnsigned32(bytes, (uint)channelLengths[index]);
            }
        }

        AddAscii(bytes, "8BIM");
        AddAscii(bytes, blendMode);
        bytes.Add((byte)opacity);
        bytes.Add((byte)clipping);
        bytes.Add((byte)flags);
        bytes.Add(0);
        List<byte> extra = new List<byte>();
        AddUnsigned32(extra, 0);
        AddUnsigned32(extra, 0);
        extra.Add((byte)name.Length);
        AddAscii(extra, name);
        while ((extra.Count & 3) != 0)
        {
            extra.Add(0);
        }

        AddUnsigned32(bytes, (uint)extra.Count);
        bytes.AddRange(extra);
        return bytes.ToArray();
    }

    public static byte[] Join(params byte[][] arrays)
    {
        return arrays.SelectMany(array => array).ToArray();
    }

    public static byte[] Unsigned32(uint value)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, value);
        return bytes.ToArray();
    }

    public static void AddSigned32(List<byte> bytes, int value)
    {
        AddUnsigned32(bytes, IntegerBitPattern.ToUnsigned32(value));
    }

    public static void AddUnsigned16(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddUnsigned64(List<byte> bytes, ulong value)
    {
        for (int shift = 56; shift >= 0; shift -= 8)
        {
            bytes.Add((byte)(value >> shift));
        }
    }

    public static void AddAscii(List<byte> bytes, string value)
    {
        bytes.AddRange(Encoding.ASCII.GetBytes(value));
    }
}
