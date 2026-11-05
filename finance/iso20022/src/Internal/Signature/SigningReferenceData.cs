namespace iso20022;

internal class SigningReferenceData
{
    internal DynamicXmlNode Node = new DynamicXmlNode();
    internal string Uri = string.Empty;
    internal XmlPathData Path = new XmlPathData();
    internal byte[] Digest = [];
}
