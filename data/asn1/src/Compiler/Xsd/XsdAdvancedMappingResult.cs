namespace asn1;

internal class XsdAdvancedMappingResult
{
    internal bool Success;
    internal XsdWildcardDefinition[] Wildcards = [];
    internal XsdSubstitutionGroupDefinition[] SubstitutionGroups = [];
    internal XsdTypeSubstitutionDefinition[] TypeSubstitutions = [];
    internal AsnDiagnostic[] Diagnostics = [];
}
