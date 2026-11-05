using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one registered code value owned by a repository code set.</summary>
public class CodeValueDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The exact abbreviated code value.</summary>
    public string Value = string.Empty;
    /// <summary>The full non-abbreviated code name.</summary>
    public string Name = string.Empty;
    /// <summary>The semantic meaning of the code.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The code set that owns the value.</summary>
    public RepositoryReference Owner = new RepositoryReference();
    /// <summary>Unrecognized fields attached to the code.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the code.</summary>
    public SourceLocation Location = new SourceLocation();
}
