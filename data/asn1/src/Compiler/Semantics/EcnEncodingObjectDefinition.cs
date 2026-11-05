namespace asn1;

internal class EcnEncodingObjectDefinition
{
    internal string Name = "";
    internal string GovernorName = "";
    internal string ReferencedObjectName = "";
    internal EcnSymbol? Symbol;
    internal EcnSymbol? GovernorSymbol;
    internal EcnSymbol? ReferencedObjectSymbol;
    internal EcnEncodingObjectSetting[] Settings = [];
    internal EcnOrderedEncodingObjectList[] OrderedLists = [];
    internal EcnEncodingProperty[] Properties = [];
    internal EcnConstructorDefinition? Constructor;
    internal EcnEncodingLink[] Links = [];
    internal EcnDeterminantRelationship[] Determinants = [];
    internal EcnValueMapping[] Mappings = [];
    internal EcnConversionDefinition? Conversion;
    internal EcnExecutablePlan? Plan;
    internal EcnActualParameter[] GovernorActualParameters = [];
    internal EcnActualParameter[] ReferenceActualParameters = [];
    internal AsnToken[] SourceTokens = [];
    internal int PropertyResolutionState;
    internal int ConstructorResolutionState;
    internal int LinkResolutionState;
    internal int ConversionResolutionState;
    internal int PlanResolutionState;
    internal bool GovernorBuiltIn;
    internal bool Parameterized;
    internal bool ReferenceDefinition;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
