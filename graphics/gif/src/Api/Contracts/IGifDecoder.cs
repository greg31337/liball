using System;
using System.IO;

namespace gif;

/// <summary>Defines GIF identification, structural decoding, and animation decoding operations.</summary>
public interface IGifDecoder
{
    /// <summary>Decodes GIF bytes into a raw document.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The decoded document.</returns>
    /// <exception cref="GifException">Thrown when a fatal decode diagnostic is produced.</exception>
    GifDocument DecodeBytes(byte[] data, GifDecodeOptions? options);

    /// <summary>Decodes GIF memory into a raw document without copying the source region.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The decoded document.</returns>
    /// <exception cref="GifException">Thrown when a fatal decode diagnostic is produced.</exception>
    GifDocument DecodeMemory(ReadOnlyMemory<byte> data, GifDecodeOptions? options);

    /// <summary>Reads a GIF stream into a raw document without closing it.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The decoded document.</returns>
    /// <exception cref="GifException">Thrown when a fatal decode diagnostic is produced.</exception>
    GifDocument DecodeStream(Stream stream, GifDecodeOptions? options);

    /// <summary>Reads metadata first, then decodes only one raw indexed frame from GIF bytes.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>A metadata-complete document where only the selected frame retains compressed and indexed pixels.</returns>
    /// <exception cref="GifException">Thrown when metadata, frame selection, or raw frame decoding produces a fatal diagnostic.</exception>
    GifDocument DecodeFrameBytes(byte[] data, int frameIndex, GifDecodeOptions? options);

    /// <summary>Reads metadata first, then decodes only one raw indexed frame from GIF memory.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>A metadata-complete document where only the selected frame retains compressed and indexed pixels.</returns>
    /// <exception cref="GifException">Thrown when metadata, frame selection, or raw frame decoding produces a fatal diagnostic.</exception>
    GifDocument DecodeFrameMemory(ReadOnlyMemory<byte> data, int frameIndex, GifDecodeOptions? options);

    /// <summary>Reads metadata first, seeks back, then decodes only one raw indexed frame from a stream without closing it.</summary>
    /// <param name="stream">The readable, seekable GIF source stream.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>A metadata-complete document where only the selected frame retains compressed and indexed pixels.</returns>
    /// <exception cref="GifException">Thrown when the stream is not seekable or metadata, frame selection, or raw frame decoding produces a fatal diagnostic.</exception>
    GifDocument DecodeFrameStream(Stream stream, int frameIndex, GifDecodeOptions? options);

    /// <summary>Decodes one raw indexed frame from GIF bytes into caller-provided storage.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="destination">Storage for one indexed byte per pixel in the selected frame rectangle.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of indexed pixel bytes written.</returns>
    /// <exception cref="GifException">Thrown when decoding fails or the destination is smaller than the selected frame's pixel count.</exception>
    int DecodeFrameBytesToSpan(byte[] data, int frameIndex, Span<byte> destination, GifDecodeOptions? options);

    /// <summary>Decodes one raw indexed frame from GIF memory into caller-provided storage.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="destination">Storage for one indexed byte per pixel in the selected frame rectangle.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of indexed pixel bytes written.</returns>
    /// <exception cref="GifException">Thrown when decoding fails or the destination is smaller than the selected frame's pixel count.</exception>
    int DecodeFrameMemoryToSpan(ReadOnlyMemory<byte> data, int frameIndex, Span<byte> destination, GifDecodeOptions? options);

    /// <summary>Decodes one raw indexed frame from a seekable GIF stream into caller-provided storage without closing it.</summary>
    /// <param name="stream">The readable, seekable GIF source stream.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="destination">Storage for one indexed byte per pixel in the selected frame rectangle.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of indexed pixel bytes written.</returns>
    /// <exception cref="GifException">Thrown when the stream is unusable, decoding fails, or the destination is smaller than the selected frame's pixel count.</exception>
    int DecodeFrameStreamToSpan(Stream stream, int frameIndex, Span<byte> destination, GifDecodeOptions? options);

    /// <summary>Decodes and composites GIF bytes into a display-ready animation.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The composited animation.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing produces a fatal diagnostic.</exception>
    GifAnimation DecodeAnimationBytes(byte[] data, GifDecodeOptions? options);

    /// <summary>Decodes and composites GIF memory into a display-ready animation without copying the source region.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The composited animation.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing produces a fatal diagnostic.</exception>
    GifAnimation DecodeAnimationMemory(ReadOnlyMemory<byte> data, GifDecodeOptions? options);

    /// <summary>Reads and composites a GIF stream without closing it.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The composited animation.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing produces a fatal diagnostic.</exception>
    GifAnimation DecodeAnimationStream(Stream stream, GifDecodeOptions? options);

    /// <summary>Reads metadata first, then composites only the selected display frame from GIF bytes.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>An animation containing only the selected full-canvas frame with its original frame index.</returns>
    /// <exception cref="GifException">Thrown when metadata, frame selection, decoding, or compositing produces a fatal diagnostic.</exception>
    GifAnimation DecodeCompositedFrameBytes(byte[] data, int frameIndex, GifDecodeOptions? options);

    /// <summary>Reads metadata first, then composites only the selected display frame from GIF memory.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>An animation containing only the selected full-canvas frame with its original frame index.</returns>
    /// <exception cref="GifException">Thrown when metadata, frame selection, decoding, or compositing produces a fatal diagnostic.</exception>
    GifAnimation DecodeCompositedFrameMemory(ReadOnlyMemory<byte> data, int frameIndex, GifDecodeOptions? options);

    /// <summary>Reads metadata first, seeks back, then composites only the selected display frame from a stream without closing it.</summary>
    /// <param name="stream">The readable, seekable GIF source stream.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>An animation containing only the selected full-canvas frame with its original frame index.</returns>
    /// <exception cref="GifException">Thrown when the stream is not seekable or metadata, frame selection, decoding, or compositing produces a fatal diagnostic.</exception>
    GifAnimation DecodeCompositedFrameStream(Stream stream, int frameIndex, GifDecodeOptions? options);

    /// <summary>Composites one display frame from GIF bytes into caller-provided RGBA32 storage.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="destination">Storage for four RGBA bytes per logical-screen pixel.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of RGBA bytes written.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing fails or the destination is smaller than four times the logical-screen pixel count.</exception>
    int DecodeCompositedFrameBytesToSpan(byte[] data, int frameIndex, Span<byte> destination, GifDecodeOptions? options);

    /// <summary>Composites one display frame from GIF memory into caller-provided RGBA32 storage.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="destination">Storage for four RGBA bytes per logical-screen pixel.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of RGBA bytes written.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing fails or the destination is smaller than four times the logical-screen pixel count.</exception>
    int DecodeCompositedFrameMemoryToSpan(ReadOnlyMemory<byte> data, int frameIndex, Span<byte> destination, GifDecodeOptions? options);

    /// <summary>Composites one display frame from a seekable GIF stream into caller-provided RGBA32 storage without closing it.</summary>
    /// <param name="stream">The readable, seekable GIF source stream.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="destination">Storage for four RGBA bytes per logical-screen pixel.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of RGBA bytes written.</returns>
    /// <exception cref="GifException">Thrown when the stream is unusable, decoding or compositing fails, or the destination is smaller than four times the logical-screen pixel count.</exception>
    int DecodeCompositedFrameStreamToSpan(Stream stream, int frameIndex, Span<byte> destination, GifDecodeOptions? options);

    /// <summary>Composites a decoded document into a display-ready animation.</summary>
    /// <param name="document">The raw GIF document.</param>
    /// <param name="options">Compositing settings; <c>null</c> uses defaults.</param>
    /// <returns>The composited animation.</returns>
    /// <exception cref="GifException">Thrown when compositing produces a fatal diagnostic.</exception>
    GifAnimation Coalesce(GifDocument? document, GifDecodeOptions? options);

    /// <summary>Reads GIF metadata from bytes without retaining compressed image payloads.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode limits and strictness; <c>null</c> uses defaults.</param>
    /// <returns>The identified GIF information.</returns>
    GifInfo IdentifyBytes(byte[] data, GifDecodeOptions? options);

    /// <summary>Reads GIF metadata from memory without copying or retaining compressed image payloads.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="options">Decode limits and strictness; <c>null</c> uses defaults.</param>
    /// <returns>The identified GIF information.</returns>
    GifInfo IdentifyMemory(ReadOnlyMemory<byte> data, GifDecodeOptions? options);

    /// <summary>Reads GIF metadata from a stream without closing it.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">Decode limits and strictness; <c>null</c> uses defaults.</param>
    /// <returns>The identified GIF information.</returns>
    GifInfo IdentifyStream(Stream stream, GifDecodeOptions? options);
}
