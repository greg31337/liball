using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes an external-schema or legacy user-defined message content model.</summary>
public class ExternalSchemaDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The normalized external content-model family.</summary>
    public DataTypeKind Kind;
    /// <summary>The exact source metamodel type name.</summary>
    public string SourceTypeName = string.Empty;
    /// <summary>The external schema definition name.</summary>
    public string Name = string.Empty;
    /// <summary>The semantic meaning of the external structure.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The data dictionary that contains the external structure definition.</summary>
    public RepositoryReference DataDictionary = new RepositoryReference();
    /// <summary>The namespace selection value, such as <c>##any</c>, <c>##other</c>, or <c>list</c>.</summary>
    public string NamespaceMode = string.Empty;
    /// <summary>The ordered namespaces allowed by the external content model.</summary>
    public List<string> NamespaceUris = new List<string>();
    /// <summary>The exact validation mode, such as <c>LAX</c>, <c>SKIP</c>, or <c>STRICT</c>.</summary>
    public string ProcessContents = string.Empty;
    /// <summary>The occurrence bounds of an imported XSD wildcard particle.</summary>
    public MultiplicityDefinition Multiplicity = new MultiplicityDefinition();
    /// <summary>Whether the type has no trace to an ISO 20022 business component.</summary>
    public bool IsTechnical;
    /// <summary>The business component from which the type is derived when present.</summary>
    public RepositoryReference? BusinessComponentTrace;
    /// <summary>The message building blocks typed by this external model.</summary>
    public List<RepositoryReference> MessageBuildingBlocks = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the external content model.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the external content model.</summary>
    public SourceLocation Location = new SourceLocation();
}
