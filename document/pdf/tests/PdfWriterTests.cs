using System.Text;
using System.Text.RegularExpressions;
using System.IO.Compression;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfWriterTests
{
    [Test]
    public void Save_AssignsExpectedObjectIdsForImagesPagesAndContentStreams()
    {
        string pdf = WriteTwoPageDocument();

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/Type /Catalog /Pages 2 0 R"));
        Assert.That(GetObjectBody(pdf, 2), Does.Contain("/Type /Pages /Count 2 /Kids [7 0 R 9 0 R ]"));
        Assert.That(GetObjectBody(pdf, 3), Does.Contain("/BaseFont /Helvetica"));
        Assert.That(GetObjectBody(pdf, 4), Does.Contain("/BaseFont /Helvetica-Bold"));
        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/Width 1 /Height 1"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/Width 1 /Height 1"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/Contents 8 0 R"));
        Assert.That(GetObjectBody(pdf, 9), Does.Contain("/Contents 10 0 R"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/XObject << /Im1 5 0 R /Im2 6 0 R >>"));
        Assert.That(GetObjectBody(pdf, 9), Does.Contain("/XObject << /Im1 5 0 R /Im2 6 0 R >>"));
    }

    [Test]
    public void Save_WritesXrefEntriesThatMatchActualObjectOffsets()
    {
        string pdf = WriteTwoPageDocument();
        Match xref = Regex.Match(pdf, @"xref\n0 (?<count>\d+)\n(?<entries>(?:\d{10} \d{5} [nf] \n)+)");

        Assert.That(xref.Success, Is.True);
        Assert.That(xref.Groups["count"].Value, Is.EqualTo("11"));

        string[] entries = xref.Groups["entries"].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(entries.Length, Is.EqualTo(11));
        Assert.That(entries[0], Is.EqualTo("0000000000 65535 f "));

        for (int objectId = 1; objectId <= 10; objectId++)
        {
            int expectedOffset = Encoding.ASCII.GetByteCount(pdf.Substring(0, pdf.IndexOf(objectId.ToString() + " 0 obj\n", StringComparison.Ordinal)));
            string expectedEntry = expectedOffset.ToString("0000000000") + " 00000 n ";

            Assert.That(entries[objectId], Is.EqualTo(expectedEntry), "xref entry for object " + objectId.ToString());
        }
    }

    [Test]
    public void Save_WritesTrailerSizeRootAndStartXref()
    {
        string pdf = WriteTwoPageDocument();
        Match trailer = Regex.Match(pdf, @"trailer\n<< /Size (?<size>\d+) /Root (?<root>\d+) 0 R >>\nstartxref\n(?<start>\d+)\n%%EOF\n$");

        Assert.That(trailer.Success, Is.True);
        Assert.That(trailer.Groups["size"].Value, Is.EqualTo("11"));
        Assert.That(trailer.Groups["root"].Value, Is.EqualTo("1"));

        int expectedStartXref = Encoding.ASCII.GetByteCount(pdf.Substring(0, pdf.IndexOf("xref\n", StringComparison.Ordinal)));
        Assert.That(trailer.Groups["start"].Value, Is.EqualTo(expectedStartXref.ToString()));
    }

    [Test]
    public void Save_WritesMetadataInfoDictionaryAndTrailerReference()
    {
        string pdf = WriteMetadataDocument();
        string infoBody = GetObjectBody(pdf, 5);

        Assert.That(infoBody, Does.Contain("/Title (Annual \\(Q1\\) \\\\ Draft)"));
        Assert.That(infoBody, Does.Contain("/Author (Finance Team)"));
        Assert.That(infoBody, Does.Contain("/Subject (Board report)"));
        Assert.That(infoBody, Does.Contain("/Keywords (revenue, forecast)"));
        Assert.That(infoBody, Does.Contain("/Creator (pdf sample)"));
        Assert.That(infoBody, Does.Contain("/Producer (pdf library)"));
        Assert.That(GetObjectBody(pdf, 2), Does.Contain("/Kids [6 0 R ]"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/Contents 7 0 R"));
        Assert.That(pdf, Does.Contain("trailer\n<< /Size 8 /Root 1 0 R /Info 5 0 R >>"));
    }

    [Test]
    public void Save_DoesNotWriteInfoDictionaryWhenMetadataIsEmpty()
    {
        string pdf = WriteTwoPageDocument();

        Assert.That(pdf, Does.Not.Contain("/Info"));
        Assert.That(pdf, Does.Not.Contain("/ID"));
        Assert.That(pdf, Does.Not.Contain("/FlateDecode"));
        Assert.That(pdf, Does.Contain("trailer\n<< /Size 11 /Root 1 0 R >>"));
    }

    [Test]
    public void SetMetadata_RejectsNull()
    {
        PdfDocument document = new PdfDocument();

        Assert.Throws<ArgumentNullException>(() => document.SetMetadata(null!));
    }

    [Test]
    public void Save_DeterministicWriterModeWritesStableTrailerId()
    {
        PdfDocument document = CreateSinglePageDocument();

        document.UseDeterministicWriterMode();
        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/ID [<00000000000000000000000000000000><00000000000000000000000000000000>]"));
    }

    [Test]
    public void Save_DeterministicWriterModeAcceptsCustomDocumentId()
    {
        PdfDocument document = CreateSinglePageDocument();

        document.UseDeterministicWriterModeWithId("abcdef0123456789abcdef0123456789");
        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/ID [<ABCDEF0123456789ABCDEF0123456789><ABCDEF0123456789ABCDEF0123456789>]"));
    }

    [Test]
    public void Save_DeterministicWriterModeRejectsInvalidDocumentIds()
    {
        PdfDocument document = CreateSinglePageDocument();

        Assert.Throws<ArgumentException>(() => document.UseDeterministicWriterModeWithId("abc"));
        Assert.Throws<ArgumentException>(() => document.UseDeterministicWriterModeWithId("0000000000000000000000000000000z"));
    }

    [Test]
    public void Save_DeterministicWriterModeProducesStableBytesAcrossDocuments()
    {
        PdfDocument first = CreateSinglePageDocument();
        PdfDocument second = CreateSinglePageDocument();

        first.UseDeterministicWriterModeWithId("11111111111111111111111111111111");
        second.UseDeterministicWriterModeWithId("11111111111111111111111111111111");

        Assert.That(second.ToBytes(), Is.EqualTo(first.ToBytes()));
    }

    [Test]
    public void Save_CompressedContentStreamsWriteFlateDecodeFilterAndValidLength()
    {
        PdfDocument document = CreateCompressionDocument();

        document.UseCompressedContentStreams();
        byte[] pdfBytes = document.ToBytes();
        string pdf = Encoding.ASCII.GetString(pdfBytes);
        string contentBody = GetObjectBody(pdf, 6);
        byte[] streamBytes = ExtractStreamBytes(pdfBytes, 6);
        int declaredLength = GetDeclaredStreamLength(contentBody);

        Assert.That(contentBody, Does.Contain("/Filter /FlateDecode"));
        Assert.That(declaredLength, Is.EqualTo(streamBytes.Length));
    }

    [Test]
    public void Save_CompressedContentStreamsInflateToOriginalCanvasCommands()
    {
        PdfDocument document = CreateCompressionDocument();

        document.UseCompressedContentStreams();
        byte[] pdfBytes = document.ToBytes();
        byte[] compressedBytes = ExtractStreamBytes(pdfBytes, 6);
        string content = Encoding.ASCII.GetString(InflateBytes(compressedBytes));

        Assert.That(content, Does.Contain("BT"));
        Assert.That(content, Does.Contain("/F2 12 Tf"));
        Assert.That(content, Does.Contain("(Compressed content stream) Tj"));
        Assert.That(content, Does.Contain("10 20 m 80 20 l S"));
    }

    [Test]
    public void Save_CompressedContentStreamsProduceStableBytesAcrossDocuments()
    {
        PdfDocument first = CreateCompressionDocument();
        PdfDocument second = CreateCompressionDocument();

        first.UseCompressedContentStreams();
        second.UseCompressedContentStreams();

        Assert.That(second.ToBytes(), Is.EqualTo(first.ToBytes()));
    }

    [Test]
    public void Save_WritesStreamLengthsThatMatchActualStreamBytes()
    {
        string pdf = WriteTwoPageDocument();

        AssertStreamLengthMatches(pdf, 5, true);
        AssertStreamLengthMatches(pdf, 6, true);
        AssertStreamLengthMatches(pdf, 8, false);
        AssertStreamLengthMatches(pdf, 10, false);
    }

    static string WriteTwoPageDocument()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "writer-tests");
        string path = Path.Combine(directory, "writer-test-" + Guid.NewGuid().ToString("N") + ".pdf");
        PdfDocument document = new PdfDocument();
        PdfImage imageOne = document.AddImage(1, 1, new byte[] { 1, 2, 3 });
        PdfImage imageTwo = document.AddImage(1, 1, new byte[] { 4, 5, 6 });
        PdfPage firstPage = document.AddPage(200, 100);
        PdfPage secondPage = document.AddPage(300, 150);

        firstPage.Canvas.DrawText("First", 10, 20, 12);
        firstPage.Canvas.DrawImage(imageOne, 20, 30, 40, 50);
        secondPage.Canvas.DrawTextWithFont("Second", 11, 21, 13, PdfFont.Bold);
        secondPage.Canvas.DrawImage(imageTwo, 21, 31, 41, 51);

        document.Save(path);

        return File.ReadAllText(path, Encoding.ASCII);
    }

    static string WriteMetadataDocument()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "writer-tests");
        string path = Path.Combine(directory, "metadata-test-" + Guid.NewGuid().ToString("N") + ".pdf");
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        metadata.Title = "Annual (Q1) \\ Draft";
        metadata.Author = "Finance Team";
        metadata.Subject = "Board report";
        metadata.Keywords = "revenue, forecast";
        metadata.Creator = "pdf sample";
        metadata.Producer = "pdf library";
        document.SetMetadata(metadata);

        PdfPage page = document.AddPage(200, 100);
        page.Canvas.DrawText("Metadata test", 10, 20, 12);

        document.Save(path);

        return File.ReadAllText(path, Encoding.ASCII);
    }

    static PdfDocument CreateSinglePageDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        page.Canvas.DrawText("Stable", 10, 20, 12);
        return document;
    }

    static PdfDocument CreateCompressionDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(120, 80);

        page.Canvas.DrawTextWithFont("Compressed content stream", 10, 50, 12, PdfFont.Bold);
        page.Canvas.DrawLine(10, 20, 80, 20);
        page.Canvas.StrokeRectangle(10, 10, 30, 15);

        return document;
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }

    static void AssertStreamLengthMatches(string pdf, int objectId, bool streamEndsWithNewline)
    {
        string body = GetObjectBody(pdf, objectId);
        Match length = Regex.Match(body, @"/Length (?<length>\d+)");
        Assert.That(length.Success, Is.True, "object " + objectId.ToString() + " should declare a stream length");

        string streamData = ExtractStreamData(body, streamEndsWithNewline);
        int actualLength = Encoding.ASCII.GetByteCount(streamData);

        Assert.That(length.Groups["length"].Value, Is.EqualTo(actualLength.ToString()), "stream length for object " + objectId.ToString());
    }

    static int GetDeclaredStreamLength(string objectBody)
    {
        Match length = Regex.Match(objectBody, @"/Length (?<length>\d+)");
        Assert.That(length.Success, Is.True);
        return int.Parse(length.Groups["length"].Value);
    }

    static byte[] ExtractStreamBytes(byte[] pdfBytes, int objectId)
    {
        string pdf = Encoding.ASCII.GetString(pdfBytes);
        string objectStartMarker = objectId.ToString() + " 0 obj\n";
        string streamStartMarker = "stream\n";
        string streamEndMarker = "endstream";
        int objectStart = pdf.IndexOf(objectStartMarker, StringComparison.Ordinal);
        int streamStart = pdf.IndexOf(streamStartMarker, objectStart, StringComparison.Ordinal);
        int streamEnd = pdf.IndexOf(streamEndMarker, streamStart, StringComparison.Ordinal);

        Assert.That(objectStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(streamStart, Is.GreaterThan(objectStart));
        Assert.That(streamEnd, Is.GreaterThan(streamStart));

        streamStart += streamStartMarker.Length;

        byte[] streamBytes = new byte[streamEnd - streamStart];
        Buffer.BlockCopy(pdfBytes, streamStart, streamBytes, 0, streamBytes.Length);
        return streamBytes;
    }

    static byte[] InflateBytes(byte[] compressedBytes)
    {
        using (MemoryStream input = new MemoryStream(compressedBytes))
        {
            using (ZLibStream compressed = new ZLibStream(input, CompressionMode.Decompress))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    compressed.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }

    static string ExtractStreamData(string objectBody, bool streamEndsWithNewline)
    {
        string startMarker = "stream\n";
        string endMarker = "endstream";
        int start = objectBody.IndexOf(startMarker, StringComparison.Ordinal);
        int end = objectBody.IndexOf(endMarker, StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));

        start += startMarker.Length;
        if (streamEndsWithNewline)
        {
            end--;
        }

        return objectBody.Substring(start, end - start);
    }
}
