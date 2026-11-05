using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Contains one independently registered set of Diameter dictionary definitions.
/// </summary>
public class DiameterDictionaryModule
{
    /// <summary>The unique canonical module name.</summary>
    public string Name = string.Empty;

    /// <summary>The module version text.</summary>
    public string Version = string.Empty;

    /// <summary>The canonical names of modules that must be composed first.</summary>
    public List<string> Dependencies = new List<string>();

    /// <summary>The vendor definitions contributed by the module.</summary>
    public List<DiameterVendorDefinition> Vendors = new List<DiameterVendorDefinition>();

    /// <summary>The application definitions contributed by the module.</summary>
    public List<DiameterApplicationDefinition> Applications =
        new List<DiameterApplicationDefinition>();

    /// <summary>The base and derived type definitions contributed by the module.</summary>
    public List<DiameterTypeDefinition> Types = new List<DiameterTypeDefinition>();

    /// <summary>The named enumerated values contributed by the module.</summary>
    public List<DiameterEnumValueDefinition> EnumValues =
        new List<DiameterEnumValueDefinition>();

    /// <summary>The AVP definitions contributed by the module.</summary>
    public List<DiameterAvpDefinition> Avps = new List<DiameterAvpDefinition>();

    /// <summary>The request and answer command definitions contributed by the module.</summary>
    public List<DiameterCommandDefinition> Commands =
        new List<DiameterCommandDefinition>();
}

/// <summary>
/// Defines a Diameter vendor numeric identity and canonical name.
/// </summary>
public class DiameterVendorDefinition
{
    /// <summary>The Vendor-ID assigned to the vendor.</summary>
    public uint Id;

    /// <summary>The canonical vendor name.</summary>
    public string Name = string.Empty;
}

/// <summary>
/// Defines a Diameter authentication or accounting application.
/// </summary>
public class DiameterApplicationDefinition
{
    /// <summary>The application identifier.</summary>
    public uint Id;

    /// <summary>The owning Vendor-ID, or zero for an IETF application.</summary>
    public uint VendorId;

    /// <summary>The canonical application name.</summary>
    public string Name = string.Empty;

    /// <summary>Indicates that the identifier represents an accounting application.</summary>
    public bool IsAccounting;
}

/// <summary>
/// Defines a named base or derived Diameter AVP value type.
/// </summary>
public class DiameterTypeDefinition
{
    /// <summary>The canonical type name.</summary>
    public string Name = string.Empty;

    /// <summary>The defining Vendor-ID, or zero for an IETF type.</summary>
    public uint VendorId;

    /// <summary>The wire-level base type used by the value.</summary>
    public DiameterAvpBaseType BaseType;

    /// <summary>The canonical parent type name, or an empty string for a base type.</summary>
    public string ParentTypeName = string.Empty;

    /// <summary>The parent type Vendor-ID, or zero for an IETF parent type.</summary>
    public uint ParentTypeVendorId;

    /// <summary>The fixed data size in octets, or <c>-1</c> when the size is variable.</summary>
    public int FixedDataLength = -1;
}

/// <summary>
/// Defines one named signed 32-bit value in an Enumerated AVP domain.
/// </summary>
public class DiameterEnumValueDefinition
{
    /// <summary>The canonical enumerated value name.</summary>
    public string Name = string.Empty;

    /// <summary>The signed 32-bit wire value.</summary>
    public int Value;

    /// <summary>The canonical derived type name containing the value.</summary>
    public string TypeName = string.Empty;

    /// <summary>The derived type Vendor-ID, or zero for an IETF type.</summary>
    public uint TypeVendorId;

    /// <summary>The associated AVP code, or zero when the value belongs only to a type.</summary>
    public uint AvpCode;

    /// <summary>The associated AVP Vendor-ID, or zero for an IETF AVP.</summary>
    public uint AvpVendorId;
}

/// <summary>
/// Defines a Diameter AVP, its value type, legal flags, and optional Grouped rules.
/// </summary>
public class DiameterAvpDefinition
{
    /// <summary>The AVP code.</summary>
    public uint Code;

    /// <summary>The Vendor-ID, or zero for an IETF AVP.</summary>
    public uint VendorId;

    /// <summary>The canonical AVP name.</summary>
    public string Name = string.Empty;

    /// <summary>The canonical base or derived value type name.</summary>
    public string TypeName = string.Empty;

    /// <summary>The wire-level base type used by the AVP.</summary>
    public DiameterAvpBaseType BaseType;

    /// <summary>The AVP flag bits that must be present.</summary>
    public DiameterAvpFlags RequiredFlags;

    /// <summary>The AVP flag bits that must not be present.</summary>
    public DiameterAvpFlags ProhibitedFlags;

    /// <summary>The fixed data size in octets, or <c>-1</c> when the size is variable.</summary>
    public int FixedDataLength = -1;

    /// <summary>The occurrence and ordering rules for AVPs within a Grouped value.</summary>
    public List<DiameterAvpRuleDefinition> GroupedRules =
        new List<DiameterAvpRuleDefinition>();

    /// <summary>Allows AVPs not listed in <c>GroupedRules</c>.</summary>
    public bool AllowUnlistedAvps = true;
}

/// <summary>
/// Defines a Diameter request or answer command and its AVP occurrence rules.
/// </summary>
public class DiameterCommandDefinition
{
    /// <summary>The command code.</summary>
    public uint Code;

    /// <summary>The application identifier carried in the message header.</summary>
    public uint ApplicationId;

    /// <summary>The canonical command name.</summary>
    public string Name = string.Empty;

    /// <summary>Indicates that this definition describes the request form.</summary>
    public bool IsRequest;

    /// <summary>The message flag bits that must be present.</summary>
    public DiameterMessageFlags RequiredFlags;

    /// <summary>The message flag bits that must not be present.</summary>
    public DiameterMessageFlags ProhibitedFlags;

    /// <summary>The occurrence and ordering rules for AVPs in the command.</summary>
    public List<DiameterAvpRuleDefinition> Rules =
        new List<DiameterAvpRuleDefinition>();

    /// <summary>Allows extension AVPs not listed in <c>Rules</c>.</summary>
    public bool AllowUnlistedAvps = true;
}

/// <summary>
/// Defines occurrence, fixed-head, and ordering constraints for one AVP.
/// </summary>
public class DiameterAvpRuleDefinition
{
    /// <summary>The constrained AVP code.</summary>
    public uint AvpCode;

    /// <summary>The constrained AVP Vendor-ID, or zero for an IETF AVP.</summary>
    public uint AvpVendorId;

    /// <summary>The canonical AVP name used for diagnostics.</summary>
    public string AvpName = string.Empty;

    /// <summary>The minimum required occurrence count.</summary>
    public int MinimumCount;

    /// <summary>The maximum occurrence count, or <c>-1</c> for no explicit maximum.</summary>
    public int MaximumCount = 1;

    /// <summary>The zero-based fixed-head position, or <c>-1</c> when not fixed.</summary>
    public int FixedHeadPosition = -1;

    /// <summary>The ordered group number, or <c>-1</c> when ordering is unconstrained.</summary>
    public int OrderingGroup = -1;
}

/// <summary>
/// Contains a composed dictionary revision and its flattened definitions.
/// </summary>
public class DiameterDictionarySnapshot
{
    /// <summary>The monotonically increasing composition revision.</summary>
    public long Revision;

    /// <summary>The registered modules in deterministic composition order.</summary>
    public List<DiameterDictionaryModule> Modules = new List<DiameterDictionaryModule>();

    /// <summary>The composed vendor definitions.</summary>
    public List<DiameterVendorDefinition> Vendors = new List<DiameterVendorDefinition>();

    /// <summary>The composed application definitions.</summary>
    public List<DiameterApplicationDefinition> Applications =
        new List<DiameterApplicationDefinition>();

    /// <summary>The composed base and derived type definitions.</summary>
    public List<DiameterTypeDefinition> Types = new List<DiameterTypeDefinition>();

    /// <summary>The composed named enumerated value definitions.</summary>
    public List<DiameterEnumValueDefinition> EnumValues =
        new List<DiameterEnumValueDefinition>();

    /// <summary>The composed AVP definitions.</summary>
    public List<DiameterAvpDefinition> Avps = new List<DiameterAvpDefinition>();

    /// <summary>The composed request and answer command definitions.</summary>
    public List<DiameterCommandDefinition> Commands =
        new List<DiameterCommandDefinition>();
}

/// <summary>
/// Contains the outcome and resulting snapshot of dictionary module registration.
/// </summary>
public class DiameterDictionaryRegistrationResult
{
    /// <summary>The registration outcome.</summary>
    public DiameterDictionaryRegistrationStatus Status;

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;

    /// <summary>The immutable snapshot active after the registration attempt.</summary>
    public DiameterDictionarySnapshot Snapshot = new DiameterDictionarySnapshot();
}
