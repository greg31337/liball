using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class RiffChunkTraverserTests
{
    [Test]
    public void Parse_ReadsHeaderChunksPayloadsAndPadding()
    {
        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "XMP " },
            new byte[][] { new byte[] { 1, 2 }, new byte[] { 3, 4, 5 } },
            new byte[] { 0, 0 });
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        ImageMetadataDocument document = new ImageMetadataDocument();

        RiffHeader header = RiffChunkTraverser.ReadHeader(document, reader);
        List<RiffChunk> chunks = RiffChunkTraverser.ReadChunks(document, reader, header.DeclaredEnd, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(header.Valid, Is.True);
        Assert.That(header.Signature, Is.EqualTo("RIFF"));
        Assert.That(header.FormType, Is.EqualTo("WEBP"));
        Assert.That(header.DeclaredEnd, Is.EqualTo(bytes.Length));
        Assert.That(chunks, Has.Count.EqualTo(2));
        Assert.That(chunks[0].Index, Is.EqualTo(0));
        Assert.That(chunks[0].FourCc, Is.EqualTo("VP8X"));
        Assert.That(chunks[0].Offset, Is.EqualTo(12));
        Assert.That(chunks[0].DataOffset, Is.EqualTo(20));
        Assert.That(chunks[0].Length, Is.EqualTo(2));
        Assert.That(chunks[0].Data, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(chunks[0].HasPadding, Is.False);
        Assert.That(chunks[1].FourCc, Is.EqualTo("XMP "));
        Assert.That(chunks[1].Offset, Is.EqualTo(22));
        Assert.That(chunks[1].Data, Is.EqualTo(new byte[] { 3, 4, 5 }));
        Assert.That(chunks[1].HasPadding, Is.True);
        Assert.That(chunks[1].PaddingOffset, Is.EqualTo(33));
        Assert.That(chunks[1].PaddingValid, Is.True);
    }

    [Test]
    public void Parse_CreatesOrderedDirectoriesAndUsesWebPDispatch()
    {
        byte[] bytes = CreateWebP(
            new string[] { "ABCD", "EFGH" },
            new byte[][] { new byte[0], new byte[] { 9, 8 } },
            new byte[] { 0, 0 });
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.WebP));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(document.Directories, Has.Count.EqualTo(3));
        Assert.That(document.Directories[0].Path, Is.EqualTo("RIFF/WEBP/Header"));
        Assert.That(document.Directories[1].Path, Is.EqualTo("RIFF/WEBP/ABCD/0"));
        Assert.That(document.Directories[2].Path, Is.EqualTo("RIFF/WEBP/EFGH/0"));
        Assert.That(document.Directories[2].Offset, Is.EqualTo(20));
        Assert.That(document.Directories[2].Length, Is.EqualTo(10));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.Header.Signature").Value.Text, Is.EqualTo("RIFF"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.Header.FormType").Value.Text, Is.EqualTo("WEBP"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.Header.DeclaredByteLength").Value.IntegerValue, Is.EqualTo(bytes.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.ABCD.Index").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.EFGH.Length").Value.UnsignedIntegerValue, Is.EqualTo(2));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_SkipsLimitedPayloadAndContinuesTraversal()
    {
        byte[] bytes = CreateWebP(
            new string[] { "BIGG", "NEXT" },
            new byte[][] { new byte[] { 1, 2, 3, 4 }, new byte[0] },
            new byte[] { 0, 0 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 3;

        ImageMetadataDocument document = ParseRiff(bytes, options);

        AssertDiagnostic(document, "riff.chunk.length.limit", MetadataSeverity.LimitExceeded);
        Assert.That(document.Directories, Has.Count.EqualTo(3));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.BIGG.DataLimited").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.NEXT.DataLimited").Value.BooleanValue, Is.False);
    }

    [Test]
    public void Parse_AssignsPerTypeRepeatIndexes()
    {
        byte[] bytes = CreateWebP(
            new string[] { "SAME", "OTHR", "SAME" },
            new byte[][] { new byte[0], new byte[0], new byte[0] },
            new byte[] { 0, 0, 0 });

        ImageMetadataDocument document = ParseRiff(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories[1].Path, Is.EqualTo("RIFF/WEBP/SAME/0"));
        Assert.That(document.Directories[1].RepeatIndex, Is.EqualTo(0));
        Assert.That(document.Directories[2].Path, Is.EqualTo("RIFF/WEBP/OTHR/0"));
        Assert.That(document.Directories[2].RepeatIndex, Is.EqualTo(0));
        Assert.That(document.Directories[3].Path, Is.EqualTo("RIFF/WEBP/SAME/1"));
        Assert.That(document.Directories[3].RepeatIndex, Is.EqualTo(1));
    }

    [Test]
    public void Parse_ReportsInvalidFourCcAndNonzeroPadding()
    {
        byte[] bytes = CreateWebP(
            new string[] { "A\u0001CD" },
            new byte[][] { new byte[] { 7 } },
            new byte[] { 99 });

        ImageMetadataDocument document = ParseRiff(bytes, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "riff.chunk.type.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "riff.chunk.padding.invalid", MetadataSeverity.RecoverableError);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[1].Tags[5].Value.BooleanValue, Is.False);
        Assert.That(document.Directories[1].Tags[4].Value.BooleanValue, Is.False);
    }

    [Test]
    public void Parse_ReportsInvalidAndTruncatedHeaders()
    {
        ImageMetadataDocument shortHeader = ParseRiff(Encoding.ASCII.GetBytes("RIFF"), new ImageMetadataReaderOptions());
        AssertDiagnostic(shortHeader, "riff.header.truncated", MetadataSeverity.Truncated);

        byte[] wrongSignature = CreateHeader("RIFX", 4, "WEBP");
        ImageMetadataDocument invalidSignature = ParseRiff(wrongSignature, new ImageMetadataReaderOptions());
        AssertDiagnostic(invalidSignature, "riff.signature.invalid", MetadataSeverity.RecoverableError);

        byte[] wrongForm = CreateHeader("RIFF", 4, "WAVE");
        ImageMetadataDocument invalidForm = ParseRiff(wrongForm, new ImageMetadataReaderOptions());
        AssertDiagnostic(invalidForm, "riff.form.invalid", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_RespectsDeclaredBoundaryAndReportsTrailingData()
    {
        byte[] first = CreateWebP(new string[] { "ONE1" }, new byte[][] { new byte[0] }, new byte[] { 0 });
        List<byte> bytes = new List<byte>(first);
        AddAscii(bytes, "TWO2");
        AddUnsigned32(bytes, 0);

        ImageMetadataDocument document = ParseRiff(bytes.ToArray(), new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "riff.trailing.data", MetadataSeverity.Warning);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.ONE1.Index"), Is.Not.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Riff.TWO2.Index"), Is.Null);
    }

    [Test]
    public void Parse_ReportsDeclaredFileAndChunkTruncation()
    {
        List<byte> bytes = new List<byte>(CreateHeader("RIFF", 14, "WEBP"));
        AddAscii(bytes, "DATA");
        AddUnsigned32(bytes, 2);
        bytes.Add(1);

        ImageMetadataDocument document = ParseRiff(bytes.ToArray(), new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "riff.file.truncated", MetadataSeverity.Truncated);
        AssertDiagnostic(document, "riff.chunk.truncated", MetadataSeverity.Truncated);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_EnforcesChunkCountLimit()
    {
        byte[] bytes = CreateWebP(
            new string[] { "ONE1", "TWO2" },
            new byte[][] { new byte[0], new byte[0] },
            new byte[] { 0, 0 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxTagCount = 1;

        ImageMetadataDocument document = ParseRiff(bytes, options);

        AssertDiagnostic(document, "riff.chunk.count.limit", MetadataSeverity.LimitExceeded);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Riff.ONE1.Index"), Is.Not.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Riff.TWO2.Index"), Is.Null);
    }

    public static byte[] CreateWebP(string[] types, byte[][] payloads, byte[] paddingValues)
    {
        List<byte> body = new List<byte>();
        AddAscii(body, "WEBP");
        int index = 0;
        while (index < types.Length)
        {
            AddAscii(body, types[index]);
            AddUnsigned32(body, (uint)payloads[index].Length);
            AddBytes(body, payloads[index]);
            if ((payloads[index].Length & 1) != 0)
            {
                body.Add(paddingValues[index]);
            }

            index = index + 1;
        }

        List<byte> bytes = new List<byte>();
        AddAscii(bytes, "RIFF");
        AddUnsigned32(bytes, (uint)body.Count);
        AddBytes(bytes, body.ToArray());
        return bytes.ToArray();
    }

    public static byte[] CreateHeader(string signature, uint fileSize, string formType)
    {
        List<byte> bytes = new List<byte>();
        AddAscii(bytes, signature);
        AddUnsigned32(bytes, fileSize);
        AddAscii(bytes, formType);
        return bytes.ToArray();
    }

    public static void AddAscii(List<byte> bytes, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            bytes.Add((byte)text[index]);
            index = index + 1;
        }
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value & 255));
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)((value >> 16) & 255));
        bytes.Add((byte)((value >> 24) & 255));
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

    public static ImageMetadataDocument ParseRiff(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return WebPMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.WebP), options);
    }
}
