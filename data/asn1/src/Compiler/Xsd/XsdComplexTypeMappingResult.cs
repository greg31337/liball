namespace asn1;

internal class XsdComplexTypeMappingResult
{
    internal bool Success;
    internal XsdComplexTypeDefinition[] Types = [];
    internal XsdModelGroupDefinition[] ModelGroups = [];
    internal XsdModelGroupDefinition[] AttributeGroups = [];
    internal AsnDiagnostic[] Diagnostics = [];
}
