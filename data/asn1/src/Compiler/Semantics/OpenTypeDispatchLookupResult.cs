namespace asn1;

internal class OpenTypeDispatchLookupResult
{
    internal NormalizedDispatchKey? Key;
    internal AsnOpenTypeDispatchEntry? Entry;
    internal AsnDiagnostic[] Diagnostics = [];
    internal bool Success;
}
