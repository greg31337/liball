using argon2;

namespace argon2.Tests.Api;

public class Argon2KeyDeriverTests
{
    [TestCase(4)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(64)]
    [TestCase(65)]
    [TestCase(96)]
    public void DerivedKeysHaveExactRequestedLengthAndMatchPrimitive(int outputLength)
    {
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        IArgon2Primitive primitive = new Argon2Primitive();
        Argon2Parameters parameters = CreateParameters();
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();
        byte[] password = CreatePassword();
        byte[] salt = CreateSalt();
        byte[] secret = CreateSecret();
        byte[] associatedData = CreateAssociatedData();
        byte[] expected = new byte[outputLength];

        byte[] derived = keyDeriver.DeriveKey(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            outputLength);
        primitive.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            expected);

        Assert.Multiple(() =>
        {
            Assert.That(derived.Length, Is.EqualTo(outputLength));
            Assert.That(derived, Is.EqualTo(expected));
        });
    }

    [Test]
    public void RequestedLengthParticipatesInOneShotDerivation()
    {
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        Argon2Parameters parameters = CreateParameters();
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();

        byte[] key16 = keyDeriver.DeriveKey(
            CreatePassword(),
            CreateSalt(),
            CreateSecret(),
            CreateAssociatedData(),
            parameters,
            executionOptions,
            16);
        byte[] repeatedKey16 = keyDeriver.DeriveKey(
            CreatePassword(),
            CreateSalt(),
            CreateSecret(),
            CreateAssociatedData(),
            parameters,
            executionOptions,
            16);
        byte[] key32 = keyDeriver.DeriveKey(
            CreatePassword(),
            CreateSalt(),
            CreateSecret(),
            CreateAssociatedData(),
            parameters,
            executionOptions,
            32);

        Assert.Multiple(() =>
        {
            Assert.That(
                Convert.ToHexString(key16).ToLowerInvariant(),
                Is.EqualTo("d0f678dfd0308872250f295a0432db55"));
            Assert.That(repeatedKey16, Is.EqualTo(key16));
            Assert.That(repeatedKey16, Is.Not.SameAs(key16));
            Assert.That(key32[..16], Is.Not.EqualTo(key16));
        });
    }

    [Test]
    public void KeyDerivationPreservesCallerOwnedInputs()
    {
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        byte[] password = CreatePassword();
        byte[] salt = CreateSalt();
        byte[] secret = CreateSecret();
        byte[] associatedData = CreateAssociatedData();
        byte[] originalPassword = (byte[])password.Clone();
        byte[] originalSalt = (byte[])salt.Clone();
        byte[] originalSecret = (byte[])secret.Clone();
        byte[] originalAssociatedData = (byte[])associatedData.Clone();
        Argon2Parameters parameters = CreateParameters();
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();

        keyDeriver.DeriveKey(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            32);

        Assert.Multiple(() =>
        {
            Assert.That(password, Is.EqualTo(originalPassword));
            Assert.That(salt, Is.EqualTo(originalSalt));
            Assert.That(secret, Is.EqualTo(originalSecret));
            Assert.That(associatedData, Is.EqualTo(originalAssociatedData));
            Assert.That(parameters.Variant, Is.EqualTo(Argon2Variant.Id));
            Assert.That(parameters.Version, Is.EqualTo(Argon2Version.Version13));
            Assert.That(parameters.Iterations, Is.EqualTo(2L));
            Assert.That(parameters.MemorySizeKiB, Is.EqualTo(32L));
            Assert.That(parameters.DegreeOfParallelism, Is.EqualTo(2));
            Assert.That(executionOptions.MaximumWorkerCount, Is.EqualTo(2));
            Assert.That(executionOptions.ClearMemory, Is.True);
        });
    }

    [Test]
    public void SecretAndAssociatedDataIndependentlyParticipateInDerivation()
    {
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        Argon2Parameters parameters = CreateParameters();
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();
        byte[] emptyContext = keyDeriver.DeriveKey(
            CreatePassword(),
            CreateSalt(),
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            16);
        byte[] secretOnly = keyDeriver.DeriveKey(
            CreatePassword(),
            CreateSalt(),
            CreateSecret(),
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            16);
        byte[] associatedDataOnly = keyDeriver.DeriveKey(
            CreatePassword(),
            CreateSalt(),
            ReadOnlySpan<byte>.Empty,
            CreateAssociatedData(),
            parameters,
            executionOptions,
            16);
        byte[] fullContext = keyDeriver.DeriveKey(
            CreatePassword(),
            CreateSalt(),
            CreateSecret(),
            CreateAssociatedData(),
            parameters,
            executionOptions,
            16);

        string[] results = new string[]
        {
            Convert.ToHexString(emptyContext),
            Convert.ToHexString(secretOnly),
            Convert.ToHexString(associatedDataOnly),
            Convert.ToHexString(fullContext)
        };
        Assert.That(results.Distinct().Count(), Is.EqualTo(4));
    }

    [Test]
    public void EveryAlgorithmParameterChangeProducesAnIndependentKey()
    {
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 1;
        Argon2Parameters baseline = CreateParameters();
        Argon2Parameters changedVariant = CreateParameters();
        changedVariant.Variant = Argon2Variant.D;
        Argon2Parameters changedVersion = CreateParameters();
        changedVersion.Version = Argon2Version.Version10;
        Argon2Parameters changedIterations = CreateParameters();
        changedIterations.Iterations = 3L;
        Argon2Parameters changedMemory = CreateParameters();
        changedMemory.MemorySizeKiB = 33L;
        Argon2Parameters changedParallelism = CreateParameters();
        changedParallelism.DegreeOfParallelism = 1;
        Argon2Parameters[] parameterSets = new Argon2Parameters[]
        {
            baseline,
            changedVariant,
            changedVersion,
            changedIterations,
            changedMemory,
            changedParallelism
        };
        string[] keys = new string[parameterSets.Length];
        int index = 0;
        while (index < parameterSets.Length)
        {
            byte[] key = keyDeriver.DeriveKey(
                CreatePassword(),
                CreateSalt(),
                CreateSecret(),
                CreateAssociatedData(),
                parameterSets[index],
                executionOptions,
                16);
            keys[index] = Convert.ToHexString(key);
            index = index + 1;
        }

        Assert.That(keys.Distinct().Count(), Is.EqualTo(parameterSets.Length));
    }

    [Test]
    public void InvalidOutputLengthsAndInputsAreRejected()
    {
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        Argon2Parameters parameters = CreateParameters();
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => keyDeriver.DeriveKey(
                    CreatePassword(),
                    CreateSalt(),
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    -1));
            Assert.Throws<Argon2ParameterException>(
                () => keyDeriver.DeriveKey(
                    CreatePassword(),
                    CreateSalt(),
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    0));
            Assert.Throws<Argon2ParameterException>(
                () => keyDeriver.DeriveKey(
                    CreatePassword(),
                    CreateSalt(),
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    3));
            Assert.Throws<Argon2ParameterException>(
                () => keyDeriver.DeriveKey(
                    CreatePassword(),
                    CreateSalt(),
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    Array.MaxLength + 1));
            Assert.Throws<Argon2ParameterException>(
                () => keyDeriver.DeriveKey(
                    CreatePassword(),
                    new byte[7],
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    32));
        });
    }

    static Argon2Parameters CreateParameters()
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = Argon2Variant.Id;
        parameters.Version = Argon2Version.Version13;
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

    static byte[] CreatePassword()
    {
        return new byte[] { 0, 1, 2, 3, 4, 5, 255 };
    }

    static byte[] CreateSalt()
    {
        return new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };
    }

    static byte[] CreateSecret()
    {
        return new byte[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 };
    }

    static byte[] CreateAssociatedData()
    {
        return new byte[] { 0xB0, 0xB1, 0xB2, 0xB3, 0xB4 };
    }
}
