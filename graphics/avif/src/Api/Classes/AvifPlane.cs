using System;

namespace avif;

/// <summary>
/// Owns or views one byte-addressable integer image plane with an explicit row stride.
/// </summary>
public class AvifPlane : IAvifPlane
{
    PlaneBuffer? buffer;
    int bitDepth;
    AvifPlaneKind kind;

    /// <summary>Allocates a tightly packed plane using the default decoded-byte limit.</summary>
    /// <param name="width">The plane width in samples.</param>
    /// <param name="height">The plane height in samples.</param>
    /// <param name="bitDepth">The integer sample depth: 8, 10, 12, or 16.</param>
    /// <param name="kind">The semantic plane kind.</param>
    /// <returns>The initialized plane.</returns>
    internal static AvifPlane Create(
        int width,
        int height,
        int bitDepth,
        AvifPlaneKind kind)
    {
        return new AvifPlane(
            width,
            height,
            bitDepth,
            kind,
            1,
            AvifSecurityLimits.DefaultMaxDecodedBytes);
    }

    /// <summary>Allocates a plane with an aligned stride and explicit byte limit.</summary>
    /// <param name="width">The plane width in samples.</param>
    /// <param name="height">The plane height in samples.</param>
    /// <param name="bitDepth">The integer sample depth: 8, 10, 12, or 16.</param>
    /// <param name="kind">The semantic plane kind.</param>
    /// <param name="alignment">The positive row-stride alignment in bytes.</param>
    /// <param name="maxBytes">The maximum allocation size in bytes.</param>
    public AvifPlane(
        int width,
        int height,
        int bitDepth,
        AvifPlaneKind kind,
        int alignment,
        long maxBytes)
    {
        Initialize(
            width,
            height,
            bitDepth,
            kind,
            alignment,
            maxBytes);
    }

    /// <summary>Allocates a plane with an aligned stride and explicit byte limit.</summary>
    /// <returns>The initialized plane.</returns>
    internal static AvifPlane Create(
        int width,
        int height,
        int bitDepth,
        AvifPlaneKind kind,
        int alignment,
        long maxBytes)
    {
        return new AvifPlane(
            width,
            height,
            bitDepth,
            kind,
            alignment,
            maxBytes);
    }

    internal static AvifPlane FromBuffer(
        PlaneBuffer buffer,
        int bitDepth,
        AvifPlaneKind kind)
    {
        AvifPlane result = new AvifPlane(
            0,
            0,
            8,
            kind,
            1,
            0);
        result.buffer = buffer;
        result.bitDepth = bitDepth;
        return result;
    }

    internal static AvifPlane FromCode(AvifResultCode code)
    {
        return FromBuffer(PlaneBuffer.FromCode(code), 0, AvifPlaneKind.Y);
    }

    /// <summary>Returns the plane state.</summary>
    /// <returns>The stable result code associated with this plane.</returns>
    public AvifResultCode GetCode()
    {
        return buffer == null
            ? AvifResultCode.Closed
            : buffer.GetStateCode();
    }

    /// <summary>Returns the width in samples.</summary>
    /// <returns>The visible sample width, or zero after closing.</returns>
    public int GetWidth()
    {
        return buffer == null ? 0 : buffer.Width;
    }

    /// <summary>Returns the height in samples.</summary>
    /// <returns>The visible sample height, or zero after closing.</returns>
    public int GetHeight()
    {
        return buffer == null ? 0 : buffer.Height;
    }

    /// <summary>Returns the row stride in bytes.</summary>
    /// <returns>The distance in bytes between adjacent rows.</returns>
    public int GetStride()
    {
        return buffer == null ? 0 : buffer.Stride;
    }

    /// <summary>Returns the number of visible bytes in each row.</summary>
    /// <returns>The width multiplied by the bytes per sample.</returns>
    public int GetRowByteCount()
    {
        return buffer == null ? 0 : buffer.Width * buffer.BytesPerSample;
    }

    /// <summary>Returns the integer sample bit depth.</summary>
    /// <returns>The bit depth supplied at allocation.</returns>
    public int GetBitDepth()
    {
        return bitDepth;
    }

    /// <summary>Returns the semantic plane kind.</summary>
    /// <returns>The luma, chroma, or alpha kind.</returns>
    public AvifPlaneKind GetKind()
    {
        return kind;
    }

    /// <summary>Returns whether this object owns its backing allocation.</summary>
    /// <returns><c>true</c> for an independently allocated plane; otherwise <c>false</c>.</returns>
    public bool OwnsStorage()
    {
        return buffer != null && buffer.OwnsBuffer;
    }

    /// <summary>Returns the writable visible bytes for one row.</summary>
    /// <param name="row">The zero-based row index.</param>
    /// <returns>The visible row bytes, or an empty span when unavailable.</returns>
    public Span<byte> GetRowSpan(int row)
    {
        return buffer == null ? Span<byte>.Empty : buffer.GetRowSpan(row);
    }

    /// <summary>Creates a non-owning rectangular view of this plane.</summary>
    /// <param name="x">The left sample coordinate.</param>
    /// <param name="y">The top sample coordinate.</param>
    /// <param name="width">The view width in samples.</param>
    /// <param name="height">The view height in samples.</param>
    /// <returns>A plane sharing this plane's storage, or a plane carrying an error code.</returns>
    public AvifPlane CreateView(int x, int y, int width, int height)
    {
        if (buffer == null)
        {
            return FromCode(AvifResultCode.Closed);
        }
        return FromBuffer(
            buffer.CreateView(x, y, width, height),
            bitDepth,
            kind);
    }

    /// <summary>Creates an independent tightly equivalent copy of visible samples.</summary>
    /// <returns>An owning plane copy, or a plane carrying an error code.</returns>
    public AvifPlane CreateCopy()
    {
        if (buffer == null)
        {
            return FromCode(AvifResultCode.Closed);
        }
        return FromBuffer(buffer.CreateCopy(), bitDepth, kind);
    }

    /// <summary>Copies every visible sample from a compatible source plane.</summary>
    /// <param name="source">The plane to copy.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode CopyFrom(AvifPlane? source)
    {
        if (buffer == null)
        {
            return AvifResultCode.Closed;
        }
        if (source == null || source.buffer == null
            || source.bitDepth != bitDepth || source.kind != kind)
        {
            return AvifResultCode.InvalidArgument;
        }
        return buffer.CopyFrom(source.buffer);
    }

    /// <summary>Closes this plane and releases owned storage.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (buffer == null)
        {
            return AvifResultCode.Success;
        }
        return buffer.Close();
    }

    internal PlaneBuffer? GetInternalBuffer()
    {
        return buffer;
    }

    void Initialize(
        int width,
        int height,
        int requestedBitDepth,
        AvifPlaneKind requestedKind,
        int alignment,
        long maxBytes)
    {
        bitDepth = requestedBitDepth;
        kind = requestedKind;
        if (!IsValidBitDepth(requestedBitDepth)
            || !IsValidKind(requestedKind))
        {
            buffer = PlaneBuffer.FromCode(AvifResultCode.InvalidArgument);
            return;
        }
        int bytesPerSample = requestedBitDepth > 8 ? 2 : 1;
        buffer = new PlaneBuffer(
            width,
            height,
            bytesPerSample,
            alignment,
            maxBytes,
            true);
    }

    static bool IsValidBitDepth(int value)
    {
        return value == 8 || value == 10 || value == 12 || value == 16;
    }

    static bool IsValidKind(AvifPlaneKind value)
    {
        int numeric = (int)value;
        return numeric >= (int)AvifPlaneKind.Y
            && numeric <= (int)AvifPlaneKind.Alpha;
    }
}
