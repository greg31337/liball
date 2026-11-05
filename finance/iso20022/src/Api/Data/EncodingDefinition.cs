using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a set of encoding rules applied to a message syntax.</summary>
public class EncodingDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The encoding name when supplied by the source adapter.</summary>
    public string Name = string.Empty;
    /// <summary>The encoding description when supplied by the source adapter.</summary>
    public string Definition = string.Empty;
    /// <summary>The lifecycle and model-version metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The syntax to which this encoding applies.</summary>
    public RepositoryReference Syntax = new RepositoryReference();
    /// <summary>The message sets for which this encoding is valid.</summary>
    public List<RepositoryReference> MessageSets = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the encoding.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the encoding.</summary>
    public SourceLocation Location = new SourceLocation();
}
