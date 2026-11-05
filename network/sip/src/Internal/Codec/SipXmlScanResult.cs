namespace sip;

internal class SipXmlScanResult
{
    internal bool Success;
    internal string? ErrorMessage;
    internal int ErrorOffset = -1;
    internal string? RootName;
    internal string? NamespaceUri;
    internal bool HasXmlDeclaration;
    internal string[] RootAttributeNames = new string[0];
    internal string[] RootAttributeValues = new string[0];
    internal string[] DirectChildNames = new string[0];
    internal string[] DirectChildNamespaces = new string[0];
}
