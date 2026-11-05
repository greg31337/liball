namespace gltf;

/// <summary>Provides media types used by glTF containers, buffers, and known images.</summary>
public static class GltfMimeTypes
{
    /// <summary>The registered media type for a JSON glTF document.</summary>
    public const string GltfJson = "model/gltf+json";

    /// <summary>The registered media type for a binary glTF container.</summary>
    public const string GltfBinary = "model/gltf-binary";

    /// <summary>The registered media type for glTF buffer data.</summary>
    public const string GltfBuffer = "application/gltf-buffer";

    /// <summary>The generic binary data media type accepted for glTF buffers.</summary>
    public const string OctetStream = "application/octet-stream";

    /// <summary>The PNG image media type.</summary>
    public const string Png = "image/png";

    /// <summary>The JPEG image media type.</summary>
    public const string Jpeg = "image/jpeg";

    /// <summary>The KTX 2 image media type.</summary>
    public const string Ktx2 = "image/ktx2";

    /// <summary>The WebP image media type.</summary>
    public const string WebP = "image/webp";

    /// <summary>The AVIF image media type.</summary>
    public const string Avif = "image/avif";

    /// <summary>The DirectDraw Surface image media type.</summary>
    public const string Dds = "image/vnd-ms.dds";
}
