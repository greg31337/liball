using System.Text;
using argon2;

namespace argon2.Tests.Interop;

public class Argon2CrossLibraryInteroperabilityTests
{
    // Generated with P-H-C/phc-winner-argon2 commit
    // f57e61e19229e23c4445b85494dbf7c07de721cb using its CLI.
    [TestCase(
        Argon2Variant.D,
        Argon2Version.Version10,
        "99d187bdc39abf808700b4a975dac6f6f6",
        "$argon2d$v=16$m=37,t=2,p=1$bGliczExc2FsdA$mdGHvcOav4CHALSpddrG9vY")]
    [TestCase(
        Argon2Variant.D,
        Argon2Version.Version13,
        "d82c7b1027c967fbd9130462b2b70c0b48",
        "$argon2d$v=19$m=37,t=2,p=1$bGliczExc2FsdA$2Cx7ECfJZ/vZEwRisrcMC0g")]
    [TestCase(
        Argon2Variant.I,
        Argon2Version.Version10,
        "4a9a9d746f84ffd56687a78a6ebacaf5a9",
        "$argon2i$v=16$m=37,t=2,p=1$bGliczExc2FsdA$SpqddG+E/9Vmh6eKbrrK9ak")]
    [TestCase(
        Argon2Variant.I,
        Argon2Version.Version13,
        "327465c4300dffc9a64ede8f453cbdf864",
        "$argon2i$v=19$m=37,t=2,p=1$bGliczExc2FsdA$MnRlxDAN/8mmTt6PRTy9+GQ")]
    [TestCase(
        Argon2Variant.Id,
        Argon2Version.Version10,
        "c154378f609059433c3a2876089b9f8b56",
        "$argon2id$v=16$m=37,t=2,p=1$bGliczExc2FsdA$wVQ3j2CQWUM8Oih2CJufi1Y")]
    [TestCase(
        Argon2Variant.Id,
        Argon2Version.Version13,
        "7c05b61964c86d42b17c2aabd5d2a190c8",
        "$argon2id$v=19$m=37,t=2,p=1$bGliczExc2FsdA$fAW2GWTIbUKxfCqr1dKhkMg")]
    public void PinnedLibargon2FixturesInteroperate(
        Argon2Variant variant,
        Argon2Version version,
        string expectedRawTag,
        string expectedEncodedHash)
    {
        AssertFixture(
            Encoding.UTF8.GetBytes("libargon2-pass"),
            Encoding.UTF8.GetBytes("libs11salt"),
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            variant,
            version,
            37L,
            2L,
            1,
            17,
            expectedRawTag,
            expectedEncodedHash);
    }

    // Generated with argon2-cffi 23.1.0 low_level.hash_secret_raw and hash_secret.
    [TestCase(
        Argon2Variant.D,
        Argon2Version.Version10,
        "b0406533442f74e13d4e63d71259cbf8015a9538af7c5e",
        "$argon2d$v=16$m=43,t=3,p=2$Y2ZmaS1zYWx0LTAx$sEBlM0QvdOE9TmPXElnL+AFalTivfF4")]
    [TestCase(
        Argon2Variant.D,
        Argon2Version.Version13,
        "d15abd561016135cddb3f3f02a9fb5d72dbd2ff6b40d31",
        "$argon2d$v=19$m=43,t=3,p=2$Y2ZmaS1zYWx0LTAx$0Vq9VhAWE1zds/PwKp+11y29L/a0DTE")]
    [TestCase(
        Argon2Variant.I,
        Argon2Version.Version10,
        "74707561dce4327baed3099573d041514b56ba87d008fb",
        "$argon2i$v=16$m=43,t=3,p=2$Y2ZmaS1zYWx0LTAx$dHB1YdzkMnuu0wmVc9BBUUtWuofQCPs")]
    [TestCase(
        Argon2Variant.I,
        Argon2Version.Version13,
        "9f37e7bbd714bdfacf581bcf314d23fc180e5b52364ec7",
        "$argon2i$v=19$m=43,t=3,p=2$Y2ZmaS1zYWx0LTAx$nzfnu9cUvfrPWBvPMU0j/BgOW1I2Tsc")]
    [TestCase(
        Argon2Variant.Id,
        Argon2Version.Version10,
        "14c8e008109b8617ecbd83a17c373dfc91dd7affbe1273",
        "$argon2id$v=16$m=43,t=3,p=2$Y2ZmaS1zYWx0LTAx$FMjgCBCbhhfsvYOhfDc9/JHdev++EnM")]
    [TestCase(
        Argon2Variant.Id,
        Argon2Version.Version13,
        "5fa0ea9062a48dcec5902479e6586481dfd9f63ed9fe45",
        "$argon2id$v=19$m=43,t=3,p=2$Y2ZmaS1zYWx0LTAx$X6DqkGKkjc7FkCR55lhkgd/Z9j7Z/kU")]
    public void PinnedArgon2CffiFixturesInteroperate(
        Argon2Variant variant,
        Argon2Version version,
        string expectedRawTag,
        string expectedEncodedHash)
    {
        AssertFixture(
            Encoding.UTF8.GetBytes("argon2-cffi-pass"),
            Encoding.UTF8.GetBytes("cffi-salt-01"),
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            variant,
            version,
            43L,
            3L,
            2,
            23,
            expectedRawTag,
            expectedEncodedHash);
    }

    // Generated with argon2-jvm 2.12 Argon2Advanced.rawHashAdvanced.
    [TestCase(
        Argon2Variant.D,
        Argon2Version.Version10,
        "b9974d96d2026a9e5d73f2ea873bd0d276dd66",
        "$argon2d$v=16$m=52,t=2,p=2$anZtLXNhbHQtMDAwMQ$uZdNltICap5dc/LqhzvQ0nbdZg")]
    [TestCase(
        Argon2Variant.D,
        Argon2Version.Version13,
        "816e872cb6dd0666307346df8291957d3706b3",
        "$argon2d$v=19$m=52,t=2,p=2$anZtLXNhbHQtMDAwMQ$gW6HLLbdBmYwc0bfgpGVfTcGsw")]
    [TestCase(
        Argon2Variant.I,
        Argon2Version.Version10,
        "af645c2b4a53be29e1f920166cb474aab7a365",
        "$argon2i$v=16$m=52,t=2,p=2$anZtLXNhbHQtMDAwMQ$r2RcK0pTvinh+SAWbLR0qrejZQ")]
    [TestCase(
        Argon2Variant.I,
        Argon2Version.Version13,
        "f2dd49bbde2f865c94ecfbcfbac27cdca23bd8",
        "$argon2i$v=19$m=52,t=2,p=2$anZtLXNhbHQtMDAwMQ$8t1Ju94vhlyU7PvPusJ83KI72A")]
    [TestCase(
        Argon2Variant.Id,
        Argon2Version.Version10,
        "c177f6db28bdc3a25c4bfdb1617a28db065056",
        "$argon2id$v=16$m=52,t=2,p=2$anZtLXNhbHQtMDAwMQ$wXf22yi9w6JcS/2xYXoo2wZQVg")]
    [TestCase(
        Argon2Variant.Id,
        Argon2Version.Version13,
        "cb4704c2a0c6c6b97f427cd63407939509b39e",
        "$argon2id$v=19$m=52,t=2,p=2$anZtLXNhbHQtMDAwMQ$y0cEwqDGxrl/QnzWNAeTlQmzng")]
    public void PinnedArgon2JvmFixturesInteroperate(
        Argon2Variant variant,
        Argon2Version version,
        string expectedRawTag,
        string expectedEncodedHash)
    {
        AssertFixture(
            Encoding.UTF8.GetBytes("argon2-jvm-pass"),
            Encoding.UTF8.GetBytes("jvm-salt-0001"),
            new byte[] { 3, 4, 5 },
            new byte[] { 6, 7, 8, 9 },
            variant,
            version,
            52L,
            2L,
            2,
            19,
            expectedRawTag,
            expectedEncodedHash);
    }

    // Generated with Konscious.Security.Cryptography.Argon2 1.3.1 GetBytes.
    // This library implements version 1.3 and does not expose version selection.
    [TestCase(
        Argon2Variant.D,
        "a42cb8bc805e03c83888b1825dc2929e39887723a2",
        "$argon2d$v=19$m=45,t=3,p=2$a29uc2Npb3VzLXNhbHQ$pCy4vIBeA8g4iLGCXcKSnjmIdyOi")]
    [TestCase(
        Argon2Variant.I,
        "343e921774f14a5f63584303d4938fce4f7daad026",
        "$argon2i$v=19$m=45,t=3,p=2$a29uc2Npb3VzLXNhbHQ$ND6SF3TxSl9jWEMD1JOPzk99qtAm")]
    [TestCase(
        Argon2Variant.Id,
        "e0578f8da93f77e712ecbcb08b1e1e8db5337dbd67",
        "$argon2id$v=19$m=45,t=3,p=2$a29uc2Npb3VzLXNhbHQ$4FePjak/d+cS7Lywix4ejbUzfb1n")]
    public void PinnedKonsciousFixturesInteroperate(
        Argon2Variant variant,
        string expectedRawTag,
        string expectedEncodedHash)
    {
        AssertFixture(
            Encoding.UTF8.GetBytes("konscious-pass"),
            Encoding.UTF8.GetBytes("konscious-salt"),
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            variant,
            Argon2Version.Version13,
            45L,
            3L,
            2,
            21,
            expectedRawTag,
            expectedEncodedHash);
    }

    static void AssertFixture(
        byte[] password,
        byte[] salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Variant variant,
        Argon2Version version,
        long memorySizeKiB,
        long iterations,
        int degreeOfParallelism,
        int tagLength,
        string expectedRawTag,
        string expectedEncodedHash)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = version;
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.Iterations = iterations;
        parameters.DegreeOfParallelism = degreeOfParallelism;
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = degreeOfParallelism;
        byte[] tag = new byte[tagLength];
        IArgon2Primitive primitive = new Argon2Primitive();
        primitive.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            tag);

        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters = parameters;
        hash.Salt = salt;
        hash.Tag = tag;
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        string encodedHash = codec.EncodeHash(hash);
        Argon2EncodedHash decoded = codec.DecodeHash(expectedEncodedHash);

        Argon2PasswordHasherOptions hasherOptions = new Argon2PasswordHasherOptions();
        hasherOptions.Parameters = parameters;
        hasherOptions.SaltLength = salt.Length;
        hasherOptions.TagLength = tagLength;
        hasherOptions.ExecutionOptions.MaximumWorkerCount = degreeOfParallelism;
        hasherOptions.VerificationPolicy.MaximumMemorySizeKiB = memorySizeKiB;
        hasherOptions.VerificationPolicy.MaximumIterations = iterations;
        hasherOptions.VerificationPolicy.MaximumDegreeOfParallelism = degreeOfParallelism;
        hasherOptions.VerificationPolicy.MaximumSaltLength = salt.Length;
        hasherOptions.VerificationPolicy.MaximumTagLength = tagLength;
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(hasherOptions);
        Argon2VerificationResult verification = hasher.VerifyPasswordBytesWithContext(
            password,
            expectedEncodedHash,
            secret,
            associatedData);

        Assert.Multiple(() =>
        {
            Assert.That(
                Convert.ToHexString(tag).ToLowerInvariant(),
                Is.EqualTo(expectedRawTag));
            Assert.That(encodedHash, Is.EqualTo(expectedEncodedHash));
            Assert.That(decoded.Parameters.Variant, Is.EqualTo(variant));
            Assert.That(decoded.Parameters.Version, Is.EqualTo(version));
            Assert.That(decoded.Parameters.MemorySizeKiB, Is.EqualTo(memorySizeKiB));
            Assert.That(decoded.Parameters.Iterations, Is.EqualTo(iterations));
            Assert.That(
                decoded.Parameters.DegreeOfParallelism,
                Is.EqualTo(degreeOfParallelism));
            Assert.That(decoded.Salt, Is.EqualTo(salt));
            Assert.That(decoded.Tag, Is.EqualTo(tag));
            Assert.That(verification.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(verification.NeedsRehash, Is.False);
        });
    }
}
