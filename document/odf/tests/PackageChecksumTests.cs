using System;

namespace odf.Tests;

public class PackageChecksumTests
{
    [TestCase(
        OdfAlgorithmIdentifiers.Sha1OneKilobyteName,
        "A9993E364706816ABA3E25717850C26C9CD0D89D")]
    [TestCase(
        OdfAlgorithmIdentifiers.Sha1OneKilobyte,
        "A9993E364706816ABA3E25717850C26C9CD0D89D")]
    [TestCase(
        OdfAlgorithmIdentifiers.Sha1,
        "A9993E364706816ABA3E25717850C26C9CD0D89D")]
    [TestCase(
        OdfAlgorithmIdentifiers.Sha256OneKilobyte,
        "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")]
    [TestCase(
        OdfAlgorithmIdentifiers.Sha256,
        "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")]
    [TestCase(
        OdfAlgorithmIdentifiers.Sha384,
        "CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED" +
        "8086072BA1E7CC2358BAECA134C825A7")]
    [TestCase(
        OdfAlgorithmIdentifiers.Sha512,
        "DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A" +
        "2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F")]
    public void CreatesKnownDigestVectors(string algorithmName, string expectedHex)
    {
        byte[] checksum = PackageChecksum.CreateChecksum(
            [97, 98, 99],
            algorithmName);

        Assert.That(Convert.ToHexString(checksum), Is.EqualTo(expectedHex));
    }

    [Test]
    public void OneKilobyteChecksumsIgnoreCompressedSuffixBytes()
    {
        byte[] first = CreateSequence(1025);
        byte[] second = (byte[])first.Clone();
        second[1024] ^= 127;

        byte[] firstPrefix = PackageChecksum.CreateChecksum(
            first,
            OdfAlgorithmIdentifiers.Sha256OneKilobyte);
        byte[] secondPrefix = PackageChecksum.CreateChecksum(
            second,
            OdfAlgorithmIdentifiers.Sha256OneKilobyte);
        byte[] firstFull = PackageChecksum.CreateChecksum(
            first,
            OdfAlgorithmIdentifiers.Sha256);
        byte[] secondFull = PackageChecksum.CreateChecksum(
            second,
            OdfAlgorithmIdentifiers.Sha256);

        Assert.Multiple(() =>
        {
            Assert.That(secondPrefix, Is.EqualTo(firstPrefix));
            Assert.That(secondFull, Is.Not.EqualTo(firstFull));
            Assert.That(
                PackageChecksum.VerifyChecksum(
                    first,
                    OdfAlgorithmIdentifiers.Sha256OneKilobyte,
                    firstPrefix),
                Is.True);
            Assert.That(
                PackageChecksum.VerifyChecksum(
                    first,
                    OdfAlgorithmIdentifiers.Sha256,
                    secondFull),
                Is.False);
        });
    }

    [Test]
    public void RejectsUnsupportedChecksumAlgorithms()
    {
        Assert.Throws<OdfSecurityException>(() =>
            PackageChecksum.CreateChecksum([1, 2, 3], "urn:example:unknown"));
    }

    static byte[] CreateSequence(int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(index & 255);
            index++;
        }
        return result;
    }
}
