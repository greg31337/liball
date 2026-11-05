namespace gltf;

/// <summary>Represents a required buffer, image, URI, or resource resolution failure.</summary>
public class GltfResourceException : System.Exception
{
    /// <summary>Initializes a resource exception with a stable diagnostic message.</summary>
    /// <param name="message">The concise resource failure message.</param>
    public GltfResourceException(string message) : base(message)
    {
    }
}
