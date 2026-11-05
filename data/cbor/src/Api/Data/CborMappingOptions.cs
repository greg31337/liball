using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Holds policies and limits for mapping between CBOR values and runtime objects.
/// </summary>
public class CborMappingOptions
{
    /// <summary>Indicates whether objects are represented as positional arrays instead of maps.</summary>
    public bool MapObjectsAsArrays;
    /// <summary>Indicates whether unrecognized encoded members are ignored.</summary>
    public bool IgnoreUnknownMembers;
    /// <summary>Indicates whether every target member must be present in the encoded value.</summary>
    public bool RequireAllMembers = true;
    /// <summary>The naming policy for reflected object member keys.</summary>
    public CborMappingNamingPolicy MemberNamingPolicy = CborMappingNamingPolicy.Exact;
    /// <summary>The policy for native dictionary keys.</summary>
    public CborMappingKeyPolicy DictionaryKeyPolicy = CborMappingKeyPolicy.Any;
    /// <summary>Indicates whether narrowing integer conversions are permitted.</summary>
    public bool AllowIntegerNarrowing;
    /// <summary>Indicates whether shared and cyclic object references are preserved.</summary>
    public bool PreserveObjectReferences;
    /// <summary>The maximum nested mapping depth.</summary>
    public int MaxNestingDepth = 64;
    /// <summary>The maximum number of members in one mapped object or collection.</summary>
    public int MaxCollectionItems = 100000;
    /// <summary>The maximum total number of values processed by one mapping operation.</summary>
    public int MaxTotalItems = 1000000;
    /// <summary>The registered custom runtime type adapters.</summary>
    public List<CborTypeRegistration> TypeRegistrations = new List<CborTypeRegistration>();
}
