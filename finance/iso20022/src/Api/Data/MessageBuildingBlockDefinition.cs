using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one typed top-level member of a message definition.</summary>
public class MessageBuildingBlockDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The building block name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the building block.</summary>
    public string Definition = string.Empty;
    /// <summary>The XML element tag.</summary>
    public string XmlTag = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The building block multiplicity.</summary>
    public MultiplicityDefinition Multiplicity = new MultiplicityDefinition();
    /// <summary>The message definition that contains the building block.</summary>
    public RepositoryReference MessageDefinition = new RepositoryReference();
    /// <summary>The effective logical member type.</summary>
    public RepositoryReference MemberType = new RepositoryReference();
    /// <summary>The simple data type when declared.</summary>
    public RepositoryReference? SimpleType;
    /// <summary>The complex message component type when declared.</summary>
    public RepositoryReference? ComplexType;
    /// <summary>Unrecognized fields attached to the building block.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the building block.</summary>
    public SourceLocation Location = new SourceLocation();
}
