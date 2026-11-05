namespace gltf;

/// <summary>Resolves referenced glTF resources into owned data.</summary>
public interface IGltfResourceResolver
{
    /// <summary>Resolves one resource request.</summary>
    /// <param name="request">The resource request.</param>
    /// <returns>The resolved resource and operation status.</returns>
    GltfResource ResolveResource(GltfResourceRequest request);
}
