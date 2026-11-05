namespace asn1;

internal class XsdDocumentInstance
{
    internal string StableId = "";
    internal string DocumentId = "";
    internal string EffectiveTargetNamespace = "";
    internal XsdLoadedDocument? Document;
    internal XsdComponentDefinition[] Components = [];
}
