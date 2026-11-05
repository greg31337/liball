namespace asn1;

internal enum LoweredDerivationKind
{
    Alias = 0,
    ComponentsOf = 1,
    ParameterInstantiation = 2,
    AutomaticTag = 3,
    XsdMapping = 4,
    EncodingControl = 5
}

internal class LoweredDerivationIr
{
    internal string StableId = "";
    internal LoweredDerivationKind Kind;
    internal string Name = "";
    internal AsnSourceSpan? Span;
}

internal class LoweredCompilationIr
{
    internal string StableId = "ir:compilation";
    internal AsnPerInstructionRegistration[] PerInstructionRegistrations = [];
    internal LoweredModuleIr[] Modules = [];
}

internal class LoweredModuleIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal int EmissionOrder;
    internal string Name = "";
    internal string ObjectIdentifier = "";
    internal string Iri = "";
    internal string VersionModule = "";
    internal string Version = "";
    internal AsnTaggingMode TagDefault;
    internal bool ExtensibilityImplied;
    internal LoweredImportIr[] Imports = [];
    internal LoweredAssignmentIr[] Assignments = [];
    internal LoweredInstructionIr[] Instructions = [];
    internal AsnPerInstructionRegistration[] PerInstructionRegistrations = [];
}

internal class LoweredImportIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string ModuleName = "";
    internal string[] Symbols = [];
}

internal class LoweredAssignmentIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal int EmissionOrder;
    internal string Name = "";
    internal string QualifiedName = "";
    internal AsnAssignmentKind Kind;
    internal bool Exported;
    internal bool Parameterized;
    internal LoweredParameterIr[] Parameters = [];
    internal LoweredTypeIr? Type;
    internal LoweredValueIr? Value;
    internal LoweredObjectClassIr? ObjectClass;
    internal LoweredObjectIr? Object;
    internal LoweredObjectSetIr? ObjectSet;
}

internal class LoweredParameterIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string Name = "";
    internal AsnParameterKind Kind;
    internal AsnTypeKind GovernorTypeKind;
    internal string GovernorObjectClassName = "";
}

internal class LoweredTypeIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string Name = "";
    internal AsnTypeKind Kind;
    internal bool Extensible;
    internal bool ParameterReference;
    internal LoweredTagIr[] Tags = [];
    internal LoweredFieldIr[] Fields = [];
    internal LoweredTypeIr? ElementType;
    internal LoweredNamedValueIr[] NamedValues = [];
    internal LoweredConstraintIr[] Constraints = [];
    internal LoweredExtensionPartitionIr Extensions = new LoweredExtensionPartitionIr();
    internal LoweredObjectTableIr[] ObjectTables = [];
    internal LoweredInstructionIr[] Instructions = [];
    internal AsnEncodingPlanStep[] PerInstructionPlanSteps = [];
}

internal class LoweredFieldIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string Name = "";
    internal LoweredTypeIr? Type;
    internal bool Optional;
    internal bool HasDefaultValue;
    internal LoweredValueIr? DefaultValue;
    internal bool Extension;
    internal int ExtensionGroupIndex = -1;
    internal LoweredTagIr[] Tags = [];
    internal LoweredInstructionIr[] Instructions = [];
    internal AsnEncodingPlanStep[] PerInstructionPlanSteps = [];
}

internal class LoweredTagIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal AsnTagClass Class;
    internal AsnTaggingMode Mode;
    internal System.Numerics.BigInteger Number;
    internal bool Universal;
    internal bool AutomaticallyGenerated;
}

internal class LoweredValueIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal AsnValueKind Kind;
    internal bool HasIntegerValue;
    internal System.Numerics.BigInteger IntegerValue;
    internal string TextValue = "";
    internal byte[] Octets = [];
    internal AsnBitStringValue? BitStringValue;
    internal AsnObjectIdentifierValue? ObjectIdentifierValue;
    internal AsnRealValue? RealValue;
    internal AsnTimeValue? TimeValue;
    internal LoweredValueIr[] Items = [];
    internal LoweredNamedValueIr[] NamedValues = [];
}

internal class LoweredNamedValueIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string Name = "";
    internal LoweredValueIr? Value;
}

internal class LoweredConstraintIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal AsnTypeKind DomainKind;
    internal LoweredConstraintNodeIr? Root;
    internal LoweredConstraintNodeIr? Extension;
    internal LoweredConstraintIr[] Applications = [];
    internal LoweredIntervalIr[] RootIntervals = [];
    internal LoweredIntervalIr[] ExtensionIntervals = [];
    internal bool Extensible;
    internal bool OpenExtension;
}

internal class LoweredConstraintNodeIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal AsnConstraintKind Kind;
    internal string Name = "";
    internal string Text = "";
    internal LoweredValueIr? Value;
    internal LoweredValueIr? LowerValue;
    internal LoweredValueIr? UpperValue;
    internal System.Numerics.BigInteger LowerBound;
    internal System.Numerics.BigInteger UpperBound;
    internal bool HasLowerBound;
    internal bool HasUpperBound;
    internal bool LowerEndpointIncluded;
    internal bool UpperEndpointIncluded;
    internal LoweredConstraintNodeIr[] Children = [];
    internal LoweredConstraintIr[] NestedConstraints = [];
}

internal class LoweredIntervalIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal System.Numerics.BigInteger LowerBound;
    internal System.Numerics.BigInteger UpperBound;
    internal bool HasLowerBound;
    internal bool HasUpperBound;
}

internal class LoweredExtensionPartitionIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal bool Extensible;
    internal LoweredFieldIr[] RootFields = [];
    internal LoweredFieldIr[] AdditionalFields = [];
    internal LoweredNamedValueIr[] RootNamedValues = [];
    internal LoweredNamedValueIr[] AdditionalNamedValues = [];
}

internal class LoweredObjectClassIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string Name = "";
    internal LoweredObjectClassFieldIr[] Fields = [];
    internal string[] SyntaxTokens = [];
}

internal class LoweredObjectClassFieldIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string Name = "";
    internal AsnObjectFieldKind Kind;
    internal LoweredTypeIr? Type;
    internal string GovernorObjectClassName = "";
    internal string[] GovernorFieldPath = [];
    internal bool Optional;
    internal bool Unique;
    internal bool HasDefaultSetting;
    internal LoweredObjectFieldSettingIr? DefaultSetting;
}

internal class LoweredObjectIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string Name = "";
    internal string ObjectClassName = "";
    internal LoweredObjectFieldSettingIr[] FieldSettings = [];
}

internal class LoweredObjectFieldSettingIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string FieldName = "";
    internal LoweredTypeIr? Type;
    internal LoweredTypeIr? ValueSetType;
    internal LoweredValueIr? Value;
    internal string ObjectStableId = "";
    internal string ObjectSetStableId = "";
}

internal class LoweredObjectSetIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string Name = "";
    internal string ObjectClassName = "";
    internal bool Extensible;
    internal LoweredObjectIr[] RootObjects = [];
    internal LoweredObjectIr[] AdditionalObjects = [];
    internal LoweredObjectTableIr? Table;
}

internal class LoweredObjectTableIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal AsnConstraintKind Kind;
    internal string ObjectClassName = "";
    internal string UniqueFieldName = "";
    internal LoweredObjectTableEntryIr[] Entries = [];
}

internal class LoweredObjectTableEntryIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal LoweredValueIr? Key;
    internal LoweredTypeIr? Type;
}

internal class LoweredInstructionIr
{
    internal string StableId = "";
    internal AsnSourceSpan? SourceSpan;
    internal LoweredDerivationIr[] Derivations = [];
    internal string EncodingReference = "";
    internal string Name = "";
    internal AsnInstructionKind Kind;
    internal bool Negated;
    internal string[] Arguments = [];
    internal string[] Targets = [];
}
