using System;
using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicReaderMalformedInputIntegrationTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void TruncatedPng_ReturnsTheSamePartialPublicResultAcrossEntryPoints(int entryPoint)
    {
        byte[] bytes = CreateTruncatedPng();
        ImageMetadataDocument document;
        string path = Path.Combine(Path.GetTempPath(), "image-malformed-public-" + Guid.NewGuid().ToString("N") + ".png");

        if (entryPoint == 0)
        {
            File.WriteAllBytes(path, bytes);
            try
            {
                document = new ImageMetadataReader().ReadFile(path);
            }
            finally
            {
                File.Delete(path);
            }
        }
        else if (entryPoint == 1)
        {
            document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));
        }
        else
        {
            document = new ImageMetadataReader().ReadStream(new NonSeekableReadStream(bytes));
        }

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(document.IsCorrupt, Is.True);
        Assert.That(document.IsIncomplete, Is.True);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.Header.SignatureValid").Value.BooleanValue, Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Png.tEXt.Length"), Is.Null);
        PublicReaderOptionsIntegrationTests.AssertDiagnostic(document, "png.chunk.truncated", MetadataSeverity.Truncated);
    }

    [Test]
    public void HugeDeclaredChunk_ReturnsPartialStructureAndBothLimitAndTruncationDiagnostics()
    {
        List<byte> bytes = new List<byte>();
        PngChunkTraverserTests.AddSignature(bytes);
        PngChunkTraverserTests.AddUnsigned32(bytes, uint.MaxValue);
        PngChunkTraverserTests.AddAscii(bytes, "tEXt");

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new NonSeekableReadStream(bytes.ToArray()));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.IsCorrupt, Is.True);
        Assert.That(document.IsIncomplete, Is.True);
        PublicReaderOptionsIntegrationTests.AssertDiagnostic(document, "png.chunk.length.limit", MetadataSeverity.LimitExceeded);
        PublicReaderOptionsIntegrationTests.AssertDiagnostic(document, "png.chunk.truncated", MetadataSeverity.Truncated);
    }

    [Test]
    public void FileLimit_ReturnsEquivalentHeaderMetadataForFileSeekableAndNonSeekableInputs()
    {
        byte[] bytes = QoiMetadataParserTests.CreateQoi(9, 7, 4, 1, new byte[] { 255, 1, 2, 3, 4 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFileBytes = 14;
        string path = Path.Combine(Path.GetTempPath(), "image-limited-public-" + Guid.NewGuid().ToString("N") + ".qoi");
        File.WriteAllBytes(path, bytes);

        try
        {
            IImageMetadataReader reader = new ImageMetadataReader();
            ImageMetadataDocument file = reader.ReadFileWithOptions(path, options);
            ImageMetadataDocument seekable = reader.ReadStreamWithOptions(new MemoryStream(bytes), options);
            ImageMetadataDocument nonSeekable = reader.ReadStreamWithOptions(new NonSeekableReadStream(bytes), options);

            AssertLimitedQoi(file);
            AssertLimitedQoi(seekable);
            AssertLimitedQoi(nonSeekable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static byte[] CreateTruncatedPng()
    {
        List<byte> bytes = new List<byte>();
        PngChunkTraverserTests.AddSignature(bytes);
        PngChunkTraverserTests.AddUnsigned32(bytes, 5);
        PngChunkTraverserTests.AddAscii(bytes, "tEXt");
        bytes.Add(1);
        bytes.Add(2);
        return bytes.ToArray();
    }

    public static void AssertLimitedQoi(ImageMetadataDocument document)
    {
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Qoi));
        Assert.That(document.Summary.Width, Is.EqualTo(9));
        Assert.That(document.Summary.Height, Is.EqualTo(7));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.IsIncomplete, Is.True);
        PublicReaderOptionsIntegrationTests.AssertDiagnostic(document, "reader.file.limit", MetadataSeverity.LimitExceeded);
        PublicReaderOptionsIntegrationTests.AssertDiagnostic(document, "qoi.end-marker.truncated", MetadataSeverity.Truncated);
    }
}
