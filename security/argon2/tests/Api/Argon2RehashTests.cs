using argon2;

namespace argon2.Tests.Api;

public class Argon2RehashTests
{
    [Test]
    public void ExactGenerationConfigurationDoesNotNeedRehash()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateRehashOptions());
        byte[] password = new byte[] { 1, 2, 3, 4, 5 };

        string encoded = hasher.HashPasswordBytes(password);
        bool needsRehash = hasher.NeedsRehash(encoded);
        Argon2VerificationResult verification = hasher.VerifyPasswordBytes(password, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(needsRehash, Is.False);
            Assert.That(verification.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(verification.NeedsRehash, Is.False);
        });
    }

    [TestCase("variant")]
    [TestCase("version")]
    [TestCase("memory")]
    [TestCase("iterations")]
    [TestCase("parallelism")]
    [TestCase("salt-length")]
    [TestCase("tag-length")]
    public void EveryConfigurationDifferenceNeedsRehashAfterSuccessfulVerification(
        string difference)
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateRehashOptions());
        byte[] password = new byte[] { 9, 8, 7, 6 };
        Argon2Variant variant = Argon2Variant.I;
        Argon2Version version = Argon2Version.Version10;
        long memorySizeKiB = 32L;
        long iterations = 2L;
        int degreeOfParallelism = 2;
        int saltLength = 8;
        int tagLength = 16;

        switch (difference)
        {
            case "variant":
                variant = Argon2Variant.D;
                break;
            case "version":
                version = Argon2Version.Version13;
                break;
            case "memory":
                memorySizeKiB = 36L;
                break;
            case "iterations":
                iterations = 3L;
                break;
            case "parallelism":
                degreeOfParallelism = 1;
                break;
            case "salt-length":
                saltLength = 9;
                break;
            default:
                tagLength = 17;
                break;
        }

        string encoded = CreateRehashCandidate(
            password,
            variant,
            version,
            memorySizeKiB,
            iterations,
            degreeOfParallelism,
            saltLength,
            tagLength);
        bool needsRehash = hasher.NeedsRehash(encoded);
        Argon2VerificationResult verification = hasher.VerifyPasswordBytes(password, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(needsRehash, Is.True);
            Assert.That(verification.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(verification.NeedsRehash, Is.True);
        });
    }

    [Test]
    public void AllConfigurationDifferencesTogetherNeedRehashAfterSuccessfulVerification()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateRehashOptions());
        byte[] password = new byte[] { 4, 3, 2, 1 };
        string encoded = CreateRehashCandidate(
            password,
            Argon2Variant.D,
            Argon2Version.Version13,
            36L,
            3L,
            1,
            9,
            17);

        Argon2EncodedHash decoded = new Argon2EncodedHashCodec().DecodeHash(encoded);
        bool needsRehash = hasher.NeedsRehash(encoded);
        Argon2VerificationResult verification = hasher.VerifyPasswordBytes(password, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Parameters.Variant, Is.EqualTo(Argon2Variant.D));
            Assert.That(decoded.Parameters.Version, Is.EqualTo(Argon2Version.Version13));
            Assert.That(decoded.Parameters.MemorySizeKiB, Is.EqualTo(36L));
            Assert.That(decoded.Parameters.Iterations, Is.EqualTo(3L));
            Assert.That(decoded.Parameters.DegreeOfParallelism, Is.EqualTo(1));
            Assert.That(decoded.Salt.Length, Is.EqualTo(9));
            Assert.That(decoded.Tag.Length, Is.EqualTo(17));
            Assert.That(needsRehash, Is.True);
            Assert.That(verification.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(verification.NeedsRehash, Is.True);
        });
    }

    [Test]
    public void MismatchNeverReportsNeedsRehash()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateRehashOptions());
        string encoded = CreateRehashCandidate(
            new byte[] { 1, 2, 3 },
            Argon2Variant.D,
            Argon2Version.Version13,
            36L,
            3L,
            1,
            9,
            17);

        Argon2VerificationResult verification = hasher.VerifyPasswordBytes(
            new byte[] { 1, 2, 4 },
            encoded);

        Assert.Multiple(() =>
        {
            Assert.That(verification.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(verification.NeedsRehash, Is.False);
        });
    }

    [Test]
    public void OmittedLegacyVersionMatchesAnExactVersion10Configuration()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateRehashOptions());
        byte[] password = new byte[] { 5, 4, 3, 2, 1 };
        string explicitVersion = CreateRehashCandidate(
            password,
            Argon2Variant.I,
            Argon2Version.Version10,
            32L,
            2L,
            2,
            8,
            16);
        string omittedVersion = explicitVersion.Replace("$v=16", "");

        bool needsRehash = hasher.NeedsRehash(omittedVersion);
        Argon2VerificationResult verification = hasher.VerifyPasswordBytes(password, omittedVersion);

        Assert.Multiple(() =>
        {
            Assert.That(needsRehash, Is.False);
            Assert.That(verification.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(verification.NeedsRehash, Is.False);
        });
    }

    [Test]
    public void StandaloneRehashDetectionRejectsMalformedOrUnsupportedHashes()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateRehashOptions());

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2FormatException>(() => hasher.NeedsRehash(null!));
            Assert.Throws<Argon2FormatException>(() => hasher.NeedsRehash("invalid"));
            Assert.Throws<Argon2FormatException>(
                () => hasher.NeedsRehash(
                    "$argon2x$v=19$m=32,t=2,p=2$c29tZXNhbHQ$dGVzdA"));
        });
    }

    static Argon2PasswordHasherOptions CreateRehashOptions()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters.Variant = Argon2Variant.I;
        options.Parameters.Version = Argon2Version.Version10;
        options.Parameters.MemorySizeKiB = 32L;
        options.Parameters.Iterations = 2L;
        options.Parameters.DegreeOfParallelism = 2;
        options.SaltLength = 8;
        options.TagLength = 16;
        options.ExecutionOptions.MaximumWorkerCount = 2;
        options.VerificationPolicy.MaximumMemorySizeKiB = 64L;
        options.VerificationPolicy.MaximumIterations = 4L;
        options.VerificationPolicy.MaximumDegreeOfParallelism = 4;
        options.VerificationPolicy.MaximumSaltLength = 32;
        options.VerificationPolicy.MaximumTagLength = 32;
        return options;
    }

    static string CreateRehashCandidate(
        ReadOnlySpan<byte> password,
        Argon2Variant variant,
        Argon2Version version,
        long memorySizeKiB,
        long iterations,
        int degreeOfParallelism,
        int saltLength,
        int tagLength)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = version;
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.Iterations = iterations;
        parameters.DegreeOfParallelism = degreeOfParallelism;
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = degreeOfParallelism < 2
            ? degreeOfParallelism
            : 2;
        byte[] salt = new byte[saltLength];
        int saltIndex = 0;
        while (saltIndex < salt.Length)
        {
            salt[saltIndex] = (byte)(saltIndex + 1);
            saltIndex = saltIndex + 1;
        }

        byte[] tag = new byte[tagLength];
        new Argon2Primitive().ComputeHash(
            password,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            tag);
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters = parameters;
        hash.Salt = salt;
        hash.Tag = tag;
        return new Argon2EncodedHashCodec().EncodeHash(hash);
    }
}
