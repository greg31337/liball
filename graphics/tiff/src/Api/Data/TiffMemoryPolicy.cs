using System.Buffers;

namespace tiff;

/// <summary>Controls memory limits, deferred metadata, virtual strip chopping, reusable segment buffers, and source loading.</summary>
/// <remarks>Maximum-size values use the documented <see cref="TiffLimits"/> defaults. A value that differs from its default overrides the corresponding legacy limit.</remarks>
public class TiffMemoryPolicy
{
    /// <summary>Contains the maximum size of one library-owned allocation in bytes.</summary>
    public long MaximumAllocationBytes;
    /// <summary>Contains the maximum decoded image-data size in bytes.</summary>
    public long MaximumDecodedBytes;
    /// <summary>Contains the minimum external tag-value size eligible for deferred loading. A negative value disables deferred loading.</summary>
    public long LazyTagValueByteThreshold;
    /// <summary>Contains the preferred maximum decoded size of virtual strips. Zero disables automatic chopped-strip views.</summary>
    public ulong ChoppedStripByteCount;
    /// <summary>Indicates whether temporary encoded segment buffers are rented from an array pool.</summary>
    public bool UsePooledSegmentBuffers;
    /// <summary>Contains the array pool used for temporary encoded segment buffers. A null value selects <see cref="ArrayPool{T}.Shared"/>.</summary>
    public ArrayPool<byte>? SegmentBufferPool;
    /// <summary>Specifies whether a random-access source is retained or copied into memory during open.</summary>
    public TiffSourceLoadingMode SourceLoadingMode;

    /// <summary>Creates a memory policy with bounded allocations, deferred large tags, streaming source access, and no pooled or chopped segments.</summary>
    public TiffMemoryPolicy()
    {
        MaximumAllocationBytes = TiffLimits.DefaultMaximumAllocationBytes;
        MaximumDecodedBytes = TiffLimits.DefaultMaximumDecodedBytes;
        LazyTagValueByteThreshold = 4096;
        ChoppedStripByteCount = 0;
        UsePooledSegmentBuffers = false;
        SegmentBufferPool = null;
        SourceLoadingMode = TiffSourceLoadingMode.Streaming;
    }

    internal TiffMemoryPolicy Copy()
    {
        TiffMemoryPolicy copy = new TiffMemoryPolicy();
        copy.MaximumAllocationBytes = MaximumAllocationBytes;
        copy.MaximumDecodedBytes = MaximumDecodedBytes;
        copy.LazyTagValueByteThreshold = LazyTagValueByteThreshold;
        copy.ChoppedStripByteCount = ChoppedStripByteCount;
        copy.UsePooledSegmentBuffers = UsePooledSegmentBuffers;
        copy.SegmentBufferPool = SegmentBufferPool;
        copy.SourceLoadingMode = SourceLoadingMode;
        return copy;
    }

    internal TiffLimits ApplyTo(TiffLimits limits)
    {
        TiffLimits effective = limits == null ? new TiffLimits() : limits.Copy();
        if (MaximumAllocationBytes != TiffLimits.DefaultMaximumAllocationBytes)
        {
            effective.MaximumAllocationBytes = MaximumAllocationBytes;
        }

        if (MaximumDecodedBytes != TiffLimits.DefaultMaximumDecodedBytes)
        {
            effective.MaximumDecodedBytes = MaximumDecodedBytes;
        }

        effective.UsePooledSegmentBuffers = UsePooledSegmentBuffers;
        effective.SegmentBufferPool = SegmentBufferPool;
        return effective;
    }
}
