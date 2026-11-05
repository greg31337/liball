using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a processable notation used to represent message instances.</summary>
public class SyntaxDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The syntax name when supplied by the source adapter.</summary>
    public string Name = string.Empty;
    /// <summary>The syntax description when supplied by the source adapter.</summary>
    public string Definition = string.Empty;
    /// <summary>The lifecycle and model-version metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The encodings possible for the syntax.</summary>
    public List<RepositoryReference> PossibleEncodings = new List<RepositoryReference>();
    /// <summary>The message sets for which the syntax is generated.</summary>
    public List<RepositoryReference> GeneratedForMessageSets = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the syntax.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the syntax.</summary>
    public SourceLocation Location = new SourceLocation();
}
