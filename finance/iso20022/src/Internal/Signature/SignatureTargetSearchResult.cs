namespace iso20022;

internal class SignatureTargetSearchResult
{
    internal DynamicXmlNode? Node;
    internal XmlPathData Path = new XmlPathData();
    internal int Count;
}
