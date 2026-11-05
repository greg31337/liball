using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains one deterministic generated file, its ownership metadata, content, and hash.</summary>
public class GeneratedFile
{
    /// <summary>The generated-file role and serialization format.</summary>
    public GenerationFileKind Kind;
    /// <summary>The normalized relative output path using forward-slash separators.</summary>
    public string RelativePath = string.Empty;
    /// <summary>The generated content media type.</summary>
    public string MediaType = string.Empty;
    /// <summary>The primary C#, XML, or schema namespace represented by the file.</summary>
    public string NamespaceName = string.Empty;
    /// <summary>The immutable-by-convention encoded generated content.</summary>
    public byte[] Content = System.Array.Empty<byte>();
    /// <summary>The generated content hash.</summary>
    public IsoBinaryValue Hash = new IsoBinaryValue();
    /// <summary>The name of the generated content-hash algorithm.</summary>
    public string HashAlgorithm = string.Empty;
    /// <summary>The exact source identifiers whose symbols are owned by the file.</summary>
    public List<string> SourceIdentifiers = new List<string>();
    /// <summary>Whether the content hash matches the file owned by the previous manifest.</summary>
    public bool IsUnchanged;
}
