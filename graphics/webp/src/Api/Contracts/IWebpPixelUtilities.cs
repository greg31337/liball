using System.Collections.Generic;

namespace webp;

/// <summary>Provides pixel-format, alpha, compositing, and animation-flattening operations for decoded WebP pixels.</summary>
/// <remarks>All returned buffers and frames are independent allocations; the supplied models are not modified.</remarks>
public interface IWebpPixelUtilities
{
    /// <summary>Copies straight-alpha RGBA pixels to compact BGRA storage.</summary>
    /// <param name="source">The source RGBA pixel buffer.</param>
    /// <returns>Compact blue, green, red, alpha bytes with no row padding.</returns>
    byte[] CopyRgbaToBgra(WebpPixelBuffer source);

    /// <summary>Converts straight-alpha RGBA pixels to premultiplied-alpha RGBA pixels.</summary>
    /// <param name="source">The straight-alpha source pixels.</param>
    /// <returns>An independent premultiplied-alpha buffer with the same dimensions and stride.</returns>
    WebpPremultipliedPixelBuffer ConvertStraightToPremultipliedAlpha(WebpPixelBuffer source);

    /// <summary>Converts premultiplied-alpha RGBA pixels to straight-alpha RGBA pixels.</summary>
    /// <param name="source">The premultiplied-alpha source pixels.</param>
    /// <returns>An independent straight-alpha buffer with the same dimensions and stride.</returns>
    WebpPixelBuffer ConvertPremultipliedToStraightAlpha(WebpPremultipliedPixelBuffer source);

    /// <summary>Checks whether any pixel is not fully opaque.</summary>
    /// <param name="source">The straight-alpha RGBA pixels to inspect.</param>
    /// <returns><c>true</c> when at least one alpha byte is below 255; otherwise <c>false</c>.</returns>
    bool HasNonOpaqueAlpha(WebpPixelBuffer source);

    /// <summary>Composites straight-alpha RGBA pixels over a solid background.</summary>
    /// <param name="source">The foreground straight-alpha RGBA pixels.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>An independent straight-alpha RGBA buffer containing the composited pixels.</returns>
    WebpPixelBuffer BlendOverBackground(WebpPixelBuffer source, uint backgroundColorBgra);

    /// <summary>Flattens decoded animation rectangles into independent full-canvas frames.</summary>
    /// <param name="image">The decoded image whose frames are in WebP display order.</param>
    /// <returns>Independent full-canvas frames after applying each frame's blend and disposal modes.</returns>
    List<WebpFrame> FlattenAnimationFrames(WebpImage image);
}
