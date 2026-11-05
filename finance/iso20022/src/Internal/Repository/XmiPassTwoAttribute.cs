namespace iso20022;

internal class XmiPassTwoAttribute
{
    internal string NamespaceUri = string.Empty;
    internal string LocalName = string.Empty;
    internal string Prefix = string.Empty;
    internal string Value = string.Empty;
    internal int AttributeOrder;
    internal bool IsReference;
    internal bool HasLineInformation;
    internal int LineNumber;
    internal int ColumnNumber;
}
