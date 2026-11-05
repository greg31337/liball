namespace asn1;

internal class XsdSchemaGraphResult
{
    internal bool Success;
    internal XsdDocumentInstance[] DocumentInstances = [];
    internal XsdSchemaGraphEdge[] Edges = [];
    internal XsdComponentDefinition[] Components = [];
    internal AsnDiagnostic[] Diagnostics = [];
}
