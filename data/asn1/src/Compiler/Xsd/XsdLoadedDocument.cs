namespace asn1;

internal class XsdLoadedDocument
{
    internal string StableId = "";
    internal string Name = "";
    internal string ResolverUri = "";
    internal string EffectiveBaseUri = "";
    internal string TargetNamespace = "";
    internal XerXmlNamespaceBinding[] NamespaceBindings = [];
    internal XsdSchemaLocation[] SchemaLocations = [];
    internal XerXmlToken[] Tokens = [];
    internal XerXmlToken? SchemaToken;
    internal AsnXsdInput? Input;
}
