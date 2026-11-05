namespace asn1;

internal class XsdDeclarationMappingResult
{
    internal bool Success;
    internal XsdDeclarationDefinition[] Declarations = [];
    internal XsdDeclarationDefinition[] Elements = [];
    internal XsdDeclarationDefinition[] Attributes = [];
    internal AsnDiagnostic[] Diagnostics = [];
}
