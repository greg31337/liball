using argon2;

namespace argon2.Tests.Codecs;

public class Argon2PhcParserTests
{
    [TestCase("argon2d", "16", Argon2Variant.D, Argon2Version.Version10)]
    [TestCase("argon2d", "19", Argon2Variant.D, Argon2Version.Version13)]
    [TestCase("argon2i", "16", Argon2Variant.I, Argon2Version.Version10)]
    [TestCase("argon2i", "19", Argon2Variant.I, Argon2Version.Version13)]
    [TestCase("argon2id", "16", Argon2Variant.Id, Argon2Version.Version10)]
    [TestCase("argon2id", "19", Argon2Variant.Id, Argon2Version.Version13)]
    public void ParsesEveryVariantAndExplicitVersion(
        string variantText,
        string versionText,
        Argon2Variant expectedVariant,
        Argon2Version expectedVersion)
    {
        string encoded = "$" + variantText
            + "$v=" + versionText
            + "$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA";

        Argon2DecodeResult result = Argon2PhcParser.Parse(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.Success));
            Assert.That(result.EncodedHash, Is.Not.Null);
            Assert.That(result.EncodedHash?.Parameters.Variant, Is.EqualTo(expectedVariant));
            Assert.That(result.EncodedHash?.Parameters.Version, Is.EqualTo(expectedVersion));
            Assert.That(result.EncodedHash?.Parameters.MemorySizeKiB, Is.EqualTo(32L));
            Assert.That(result.EncodedHash?.Parameters.Iterations, Is.EqualTo(2L));
            Assert.That(result.EncodedHash?.Parameters.DegreeOfParallelism, Is.EqualTo(4));
            Assert.That(result.EncodedHash?.Salt, Is.EqualTo("somesalt"u8.ToArray()));
            Assert.That(result.EncodedHash?.Tag, Is.EqualTo("test"u8.ToArray()));
        });
    }

    [Test]
    public void OmittedVersionParsesAsLegacyVersion10()
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(
            "$argon2i$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.Success));
            Assert.That(result.EncodedHash?.Parameters.Variant, Is.EqualTo(Argon2Variant.I));
            Assert.That(result.EncodedHash?.Parameters.Version, Is.EqualTo(Argon2Version.Version10));
        });
    }

    [TestCase("$argon2id$v=019$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=08,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=01,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=01$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=+8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=-1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p= 1$c29tZXNhbHQ$dGVzdA")]
    public void RejectsNoncanonicalDecimalFields(string encoded)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.InvalidFormat));
            Assert.That(result.EncodedHash, Is.Null);
        });
    }

    [TestCase("$argon2x$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.UnsupportedVariant)]
    [TestCase("$argon2id$v=18$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.UnsupportedVersion)]
    [TestCase("$argon2id$v=999999999999999999999$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.UnsupportedVersion)]
    public void ReportsUnsupportedAlgorithmIdentifiers(
        string encoded,
        Argon2DecodeStatus expectedStatus)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(encoded);

        Assert.That(result.Status, Is.EqualTo(expectedStatus));
    }

    [TestCase("$argon2id$v=19$m=7,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=31,t=1,p=4$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=0,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=0$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=134217728,t=1,p=16777216$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=4294967296,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=4294967296,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c2FsdA$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$YWJj")]
    public void ReportsOutOfRangeParametersAndBinaryLengths(string encoded)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(encoded);

        Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.InvalidParameters));
    }

    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ=$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA==")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdB")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29t ZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdé")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$!!!!$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$!!!!")]
    public void ReportsInvalidBase64(string encoded)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(encoded);

        Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.InvalidBase64));
    }

    [TestCase("")]
    [TestCase("argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$t=1,m=8,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA$extra")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ")]
    public void RejectsInvalidStructureAndTrailingFields(string encoded)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(encoded);

        Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.InvalidFormat));
    }

    [Test]
    public void AcceptsMaximumRfcMemoryAndIterationValuesWithoutAllocatingWorkspace()
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(
            "$argon2d$v=19$m=4294967295,t=4294967295,p=1$c29tZXNhbHQ$dGVzdA");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.Success));
            Assert.That(result.EncodedHash?.Parameters.MemorySizeKiB, Is.EqualTo(4294967295L));
            Assert.That(result.EncodedHash?.Parameters.Iterations, Is.EqualTo(4294967295L));
        });
    }
}
