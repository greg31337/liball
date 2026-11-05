using argon2;

namespace argon2.Tests.Api;

public class Argon2EncodedHashCodecTests
{
    [TestCase(Argon2Variant.D, Argon2Version.Version10, "argon2d", "16")]
    [TestCase(Argon2Variant.D, Argon2Version.Version13, "argon2d", "19")]
    [TestCase(Argon2Variant.I, Argon2Version.Version10, "argon2i", "16")]
    [TestCase(Argon2Variant.I, Argon2Version.Version13, "argon2i", "19")]
    [TestCase(Argon2Variant.Id, Argon2Version.Version10, "argon2id", "16")]
    [TestCase(Argon2Variant.Id, Argon2Version.Version13, "argon2id", "19")]
    public void EncodeAndDecodeRoundTripEveryVariantAndVersion(
        Argon2Variant variant,
        Argon2Version version,
        string variantText,
        string versionText)
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        Argon2EncodedHash hash = CreateHash(variant, version);
        byte[] originalSalt = (byte[])hash.Salt.Clone();
        byte[] originalTag = (byte[])hash.Tag.Clone();
        string expected = "$" + variantText
            + "$v=" + versionText
            + "$m=37,t=2,p=4$c29tZXNhbHQ$dGVzdA";

        string encoded = codec.EncodeHash(hash);
        Argon2EncodedHash decoded = codec.DecodeHash(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(codec.GetEncodedHashLength(hash), Is.EqualTo(encoded.Length));
            Assert.That(decoded.Parameters.Variant, Is.EqualTo(variant));
            Assert.That(decoded.Parameters.Version, Is.EqualTo(version));
            Assert.That(decoded.Parameters.MemorySizeKiB, Is.EqualTo(37L));
            Assert.That(decoded.Parameters.Iterations, Is.EqualTo(2L));
            Assert.That(decoded.Parameters.DegreeOfParallelism, Is.EqualTo(4));
            Assert.That(decoded.Salt, Is.EqualTo(hash.Salt));
            Assert.That(decoded.Tag, Is.EqualTo(hash.Tag));
            Assert.That(decoded.Parameters, Is.Not.SameAs(hash.Parameters));
            Assert.That(decoded.Salt, Is.Not.SameAs(hash.Salt));
            Assert.That(decoded.Tag, Is.Not.SameAs(hash.Tag));
            Assert.That(hash.Salt, Is.EqualTo(originalSalt));
            Assert.That(hash.Tag, Is.EqualTo(originalTag));
        });
    }

    [Test]
    public void ExceptionlessDecodeReturnsDecodedMetadata()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();

        Argon2DecodeResult result = codec.DecodeHashWithoutException(
            "$argon2id$v=19$m=65536,t=3,p=4$c29tZXNhbHQ$/+7dzLs");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.Success));
            Assert.That(result.EncodedHash, Is.Not.Null);
            Assert.That(result.EncodedHash?.Parameters.Variant, Is.EqualTo(Argon2Variant.Id));
            Assert.That(result.EncodedHash?.Parameters.Version, Is.EqualTo(Argon2Version.Version13));
            Assert.That(result.EncodedHash?.Parameters.MemorySizeKiB, Is.EqualTo(65536L));
            Assert.That(result.EncodedHash?.Parameters.Iterations, Is.EqualTo(3L));
            Assert.That(result.EncodedHash?.Parameters.DegreeOfParallelism, Is.EqualTo(4));
            Assert.That(result.EncodedHash?.Salt, Is.EqualTo("somesalt"u8.ToArray()));
            Assert.That(result.EncodedHash?.Tag, Is.EqualTo(new byte[] { 0xFF, 0xEE, 0xDD, 0xCC, 0xBB }));
        });
    }

    [TestCase("argon2d", Argon2Variant.D)]
    [TestCase("argon2i", Argon2Variant.I)]
    [TestCase("argon2id", Argon2Variant.Id)]
    public void OmittedVersionDecodesAsLegacyAndReencodesExplicitly(
        string variantText,
        Argon2Variant expectedVariant)
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        string legacy = "$" + variantText + "$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA";

        Argon2EncodedHash decoded = codec.DecodeHash(legacy);
        Argon2DecodeResult result = codec.DecodeHashWithoutException(legacy);
        string canonical = codec.EncodeHash(decoded);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Parameters.Variant, Is.EqualTo(expectedVariant));
            Assert.That(decoded.Parameters.Version, Is.EqualTo(Argon2Version.Version10));
            Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.Success));
            Assert.That(result.EncodedHash?.Parameters.Version, Is.EqualTo(Argon2Version.Version10));
            Assert.That(
                canonical,
                Is.EqualTo("$" + variantText + "$v=16$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA"));
        });
    }

    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.Success)]
    [TestCase("invalid", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdB", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("$argon2id$v=19$m=7,t=1,p=1$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidParameters)]
    [TestCase("$argon2x$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.UnsupportedVariant)]
    [TestCase("$argon2id$v=18$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.UnsupportedVersion)]
    public void ExceptionlessDecodeReportsEveryStatus(
        string encoded,
        Argon2DecodeStatus expectedStatus)
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();

        Argon2DecodeResult result = codec.DecodeHashWithoutException(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            if (expectedStatus == Argon2DecodeStatus.Success)
            {
                Assert.That(result.EncodedHash, Is.Not.Null);
            }
            else
            {
                Assert.That(result.EncodedHash, Is.Null);
            }
        });
    }

    [Test]
    public void RepeatedDecodeCallsReturnIndependentDataGraphs()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        string encoded = "$argon2id$v=19$m=32,t=2,p=2$AAECAwQFBgc$/+7dzA";

        Argon2EncodedHash first = codec.DecodeHash(encoded);
        Argon2EncodedHash second = codec.DecodeHash(encoded);
        first.Parameters.MemorySizeKiB = 64L;
        first.Salt[0] = 0xFF;
        first.Tag[0] = 0x00;

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Parameters, Is.Not.SameAs(second.Parameters));
            Assert.That(first.Salt, Is.Not.SameAs(second.Salt));
            Assert.That(first.Tag, Is.Not.SameAs(second.Tag));
            Assert.That(second.Parameters.MemorySizeKiB, Is.EqualTo(32L));
            Assert.That(second.Salt, Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(second.Tag, Is.EqualTo(new byte[] { 0xFF, 0xEE, 0xDD, 0xCC }));
        });
    }

    [TestCase("invalid")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdB")]
    [TestCase("$argon2id$v=19$m=7,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2x$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=18$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    public void ThrowingDecodeRejectsEveryFailureCategory(string encoded)
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();

        Assert.Throws<Argon2FormatException>(() => codec.DecodeHash(encoded));
    }

    [Test]
    public void NullEncodedInputIsMalformedWithoutAnUnexpectedException()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();

        Argon2DecodeResult result = codec.DecodeHashWithoutException(null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.InvalidFormat));
            Assert.That(result.EncodedHash, Is.Null);
            Assert.Throws<Argon2FormatException>(() => codec.DecodeHash(null!));
        });
    }

    [Test]
    public void EncodeAndLengthRejectInvalidDecodedComponents()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        Argon2EncodedHash missingParameters = CreateHash(
            Argon2Variant.Id,
            Argon2Version.Version13);
        missingParameters.Parameters = null!;
        Argon2EncodedHash missingSalt = CreateHash(
            Argon2Variant.Id,
            Argon2Version.Version13);
        missingSalt.Salt = null!;
        Argon2EncodedHash missingTag = CreateHash(
            Argon2Variant.Id,
            Argon2Version.Version13);
        missingTag.Tag = null!;
        Argon2EncodedHash shortSalt = CreateHash(
            Argon2Variant.Id,
            Argon2Version.Version13);
        shortSalt.Salt = new byte[7];
        Argon2EncodedHash shortTag = CreateHash(
            Argon2Variant.Id,
            Argon2Version.Version13);
        shortTag.Tag = new byte[3];

        Assert.Multiple(() =>
        {
            AssertInvalidComponents(codec, null!);
            AssertInvalidComponents(codec, missingParameters);
            AssertInvalidComponents(codec, missingSalt);
            AssertInvalidComponents(codec, missingTag);
            AssertInvalidComponents(codec, shortSalt);
            AssertInvalidComponents(codec, shortTag);
        });
    }

    static void AssertInvalidComponents(
        IArgon2EncodedHashCodec codec,
        Argon2EncodedHash hash)
    {
        Assert.Throws<Argon2ParameterException>(() => codec.EncodeHash(hash));
        Assert.Throws<Argon2ParameterException>(() => codec.GetEncodedHashLength(hash));
    }

    static Argon2EncodedHash CreateHash(
        Argon2Variant variant,
        Argon2Version version)
    {
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters.Variant = variant;
        hash.Parameters.Version = version;
        hash.Parameters.MemorySizeKiB = 37L;
        hash.Parameters.Iterations = 2L;
        hash.Parameters.DegreeOfParallelism = 4;
        hash.Salt = "somesalt"u8.ToArray();
        hash.Tag = "test"u8.ToArray();
        return hash;
    }
}
