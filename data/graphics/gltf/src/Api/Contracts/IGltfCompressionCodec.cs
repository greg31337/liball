namespace gltf;

/// <summary>Encodes and decodes codec-neutral glTF geometry data.</summary>
public interface IGltfCompressionCodec
{
    /// <summary>Decodes compressed geometry.</summary>
    /// <param name="request">The compression request.</param>
    /// <returns>The compression result.</returns>
    GltfCompressionResult DecodeGeometry(GltfCompressionRequest request);

    /// <summary>Encodes geometry.</summary>
    /// <param name="request">The compression request.</param>
    /// <returns>The compression result.</returns>
    GltfCompressionResult EncodeGeometry(GltfCompressionRequest request);

    /// <summary>Decodes Meshopt buffer data.</summary>
    /// <param name="request">The compression request.</param>
    /// <returns>The compression result.</returns>
    GltfCompressionResult DecodeMeshoptData(GltfCompressionRequest request);

    /// <summary>Encodes Meshopt buffer data.</summary>
    /// <param name="request">The compression request.</param>
    /// <returns>The compression result.</returns>
    GltfCompressionResult EncodeMeshoptData(GltfCompressionRequest request);
}
