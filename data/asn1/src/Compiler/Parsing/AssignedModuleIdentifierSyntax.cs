namespace asn1;

internal class AssignedModuleIdentifierSyntax
{
    internal bool HasObjectIdentifier;
    internal AssignedOidComponentSyntax[] ObjectIdentifierComponents = [];
    internal bool HasDefinedValue;
    internal string DefinedValueModule = "";
    internal string DefinedValue = "";
    internal bool HasIri;
    internal string Iri = "";
    internal AsnSourceSpan? Span;
}
