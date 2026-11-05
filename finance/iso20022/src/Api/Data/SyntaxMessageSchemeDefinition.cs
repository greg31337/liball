using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a syntax-processable scheme derived from one message definition.</summary>
public class SyntaxMessageSchemeDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The syntax message scheme name.</summary>
    public string Name = string.Empty;
    /// <summary>The semantic meaning of the scheme.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process catalogue that contains the scheme.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The message definition from which the scheme is derived.</summary>
    public RepositoryReference MessageDefinitionTrace = new RepositoryReference();
    /// <summary>The syntax used by the scheme when declared by an adapter.</summary>
    public RepositoryReference? Syntax;
    /// <summary>The encoding used by the scheme when declared by an adapter.</summary>
    public RepositoryReference? Encoding;
    /// <summary>Unrecognized fields attached to the scheme.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the scheme.</summary>
    public SourceLocation Location = new SourceLocation();
}
