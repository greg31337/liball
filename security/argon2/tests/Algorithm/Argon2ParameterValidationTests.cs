using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2ParameterValidationTests
{
    [Test]
    public void SupportedVariantsAndVersionsAreAccepted()
    {
        Argon2Parameters parameters = CreateMinimumParameters();

        parameters.Variant = Argon2Variant.D;
        parameters.Version = Argon2Version.Version10;
        Assert.DoesNotThrow(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.Variant = Argon2Variant.I;
        parameters.Version = Argon2Version.Version13;
        Assert.DoesNotThrow(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.Variant = Argon2Variant.Id;
        Assert.DoesNotThrow(() => Argon2ParameterValidator.ValidateParameters(parameters));
    }

    [Test]
    public void UnsupportedVariantAndVersionValuesAreRejected()
    {
        Argon2Parameters parameters = CreateMinimumParameters();

        parameters.Variant = (Argon2Variant)3;
        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.Variant = Argon2Variant.Id;
        parameters.Version = (Argon2Version)18;
        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateParameters(parameters));
    }

    [Test]
    public void IterationBoundariesAreValidatedWithoutExecutingWork()
    {
        Argon2Parameters parameters = CreateMinimumParameters();

        parameters.Iterations = Argon2Limits.MinimumIterations;
        Assert.DoesNotThrow(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.Iterations = Argon2Limits.MaximumIterations;
        Assert.DoesNotThrow(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.Iterations = Argon2Limits.MinimumIterations - 1L;
        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.Iterations = Argon2Limits.MaximumIterations + 1L;
        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateParameters(parameters));
    }

    [Test]
    public void MemoryMustProvideTwoBlocksPerSliceAndLane()
    {
        Argon2Parameters parameters = CreateMinimumParameters();
        parameters.DegreeOfParallelism = 4;
        parameters.MemorySizeKiB = 31L;

        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.MemorySizeKiB = 32L;
        Assert.DoesNotThrow(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.MemorySizeKiB = 35L;
        Assert.Multiple(() =>
        {
            Assert.That(Argon2ParameterValidator.GetNormalizedMemoryBlockCount(parameters), Is.EqualTo(32L));
            Assert.That(Argon2ParameterValidator.GetNormalizedMemoryByteCount(parameters), Is.EqualTo(32768L));
        });
    }

    [Test]
    public void MaximumRfcMemoryIsNormalizedWithoutArithmeticOverflow()
    {
        Argon2Parameters parameters = CreateMinimumParameters();
        parameters.MemorySizeKiB = Argon2Limits.MaximumMemorySizeKiB;

        long blocks = Argon2ParameterValidator.GetNormalizedMemoryBlockCount(parameters);
        long bytes = Argon2ParameterValidator.GetNormalizedMemoryByteCount(parameters);

        Assert.Multiple(() =>
        {
            Assert.That(blocks, Is.EqualTo(4294967292L));
            Assert.That(bytes, Is.EqualTo(4398046507008L));
        });

        parameters.MemorySizeKiB = Argon2Limits.MaximumMemorySizeKiB + 1L;
        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateParameters(parameters));
    }

    [Test]
    public void ParallelismAndWorkerBoundariesAreValidated()
    {
        Argon2Parameters parameters = CreateMinimumParameters();
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        parameters.DegreeOfParallelism = 4;
        parameters.MemorySizeKiB = 32L;
        executionOptions.MaximumWorkerCount = 4;

        Assert.DoesNotThrow(
            () => Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters));

        executionOptions.MaximumWorkerCount = 0;
        Assert.Throws<Argon2ParameterException>(
            () => Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters));

        executionOptions.MaximumWorkerCount = 5;
        Assert.Throws<Argon2ParameterException>(
            () => Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters));

        parameters.DegreeOfParallelism = 0;
        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateParameters(parameters));

        parameters.DegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism + 1;
        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateParameters(parameters));
    }

    [Test]
    public void MaximumParallelismAndWorkerCountAreAcceptedWithoutExecutingWork()
    {
        Argon2Parameters parameters = CreateMinimumParameters();
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        parameters.DegreeOfParallelism = Argon2Limits.MaximumDegreeOfParallelism;
        parameters.MemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB
            * parameters.DegreeOfParallelism;
        executionOptions.MaximumWorkerCount = Argon2Limits.MaximumWorkerCount;

        Assert.DoesNotThrow(
            () => Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters));
    }

    [Test]
    public void EmptyOptionalInputsAndMinimumSaltAndTagAreAccepted()
    {
        Assert.DoesNotThrow(
            () => Argon2ParameterValidator.ValidateInputLengths(
                ReadOnlySpan<byte>.Empty,
                new byte[8],
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                4));
    }

    [Test]
    public void ShortSaltAndTagAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2ParameterValidator.ValidateInputLengths(
                    ReadOnlySpan<byte>.Empty,
                    new byte[7],
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    4));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2ParameterValidator.ValidateInputLengths(
                    ReadOnlySpan<byte>.Empty,
                    new byte[8],
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    3));
        });
    }

    [Test]
    public void ManagedArrayLengthConversionRejectsUnaddressableValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Argon2ParameterValidator.GetManagedArrayLength(0L), Is.EqualTo(0));
            Assert.That(Argon2ParameterValidator.GetManagedArrayLength(Array.MaxLength), Is.EqualTo(Array.MaxLength));
            Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.GetManagedArrayLength(-1L));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2ParameterValidator.GetManagedArrayLength((long)Array.MaxLength + 1L));
        });
    }

    [Test]
    public void DefaultPasswordHasherOptionsAreCoherent()
    {
        Assert.DoesNotThrow(
            () => Argon2ParameterValidator.ValidatePasswordHasherOptions(new Argon2PasswordHasherOptions()));
    }

    [Test]
    public void PasswordHasherPolicyMustAcceptGeneratedHashes()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.VerificationPolicy.MaximumIterations = options.Parameters.Iterations - 1L;

        Assert.Throws<Argon2ParameterException>(
            () => Argon2ParameterValidator.ValidatePasswordHasherOptions(options));

        options = new Argon2PasswordHasherOptions();
        options.VerificationPolicy.AllowVariantId = false;
        Assert.Throws<Argon2ParameterException>(
            () => Argon2ParameterValidator.ValidatePasswordHasherOptions(options));
    }

    [Test]
    public void EncodedHashComponentsUseTheSameCentralValidation()
    {
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Salt = new byte[8];
        hash.Tag = new byte[4];

        Assert.DoesNotThrow(() => Argon2ParameterValidator.ValidateEncodedHash(hash));

        hash.Salt = new byte[7];
        Assert.Throws<Argon2ParameterException>(() => Argon2ParameterValidator.ValidateEncodedHash(hash));
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
}
