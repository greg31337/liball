using System.Collections.Generic;

namespace iso20022;

/// <summary>Preserves an unrecognized repository type, element, attribute, or extension value.</summary>
public class RepositoryUnknownData
{
    /// <summary>The XMI identifier of the entity that owns the unknown content.</summary>
    public string OwnerIdentifier = string.Empty;
    /// <summary>The normalized kind of the owning entity when known.</summary>
    public RepositoryEntityKind OwnerKind;
    /// <summary>The XMI identifier carried by the unknown entity when present.</summary>
    public string Identifier = string.Empty;
    /// <summary>The unrecognized metamodel type name, including its source prefix when present.</summary>
    public string TypeName = string.Empty;
    /// <summary>The qualified XML name of the unknown content.</summary>
    public QualifiedNameData Name = new QualifiedNameData();
    /// <summary>Whether this content originated from an XML attribute.</summary>
    public bool IsAttribute;
    /// <summary>The decoded lexical value for an attribute or text-only element.</summary>
    public string Value = string.Empty;
    /// <summary>The preserved XML representation for structured unknown content.</summary>
    public RawXmlFragment? RawContent;
    /// <summary>The references discovered in the unknown content.</summary>
    public List<RepositoryReference> References = new List<RepositoryReference>();
    /// <summary>The ordered nested unknown content.</summary>
    public List<RepositoryUnknownData> Children = new List<RepositoryUnknownData>();
    /// <summary>The source location of the unknown content.</summary>
    public SourceLocation Location = new SourceLocation();
}
