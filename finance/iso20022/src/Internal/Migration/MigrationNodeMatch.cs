namespace iso20022;

internal class MigrationNodeMatch
{
    internal DynamicXmlNode Node = new DynamicXmlNode();
    internal DynamicXmlNode? Parent;
    internal int Index;
    internal string Path = string.Empty;
}
