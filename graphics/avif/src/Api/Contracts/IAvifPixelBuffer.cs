using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifPixelBuffer"/>.</summary>
public interface IAvifPixelBuffer
{
    /// <summary>Returns the current buffer state.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the width in pixels.</summary>
    /// <returns>The visible width, or zero after closing.</returns>
    int GetWidth();

    /// <summary>Returns the height in pixels.</summary>
    /// <returns>The visible height, or zero after closing.</returns>
    int GetHeight();

    /// <summary>Returns the row stride in bytes.</summary>
    /// <returns>The distance between adjacent packed rows.</returns>
    int GetStride();

    /// <summary>Returns the visible byte count in one row.</summary>
    /// <returns>The width multiplied by the packed pixel size.</returns>
    int GetRowByteCount();

    /// <summary>Returns the byte size of one pixel.</summary>
    /// <returns>The packed pixel size.</returns>
    int GetBytesPerPixel();

    /// <summary>Returns the component count.</summary>
    /// <returns>One through four components, or three logical components for RGB565.</returns>
    int GetChannelCount();

    /// <summary>Returns the component bit depth.</summary>
    /// <returns>The configured integer depth, or 16 for half float.</returns>
    int GetBitDepth();

    /// <summary>Returns the packed component order.</summary>
    /// <returns>The pixel format.</returns>
    AvifPixelFormat GetPixelFormat();

    /// <summary>Returns whether components contain linear half-float values.</summary>
    /// <returns><c>true</c> for half-float RGB/RGBA; otherwise <c>false</c>.</returns>
    bool IsFloatingPoint();

    /// <summary>Returns whether the packed layout contains alpha.</summary>
    /// <returns><c>true</c> for a two- or four-component alpha layout.</returns>
    bool HasAlpha();

    /// <summary>Returns whether this object owns its pixel allocation.</summary>
    /// <returns><c>true</c> for allocated roots; <c>false</c> for caller buffers and views.</returns>
    bool OwnsStorage();

    /// <summary>Returns one writable visible pixel row.</summary>
    /// <param name="row">The zero-based row index.</param>
    /// <returns>The visible row bytes, or an empty span when unavailable.</returns>
    Span<byte> GetRowSpan(int row);

    /// <summary>Returns the accessible bytes through the end of the last visible row.</summary>
    /// <returns>A span including inter-row padding, or an empty span when unavailable.</returns>
    Span<byte> GetSpan();

    /// <summary>Creates a non-owning rectangular packed-pixel view.</summary>
    /// <param name="x">The left pixel coordinate.</param>
    /// <param name="y">The top pixel coordinate.</param>
    /// <param name="width">The view width in pixels.</param>
    /// <param name="height">The view height in pixels.</param>
    /// <returns>A shared view, or a buffer carrying an error code.</returns>
    AvifPixelBuffer CreateView(int x, int y, int width, int height);

    /// <summary>Creates an independent copy of all visible packed pixels.</summary>
    /// <returns>An owning packed buffer copy, or a buffer carrying an error code.</returns>
    AvifPixelBuffer CreateCopy();

    /// <summary>Converts between straight and premultiplied alpha representations.</summary>
    /// <param name="sourcePremultiplied">Whether the current color components are premultiplied.</param>
    /// <param name="outputPremultiplied">Whether output color components should be premultiplied.</param>
    /// <returns>An owning converted copy or a buffer carrying an error code.</returns>
    AvifPixelBuffer ConvertAlphaMode(
        bool sourcePremultiplied,
        bool outputPremultiplied);

    /// <summary>Composites these foreground pixels over another packed buffer.</summary>
    /// <param name="background">The normalized straight or premultiplied background pixels.</param>
    /// <param name="foregroundPremultiplied">Whether this buffer's color is premultiplied.</param>
    /// <param name="backgroundPremultiplied">Whether background color is premultiplied.</param>
    /// <param name="outputPremultiplied">Whether output color should remain premultiplied.</param>
    /// <returns>An owning source-over result or a buffer carrying an error code.</returns>
    AvifPixelBuffer CompositeOver(
        AvifPixelBuffer background,
        bool foregroundPremultiplied,
        bool backgroundPremultiplied,
        bool outputPremultiplied);

    /// <summary>Copies every visible row from a layout-compatible packed buffer.</summary>
    /// <param name="source">The source packed pixels.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode CopyFrom(AvifPixelBuffer source);

    /// <summary>Closes this buffer and releases owned storage.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
