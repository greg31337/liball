namespace docx;

internal class DocxRelationshipAttribute
{
    internal string Prefix;
    internal string LocalName;
    internal string NamespaceUri;
    internal string Value;

    internal DocxRelationshipAttribute(string prefix, string localName, string namespaceUri, string value)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (localName == "")
        {
            throw new ArgumentException("Attribute local name is required.", "localName");
        }

        this.Prefix = prefix;
        this.LocalName = localName;
        this.NamespaceUri = namespaceUri;
        this.Value = value;
    }
}
