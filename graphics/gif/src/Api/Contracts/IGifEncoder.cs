using System.IO;

namespace gif;

/// <summary>Defines raw-document, indexed-pixel, and RGBA GIF encoding operations.</summary>
public interface IGifEncoder
{
    /// <summary>Encodes a raw GIF document.</summary>
    /// <param name="document">The document to validate and encode.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encoded GIF bytes.</returns>
    /// <exception cref="GifException">Thrown when validation or encoding produces a fatal diagnostic.</exception>
    byte[] Encode(GifDocument? document, GifEncodeOptions? options);

    /// <summary>Validates and writes a raw GIF document without closing the destination.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="document">The document to validate and encode.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <exception cref="GifException">Thrown when the destination is unusable or validation or encoding produces a fatal diagnostic.</exception>
    void Write(Stream? stream, GifDocument? document, GifEncodeOptions? options);

    /// <summary>Encodes one indexed bitmap.</summary>
    /// <param name="bitmap">The indexed bitmap.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <param name="hasTransparency">Whether a palette index is transparent.</param>
    /// <param name="transparentColorIndex">The transparent palette index.</param>
    /// <returns>The encode result.</returns>
    GifEncodeResult EncodeIndexedStill(GifBitmap bitmap, GifEncodeOptions? options, bool hasTransparency, int transparentColorIndex);

    /// <summary>Encodes an indexed animation.</summary>
    /// <param name="animation">The indexed animation.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    GifEncodeResult EncodeIndexedAnimation(GifAnimation animation, GifEncodeOptions? options);

    /// <summary>Encodes indexed bitmaps as an animation.</summary>
    /// <param name="frames">The indexed frame bitmaps.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    GifEncodeResult EncodeIndexedFrames(GifBitmap[] frames, GifEncodeOptions? options);

    /// <summary>Quantizes and encodes one RGBA bitmap.</summary>
    /// <param name="bitmap">The RGBA bitmap.</param>
    /// <param name="quantizeOptions">Quantization settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    GifEncodeResult EncodeRgbaStill(GifBitmap bitmap, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options);

    /// <summary>Quantizes and encodes an RGBA animation.</summary>
    /// <param name="animation">The RGBA animation.</param>
    /// <param name="quantizeOptions">Quantization settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    GifEncodeResult EncodeRgbaAnimation(GifAnimation animation, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options);

    /// <summary>Quantizes and encodes RGBA bitmaps as an animation.</summary>
    /// <param name="frames">The RGBA frame bitmaps.</param>
    /// <param name="quantizeOptions">Quantization settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The encode result.</returns>
    GifEncodeResult EncodeRgbaFrames(GifBitmap[] frames, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options);
}
