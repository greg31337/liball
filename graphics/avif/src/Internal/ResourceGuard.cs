namespace avif;

internal class ResourceCounter
{
    internal long Current;
    internal long Limit;

    internal ResourceCounter(long limit)
    {
        Current = 0;
        Limit = limit;
    }

    internal bool CanAdd(long amount)
    {
        if (amount < 0 || Limit < 0)
        {
            return false;
        }

        return Current <= Limit && amount <= Limit - Current;
    }

    internal AvifResultCode Add(long amount)
    {
        if (amount < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (!CanAdd(amount))
        {
            return AvifResultCode.LimitExceeded;
        }

        Current += amount;
        return AvifResultCode.Success;
    }

    internal AvifResultCode Release(long amount)
    {
        if (amount < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (amount > Current)
        {
            return AvifResultCode.InvalidState;
        }

        Current -= amount;
        return AvifResultCode.Success;
    }

    internal void AddUnchecked(long amount)
    {
        Current += amount;
    }

    internal void Reset()
    {
        Current = 0;
    }
}

internal class ResourceGuard
{
    internal AvifSecurityLimits Limits;
    internal AvifResultCode Code;
    internal ResourceCounter SourceBytes;
    internal ResourceCounter BoxDepth;
    internal ResourceCounter BoxCount;
    internal ResourceCounter ItemCount;
    internal ResourceCounter PropertyCount;
    internal ResourceCounter ReferenceCount;
    internal ResourceCounter TrackCount;
    internal ResourceCounter SampleCount;
    internal ResourceCounter FrameCount;
    internal ResourceCounter TileCount;
    internal ResourceCounter MetadataBytes;
    internal ResourceCounter RecursionDepth;
    internal ResourceCounter DependencyDepth;
    internal ResourceCounter DecodedBytes;
    internal ResourceCounter TemporaryBytes;
    internal ResourceCounter WorkerCount;

    internal ResourceGuard(AvifSecurityLimits? limits)
    {
        Limits = CopyLimits(limits);
        Code = ValidateLimits(limits);
        SourceBytes = new ResourceCounter(Limits.MaxSourceBytes);
        BoxDepth = new ResourceCounter(Limits.MaxBoxDepth);
        BoxCount = new ResourceCounter(Limits.MaxBoxCount);
        ItemCount = new ResourceCounter(Limits.MaxItemCount);
        PropertyCount = new ResourceCounter(Limits.MaxPropertyCount);
        ReferenceCount = new ResourceCounter(Limits.MaxReferenceCount);
        TrackCount = new ResourceCounter(Limits.MaxTrackCount);
        SampleCount = new ResourceCounter(Limits.MaxSampleCount);
        FrameCount = new ResourceCounter(Limits.MaxFrameCount);
        TileCount = new ResourceCounter(Limits.MaxTileCount);
        MetadataBytes = new ResourceCounter(Limits.MaxMetadataBytes);
        RecursionDepth = new ResourceCounter(Limits.MaxRecursionDepth);
        DependencyDepth = new ResourceCounter(Limits.MaxDependencyDepth);
        DecodedBytes = new ResourceCounter(Limits.MaxDecodedBytes);
        TemporaryBytes = new ResourceCounter(Limits.MaxTemporaryBytes);
        WorkerCount = new ResourceCounter(Limits.MaxWorkerCount);
    }

    internal AvifResultCode CountSourceBytes(long amount)
    {
        return Add(SourceBytes, amount);
    }

    internal AvifResultCode EnterBox()
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (!BoxDepth.CanAdd(1) || !BoxCount.CanAdd(1))
        {
            return AvifResultCode.LimitExceeded;
        }

        BoxDepth.AddUnchecked(1);
        BoxCount.AddUnchecked(1);
        return AvifResultCode.Success;
    }

    internal AvifResultCode LeaveBox()
    {
        return Release(BoxDepth, 1);
    }

    internal AvifResultCode CountItems(int amount)
    {
        return Add(ItemCount, amount);
    }

    internal AvifResultCode CountProperties(int amount)
    {
        return Add(PropertyCount, amount);
    }

    internal AvifResultCode CountReferences(int amount)
    {
        return Add(ReferenceCount, amount);
    }

    internal AvifResultCode CountTracks(int amount)
    {
        return Add(TrackCount, amount);
    }

    internal AvifResultCode CountSamples(int amount)
    {
        return Add(SampleCount, amount);
    }

    internal AvifResultCode CountFrames(int amount)
    {
        return Add(FrameCount, amount);
    }

    internal void BeginTileSet()
    {
        TileCount.Reset();
    }

    internal AvifResultCode CountTiles(int amount)
    {
        return Add(TileCount, amount);
    }

    internal AvifResultCode CheckImageDimensions(int width, int height)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (width <= 0 || height <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (width > Limits.MaxWidth || height > Limits.MaxHeight)
        {
            return AvifResultCode.LimitExceeded;
        }

        CheckedIntegerResult pixels = CheckedMath.ComputePixelCount(width, height, Limits.MaxPixels);
        return pixels.Code;
    }

    internal AvifResultCode CountMetadataBytes(long amount)
    {
        return Add(MetadataBytes, amount);
    }

    internal AvifResultCode CountIccBytes(long amount)
    {
        return CountSpecializedMetadata(amount, Limits.MaxIccBytes);
    }

    internal AvifResultCode CountExifBytes(long amount)
    {
        return CountSpecializedMetadata(amount, Limits.MaxExifBytes);
    }

    internal AvifResultCode CountXmpBytes(long amount)
    {
        return CountSpecializedMetadata(amount, Limits.MaxXmpBytes);
    }

    internal AvifResultCode EnterRecursion()
    {
        return Add(RecursionDepth, 1);
    }

    internal AvifResultCode LeaveRecursion()
    {
        return Release(RecursionDepth, 1);
    }

    internal AvifResultCode EnterDependency()
    {
        return Add(DependencyDepth, 1);
    }

    internal AvifResultCode LeaveDependency()
    {
        return Release(DependencyDepth, 1);
    }

    internal AvifResultCode ReserveDecodedBytes(long amount)
    {
        return Add(DecodedBytes, amount);
    }

    internal AvifResultCode ReleaseDecodedBytes(long amount)
    {
        return Release(DecodedBytes, amount);
    }

    internal AvifResultCode ReserveTemporaryBytes(long amount)
    {
        return Add(TemporaryBytes, amount);
    }

    internal AvifResultCode ReleaseTemporaryBytes(long amount)
    {
        return Release(TemporaryBytes, amount);
    }

    internal AvifResultCode AcquireWorkers(int amount)
    {
        return Add(WorkerCount, amount);
    }

    internal AvifResultCode ReleaseWorkers(int amount)
    {
        return Release(WorkerCount, amount);
    }

    internal AvifResultCode CountSpecializedMetadata(long amount, long specializedLimit)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (amount < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (amount > specializedLimit)
        {
            return AvifResultCode.LimitExceeded;
        }

        return MetadataBytes.Add(amount);
    }

    internal AvifResultCode Add(ResourceCounter counter, long amount)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        return counter.Add(amount);
    }

    internal AvifResultCode Release(ResourceCounter counter, long amount)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        return counter.Release(amount);
    }

    internal static AvifResultCode ValidateLimits(AvifSecurityLimits? limits)
    {
        if (limits == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (limits.MaxSourceBytes <= 0
            || limits.MaxBoxDepth <= 0
            || limits.MaxBoxCount <= 0
            || limits.MaxItemCount <= 0
            || limits.MaxPropertyCount <= 0
            || limits.MaxReferenceCount <= 0
            || limits.MaxTrackCount <= 0
            || limits.MaxSampleCount <= 0
            || limits.MaxFrameCount <= 0
            || limits.MaxWidth <= 0
            || limits.MaxHeight <= 0
            || limits.MaxPixels <= 0
            || limits.MaxTileCount <= 0
            || limits.MaxMetadataBytes <= 0
            || limits.MaxIccBytes <= 0
            || limits.MaxExifBytes <= 0
            || limits.MaxXmpBytes <= 0
            || limits.MaxRecursionDepth <= 0
            || limits.MaxDependencyDepth <= 0
            || limits.MaxDecodedBytes <= 0
            || limits.MaxTemporaryBytes <= 0
            || limits.MaxWorkerCount <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        return AvifResultCode.Success;
    }

    internal static AvifSecurityLimits CopyLimits(AvifSecurityLimits? source)
    {
        AvifSecurityLimits copy = new AvifSecurityLimits();
        if (source == null)
        {
            return copy;
        }

        copy.MaxSourceBytes = source.MaxSourceBytes;
        copy.MaxBoxDepth = source.MaxBoxDepth;
        copy.MaxBoxCount = source.MaxBoxCount;
        copy.MaxItemCount = source.MaxItemCount;
        copy.MaxPropertyCount = source.MaxPropertyCount;
        copy.MaxReferenceCount = source.MaxReferenceCount;
        copy.MaxTrackCount = source.MaxTrackCount;
        copy.MaxSampleCount = source.MaxSampleCount;
        copy.MaxFrameCount = source.MaxFrameCount;
        copy.MaxWidth = source.MaxWidth;
        copy.MaxHeight = source.MaxHeight;
        copy.MaxPixels = source.MaxPixels;
        copy.MaxTileCount = source.MaxTileCount;
        copy.MaxMetadataBytes = source.MaxMetadataBytes;
        copy.MaxIccBytes = source.MaxIccBytes;
        copy.MaxExifBytes = source.MaxExifBytes;
        copy.MaxXmpBytes = source.MaxXmpBytes;
        copy.MaxRecursionDepth = source.MaxRecursionDepth;
        copy.MaxDependencyDepth = source.MaxDependencyDepth;
        copy.MaxDecodedBytes = source.MaxDecodedBytes;
        copy.MaxTemporaryBytes = source.MaxTemporaryBytes;
        copy.MaxWorkerCount = source.MaxWorkerCount;
        return copy;
    }
}
