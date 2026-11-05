using argon2;

namespace argon2.Tests.Api;

public class Argon2PrimitiveTests
{
    [TestCase(Argon2Variant.D, Argon2Version.Version10, false, "07b8b74a21872d8e6e0a615086065abf")]
    [TestCase(Argon2Variant.D, Argon2Version.Version10, true, "03d1efb796700626e987177059e723a4")]
    [TestCase(Argon2Variant.D, Argon2Version.Version13, false, "9dcd746b43d1fd82a4db2b0889e3d7a4")]
    [TestCase(Argon2Variant.D, Argon2Version.Version13, true, "2f55db4a5d213eaae4fe240479d2b3e9")]
    [TestCase(Argon2Variant.I, Argon2Version.Version10, false, "c62dbe795122712577c6b9808cbb236d")]
    [TestCase(Argon2Variant.I, Argon2Version.Version10, true, "ad9dc9584967fbae905101ba5fdf94fe")]
    [TestCase(Argon2Variant.I, Argon2Version.Version13, false, "cf213edff33bb9fa90242c35f4dddb96")]
    [TestCase(Argon2Variant.I, Argon2Version.Version13, true, "b732c4579c14cdf98080a6a2572e9d55")]
    [TestCase(Argon2Variant.Id, Argon2Version.Version10, false, "6b1065b5f76466de42af8da83a1f8a9d")]
    [TestCase(Argon2Variant.Id, Argon2Version.Version10, true, "50ae4dbf13bc9019cb6c45043275ae45")]
    [TestCase(Argon2Variant.Id, Argon2Version.Version13, false, "f83add8f111e380ade3f5b3f4b25f65b")]
    [TestCase(Argon2Variant.Id, Argon2Version.Version13, true, "d0f678dfd0308872250f295a0432db55")]
    public void HashingAndVerificationSupportEveryVariantVersionAndContextCombination(
        Argon2Variant variant,
        Argon2Version version,
        bool includeContext,
        string expectedHex)
    {
        IArgon2Primitive primitive = new Argon2Primitive();
        Argon2Parameters parameters = CreateParameters(variant, version);
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();
        byte[] password = new byte[] { 0, 1, 2, 3, 4, 5, 255 };
        byte[] salt = new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };
        byte[] secret = includeContext
            ? new byte[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 }
            : Array.Empty<byte>();
        byte[] associatedData = includeContext
            ? new byte[] { 0xB0, 0xB1, 0xB2, 0xB3, 0xB4 }
            : Array.Empty<byte>();
        byte[] originalPassword = (byte[])password.Clone();
        byte[] originalSalt = (byte[])salt.Clone();
        byte[] originalSecret = (byte[])secret.Clone();
        byte[] originalAssociatedData = (byte[])associatedData.Clone();
        byte[] destination = new byte[16];

        primitive.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            destination);
        bool verified = primitive.VerifyHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(destination).ToLowerInvariant(), Is.EqualTo(expectedHex));
            Assert.That(verified, Is.True);
            Assert.That(password, Is.EqualTo(originalPassword));
            Assert.That(salt, Is.EqualTo(originalSalt));
            Assert.That(secret, Is.EqualTo(originalSecret));
            Assert.That(associatedData, Is.EqualTo(originalAssociatedData));
            Assert.That(parameters.Variant, Is.EqualTo(variant));
            Assert.That(parameters.Version, Is.EqualTo(version));
            Assert.That(parameters.Iterations, Is.EqualTo(2L));
            Assert.That(parameters.MemorySizeKiB, Is.EqualTo(32L));
            Assert.That(parameters.DegreeOfParallelism, Is.EqualTo(2));
            Assert.That(executionOptions.MaximumWorkerCount, Is.EqualTo(2));
            Assert.That(executionOptions.ClearMemory, Is.True);
        });
    }

    [TestCase(4)]
    [TestCase(32)]
    [TestCase(64)]
    [TestCase(65)]
    [TestCase(96)]
    public void EmptyPasswordSupportsDestinationBranchesAndRepeatedCalls(int destinationLength)
    {
        IArgon2Primitive primitive = new Argon2Primitive();
        Argon2Parameters parameters = CreateMinimumParameters();
        Argon2ExecutionOptions executionOptions = CreateMinimumExecutionOptions();
        byte[] salt = new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };
        byte[] first = Enumerable.Repeat((byte)0xA5, destinationLength).ToArray();
        byte[] second = Enumerable.Repeat((byte)0x5A, destinationLength).ToArray();

        primitive.ComputeHash(
            ReadOnlySpan<byte>.Empty,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            first);
        primitive.ComputeHash(
            ReadOnlySpan<byte>.Empty,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            second);
        bool verified = primitive.VerifyHash(
            ReadOnlySpan<byte>.Empty,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            first);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Has.Some.Not.Zero);
            Assert.That(verified, Is.True);
        });
    }

    [Test]
    public void SecretAndAssociatedDataAreIndependentOptionalInputs()
    {
        IArgon2Primitive primitive = new Argon2Primitive();
        Argon2Parameters parameters = CreateMinimumParameters();
        Argon2ExecutionOptions executionOptions = CreateMinimumExecutionOptions();
        byte[] password = new byte[] { 0, 1, 2, 255 };
        byte[] salt = new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };
        byte[] secret = new byte[] { 0xA0, 0xA1, 0xA2 };
        byte[] associatedData = new byte[] { 0xB0, 0xB1, 0xB2 };
        byte[] emptyContextTag = new byte[16];
        byte[] secretOnlyTag = new byte[16];
        byte[] associatedDataOnlyTag = new byte[16];
        byte[] fullContextTag = new byte[16];

        primitive.ComputeHash(
            password,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            emptyContextTag);
        primitive.ComputeHash(
            password,
            salt,
            secret,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            secretOnlyTag);
        primitive.ComputeHash(
            password,
            salt,
            ReadOnlySpan<byte>.Empty,
            associatedData,
            parameters,
            executionOptions,
            associatedDataOnlyTag);
        primitive.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            fullContextTag);

        string[] tags = new string[]
        {
            Convert.ToHexString(emptyContextTag),
            Convert.ToHexString(secretOnlyTag),
            Convert.ToHexString(associatedDataOnlyTag),
            Convert.ToHexString(fullContextTag)
        };
        Assert.Multiple(() =>
        {
            Assert.That(tags.Distinct().Count(), Is.EqualTo(4));
            Assert.That(primitive.VerifyHash(
                password,
                salt,
                secret,
                associatedData,
                parameters,
                executionOptions,
                fullContextTag), Is.True);
            Assert.That(primitive.VerifyHash(
                password,
                salt,
                ReadOnlySpan<byte>.Empty,
                associatedData,
                parameters,
                executionOptions,
                fullContextTag), Is.False);
        });
    }

    [Test]
    public void VerificationReturnsFalseForTagInputAndContextMismatches()
    {
        IArgon2Primitive primitive = new Argon2Primitive();
        Argon2Parameters parameters = CreateParameters(
            Argon2Variant.Id,
            Argon2Version.Version13);
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();
        byte[] password = new byte[] { 0, 1, 2, 3, 4, 5, 255 };
        byte[] salt = new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };
        byte[] secret = new byte[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 };
        byte[] associatedData = new byte[] { 0xB0, 0xB1, 0xB2, 0xB3, 0xB4 };
        byte[] expectedTag = Convert.FromHexString("d0f678dfd0308872250f295a0432db55");
        byte[] originalTag = (byte[])expectedTag.Clone();
        byte[] alteredTag = (byte[])expectedTag.Clone();
        alteredTag[0] = (byte)(alteredTag[0] ^ 1);

        bool matchingResult = primitive.VerifyHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            expectedTag);

        bool alteredTagResult = primitive.VerifyHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            alteredTag);
        bool alteredPasswordResult = primitive.VerifyHash(
            new byte[] { 0, 1, 2, 3, 4, 5, 254 },
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            expectedTag);
        bool alteredSaltResult = primitive.VerifyHash(
            password,
            new byte[] { 8, 9, 10, 11, 12, 13, 14, 16 },
            secret,
            associatedData,
            parameters,
            executionOptions,
            expectedTag);
        bool alteredSecretResult = primitive.VerifyHash(
            password,
            salt,
            new byte[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA6 },
            associatedData,
            parameters,
            executionOptions,
            expectedTag);
        bool alteredContextResult = primitive.VerifyHash(
            password,
            salt,
            secret,
            new byte[] { 0xB0, 0xB1, 0xB2, 0xB3, 0xB5 },
            parameters,
            executionOptions,
            expectedTag);

        Assert.Multiple(() =>
        {
            Assert.That(matchingResult, Is.True);
            Assert.That(alteredTagResult, Is.False);
            Assert.That(alteredPasswordResult, Is.False);
            Assert.That(alteredSaltResult, Is.False);
            Assert.That(alteredSecretResult, Is.False);
            Assert.That(alteredContextResult, Is.False);
            Assert.That(expectedTag, Is.EqualTo(originalTag));
        });
    }

    [Test]
    public void InvalidDestinationsAndExpectedTagsAreRejected()
    {
        IArgon2Primitive primitive = new Argon2Primitive();
        Argon2Parameters parameters = CreateParameters(
            Argon2Variant.Id,
            Argon2Version.Version13);
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();
        byte[] salt = new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => primitive.ComputeHash(
                    ReadOnlySpan<byte>.Empty,
                    salt,
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    new byte[3]));
            Assert.Throws<Argon2ParameterException>(
                () => primitive.ComputeHash(
                    ReadOnlySpan<byte>.Empty,
                    salt,
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    Span<byte>.Empty));
            Assert.Throws<Argon2ParameterException>(
                () => primitive.VerifyHash(
                    ReadOnlySpan<byte>.Empty,
                    salt,
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    new byte[3]));
            Assert.Throws<Argon2ParameterException>(
                () => primitive.VerifyHash(
                    ReadOnlySpan<byte>.Empty,
                    salt,
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    ReadOnlySpan<byte>.Empty));
        });
    }

    static Argon2Parameters CreateParameters(
        Argon2Variant variant,
        Argon2Version version)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = version;
        parameters.Iterations = 2L;
        parameters.MemorySizeKiB = 32L;
        parameters.DegreeOfParallelism = 2;
        return parameters;
    }

    static Argon2ExecutionOptions CreateExecutionOptions()
    {
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 2;
        executionOptions.ClearMemory = true;
        return executionOptions;
    }

    static Argon2Parameters CreateMinimumParameters()
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = Argon2Variant.Id;
        parameters.Version = Argon2Version.Version13;
        parameters.Iterations = 1L;
        parameters.MemorySizeKiB = 8L;
        parameters.DegreeOfParallelism = 1;
        return parameters;
    }

    static Argon2ExecutionOptions CreateMinimumExecutionOptions()
    {
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 1;
        executionOptions.ClearMemory = true;
        return executionOptions;
    }
}
