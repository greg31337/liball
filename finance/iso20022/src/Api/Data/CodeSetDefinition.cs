using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes an internal, external, or derived string-valued code set.</summary>
public class CodeSetDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The code-set source or derivation kind.</summary>
    public CodeSetKind Kind;
    /// <summary>The code-set name.</summary>
    public string Name = string.Empty;
    /// <summary>The semantic meaning of the code set.</summary>
    public string Definition = string.Empty;
    /// <summary>The URI identifying the code set when present.</summary>
    public string IdentificationScheme = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The data dictionary that contains an internal code set.</summary>
    public RepositoryReference DataDictionary = new RepositoryReference();
    /// <summary>The string facets applied to code values.</summary>
    public List<DataTypeFacet> Facets = new List<DataTypeFacet>();
    /// <summary>The code set from which this set is derived when present.</summary>
    public RepositoryReference? BaseCodeSet;
    /// <summary>The code sets derived from this set.</summary>
    public List<RepositoryReference> DerivedCodeSets = new List<RepositoryReference>();
    /// <summary>The ordered code values owned by the set.</summary>
    public List<CodeValueDefinition> Codes = new List<CodeValueDefinition>();
    /// <summary>Unrecognized fields attached to the code set.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the code set.</summary>
    public SourceLocation Location = new SourceLocation();
}
