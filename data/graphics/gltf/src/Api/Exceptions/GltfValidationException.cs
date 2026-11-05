namespace gltf;

/// <summary>Represents a validation failure reported by a strict operation.</summary>
public class GltfValidationException : System.Exception
{
    /// <summary>Initializes a validation exception with a stable diagnostic message.</summary>
    /// <param name="message">The concise validation failure message.</param>
    public GltfValidationException(string message) : base(message)
    {
    }
}
