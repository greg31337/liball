using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a registered set of message definitions and its valid encodings.</summary>
public class MessageSetDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The message set name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the message set.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process catalogue that contains the set.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The syntaxes generated for the set.</summary>
    public List<RepositoryReference> GeneratedSyntaxes = new List<RepositoryReference>();
    /// <summary>The encodings valid for the set.</summary>
    public List<RepositoryReference> ValidEncodings = new List<RepositoryReference>();
    /// <summary>The message definitions belonging to the set.</summary>
    public List<RepositoryReference> MessageDefinitions = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the set.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the message set.</summary>
    public SourceLocation Location = new SourceLocation();
}
