namespace image;

internal class MetadataXmlRoot
{
    internal string LocalName;
    internal string NamespaceUri;
    internal string Prefix;
    internal bool Found;

    internal MetadataXmlRoot()
    {
        LocalName = "";
        NamespaceUri = "";
        Prefix = "";
        Found = false;
    }
}
