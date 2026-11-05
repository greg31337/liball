namespace gltf;

internal class GltfImplementationShell
{
    internal GltfUnsupportedFeatureException CreateUnavailableException(string operation)
    {
        return new GltfUnsupportedFeatureException($"The {operation} operation is not implemented.");
    }
}
