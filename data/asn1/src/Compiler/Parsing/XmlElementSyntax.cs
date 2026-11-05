namespace asn1;

internal class XmlElementSyntax
{
    internal string Name = "";
    internal string LocalName = "";
    internal string Prefix = "";
    internal string NamespaceUri = "";
    internal string Text = "";
    internal XmlAttributeSyntax[] Attributes = [];
    internal XmlElementSyntax[] Children = [];
    internal bool Empty;
    internal int StartOffset;
    internal int EndOffset;
}
