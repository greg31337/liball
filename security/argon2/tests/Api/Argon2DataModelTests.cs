using argon2;

namespace argon2.Tests.Api;

public class Argon2DataModelTests
{
    [Test]
    public void ParametersDefaultToRfcLowMemoryProfile()
    {
        Argon2Parameters parameters = new Argon2Parameters();

        Assert.Multiple(() =>
        {
            Assert.That(parameters.Variant, Is.EqualTo(Argon2Variant.Id));
            Assert.That(parameters.Version, Is.EqualTo(Argon2Version.Version13));
            Assert.That(parameters.Iterations, Is.EqualTo(3L));
            Assert.That(parameters.MemorySizeKiB, Is.EqualTo(65536L));
            Assert.That(parameters.DegreeOfParallelism, Is.EqualTo(4));
        });
    }

    [Test]
    public void ExecutionOptionsDefaultToClearingOwnedMemory()
    {
        Argon2ExecutionOptions options = new Argon2ExecutionOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.MaximumWorkerCount, Is.EqualTo(4));
            Assert.That(options.ClearMemory, Is.True);
        });
    }

    [Test]
    public void VerificationPolicyAcceptsDefaultGenerationConfiguration()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        Argon2VerificationPolicy policy = options.VerificationPolicy;

        Assert.Multiple(() =>
        {
            Assert.That(policy.MaximumEncodedHashLength, Is.GreaterThan(0));
            Assert.That(policy.MaximumMemorySizeKiB, Is.GreaterThanOrEqualTo(options.Parameters.MemorySizeKiB));
            Assert.That(policy.MaximumIterations, Is.GreaterThanOrEqualTo(options.Parameters.Iterations));
            Assert.That(
                policy.MaximumDegreeOfParallelism,
                Is.GreaterThanOrEqualTo(options.Parameters.DegreeOfParallelism));
            Assert.That(policy.MaximumSaltLength, Is.GreaterThanOrEqualTo(options.SaltLength));
            Assert.That(policy.MaximumTagLength, Is.GreaterThanOrEqualTo(options.TagLength));
            Assert.That(policy.AllowVariantD, Is.True);
            Assert.That(policy.AllowVariantI, Is.True);
            Assert.That(policy.AllowVariantId, Is.True);
            Assert.That(policy.AllowVersion10, Is.True);
            Assert.That(policy.AllowVersion13, Is.True);
        });
    }

    [Test]
    public void PasswordHasherOptionsOwnIndependentMutableModels()
    {
        Argon2PasswordHasherOptions first = new Argon2PasswordHasherOptions();
        Argon2PasswordHasherOptions second = new Argon2PasswordHasherOptions();

        first.Parameters.Iterations = 9L;
        first.ExecutionOptions.MaximumWorkerCount = 1;
        first.VerificationPolicy.AllowVariantD = false;

        Assert.Multiple(() =>
        {
            Assert.That(second.Parameters.Iterations, Is.EqualTo(3L));
            Assert.That(second.ExecutionOptions.MaximumWorkerCount, Is.EqualTo(4));
            Assert.That(second.VerificationPolicy.AllowVariantD, Is.True);
            Assert.That(first.Parameters, Is.Not.SameAs(second.Parameters));
            Assert.That(first.ExecutionOptions, Is.Not.SameAs(second.ExecutionOptions));
            Assert.That(first.VerificationPolicy, Is.Not.SameAs(second.VerificationPolicy));
        });
    }

    [Test]
    public void EncodedHashStartsWithIndependentEmptyComponents()
    {
        Argon2EncodedHash first = new Argon2EncodedHash();
        Argon2EncodedHash second = new Argon2EncodedHash();

        first.Parameters.Variant = Argon2Variant.D;
        first.Salt = new byte[] { 1, 2, 3 };
        first.Tag = new byte[] { 4, 5, 6 };

        Assert.Multiple(() =>
        {
            Assert.That(second.Parameters.Variant, Is.EqualTo(Argon2Variant.Id));
            Assert.That(second.Salt, Is.Empty);
            Assert.That(second.Tag, Is.Empty);
            Assert.That(first.Parameters, Is.Not.SameAs(second.Parameters));
        });
    }

    [Test]
    public void ResultModelsDefaultToFailureWithoutRehash()
    {
        Argon2DecodeResult decodeResult = new Argon2DecodeResult();
        Argon2VerificationResult verificationResult = new Argon2VerificationResult();

        Assert.Multiple(() =>
        {
            Assert.That(decodeResult.Status, Is.EqualTo(Argon2DecodeStatus.InvalidFormat));
            Assert.That(decodeResult.EncodedHash, Is.Null);
            Assert.That(verificationResult.Status, Is.EqualTo(Argon2VerificationStatus.InvalidHash));
            Assert.That(verificationResult.NeedsRehash, Is.False);
        });
    }
}
