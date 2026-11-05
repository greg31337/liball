using System;
using System.IO;

namespace gif;

/// <summary>Provides the public GIF decode, encode, construction, validation, and transformation API.</summary>
public class GifCodec : IGifDecoder, IGifEncoder, IGifFactory, IGifTransformer
{
    /// <summary>Creates a stateless GIF codec.</summary>
    public GifCodec()
    {
    }

    /// <summary>Decodes GIF bytes into a raw document.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The decoded document.</returns>
    /// <exception cref="GifException">Thrown when a fatal decode diagnostic is produced.</exception>
    public GifDocument DecodeBytes(byte[] data, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result = reader.ReadBytesWithOptions(data, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Document;
    }

    /// <summary>Decodes GIF memory into a raw document without copying the source region.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The decoded document.</returns>
    /// <exception cref="GifException">Thrown when a fatal decode diagnostic is produced.</exception>
    public GifDocument DecodeMemory(ReadOnlyMemory<byte> data, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result = reader.ReadMemoryWithOptions(data, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Document;
    }

    /// <summary>Reads a GIF stream into a raw document without closing it.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The decoded document.</returns>
    /// <exception cref="GifException">Thrown when a fatal decode diagnostic is produced.</exception>
    public GifDocument DecodeStream(Stream stream, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result = reader.ReadStreamWithOptions(stream, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Document;
    }

    /// <summary>Reads metadata first, then decodes only one raw indexed frame from GIF bytes.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>A metadata-complete document where only the selected frame retains compressed and indexed pixels.</returns>
    /// <exception cref="GifException">Thrown when metadata, frame selection, or raw frame decoding produces a fatal diagnostic.</exception>
    public GifDocument DecodeFrameBytes(byte[] data, int frameIndex, GifDecodeOptions? options)
    {
        GifSelectedFrameDecoder decoder = new GifSelectedFrameDecoder();
        GifSelectedRawFrameDecodeResult result = decoder.DecodeRawBytes(data, frameIndex, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Document;
    }

    /// <summary>Reads metadata first, then decodes only one raw indexed frame from GIF memory.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>A metadata-complete document where only the selected frame retains compressed and indexed pixels.</returns>
    /// <exception cref="GifException">Thrown when metadata, frame selection, or raw frame decoding produces a fatal diagnostic.</exception>
    public GifDocument DecodeFrameMemory(ReadOnlyMemory<byte> data, int frameIndex, GifDecodeOptions? options)
    {
        GifSelectedFrameDecoder decoder = new GifSelectedFrameDecoder();
        GifSelectedRawFrameDecodeResult result = decoder.DecodeRawMemory(data, frameIndex, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Document;
    }

    /// <summary>Reads metadata first, seeks back, then decodes only one raw indexed frame from a stream without closing it.</summary>
    /// <param name="stream">The readable, seekable GIF source stream.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>A metadata-complete document where only the selected frame retains compressed and indexed pixels.</returns>
    /// <exception cref="GifException">Thrown when the stream is not seekable or metadata, frame selection, or raw frame decoding produces a fatal diagnostic.</exception>
    public GifDocument DecodeFrameStream(Stream stream, int frameIndex, GifDecodeOptions? options)
    {
        GifSelectedFrameDecoder decoder = new GifSelectedFrameDecoder();
        GifSelectedRawFrameDecodeResult result = decoder.DecodeRawStream(stream, frameIndex, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Document;
    }

    /// <summary>Decodes one raw indexed frame from GIF bytes into caller-provided storage.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="destination">Storage for one indexed byte per pixel in the selected frame rectangle.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of indexed pixel bytes written.</returns>
    /// <exception cref="GifException">Thrown when decoding fails or the destination is smaller than the selected frame's pixel count.</exception>
    public int DecodeFrameBytesToSpan(byte[] data, int frameIndex, Span<byte> destination, GifDecodeOptions? options)
    {
        GifDocument document = DecodeFrameBytes(data, frameIndex, options);
        return WriteDecodedPixelsToSpan(document.Frames[frameIndex].IndexedPixels, destination, frameIndex, "RawFrameDestination");
    }

    /// <summary>Decodes one raw indexed frame from GIF memory into caller-provided storage.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="destination">Storage for one indexed byte per pixel in the selected frame rectangle.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of indexed pixel bytes written.</returns>
    /// <exception cref="GifException">Thrown when decoding fails or the destination is smaller than the selected frame's pixel count.</exception>
    public int DecodeFrameMemoryToSpan(ReadOnlyMemory<byte> data, int frameIndex, Span<byte> destination, GifDecodeOptions? options)
    {
        GifDocument document = DecodeFrameMemory(data, frameIndex, options);
        return WriteDecodedPixelsToSpan(document.Frames[frameIndex].IndexedPixels, destination, frameIndex, "RawFrameDestination");
    }

    /// <summary>Decodes one raw indexed frame from a seekable GIF stream into caller-provided storage without closing it.</summary>
    /// <param name="stream">The readable, seekable GIF source stream.</param>
    /// <param name="frameIndex">The zero-based frame index to decode.</param>
    /// <param name="destination">Storage for one indexed byte per pixel in the selected frame rectangle.</param>
    /// <param name="options">Decode settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of indexed pixel bytes written.</returns>
    /// <exception cref="GifException">Thrown when the stream is unusable, decoding fails, or the destination is smaller than the selected frame's pixel count.</exception>
    public int DecodeFrameStreamToSpan(Stream stream, int frameIndex, Span<byte> destination, GifDecodeOptions? options)
    {
        GifDocument document = DecodeFrameStream(stream, frameIndex, options);
        return WriteDecodedPixelsToSpan(document.Frames[frameIndex].IndexedPixels, destination, frameIndex, "RawFrameDestination");
    }

    /// <summary>Decodes and composites GIF bytes into a display-ready animation.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The composited animation.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing produces a fatal diagnostic.</exception>
    public GifAnimation DecodeAnimationBytes(byte[] data, GifDecodeOptions? options)
    {
        GifDocument document = DecodeBytes(data, options);
        return Coalesce(document, options);
    }

    /// <summary>Decodes and composites GIF memory into a display-ready animation without copying the source region.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The composited animation.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing produces a fatal diagnostic.</exception>
    public GifAnimation DecodeAnimationMemory(ReadOnlyMemory<byte> data, GifDecodeOptions? options)
    {
        GifDocument document = DecodeMemory(data, options);
        return Coalesce(document, options);
    }

    /// <summary>Reads and composites a GIF stream without closing it.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>The composited animation.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing produces a fatal diagnostic.</exception>
    public GifAnimation DecodeAnimationStream(Stream stream, GifDecodeOptions? options)
    {
        GifDocument document = DecodeStream(stream, options);
        return Coalesce(document, options);
    }

    /// <summary>Reads metadata first, then composites only the selected display frame from GIF bytes.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>An animation containing only the selected full-canvas frame with its original frame index.</returns>
    /// <exception cref="GifException">Thrown when metadata, frame selection, decoding, or compositing produces a fatal diagnostic.</exception>
    public GifAnimation DecodeCompositedFrameBytes(byte[] data, int frameIndex, GifDecodeOptions? options)
    {
        GifSelectedFrameDecoder decoder = new GifSelectedFrameDecoder();
        GifSelectedAnimationFrameDecodeResult result = decoder.DecodeCompositedBytes(data, frameIndex, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Animation;
    }

    /// <summary>Reads metadata first, then composites only the selected display frame from GIF memory.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>An animation containing only the selected full-canvas frame with its original frame index.</returns>
    /// <exception cref="GifException">Thrown when metadata, frame selection, decoding, or compositing produces a fatal diagnostic.</exception>
    public GifAnimation DecodeCompositedFrameMemory(ReadOnlyMemory<byte> data, int frameIndex, GifDecodeOptions? options)
    {
        GifSelectedFrameDecoder decoder = new GifSelectedFrameDecoder();
        GifSelectedAnimationFrameDecodeResult result = decoder.DecodeCompositedMemory(data, frameIndex, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Animation;
    }

    /// <summary>Reads metadata first, seeks back, then composites only the selected display frame from a stream without closing it.</summary>
    /// <param name="stream">The readable, seekable GIF source stream.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>An animation containing only the selected full-canvas frame with its original frame index.</returns>
    /// <exception cref="GifException">Thrown when the stream is not seekable or metadata, frame selection, decoding, or compositing produces a fatal diagnostic.</exception>
    public GifAnimation DecodeCompositedFrameStream(Stream stream, int frameIndex, GifDecodeOptions? options)
    {
        GifSelectedFrameDecoder decoder = new GifSelectedFrameDecoder();
        GifSelectedAnimationFrameDecodeResult result = decoder.DecodeCompositedStream(stream, frameIndex, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Animation;
    }

    /// <summary>Composites one display frame from GIF bytes into caller-provided RGBA32 storage.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="destination">Storage for four RGBA bytes per logical-screen pixel.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of RGBA bytes written.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing fails or the destination is smaller than four times the logical-screen pixel count.</exception>
    public int DecodeCompositedFrameBytesToSpan(byte[] data, int frameIndex, Span<byte> destination, GifDecodeOptions? options)
    {
        GifAnimation animation = DecodeCompositedFrameBytes(data, frameIndex, options);
        return WriteDecodedPixelsToSpan(animation.Frames[0].Bitmap.RgbaPixels, destination, frameIndex, "CompositedFrameDestination");
    }

    /// <summary>Composites one display frame from GIF memory into caller-provided RGBA32 storage.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="destination">Storage for four RGBA bytes per logical-screen pixel.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of RGBA bytes written.</returns>
    /// <exception cref="GifException">Thrown when decoding or compositing fails or the destination is smaller than four times the logical-screen pixel count.</exception>
    public int DecodeCompositedFrameMemoryToSpan(ReadOnlyMemory<byte> data, int frameIndex, Span<byte> destination, GifDecodeOptions? options)
    {
        GifAnimation animation = DecodeCompositedFrameMemory(data, frameIndex, options);
        return WriteDecodedPixelsToSpan(animation.Frames[0].Bitmap.RgbaPixels, destination, frameIndex, "CompositedFrameDestination");
    }

    /// <summary>Composites one display frame from a seekable GIF stream into caller-provided RGBA32 storage without closing it.</summary>
    /// <param name="stream">The readable, seekable GIF source stream.</param>
    /// <param name="frameIndex">The zero-based frame index to composite.</param>
    /// <param name="destination">Storage for four RGBA bytes per logical-screen pixel.</param>
    /// <param name="options">Decode and compositing settings; metadata-only and first-frame-only flags are ignored.</param>
    /// <returns>The number of RGBA bytes written.</returns>
    /// <exception cref="GifException">Thrown when the stream is unusable, decoding or compositing fails, or the destination is smaller than four times the logical-screen pixel count.</exception>
    public int DecodeCompositedFrameStreamToSpan(Stream stream, int frameIndex, Span<byte> destination, GifDecodeOptions? options)
    {
        GifAnimation animation = DecodeCompositedFrameStream(stream, frameIndex, options);
        return WriteDecodedPixelsToSpan(animation.Frames[0].Bitmap.RgbaPixels, destination, frameIndex, "CompositedFrameDestination");
    }

    /// <summary>Composites a decoded document into a display-ready animation.</summary>
    /// <param name="document">The raw GIF document.</param>
    /// <param name="options">Compositing settings; <c>null</c> uses defaults.</param>
    /// <returns>The composited animation.</returns>
    /// <exception cref="GifException">Thrown when compositing produces a fatal diagnostic.</exception>
    public GifAnimation Coalesce(GifDocument? document, GifDecodeOptions? options)
    {
        GifCoalescer coalescer = new GifCoalescer();
        GifCoalesceResult result = coalescer.CoalesceWithOptions(document, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Animation;
    }

    /// <summary>Reads GIF metadata from bytes without retaining compressed image payloads.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode limits and strictness; <c>null</c> uses defaults.</param>
    /// <returns>The identified GIF information.</returns>
    public GifInfo IdentifyBytes(byte[] data, GifDecodeOptions? options)
    {
        return Gif.Identify(data, options);
    }

    /// <summary>Reads GIF metadata from memory without copying or retaining compressed image payloads.</summary>
    /// <param name="data">The complete GIF file memory.</param>
    /// <param name="options">Decode limits and strictness; <c>null</c> uses defaults.</param>
    /// <returns>The identified GIF information.</returns>
    public GifInfo IdentifyMemory(ReadOnlyMemory<byte> data, GifDecodeOptions? options)
    {
        GifIdentifier identifier = new GifIdentifier();
        return identifier.IdentifyMemoryWithOptions(data, options);
    }

    /// <summary>Reads GIF metadata from a stream without closing it.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">Decode limits and strictness; <c>null</c> uses defaults.</param>
    /// <returns>The identified GIF information.</returns>
    public GifInfo IdentifyStream(Stream stream, GifDecodeOptions? options)
    {
        return Gif.Identify(stream, options);
    }

    /// <summary>Encodes a raw GIF document.</summary>
    /// <param name="document">The document to validate and encode.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encoded GIF bytes.</returns>
    /// <exception cref="GifException">Thrown when validation or encoding produces a fatal diagnostic.</exception>
    public byte[] Encode(GifDocument? document, GifEncodeOptions? options)
    {
        GifEncodeResult result = Gif.EncodeDocumentForPublicApi(document, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Data;
    }

    /// <summary>Validates and writes a raw GIF document without closing the destination.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="document">The document to validate and encode.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <exception cref="GifException">Thrown when the destination is unusable or validation or encoding produces a fatal diagnostic.</exception>
    public void Write(Stream? stream, GifDocument? document, GifEncodeOptions? options)
    {
        if (stream == null)
        {
            GifErrorDiagnostic error = new GifErrorDiagnostic(GifErrorCode.InvalidOperationState, "GIF destination stream is missing.", new GifDiagnosticPosition(-1, -1, "DestinationStream"));
            throw GifException.FromDiagnostic(error);
        }

        if (!stream.CanWrite)
        {
            GifErrorDiagnostic error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "GIF destination stream is not writable.", new GifDiagnosticPosition(-1, -1, "DestinationStream"));
            throw GifException.FromDiagnostic(error);
        }

        byte[] data = Encode(document, options);
        stream.Write(data, 0, data.Length);
    }

    /// <summary>Encodes one indexed bitmap.</summary>
    /// <param name="bitmap">The indexed bitmap.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <param name="hasTransparency">Whether a palette index is transparent.</param>
    /// <param name="transparentColorIndex">The transparent palette index.</param>
    /// <returns>The encode result.</returns>
    public GifEncodeResult EncodeIndexedStill(GifBitmap bitmap, GifEncodeOptions? options, bool hasTransparency, int transparentColorIndex)
    {
        return Gif.EncodeIndexedStill(bitmap, options, hasTransparency, transparentColorIndex);
    }

    /// <summary>Encodes an indexed animation.</summary>
    /// <param name="animation">The indexed animation.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    public GifEncodeResult EncodeIndexedAnimation(GifAnimation animation, GifEncodeOptions? options)
    {
        return Gif.EncodeIndexedAnimation(animation, options);
    }

    /// <summary>Encodes indexed bitmaps as an animation.</summary>
    /// <param name="frames">The indexed frame bitmaps.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    public GifEncodeResult EncodeIndexedFrames(GifBitmap[] frames, GifEncodeOptions? options)
    {
        return Gif.EncodeIndexedAnimation(frames, options);
    }

    /// <summary>Quantizes and encodes one RGBA bitmap.</summary>
    /// <param name="bitmap">The RGBA bitmap.</param>
    /// <param name="quantizeOptions">Quantization settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    public GifEncodeResult EncodeRgbaStill(GifBitmap bitmap, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options)
    {
        return Gif.EncodeRgbaStill(bitmap, quantizeOptions, options);
    }

    /// <summary>Quantizes and encodes an RGBA animation.</summary>
    /// <param name="animation">The RGBA animation.</param>
    /// <param name="quantizeOptions">Quantization settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    public GifEncodeResult EncodeRgbaAnimation(GifAnimation animation, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options)
    {
        return Gif.EncodeRgbaAnimation(animation, quantizeOptions, options);
    }

    /// <summary>Quantizes and encodes RGBA bitmaps as an animation.</summary>
    /// <param name="frames">The RGBA frame bitmaps.</param>
    /// <param name="quantizeOptions">Quantization settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    public GifEncodeResult EncodeRgbaFrames(GifBitmap[] frames, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options)
    {
        return Gif.EncodeRgbaAnimation(frames, quantizeOptions, options);
    }

    /// <summary>Creates an indexed bitmap over caller-supplied pixels and palette entries.</summary>
    /// <param name="width">The bitmap width.</param>
    /// <param name="height">The bitmap height.</param>
    /// <param name="indexedPixels">One palette index per pixel.</param>
    /// <param name="palette">The referenced palette.</param>
    /// <returns>The indexed bitmap.</returns>
    public GifBitmap CreateIndexedBitmap(int width, int height, byte[]? indexedPixels, GifPalette? palette)
    {
        return GifBitmap.CreateIndexed(width, height, indexedPixels, palette);
    }

    /// <summary>Creates a raw document containing one bitmap.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The created document.</returns>
    public GifDocument CreateStill(GifBitmap? bitmap, GifEncodeOptions? options)
    {
        return Gif.CreateStill(bitmap, options);
    }

    /// <summary>Creates a raw animation document from bitmap frames.</summary>
    /// <param name="frames">The source bitmaps.</param>
    /// <param name="animationOptions">Animation timing and placement settings; <c>null</c> uses defaults.</param>
    /// <param name="encodeOptions">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The created document.</returns>
    public GifDocument CreateAnimation(GifBitmap[]? frames, GifAnimationOptions? animationOptions, GifEncodeOptions? encodeOptions)
    {
        return Gif.CreateAnimation(frames, animationOptions, encodeOptions);
    }

    /// <summary>Deconstructs full-canvas animation frames into changed rectangles.</summary>
    /// <param name="animation">The source animation.</param>
    /// <returns>The deconstructed animation.</returns>
    /// <exception cref="GifException">Thrown when deconstruction produces a fatal diagnostic.</exception>
    public GifAnimation Deconstruct(GifAnimation? animation)
    {
        GifOptimizer optimizer = new GifOptimizer();
        GifDeconstructResult result = optimizer.Deconstruct(animation);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Animation;
    }

    /// <summary>Validates a raw GIF document.</summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">Encode constraints; <c>null</c> uses defaults.</param>
    /// <returns>The first fatal diagnostic, or a no-error diagnostic.</returns>
    public GifErrorDiagnostic ValidateDocument(GifDocument? document, GifEncodeOptions? options)
    {
        return Gif.Validate(document, options);
    }

    /// <summary>Validates a bitmap for GIF encoding.</summary>
    /// <param name="bitmap">The bitmap to validate.</param>
    /// <param name="options">Encode constraints; <c>null</c> uses defaults.</param>
    /// <returns>The first fatal diagnostic, or a no-error diagnostic.</returns>
    public GifErrorDiagnostic ValidateBitmap(GifBitmap? bitmap, GifEncodeOptions? options)
    {
        return Gif.Validate(bitmap, options);
    }

    /// <summary>Validates an animation for GIF encoding.</summary>
    /// <param name="animation">The animation to validate.</param>
    /// <param name="options">Encode constraints; <c>null</c> uses defaults.</param>
    /// <returns>The first fatal diagnostic, or a no-error diagnostic.</returns>
    public GifErrorDiagnostic ValidateAnimation(GifAnimation? animation, GifEncodeOptions? options)
    {
        return Gif.Validate(animation, options);
    }

    /// <summary>Converts a GIF delay in hundredths of a second to milliseconds.</summary>
    /// <param name="delayHundredths">A delay from 0 through 65535 hundredths.</param>
    /// <returns>The exact delay in milliseconds.</returns>
    /// <exception cref="GifException">Thrown when the delay is negative, outside the GIF range, or would overflow the millisecond result.</exception>
    public int ConvertDelayHundredthsToMilliseconds(int delayHundredths)
    {
        GifDelayConversionResult result = new GifDelayConverter().ConvertHundredthsToMilliseconds(delayHundredths);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Value;
    }

    /// <summary>Converts milliseconds to GIF hundredths of a second using explicit rounding.</summary>
    /// <param name="delayMilliseconds">A nonnegative delay in milliseconds.</param>
    /// <param name="roundingMode">The rounding rule applied at 10-millisecond boundaries.</param>
    /// <returns>The rounded GIF delay in hundredths.</returns>
    /// <exception cref="GifException">Thrown when the input, rounding mode, or rounded GIF delay is invalid.</exception>
    public int ConvertDelayMillisecondsToHundredths(int delayMilliseconds, GifDelayRoundingMode roundingMode)
    {
        GifDelayConversionResult result = new GifDelayConverter().ConvertMillisecondsToHundredths(delayMilliseconds, roundingMode);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Value;
    }

    /// <summary>Sets one delay on every animation frame.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="delayMilliseconds">The frame delay in milliseconds.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or delay is invalid.</exception>
    public GifAnimation SetDelay(GifAnimation? animation, int delayMilliseconds)
    {
        RequireAnimation(animation, "Timeline");
        GifFrameDelayEncoder encoder = new GifFrameDelayEncoder();
        ThrowForDiagnostic(encoder.ValidateDelayMilliseconds(delayMilliseconds, -1));
        return Gif.SetDelay(animation, delayMilliseconds);
    }

    /// <summary>Sets individual animation-frame delays.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="delaysMilliseconds">One delay per frame.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or a delay is invalid.</exception>
    public GifAnimation SetDelays(GifAnimation? animation, int[]? delaysMilliseconds)
    {
        RequireAnimation(animation, "Timeline");
        if (delaysMilliseconds == null)
        {
            GifErrorDiagnostic error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF frame delays are missing for transformation.", new GifDiagnosticPosition(-1, -1, "Timeline"));
            throw GifException.FromDiagnostic(error);
        }

        GifFrameDelayEncoder encoder = new GifFrameDelayEncoder();
        int frameIndex = 0;
        while (frameIndex < delaysMilliseconds.Length)
        {
            ThrowForDiagnostic(encoder.ValidateDelayMilliseconds(delaysMilliseconds[frameIndex], frameIndex));
            frameIndex = frameIndex + 1;
        }

        return Gif.SetDelays(animation, delaysMilliseconds);
    }

    /// <summary>Sets one disposal method on every animation frame.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="disposalMethod">The disposal method.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or disposal method is invalid.</exception>
    public GifAnimation SetDisposal(GifAnimation? animation, GifDisposalMethod disposalMethod)
    {
        RequireAnimation(animation, "GraphicControlExtension");
        GifFrameDisposalEncoder encoder = new GifFrameDisposalEncoder();
        ThrowForDiagnostic(encoder.ValidateDisposalMethod(disposalMethod, -1));
        return Gif.SetDisposal(animation, disposalMethod);
    }

    /// <summary>Sets individual animation-frame disposal methods.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="disposalMethods">One disposal method per frame.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or a disposal method is invalid.</exception>
    public GifAnimation SetDisposals(GifAnimation? animation, GifDisposalMethod[]? disposalMethods)
    {
        RequireAnimation(animation, "GraphicControlExtension");
        if (disposalMethods == null)
        {
            GifErrorDiagnostic error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF frame disposal methods are missing for transformation.", new GifDiagnosticPosition(-1, -1, "GraphicControlExtension"));
            throw GifException.FromDiagnostic(error);
        }

        GifFrameDisposalEncoder encoder = new GifFrameDisposalEncoder();
        int frameIndex = 0;
        while (frameIndex < disposalMethods.Length)
        {
            ThrowForDiagnostic(encoder.ValidateDisposalMethod(disposalMethods[frameIndex], frameIndex));
            frameIndex = frameIndex + 1;
        }

        return Gif.SetDisposals(animation, disposalMethods);
    }

    /// <summary>Sets the loop count on an animation.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="loopCount">The loop count.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or loop count is invalid.</exception>
    public GifAnimation SetAnimationLoopCount(GifAnimation? animation, int loopCount)
    {
        RequireAnimation(animation, "ApplicationExtension");
        GifLoopCountEncoder encoder = new GifLoopCountEncoder();
        ThrowForDiagnostic(encoder.ValidateOptionalLoopCount(loopCount, -1));
        return Gif.SetLoopCount(animation, loopCount);
    }

    /// <summary>Sets the loop count on a raw document.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="loopCount">The loop count.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document or loop count is invalid.</exception>
    public GifDocument SetDocumentLoopCount(GifDocument? document, int loopCount)
    {
        RequireDocument(document, "ApplicationExtension");
        GifLoopCountEncoder encoder = new GifLoopCountEncoder();
        ThrowForDiagnostic(encoder.ValidateOptionalLoopCount(loopCount, -1));
        return Gif.SetLoopCount(document, loopCount);
    }

    /// <summary>Replaces document comments from strings.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comments">The comment text values.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document is missing.</exception>
    public GifDocument SetCommentsFromText(GifDocument? document, string[]? comments)
    {
        RequireDocument(document, "CommentExtension");
        return Gif.SetComments(document, comments);
    }

    /// <summary>Replaces structured document comments.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comments">The structured comments.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document or comments are invalid.</exception>
    public GifDocument SetComments(GifDocument? document, GifCommentExtension[]? comments)
    {
        RequireDocument(document, "CommentExtension");
        GifCommentEncoder encoder = new GifCommentEncoder();
        ThrowForDiagnostic(encoder.ValidateComments(comments));
        return Gif.SetComments(document, comments);
    }

    /// <summary>Sets one global palette on a raw document.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="palette">The global palette.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document or palette is invalid.</exception>
    public GifDocument SetPalette(GifDocument? document, GifPalette palette)
    {
        GifDocument activeDocument = RequireDocument(document, "ColorTableMode");
        GifGlobalColorTableEncoder encoder = new GifGlobalColorTableEncoder();
        ThrowForDiagnostic(encoder.Apply(activeDocument, palette));
        return activeDocument;
    }

    /// <summary>Sets one local palette for each raw frame.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="palettes">One palette per frame.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document or palettes are invalid.</exception>
    public GifDocument SetPalettes(GifDocument? document, GifPalette[] palettes)
    {
        GifDocument activeDocument = RequireDocument(document, "ColorTableMode");
        GifLocalColorTableEncoder encoder = new GifLocalColorTableEncoder();
        ThrowForDiagnostic(encoder.Apply(activeDocument, palettes));
        return activeDocument;
    }

    /// <summary>Optimizes an animation using caller-selected encode settings.</summary>
    /// <param name="animation">The animation to optimize.</param>
    /// <param name="options">Optimization settings; <c>null</c> uses defaults.</param>
    /// <returns>The optimized animation.</returns>
    /// <exception cref="GifException">Thrown when optimization produces a fatal diagnostic.</exception>
    public GifAnimation Optimize(GifAnimation? animation, GifEncodeOptions? options)
    {
        GifOptimizer optimizer = new GifOptimizer();
        GifOptimizationResult result = optimizer.OptimizeWithOptions(animation, options);
        ThrowForFailedOperation(result.Success, result.Error);
        return result.Animation;
    }

    internal void ThrowForFailedOperation(bool success, GifErrorDiagnostic error)
    {
        if (!success)
        {
            throw GifException.FromDiagnostic(error);
        }
    }

    internal int WriteDecodedPixelsToSpan(byte[] pixels, Span<byte> destination, int frameIndex, string blockName)
    {
        if (destination.Length < pixels.Length)
        {
            GifErrorDiagnostic error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, $"The destination requires at least {pixels.Length} bytes but contains {destination.Length} bytes.", new GifDiagnosticPosition(-1, frameIndex, blockName));
            throw GifException.FromDiagnostic(error);
        }

        pixels.AsSpan().CopyTo(destination);
        return pixels.Length;
    }

    internal void ThrowForDiagnostic(GifErrorDiagnostic error)
    {
        if (error != null && error.Code != GifErrorCode.None)
        {
            throw GifException.FromDiagnostic(error);
        }
    }

    internal void RequireAnimation(GifAnimation? animation, string blockName)
    {
        if (animation == null)
        {
            GifErrorDiagnostic error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for transformation.", new GifDiagnosticPosition(-1, -1, blockName));
            throw GifException.FromDiagnostic(error);
        }
    }

    internal GifDocument RequireDocument(GifDocument? document, string blockName)
    {
        if (document == null)
        {
            GifErrorDiagnostic error = new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "GIF document is missing for transformation.", new GifDiagnosticPosition(-1, -1, blockName));
            throw GifException.FromDiagnostic(error);
        }

        return document;
    }
}
