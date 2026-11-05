namespace gif;

/// <summary>Defines construction operations for indexed bitmaps and GIF documents.</summary>
public interface IGifFactory
{
    /// <summary>Creates an indexed bitmap over caller-supplied pixels and palette entries.</summary>
    /// <param name="width">The bitmap width.</param>
    /// <param name="height">The bitmap height.</param>
    /// <param name="indexedPixels">One palette index per pixel.</param>
    /// <param name="palette">The referenced palette.</param>
    /// <returns>The indexed bitmap.</returns>
    GifBitmap CreateIndexedBitmap(int width, int height, byte[]? indexedPixels, GifPalette? palette);

    /// <summary>Creates a raw document containing one bitmap.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="options">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The created document.</returns>
    GifDocument CreateStill(GifBitmap? bitmap, GifEncodeOptions? options);

    /// <summary>Creates a raw animation document from bitmap frames.</summary>
    /// <param name="frames">The source bitmaps.</param>
    /// <param name="animationOptions">Animation timing and placement settings; <c>null</c> uses defaults.</param>
    /// <param name="encodeOptions">Encode settings; <c>null</c> uses defaults.</param>
    /// <returns>The created document.</returns>
    GifDocument CreateAnimation(GifBitmap[]? frames, GifAnimationOptions? animationOptions, GifEncodeOptions? encodeOptions);
}
