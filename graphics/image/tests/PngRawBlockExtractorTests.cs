using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PngRawBlockExtractorTests
{
    [Test]
    public void Parse_UnknownAncillaryChunkPreservesPayloadRangeByDefault()
    {
        byte[] payload = new byte[] { 1, 2, 3, 4 };

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "vpAg", "IEND" },
            new byte[][] { Ihdr(), payload, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Family, Is.EqualTo(MetadataFamily.Png));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("vpAg"));
        Assert.That(document.RawBlocks[0].Name, Is.EqualTo("Unknown PNG Ancillary Chunk"));
        Assert.That(document.RawBlocks[0].Offset, Is.EqualTo(41));
        Assert.That(document.RawBlocks[0].Length, Is.EqualTo(4));
        Assert.That(document.RawBlocks[0].Data, Is.Empty);
    }

    [Test]
    public void Parse_CapturesUnknownAncillaryPayloadWhenEnabled()
    {
        byte[] payload = new byte[] { 9, 8, 7 };
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "aaAA", "IEND" },
            new byte[][] { Ihdr(), payload, new byte[0] }), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(payload));
        Assert.That(document.RawBlocks[0].Data, Is.Not.SameAs(payload));
    }

    [Test]
    public void Parse_PreservesRepeatedUnknownAncillaryChunksInSourceOrder()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "vpAg", "aaAA", "vpAg", "IEND" },
            new byte[][] { Ihdr(), new byte[] { 1 }, new byte[] { 2, 3 }, new byte[] { 4 }, new byte[0] }), options);

        Assert.That(document.RawBlocks, Has.Count.EqualTo(3));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("vpAg"));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(new byte[] { 1 }));
        Assert.That(document.RawBlocks[1].BlockType, Is.EqualTo("aaAA"));
        Assert.That(document.RawBlocks[1].Data, Is.EqualTo(new byte[] { 2, 3 }));
        Assert.That(document.RawBlocks[2].BlockType, Is.EqualTo("vpAg"));
        Assert.That(document.RawBlocks[2].Data, Is.EqualTo(new byte[] { 4 }));
        Assert.That(document.RawBlocks[0].Offset, Is.LessThan(document.RawBlocks[1].Offset));
        Assert.That(document.RawBlocks[1].Offset, Is.LessThan(document.RawBlocks[2].Offset));
    }

    [Test]
    public void Parse_CaptureRawBlocksFalseSkipsUnknownAncillaryChunks()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlocks = false;
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "vpAg", "IEND" },
            new byte[][] { Ihdr(), new byte[] { 1, 2 }, new byte[0] }), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
    }

    [Test]
    public void Parse_RawBlockLimitKeepsRangeAndOmitsPayload()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;
        options.MaxRawBlockBytes = 2;

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "vpAg", "IEND" },
            new byte[][] { Ihdr(), new byte[] { 1, 2, 3 }, new byte[0] }), options);

        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Length, Is.EqualTo(3));
        Assert.That(document.RawBlocks[0].Data, Is.Empty);
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.raw.block.limit" && diagnostic.Severity == MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_KnownAncillaryAndUnknownCriticalChunksAreNotRawBlocks()
    {
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "gAMA", "tEXt", "ABCD", "IEND" },
            new byte[][] { Ihdr(), PngColorExtractorTests.Unsigned32(45455), new byte[] { 75, 0, 86 }, new byte[] { 5, 6 }, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.gAMA.Gamma"), Is.Not.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.tEXt.Text"), Is.Not.Null);
    }

    [Test]
    public void Parse_CrcInvalidUnknownAncillaryChunkStillPreservesRawPayload()
    {
        byte[] png = CreatePng(
            new string[] { "IHDR", "vpAg", "IEND" },
            new byte[][] { Ihdr(), new byte[] { 1, 2, 3 }, new byte[0] });
        png[47] = (byte)(png[47] + 1);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = ParsePng(png, options);

        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.crc.mismatch"));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    public static byte[] Ihdr()
    {
        return PngIhdrExtractorTests.CreateIhdr(16, 12, 8, 6, 0, 0, 0);
    }

    public static byte[] CreatePng(string[] types, byte[][] payloads)
    {
        return PngChunkTraverserTests.CreatePng(types, payloads);
    }

    public static ImageMetadataDocument ParsePng(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return PngMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Png), options);
    }
}
