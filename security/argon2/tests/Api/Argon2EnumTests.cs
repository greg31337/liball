using argon2;

namespace argon2.Tests.Api;

public class Argon2EnumTests
{
    [Test]
    public void VariantValuesMatchArgon2TypeCodes()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)Argon2Variant.D, Is.EqualTo(0));
            Assert.That((int)Argon2Variant.I, Is.EqualTo(1));
            Assert.That((int)Argon2Variant.Id, Is.EqualTo(2));
        });
    }

    [Test]
    public void VersionValuesMatchEncodedVersionNumbers()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)Argon2Version.Version10, Is.EqualTo(0x10));
            Assert.That((int)Argon2Version.Version13, Is.EqualTo(0x13));
        });
    }

    [Test]
    public void DecodeStatusValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)Argon2DecodeStatus.Success, Is.EqualTo(0));
            Assert.That((int)Argon2DecodeStatus.InvalidFormat, Is.EqualTo(1));
            Assert.That((int)Argon2DecodeStatus.InvalidBase64, Is.EqualTo(2));
            Assert.That((int)Argon2DecodeStatus.InvalidParameters, Is.EqualTo(3));
            Assert.That((int)Argon2DecodeStatus.UnsupportedVariant, Is.EqualTo(4));
            Assert.That((int)Argon2DecodeStatus.UnsupportedVersion, Is.EqualTo(5));
        });
    }

    [Test]
    public void VerificationStatusValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)Argon2VerificationStatus.Match, Is.EqualTo(0));
            Assert.That((int)Argon2VerificationStatus.Mismatch, Is.EqualTo(1));
            Assert.That((int)Argon2VerificationStatus.InvalidHash, Is.EqualTo(2));
            Assert.That((int)Argon2VerificationStatus.RejectedByPolicy, Is.EqualTo(3));
        });
    }

    [Test]
    public void PublicDataModelsRetainEnumValues()
    {
        Argon2Parameters parameters = new Argon2Parameters();
        Argon2DecodeResult decodeResult = new Argon2DecodeResult();
        Argon2VerificationResult verificationResult = new Argon2VerificationResult();
        parameters.Variant = Argon2Variant.I;
        parameters.Version = Argon2Version.Version10;
        decodeResult.Status = Argon2DecodeStatus.InvalidBase64;
        verificationResult.Status = Argon2VerificationStatus.Match;
        verificationResult.NeedsRehash = true;

        Assert.Multiple(() =>
        {
            Assert.That(parameters.Variant, Is.EqualTo(Argon2Variant.I));
            Assert.That(parameters.Version, Is.EqualTo(Argon2Version.Version10));
            Assert.That(decodeResult.Status, Is.EqualTo(Argon2DecodeStatus.InvalidBase64));
            Assert.That(verificationResult.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(verificationResult.NeedsRehash, Is.True);
        });
    }
}
