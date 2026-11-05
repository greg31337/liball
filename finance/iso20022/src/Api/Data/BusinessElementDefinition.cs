using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a business attribute or association end within a business component.</summary>
public class BusinessElementDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The normalized business element subtype.</summary>
    public MessageElementKind Kind;
    /// <summary>The business element name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the element.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The element multiplicity.</summary>
    public MultiplicityDefinition Multiplicity = new MultiplicityDefinition();
    /// <summary>Whether the element can be computed from other business elements.</summary>
    public bool IsDerived;
    /// <summary>The business component that owns the element.</summary>
    public RepositoryReference ElementContext = new RepositoryReference();
    /// <summary>The effective business element type.</summary>
    public RepositoryReference MemberType = new RepositoryReference();
    /// <summary>The simple type of an attribute when declared.</summary>
    public RepositoryReference? SimpleType;
    /// <summary>The complex type of an attribute or association end when declared.</summary>
    public RepositoryReference? ComplexType;
    /// <summary>The opposite end of a bidirectional business association when declared.</summary>
    public RepositoryReference? Opposite;
    /// <summary>The exact aggregation value of an association end.</summary>
    public string Aggregation = string.Empty;
    /// <summary>The message elements derived from this business element.</summary>
    public List<RepositoryReference> DerivedMessageElements = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the element.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the element.</summary>
    public SourceLocation Location = new SourceLocation();
}
