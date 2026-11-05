namespace avif.Tests;

public class ResourceGuardTests
{
    [Test]
    public void GuardRejectsInvalidConfigurationAndSnapshotsValidLimits()
    {
        AvifSecurityLimits invalidLimits = new AvifSecurityLimits();
        invalidLimits.MaxItemCount = 0;
        ResourceGuard invalid = new ResourceGuard(invalidLimits);
        ResourceGuard missing = new ResourceGuard(null);

        AvifSecurityLimits original = CreateSmallLimits();
        original.MaxItemCount = 1;
        ResourceGuard snapshot = new ResourceGuard(original);
        original.MaxItemCount = 100;

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalid.CountItems(1), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(missing.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(snapshot.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(snapshot.CountItems(1), Is.EqualTo(AvifResultCode.Success));
            Assert.That(snapshot.CountItems(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(snapshot.ItemCount.Current, Is.EqualTo(1));
        });
    }

    [Test]
    public void BoxDepthAndTotalCountAreCheckedAtomically()
    {
        AvifSecurityLimits limits = CreateSmallLimits();
        limits.MaxBoxDepth = 2;
        limits.MaxBoxCount = 3;
        ResourceGuard guard = new ResourceGuard(limits);

        Assert.That(guard.EnterBox(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(guard.EnterBox(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(guard.EnterBox(), Is.EqualTo(AvifResultCode.LimitExceeded));
        Assert.Multiple(() =>
        {
            Assert.That(guard.BoxDepth.Current, Is.EqualTo(2));
            Assert.That(guard.BoxCount.Current, Is.EqualTo(2));
        });

        Assert.That(guard.LeaveBox(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(guard.LeaveBox(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(guard.EnterBox(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(guard.LeaveBox(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(guard.EnterBox(), Is.EqualTo(AvifResultCode.LimitExceeded));
        Assert.Multiple(() =>
        {
            Assert.That(guard.BoxDepth.Current, Is.Zero);
            Assert.That(guard.BoxCount.Current, Is.EqualTo(3));
            Assert.That(guard.LeaveBox(), Is.EqualTo(AvifResultCode.InvalidState));
        });
    }

    [Test]
    public void StructuralCountersAcceptExactLimitsWithoutPartialOverrun()
    {
        ResourceGuard guard = new ResourceGuard(CreateSmallLimits());

        Assert.Multiple(() =>
        {
            Assert.That(guard.CountItems(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountItems(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountProperties(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountProperties(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountReferences(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountReferences(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountTracks(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountTracks(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountSamples(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountSamples(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountFrames(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountFrames(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountItems(-1), Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        Assert.Multiple(() =>
        {
            Assert.That(guard.ItemCount.Current, Is.EqualTo(2));
            Assert.That(guard.PropertyCount.Current, Is.EqualTo(2));
            Assert.That(guard.ReferenceCount.Current, Is.EqualTo(2));
            Assert.That(guard.TrackCount.Current, Is.EqualTo(2));
            Assert.That(guard.SampleCount.Current, Is.EqualTo(2));
            Assert.That(guard.FrameCount.Current, Is.EqualTo(2));
        });
    }

    [Test]
    public void SourceImageAndPerFrameTileLimitsAreIndependent()
    {
        AvifSecurityLimits limits = CreateSmallLimits();
        limits.MaxSourceBytes = 10;
        limits.MaxWidth = 4;
        limits.MaxHeight = 3;
        limits.MaxPixels = 8;
        limits.MaxTileCount = 2;
        ResourceGuard guard = new ResourceGuard(limits);

        Assert.Multiple(() =>
        {
            Assert.That(guard.CountSourceBytes(10), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountSourceBytes(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CheckImageDimensions(4, 2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CheckImageDimensions(4, 3), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CheckImageDimensions(5, 1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CheckImageDimensions(0, 1), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(guard.CountTiles(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountTiles(1), Is.EqualTo(AvifResultCode.LimitExceeded));
        });

        guard.BeginTileSet();
        Assert.That(guard.CountTiles(2), Is.EqualTo(AvifResultCode.Success));
    }

    [Test]
    public void SpecializedMetadataAlsoConsumesTheCombinedBudget()
    {
        AvifSecurityLimits limits = CreateSmallLimits();
        limits.MaxMetadataBytes = 10;
        limits.MaxIccBytes = 8;
        limits.MaxExifBytes = 7;
        limits.MaxXmpBytes = 6;
        ResourceGuard guard = new ResourceGuard(limits);

        Assert.Multiple(() =>
        {
            Assert.That(guard.CountIccBytes(8), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountExifBytes(8), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountXmpBytes(3), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountXmpBytes(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.CountMetadataBytes(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.CountIccBytes(-1), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(guard.MetadataBytes.Current, Is.EqualTo(10));
        });
    }

    [Test]
    public void DepthStorageAndWorkerReservationsCanBeReleasedAndReused()
    {
        AvifSecurityLimits limits = CreateSmallLimits();
        limits.MaxDecodedBytes = 12;
        limits.MaxTemporaryBytes = 8;
        limits.MaxWorkerCount = 2;
        ResourceGuard guard = new ResourceGuard(limits);

        Assert.Multiple(() =>
        {
            Assert.That(guard.EnterRecursion(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.EnterRecursion(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.EnterRecursion(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.EnterDependency(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.EnterDependency(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.EnterDependency(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.ReserveDecodedBytes(12), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.ReserveDecodedBytes(1), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(guard.ReserveTemporaryBytes(8), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.AcquireWorkers(2), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.AcquireWorkers(1), Is.EqualTo(AvifResultCode.LimitExceeded));
        });

        Assert.Multiple(() =>
        {
            Assert.That(guard.LeaveRecursion(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.EnterRecursion(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.LeaveDependency(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.EnterDependency(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.ReleaseDecodedBytes(5), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.ReserveDecodedBytes(5), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.ReleaseTemporaryBytes(8), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.ReserveTemporaryBytes(8), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.ReleaseWorkers(1), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.AcquireWorkers(1), Is.EqualTo(AvifResultCode.Success));
            Assert.That(guard.ReleaseWorkers(3), Is.EqualTo(AvifResultCode.InvalidState));
        });
    }

    private static AvifSecurityLimits CreateSmallLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 16;
        limits.MaxBoxDepth = 2;
        limits.MaxBoxCount = 2;
        limits.MaxItemCount = 2;
        limits.MaxPropertyCount = 2;
        limits.MaxReferenceCount = 2;
        limits.MaxTrackCount = 2;
        limits.MaxSampleCount = 2;
        limits.MaxFrameCount = 2;
        limits.MaxWidth = 4;
        limits.MaxHeight = 4;
        limits.MaxPixels = 16;
        limits.MaxTileCount = 2;
        limits.MaxMetadataBytes = 16;
        limits.MaxIccBytes = 16;
        limits.MaxExifBytes = 16;
        limits.MaxXmpBytes = 16;
        limits.MaxRecursionDepth = 2;
        limits.MaxDependencyDepth = 2;
        limits.MaxDecodedBytes = 16;
        limits.MaxTemporaryBytes = 16;
        limits.MaxWorkerCount = 2;
        return limits;
    }
}
