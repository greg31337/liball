using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a message attribute or association end within a message component or choice.</summary>
public class MessageElementDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The normalized message element subtype.</summary>
    public MessageElementKind Kind;
    /// <summary>The message element name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the element.</summary>
    public string Definition = string.Empty;
    /// <summary>The XML element tag.</summary>
    public string XmlTag = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The message element multiplicity.</summary>
    public MultiplicityDefinition Multiplicity = new MultiplicityDefinition();
    /// <summary>Whether the element has no business-component or business-element trace.</summary>
    public bool IsTechnical;
    /// <summary>Whether the element can be computed from other message elements.</summary>
    public bool IsDerived;
    /// <summary>Whether an association end uses composite containment.</summary>
    public bool IsComposite;
    /// <summary>The message component or choice that contains the element.</summary>
    public RepositoryReference ComponentContext = new RepositoryReference();
    /// <summary>The effective logical member type.</summary>
    public RepositoryReference MemberType = new RepositoryReference();
    /// <summary>The simple data type of an attribute when declared.</summary>
    public RepositoryReference? SimpleType;
    /// <summary>The complex component type of an attribute or association end when declared.</summary>
    public RepositoryReference? ComplexType;
    /// <summary>The business component from which the element is derived when present.</summary>
    public RepositoryReference? BusinessComponentTrace;
    /// <summary>The business element from which the element is derived when present.</summary>
    public RepositoryReference? BusinessElementTrace;
    /// <summary>Additional derivation targets retained by future metamodel adapters.</summary>
    public List<RepositoryReference> Derivations = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the element.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the element.</summary>
    public SourceLocation Location = new SourceLocation();
}
