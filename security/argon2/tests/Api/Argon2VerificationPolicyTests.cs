using System.Text;
using argon2;

namespace argon2.Tests.Api;

public class Argon2VerificationPolicyTests
{
    [TestCase("encoded-length")]
    [TestCase("memory")]
    [TestCase("iterations")]
    [TestCase("parallelism")]
    [TestCase("salt-length")]
    [TestCase("tag-length")]
    public void EveryConfiguredMaximumRejectsBeforeHashComputation(string maximum)
    {
        Argon2PasswordHasherOptions options = CreatePolicyOptions(
            Argon2Variant.I,
            Argon2Version.Version10);
        long memorySizeKiB = 8L;
        long iterations = 1L;
        int degreeOfParallelism = 1;
        int saltLength = 8;
        int tagLength = 16;

        switch (maximum)
        {
            case "encoded-length":
                options.VerificationPolicy.MaximumEncodedHashLength = 60;
                options.VerificationPolicy.MaximumMemorySizeKiB = 16L;
                memorySizeKiB = 16L;
                break;
            case "memory":
                options.VerificationPolicy.MaximumMemorySizeKiB = 8L;
                memorySizeKiB = Argon2Limits.MaximumMemorySizeKiB;
                break;
            case "iterations":
                options.VerificationPolicy.MaximumIterations = 1L;
                iterations = Argon2Limits.MaximumIterations;
                break;
            case "parallelism":
                options.VerificationPolicy.MaximumDegreeOfParallelism = 1;
                memorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB
                    * Argon2Limits.MaximumDegreeOfParallelism;
                degreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism;
                break;
            case "salt-length":
                options.VerificationPolicy.MaximumSaltLength = 8;
                saltLength = 9;
                break;
            default:
                options.VerificationPolicy.MaximumTagLength = 16;
                tagLength = 17;
                break;
        }

        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(options);
        string encoded = CreatePolicyCandidate(
            Argon2Variant.I,
            Argon2Version.Version10,
            memorySizeKiB,
            iterations,
            degreeOfParallelism,
            saltLength,
            tagLength);

        Argon2VerificationResult result = hasher.VerifyPasswordBytes(
            ReadOnlySpan<byte>.Empty,
            encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2VerificationStatus.RejectedByPolicy));
            Assert.That(result.NeedsRehash, Is.False);
        });
    }

    [TestCase(Argon2Variant.D)]
    [TestCase(Argon2Variant.I)]
    [TestCase(Argon2Variant.Id)]
    public void EachDisabledVariantIsRejected(Argon2Variant candidateVariant)
    {
        Argon2Variant generationVariant = candidateVariant == Argon2Variant.D
            ? Argon2Variant.I
            : Argon2Variant.D;
        Argon2PasswordHasherOptions options = CreatePolicyOptions(
            generationVariant,
            Argon2Version.Version10);
        if (candidateVariant == Argon2Variant.D)
        {
            options.VerificationPolicy.AllowVariantD = false;
        }
        else if (candidateVariant == Argon2Variant.I)
        {
            options.VerificationPolicy.AllowVariantI = false;
        }
        else
        {
            options.VerificationPolicy.AllowVariantId = false;
        }

        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(options);
        string encoded = CreatePolicyCandidate(
            candidateVariant,
            Argon2Version.Version10,
            8L,
            1L,
            1,
            8,
            16);

        Argon2VerificationResult result = hasher.VerifyPasswordBytes(
            ReadOnlySpan<byte>.Empty,
            encoded);

        Assert.That(result.Status, Is.EqualTo(Argon2VerificationStatus.RejectedByPolicy));
    }

    [TestCase(Argon2Version.Version10)]
    [TestCase(Argon2Version.Version13)]
    public void EachDisabledVersionIsRejected(Argon2Version candidateVersion)
    {
        Argon2Version generationVersion = candidateVersion == Argon2Version.Version10
            ? Argon2Version.Version13
            : Argon2Version.Version10;
        Argon2PasswordHasherOptions options = CreatePolicyOptions(
            Argon2Variant.I,
            generationVersion);
        if (candidateVersion == Argon2Version.Version10)
        {
            options.VerificationPolicy.AllowVersion10 = false;
        }
        else
        {
            options.VerificationPolicy.AllowVersion13 = false;
        }

        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(options);
        string encoded = CreatePolicyCandidate(
            Argon2Variant.I,
            candidateVersion,
            8L,
            1L,
            1,
            8,
            16);

        Argon2VerificationResult result = hasher.VerifyPasswordBytes(
            ReadOnlySpan<byte>.Empty,
            encoded);

        Assert.That(result.Status, Is.EqualTo(Argon2VerificationStatus.RejectedByPolicy));
    }

    [Test]
    public void ConstructorRequiresPolicyToAcceptGeneratedEncodedLength()
    {
        Argon2PasswordHasherOptions options = CreatePolicyOptions(
            Argon2Variant.I,
            Argon2Version.Version10);
        options.VerificationPolicy.MaximumEncodedHashLength = 59;

        Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(options));

        options.VerificationPolicy.MaximumEncodedHashLength = 60;
        Assert.DoesNotThrow(() => new Argon2PasswordHasher(options));
    }

    [Test]
    public void ExactPolicyBoundariesAreAcceptedAndComputed()
    {
        byte[] password = new byte[] { 0x00, 0x01, 0xFE, 0xFF };
        byte[] originalPassword = (byte[])password.Clone();
        string encoded = CreateMatchingPolicyCandidate(
            password,
            Argon2Variant.D,
            Argon2Version.Version13,
            16L,
            2L,
            2,
            9,
            17);
        Argon2PasswordHasherOptions options = CreatePolicyOptions(
            Argon2Variant.I,
            Argon2Version.Version10);
        options.VerificationPolicy.MaximumEncodedHashLength = encoded.Length;
        options.VerificationPolicy.MaximumMemorySizeKiB = 16L;
        options.VerificationPolicy.MaximumIterations = 2L;
        options.VerificationPolicy.MaximumDegreeOfParallelism = 2;
        options.VerificationPolicy.MaximumSaltLength = 9;
        options.VerificationPolicy.MaximumTagLength = 17;
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(options);
        Argon2EncodedHash decoded = new Argon2EncodedHashCodec().DecodeHash(encoded);

        Argon2VerificationResult result = hasher.VerifyPasswordBytes(password, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(
                encoded.Length,
                Is.EqualTo(options.VerificationPolicy.MaximumEncodedHashLength));
            Assert.That(
                decoded.Parameters.MemorySizeKiB,
                Is.EqualTo(options.VerificationPolicy.MaximumMemorySizeKiB));
            Assert.That(
                decoded.Parameters.Iterations,
                Is.EqualTo(options.VerificationPolicy.MaximumIterations));
            Assert.That(
                decoded.Parameters.DegreeOfParallelism,
                Is.EqualTo(options.VerificationPolicy.MaximumDegreeOfParallelism));
            Assert.That(
                decoded.Salt.Length,
                Is.EqualTo(options.VerificationPolicy.MaximumSaltLength));
            Assert.That(
                decoded.Tag.Length,
                Is.EqualTo(options.VerificationPolicy.MaximumTagLength));
            Assert.That(result.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(result.NeedsRehash, Is.True);
            Assert.That(password, Is.EqualTo(originalPassword));
        });
    }

    [Test]
    public void DefaultPolicyAcceptsHashesGeneratedByTheSameHasher()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(new Argon2PasswordHasherOptions());
        byte[] password = Encoding.UTF8.GetBytes("default-policy-round-trip");

        string encoded = hasher.HashPasswordBytes(password);
        Argon2VerificationResult result = hasher.VerifyPasswordBytes(password, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(result.NeedsRehash, Is.False);
        });
    }

    static Argon2PasswordHasherOptions CreatePolicyOptions(
        Argon2Variant generationVariant,
        Argon2Version generationVersion)
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters.Variant = generationVariant;
        options.Parameters.Version = generationVersion;
        options.Parameters.MemorySizeKiB = 8L;
        options.Parameters.Iterations = 1L;
        options.Parameters.DegreeOfParallelism = 1;
        options.SaltLength = 8;
        options.TagLength = 16;
        options.ExecutionOptions.MaximumWorkerCount = 1;
        options.VerificationPolicy.MaximumEncodedHashLength = 4096;
        options.VerificationPolicy.MaximumMemorySizeKiB = Argon2Limits.MaximumMemorySizeKiB;
        options.VerificationPolicy.MaximumIterations = Argon2Limits.MaximumIterations;
        options.VerificationPolicy.MaximumDegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism;
        options.VerificationPolicy.MaximumSaltLength = 64;
        options.VerificationPolicy.MaximumTagLength = 64;
        return options;
    }

    static string CreatePolicyCandidate(
        Argon2Variant variant,
        Argon2Version version,
        long memorySizeKiB,
        long iterations,
        int degreeOfParallelism,
        int saltLength,
        int tagLength)
    {
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters.Variant = variant;
        hash.Parameters.Version = version;
        hash.Parameters.MemorySizeKiB = memorySizeKiB;
        hash.Parameters.Iterations = iterations;
        hash.Parameters.DegreeOfParallelism = degreeOfParallelism;
        hash.Salt = new byte[saltLength];
        hash.Tag = new byte[tagLength];
        return new Argon2EncodedHashCodec().EncodeHash(hash);
    }

    static string CreateMatchingPolicyCandidate(
        byte[] password,
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
        executionOptions.MaximumWorkerCount = 1;
        byte[] salt = new byte[saltLength];
        int index = 0;
        while (index < salt.Length)
        {
            salt[index] = (byte)(index + 1);
            index = index + 1;
        }

        byte[] tag = new byte[tagLength];
        IArgon2Primitive primitive = new Argon2Primitive();
        primitive.ComputeHash(
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
