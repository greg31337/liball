using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifPlane"/>.</summary>
public interface IAvifPlane
{
    /// <summary>Returns the plane state.</summary>
    /// <returns>The stable result code associated with this plane.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the width in samples.</summary>
    /// <returns>The visible sample width, or zero after closing.</returns>
    int GetWidth();

    /// <summary>Returns the height in samples.</summary>
    /// <returns>The visible sample height, or zero after closing.</returns>
    int GetHeight();

    /// <summary>Returns the row stride in bytes.</summary>
    /// <returns>The distance in bytes between adjacent rows.</returns>
    int GetStride();

    /// <summary>Returns the number of visible bytes in each row.</summary>
    /// <returns>The width multiplied by the bytes per sample.</returns>
    int GetRowByteCount();

    /// <summary>Returns the integer sample bit depth.</summary>
    /// <returns>The bit depth supplied at allocation.</returns>
    int GetBitDepth();

    /// <summary>Returns the semantic plane kind.</summary>
    /// <returns>The luma, chroma, or alpha kind.</returns>
    AvifPlaneKind GetKind();

    /// <summary>Returns whether this object owns its backing allocation.</summary>
    /// <returns><c>true</c> for an independently allocated plane; otherwise <c>false</c>.</returns>
    bool OwnsStorage();

    /// <summary>Returns the writable visible bytes for one row.</summary>
    /// <param name="row">The zero-based row index.</param>
    /// <returns>The visible row bytes, or an empty span when unavailable.</returns>
    Span<byte> GetRowSpan(int row);

    /// <summary>Creates a non-owning rectangular view of this plane.</summary>
    /// <param name="x">The left sample coordinate.</param>
    /// <param name="y">The top sample coordinate.</param>
    /// <param name="width">The view width in samples.</param>
    /// <param name="height">The view height in samples.</param>
    /// <returns>A plane sharing this plane's storage, or a plane carrying an error code.</returns>
    AvifPlane CreateView(int x, int y, int width, int height);

    /// <summary>Creates an independent tightly equivalent copy of visible samples.</summary>
    /// <returns>An owning plane copy, or a plane carrying an error code.</returns>
    AvifPlane CreateCopy();

    /// <summary>Copies every visible sample from a compatible source plane.</summary>
    /// <param name="source">The plane to copy.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode CopyFrom(AvifPlane? source);

    /// <summary>Closes this plane and releases owned storage.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
