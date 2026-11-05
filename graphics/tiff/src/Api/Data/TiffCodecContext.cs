namespace tiff;

/// <summary>Supplies image layout, segment identity, byte order, and safety limits to a compression operation.</summary>
public class TiffCodecContext
{
    /// <summary>Contains the normalized image layout and pixel information.</summary>
    public TiffImageInfo Info;
    /// <summary>Specifies the byte order used for multibyte values.</summary>
    public TiffByteOrder ByteOrder;
    /// <summary>Indicates whether the codec context describes a tile rather than a strip.</summary>
    public bool IsTile;
    /// <summary>Identifies the zero-based strip or tile being processed.</summary>
    public ulong SegmentIndex;
    /// <summary>Specifies the exact decoded segment size expected by the codec and its expansion checks.</summary>
    public ulong ExpectedDecodedByteCount;
    /// <summary>Specifies the resource limits enforced by the operation.</summary>
    public TiffLimits Limits;

    /// <summary>Creates a new instance of the TIFF Codec Context class.</summary>
    public TiffCodecContext()
    {
        Info = new TiffImageInfo();
        ByteOrder = TiffByteOrder.LittleEndian;
        IsTile = false;
        SegmentIndex = 0;
        ExpectedDecodedByteCount = 0;
        Limits = new TiffLimits();
    }

    /// <summary>Creates a new instance of the TIFF Codec Context class.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="isTile">Whether tile applies.</param>
    /// <param name="segmentIndex">The segment index.</param>
    /// <param name="expectedDecodedByteCount">The expected decoded byte count.</param>
    /// <param name="limits">The allocation and decoded-size limits to enforce.</param>
    internal static TiffCodecContext Create(TiffImageInfo info, bool isTile, ulong segmentIndex, ulong expectedDecodedByteCount, TiffLimits limits)
    {
        TiffCodecContext value = new TiffCodecContext();
        value.Initialize(info, isTile, segmentIndex, expectedDecodedByteCount, limits);
        return value;
    }

    internal void Initialize(TiffImageInfo info, bool isTile, ulong segmentIndex, ulong expectedDecodedByteCount, TiffLimits limits)
    {
        if (info == null)
        {
            Info = new TiffImageInfo();
        }
        else
        {
            Info = info.Copy();
        }

        IsTile = isTile;
        ByteOrder = TiffByteOrder.LittleEndian;
        SegmentIndex = segmentIndex;
        ExpectedDecodedByteCount = expectedDecodedByteCount;
        Limits = TiffTagValueLoader.NormalizeLimits(limits);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF codec context.</returns>
    internal TiffCodecContext Copy()
    {
        TiffCodecContext copy = TiffCodecContext.Create(Info, IsTile, SegmentIndex, ExpectedDecodedByteCount, Limits.Copy());
        copy.ByteOrder = ByteOrder;
        return copy;
    }
}
