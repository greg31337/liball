namespace asn1;

internal class XerWrappedValue
{
    internal bool Success;
    internal bool NeedMoreData;
    internal bool Empty;
    internal XerXmlAttribute[] Attributes = [];
    internal XerXmlNamespaceBinding[] NamespaceBindings = [];
    internal XerXmlToken[] Content = [];
    internal int ConsumedCount;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
