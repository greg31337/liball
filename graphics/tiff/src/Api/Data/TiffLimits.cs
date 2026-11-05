using System.Buffers;

namespace tiff;

/// <summary>Defines allocation, decoded-size, dimension, directory, tag, and nesting safety limits.</summary>
public class TiffLimits
{
    /// <summary>Defines the default maximum allocation bytes value used by this API.</summary>
    public const long DefaultMaximumAllocationBytes = 268435456;
    /// <summary>Defines the default maximum decoded bytes value used by this API.</summary>
    public const long DefaultMaximumDecodedBytes = 1073741824;
    /// <summary>Defines the default maximum codec expansion ratio value used by this API.</summary>
    public const long DefaultMaximumCodecExpansionRatio = 1000000;
    /// <summary>Defines the default maximum image width value used by this API.</summary>
    public const int DefaultMaximumImageWidth = 1000000;
    /// <summary>Defines the default maximum image length value used by this API.</summary>
    public const int DefaultMaximumImageLength = 1000000;
    /// <summary>Defines the default maximum image pixels value used by this API.</summary>
    public const long DefaultMaximumImagePixels = 1000000000;
    /// <summary>Defines the default maximum IFD count value used by this API.</summary>
    public const int DefaultMaximumIfdCount = 65536;
    /// <summary>Defines the default maximum tag count per IFD value used by this API.</summary>
    public const int DefaultMaximumTagCountPerIfd = 4096;
    /// <summary>Defines the default maximum sub IFD depth value used by this API.</summary>
    public const int DefaultMaximumSubIfdDepth = 16;

    /// <summary>Contains the maximum allocation bytes.</summary>
    public long MaximumAllocationBytes;
    /// <summary>Contains the maximum decoded bytes.</summary>
    public long MaximumDecodedBytes;
    /// <summary>Contains the maximum codec expansion ratio.</summary>
    public long MaximumCodecExpansionRatio;
    /// <summary>Contains the maximum image width.</summary>
    public int MaximumImageWidth;
    /// <summary>Contains the maximum image length.</summary>
    public int MaximumImageLength;
    /// <summary>Contains the maximum image pixels.</summary>
    public long MaximumImagePixels;
    /// <summary>Contains the maximum IFD count.</summary>
    public int MaximumIfdCount;
    /// <summary>Contains the maximum tag count per IFD.</summary>
    public int MaximumTagCountPerIfd;
    /// <summary>Contains the maximum sub IFD depth.</summary>
    public int MaximumSubIfdDepth;
    internal bool UsePooledSegmentBuffers;
    internal ArrayPool<byte>? SegmentBufferPool;

    /// <summary>Creates a new instance of the TIFF Limits class.</summary>
    public TiffLimits()
    {
        MaximumAllocationBytes = DefaultMaximumAllocationBytes;
        MaximumDecodedBytes = DefaultMaximumDecodedBytes;
        MaximumCodecExpansionRatio = DefaultMaximumCodecExpansionRatio;
        MaximumImageWidth = DefaultMaximumImageWidth;
        MaximumImageLength = DefaultMaximumImageLength;
        MaximumImagePixels = DefaultMaximumImagePixels;
        MaximumIfdCount = DefaultMaximumIfdCount;
        MaximumTagCountPerIfd = DefaultMaximumTagCountPerIfd;
        MaximumSubIfdDepth = DefaultMaximumSubIfdDepth;
        UsePooledSegmentBuffers = false;
        SegmentBufferPool = null;
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffLimits CreateDefault()
    {
        return new TiffLimits();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF limits.</returns>
    internal TiffLimits Copy()
    {
        TiffLimits copy = new TiffLimits();
        copy.MaximumAllocationBytes = MaximumAllocationBytes;
        copy.MaximumDecodedBytes = MaximumDecodedBytes;
        copy.MaximumCodecExpansionRatio = MaximumCodecExpansionRatio;
        copy.MaximumImageWidth = MaximumImageWidth;
        copy.MaximumImageLength = MaximumImageLength;
        copy.MaximumImagePixels = MaximumImagePixels;
        copy.MaximumIfdCount = MaximumIfdCount;
        copy.MaximumTagCountPerIfd = MaximumTagCountPerIfd;
        copy.MaximumSubIfdDepth = MaximumSubIfdDepth;
        copy.UsePooledSegmentBuffers = UsePooledSegmentBuffers;
        copy.SegmentBufferPool = SegmentBufferPool;
        return copy;
    }

    /// <summary>Determines whether allows allocation under the current TIFF data and policy.</summary>
    /// <param name="byteCount">The byte count.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsAllocation(long byteCount)
    {
        if (byteCount < 0)
        {
            return false;
        }

        return byteCount <= MaximumAllocationBytes;
    }

    /// <summary>Determines whether allows decoded bytes under the current TIFF data and policy.</summary>
    /// <param name="byteCount">The byte count.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsDecodedBytes(long byteCount)
    {
        if (byteCount < 0)
        {
            return false;
        }

        return byteCount <= MaximumDecodedBytes;
    }

    /// <summary>Determines whether allows codec expansion under the current TIFF data and policy.</summary>
    /// <param name="encodedByteCount">The encoded byte count.</param>
    /// <param name="decodedByteCount">The decoded byte count.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsCodecExpansion(long encodedByteCount, long decodedByteCount)
    {
        if (encodedByteCount < 0 || decodedByteCount < 0)
        {
            return false;
        }

        if (decodedByteCount == 0)
        {
            return true;
        }

        if (encodedByteCount == 0)
        {
            return false;
        }

        if (MaximumCodecExpansionRatio <= 0)
        {
            return false;
        }

        if (encodedByteCount > long.MaxValue / MaximumCodecExpansionRatio)
        {
            return true;
        }

        return decodedByteCount <= encodedByteCount * MaximumCodecExpansionRatio;
    }

    /// <summary>Determines whether allows image dimensions under the current TIFF data and policy.</summary>
    /// <param name="width">The width.</param>
    /// <param name="length">The length.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsImageDimensions(int width, int length)
    {
        if (width <= 0 || length <= 0)
        {
            return false;
        }

        if (width > MaximumImageWidth || length > MaximumImageLength)
        {
            return false;
        }

        if ((long)width > MaximumImagePixels / (long)length)
        {
            return false;
        }

        return true;
    }

    /// <summary>Returns the number of available allows IFD entries.</summary>
    /// <param name="count">The count.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsIfdCount(int count)
    {
        if (count < 0)
        {
            return false;
        }

        return count <= MaximumIfdCount;
    }

    /// <summary>Determines whether allows tag count per IFD under the current TIFF data and policy.</summary>
    /// <param name="count">The count.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsTagCountPerIfd(int count)
    {
        if (count < 0)
        {
            return false;
        }

        return count <= MaximumTagCountPerIfd;
    }

    /// <summary>Determines whether allows sub IFD depth under the current TIFF data and policy.</summary>
    /// <param name="depth">The depth.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsSubIfdDepth(int depth)
    {
        if (depth < 0)
        {
            return false;
        }

        return depth <= MaximumSubIfdDepth;
    }
}
