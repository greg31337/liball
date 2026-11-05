using argon2;

namespace argon2.Tests.Api;

public class Argon2PublicParameterValidationTests
{
    [Test]
    public void EncodedLengthAcceptsEveryRfcNumericBoundaryWithoutAllocatingAWorkspace()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        Argon2EncodedHash minimum = CreateMinimumHash();
        Argon2EncodedHash maximumIterations = CreateMinimumHash();
        maximumIterations.Parameters.Iterations = Argon2Limits.MaximumIterations;
        Argon2EncodedHash maximumMemory = CreateMinimumHash();
        maximumMemory.Parameters.MemorySizeKiB = Argon2Limits.MaximumMemorySizeKiB;
        Argon2EncodedHash maximumParallelism = CreateMinimumHash();
        maximumParallelism.Parameters.DegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism;
        maximumParallelism.Parameters.MemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB
            * Argon2Limits.MaximumDegreeOfParallelism;

        Assert.Multiple(() =>
        {
            AssertEncodingSucceeds(codec, minimum);
            AssertEncodingSucceeds(codec, maximumIterations);
            AssertEncodingSucceeds(codec, maximumMemory);
            AssertEncodingSucceeds(codec, maximumParallelism);
        });
    }

    [Test]
    public void EncodedLengthRejectsEveryJustOutsideNumericBoundaryAndInvalidEnum()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        Argon2EncodedHash iterationsBelow = CreateMinimumHash();
        iterationsBelow.Parameters.Iterations = Argon2Limits.MinimumIterations - 1L;
        Argon2EncodedHash iterationsAbove = CreateMinimumHash();
        iterationsAbove.Parameters.Iterations = Argon2Limits.MaximumIterations + 1L;
        Argon2EncodedHash memoryBelow = CreateMinimumHash();
        memoryBelow.Parameters.MemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB - 1L;
        Argon2EncodedHash memoryAbove = CreateMinimumHash();
        memoryAbove.Parameters.MemorySizeKiB = Argon2Limits.MaximumMemorySizeKiB + 1L;
        Argon2EncodedHash parallelismBelow = CreateMinimumHash();
        parallelismBelow.Parameters.DegreeOfParallelism = Argon2Limits.MinimumDegreeOfParallelism - 1;
        Argon2EncodedHash parallelismAbove = CreateMinimumHash();
        parallelismAbove.Parameters.DegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism + 1;
        Argon2EncodedHash invalidVariant = CreateMinimumHash();
        invalidVariant.Parameters.Variant = (Argon2Variant)99;
        Argon2EncodedHash invalidVersion = CreateMinimumHash();
        invalidVersion.Parameters.Version = (Argon2Version)18;

        Assert.Multiple(() =>
        {
            AssertEncodingFails(codec, iterationsBelow);
            AssertEncodingFails(codec, iterationsAbove);
            AssertEncodingFails(codec, memoryBelow);
            AssertEncodingFails(codec, memoryAbove);
            AssertEncodingFails(codec, parallelismBelow);
            AssertEncodingFails(codec, parallelismAbove);
            AssertEncodingFails(codec, invalidVariant);
            AssertEncodingFails(codec, invalidVersion);
        });
    }

    [Test]
    public void EncodedLengthEnforcesMemoryLaneAndComponentMinimumRelationships()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        Argon2EncodedHash exactMemory = CreateMinimumHash();
        exactMemory.Parameters.DegreeOfParallelism = 4;
        exactMemory.Parameters.MemorySizeKiB = 32L;
        Argon2EncodedHash insufficientMemory = CreateMinimumHash();
        insufficientMemory.Parameters.DegreeOfParallelism = 4;
        insufficientMemory.Parameters.MemorySizeKiB = 31L;
        Argon2EncodedHash shortSalt = CreateMinimumHash();
        shortSalt.Salt = new byte[Argon2Limits.MinimumSaltLength - 1];
        Argon2EncodedHash shortTag = CreateMinimumHash();
        shortTag.Tag = new byte[Argon2Limits.MinimumTagLength - 1];

        Assert.Multiple(() =>
        {
            AssertEncodingSucceeds(codec, exactMemory);
            AssertEncodingFails(codec, insufficientMemory);
            AssertEncodingFails(codec, shortSalt);
            AssertEncodingFails(codec, shortTag);
        });
    }

    [Test]
    public void PrimitiveAndKeyDeriverRejectMissingConfigurationModels()
    {
        Argon2Primitive primitive = new Argon2Primitive();
        Argon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        Argon2Parameters parameters = CreateMinimumHash().Parameters;
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 1;
        byte[] salt = new byte[Argon2Limits.MinimumSaltLength];
        byte[] tag = new byte[Argon2Limits.MinimumTagLength];

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => primitive.ComputeHash(
                ReadOnlySpan<byte>.Empty,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                null!,
                executionOptions,
                tag));
            Assert.Throws<Argon2ParameterException>(() => primitive.VerifyHash(
                ReadOnlySpan<byte>.Empty,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                null!,
                tag));
            Assert.Throws<Argon2ParameterException>(() => keyDeriver.DeriveKey(
                ReadOnlySpan<byte>.Empty,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                null!,
                executionOptions,
                Argon2Limits.MinimumTagLength));
            Assert.Throws<Argon2ParameterException>(() => keyDeriver.DeriveKey(
                ReadOnlySpan<byte>.Empty,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                null!,
                Argon2Limits.MinimumTagLength));
        });
    }

    [Test]
    public void PasswordHasherConstructorAcceptsMinimumAndMaximumWorkerBoundaries()
    {
        Argon2PasswordHasherOptions minimum = CreateMinimumOptions();
        Argon2PasswordHasherOptions maximum = CreateMinimumOptions();
        maximum.Parameters.DegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism;
        maximum.Parameters.MemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB
            * Argon2Limits.MaximumDegreeOfParallelism;
        maximum.ExecutionOptions.MaximumWorkerCount = Argon2Limits.MaximumWorkerCount;
        maximum.VerificationPolicy.MaximumEncodedHashLength = Array.MaxLength;
        maximum.VerificationPolicy.MaximumMemorySizeKiB = maximum.Parameters.MemorySizeKiB;
        maximum.VerificationPolicy.MaximumDegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => new Argon2PasswordHasher(minimum));
            Assert.DoesNotThrow(() => new Argon2PasswordHasher(maximum));
        });
    }

    [Test]
    public void PasswordHasherConstructorRejectsWorkersOutsideTheirRangeOrAboveLaneCount()
    {
        Argon2PasswordHasherOptions below = CreateMinimumOptions();
        below.ExecutionOptions.MaximumWorkerCount = Argon2Limits.MinimumWorkerCount - 1;
        Argon2PasswordHasherOptions above = CreateMinimumOptions();
        above.ExecutionOptions.MaximumWorkerCount = Argon2Limits.MaximumWorkerCount + 1;
        Argon2PasswordHasherOptions aboveLaneCount = CreateMinimumOptions();
        aboveLaneCount.ExecutionOptions.MaximumWorkerCount = 2;

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(below));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(above));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(aboveLaneCount));
        });
    }

    [Test]
    public void PasswordHasherConstructorAcceptsCoherentMinimumAndMaximumPolicyBoundaries()
    {
        Argon2PasswordHasherOptions minimum = CreateMinimumOptions();
        Argon2PasswordHasherOptions maximum = CreateMinimumOptions();
        maximum.VerificationPolicy.MaximumEncodedHashLength = Array.MaxLength;
        maximum.VerificationPolicy.MaximumMemorySizeKiB = Argon2Limits.MaximumMemorySizeKiB;
        maximum.VerificationPolicy.MaximumIterations = Argon2Limits.MaximumIterations;
        maximum.VerificationPolicy.MaximumDegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism;
        maximum.VerificationPolicy.MaximumSaltLength = Array.MaxLength;
        maximum.VerificationPolicy.MaximumTagLength = Array.MaxLength;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => new Argon2PasswordHasher(minimum));
            Assert.DoesNotThrow(() => new Argon2PasswordHasher(maximum));
        });
    }

    [Test]
    public void PasswordHasherConstructorRejectsEveryJustOutsidePolicyBoundary()
    {
        Assert.Multiple(() =>
        {
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumEncodedHashLength = 0);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumEncodedHashLength = Array.MaxLength + 1);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumMemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB - 1L);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumMemorySizeKiB = Argon2Limits.MaximumMemorySizeKiB + 1L);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumIterations = Argon2Limits.MinimumIterations - 1L);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumIterations = Argon2Limits.MaximumIterations + 1L);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumDegreeOfParallelism = Argon2Limits.MinimumDegreeOfParallelism - 1);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumDegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism + 1);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumSaltLength = Argon2Limits.MinimumSaltLength - 1);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumSaltLength = Array.MaxLength + 1);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumTagLength = Argon2Limits.MinimumTagLength - 1);
            AssertInvalidOptions(options => options.VerificationPolicy.MaximumTagLength = Array.MaxLength + 1);
        });
    }

    [Test]
    public void PasswordHasherConstructorRejectsMissingNestedModels()
    {
        Argon2PasswordHasherOptions missingParameters = CreateMinimumOptions();
        missingParameters.Parameters = null!;
        Argon2PasswordHasherOptions missingExecutionOptions = CreateMinimumOptions();
        missingExecutionOptions.ExecutionOptions = null!;
        Argon2PasswordHasherOptions missingVerificationPolicy = CreateMinimumOptions();
        missingVerificationPolicy.VerificationPolicy = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => new Argon2PasswordHasher(missingParameters));
            Assert.Throws<Argon2ParameterException>(
                () => new Argon2PasswordHasher(missingExecutionOptions));
            Assert.Throws<Argon2ParameterException>(
                () => new Argon2PasswordHasher(missingVerificationPolicy));
        });
    }

    [Test]
    public void PasswordHasherConstructorRejectsGenerationLengthsOutsideManagedBoundaries()
    {
        Assert.Multiple(() =>
        {
            AssertInvalidOptions(options => options.SaltLength = Argon2Limits.MinimumSaltLength - 1);
            AssertInvalidOptions(options => options.SaltLength = Array.MaxLength + 1);
            AssertInvalidOptions(options => options.TagLength = Argon2Limits.MinimumTagLength - 1);
            AssertInvalidOptions(options => options.TagLength = Array.MaxLength + 1);
        });
    }

    [TestCase("memory")]
    [TestCase("iterations")]
    [TestCase("parallelism")]
    [TestCase("salt-length")]
    [TestCase("tag-length")]
    public void PasswordHasherConstructorRejectsPolicyBelowEveryGeneratedField(string field)
    {
        Argon2PasswordHasherOptions options = CreateCoherenceOptions();
        switch (field)
        {
            case "memory":
                options.VerificationPolicy.MaximumMemorySizeKiB = 31L;
                break;
            case "iterations":
                options.VerificationPolicy.MaximumIterations = 1L;
                break;
            case "parallelism":
                options.VerificationPolicy.MaximumDegreeOfParallelism = 1;
                break;
            case "salt-length":
                options.VerificationPolicy.MaximumSaltLength = 8;
                break;
            default:
                options.VerificationPolicy.MaximumTagLength = 4;
                break;
        }

        Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(options));
    }

    [Test]
    public void PasswordHasherConstructorRejectsEmptyOrGenerationExcludingAllowLists()
    {
        Argon2PasswordHasherOptions noVariants = CreateCoherenceOptions();
        noVariants.VerificationPolicy.AllowVariantD = false;
        noVariants.VerificationPolicy.AllowVariantI = false;
        noVariants.VerificationPolicy.AllowVariantId = false;
        Argon2PasswordHasherOptions noVersions = CreateCoherenceOptions();
        noVersions.VerificationPolicy.AllowVersion10 = false;
        noVersions.VerificationPolicy.AllowVersion13 = false;
        Argon2PasswordHasherOptions excludesGeneratedVariant = CreateCoherenceOptions();
        excludesGeneratedVariant.VerificationPolicy.AllowVariantD = false;
        Argon2PasswordHasherOptions excludesGeneratedVersion = CreateCoherenceOptions();
        excludesGeneratedVersion.VerificationPolicy.AllowVersion10 = false;

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(noVariants));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(noVersions));
            Assert.Throws<Argon2ParameterException>(
                () => new Argon2PasswordHasher(excludesGeneratedVariant));
            Assert.Throws<Argon2ParameterException>(
                () => new Argon2PasswordHasher(excludesGeneratedVersion));
        });
    }

    [Test]
    public void PasswordHasherConstructorChecksExactManagedEncodedLengthOverflowWithoutAllocatingComponents()
    {
        int minimumEncodedLength = GetMinimumEncodedHashLength();
        int saltCharactersAvailable = Array.MaxLength
            - minimumEncodedLength
            + GetUnpaddedBase64Length(Argon2Limits.MinimumSaltLength);
        int tagCharactersAvailable = Array.MaxLength
            - minimumEncodedLength
            + GetUnpaddedBase64Length(Argon2Limits.MinimumTagLength);
        int maximumSaltLength = GetLargestByteLength(saltCharactersAvailable);
        int maximumTagLength = GetLargestByteLength(tagCharactersAvailable);
        Argon2PasswordHasherOptions acceptedSalt = CreateMaximumEncodedLengthOptions();
        acceptedSalt.SaltLength = maximumSaltLength;
        Argon2PasswordHasherOptions rejectedSalt = CreateMaximumEncodedLengthOptions();
        rejectedSalt.SaltLength = maximumSaltLength + 1;
        Argon2PasswordHasherOptions acceptedTag = CreateMaximumEncodedLengthOptions();
        acceptedTag.TagLength = maximumTagLength;
        Argon2PasswordHasherOptions rejectedTag = CreateMaximumEncodedLengthOptions();
        rejectedTag.TagLength = maximumTagLength + 1;

        Assert.Multiple(() =>
        {
            Assert.That(maximumSaltLength, Is.LessThan(Array.MaxLength));
            Assert.That(maximumTagLength, Is.LessThan(Array.MaxLength));
            Assert.DoesNotThrow(() => new Argon2PasswordHasher(acceptedSalt));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(rejectedSalt));
            Assert.DoesNotThrow(() => new Argon2PasswordHasher(acceptedTag));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(rejectedTag));
        });
    }

    [Test]
    public void PrimitiveAndKeyDeriverEnforceOutputBoundariesBeforeExpensiveWork()
    {
        IArgon2Primitive primitive = new Argon2Primitive();
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        Argon2Parameters parameters = CreateMinimumHash().Parameters;
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 1;
        byte[] salt = new byte[Argon2Limits.MinimumSaltLength];
        byte[] minimumTag = new byte[Argon2Limits.MinimumTagLength];

        primitive.ComputeHash(
            ReadOnlySpan<byte>.Empty,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            minimumTag);
        bool verified = primitive.VerifyHash(
            ReadOnlySpan<byte>.Empty,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            minimumTag);
        byte[] minimumKey = keyDeriver.DeriveKey(
            ReadOnlySpan<byte>.Empty,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            Argon2Limits.MinimumTagLength);

        Assert.Multiple(() =>
        {
            Assert.That(verified, Is.True);
            Assert.That(minimumKey.Length, Is.EqualTo(Argon2Limits.MinimumTagLength));
            Assert.Throws<Argon2ParameterException>(() => primitive.ComputeHash(
                ReadOnlySpan<byte>.Empty,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                executionOptions,
                new byte[Argon2Limits.MinimumTagLength - 1]));
            Assert.Throws<Argon2ParameterException>(() => primitive.VerifyHash(
                ReadOnlySpan<byte>.Empty,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                executionOptions,
                new byte[Argon2Limits.MinimumTagLength - 1]));
            Assert.Throws<Argon2ParameterException>(() => keyDeriver.DeriveKey(
                ReadOnlySpan<byte>.Empty,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                executionOptions,
                Argon2Limits.MinimumTagLength - 1));
            Assert.Throws<Argon2ParameterException>(() => keyDeriver.DeriveKey(
                ReadOnlySpan<byte>.Empty,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                executionOptions,
                Array.MaxLength + 1));
        });
    }

    static Argon2EncodedHash CreateMinimumHash()
    {
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters.Variant = Argon2Variant.D;
        hash.Parameters.Version = Argon2Version.Version10;
        hash.Parameters.Iterations = Argon2Limits.MinimumIterations;
        hash.Parameters.MemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB;
        hash.Parameters.DegreeOfParallelism = Argon2Limits.MinimumDegreeOfParallelism;
        hash.Salt = new byte[Argon2Limits.MinimumSaltLength];
        hash.Tag = new byte[Argon2Limits.MinimumTagLength];
        return hash;
    }

    static Argon2PasswordHasherOptions CreateMinimumOptions()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters = CreateMinimumHash().Parameters;
        options.SaltLength = Argon2Limits.MinimumSaltLength;
        options.TagLength = Argon2Limits.MinimumTagLength;
        options.ExecutionOptions.MaximumWorkerCount = Argon2Limits.MinimumWorkerCount;
        options.VerificationPolicy.MaximumEncodedHashLength = GetMinimumEncodedHashLength();
        options.VerificationPolicy.MaximumMemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB;
        options.VerificationPolicy.MaximumIterations = Argon2Limits.MinimumIterations;
        options.VerificationPolicy.MaximumDegreeOfParallelism = Argon2Limits.MinimumDegreeOfParallelism;
        options.VerificationPolicy.MaximumSaltLength = Argon2Limits.MinimumSaltLength;
        options.VerificationPolicy.MaximumTagLength = Argon2Limits.MinimumTagLength;
        options.VerificationPolicy.AllowVariantD = true;
        options.VerificationPolicy.AllowVariantI = false;
        options.VerificationPolicy.AllowVariantId = false;
        options.VerificationPolicy.AllowVersion10 = true;
        options.VerificationPolicy.AllowVersion13 = false;
        return options;
    }

    static Argon2PasswordHasherOptions CreateMaximumEncodedLengthOptions()
    {
        Argon2PasswordHasherOptions options = CreateMinimumOptions();
        options.VerificationPolicy.MaximumEncodedHashLength = Array.MaxLength;
        options.VerificationPolicy.MaximumSaltLength = Array.MaxLength;
        options.VerificationPolicy.MaximumTagLength = Array.MaxLength;
        return options;
    }

    static Argon2PasswordHasherOptions CreateCoherenceOptions()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters.Variant = Argon2Variant.D;
        options.Parameters.Version = Argon2Version.Version10;
        options.Parameters.MemorySizeKiB = 32L;
        options.Parameters.Iterations = 2L;
        options.Parameters.DegreeOfParallelism = 2;
        options.SaltLength = 9;
        options.TagLength = 5;
        options.ExecutionOptions.MaximumWorkerCount = 1;
        options.VerificationPolicy.MaximumMemorySizeKiB = 32L;
        options.VerificationPolicy.MaximumIterations = 2L;
        options.VerificationPolicy.MaximumDegreeOfParallelism = 2;
        options.VerificationPolicy.MaximumSaltLength = 9;
        options.VerificationPolicy.MaximumTagLength = 5;
        return options;
    }

    static int GetMinimumEncodedHashLength()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        return codec.GetEncodedHashLength(CreateMinimumHash());
    }

    static int GetUnpaddedBase64Length(int byteLength)
    {
        int remainder = byteLength % 3;
        return byteLength / 3 * 4 + (remainder == 0 ? 0 : remainder + 1);
    }

    static int GetLargestByteLength(int encodedCharactersAvailable)
    {
        long candidate = (long)(encodedCharactersAvailable / 4) * 3L + 2L;
        while (GetUnpaddedBase64Length((int)candidate) > encodedCharactersAvailable)
        {
            candidate = candidate - 1L;
        }

        while (candidate < Array.MaxLength
            && GetUnpaddedBase64Length((int)(candidate + 1L)) <= encodedCharactersAvailable)
        {
            candidate = candidate + 1L;
        }

        return (int)candidate;
    }

    static void AssertEncodingSucceeds(
        IArgon2EncodedHashCodec codec,
        Argon2EncodedHash hash)
    {
        string encoded = codec.EncodeHash(hash);
        Assert.That(codec.GetEncodedHashLength(hash), Is.EqualTo(encoded.Length));
    }

    static void AssertEncodingFails(
        IArgon2EncodedHashCodec codec,
        Argon2EncodedHash hash)
    {
        Assert.Throws<Argon2ParameterException>(() => codec.EncodeHash(hash));
        Assert.Throws<Argon2ParameterException>(() => codec.GetEncodedHashLength(hash));
    }

    static void AssertInvalidOptions(Action<Argon2PasswordHasherOptions> change)
    {
        Argon2PasswordHasherOptions options = CreateMinimumOptions();
        change(options);
        Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(options));
    }
}
