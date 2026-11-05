using argon2;

namespace argon2.Tests.Api;

public class Argon2StaticDataTests
{
    [Test]
    public void StructuralConstantsMatchArgon2Definition()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Argon2Limits.BlockSizeInBytes, Is.EqualTo(1024));
            Assert.That(Argon2Limits.WordsPerBlock, Is.EqualTo(128));
            Assert.That(Argon2Limits.SynchronizationPoints, Is.EqualTo(4));
            Assert.That(Argon2Limits.InitialBlocksPerLane, Is.EqualTo(2));
            Assert.That(Argon2Limits.PrehashDigestLength, Is.EqualTo(64));
            Assert.That(Argon2Limits.PrehashSeedLength, Is.EqualTo(72));
            Assert.That(Argon2Limits.Blake2bBlockSizeInBytes, Is.EqualTo(128));
            Assert.That(Argon2Limits.Blake2bMaximumDigestLength, Is.EqualTo(64));
        });
    }

    [Test]
    public void InputBoundsMatchRfcAndInteroperabilityLimits()
    {
        const long MaximumRfcLength = 4294967295L;

        Assert.Multiple(() =>
        {
            Assert.That(Argon2Limits.MinimumTagLength, Is.EqualTo(4));
            Assert.That(Argon2Limits.MaximumTagLength, Is.EqualTo(MaximumRfcLength));
            Assert.That(Argon2Limits.MinimumSaltLength, Is.EqualTo(8));
            Assert.That(Argon2Limits.MaximumSaltLength, Is.EqualTo(MaximumRfcLength));
            Assert.That(Argon2Limits.MinimumPasswordLength, Is.Zero);
            Assert.That(Argon2Limits.MaximumPasswordLength, Is.EqualTo(MaximumRfcLength));
            Assert.That(Argon2Limits.MinimumSecretLength, Is.Zero);
            Assert.That(Argon2Limits.MaximumSecretLength, Is.EqualTo(MaximumRfcLength));
            Assert.That(Argon2Limits.MinimumAssociatedDataLength, Is.Zero);
            Assert.That(Argon2Limits.MaximumAssociatedDataLength, Is.EqualTo(MaximumRfcLength));
            Assert.That(Argon2Limits.MinimumIterations, Is.EqualTo(1L));
            Assert.That(Argon2Limits.MaximumIterations, Is.EqualTo(MaximumRfcLength));
            Assert.That(Argon2Limits.MinimumMemorySizePerLaneKiB, Is.EqualTo(8L));
            Assert.That(Argon2Limits.MaximumMemorySizeKiB, Is.EqualTo(MaximumRfcLength));
            Assert.That(Argon2Limits.MinimumDegreeOfParallelism, Is.EqualTo(1));
            Assert.That(Argon2Limits.MaximumDegreeOfParallelism, Is.EqualTo(16777215));
            Assert.That(Argon2Limits.MinimumWorkerCount, Is.EqualTo(1));
            Assert.That(Argon2Limits.MaximumWorkerCount, Is.EqualTo(16777215));
        });
    }

    [Test]
    public void FirstRecommendedProfileMatchesRfc9106()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Argon2Profiles.FirstRecommendedVariant, Is.EqualTo(Argon2Variant.Id));
            Assert.That(Argon2Profiles.FirstRecommendedVersion, Is.EqualTo(Argon2Version.Version13));
            Assert.That(Argon2Profiles.FirstRecommendedIterations, Is.EqualTo(1L));
            Assert.That(Argon2Profiles.FirstRecommendedMemorySizeKiB, Is.EqualTo(2097152L));
            Assert.That(Argon2Profiles.FirstRecommendedDegreeOfParallelism, Is.EqualTo(4));
            Assert.That(Argon2Profiles.FirstRecommendedSaltLength, Is.EqualTo(16));
            Assert.That(Argon2Profiles.FirstRecommendedTagLength, Is.EqualTo(32));
        });
    }

    [Test]
    public void SecondRecommendedProfileMatchesRfc9106()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Argon2Profiles.SecondRecommendedVariant, Is.EqualTo(Argon2Variant.Id));
            Assert.That(Argon2Profiles.SecondRecommendedVersion, Is.EqualTo(Argon2Version.Version13));
            Assert.That(Argon2Profiles.SecondRecommendedIterations, Is.EqualTo(3L));
            Assert.That(Argon2Profiles.SecondRecommendedMemorySizeKiB, Is.EqualTo(65536L));
            Assert.That(Argon2Profiles.SecondRecommendedDegreeOfParallelism, Is.EqualTo(4));
            Assert.That(Argon2Profiles.SecondRecommendedSaltLength, Is.EqualTo(16));
            Assert.That(Argon2Profiles.SecondRecommendedTagLength, Is.EqualTo(32));
        });
    }
}
