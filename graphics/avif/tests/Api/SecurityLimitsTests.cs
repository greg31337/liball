namespace avif.Tests;

public class SecurityLimitsTests
{
    [Test]
    public void DefaultsCoverEveryPlannedResourceDimension()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaxSourceBytes, Is.EqualTo(AvifSecurityLimits.DefaultMaxSourceBytes));
            Assert.That(limits.MaxBoxDepth, Is.EqualTo(AvifSecurityLimits.DefaultMaxBoxDepth));
            Assert.That(limits.MaxBoxCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxBoxCount));
            Assert.That(limits.MaxItemCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxItemCount));
            Assert.That(limits.MaxPropertyCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxPropertyCount));
            Assert.That(limits.MaxReferenceCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxReferenceCount));
            Assert.That(limits.MaxTrackCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxTrackCount));
            Assert.That(limits.MaxSampleCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxSampleCount));
            Assert.That(limits.MaxFrameCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxFrameCount));
            Assert.That(limits.MaxWidth, Is.EqualTo(AvifSecurityLimits.DefaultMaxWidth));
            Assert.That(limits.MaxHeight, Is.EqualTo(AvifSecurityLimits.DefaultMaxHeight));
            Assert.That(limits.MaxPixels, Is.EqualTo(AvifSecurityLimits.DefaultMaxPixels));
            Assert.That(limits.MaxTileCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxTileCount));
            Assert.That(limits.MaxMetadataBytes, Is.EqualTo(AvifSecurityLimits.DefaultMaxMetadataBytes));
            Assert.That(limits.MaxIccBytes, Is.EqualTo(AvifSecurityLimits.DefaultMaxIccBytes));
            Assert.That(limits.MaxExifBytes, Is.EqualTo(AvifSecurityLimits.DefaultMaxExifBytes));
            Assert.That(limits.MaxXmpBytes, Is.EqualTo(AvifSecurityLimits.DefaultMaxXmpBytes));
            Assert.That(limits.MaxRecursionDepth, Is.EqualTo(AvifSecurityLimits.DefaultMaxRecursionDepth));
            Assert.That(limits.MaxDependencyDepth, Is.EqualTo(AvifSecurityLimits.DefaultMaxDependencyDepth));
            Assert.That(limits.MaxDecodedBytes, Is.EqualTo(AvifSecurityLimits.DefaultMaxDecodedBytes));
            Assert.That(limits.MaxTemporaryBytes, Is.EqualTo(AvifSecurityLimits.DefaultMaxTemporaryBytes));
            Assert.That(limits.MaxWorkerCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxWorkerCount));
        });
    }

    [Test]
    public void DefaultsAreFinitePositiveAndInternallyConsistent()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaxSourceBytes, Is.GreaterThan(0));
            Assert.That(limits.MaxBoxDepth, Is.GreaterThan(0));
            Assert.That(limits.MaxBoxCount, Is.GreaterThan(limits.MaxBoxDepth));
            Assert.That(limits.MaxReferenceCount, Is.GreaterThanOrEqualTo(limits.MaxItemCount));
            Assert.That(limits.MaxSampleCount, Is.GreaterThanOrEqualTo(limits.MaxFrameCount));
            Assert.That(limits.MaxPixels, Is.LessThanOrEqualTo((long)limits.MaxWidth * limits.MaxHeight));
            Assert.That(limits.MaxIccBytes, Is.LessThanOrEqualTo(limits.MaxMetadataBytes));
            Assert.That(limits.MaxExifBytes, Is.LessThanOrEqualTo(limits.MaxMetadataBytes));
            Assert.That(limits.MaxXmpBytes, Is.LessThanOrEqualTo(limits.MaxMetadataBytes));
            Assert.That(limits.MaxDecodedBytes, Is.GreaterThan(0));
            Assert.That(limits.MaxTemporaryBytes, Is.GreaterThan(0));
            Assert.That(limits.MaxWorkerCount, Is.EqualTo(1));
        });
    }
}
