using System.Collections.Generic;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class HashHelperTests
{
    [Test]
    public void ComputeSha256Hex_ReturnsKnownHash()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("abc");

        string hash = HashHelper.ComputeSha256Hex(bytes);

        Assert.That(hash, Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
    }

    [Test]
    public void ComputeSha1AndMd5_ReturnKnownHashes()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("abc");

        string sha1 = HashHelper.ToHex(HashHelper.ComputeSha1(bytes));
        string md5 = HashHelper.ToHex(HashHelper.ComputeMd5(bytes));

        Assert.That(sha1, Is.EqualTo("a9993e364706816aba3e25717850c26c9cd0d89d"));
        Assert.That(md5, Is.EqualTo("900150983cd24fb0d6963f7d28e17f72"));
    }

    [Test]
    public void ComputeHash_DispatchesCommonAlgorithmNamesCaseInsensitively()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = Encoding.ASCII.GetBytes("abc");

        string sha256 = HashHelper.ComputeHashHex(bytes, "sha-256", diagnostics, "hash");
        string sha1 = HashHelper.ComputeHashHex(bytes, "SHA1", diagnostics, "hash");
        string md5 = HashHelper.ComputeHashHex(bytes, "md5", diagnostics, "hash");

        Assert.That(sha256, Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        Assert.That(sha1, Is.EqualTo("a9993e364706816aba3e25717850c26c9cd0d89d"));
        Assert.That(md5, Is.EqualTo("900150983cd24fb0d6963f7d28e17f72"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void HashRange_HashesOnlySelectedBytes()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = Encoding.ASCII.GetBytes("xxabcyy");

        string hash = HashHelper.ToHex(HashHelper.ComputeSha256Range(bytes, 2, 3, diagnostics, "range"));

        Assert.That(hash, Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ToHex_UsesLowercaseAndPreservesLeadingZeroes()
    {
        byte[] bytes = new byte[] { 0, 1, 15, 16, 255 };

        string hex = HashHelper.ToHex(bytes);

        Assert.That(hex, Is.EqualTo("00010f10ff"));
    }

    [Test]
    public void InvalidAlgorithmNames_ReturnDiagnostics()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = Encoding.ASCII.GetBytes("abc");

        byte[] emptyName = HashHelper.ComputeHash(bytes, "", diagnostics, "hash");
        byte[] unsupported = HashHelper.ComputeHash(bytes, "CRC32", diagnostics, "hash");

        Assert.That(emptyName, Is.Empty);
        Assert.That(unsupported, Is.Empty);
        Assert.That(diagnostics, Has.Count.EqualTo(2));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
        Assert.That(diagnostics[0].Code, Is.EqualTo("hash.algorithm.empty"));
        Assert.That(diagnostics[1].Severity, Is.EqualTo(MetadataSeverity.Unsupported));
        Assert.That(diagnostics[1].Code, Is.EqualTo("hash.algorithm.unsupported"));
    }

    [Test]
    public void InvalidRanges_ReturnEmptyHashAndDiagnostics()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = new byte[] { 1, 2, 3 };

        byte[] nullHash = HashHelper.ComputeSha256Range(null, 0, 1, diagnostics, "hash");
        byte[] negativeHash = HashHelper.ComputeSha256Range(bytes, -1, 1, diagnostics, "hash");
        byte[] outsideHash = HashHelper.ComputeSha256Range(bytes, 5, 1, diagnostics, "hash");
        byte[] truncatedHash = HashHelper.ComputeSha256Range(bytes, 2, 4, diagnostics, "hash");

        Assert.That(nullHash, Is.Empty);
        Assert.That(negativeHash, Is.Empty);
        Assert.That(outsideHash, Is.Empty);
        Assert.That(truncatedHash, Is.Empty);
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(2));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.Truncated), Is.EqualTo(2));
    }
}
