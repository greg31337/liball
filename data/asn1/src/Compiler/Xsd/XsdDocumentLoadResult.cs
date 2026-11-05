namespace asn1;

internal class XsdDocumentLoadResult
{
    internal bool Success;
    internal XsdLoadedDocument[] Documents = [];
    internal XsdResolverInput[] ResolverInputs = [];
    internal AsnDiagnostic[] Diagnostics = [];
}
