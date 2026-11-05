using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PhotoshopImageResourceParserTests
{
    [Test]
    public void Parse_ReadsSignaturesPascalNamesPaddingPayloadsAndOffsets()
    {
        byte[] first = BuildResource("8BIM", 1005, "A", new byte[] { 1, 2, 3 }, true);
        byte[] second = BuildResource("8B64", 2000, "", new byte[] { 4, 5 }, true);
        byte[] prefix = new byte[] { 9, 8, 7 };
        byte[] bytes = Join(prefix, first, second, new byte[] { 6 });
        ImageMetadataDocument document = new ImageMetadataDocument();

        List<PhotoshopImageResource> resources = Parse(document, bytes, prefix.Length, first.Length + second.Length, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(resources, Has.Count.EqualTo(2));
        Assert.That(resources[0].Signature, Is.EqualTo("8BIM"));
        Assert.That(resources[0].ResourceId, Is.EqualTo(1005));
        Assert.That(resources[0].Name, Is.EqualTo("A"));
        Assert.That(resources[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(resources[0].ResourceOffset, Is.EqualTo(3));
        Assert.That(resources[0].DataOffset, Is.EqualTo(15));
        Assert.That(resources[0].ResourceLength, Is.EqualTo(16));
        Assert.That(resources[0].DeclaredDataLength, Is.EqualTo(3));
        Assert.That(resources[0].NameStorageLength, Is.EqualTo(2));
        Assert.That(resources[0].Truncated, Is.False);
        Assert.That(resources[1].Signature, Is.EqualTo("8B64"));
        Assert.That(resources[1].ResourceId, Is.EqualTo(2000));
        Assert.That(resources[1].Name, Is.Empty);
        Assert.That(resources[1].Data, Is.EqualTo(new byte[] { 4, 5 }));
        Assert.That(resources[1].ResourceOffset, Is.EqualTo(19));
        Assert.That(resources[1].ResourceLength, Is.EqualTo(14));
    }

    [Test]
    public void AddDirectory_CreatesStandardResourceTagsForTopLevelAndChildUses()
    {
        byte[] bytes = BuildResource("8BIM", 1039, "Display", new byte[] { 1, 2 }, true);
        ImageMetadataDocument document = new ImageMetadataDocument();
        PhotoshopImageResource resource = Parse(document, bytes, 0, bytes.Length, new ImageMetadataReaderOptions()).Single();

        MetadataDirectory topLevel = PhotoshopImageResourceParser.AddDirectory(document, null, resource, "TIFF/Photoshop/1039");
        MetadataDirectory parent = document.AddDirectoryWithName(MetadataFamily.Psd, "Resources", "Resources");
        MetadataDirectory child = PhotoshopImageResourceParser.AddDirectory(document, parent, resource, "PSD/ImageResources/1039");

        Assert.That(topLevel.Name, Is.EqualTo("ICC Profile"));
        Assert.That(topLevel.Path, Is.EqualTo("TIFF/Photoshop/1039"));
        Assert.That(topLevel.Offset, Is.EqualTo(0));
        Assert.That(topLevel.Length, Is.EqualTo(bytes.Length));
        Assert.That(topLevel.Tags.Single(tag => tag.Key == "Photoshop.IRB.ResourceId").NumericId, Is.EqualTo(1039));
        Assert.That(topLevel.Tags.Single(tag => tag.Key == "Photoshop.IRB.ResourceId").DisplayValue, Is.EqualTo("ICC Profile"));
        Assert.That(topLevel.Tags.Single(tag => tag.Key == "Photoshop.IRB.Name").Value.Text, Is.EqualTo("Display"));
        Assert.That(topLevel.Tags.Single(tag => tag.Key == "Photoshop.IRB.DataByteLength").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(topLevel.Tags.Single(tag => tag.Key == "Photoshop.IRB.Truncated").Value.BooleanValue, Is.False);
        Assert.That(parent.Children, Has.Count.EqualTo(1));
        Assert.That(child.Path, Is.EqualTo("PSD/ImageResources/1039"));
    }

    [Test]
    public void Parse_ReportsMalformedHeaderSignatureNameDataAndPadding()
    {
        ImageMetadataDocument header = new ImageMetadataDocument();
        Parse(header, Encoding.ASCII.GetBytes("8BIM"), 0, 4, new ImageMetadataReaderOptions());
        AssertCode(header, "test.irb.header.truncated", MetadataSeverity.Truncated);

        byte[] invalidSignature = BuildResource("NOPE", 1005, "", new byte[0], true);
        ImageMetadataDocument signature = new ImageMetadataDocument();
        Parse(signature, invalidSignature, 0, invalidSignature.Length, new ImageMetadataReaderOptions());
        AssertCode(signature, "test.irb.signature.invalid", MetadataSeverity.RecoverableError);

        List<byte> invalidName = new List<byte>();
        invalidName.AddRange(Encoding.ASCII.GetBytes("8BIM"));
        invalidName.Add(3);
        invalidName.Add(237);
        invalidName.Add(8);
        invalidName.AddRange(new byte[5]);
        ImageMetadataDocument name = new ImageMetadataDocument();
        Parse(name, invalidName.ToArray(), 0, invalidName.Count, new ImageMetadataReaderOptions());
        AssertCode(name, "test.irb.name.truncated", MetadataSeverity.Truncated);

        byte[] full = BuildResource("8BIM", 1058, "", new byte[] { 1, 2, 3 }, true);
        byte[] dataTruncated = full.Take(full.Length - 2).ToArray();
        ImageMetadataDocument data = new ImageMetadataDocument();
        List<PhotoshopImageResource> dataResources = Parse(data, dataTruncated, 0, dataTruncated.Length, new ImageMetadataReaderOptions());
        AssertCode(data, "test.irb.data.truncated", MetadataSeverity.Truncated);
        Assert.That(dataResources, Has.Count.EqualTo(1));
        Assert.That(dataResources[0].Truncated, Is.True);
        Assert.That(dataResources[0].Data, Is.Empty);

        byte[] paddingTruncated = BuildResource("8BIM", 1058, "", new byte[] { 1, 2, 3 }, false);
        ImageMetadataDocument padding = new ImageMetadataDocument();
        List<PhotoshopImageResource> paddingResources = Parse(padding, paddingTruncated, 0, paddingTruncated.Length, new ImageMetadataReaderOptions());
        AssertCode(padding, "test.irb.padding.truncated", MetadataSeverity.Truncated);
        Assert.That(paddingResources.Single().Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(paddingResources.Single().Truncated, Is.True);
    }

    [Test]
    public void Parse_EnforcesSegmentAndResourceCountLimits()
    {
        byte[] first = BuildResource("8BIM", 1005, "", new byte[0], true);
        byte[] second = BuildResource("8BIM", 1028, "", new byte[0], true);
        byte[] bytes = Join(first, second);
        ImageMetadataReaderOptions countOptions = new ImageMetadataReaderOptions();
        countOptions.MaxTagCount = 1;
        ImageMetadataDocument countDocument = new ImageMetadataDocument();

        List<PhotoshopImageResource> resources = Parse(countDocument, bytes, 0, bytes.Length, countOptions);

        Assert.That(resources, Has.Count.EqualTo(1));
        Assert.That(resources[0].ResourceId, Is.EqualTo(1005));
        AssertCode(countDocument, "test.irb.count.limit", MetadataSeverity.LimitExceeded);

        ImageMetadataReaderOptions sizeOptions = new ImageMetadataReaderOptions();
        sizeOptions.MaxSegmentBytes = bytes.Length - 1;
        ImageMetadataDocument sizeDocument = new ImageMetadataDocument();
        List<PhotoshopImageResource> limited = Parse(sizeDocument, bytes, 0, bytes.Length, sizeOptions);

        Assert.That(limited, Is.Empty);
        AssertCode(sizeDocument, "test.irb.length.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void Parse_ReportsAndBoundsRangesOutsideAvailableData()
    {
        byte[] resource = BuildResource("8BIM", 1036, "", new byte[] { 1, 2 }, true);
        ImageMetadataDocument partial = new ImageMetadataDocument();

        List<PhotoshopImageResource> resources = Parse(partial, resource, 0, resource.Length + 5, new ImageMetadataReaderOptions());

        AssertCode(partial, "test.irb.range.truncated", MetadataSeverity.Truncated);
        Assert.That(resources, Has.Count.EqualTo(1));
        Assert.That(resources[0].Data, Is.EqualTo(new byte[] { 1, 2 }));

        ImageMetadataDocument outside = new ImageMetadataDocument();
        List<PhotoshopImageResource> none = Parse(outside, resource, resource.Length + 1, 2, new ImageMetadataReaderOptions());
        Assert.That(none, Is.Empty);
        AssertCode(outside, "test.irb.range.truncated", MetadataSeverity.Truncated);
    }

    internal static List<PhotoshopImageResource> Parse(ImageMetadataDocument document, byte[] bytes, long offset, long length, ImageMetadataReaderOptions options)
    {
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, document.Diagnostics);
        return PhotoshopImageResourceParser.Parse(document, reader, offset, length, options, "test.irb", "IRB");
    }

    public static byte[] BuildResource(string signature, int resourceId, string name, byte[] data, bool includeDataPadding)
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes(signature));
        bytes.Add((byte)(resourceId >> 8));
        bytes.Add((byte)resourceId);
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        bytes.Add((byte)nameBytes.Length);
        bytes.AddRange(nameBytes);
        if (((1 + nameBytes.Length) & 1) != 0)
        {
            bytes.Add(0);
        }

        bytes.Add((byte)(data.Length >> 24));
        bytes.Add((byte)(data.Length >> 16));
        bytes.Add((byte)(data.Length >> 8));
        bytes.Add((byte)data.Length);
        bytes.AddRange(data);
        if (includeDataPadding && (data.Length & 1) != 0)
        {
            bytes.Add(0);
        }

        return bytes.ToArray();
    }

    public static byte[] Join(params byte[][] arrays)
    {
        return arrays.SelectMany(array => array).ToArray();
    }

    public static void AssertCode(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }
}
