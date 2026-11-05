namespace iso20022;

internal class ProfileComparisonEntry
{
    internal string Key = string.Empty;
    internal string Identifier = string.Empty;
    internal string Name = string.Empty;
    internal string PropertyName = string.Empty;
    internal string Value = string.Empty;
    internal XmlPathData Path = new XmlPathData();
    internal RepositoryEntityKind EntityKind;
}
