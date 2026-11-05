namespace asn1;

/// <summary>
/// Contains stable reflection metadata for a compiled schema.
/// </summary>
public class AsnReflectionCatalog
{
    internal EcnCodecPlanSelection[] EcnCodecPlans = [];

    /// <summary>
    /// Contains the catalog format identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains module descriptors in deterministic order.
    /// </summary>
    public AsnModuleDescriptor[] Modules = [];

    /// <summary>
    /// Contains every assignment in deterministic module and emission order.
    /// </summary>
    public AsnAssignmentDescriptor[] Assignments = [];

    /// <summary>
    /// Contains the registry for dynamic additions shared by users of this catalog.
    /// </summary>
    public IAsnObjectSetRegistry ObjectSetRegistry = new AsnObjectSetRegistry();
}
