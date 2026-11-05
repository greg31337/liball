namespace asn1;

/// <summary>
/// Contains reflection metadata for one ASN.1 assignment.
/// </summary>
public class AsnAssignmentDescriptor
{
    /// <summary>
    /// Contains the stable assignment identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the ASN.1 assignment name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the qualified module and assignment name.
    /// </summary>
    public string QualifiedName = "";

    /// <summary>
    /// Contains the generated assignment name.
    /// </summary>
    public string GeneratedName = "";

    /// <summary>
    /// Contains the assignment category.
    /// </summary>
    public AsnAssignmentKind Kind;

    /// <summary>
    /// Indicates whether the assignment is exported.
    /// </summary>
    public bool Exported;

    /// <summary>
    /// Indicates whether the assignment declares formal parameters.
    /// </summary>
    public bool Parameterized;

    /// <summary>
    /// Contains formal parameters in declaration order.
    /// </summary>
    public AsnParameterDescriptor[] Parameters = [];

    /// <summary>
    /// Contains the reflected type when present.
    /// </summary>
    public AsnTypeDescriptor? Type;

    /// <summary>
    /// Contains the reflected value when present.
    /// </summary>
    public AsnValueDescriptor? Value;

    /// <summary>
    /// Contains the reflected object class when present.
    /// </summary>
    public AsnObjectClass? ObjectClass;

    /// <summary>
    /// Contains the reflected information object when present.
    /// </summary>
    public AsnInformationObject? Object;

    /// <summary>
    /// Contains the reflected information object set when present.
    /// </summary>
    public AsnInformationObjectSet? ObjectSet;

    /// <summary>
    /// Contains object tables associated with this assignment.
    /// </summary>
    public AsnObjectTableDescriptor[] ObjectTables = [];

    /// <summary>
    /// Contains the assignment source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the assignment.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];
}
