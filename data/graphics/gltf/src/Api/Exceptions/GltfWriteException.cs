namespace gltf;

/// <summary>Represents invalid or unrepresentable output and resource layouts.</summary>
public class GltfWriteException : System.Exception
{
    /// <summary>Initializes a write exception with a stable diagnostic message.</summary>
    /// <param name="message">The concise write failure message.</param>
    public GltfWriteException(string message) : base(message)
    {
    }
}
