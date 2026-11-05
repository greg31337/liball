using argon2;

namespace argon2.Tests.Codecs;

public class Argon2PhcFormatterTests
{
    [TestCase(Argon2Variant.D, Argon2Version.Version10, "argon2d", "16")]
    [TestCase(Argon2Variant.D, Argon2Version.Version13, "argon2d", "19")]
    [TestCase(Argon2Variant.I, Argon2Version.Version10, "argon2i", "16")]
    [TestCase(Argon2Variant.I, Argon2Version.Version13, "argon2i", "19")]
    [TestCase(Argon2Variant.Id, Argon2Version.Version10, "argon2id", "16")]
    [TestCase(Argon2Variant.Id, Argon2Version.Version13, "argon2id", "19")]
    public void FormatsEveryVariantAndVersionCanonically(
        Argon2Variant variant,
        Argon2Version version,
        string variantText,
        string versionText)
    {
        Argon2EncodedHash hash = CreateHash(variant, version, 32L, 2L, 4);
        string expected = "$" + variantText
            + "$v=" + versionText
            + "$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA";

        string encoded = Argon2PhcFormatter.Format(hash);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(Argon2PhcFormatter.GetEncodedLength(hash), Is.EqualTo(expected.Length));
        });
    }

    [Test]
    public void PreservesOriginalRequestedCostsAndBinaryValues()
    {
        Argon2EncodedHash hash = CreateHash(
            Argon2Variant.Id,
            Argon2Version.Version13,
            37L,
            11L,
            4);
        hash.Salt = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        hash.Tag = new byte[] { 0xFF, 0xEE, 0xDD, 0xCC, 0xBB };

        string encoded = Argon2PhcFormatter.Format(hash);
        Argon2DecodeResult decoded = Argon2PhcParser.Parse(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo("$argon2id$v=19$m=37,t=11,p=4$AAECAwQFBgc$/+7dzLs"));
            Assert.That(Argon2PhcFormatter.GetEncodedLength(hash), Is.EqualTo(encoded.Length));
            Assert.That(decoded.Status, Is.EqualTo(Argon2DecodeStatus.Success));
            Assert.That(decoded.EncodedHash?.Parameters.MemorySizeKiB, Is.EqualTo(37L));
            Assert.That(decoded.EncodedHash?.Salt, Is.EqualTo(hash.Salt));
            Assert.That(decoded.EncodedHash?.Tag, Is.EqualTo(hash.Tag));
        });
    }

    [Test]
    public void LengthCalculationHandlesMaximumCanonicalDecimalFields()
    {
        Argon2EncodedHash hash = CreateHash(
            Argon2Variant.D,
            Argon2Version.Version13,
            4294967295L,
            4294967295L,
            16777215);

        string encoded = Argon2PhcFormatter.Format(hash);

        Assert.Multiple(() =>
        {
            Assert.That(
                encoded,
                Is.EqualTo("$argon2d$v=19$m=4294967295,t=4294967295,p=16777215$c29tZXNhbHQ$dGVzdA"));
            Assert.That(Argon2PhcFormatter.GetEncodedLength(hash), Is.EqualTo(encoded.Length));
        });
    }

    [TestCase(8, 4)]
    [TestCase(9, 5)]
    [TestCase(10, 6)]
    [TestCase(11, 7)]
    public void ExactLengthCoversEveryBase64Remainder(int saltLength, int tagLength)
    {
        Argon2EncodedHash hash = CreateHash(
            Argon2Variant.I,
            Argon2Version.Version10,
            8L,
            1L,
            1);
        hash.Salt = new byte[saltLength];
        hash.Tag = new byte[tagLength];

        string encoded = Argon2PhcFormatter.Format(hash);

        Assert.That(Argon2PhcFormatter.GetEncodedLength(hash), Is.EqualTo(encoded.Length));
    }

    [Test]
    public void RejectsInvalidDecodedHashComponents()
    {
        Argon2EncodedHash invalidVariant = CreateHash(
            (Argon2Variant)99,
            Argon2Version.Version13,
            8L,
            1L,
            1);
        Argon2EncodedHash shortSalt = CreateHash(
            Argon2Variant.Id,
            Argon2Version.Version13,
            8L,
            1L,
            1);
        shortSalt.Salt = new byte[7];
        Argon2EncodedHash shortTag = CreateHash(
            Argon2Variant.Id,
            Argon2Version.Version13,
            8L,
            1L,
            1);
        shortTag.Tag = new byte[3];

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => Argon2PhcFormatter.Format(invalidVariant));
            Assert.Throws<Argon2ParameterException>(() => Argon2PhcFormatter.GetEncodedLength(invalidVariant));
            Assert.Throws<Argon2ParameterException>(() => Argon2PhcFormatter.Format(shortSalt));
            Assert.Throws<Argon2ParameterException>(() => Argon2PhcFormatter.Format(shortTag));
        });
    }

    static Argon2EncodedHash CreateHash(
        Argon2Variant variant,
        Argon2Version version,
        long memorySizeKiB,
        long iterations,
        int degreeOfParallelism)
    {
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters.Variant = variant;
        hash.Parameters.Version = version;
        hash.Parameters.MemorySizeKiB = memorySizeKiB;
        hash.Parameters.Iterations = iterations;
        hash.Parameters.DegreeOfParallelism = degreeOfParallelism;
        hash.Salt = "somesalt"u8.ToArray();
        hash.Tag = "test"u8.ToArray();
        return hash;
    }
}
