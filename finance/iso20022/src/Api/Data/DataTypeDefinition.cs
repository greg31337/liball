using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes any normalized ISO 20022 primitive, value, or schema data-type family.</summary>
public class DataTypeDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The normalized data-type family.</summary>
    public DataTypeKind Kind;
    /// <summary>The exact source metamodel type name.</summary>
    public string SourceTypeName = string.Empty;
    /// <summary>The data type name.</summary>
    public string Name = string.Empty;
    /// <summary>The semantic meaning of the data type.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The data dictionary that contains the type.</summary>
    public RepositoryReference DataDictionary = new RepositoryReference();
    /// <summary>The ordered facets applied to the type.</summary>
    public List<DataTypeFacet> Facets = new List<DataTypeFacet>();
    /// <summary>The URI identifying an identifier set.</summary>
    public string IdentificationScheme = string.Empty;
    /// <summary>The semantic meaning of an indicator set to true.</summary>
    public string MeaningWhenTrue = string.Empty;
    /// <summary>The semantic meaning of an indicator set to false.</summary>
    public string MeaningWhenFalse = string.Empty;
    /// <summary>The base ratio value for a rate.</summary>
    public IsoDecimalValue? BaseValue;
    /// <summary>The unit code qualifying a quantity or rate base.</summary>
    public string UnitCode = string.Empty;
    /// <summary>The data type that defines allowed currencies for an amount.</summary>
    public RepositoryReference? CurrencyIdentifierSet;
    /// <summary>The lexical encoding of a binary family when constrained.</summary>
    public BinaryEncodingKind BinaryEncoding;
    /// <summary>The exact XML Schema built-in type name for a schema type.</summary>
    public string SchemaTypeName = string.Empty;
    /// <summary>The business elements typed by this data type.</summary>
    public List<RepositoryReference> BusinessElements = new List<RepositoryReference>();
    /// <summary>The message members typed by this data type.</summary>
    public List<RepositoryReference> MessageMembers = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the data type.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the data type.</summary>
    public SourceLocation Location = new SourceLocation();
}
