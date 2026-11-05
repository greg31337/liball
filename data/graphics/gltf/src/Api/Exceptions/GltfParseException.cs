namespace gltf;

/// <summary>Represents malformed JSON, GLB data, or unsafe declared input ranges.</summary>
public class GltfParseException : System.Exception
{
    /// <summary>Initializes a parse exception with a stable diagnostic message.</summary>
    /// <param name="message">The concise parse failure message.</param>
    public GltfParseException(string message) : base(message)
    {
    }
}
