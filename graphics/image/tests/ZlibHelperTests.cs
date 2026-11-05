using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ZlibHelperTests
{
    [Test]
    public void Decompress_WithValidZlibData_ReturnsOriginalBytes()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] original = Encoding.UTF8.GetBytes("metadata text");
        byte[] compressed = Compress(original);

        byte[] decompressed = ZlibHelper.Decompress(compressed, 100, diagnostics, "zTXt");

        Assert.That(Encoding.UTF8.GetString(decompressed), Is.EqualTo("metadata text"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void Decompress_WhenOutputExceedsLimit_ReturnsPrefixAndAddsDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] original = Encoding.ASCII.GetBytes("abcdefghij");
        byte[] compressed = Compress(original);

        byte[] decompressed = ZlibHelper.Decompress(compressed, 4, diagnostics, "iCCP");

        Assert.That(Encoding.ASCII.GetString(decompressed), Is.EqualTo("abcd"));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
        Assert.That(diagnostics[0].Code, Is.EqualTo("zlib.limit"));
    }

    [Test]
    public void Decompress_WithZeroLimit_ReturnsEmptyAndAddsDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] compressed = Compress(Encoding.ASCII.GetBytes("abc"));

        byte[] decompressed = ZlibHelper.Decompress(compressed, 0, diagnostics, "zTXt");

        Assert.That(decompressed, Is.Empty);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("zlib.limit"));
    }

    [Test]
    public void Decompress_WhenOutputExactlyMatchesLimit_DoesNotAddDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] original = Encoding.ASCII.GetBytes("abcd");

        byte[] decompressed = ZlibHelper.Decompress(Compress(original), 4, diagnostics, "zTXt");

        Assert.That(decompressed, Is.EqualTo(original));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void Decompress_EmptyOutputWithZeroLimit_DoesNotAddDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        byte[] emptyZlib = new byte[] { 120, 156, 3, 0, 0, 0, 0, 1 };

        byte[] decompressed = ZlibHelper.Decompress(emptyZlib, 0, diagnostics, "iTXt");

        Assert.That(decompressed, Is.Empty);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ValidateInput_RejectsNullNegativeLimitAndTruncatedHeaders()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        byte[] nullResult = ZlibHelper.Decompress(null, 10, diagnostics, "zTXt");
        byte[] negativeLimitResult = ZlibHelper.Decompress(new byte[] { 120, 156 }, -1, diagnostics, "zTXt");
        byte[] truncatedResult = ZlibHelper.Decompress(new byte[] { 120 }, 10, diagnostics, "zTXt");

        Assert.That(nullResult, Is.Empty);
        Assert.That(negativeLimitResult, Is.Empty);
        Assert.That(truncatedResult, Is.Empty);
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(2));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.Truncated), Is.EqualTo(1));
    }

    [Test]
    public void HasSupportedHeader_RejectsInvalidHeaderCheckAndUnsupportedDictionary()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        bool invalidCheck = ZlibHelper.HasSupportedHeader(new byte[] { 120, 157 }, diagnostics, "zTXt");
        bool dictionary = ZlibHelper.HasSupportedHeader(new byte[] { 120, 32 }, diagnostics, "zTXt");

        Assert.That(invalidCheck, Is.False);
        Assert.That(dictionary, Is.False);
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
        Assert.That(diagnostics[0].Code, Is.EqualTo("zlib.header.invalid"));
        Assert.That(diagnostics[1].Severity, Is.EqualTo(MetadataSeverity.Unsupported));
        Assert.That(diagnostics[1].Code, Is.EqualTo("zlib.dictionary.unsupported"));
    }

    [Test]
    public void HasSupportedHeader_RejectsUnsupportedCompressionMethod()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        bool supported = ZlibHelper.HasSupportedHeader(new byte[] { 113, 29 }, diagnostics, "zTXt");

        Assert.That(supported, Is.False);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Unsupported));
        Assert.That(diagnostics[0].Code, Is.EqualTo("zlib.method.unsupported"));
    }

    static byte[] Compress(byte[] bytes)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (ZLibStream zlib = new ZLibStream(output, CompressionLevel.SmallestSize, true))
            {
                zlib.Write(bytes, 0, bytes.Length);
            }

            return output.ToArray();
        }
    }
}
