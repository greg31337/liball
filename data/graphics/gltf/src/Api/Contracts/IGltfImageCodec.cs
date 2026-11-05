namespace gltf;

/// <summary>Inspects, decodes, encodes, and transcodes image data.</summary>
public interface IGltfImageCodec
{
    /// <summary>Inspects encoded image metadata.</summary>
    /// <param name="request">The image request.</param>
    /// <returns>The recognized image information.</returns>
    GltfImageInfo InspectImage(GltfImageRequest request);

    /// <summary>Decodes image data.</summary>
    /// <param name="request">The image request.</param>
    /// <returns>The image operation result.</returns>
    GltfImageResult DecodeImage(GltfImageRequest request);

    /// <summary>Encodes image data.</summary>
    /// <param name="request">The image request.</param>
    /// <returns>The image operation result.</returns>
    GltfImageResult EncodeImage(GltfImageRequest request);

    /// <summary>Transcodes encoded image data.</summary>
    /// <param name="request">The image request.</param>
    /// <returns>The image operation result.</returns>
    GltfImageResult TranscodeImage(GltfImageRequest request);
}
