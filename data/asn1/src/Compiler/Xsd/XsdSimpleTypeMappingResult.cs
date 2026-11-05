namespace asn1;

internal class XsdSimpleTypeMappingResult
{
    internal bool Success;
    internal XsdSimpleTypeDefinition[] Types = [];
    internal XsdValueConstraintDefinition[] ValueConstraints = [];
    internal AsnDiagnostic[] Diagnostics = [];
}
