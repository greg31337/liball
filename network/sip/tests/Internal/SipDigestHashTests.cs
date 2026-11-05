namespace sip.Tests;

public class SipDigestHashTests
{
    [TestCase(SipDigestAlgorithm.Md5,
        "900150983cd24fb0d6963f7d28e17f72")]
    [TestCase(SipDigestAlgorithm.Md5Sess,
        "900150983cd24fb0d6963f7d28e17f72")]
    [TestCase(SipDigestAlgorithm.Sha256,
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [TestCase(SipDigestAlgorithm.Sha256Sess,
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [TestCase(SipDigestAlgorithm.Sha512256,
        "53048e2681941ef99b2e29b76b4c7dabe4c2d0c634fc6d46e0e2f13107e7af23")]
    [TestCase(SipDigestAlgorithm.Sha512256Sess,
        "53048e2681941ef99b2e29b76b4c7dabe4c2d0c634fc6d46e0e2f13107e7af23")]
    public void HashesUtf8TextWithEveryDigestAlgorithm(
        SipDigestAlgorithm algorithm, string expected)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("abc");

        byte[] hash = SipDigestHash.HashText(algorithm, "abc");
        string textHex = SipDigestHash.HashTextHex(algorithm, "abc");
        string byteHex = SipDigestHash.HashHex(algorithm, bytes);

        Assert.Multiple(() =>
        {
            Assert.That(System.Convert.ToHexString(hash).ToLowerInvariant(),
                Is.EqualTo(expected));
            Assert.That(textHex, Is.EqualTo(expected));
            Assert.That(byteHex, Is.EqualTo(expected));
            Assert.That(textHex, Does.Match("^[0-9a-f]+$"));
            Assert.That(hash, Has.Length.EqualTo(
                SipDigestHash.HashByteCount(algorithm)));
        });
    }

    [TestCase(SipDigestAlgorithm.Md5, false, 16)]
    [TestCase(SipDigestAlgorithm.Md5Sess, true, 16)]
    [TestCase(SipDigestAlgorithm.Sha256, false, 32)]
    [TestCase(SipDigestAlgorithm.Sha256Sess, true, 32)]
    [TestCase(SipDigestAlgorithm.Sha512256, false, 32)]
    [TestCase(SipDigestAlgorithm.Sha512256Sess, true, 32)]
    public void ReportsSessionModeAndDigestSize(
        SipDigestAlgorithm algorithm, bool session, int byteCount)
    {
        Assert.Multiple(() =>
        {
            Assert.That(SipDigestHash.UsesSessionKey(algorithm), Is.EqualTo(session));
            Assert.That(SipDigestHash.HashByteCount(algorithm), Is.EqualTo(byteCount));
        });
    }

    [Test]
    public void HashesEmptyInputWithCanonicalVectors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SipDigestHash.HashTextHex(SipDigestAlgorithm.Md5, ""),
                Is.EqualTo("d41d8cd98f00b204e9800998ecf8427e"));
            Assert.That(SipDigestHash.HashTextHex(SipDigestAlgorithm.Sha256, ""),
                Is.EqualTo(
                    "e3b0c44298fc1c149afbf4c8996fb924"
                    + "27ae41e4649b934ca495991b7852b855"));
            Assert.That(SipDigestHash.HashTextHex(
                    SipDigestAlgorithm.Sha512256, ""),
                Is.EqualTo(
                    "c672b8d1ef56ed28ab87c3622c511406"
                    + "9bdd3ad7b8f9737498d0c01ecef0967a"));
        });
    }

    [TestCase(111,
        "84d71ab5b507d9440277cc05cdde25e57e396e3a880ee24b0009fade06bc4cd7")]
    [TestCase(112,
        "ff0114959575adb2a8a59ddecba0e51855b385eb4a24017c923a0998062698f2")]
    [TestCase(127,
        "fceee8db128448e9d902b746eb7cc3fe08be09d0a08d7b7643d0a478a91a5568")]
    [TestCase(128,
        "aa399434cc7ac88f2a667659396d5e9659ccbdc53d8f0d0c9ccd78dbfd970921")]
    [TestCase(129,
        "345de9019055b4b8037c75bc5a9d282bc721f58c26506e28d3b4380cd9cc0ac0")]
    [TestCase(256,
        "4d0dc8009a94956aa5cd5c66672768695dd4e42adf7d0f5875e6446b4cfbcca2")]
    public void HashesSha512256AcrossPaddingAndBlockBoundaries(
        int length, string expected)
    {
        byte[] value = new byte[length];
        int index = 0;
        while (index < value.Length)
        {
            value[index] = (byte)((index * 37 + 11) & 255);
            index++;
        }

        string actual = SipDigestHash.HashHex(
            SipDigestAlgorithm.Sha512256, value);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ComparesBytesAndCanonicalHexWithoutNormalizingInput()
    {
        byte[] expectedBytes = SipDigestHash.HashText(
            SipDigestAlgorithm.Sha256, "expected");
        byte[] equalBytes = (byte[])expectedBytes.Clone();
        byte[] changedBytes = (byte[])expectedBytes.Clone();
        changedBytes[changedBytes.Length - 1] ^= 1;
        string expectedHex = SipDigestHash.HashTextHex(
            SipDigestAlgorithm.Sha256, "expected");
        string changedHex = expectedHex.Substring(0, expectedHex.Length - 1)
            + (expectedHex.EndsWith("0", StringComparison.Ordinal) ? "1" : "0");

        Assert.Multiple(() =>
        {
            Assert.That(SipDigestHash.FixedTimeEqualsBytes(
                expectedBytes, equalBytes), Is.True);
            Assert.That(SipDigestHash.FixedTimeEqualsBytes(
                expectedBytes, changedBytes), Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsBytes(
                expectedBytes, new byte[1]), Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsBytes(null, equalBytes), Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Sha256, expectedHex, expectedHex), Is.True);
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Sha256, expectedHex, changedHex), Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Sha256, expectedHex,
                expectedHex.ToUpperInvariant()), Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Md5, expectedHex, expectedHex), Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Sha256, expectedHex, null), Is.False);
        });
    }

    [Test]
    public void RejectsUnknownAlgorithmsAndNullHashInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SipDigestHash.HashBytes(SipDigestAlgorithm.Unknown, new byte[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SipDigestHash.HashByteCount(SipDigestAlgorithm.Unknown));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SipDigestHash.UsesSessionKey(SipDigestAlgorithm.Unknown));
            Assert.Throws<ArgumentNullException>(() =>
                SipDigestHash.HashBytes(SipDigestAlgorithm.Md5, null));
            Assert.Throws<ArgumentNullException>(() =>
                SipDigestHash.HashText(SipDigestAlgorithm.Sha256, null));
        });
    }
}
