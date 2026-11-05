namespace gltf;

/// <summary>Resolves policy-bounded relative, file, and data URI resources.</summary>
public class FileGltfResourceResolver : IGltfResourceResolver
{
    internal FileResourceResolver Resolver;

    /// <summary>Initializes a file resource resolver.</summary>
    public FileGltfResourceResolver()
    {
        Resolver = new FileResourceResolver();
    }

    /// <summary>Resolves one resource request.</summary>
    /// <param name="request">The resource request.</param>
    /// <returns>The resolved resource and operation status.</returns>
    public GltfResource ResolveResource(GltfResourceRequest request)
    {
        return Resolver.ResolveResource(request);
    }
}
