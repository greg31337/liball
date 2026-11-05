namespace gltf;

/// <summary>Identifies a known encoded image format.</summary>
public enum GltfImageFormat
{
    /// <summary>The image format is unknown.</summary>
    Unknown = 0,

    /// <summary>Portable Network Graphics.</summary>
    Png = 1,

    /// <summary>JPEG image data.</summary>
    Jpeg = 2,

    /// <summary>Khronos Texture 2 image data.</summary>
    Ktx2 = 3,

    /// <summary>WebP image data.</summary>
    WebP = 4,

    /// <summary>AV1 Image File Format data.</summary>
    Avif = 5,

    /// <summary>DirectDraw Surface image data.</summary>
    Dds = 6,

    /// <summary>Adaptive Scalable Texture Compression image data.</summary>
    Astc = 7,

    /// <summary>An application-defined image format.</summary>
    Custom = 8
}
