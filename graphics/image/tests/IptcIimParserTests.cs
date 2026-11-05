using System.Collections.Generic;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class IptcIimParserTests
{
    [Test]
    public void Parse_DecodesCommonDatasetsRecordsUtf8TypesAndRepeatedValues()
    {
        List<byte> data = new List<byte>();
        AddStandard(data, 1, 0, new byte[] { 0, 4 });
        AddStandard(data, 1, 90, new byte[] { 27, 37, 71 });
        AddStandard(data, 2, 5, Encoding.UTF8.GetBytes("Café"));
        AddStandard(data, 2, 25, Encoding.UTF8.GetBytes("news"));
        AddStandard(data, 2, 25, Encoding.UTF8.GetBytes("photo"));
        AddStandard(data, 2, 55, Encoding.ASCII.GetBytes("20260812"));
        AddStandard(data, 2, 125, new byte[] { 1, 2, 3 });
        AddStandard(data, 2, 250, new byte[] { 9, 8 });
        ImageMetadataDocument document = Parse(data.ToArray(), 100, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories.Select(directory => directory.Group), Is.EqualTo(new[] { "Envelope", "Application2" }));
        Assert.That(document.Directories[0].Path, Is.EqualTo("TEST/IPTC/Envelope"));
        Assert.That(document.Directories[1].Path, Is.EqualTo("TEST/IPTC/Application2"));
        Assert.That(Find(document, "Iptc.Envelope.ModelVersion").Value.Kind, Is.EqualTo(MetadataValueKind.UnsignedInteger));
        Assert.That(Find(document, "Iptc.Envelope.ModelVersion").Value.UnsignedIntegerValue, Is.EqualTo(4));
        Assert.That(Find(document, "Iptc.Envelope.CodedCharacterSet").Value.Text, Is.EqualTo("UTF-8"));
        Assert.That(Find(document, "Iptc.Application2.ObjectName").Value.Text, Is.EqualTo("Café"));
        Assert.That(new ImageMetadataQuery().Find(document, "Iptc.Application2.Keywords").Select(tag => tag.Value.Text), Is.EqualTo(new[] { "news", "photo" }));
        Assert.That(Find(document, "Iptc.Application2.DateCreated").Value.Kind, Is.EqualTo(MetadataValueKind.DateText));
        Assert.That(Find(document, "Iptc.Application2.DateCreated").Value.Text, Is.EqualTo("20260812"));
        Assert.That(Find(document, "Iptc.Application2.RasterizedCaption").Value.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(Find(document, "Iptc.Application2.Dataset250").Value.Bytes, Is.EqualTo(new byte[] { 9, 8 }));
        Assert.That(Find(document, "Iptc.Application2.ObjectName").RawValue, Is.EqualTo(Encoding.UTF8.GetBytes("Café")));
        Assert.That(Find(document, "Iptc.Application2.ObjectName").Offset, Is.GreaterThan(100));
        Assert.That(Find(document, "Iptc.Application2.ObjectName").DisplayValue, Is.EqualTo("2:5"));
    }

    [Test]
    public void Parse_NormalizesContentInsteadOfDatasetDisplayIdentifiers()
    {
        List<byte> data = new List<byte>();
        AddStandard(data, 2, 5, Encoding.ASCII.GetBytes("News Photo"));
        AddStandard(data, 2, 25, Encoding.ASCII.GetBytes("travel"));
        AddStandard(data, 2, 25, Encoding.ASCII.GetBytes("coast"));
        AddStandard(data, 2, 65, Encoding.ASCII.GetBytes("Photo Desk"));
        AddStandard(data, 2, 70, Encoding.ASCII.GetBytes("4.2"));
        AddStandard(data, 2, 80, Encoding.ASCII.GetBytes("Ada"));
        AddStandard(data, 2, 116, Encoding.ASCII.GetBytes("Copyright 2026"));
        AddStandard(data, 2, 120, Encoding.ASCII.GetBytes("Evening light"));
        ImageMetadataDocument document = Parse(data.ToArray(), 0, new ImageMetadataReaderOptions());

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Author, Is.EqualTo("Ada"));
        Assert.That(summary.Copyright, Is.EqualTo("Copyright 2026"));
        Assert.That(summary.Title, Is.EqualTo("News Photo"));
        Assert.That(summary.Description, Is.EqualTo("Evening light"));
        Assert.That(summary.Keywords, Is.EqualTo(new[] { "travel", "coast" }));
        Assert.That(summary.Software, Is.EqualTo("Photo Desk 4.2"));
    }

    [Test]
    public void Parse_ReadsExtendedDatasetLength()
    {
        byte[] caption = Enumerable.Repeat((byte)65, 32768).ToArray();
        List<byte> data = new List<byte>();
        AddExtended(data, 2, 120, caption);
        ImageMetadataDocument document = Parse(data.ToArray(), 0, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        MetadataTag tag = Find(document, "Iptc.Application2.Caption");
        Assert.That(tag.Value.Text, Has.Length.EqualTo(32768));
        Assert.That(tag.Length, Is.EqualTo(32768));
        Assert.That(tag.RawValue, Is.EqualTo(caption));
    }

    [Test]
    public void Parse_UsesLatin1WhenUtf8WasNotDesignated()
    {
        List<byte> data = new List<byte>();
        AddStandard(data, 2, 80, new byte[] { 82, 101, 110, 233 });

        ImageMetadataDocument document = Parse(data.ToArray(), 0, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Iptc.Application2.Byline").Value.Text, Is.EqualTo("René"));
    }

    [Test]
    public void Parse_ReportsInvalidMarkerHeadersExtendedLengthsAndValues()
    {
        ImageMetadataDocument header = Parse(new byte[] { 28, 2, 5, 0 }, 20, new ImageMetadataReaderOptions());
        AssertCode(header, "test.iptc.dataset.truncated", MetadataSeverity.Truncated);

        ImageMetadataDocument marker = Parse(new byte[] { 0, 2, 5, 0, 0 }, 20, new ImageMetadataReaderOptions());
        AssertCode(marker, "test.iptc.marker.invalid", MetadataSeverity.RecoverableError);

        ImageMetadataDocument invalidExtended = Parse(new byte[] { 28, 2, 5, 128, 0 }, 20, new ImageMetadataReaderOptions());
        AssertCode(invalidExtended, "test.iptc.extended.length.invalid", MetadataSeverity.RecoverableError);

        ImageMetadataDocument truncatedExtended = Parse(new byte[] { 28, 2, 5, 128, 3, 0, 1 }, 20, new ImageMetadataReaderOptions());
        AssertCode(truncatedExtended, "test.iptc.extended.length.truncated", MetadataSeverity.Truncated);

        ImageMetadataDocument value = Parse(new byte[] { 28, 2, 5, 0, 4, 65 }, 20, new ImageMetadataReaderOptions());
        AssertCode(value, "test.iptc.dataset.value.truncated", MetadataSeverity.Truncated);
        Assert.That(value.Directories, Is.Empty);
    }

    [Test]
    public void Parse_EnforcesSegmentAndDatasetCountLimits()
    {
        List<byte> data = new List<byte>();
        AddStandard(data, 2, 5, Encoding.ASCII.GetBytes("one"));
        AddStandard(data, 2, 25, Encoding.ASCII.GetBytes("two"));
        ImageMetadataReaderOptions countOptions = new ImageMetadataReaderOptions();
        countOptions.MaxTagCount = 1;

        ImageMetadataDocument count = Parse(data.ToArray(), 0, countOptions);

        AssertCode(count, "test.iptc.dataset.count.limit", MetadataSeverity.LimitExceeded);
        Assert.That(count.Directories.Single().Tags, Has.Count.EqualTo(1));

        ImageMetadataReaderOptions sizeOptions = new ImageMetadataReaderOptions();
        sizeOptions.MaxSegmentBytes = data.Count - 1;
        ImageMetadataDocument size = Parse(data.ToArray(), 0, sizeOptions);
        AssertCode(size, "test.iptc.length.limit", MetadataSeverity.LimitExceeded);
        Assert.That(size.Directories, Is.Empty);
    }

    [Test]
    public void DatasetNameTable_ProvidesCommonEnvelopeApplicationAndFallbackNames()
    {
        Assert.That(IptcDatasetNameTable.NameFor(1, 30), Is.EqualTo("ServiceIdentifier"));
        Assert.That(IptcDatasetNameTable.NameFor(2, 4), Is.EqualTo("ObjectAttributeReference"));
        Assert.That(IptcDatasetNameTable.NameFor(2, 92), Is.EqualTo("SubLocation"));
        Assert.That(IptcDatasetNameTable.NameFor(2, 202), Is.EqualTo("ObjectPreviewData"));
        Assert.That(IptcDatasetNameTable.NameFor(2, 249), Is.EqualTo("Dataset249"));
        Assert.That(IptcDatasetNameTable.RecordGroup(8), Is.EqualTo("ObjectData"));
        Assert.That(IptcDatasetNameTable.RecordGroup(10), Is.EqualTo("Record10"));
    }

    public static ImageMetadataDocument Parse(byte[] data, long baseOffset, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        IptcIimParser.Parse(document, data, baseOffset, "TEST/IPTC", "test.iptc", "IPTC", options);
        return document;
    }

    public static void AddStandard(List<byte> bytes, int record, int dataset, byte[] value)
    {
        bytes.Add(28);
        bytes.Add((byte)record);
        bytes.Add((byte)dataset);
        bytes.Add((byte)(value.Length >> 8));
        bytes.Add((byte)value.Length);
        bytes.AddRange(value);
    }

    public static void AddExtended(List<byte> bytes, int record, int dataset, byte[] value)
    {
        bytes.Add(28);
        bytes.Add((byte)record);
        bytes.Add((byte)dataset);
        bytes.Add(128);
        bytes.Add(4);
        bytes.Add((byte)(value.Length >> 24));
        bytes.Add((byte)(value.Length >> 16));
        bytes.Add((byte)(value.Length >> 8));
        bytes.Add((byte)value.Length);
        bytes.AddRange(value);
    }

    public static MetadataTag Find(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key);
    }

    public static void AssertCode(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }
}
