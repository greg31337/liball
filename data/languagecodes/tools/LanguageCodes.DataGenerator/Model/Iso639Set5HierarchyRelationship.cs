namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set5HierarchyRelationship
{
    internal Iso639Set5HierarchyRelationship(string parentCode, string childCode)
    {
        ParentCode = parentCode;
        ChildCode = childCode;
    }

    internal string ParentCode { get; }

    internal string ChildCode { get; }
}
