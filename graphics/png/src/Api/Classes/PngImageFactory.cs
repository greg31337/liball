namespace png;

/// <summary>Default service for creating PNG image models.</summary>
public class PngImageFactory : IPngImageFactory
{
    /// <summary>Creates an independent PNG image factory.</summary>
    public PngImageFactory()
    {
    }

    /// <summary>Creates a blank image.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <returns>The created image.</returns>
    public PngImage CreateImage(int width, int height, PngPixelFormat pixelFormat)
    {
        return Png.CreateImage(width, height, pixelFormat);
    }

    /// <summary>Creates an image from caller-supplied pixel bytes.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="data">The pixel bytes.</param>
    /// <returns>The created image.</returns>
    public PngImage CreateImageWithData(int width, int height, PngPixelFormat pixelFormat, byte[] data)
    {
        return Png.CreateImage(width, height, pixelFormat, data);
    }

    /// <summary>Creates a tightly packed image from a bounded span, copying the supplied pixels.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="data">The tightly packed pixel bytes.</param>
    /// <returns>The created image.</returns>
    public PngImage CreateImageFromSpan(int width, int height, PngPixelFormat pixelFormat, ReadOnlySpan<byte> data)
    {
        return PngImageFactoryCore.CreateFromSpan(width, height, pixelFormat, data);
    }

    /// <summary>Creates an image with a caller-supplied row stride, copying the supplied pixels.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="stride">The byte distance between consecutive rows.</param>
    /// <param name="data">The pixel bytes including any row padding.</param>
    /// <returns>The created image.</returns>
    public PngImage CreateImageWithStride(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data)
    {
        return PngImageFactoryCore.CreateWithStride(width, height, pixelFormat, stride, data);
    }

    /// <summary>Creates an indexed image with its palette, copying both indices and palette entries.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The indexed pixel format.</param>
    /// <param name="stride">The byte distance between consecutive rows.</param>
    /// <param name="data">The packed or unpacked palette indices.</param>
    /// <param name="palette">The palette referenced by the indices.</param>
    /// <returns>The created indexed image.</returns>
    public PngImage CreateIndexedImage(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, PngPalette palette)
    {
        return PngImageFactoryCore.CreateIndexed(width, height, pixelFormat, stride, data, palette);
    }

    /// <summary>Creates an image with caller-supplied metadata and copied pixel bytes.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="stride">The byte distance between consecutive rows.</param>
    /// <param name="data">The pixel bytes including any row padding.</param>
    /// <param name="metadata">The metadata model to attach to the image.</param>
    /// <returns>The created image.</returns>
    public PngImage CreateImageWithMetadata(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, PngMetadata metadata)
    {
        return PngImageFactoryCore.CreateWithMetadata(width, height, pixelFormat, stride, data, metadata);
    }

    /// <summary>Creates an APNG frame rectangle with copied pixel bytes.</summary>
    /// <param name="width">The frame width.</param>
    /// <param name="height">The frame height.</param>
    /// <param name="xOffset">The horizontal canvas offset.</param>
    /// <param name="yOffset">The vertical canvas offset.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="stride">The byte distance between consecutive rows.</param>
    /// <param name="data">The frame pixel bytes.</param>
    /// <param name="delayNumerator">The frame delay numerator.</param>
    /// <param name="delayDenominator">The frame delay denominator.</param>
    /// <param name="disposeOperation">The post-display disposal operation.</param>
    /// <param name="blendOperation">The canvas blend operation.</param>
    /// <returns>The created frame.</returns>
    public PngFrame CreateFrameWithData(int width, int height, int xOffset, int yOffset, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, int delayNumerator, int delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation)
    {
        return PngImageFactoryCore.CreateFrame(width, height, xOffset, yOffset, pixelFormat, stride, data, delayNumerator, delayDenominator, disposeOperation, blendOperation);
    }

    /// <summary>Creates an APNG image from a static or default image and a bounded frame collection.</summary>
    /// <param name="image">The static image or default-frame pixels and metadata.</param>
    /// <param name="playCount">The animation play count; zero repeats indefinitely.</param>
    /// <param name="defaultImageIsFirstFrame">Whether the image pixels are also the first animation frame.</param>
    /// <param name="frames">The animation frames in display order.</param>
    /// <returns>The created animation image.</returns>
    public PngImage CreateAnimationImage(PngImage image, int playCount, bool defaultImageIsFirstFrame, ReadOnlySpan<PngFrame> frames)
    {
        return PngImageFactoryCore.CreateAnimation(image, playCount, defaultImageIsFirstFrame, frames);
    }
}
