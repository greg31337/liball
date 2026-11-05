namespace asn1;

internal class XsdSchemaGraphEdge
{
    internal string Kind = "";
    internal string SourceInstanceId = "";
    internal string TargetInstanceId = "";
    internal bool Chameleon;
    internal bool Cycle;
    internal XsdSchemaLocation? Location;
}
