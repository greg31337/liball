namespace gltf;

/// <summary>Represents a missing required extension, codec, or transform capability.</summary>
public class GltfUnsupportedFeatureException : System.Exception
{
    /// <summary>Initializes an unsupported-feature exception with a stable diagnostic message.</summary>
    /// <param name="message">The concise unsupported-feature message.</param>
    public GltfUnsupportedFeatureException(string message) : base(message)
    {
    }
}
