namespace fix;

/// <summary>Represents one generated file returned to the caller.</summary>
public class GeneratedFile
{
    /// <summary>The relative output path.</summary>
    public string Path = "";
    /// <summary>The complete text content.</summary>
    public string Content = "";
    /// <summary>A deterministic content hash.</summary>
    public string ContentHash = "";
    /// <summary>Whether the output matches a caller-provided prior manifest.</summary>
    public bool IsUnchanged;
}
