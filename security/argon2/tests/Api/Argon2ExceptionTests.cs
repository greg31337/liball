using argon2;

namespace argon2.Tests.Api;

public class Argon2ExceptionTests
{
    [Test]
    public void ParameterExceptionPreservesMessage()
    {
        Argon2ParameterException exception = new Argon2ParameterException("Memory is too small.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<Exception>());
            Assert.That(exception.Message, Is.EqualTo("Memory is too small."));
        });
    }

    [Test]
    public void FormatExceptionPreservesMessage()
    {
        Argon2FormatException exception = new Argon2FormatException("The encoded hash is malformed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<Exception>());
            Assert.That(exception.Message, Is.EqualTo("The encoded hash is malformed."));
        });
    }

    [Test]
    public void ExceptionTypesDistinguishCallerParametersFromEncodedInput()
    {
        Argon2ParameterException parameterException = new Argon2ParameterException("Invalid parameter.");
        Argon2FormatException formatException = new Argon2FormatException("Invalid encoded hash.");

        Assert.Multiple(() =>
        {
            Assert.That(parameterException, Is.Not.InstanceOf<Argon2FormatException>());
            Assert.That(formatException, Is.Not.InstanceOf<Argon2ParameterException>());
        });
    }

    [Test]
    public void InvalidCallerParametersThrowParameterExceptionsFromPublicApis()
    {
        IArgon2Primitive primitive = new Argon2Primitive();
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateHasherOptions(8L, 8L));
        Argon2Parameters parameters = CreateParameters(8L);
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => primitive.ComputeHash(
                    ReadOnlySpan<byte>.Empty,
                    new byte[8],
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    null!,
                    executionOptions,
                    new byte[16]));
            Assert.Throws<Argon2ParameterException>(
                () => keyDeriver.DeriveKey(
                    ReadOnlySpan<byte>.Empty,
                    new byte[7],
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    parameters,
                    executionOptions,
                    16));
            Assert.Throws<Argon2ParameterException>(() => codec.EncodeHash(new Argon2EncodedHash()));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(null!));
            Assert.Throws<Argon2ParameterException>(() => hasher.HashPasswordText(null!));
            Assert.Throws<Argon2ParameterException>(() => hasher.VerifyPasswordText(null!, "invalid"));
        });
    }

    [TestCase("invalid")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdB")]
    [TestCase("$argon2id$v=19$m=7,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2x$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=18$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    public void ThrowingEncodedHashOperationsUseFormatExceptions(string encoded)
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateHasherOptions(8L, 8L));

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2FormatException>(() => codec.DecodeHash(encoded));
            Assert.Throws<Argon2FormatException>(() => hasher.NeedsRehash(encoded));
        });
    }

    [Test]
    public void RoutineVerificationFailuresReturnResultsInsteadOfExceptions()
    {
        byte[] password = new byte[] { 1, 2, 3, 4 };
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateHasherOptions(8L, 8L));
        IArgon2PasswordHasher higherCostHasher = new Argon2PasswordHasher(
            CreateHasherOptions(16L, 16L));
        string encoded = hasher.HashPasswordBytes(password);
        string higherCostEncoded = higherCostHasher.HashPasswordBytes(password);

        Argon2VerificationResult mismatch = hasher.VerifyPasswordBytes(
            new byte[] { 1, 2, 3, 5 },
            encoded);
        Argon2VerificationResult malformed = hasher.VerifyPasswordBytes(password, "invalid");
        Argon2VerificationResult rejected = hasher.VerifyPasswordBytes(password, higherCostEncoded);

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(mismatch.NeedsRehash, Is.False);
            Assert.That(malformed.Status, Is.EqualTo(Argon2VerificationStatus.InvalidHash));
            Assert.That(malformed.NeedsRehash, Is.False);
            Assert.That(rejected.Status, Is.EqualTo(Argon2VerificationStatus.RejectedByPolicy));
            Assert.That(rejected.NeedsRehash, Is.False);
        });
    }

    static Argon2PasswordHasherOptions CreateHasherOptions(
        long memorySizeKiB,
        long maximumMemorySizeKiB)
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters = CreateParameters(memorySizeKiB);
        options.SaltLength = 8;
        options.TagLength = 16;
        options.ExecutionOptions.MaximumWorkerCount = 1;
        options.VerificationPolicy.MaximumMemorySizeKiB = maximumMemorySizeKiB;
        options.VerificationPolicy.MaximumIterations = 1L;
        options.VerificationPolicy.MaximumDegreeOfParallelism = 1;
        options.VerificationPolicy.MaximumSaltLength = 8;
        options.VerificationPolicy.MaximumTagLength = 16;
        return options;
    }

    static Argon2Parameters CreateParameters(long memorySizeKiB)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = Argon2Variant.Id;
        parameters.Version = Argon2Version.Version13;
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.Iterations = 1L;
        parameters.DegreeOfParallelism = 1;
        return parameters;
    }
}
