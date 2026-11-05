namespace asn1;

internal class XerXmlToken
{
    internal XerXmlTokenKind Kind;
    internal string Name = "";
    internal string Prefix = "";
    internal string LocalName = "";
    internal string NamespaceUri = "";
    internal string Value = "";
    internal XerXmlAttribute[] Attributes = [];
    internal XerXmlNamespaceBinding[] NamespaceBindings = [];
    internal string XmlVersion = "";
    internal string XmlEncoding = "";
    internal bool HasStandalone;
    internal bool Standalone;
    internal int ByteOffset;
    internal int ByteLength;
}
